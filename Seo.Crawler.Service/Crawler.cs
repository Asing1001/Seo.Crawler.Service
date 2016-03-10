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

namespace Seo.Crawler.Service
{
    public class Crawler
    {
        //private RemoteWebDriver _driver;
        private CrawlerOptions _options;
        private Stopwatch _watch;
        private Logger logger;
        private static ConcurrentDictionary<Uri, Uri> pageParentURLMapping; // Key is current Page,Content is parent Page
        private int _maxThread = 4;
        private static ConcurrentDictionary<Uri, Uri> pagesToVisit;
        private List<RemoteWebDriver> WebdriverList;
        private Dictionary<Uri, Uri> PartThreading ;
        public Crawler(CrawlerOptions options)
        {
            _options = options;
            pageParentURLMapping = new ConcurrentDictionary<Uri, Uri>();
            pagesToVisit = new ConcurrentDictionary<Uri,Uri>();
            _watch = new Stopwatch();
            logger = LogManager.GetCurrentClassLogger();
            PartThreading = new Dictionary<Uri, Uri>();

            WebdriverList = new List<RemoteWebDriver>();
            for (int i = 0; i < _maxThread; i++)
            {
                ChromeOptions chromeOptions = new ChromeOptions();
                chromeOptions.AddArgument("user-data-dir=C:/Debug");
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

            /*while (true)
            {
                PartThreading = new Dictionary<Uri, Uri>();
                logger.Info(" Page Visit Size :{0}", pagesToVisit.Count);                    
                foreach (var pTV in pagesToVisit)
                {
                    PartThreading.Add(pTV.Key,pTV.Value);
                    Uri value;
                    pagesToVisit.TryRemove(pTV.Key, out value);
                }

                ChromeOptions options = new ChromeOptions();
                
                options.AddArgument("user-data-dir=C:/Debug");

                //Thread.Sleep(1000);
                logger.Info(" Starting Parallel : [{0}] , pageToVisit : [{1}]", PartThreading.Count, pagesToVisit.Count);
                Parallel.ForEach(PartThreading, new ParallelOptions { MaxDegreeOfParallelism = 1 }, (keyValue) =>
                {
                    logger.Info(" PageToVisit :[{0}] ,Page Finish Size : [{1}]", keyValue.Key, pageParentURLMapping.Count);


                    var _driver = new RemoteWebDriver(_options.RemoteHubUrl, options.ToCapabilities()); // instead of this url you can put the url of your remote hub

                    _driver.Navigate().GoToUrl(keyValue.Key);

                    SaveHtmlAndScreenShot(keyValue.Key, _driver);

                    GetUnvisitedLinks(_driver, keyValue.Key);
                    _driver.Quit();
                    Uri value;
                    _driver.Dispose();    
                    logger.Info("Concurrent List add " + pageParentURLMapping.TryAdd(keyValue.Key, keyValue.Value));
                });


                logger.Info("Finish Parallel  Finish Size: " + pageParentURLMapping.Count);
                if (pagesToVisit.Count == 0)
                {
                    Thread.Sleep(1000);
                    break;
                }
                
                
            }*/

            while (true && pagesToVisit.Count > 0)
            {

                PartThreading = new Dictionary<Uri, Uri>();
                logger.Info(" Page Visit Size :{0}", pagesToVisit.Count);
                foreach (var pTV in pagesToVisit)
                {
                    PartThreading.Add(pTV.Key, pTV.Value);
                    Uri value;
                    pagesToVisit.TryRemove(pTV.Key, out value);
                }


                List<Task> waitHandles =new List<Task>();
                foreach (var Key in PartThreading.Keys)
                {

                    Task task = Task.Factory.StartNew(() => 
                    {
                        logger.Info(" PageToVisit :[{0}] ,Page Finish Size : [{1}]", Key, pageParentURLMapping.Count);


                        WebdriverList[waitHandles.Count].Navigate().GoToUrl(Key);


                        SaveHtmlAndScreenShot(Key, WebdriverList[waitHandles.Count]);

                        GetUnvisitedLinks(WebdriverList[waitHandles.Count], Key);


                        logger.Info("Concurrent List add " + pageParentURLMapping.TryAdd(Key, PartThreading[Key]));
                    });
                    waitHandles.Add(task);
                    if(waitHandles.Count == _maxThread)
                    {
                        Task.WaitAll(waitHandles.ToArray());
                        waitHandles= new List<Task>();
                    }

                }
                if (waitHandles.Count != 0)
                {
                    Task.WaitAll(waitHandles.ToArray());
                }
            }

            logger.Info(" [Finish] PageToVisitSize :[{0}] ,Page Finish Size : [{1}]", pagesToVisit.Count, pageParentURLMapping.Count);
            Finish();
        }


        



        private void Finish()
        {
            
            //SaveSitemap();
            _watch.Stop();
            logger.Info("Finish all task in {0}", _watch.Elapsed);
        }

       

        private void GetUnvisitedLinks(RemoteWebDriver _driver ,Uri parentUri)
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
                if (link != null && link.Host.Contains(originHost) && !pageParentURLMapping.ContainsKey(link)
                    && !pagesToVisit.ContainsKey(link) && !PartThreading.ContainsKey(link) && !result.Contains(link))
                {
                    result.Add(link );
                }
            }

            foreach(var r in result)
            {
                pagesToVisit.TryAdd(r,parentUri);

            }

            logger.Info("Get {0} sameDomainUnvisitedLinks, list as below :{1}", result.Count
                //JsonConvert.SerializeObject(result.Select(uri => uri.AbsolutePath))
                );
            //return result;

        }

        private void ValidatePage(RemoteWebDriver _driver ,Uri currentUri)
        {

           /* DataRow drRow = pagesErrorList.NewRow();
            drRow["URL"] = currentUri.AbsolutePath;
            List<LogEntry> logEntry = _driver.Manage().Logs.GetLog(LogType.Browser).ToList();
            Boolean ValidateFailed = false;

            if (_driver.PageSource.Contains("Error 404") || _driver.PageSource.Contains("404") || _driver.PageSource.ToLower().Contains("not found"))
            {
                drRow["NotFound"] = " Page Not Found";
                ValidateFailed = true;
            }
            if (logEntry.Count > 0)
            {

                drRow["LogCount"] = logEntry.Count.ToString();
                drRow["Error"] += string.Join(" , ", logEntry.Where(log => !log.Message.Contains("$modal is now deprecated. Use $uibModal instead.")).Select(log => log.Message).ToList());
                ValidateFailed = true;
            }
            if (ValidateFailed)
            {
                drRow["SourceURL"] = pageParentURLMapping.ContainsKey(currentUri.AbsolutePath) ? pageParentURLMapping[currentUri.AbsolutePath] : "";
                pagesErrorList.Rows.Add(drRow);
            }
            if (pageParentURLMapping.Count > 0)
                pageParentURLMapping.Remove(currentUri.AbsoluteUri);*/
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
                    //siteMapGenerator.Generate(pagesVisited);
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
