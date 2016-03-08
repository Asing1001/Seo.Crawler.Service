using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
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

        private static ConcurrentDictionary<Uri, Uri> pagesToVisit;

        public Crawler(CrawlerOptions options)
        {
            _options = options;
            pageParentURLMapping = new ConcurrentDictionary<Uri, Uri>();
            pagesToVisit = new ConcurrentDictionary<Uri,Uri>();
            _watch = new Stopwatch();
            logger = LogManager.GetCurrentClassLogger();
            
        }


        public void Start()
        {
            _watch.Start();
            Crawl(_options.StartUrl);
        }

        private void Crawl(Uri uri)
        {
            
            //First PAge
            if (pageParentURLMapping.TryAdd(uri,null))
            {

                var driver = new RemoteWebDriver(new Uri("http://localhost:4500/wd/hub"), DesiredCapabilities.Chrome()); // instead of this url you can put the url of your remote hub
                driver.Navigate().GoToUrl(uri);
                SaveHtmlAndScreenShot(uri, driver);
                GetUnvisitedLinks(driver ,uri);


                logger.Info("[{0}] Open page :{1}", pagesToVisit.Count, uri);
                Parallel.ForEach(pagesToVisit, (keyValue) =>
                {
                    
                    var _driver = new RemoteWebDriver(new Uri("http://localhost:4500/wd/hub"), DesiredCapabilities.Chrome()); // instead of this url you can put the url of your remote hub
                    _driver.Navigate().GoToUrl(keyValue.Key);
                    
                    SaveHtmlAndScreenShot(uri, _driver);

                    GetUnvisitedLinks(_driver, keyValue.Value);
                    _driver.Quit();
                    Uri value ;
                    if (pageParentURLMapping.TryRemove(keyValue.Key,out value))
                    {
                        pageParentURLMapping.TryAdd(keyValue.Key, keyValue.Value);
                    }

                });
            }
            
            

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
                    && !pagesToVisit.ContainsKey(link) && !result.Contains(link))
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
