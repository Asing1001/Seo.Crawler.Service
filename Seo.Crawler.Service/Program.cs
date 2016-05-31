using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
                    var cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var localSections = cfg.Sections.Cast<ConfigurationSection>()
                        .Where(s => s.SectionInformation.IsDeclared);
                    foreach (var i in localSections)
                    {

                        var options =
                            System.Configuration.ConfigurationManager.GetSection(i.SectionInformation.SectionName) as
                                CrawlerOptions;
                        if (options.Run && i.SectionInformation.SectionName.Contains("CrawlerOptions"))
                        {
                            logger.Info(options.Name + " Config is {0}", options);
                            var crawler = new Crawler(options);
                            crawler.Start();
                        }
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
