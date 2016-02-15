using System;
using System.ServiceProcess;
using System.Timers;
using NLog;

namespace Seo.Crawler.Service
{
    public partial class Service1 : ServiceBase
    {
        private Timer _timer;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer();
            _timer.Elapsed += Timer_Elapsed;
            _timer.Interval = 12 * 60 * 60 * 1000;
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
        }

        protected override void OnStop()
        {
            _timer.Stop();
            _timer = null;
        }
    }
}
