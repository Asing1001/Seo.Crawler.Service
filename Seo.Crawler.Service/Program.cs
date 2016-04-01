using System;
using System.Collections.Generic;
using System.ServiceProcess;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
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
                    new CrawlerSerivce()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // Startup as application
                try
                {

                    var options = System.Configuration.ConfigurationManager.GetSection("WebCrawlerOptions") as CrawlerOptions;
                    if (options.Run)
                    {
                        logger.Info(options.Name + " Config is {0}", options);
                        var crawler = new Crawler(options);
                        crawler.Start();
                    }
                    options =System.Configuration.ConfigurationManager.GetSection("MobileCrawlerOptions") as CrawlerOptions;
                    if (options.Run)
                    {
                        
                        logger.Info(options.Name + " Config is {0}", options);
                        var crawler = new Crawler(options);
                        crawler.Start();
                    }
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex);
                }

            }


        }
    }
}
