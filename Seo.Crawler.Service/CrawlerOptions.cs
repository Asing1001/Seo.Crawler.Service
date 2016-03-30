﻿using System;
using System.Configuration;

namespace Seo.Crawler.Service
{
    public class CrawlerOptions : ConfigurationSection
    {

        [ConfigurationProperty("FolderPath")]
        public string FolderPath
        {
            get { return (string)this["FolderPath"]; }
        }
       
        [ConfigurationProperty("UserAgent")]
        public string UserAgent
        {
            get { return (string)this["UserAgent"]; }
        }

        [ConfigurationProperty("MaxPageToVisit")]
        public int MaxPageToVisit
        {
            get { return (int)this["MaxPageToVisit"]; }
        }

        [ConfigurationProperty("StartUrl")]
        public Uri StartUrl
        {
            get { return (Uri)this["StartUrl"]; }
        }


        [ConfigurationProperty("RemoteHubUrl")]
        public Uri RemoteHubUrl
        {
            get { return (Uri)this["RemoteHubUrl"]; }
        }


        [ConfigurationProperty("MaxThread")]
        public int MaxThread
        {
            get { return (int)this["MaxThread"]; }
        }
        [ConfigurationProperty("Languages")]
        public string Languages
        {
            get { return (string)this["Languages"]; }
        }

        public override string ToString()
        {
            return string.Format(
                "FolderPath:{0}, UserAgent:{1}, MaxPageToVisit:{2}, StartUrl:{3}",
                FolderPath, UserAgent, MaxPageToVisit, StartUrl);
        }
    }
}