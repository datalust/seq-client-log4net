using System;
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
                .WriteTo.Seq("http://my-seq", inputKey: "jDWzqqRlK0pURdWFyHt", bufferBaseFilename: "Logs\\sample")
                .CreateLogger();

            Log.Information("This event has no properties");
            Log.Information("Hello, {Name}!", Environment.UserName);

            Console.ReadKey(true);
        }
    }
}
