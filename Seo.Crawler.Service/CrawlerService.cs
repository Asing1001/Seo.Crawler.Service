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
            _timer.Elapsed += Timer_Elapsed;
            _timer.Interval =10 * 1000;
            _timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
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
           

            logger.Debug("start debug");
            Thread oThreadA = new Thread(new ThreadStart(StartOpenPage));
            oThreadA.Name = "A Thread";
            Thread oThreadB = new Thread(new ThreadStart(StartOpenPage));
            oThreadB.Name = "B Thread";
            Thread oThreadC = new Thread(new ThreadStart(StartOpenPage));
            oThreadC.Name = "C Thread";
            Thread oThreadD = new Thread(new ThreadStart(StartOpenPage));
            oThreadD.Name = "D Thread";
            Thread oThreadE = new Thread(new ThreadStart(StartOpenPage));
            oThreadE.Name = "E Thread";

            //啟動執行緒物件
            oThreadA.Start();
            oThreadB.Start();
            oThreadC.Start();
            oThreadD.Start();
            oThreadE.Start();
        }

        private void StartOpenPage()
        {

            EventLog myLog = new EventLog();
            myLog.Source = "TESTS111";
            logger.Debug("start open");
            // Write an informational entry to the event log.    
            myLog.WriteEntry("Writing to event log.");
            var driver = new RemoteWebDriver(new Uri("http://localhost:4500/wd/hub"), DesiredCapabilities.Chrome()); // instead of this url you can put the url of your remote hub
                driver.Navigate().GoToUrl("http://www.google.com.tw");
                driver.Quit();
            
        }

        protected override void OnStop()
        {
            _timer.Stop();
            _timer = null;
        }
    }
}
