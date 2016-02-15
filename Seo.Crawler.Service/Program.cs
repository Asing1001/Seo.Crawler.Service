using System;
using System.ServiceProcess;
using NLog;

namespace Seo.Crawler.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();
        static void Main()
        {
            if (!Environment.UserInteractive)
            {
                // Startup as service.
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new Service1()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // Startup as application
                try
                {
                    var options = System.Configuration.ConfigurationManager.GetSection("CrawlerOptions") as CrawlerOptions;
                    logger.Info("Config is {0}", options);
                    var crawler = new Crawler(options);
                    crawler.Start();
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex);
                }

            }


        }
    }
}
