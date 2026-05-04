using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace v2_grupa3
{
    internal class Events
    {
        //private int id;
        //public int Id
        //{
        //    get { return id; }
        //    set { id = value; }
        //}
        public int Id { get; set;  }

        //private event FirstDelegate event1;
        //public event FirstDelegate Event1
        //{
        //    add
        //    {
        //        Console.WriteLine("subscribed");
        //    }
        //    remove
        //    {
        //        Console.WriteLine("unsubscribed");
        //    }
        //}
        //public event FirstDelegate Event1
        //{
        //    add
        //    {
        //        lock(this)
        //        {
        //            event1 += value;
        //        }
        //    }
        //    remove
        //    {
        //        lock (this)
        //        {
        //            event1 -= value;
        //        }
        //    }
        //}
        public event FirstDelegate Event1;

        public void Simulation()
        {
            for (int i=0; i<100; i+=5)
            {
                //if (Event1 != null)
                //{
                //    Event1.Invoke(i);
                //}
                Event1?.Invoke(i);

                Thread.Sleep(500);
            }
        }
    }
}
