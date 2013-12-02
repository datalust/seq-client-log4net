using System;
using Seq;
using Serilog;

namespace SeqSimpleExample
{
    class Program
    {
        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            Log.Information("Hello, {Name}!", Environment.UserName);
            Console.ReadKey(true);
        }
    }
}
