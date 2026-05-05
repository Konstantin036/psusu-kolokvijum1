using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IndustrialProcessingSystem.Core.Configuration;
using IndustrialProcessingSystem.Core.Models;
using IndustrialProcessingSystem.Core.Processing;

namespace IndustrialProcessingSystem.Core
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing Industrial Processing System API...");

            string configPath = "SystemConfig.xml";
            if (!File.Exists(configPath))
            {
                Console.WriteLine("SystemConfig.xml not found! Make sure it's in the working directory.");
                return;
            }

            var config = SystemConfig.Load(configPath);
            int workerCount = config.WorkerCount == 0 ? 5 : config.WorkerCount;
            int maxQueueSize = config.MaxQueueSize == 0 ? 100 : config.MaxQueueSize;

            using var system = new ProcessingSystem(workerCount, maxQueueSize);

            // Inicijalno ucitavanje poslova iz fajla
            foreach (var job in config.Jobs)
            {
                try
                {
                    system.Submit(job);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error submitting initial job {job.Id}: {ex.Message}");
                }
            }

            // Random task generation na odredjen broj niti (npr workerCount jer nije receno iz konfiguracije)
            // U tekstu: "procitati broj niti iz konfiguracionog fajla", "pokrenuti odgovarajuci broj niti koje nasumicno dodaju nove poslove"
            // Znaci broj niti za dadavanje poslova takodje zavisi od worker count
            CancellationTokenSource cts = new CancellationTokenSource();
            var producerTasks = new List<Task>();
            for (int i = 0; i < workerCount; i++)
            {
                producerTasks.Add(Task.Run(() => ProduceJobsRandomly(system, cts.Token)));
            }

            Console.WriteLine("System is running. Press ENTER to simulate system shutdown...");
            Console.ReadLine();

            cts.Cancel();
            await Task.WhenAll(producerTasks);
            Console.WriteLine("Shutting down the processing system...");
        }

        static async Task ProduceJobsRandomly(ProcessingSystem system, CancellationToken token)
        {
            var rnd = new Random();
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(rnd.Next(1000, 3000), token);

                    var job = new Job
                    {
                        Priority = rnd.Next(1, 10)
                    };

                    if (rnd.Next(0, 2) == 0)
                    {
                        job.Type = JobType.Prime;
                        job.Payload = $"numbers:{rnd.Next(5000, 25000)},threads:{rnd.Next(1, 9)}";
                    }
                    else
                    {
                        job.Type = JobType.IO;
                        job.Payload = $"delay:{rnd.Next(500, 2500)}";
                    }

                    system.Submit(job);
                    Console.WriteLine($"Produced a new job: {job.Id} (Prio: {job.Priority})");
                }
                catch (TaskCanceledException)
                {
                    // Ignore on shutdown
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Queue busy/full: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Producer Error: {ex.Message}");
                }
            }
        }
    }
}
