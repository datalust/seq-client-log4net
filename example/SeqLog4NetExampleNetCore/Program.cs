using log4net;
using log4net.Config;
using System;
using System.IO;
using System.Reflection;

namespace SeqLog4NetExampleNetCore
{
    class Program
    {
        static void Main()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            var log = LogManager.GetLogger(typeof(Program));

            log.InfoFormat("Hello, {0}, from {1}!", Environment.UserName, "log4net");

            try
            {
                throw new DivideByZeroException();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
            }
            finally
            {
                // log4net may hang on shutdown without this call.
                LogManager.Shutdown();
            }
        }
    }
}
