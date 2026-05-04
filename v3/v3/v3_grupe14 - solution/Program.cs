using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace v3_grupe14
{
    internal class Program
    {
        static bool done = false;
        private static readonly object locker = new object();
        private static readonly object lvlLocker = new object();
        static void Main(string[] args)
        {

            //Thread t = new Thread(WriteY);
            //t.Start();
            //Thread.Sleep(50);

            //t.Join();

            //for (int i = 0; i < 1000; i++) Console.Write("x");

            //Thread t = new Thread(WriteY);
            //t.Start();

            //WriteY();

            //new Thread(Go).Start();
            //Go();

            //Thread t3 = new Thread(() => SayMyName1("Heisenberg"));
            //t3.Start();

            //Thread t4 = new Thread(SayMyName2);
            //t4.Start("Heisenberg2");

            Console.WriteLine("working...");

            WriteInDb();

            Console.WriteLine("working...");

            Task task1 = Task.Run(() => WriteInDbAsync());

            Console.WriteLine("working...");
            
            Task<string> task2 = Task.Run(() => ReadFromDbAsync());
            
            Console.WriteLine("working...");

            task1.Wait();

            Console.WriteLine($"Value from Db: {task2.Result}");
        }
        static async Task<string> ReadFromDbAsync()
        {
            await Task.Delay(3000);
            return "Heisenberg";
        }
        static async Task WriteInDbAsync()
        {
            await Task.Delay(3000);
            Console.WriteLine("done");
        }
        static void WriteInDb()
        {
            Thread.Sleep(3000);
            Console.WriteLine("done");
        }
        //static void SayMyName3(object studentObj)
        //{
        //    string name = ((Student)studentObj).Name;
        //    int index = ((Student)studentObj).Id;
        //    Console.WriteLine($"my name is: {name}");
        //}
        static void SayMyName2(object nameObj)
        {
            string name = (string)nameObj;
            Console.WriteLine($"my name is: {name}");
        }
        static void SayMyName1(string name)
        {
            Console.WriteLine($"my name is: {name}");
        }
        static void WriteY()
        {
            for(int  i = 0; i < 1000; i++)
            {
                Console.Write("y");
            }
        }
        static void Go()
        {
            lock (locker)
            {
                if (!done)
                {
                    Console.WriteLine("done");
                    done = true;
                }
            }
            //lock (lvlLocker)
            //{

            //}
        }
    }
}
