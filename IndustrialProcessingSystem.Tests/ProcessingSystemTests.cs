using System;
using System.Threading.Tasks;
using Xunit;
using IndustrialProcessingSystem.Core.Models;
using IndustrialProcessingSystem.Core.Processing;
using IndustrialProcessingSystem.Core.Configuration;

namespace IndustrialProcessingSystem.Tests
{
    public class ProcessingSystemTests
    {
        [Fact]
        public void SubmitJob_Success_ReturnsHandle()
        {
            using var system = new ProcessingSystem(workerCount: 1, maxQueueSize: 10);
            var job = new Job { Payload = "delay:50", Type = JobType.IO, Priority = 1 };

            var handle = system.Submit(job);

            Assert.NotNull(handle);
            Assert.Equal(job.Id, handle.Id);
        }

        [Fact]
        public void SubmitJob_QueueFull_ThrowsException()
        {
            using var system = new ProcessingSystem(workerCount: 0, maxQueueSize: 1); // 0 workers da se ne bi praznio red
            var job1 = new Job { Payload = "delay:50", Type = JobType.IO };
            system.Submit(job1);

            var job2 = new Job { Payload = "delay:50", Type = JobType.IO };
            Assert.Throws<InvalidOperationException>(() => system.Submit(job2));
        }

        [Fact]
        public void SubmitJob_IdempotencyViolation_ThrowsException()
        {
            using var system = new ProcessingSystem(workerCount: 0, maxQueueSize: 10);
            var job1 = new Job { Payload = "delay:10", Type = JobType.IO };
            system.Submit(job1);

            Assert.Throws<InvalidOperationException>(() => system.Submit(job1));
        }

        [Fact]
        public void SystemConfig_LoadInvalidPath_ReturnsEmpty()
        {
            var config = SystemConfig.Load("invalid_path.xml");
            Assert.NotNull(config);
            Assert.Empty(config.Jobs);
        }

        [Fact]
        public async Task JobExecution_IO_Success()
        {
            using var system = new ProcessingSystem(workerCount: 1, maxQueueSize: 10);
            var job = new Job { Payload = "delay:10", Type = JobType.IO };
            var handle = system.Submit(job);

            var completed = await Task.WhenAny(handle.Result, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.Same(handle.Result, completed);

            var result = await handle.Result;
            Assert.InRange(result, 0, 100);

            var storedJob = system.GetJob(job.Id);
            Assert.NotNull(storedJob);
            Assert.Equal(job.Id, storedJob.Id);
        }

        [Fact]
        public async Task JobExecution_Prime_Success()
        {
            using var system = new ProcessingSystem(workerCount: 2, maxQueueSize: 10);
            var job = new Job { Payload = "numbers:10,threads:1", Type = JobType.Prime };
            var handle = system.Submit(job);

            var completed = await Task.WhenAny(handle.Result, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.Same(handle.Result, completed);

            var result = await handle.Result;
            Assert.Equal(4, result);
        }

        [Fact]
        public async Task JobExecution_Timeout_FailsAndRaisesEvent()
        {
            using var system = new ProcessingSystem(workerCount: 1, maxQueueSize: 10);
            var job = new Job { Payload = "delay:3000", Type = JobType.IO };

            var failedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            system.JobFailed += (sender, args) =>
            {
                if (args.Job.Id == job.Id)
                    failedTcs.TrySetResult(true);
            };

            system.Submit(job);

            var completed = await Task.WhenAny(failedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(failedTcs.Task, completed);
        }
    }
}
