using System;
using NLog;

namespace SeqNLogExample
{
    class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Main()
        {
            const int k = 42;
            const int l = 100;

            logger.Info(new { A = 1 });
            logger.Trace("Sample trace message, k={0}, l={1}", k, l);
            logger.Debug("Sample debug message, k={0}, l={1}", k, l);
            logger.Info("Sample informational message, k={0}, l={1}", k, l);
            logger.Warn("Sample warning message, k={0}, l={1}", k, l);
            logger.Error("Sample error message, k={0}, l={1}", k, l);
            logger.Fatal("Sample fatal error message, k={0}, l={1}", k, l);
            logger.Log(LogLevel.Info, "Sample informational message, k={0}, l={1}", k, l);

            Console.ReadKey();
        }
    }
}
