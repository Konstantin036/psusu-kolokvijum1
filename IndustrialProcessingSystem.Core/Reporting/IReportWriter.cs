using System.Xml.Linq;

namespace IndustrialProcessingSystem.Core.Reporting
{
    internal interface IReportWriter
    {
        void Write(XElement report);
    }
}
