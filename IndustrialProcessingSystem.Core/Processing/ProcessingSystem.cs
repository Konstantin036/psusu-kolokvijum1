using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using IndustrialProcessingSystem.Core.Infrastructure;
using IndustrialProcessingSystem.Core.Models;
using IndustrialProcessingSystem.Core.Reporting;
using IndustrialProcessingSystem.Core.Services;

namespace IndustrialProcessingSystem.Core.Processing
{
    public class ProcessingSystem : IDisposable
    {
        private readonly ConcurrentPriorityQueue<int, Job> _queue;
        private readonly SemaphoreSlim _queueSpace;
        private readonly ConcurrentDictionary<Guid, Job> _jobHistory;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<int>> _jobCompletions;
        private readonly CancellationTokenSource _cts;
        private readonly List<Task> _workers;
        private readonly IJobProcessor _processor;
        private readonly IEventLogger _eventLogger;
        private readonly IReportWriter _reportWriter;
        private readonly PeriodicTimer? _reportTimer;

        public event EventHandler<JobEventsArgs>? JobCompleted;
        public event EventHandler<JobEventsArgs>? JobFailed;

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

        internal ProcessingSystem(
            int workerCount,
            int maxQueueSize,
            IJobProcessor processor,
            IEventLogger eventLogger,
            IReportWriter reportWriter,
            bool startReportLoop)
        {
            if (workerCount < 0) throw new ArgumentOutOfRangeException(nameof(workerCount));
            if (maxQueueSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxQueueSize));

            _queue = new ConcurrentPriorityQueue<int, Job>();
            _queueSpace = new SemaphoreSlim(maxQueueSize, maxQueueSize);
            _jobHistory = new ConcurrentDictionary<Guid, Job>();
            _jobCompletions = new ConcurrentDictionary<Guid, TaskCompletionSource<int>>();
            _cts = new CancellationTokenSource();
            _workers = new List<Task>(workerCount);
            _processor = processor;
            _eventLogger = eventLogger;
            _reportWriter = reportWriter;

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

            // Idempotentnost: isti posao se prihvata samo jednom, bez obzira na broj niti.
            if (!_jobHistory.TryAdd(job.Id, job))
                throw new InvalidOperationException("Idempotency violation");

            // Semaphore cuva ukupan broj aktivnih poslova: i one u redu i one koji se trenutno rade.
            if (!_queueSpace.Wait(0))
            {
                _jobHistory.TryRemove(job.Id, out _);
                throw new InvalidOperationException("Queue is full");
            }

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_jobCompletions.TryAdd(job.Id, tcs))
            {
                _jobHistory.TryRemove(job.Id, out _);
                _queueSpace.Release();
                throw new InvalidOperationException("Idempotency violation");
            }

            _queue.Enqueue(job.Priority, job);

            return new JobHandle { Id = job.Id, Result = tcs.Task };
        }

        public IEnumerable<Job> GetTopJobs(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));

            return _queue.GetItems()
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
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Worker stalno pokusava da uzme najprioritetniji posao iz reda.
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
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ProcessJobWithRetry(Job job, CancellationToken token)
        {
            const int maxRetries = 2;

            try
            {
                for (int attempt = 0; attempt <= maxRetries; attempt++)
                {
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        var execTask = _processor.ProcessAsync(job, token);
                        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), token);
                        var completed = await Task.WhenAny(execTask, timeoutTask).ConfigureAwait(false);

                        // Posao se smatra neuspesnim ako ne zavrsi u roku od 2 sekunde.
                        if (completed != execTask)
                            throw new TimeoutException("Job took more than 2 seconds.");

                        var result = await execTask.ConfigureAwait(false);
                        sw.Stop();

                        if (_jobCompletions.TryGetValue(job.Id, out var tcs))
                            tcs.TrySetResult(result);

                        ExecutedJobs.Add(new JobInfo(job.Type, success: true, sw.Elapsed.TotalMilliseconds));
                        JobCompleted?.Invoke(this, new JobEventsArgs(job, result));
                        return;
                    }
                    catch (Exception ex) when (!token.IsCancellationRequested)
                    {
                        sw.Stop();

                        // Ukupno postoje 3 pokusaja: originalni pokusaj + 2 retry pokusaja.
                        if (attempt < maxRetries)
                            continue;

                        ExecutedJobs.Add(new JobInfo(job.Type, success: false, sw.Elapsed.TotalMilliseconds));
                        await _eventLogger.LogAsync(DateTime.Now, "ABORT", job.Id, 0).ConfigureAwait(false);

                        if (_jobCompletions.TryGetValue(job.Id, out var tcs))
                            tcs.TrySetCanceled(token);

                        JobFailed?.Invoke(this, new JobEventsArgs(job, 0, ex));
                    }
                }
            }
            finally
            {
                // Kapacitet se oslobadja tek kada je posao stvarno gotov.
                _queueSpace.Release();
            }
        }

        private async Task ReportLoop()
        {
            if (_reportTimer == null) return;

            try
            {
                while (await _reportTimer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
                    GenerateReport();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void GenerateReport()
        {
            // Snapshot pravi stabilnu sliku trenutne metrike dok worker niti nastavljaju da rade.
            var snapshot = ExecutedJobs.ToList();

            var successfulByType = snapshot
                .Where(x => x.Success)
                .GroupBy(x => x.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    AvgDuration = g.Average(x => x.DurationMs)
                });

            var failedByType = snapshot
                .Where(x => !x.Success)
                .GroupBy(x => x.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Type);

            var root = new XElement("Report", new XElement("GeneratedAt", DateTime.Now));

            foreach (var item in successfulByType)
            {
                root.Add(new XElement("Success",
                    new XAttribute("Type", item.Type),
                    new XAttribute("Count", item.Count),
                    new XAttribute("AvgTimeMs", item.AvgDuration)));
            }

            foreach (var item in failedByType)
            {
                root.Add(new XElement("Failure",
                    new XAttribute("Type", item.Type),
                    new XAttribute("Count", item.Count)));
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
            }

            _reportTimer?.Dispose();
            _queueSpace.Dispose();
            _cts.Dispose();
        }
    }
}
