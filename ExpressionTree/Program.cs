using BenchmarkDotNet.Running;

namespace ExpressionTree
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Trials>();
        }
    }
}
