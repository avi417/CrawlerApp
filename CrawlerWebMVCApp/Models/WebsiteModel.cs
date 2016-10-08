using CrawlerData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CrawlerWebMVCApp.Models
{
    public class WebsiteModel
    {
        public int WebsiteID { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string Color { get; set; }
        public bool Active { get; set; }

        public WebsiteModel()
        {

        }

        public static List<SelectListItem> GetWebsiteSelectList(int defaultSelected = 0, bool addDefault = true)
        {
            List<SelectListItem> selectWebsiteList = new List<SelectListItem>();
            if(addDefault)
            {             
                selectWebsiteList.Add(new SelectListItem() { Text = "---Any---", Value = "0" , Selected = true});
            }

            WebscrapEntities repository = new WebscrapEntities();
            var websiteList = repository.Sites.Where(item => item.active.HasValue && item.active.Value == true);
            foreach(var website in websiteList)
            {
                if(defaultSelected == website.id)
                    selectWebsiteList.Add(new SelectListItem() { Text = website.name, Value = website.id.ToString(), Selected = true});
                else
                    selectWebsiteList.Add(new SelectListItem() { Text = website.name, Value = website.id.ToString() });
            }

            return selectWebsiteList;
        }
    }
}