using System;
using System.Threading;
using System.Threading.Tasks;
using IndustrialProcessingSystem.Core.Models;

namespace IndustrialProcessingSystem.Core.Services
{
    internal sealed class JobProcessor : IJobProcessor
    {
        private static readonly ThreadLocal<Random> Rng = new(() => new Random());

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
            var (limit, threads) = PayloadParser.ParsePrimePayload(payload);
            return CountPrimes(limit, threads);
        }

        private static int ProcessIo(string payload)
        {
            int delayMs = PayloadParser.ParseIoDelay(payload);
            Thread.Sleep(delayMs);
            return Rng.Value!.Next(0, 101);
        }

        private static int CountPrimes(int limit, int threads)
        {
            int count = 0;

            // Parallel.For deli opseg brojeva na najvise onoliko niti koliko payload dozvoljava.
            Parallel.For(2, limit, new ParallelOptions { MaxDegreeOfParallelism = threads }, number =>
            {
                if (IsPrime(number))
                    Interlocked.Increment(ref count);
            });

            return count;
        }

        private static bool IsPrime(int number)
        {
            if (number < 2) return false;

            int boundary = (int)Math.Sqrt(number);
            for (int divisor = 2; divisor <= boundary; divisor++)
            {
                if (number % divisor == 0)
                    return false;
            }

            return true;
        }
    }
}
