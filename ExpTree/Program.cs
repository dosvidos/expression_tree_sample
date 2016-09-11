using BenchmarkDotNet.Running;

namespace ExpTree
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Trials>();
        }
    }
}
