using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Breakfast
{
    public class Juice { }
    public class Toast { }
    public class Bacon { }
    public class Egg { }
    public class Coffee { }
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Coffee cup = PourCoffe();
            Console.WriteLine("Coffee is ready");
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            //Egg eggs = FryEggs(4);
            Task<Egg> eggTask = Task.Run(() => FryEggsAsync(4));
            Task<Bacon> baconTask = Task.Run(() => FryBaconAsync(9));
            Task<Toast> toastTask = Task.Run(() => ToastBreadWithButterAndJamAsync(3));
            
            List<Task> breakfastTasks = new List<Task>() { eggTask, baconTask, toastTask };

            Thread.Sleep(7000);

            while(breakfastTasks.Count > 0)
            {

                var finishedTask = await Task.WhenAny(breakfastTasks);

                if(finishedTask == eggTask)
                    Console.WriteLine("Eggs are ready");
                if(finishedTask == baconTask)
                    Console.WriteLine("Bacon is ready");
                if (finishedTask == toastTask)
                    Console.WriteLine("Toast is ready");

                breakfastTasks.Remove(finishedTask);

                Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            }

            //Egg egg = eggTask.Result;
            ////Bacon bacon = FryBacon(9);
            //Bacon bacon = baconTask.Result;
            //Console.WriteLine("Bacon is ready");
            //Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            ////Toast toast = ToastBread(3);
            //Toast toast = toastTask.Result;
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
        static async Task<Egg> FryEggsAsync(int howMany)
        {
            Console.WriteLine("Warning the egg pan...");
            await Task.Delay(2000);

            Console.WriteLine($"Cracking {howMany} eggs");
            Console.WriteLine("Cooking the eggs...");
            await Task.Delay(3000);
            Console.WriteLine("Put eggs on a plate");
            return new Egg();
        }
        static async Task<Bacon> FryBaconAsync(int slices)
        {
            Console.WriteLine($"Putting {slices} of bacon in the pan");
            Console.WriteLine("Cooking the first side of bacon...");
            await Task.Delay(3000);
            for (int slice = 0; slice < slices; slice++)
            {
                Console.WriteLine("Flipping a slice of bacon");
            }

            Console.WriteLine("Cooking the second side of bacon...");
            await Task.Delay(3000);
            Console.WriteLine("Put bacon on a plate");
            return new Bacon();
        }
        static async Task<Toast> ToastBreadWithButterAndJamAsync(int slices)
        {
            Toast toast = await ToastBreadAsync(slices);

            ApplyButter(toast);
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            ApplyJam(toast);
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

            return toast;

        }
        static async Task<Toast> ToastBreadAsync(int slices)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                Console.WriteLine("Putting a slice of bread in the toaster");
            }
            Console.WriteLine("Start toasting");
            await Task.Delay(3000);

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
