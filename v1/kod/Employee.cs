using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vezbe1_grupa1
{
    internal sealed class Employee : Person
    {
        public double Salary { get; set; }

        public sealed override string GetFullName()
        {
            return $"{base.GetFullName()}, plata mi je: {this.Salary}";
        }

        //public override void GetId()
        //{
        //    throw new NotImplementedException();
        //}
    }
}
