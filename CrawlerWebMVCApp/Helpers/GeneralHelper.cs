using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CrawlerWebMVCApp.Helpers
{
    public static class GeneralHelper
    {
        public static string ComputeProductLink(string link, string website)
        {
            if (link == null || link.Trim().Length <= 2)
                return "";

            if (link.Contains("http"))
                return link;

            return website + link;
        }
    }
}