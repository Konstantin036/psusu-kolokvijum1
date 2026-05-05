using System;
using System.Threading.Tasks;

namespace IndustrialProcessingSystem.Core.Reporting
{
    internal interface IEventLogger
    {
        Task LogAsync(DateTime timestamp, string status, Guid jobId, int result);
    }
}
