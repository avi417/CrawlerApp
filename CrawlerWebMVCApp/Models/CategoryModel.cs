using CrawlerData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CrawlerWebMVCApp.Models
{
    public class CategoryModel
    {
        public int CategoryID { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public int SiteID { get; set; }
        public bool Active { get; set; }

        public CategoryModel()
        {

        }

        public static List<SelectListItem> GetCategorySelectList(int websiteId = 0, int defaultSelected = 0, bool addDefault = true)
        {
            List<SelectListItem> selectCategoryList = new List<SelectListItem>();
            if (addDefault)
            {
                selectCategoryList.Add(new SelectListItem() { Text = "---Any---", Value = "0", Selected = true });
            }

            WebscrapEntities repository = new WebscrapEntities();
            var categoryList = repository.Categories.Where(item => item.active.HasValue && item.active.Value == true
                                    && item.idSite == websiteId);

            foreach (var category in categoryList)
            {
                if(defaultSelected == category.id)
                    selectCategoryList.Add(new SelectListItem() { Text = category.name, Value = category.id.ToString(), Selected = true });
                else
                    selectCategoryList.Add(new SelectListItem() { Text = category.name, Value = category.id.ToString()});
            }

            return selectCategoryList;
        }

    }
}