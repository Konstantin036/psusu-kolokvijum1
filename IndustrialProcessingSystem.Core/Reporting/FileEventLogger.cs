using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IndustrialProcessingSystem.Core.Reporting
{
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
}
