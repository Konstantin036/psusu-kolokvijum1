using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vezbe1_grupa1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //string s1 = "22";
            //int broj1 = 5;

            //s1 = broj1.ToString();
            //broj1 = int.Parse(s1);
            //short broj2 = (short)broj1;

            //Console.WriteLine("Hello!");

            //DateTime dateTime = DateTime.Now;

            ////string s3 = "Danas je" + dateTime + "i s1 = " + s1;

            //string s4 = $"Danas je: {dateTime} i s1 = {s1}";

            //int[] statickiNiz = { 1, 2, 3 };
            //List<int> dinamickiNiz = new List<int> { 4, 5, 6 };
            //LinkedList<int> lista = new LinkedList<int>();

            //try
            //{
            //    Console.WriteLine(dinamickiNiz[1]);
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //}
            //finally
            //{
            //    Console.WriteLine("Napustio sam niz");
            //}


            //for (int i = 0; i < dinamickiNiz.Count; i++)
            //{
            //    Console.WriteLine(dinamickiNiz[i]);
            //}
            //foreach (int x in dinamickiNiz)
            //{
            //    Console.WriteLine(x);
            //}
            //Console.WriteLine("~~~KRAJ PROGRAMA~~~");

            Person p1 = new Person();
            Employee e1 = new Employee();
            p1.Name = "Veljko Topalov";
            p1.Id = 1;
            e1.Name = "Stefan Radojicic";
            e1.Id = 2;
            e1.Salary = 100_000;

            Console.WriteLine(p1.GetFullName());
            Console.WriteLine(e1.GetFullName());

            Person p2 = new Employee();
            //Employee e2 = new Person();
            p2.Name = "Eren Jeger";
            p2.Id = 3;
            //p2.sal
            Console.WriteLine(p2.GetFullName());

            p2.ToString();

            List<Stamp> stamps = new List<Stamp>()
            {
                new Stamp(1, "Srbija"),
                new Stamp(2, "Austrija"),
                new Stamp(3, "Brazil")
            };
            StampCollector stampCollector = new StampCollector
            {
                Stamps = stamps
            };
            stampCollector.PrintAllStamps();
        }
    }
}
