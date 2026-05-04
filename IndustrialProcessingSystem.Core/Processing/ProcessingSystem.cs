using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using IndustrialProcessingSystem.Core.Models;

namespace IndustrialProcessingSystem.Core.Processing
{
    public class JobEventsArgs : EventArgs
    {
        public required Job Job { get; set; }
        public int Result { get; set; }
        public Exception? Exception { get; set; }
    }

    public class ProcessingSystem : IDisposable
    {
        private readonly int _maxQueueSize;
        private readonly ConcurrentPriorityQueue<int, Job> _queue;

        // idempotency + handle access
        private readonly ConcurrentDictionary<Guid, Job> _jobHistory;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<int>> _jobCompletions;

        private readonly CancellationTokenSource _cts;
        private readonly List<Task> _workers;

        private readonly IJobProcessor _processor;
        private readonly IEventLogger _eventLogger;
        private readonly IReportWriter _reportWriter;
        private readonly PeriodicTimer? _reportTimer;
        
        // Eventi
        public event EventHandler<JobEventsArgs>? JobCompleted;
        public event EventHandler<JobEventsArgs>? JobFailed;

        // Metrika za izvestaj
        public ConcurrentBag<JobInfo> ExecutedJobs { get; } = new();

        public ProcessingSystem(int workerCount, int maxQueueSize)
            : this(
                workerCount,
                maxQueueSize,
                new JobProcessor(),
                new FileEventLogger("events.log"),
                new RollingXmlReportWriter(".", maxFilesToKeep: 10),
                startReportLoop: true)
        {
        }

        // dodatni ctor za testove (brže i deterministi?nije)
        internal ProcessingSystem(
            int workerCount,
            int maxQueueSize,
            IJobProcessor processor,
            IEventLogger eventLogger,
            IReportWriter reportWriter,
            bool startReportLoop)
        {
            if (maxQueueSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxQueueSize));
            if (workerCount < 0) throw new ArgumentOutOfRangeException(nameof(workerCount));

            _maxQueueSize = maxQueueSize;
            _queue = new ConcurrentPriorityQueue<int, Job>();
            _jobHistory = new ConcurrentDictionary<Guid, Job>();
            _jobCompletions = new ConcurrentDictionary<Guid, TaskCompletionSource<int>>();

            _processor = processor;
            _eventLogger = eventLogger;
            _reportWriter = reportWriter;

            _cts = new CancellationTokenSource();
            _workers = new List<Task>(workerCount);

            // lambda subscriptions (zahtev)
            JobCompleted += (sender, args) => _ = _eventLogger.LogAsync(DateTime.Now, "Completed", args.Job.Id, args.Result);
            JobFailed += (sender, args) => _ = _eventLogger.LogAsync(DateTime.Now, "Failed", args.Job.Id, args.Result);

            for (int i = 0; i < workerCount; i++)
                _workers.Add(Task.Run(() => WorkerLoop(_cts.Token)));

            if (startReportLoop)
            {
                _reportTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
                _ = Task.Run(ReportLoop);
            }
        }

        public JobHandle Submit(Job job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));

            // idempotency: isti Job.Id samo jednom
            if (!_jobHistory.TryAdd(job.Id, job))
                throw new InvalidOperationException("Idempotency violation");

            if (_queue.Count >= _maxQueueSize)
            {
                // ako ne može u red, vrati stanje kao da nije ni prihva?en
                _jobHistory.TryRemove(job.Id, out _);
                throw new InvalidOperationException("Queue is full");
            }

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_jobCompletions.TryAdd(job.Id, tcs))
            {
                _jobHistory.TryRemove(job.Id, out _);
                throw new InvalidOperationException("Idempotency violation");
            }

            _queue.Enqueue(job.Priority, job);

            return new JobHandle { Id = job.Id, Result = tcs.Task };
        }

        public IEnumerable<Job> GetTopJobs(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            return _queue.GetUnorderedItems()
                .OrderBy(x => x.Priority)
                .Select(x => x.Element)
                .Take(n)
                .ToList();
        }

        public Job? GetJob(Guid id)
        {
            _jobHistory.TryGetValue(id, out var job);
            return job;
        }

        private async Task WorkerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var job))
                {
                    await ProcessJobWithRetry(job, token).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(20, token).ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessJobWithRetry(Job job, CancellationToken token)
        {
            const int maxRetries = 2; // + original = 3 total

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var execTask = _processor.ProcessAsync(job, token);
                    var completed = await Task.WhenAny(execTask, Task.Delay(TimeSpan.FromSeconds(2), token)).ConfigureAwait(false);

                    if (completed != execTask)
                        throw new TimeoutException("Job took more than 2 seconds.");

                    var result = await execTask.ConfigureAwait(false);
                    sw.Stop();

                    if (_jobCompletions.TryGetValue(job.Id, out var tcs))
                        tcs.TrySetResult(result);

                    ExecutedJobs.Add(new JobInfo { Type = job.Type, Success = true, Duration = sw.Elapsed.TotalMilliseconds });
                    JobCompleted?.Invoke(this, new JobEventsArgs { Job = job, Result = result });
                    return;
                }
                catch (Exception ex) when (!token.IsCancellationRequested)
                {
                    sw.Stop();

                    if (attempt >= maxRetries)
                    {
                        ExecutedJobs.Add(new JobInfo { Type = job.Type, Success = false, Duration = sw.Elapsed.TotalMilliseconds });

                        // ABORT log (zahtev: ako i tre?i put failuje)
                        await _eventLogger.LogAsync(DateTime.Now, "ABORT", job.Id, 0).ConfigureAwait(false);

                        if (_jobCompletions.TryGetValue(job.Id, out var tcs))
                            tcs.TrySetCanceled(); // rezultat se ignoriše

                        JobFailed?.Invoke(this, new JobEventsArgs { Job = job, Result = 0, Exception = ex });
                        return;
                    }
                }
            }
        }

        private async Task ReportLoop()
        {
            if (_reportTimer == null) return;

            try
            {
                while (await _reportTimer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
                {
                    GenerateReport();
                }
            }
            catch (OperationCanceledException)
            {
                // shutdown
            }
        }

        private void GenerateReport()
        {
            var snapshot = ExecutedJobs.ToList();

            var successfulByType = snapshot
                .Where(x => x.Success)
                .GroupBy(x => x.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    AvgDuration = g.Average(x => x.Duration)
                })
                .ToList();

            var failedByType = snapshot
                .Where(x => !x.Success)
                .GroupBy(x => x.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Type)
                .ToList();

            XElement root = new XElement("Report",
                new XElement("GeneratedAt", DateTime.Now));

            foreach (var s in successfulByType)
            {
                root.Add(new XElement("Success",
                    new XAttribute("Type", s.Type),
                    new XAttribute("Count", s.Count),
                    new XAttribute("AvgTimeMs", s.AvgDuration)));
            }

            foreach (var f in failedByType)
            {
                root.Add(new XElement("Failure",
                    new XAttribute("Type", f.Type),
                    new XAttribute("Count", f.Count)));
            }

            _reportWriter.Write(root);
        }

        public void Dispose()
        {
            _cts.Cancel();

            try
            {
                Task.WaitAll(_workers.ToArray(), TimeSpan.FromSeconds(5));
            }
            catch
            {
                // ignore shutdown issues
            }

            _reportTimer?.Dispose();
            _cts.Dispose();
        }
    }

    public class JobInfo
    {
        public JobType Type { get; set; }
        public bool Success { get; set; }
        public double Duration { get; set; }
    }

    internal interface IJobProcessor
    {
        Task<int> ProcessAsync(Job job, CancellationToken token);
    }

    internal sealed class JobProcessor : IJobProcessor
    {
        private static readonly ThreadLocal<Random> _rng = new(() => new Random());

        public Task<int> ProcessAsync(Job job, CancellationToken token)
        {
            return Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();

                return job.Type switch
                {
                    JobType.Prime => ProcessPrime(job.Payload),
                    JobType.IO => ProcessIo(job.Payload),
                    _ => 0
                };
            }, token);
        }

        private static int ProcessPrime(string payload)
        {
            // payload: numbers:10000,threads:3
            var parts = payload.Split(',');
            int limit = int.Parse(parts[0].Split(':')[1]);
            int threadsRaw = int.Parse(parts[1].Split(':')[1]);
            int threads = Math.Clamp(threadsRaw, 1, 8);

            return CalculatePrimesCount(limit, threads);
        }

        private static int ProcessIo(string payload)
        {
            // payload: delay:1000
            var parts = payload.Split(':');
            int delayMs = int.Parse(parts[1]);
            Thread.Sleep(delayMs);
            return _rng.Value!.Next(0, 101);
        }

        private static int CalculatePrimesCount(int limit, int threads)
        {
            int count = 0;
            Parallel.For(2, limit, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
            {
                bool isPrime = true;
                int boundary = (int)Math.Sqrt(i);
                for (int j = 2; j <= boundary; j++)
                {
                    if (i % j == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }

                if (isPrime)
                    Interlocked.Increment(ref count);
            });
            return count;
        }
    }

    internal interface IEventLogger
    {
        Task LogAsync(DateTime timestamp, string status, Guid jobId, int result);
    }

    internal sealed class FileEventLogger : IEventLogger
    {
        private readonly string _path;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public FileEventLogger(string path)
        {
            _path = path;
        }

        public async Task LogAsync(DateTime timestamp, string status, Guid jobId, int result)
        {
            string line = $"[{timestamp:O}] [{status}] {jobId}, {result}{Environment.NewLine}";

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await File.AppendAllTextAsync(_path, line).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    internal interface IReportWriter
    {
        void Write(XElement report);
    }

    internal sealed class RollingXmlReportWriter : IReportWriter
    {
        private readonly string _directory;
        private readonly int _maxFilesToKeep;

        public RollingXmlReportWriter(string directory, int maxFilesToKeep)
        {
            _directory = directory;
            _maxFilesToKeep = maxFilesToKeep;
        }

        public void Write(XElement report)
        {
            Directory.CreateDirectory(_directory);

            string reportName = Path.Combine(_directory, $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
            report.Save(reportName);

            var reports = Directory.GetFiles(_directory, "Report_*.xml")
                .OrderBy(x => x)
                .ToList();

            while (reports.Count > _maxFilesToKeep)
            {
                File.Delete(reports[0]);
                reports.RemoveAt(0);
            }
        }
    }

    public class ConcurrentPriorityQueue<TPriority, TElement> where TPriority : IComparable<TPriority>
    {
        private readonly List<(TPriority Priority, TElement Element)> _elements = new();
        private readonly object _sync = new();

        public int Count
        {
            get
            {
                lock (_sync)
                    return _elements.Count;
            }
        }

        public void Enqueue(TPriority priority, TElement item)
        {
            lock (_sync)
            {
                _elements.Add((priority, item));
                _elements.Sort((x, y) => x.Priority.CompareTo(y.Priority));
            }
        }

        public bool TryDequeue(out TElement item)
        {
            lock (_sync)
            {
                if (_elements.Count == 0)
                {
                    item = default!;
                    return false;
                }

                item = _elements[0].Element;
                _elements.RemoveAt(0);
                return true;
            }
        }

        public IEnumerable<(TPriority Priority, TElement Element)> GetUnorderedItems()
        {
            lock (_sync)
            {
                return _elements.ToList();
            }
        }
    }
}
