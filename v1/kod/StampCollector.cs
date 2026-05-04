using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vezbe1_grupa1
{
    internal class StampCollector : Person, IStampCollector
    {
        public List<Stamp> Stamps { get; set; }

        public void PrintAllStamps()
        {
            foreach (var stamp in Stamps)
            {
                Console.WriteLine(stamp.ToString());
            }
        }
    }
}
