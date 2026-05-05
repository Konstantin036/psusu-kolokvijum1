using IndustrialProcessingSystem.Core.Models;

namespace IndustrialProcessingSystem.Core.Processing
{
    public class JobInfo
    {
        public JobInfo(JobType type, bool success, double durationMs)
        {
            Type = type;
            Success = success;
            DurationMs = durationMs;
        }

        public JobType Type { get; }
        public bool Success { get; }
        public double DurationMs { get; }
    }
}
