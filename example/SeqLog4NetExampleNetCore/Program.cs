using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Reflection;

namespace SeqLog4NetExampleNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            var log = LogManager.GetLogger(typeof(Program));

            log.InfoFormat("Hello, {0}, from log4net!", Environment.UserName);

            try
            {
                throw new DivideByZeroException();
            }
            catch (Exception ex)
            {
                log.Error("Oops!", ex);
            }

            Console.ReadKey();
        }
    }
}
