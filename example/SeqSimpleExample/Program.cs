using System;
using Serilog;
using Serilog.Debugging;

namespace SeqSerilogExample
{
    class Program
    {
        public static void Main()
        {
            SelfLog.Out = Console.Out;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .WriteTo.Seq("http://my-seq", apiKey: "zwzMZ9huokQDBcMxvv3", bufferBaseFilename: "Logs\\sample", period: TimeSpan.FromMilliseconds(100))
                .CreateLogger();

            Log.Information("This event has no properties");
            Log.Information("Hello, {Name}!", Environment.UserName);

            Console.ReadKey(true);
        }
    }
}
