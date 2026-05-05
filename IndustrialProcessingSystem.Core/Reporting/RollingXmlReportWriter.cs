using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace IndustrialProcessingSystem.Core.Reporting
{
    internal sealed class RollingXmlReportWriter : IReportWriter
    {
        private readonly string _directory;
        private readonly int _maxFilesToKeep;

        public RollingXmlReportWriter(string directory, int maxFilesToKeep)
        {
            _directory = directory;
            _maxFilesToKeep = maxFilesToKeep;
        }

        public void Write(XElement report)
        {
            Directory.CreateDirectory(_directory);

            string reportPath = Path.Combine(_directory, $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.xml");
            report.Save(reportPath);

            var reports = Directory.GetFiles(_directory, "Report_*.xml")
                .OrderBy(path => path)
                .ToList();

            while (reports.Count > _maxFilesToKeep)
            {
                File.Delete(reports[0]);
                reports.RemoveAt(0);
            }
        }
    }
}
