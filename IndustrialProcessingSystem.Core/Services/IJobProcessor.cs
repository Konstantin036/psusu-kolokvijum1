using System.Threading;
using System.Threading.Tasks;
using IndustrialProcessingSystem.Core.Models;

namespace IndustrialProcessingSystem.Core.Services
{
    internal interface IJobProcessor
    {
        Task<int> ProcessAsync(Job job, CancellationToken token);
    }
}
