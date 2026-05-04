using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace v2_grupa3
{
    public delegate void FirstDelegate(int x);
    internal class Sample
    {
        public delegate string SecondDelegate(char a, char b);

        public FirstDelegate d1 { get; set; }
        public FirstDelegate d2 { get; set; }
        public FirstDelegate d3 { get; set; }
        public FirstDelegate d4 { get; set; }
        public FirstDelegate d5 { get; set; }
        public FirstDelegate d6 { get; set; }
        public void InstanceMethod(int a)
        {
            Console.WriteLine($"x is: {a} from Sample Class Instance MEthod");
        }
        public static void StaticMethod(int a)
        {
            Console.WriteLine($"x is: {a} from Sample Class Static MEthod");
        }
        public void DefineDelegates()
        {
            d1 = new FirstDelegate(InstanceMethod);
            d2 = new FirstDelegate(this.InstanceMethod);
            Sample s = new Sample();
            d3 = new FirstDelegate(s.InstanceMethod);
            d4 = new FirstDelegate(StaticMethod);
            OtherSample os = new OtherSample();
            d5 = new FirstDelegate(os.InstanceMethod);
            d6 = new FirstDelegate(OtherSample.StaticMethod);
        }
    }
    public class OtherSample
    {
        public void InstanceMethod(int a)
        {
            Console.WriteLine($"x is: {a} from Other Sample Class Instance MEthod");
        }
        public static void StaticMethod(int a)
        {
            Console.WriteLine($"x is: {a} from Other Sample Class Static MEthod");
        }
    }
}
