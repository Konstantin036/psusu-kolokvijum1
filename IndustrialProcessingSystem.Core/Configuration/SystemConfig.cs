using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace IndustrialProcessingSystem.Core.Configuration
{
    public class SystemConfig
    {
        public int WorkerCount { get; set; }
        public int MaxQueueSize { get; set; }
        public List<Models.Job> Jobs { get; set; } = new List<Models.Job>();

        public static SystemConfig Load(string filePath)
        {
            var config = new SystemConfig();
            if (!File.Exists(filePath)) return config;

            var xml = XDocument.Load(filePath);
            var root = xml.Root;
            if (root != null)
            {
                if (int.TryParse(root.Element("WorkerCount")?.Value, out int workers))
                    config.WorkerCount = workers;
                if (int.TryParse(root.Element("MaxQueueSize")?.Value, out int maxQueue))
                    config.MaxQueueSize = maxQueue;

                var jobsNode = root.Element("Jobs");
                if (jobsNode != null)
                {
                    foreach (var jobNode in jobsNode.Elements("Job"))
                    {
                        var job = new Models.Job();
                        var typeStr = jobNode.Attribute("Type")?.Value;
                        if (Enum.TryParse<Models.JobType>(typeStr, out var jobType))
                            job.Type = jobType;

                        job.Payload = jobNode.Attribute("Payload")?.Value;

                        if (int.TryParse(jobNode.Attribute("Priority")?.Value, out int priority))
                            job.Priority = priority;

                        config.Jobs.Add(job);
                    }
                }
            }

            return config;
        }
    }
}
