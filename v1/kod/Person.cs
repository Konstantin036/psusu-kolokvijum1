using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vezbe1_grupa1
{
    internal /*abstract*/ class Person
    {
        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }
        //public int Id { get; set; }
        public string Name { get; set; }
        protected DateTime Born {  get; set; }
        public virtual string GetFullName()
        {
            return $"Moje ime je: {Name}, Id: {Id}";
        }
        //public abstract void GetId();
    }
}
