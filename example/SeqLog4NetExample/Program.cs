using System;
using log4net;
using log4net.Config;

namespace SeqLog4NetExample
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

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
