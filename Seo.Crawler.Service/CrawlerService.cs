using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using NLog;
using OpenQA.Selenium.Remote;
using Timer = System.Timers.Timer;

namespace Seo.Crawler.Service
{
    public partial class CrawlerSerivce : ServiceBase
    {
        private Timer _timer;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public CrawlerSerivce()
        {
            InitializeComponent();
            

        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer();
            _timer.AutoReset = false;
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
            
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var RepeatTime = ConfigurationManager.GetSection("TimeInterval") as TimeInterval;
            try
            {

                var cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var localSections = cfg.Sections.Cast<ConfigurationSection>()
                    .Where(s => s.SectionInformation.IsDeclared);
                foreach (var i in localSections)
                {

                    var options =
                        ConfigurationManager.GetSection(i.SectionInformation.SectionName) as
                            CrawlerOptions;
                    if (options.Run && i.SectionInformation.SectionName.Contains("CrawlerOptions"))
                    {
                        logger.Info(options.Name + " Config is {0}", options);
                        var crawler = new Crawler(options);
                        crawler.Start();
                    }
                }

                RepeatTime = ConfigurationManager.GetSection("TimeInterval") as TimeInterval;

                logger.Info("[Report TimeR]" + RepeatTime.time);
                _timer.Stop();
                _timer.Interval = RepeatTime.time; //Set your new interval here
                _timer.Start();
                
            }
            catch (Exception ex)
            {
                logger.Fatal(ex);
                logger.Info("[Report TimeR]" + RepeatTime.time);
                _timer.Stop();
                _timer.Interval = RepeatTime.time; //Set your new interval here
                _timer.Start();
            }
           

        
        }


        protected override void OnStop()
        {
            _timer.Stop();
            _timer = null;
        }
    }
}
