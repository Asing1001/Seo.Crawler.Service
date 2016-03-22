using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NLog;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using System.Threading.Tasks;
using System.Data;
using OpenQA.Selenium;
using Seo.Crawler.Selenium;

namespace Seo.Crawler.Service
{
    public class Crawler
    {
        //private RemoteWebDriver _driver;
        private CrawlerOptions _options;
        private Stopwatch _watch;
        private Logger logger;
        private ConcurrentDictionary<Uri, Uri> pageVisitedURLMapping; // Key is current Page,Content is parent Page
        
        private ConcurrentDictionary<Uri, Uri> pagesToVisit;
        private List<RemoteWebDriver> WebdriverList;
        private ConcurrentDictionary<Uri, Uri> PartThreading;
        private ConcurrentDictionary<Uri, PageInfoToExcel> pageNotFoundMapping;
        private static int CurrentTask = 0;
        public Crawler(CrawlerOptions options)
        {
            _options = options;
            pageVisitedURLMapping = new ConcurrentDictionary<Uri, Uri>();
            pagesToVisit = new ConcurrentDictionary<Uri,Uri>();
            _watch = new Stopwatch();
            logger = LogManager.GetCurrentClassLogger();
            pageNotFoundMapping = new ConcurrentDictionary<Uri, PageInfoToExcel>();
            PartThreading = new ConcurrentDictionary<Uri, Uri>();

            WebdriverList = new List<RemoteWebDriver>();
            for (int i = 0; i < _options.MaxThread; i++)
            {
                ChromeOptions chromeOptions = new ChromeOptions();
                chromeOptions.AddArgument("user-data-dir=C:/Debug/" + i);
                var _driver = new RemoteWebDriver(_options.RemoteHubUrl, chromeOptions.ToCapabilities());
                
                WebdriverList.Add(_driver);
            }
        }


        public void Start()
        {
            _watch.Start();
            Crawl(_options.StartUrl);
        }

        private void Crawl(Uri uri)
        {
            
            pagesToVisit.TryAdd(uri, null);//First Page

            while (true && pagesToVisit.Count > 0)
            {

                PartThreading = new ConcurrentDictionary<Uri, Uri>();
                logger.Info(" Page Visit Size :{0}", pagesToVisit.Count);
                foreach (var pTV in pagesToVisit)
                {

                    PartThreading.TryAdd(pTV.Key, pTV.Value);
                    Uri value;
                    pagesToVisit.TryRemove(pTV.Key, out value);
                }

                List<Task> waitHandles = new List<Task>();


                foreach (var Key in PartThreading.Keys)
                {

                    
                    logger.Info(" PageToVisit :[{0}] ,Page Finish Size : [{1}] , CurrentTask : [{2}]", Key, pageVisitedURLMapping.Count, CurrentTask);
                    WebdriverList[CurrentTask - 1].Navigate().GoToUrl(Key);
                    SaveHtmlAndScreenShot(Key, WebdriverList[CurrentTask - 1]);
                    GetUnvisitedLinks(WebdriverList[CurrentTask - 1], Key, WebdriverList[CurrentTask - 1].Url);
                    logger.Info("Concurrent List add " + pageVisitedURLMapping.TryAdd(Key, PartThreading[Key]));
                }
                logger.Debug("Next Round Page to Visit :" + pagesToVisit.Count);
            }

            /*while (true && pagesToVisit.Count > 0)
            {

                PartThreading = new ConcurrentDictionary<Uri, Uri>();
                logger.Info(" Page Visit Size :{0}", pagesToVisit.Count);
                foreach (var pTV in pagesToVisit)
                {

                    PartThreading.TryAdd(pTV.Key, pTV.Value);
                    Uri value;
                    pagesToVisit.TryRemove(pTV.Key, out value);
                }

                List<Task> waitHandles = new List<Task>();


                foreach (var Key in PartThreading.Keys)
                {

                    Task task = new Task(() => 
                    {
                        logger.Info(" PageToVisit :[{0}] ,Page Finish Size : [{1}] , CurrentTask : [{2}]", Key, pageVisitedURLMapping.Count ,CurrentTask);

                        lock (WebdriverList[CurrentTask - 1])
                        {
                            WebdriverList[CurrentTask - 1].Navigate().GoToUrl(Key);
                            SaveHtmlAndScreenShot(Key, WebdriverList[CurrentTask - 1]);

                            GetUnvisitedLinks(WebdriverList[CurrentTask - 1], Key, WebdriverList[CurrentTask - 1].Url);     
                        }
                       
                        logger.Info("Concurrent List add " + pageVisitedURLMapping.TryAdd(Key, PartThreading[Key]));                        
                    });
                    GetNextNumber();
                    task.Start();
                    logger.Info("CurrentTask : " +  CurrentTask);
                    waitHandles.Add(task);
                    if (waitHandles.Count == _options.MaxThread)
                    {
                        Task.WaitAll(waitHandles.ToArray());
                        waitHandles = new List<Task>();
                        CurrentTask = 0;
                    }

                }
                if (waitHandles.Count != 0)
                {
                    
                    Task.WaitAll(waitHandles.ToArray());
                    waitHandles = new List<Task>();
                    CurrentTask = 0;
                }
                logger.Debug("Next Round Page to Visit :" + pagesToVisit.Count);
            }*/

            logger.Info(" [Finish] PageToVisitSize :[{0}] ,Page Finish Size : [{1}]", pagesToVisit.Count, pageVisitedURLMapping.Count);
            Finish();

        }


        public static int GetNextNumber()
        {
            return Interlocked.Increment(ref CurrentTask);
        }


        private void Finish()
        {
            
            //SaveSitemap();
            _watch.Stop();
            foreach (var Wdl in WebdriverList)
            {
                Wdl.Quit();
                Wdl.Dispose();
            }
            logger.Info("Finish all task in {0}", _watch.Elapsed);
        }

       

        private void GetUnvisitedLinks(RemoteWebDriver _driver ,Uri parentUri,string DriverUri)
        {
            
            var result = new List<Uri>();
            var originHost = _options.StartUrl.Host;
            var links = _driver.FindElementsByCssSelector("a[href]")
                .Select(a =>
                {
                    try
                    {
                        return new Uri(a.GetAttribute("href"));
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
                }
                );

            foreach (var link in links)
            {
                if (link != null && link.Host.Contains(originHost) && !pageVisitedURLMapping.ContainsKey(link)
                    && !pagesToVisit.ContainsKey(link) && !PartThreading.ContainsKey(link) && !result.Contains(link))
                {
                    result.Add(link );
                }
            }

            foreach(var r in result)
            {
                pagesToVisit.TryAdd(r,parentUri);

            }

            logger.Info("Get [{0}] sameDomainUnvisitedLinks, Size :[{1}] should be the same , Current URL : [{2}]", result.Count, pagesToVisit.Count, parentUri
                
                );
            //return result;

        }

        private void ValidatePage(RemoteWebDriver _driver ,Uri currentUri ,Uri parentUri)
        {
            PageInfoToExcel pageInfo = new PageInfoToExcel();

            List<LogEntry> logEntry = _driver.Manage().Logs.GetLog(LogType.Browser).ToList();
            Boolean ValidateFailed = false;

            if (_driver.PageSource.Contains("Error 404") || _driver.PageSource.Contains("404") || _driver.PageSource.ToLower().Contains("not found"))
            {
                pageInfo.NotFound= " Page Not Found";
                ValidateFailed = true;
            }
            if (logEntry.Count > 0)
            {

                pageInfo.LogCount = logEntry.Count.ToString();
                pageInfo.Error += string.Join(" , ", logEntry.Where(log => !log.Message.Contains("$modal is now deprecated. Use $uibModal instead.")).Select(log => log.Message).ToList());
                ValidateFailed = true;
            }
            if (ValidateFailed)
            {
                pageInfo.SourceURL = parentUri.ToString();
                pageNotFoundMapping.TryAdd(currentUri, pageInfo);
            }

        }

        private void SaveHtmlAndScreenShot(Uri uri, RemoteWebDriver _driver)
        {
            try
            {
                var removeScriptTag =
                    "Array.prototype.slice.call(document.getElementsByTagName('script')).forEach(function(item) { item.parentNode.removeChild(item);});";
                var addClassToBody = "document.getElementsByTagName('body')[0].className += ' seoPrerender';";

                _driver.ExecuteScript(removeScriptTag + addClassToBody);   
                //uri.AbsolutePath is relative url
                var result = _driver.PageSource;
                string filenameWithPath = _options.FolderPath + uri.AbsolutePath + MakeValidFileName(uri.Query);
                Directory.CreateDirectory(Path.GetDirectoryName(filenameWithPath));
                File.WriteAllText(filenameWithPath + ".html", result);
                logger.Info("SaveHtmlAndScreenShot to {0}.html", filenameWithPath);
                _driver.GetScreenshot().SaveAsFile(filenameWithPath + ".jpg", ImageFormat.Jpeg);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        private void SaveSitemap()
        {
            try
            {
                string sitemapPath = string.Format("{0}/{1}sitemap.xml", _options.FolderPath, DateTime.Now.ToString("dd-MM-yyyy"));
                using (var fileStream = new FileStream(sitemapPath, FileMode.Create))
                {
                    var siteMapGenerator = new SiteMapGenerator(fileStream, Encoding.UTF8);
                    siteMapGenerator.Generate(pageVisitedURLMapping.Keys.ToList());
                    siteMapGenerator.Close();
                }
                logger.Info("SiteMap save to {0}", sitemapPath);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}
