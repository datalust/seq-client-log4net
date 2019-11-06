using System;
using log4net;
using log4net.Config;

namespace SeqLog4NetExample
{
    class Program
    {
        static void Main()
        {
            XmlConfigurator.Configure();

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
