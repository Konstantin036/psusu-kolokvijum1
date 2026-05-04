using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Breakfast_sync
{
    public class Juice { }
    public class Toast { }
    public class Bacon { }
    public class Egg { }
    public class Coffee { }
    internal class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Coffee cup = PourCoffe();
            Console.WriteLine("Coffee is ready");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Egg eggs = FryEggs(4);
            Console.WriteLine("Eggs are ready");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Bacon bacon = FryBacon(9);
            Console.WriteLine("Bacon is ready");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Toast toast = ToastBread(3);
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            ApplyButter(toast);
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            ApplyJam(toast);
            Console.WriteLine("Toast is ready");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Juice juice = PourJuice();
            Console.WriteLine("Juice is ready");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

            Console.WriteLine("Breakfast is ready!");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }
        static Coffee PourCoffe()
        {
            Console.WriteLine("Pouring coffee...");
            return new Coffee();
        }
        static Egg FryEggs(int howMany)
        {
            Console.WriteLine("Warning the egg pan...");
            Thread.Sleep(2000);

            Console.WriteLine($"Cracking {howMany} eggs");
            Console.WriteLine("Cooking the eggs...");
            Thread.Sleep(3000);
            Console.WriteLine("Put eggs on a plate");
            return new Egg();
        }
        static Bacon FryBacon(int slices)
        {
            Console.WriteLine($"Putting {slices} of bacon in the pan");
            Console.WriteLine("Cooking the first side of bacon...");
            Thread.Sleep(3000);
            for (int slice = 0; slice < slices; slice++)
            {
                Console.WriteLine("Flipping a slice of bacon");
            }

            Console.WriteLine("Cooking the second side of bacon...");
            Thread.Sleep(3000);
            Console.WriteLine("Put bacon on a plate");
            return new Bacon();
        }
        static Toast ToastBread(int slices)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                Console.WriteLine("Putting a slice of bread in the toaster");
            }
            Console.WriteLine("Start toasting");
            Thread.Sleep(3000);

            Console.WriteLine("Remove toast from toaster");
            return new Toast();
        }

        static void ApplyButter(Toast toast)
        {
            Console.WriteLine("Putting butter on the toast");
        }
        static void ApplyJam(Toast toast)
        {
            Console.WriteLine("Putting jam on the toast");
        }
        static Juice PourJuice()
        {
            Console.WriteLine("Pouring orange juice");
            return new Juice();
        }
    }
}
