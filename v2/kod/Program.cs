using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace v2_grupa3
{
    internal class Program
    {
        public delegate int ThirdDelegate(int a, int b);
        static void Main(string[] args)
        {
            Sample s = new Sample();
            s.DefineDelegates();
            //s.d1.Invoke(5);
            //s.d2.Invoke(55);
            //s.d3.Invoke(52);
            //s.d4.Invoke(2);

            FirstDelegate combinedDelegate = s.d1 + s.d2 + s.d3 + s.d4 + s.d5 + s.d6;
            //combinedDelegate(20);

            ThirdDelegate d7 = Add;
            ThirdDelegate d8 = Sub;
            ThirdDelegate combinedDelegate2 = d7 + d8;
            int res = combinedDelegate2(15, 5);
            //Console.WriteLine(res);

            List<int> results = new List<int>();
            foreach (ThirdDelegate d in combinedDelegate2.GetInvocationList())
            {
                results.Add(d(15, 5));
            }
            //foreach(int result in results)
            //{
            //    Console.WriteLine(result);
            //}

            Events events = new Events();
            //events.Event1 = combinedDelegate2;
            //events.Event1(20);
            events.Event1 += PoolLevel;
            events.Event1 += (lev) => { Console.WriteLine($"Level is {lev} in lambda func"); };
            events.Event1 += PoolLevel;
            events.Event1 -= null;
            // += DbLogger
            // += FileLogger
            // += WebClientGui
            // += ConsoleWrite

            Action hello = () => Console.WriteLine("Hi!");
            hello();
            //Predicate

            events.Simulation();
        }
        public static void PoolLevel(int y)
        {
            Console.WriteLine($"Pool level is: {y} at {DateTime.Now}");
        }
        public static int Add(int x, int y)
        {
            return x + y;
        }
        public static int Sub(int x, int y)
        {
            return x - y;
        }
    }
}
