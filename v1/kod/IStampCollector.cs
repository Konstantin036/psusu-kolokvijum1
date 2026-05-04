using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vezbe1_grupa1
{
    internal interface IStampCollector
    {
        List<Stamp> Stamps { get; set; }
        void PrintAllStamps();
    }
    public class Stamp
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Stamp(int id, string name)
        {
            Id = id;
            Name = name;
        }
        public override string ToString()
        {
            return $"{Name}:[{Id}]";
        }
    }
}
