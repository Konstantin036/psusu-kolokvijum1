using System;
using IndustrialProcessingSystem.Core.Models;

namespace IndustrialProcessingSystem.Core.Processing
{
    public class JobEventsArgs : EventArgs
    {
        public JobEventsArgs(Job job, int result, Exception? exception = null)
        {
            Job = job;
            Result = result;
            Exception = exception;
        }

        public Job Job { get; }
        public int Result { get; }
        public Exception? Exception { get; }
    }
}
