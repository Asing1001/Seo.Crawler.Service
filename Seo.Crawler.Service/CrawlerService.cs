using System;
using System.Diagnostics;
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
            try
            {

                var options = System.Configuration.ConfigurationManager.GetSection("WebCrawlerOptions") as CrawlerOptions;
                logger.Info(options.Name + " Config is {0}", options);
                var crawler = new Crawler(options);
                crawler.Start();


                options = System.Configuration.ConfigurationManager.GetSection("MobileCrawlerOptions") as CrawlerOptions;
                logger.Info(options.Name + " Config is {0}", options);
                crawler = new Crawler(options);
                crawler.Start();
                _timer.Stop();
                _timer.Interval = 60*60*1000*6; //Set your new interval here
                _timer.Start();
                
            }
            catch (Exception ex)
            {
                logger.Fatal(ex);
            }
           

        
        }


        protected override void OnStop()
        {
            _timer.Stop();
            _timer = null;
        }
    }
}
