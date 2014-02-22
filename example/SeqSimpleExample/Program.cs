using System;
using System.Threading;
using Seq;
using Serilog;
using Serilog.Debugging;

namespace SeqSimpleExample
{
    class Program
    {
        public static void Main()
        {
            SelfLog.Out = Console.Out;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .WriteTo.Seq("http://my-seq", "Logs\\sample")
                .CreateLogger();

            for (int i = 0; i < 2000; ++i)
            {
                var p = new string('P', i % 200);
    //            Log.Information("Hello, {Name} {P}!", Environment.UserName, p);
                Thread.Sleep(10);
            }
            Console.ReadKey(true);
        }
    }
}
