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
        private static CrawlerOptions _options;
        private static Stopwatch _watch;
        private static Logger logger;
        private static ConcurrentDictionary<Uri, Uri> pageVisitedURLMapping; // Key is current Page,Content is parent Page
        private static ConcurrentDictionary<Uri, PageInfoToExcel> pageNotFoundMapping = new ConcurrentDictionary<Uri, PageInfoToExcel>();
        public Crawler(CrawlerOptions options)
        {
            _options = options;
            pageVisitedURLMapping = new ConcurrentDictionary<Uri, Uri>();
            _watch = new Stopwatch();
            logger = LogManager.GetCurrentClassLogger();
            pageNotFoundMapping = new ConcurrentDictionary<Uri, PageInfoToExcel>();
        
        }


        public void Start()
        {
            _watch.Start();
            CrawlByLanguage(_options.Languages, _options.StartUrl);
            Finish();
        }

        private void CrawlByLanguage(string Languages,Uri uri )
        {
            List<string> LanguageMapping = Languages.Split(';').ToList();
            List<Task> TaskList = new List<Task>();

            CrawlFirstPage(uri);
            foreach (var Lan in LanguageMapping)
            {
                
                if (Lan != "")
                {
                    var NewLanguage = new Uri(uri.AbsoluteUri + Lan);
                    TaskList.Add(Task.Run(() => CrawlPage(NewLanguage)));
                }

            }
            Task.WaitAll(TaskList.ToArray());
           
        }


        private static void CrawlFirstPage(Uri startUrl)
        {
            ChromeOptions chromeOptions = new ChromeOptions();
            var _driver = new RemoteWebDriver(_options.RemoteHubUrl, chromeOptions.ToCapabilities());
            try
            {
                _driver.Navigate().GoToUrl(startUrl);
                SaveHtmlAndScreenShot(startUrl, _driver);
                pageVisitedURLMapping.TryAdd(startUrl, startUrl);
                _driver.Close();
                _driver.Quit();
            }
            catch (Exception ex)
            {
                logger.Info(" Thread : " + startUrl.PathAndQuery + " Error at " + ex.StackTrace);
                _driver.Close();
                _driver.Quit();
            }
        }

        public static void CrawlPage(Uri startUrl)
        {
            ChromeOptions chromeOptions = new ChromeOptions();
            var _driver = new RemoteWebDriver(_options.RemoteHubUrl, chromeOptions.ToCapabilities());
            
            try
            {
                ConcurrentDictionary<Uri, Uri> pageToVisit = new ConcurrentDictionary<Uri, Uri>();
                ConcurrentDictionary<Uri, Uri> PartThreading;
                pageToVisit.TryAdd(startUrl, null);
                
                while (true && pageToVisit.Count > 0)
                {
                    PartThreading = new ConcurrentDictionary<Uri, Uri>();
                    logger.Info(_options.Name + " Thread : " + startUrl.PathAndQuery + " Page To Visit Size :{0}", pageToVisit.Count + " SessionId" + _driver.SessionId );
                    foreach (var pTV in pageToVisit)
                    {

                        PartThreading.TryAdd(pTV.Key, pTV.Value);
                        Uri value;
                        pageToVisit.TryRemove(pTV.Key, out value);
                    }
                    foreach (var Key in PartThreading.Keys)
                    {
                        _driver.Navigate().GoToUrl(Key);
                        SaveHtmlAndScreenShot(Key, _driver);
                        pageToVisit = GetUnvisitedLinks(_driver, Key, _driver.Url, PartThreading, pageToVisit, startUrl);
                        pageVisitedURLMapping.TryAdd(Key, PartThreading[Key]);
                        
                        ValidatePage(_driver, Key, PartThreading[Key]);

                    }

                    logger.Info(" Thread : " + startUrl.PathAndQuery + " Page Finish Visit Size :{0}",   pageVisitedURLMapping.Count);

                }
                _driver.Close();
                _driver.Quit();
            }
            catch (Exception ex)
            {
                logger.Info(" Thread : " + startUrl.PathAndQuery + " Error at " + ex.StackTrace);
                _driver.Close();
                _driver.Quit();
            }


        }






        private static void Finish()
        {
            CompareLinks();
            ExcelHandler.DataTableToExcel(_options.FolderPath + "\\" + _options.Name+"AllLinksTable" + DateTime.Now.ToString("yyyymmddHHMMss") + ".xlsx", ExcelHandler.LinkToDatatTable(pageVisitedURLMapping), "Sheet1");
            ExcelHandler.DataTableToExcel(_options.FolderPath + "\\PageNonValidateList.xlsx", ExcelHandler.ConvertClassToTable(pageNotFoundMapping));
            SaveSitemap();
            _watch.Stop();
            logger.Info(_options.Name +  " Finish all task in {0}", _watch.Elapsed);
        }



        private static ConcurrentDictionary<Uri, Uri> GetUnvisitedLinks(RemoteWebDriver _driver, Uri parentUri, string DriverUri, ConcurrentDictionary<Uri, Uri> PartThreading, ConcurrentDictionary<Uri, Uri> pageToVist,Uri startUri)
        {
            
            var result = new List<Uri>();
            var originHost = _options.StartUrl.Host;
            var links = _driver.FindElementsByTagName("a")
                .Select(a =>
                {
                    try
                    {
                        if (a.GetAttribute("href") != null)
                            return new Uri(a.GetAttribute("href"));
                        else
                            return new Uri(a.GetAttribute("ng-href"));
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
                }
                );

            foreach (var link in links)
            {
                if (link != null && link.AbsoluteUri.Contains(startUri.AbsoluteUri) && !pageVisitedURLMapping.ContainsKey(link)
                    && !pageToVist.ContainsKey(link) && !PartThreading.ContainsKey(link) && !result.Contains(link))
                {
                    result.Add(link );
                }
            }

            foreach(var r in result)
            {
                pageToVist.TryAdd(r, parentUri);

            }

            logger.Info(" Thead : [{3}]   Get [{0}] sameDomainUnvisitedLinks, Size :[{1}] should be the same , Current URL : [{2}]", result.Count, pageToVist.Count, parentUri ,startUri.PathAndQuery
                
                );
            return pageToVist;

        }

        private static void ValidatePage(RemoteWebDriver _driver ,Uri currentUri ,Uri parentUri)
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

        private static void SaveHtmlAndScreenShot(Uri uri, RemoteWebDriver _driver)
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

        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        private static void SaveSitemap()
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

        private static void CompareLinks()
        {
            string fileName = ExcelHandler.GetLastFileName(_options.FolderPath, _options.Name + "AllLinksTable*.xlsx");
            if (!string.IsNullOrEmpty(fileName))
            {
                Dictionary<string, Object> lastParseLinks = ExcelHandler.LoadFileToDictionary(_options.FolderPath + "\\" + fileName);
                if (lastParseLinks != null)
                {
                    Dictionary<string, string> differentLinksMap = new Dictionary<string, string>();
                    foreach (var lastTimeVistLink in lastParseLinks.Keys)
                    {
                        if (!string.IsNullOrEmpty(lastTimeVistLink) && !pageVisitedURLMapping.ContainsKey(new Uri(lastTimeVistLink)))
                        {
                            differentLinksMap.Add(lastTimeVistLink, "Last Time has link");
                        }

                    }
                    foreach (var thisTimeVistLink in pageVisitedURLMapping.Keys)
                    {
                        if (!lastParseLinks.ContainsKey(thisTimeVistLink.ToString()))
                        {
                            differentLinksMap.Add(thisTimeVistLink.ToString(), "This Time has link");
                        }

                    }

                    ExcelHandler.DataTableToExcel(_options.FolderPath + "\\" + _options.Name + "Different" + DateTime.Now.ToString("yyyymmddHHMMss") + ".xlsx", ExcelHandler.DictToDatatTable(differentLinksMap), "Sheet1");

                }
            }
            

            
        }

        
    }
}
