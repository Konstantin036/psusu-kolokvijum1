using System;
using System.Threading.Tasks;

namespace IndustrialProcessingSystem.Core.Models
{
    public class JobHandle
    {
        public Guid Id { get; set; }
        public Task<int> Result { get; set; }
    }
}
