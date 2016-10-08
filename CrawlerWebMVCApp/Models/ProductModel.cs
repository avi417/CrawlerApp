using CrawlerData;
using CrawlerWebMVCApp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CrawlerWebMVCApp.Models
{
    #region Domain Model

    public class ProductModel
    {
        public int ProductID { get; set; }

        public int CategoryID { get; set; }
        public string CategoryName { get; set; }

        public int SiteID { get; set; }
        public string SiteName { get; set; }

        public string Name { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public double Price { get; set; }
        public string PhotoLink { get; set; }
        public DateTime InsertDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool Active { get; set; }
        public string WebsiteId { get; set; }

        public ProductModel()
        {

        }

        public ProductModel(Product product)
        {
            ConvertFromProduct(product);
        }

        public void ConvertFromProduct(Product product)
        {
            if (product == null)
                return;

            this.ProductID = product.id;
            this.CategoryID = product.idCategory.GetValueOrDefault(0);
            if (this.CategoryID > 0)
            {
                this.CategoryName = product.Category.name;
                this.SiteID = product.Category.idSite.GetValueOrDefault(0);
                if (this.SiteID > 0)
                {
                    this.SiteName = product.Category.Site.name;
                    this.Link = GeneralHelper.ComputeProductLink(product.link, product.Category.Site.link);
                }
            }
            this.Name = product.name;
            this.Title = product.title;           
            this.Price = product.price.GetValueOrDefault(0);
            this.InsertDate = product.insertDate.GetValueOrDefault(DateTime.Now);
            this.UpdateDate = product.updateDate.GetValueOrDefault(DateTime.Now);
            this.Active = product.active.GetValueOrDefault(false);
            this.PhotoLink = GeneralHelper.ComputeProductLink(product.photoLink, product.Category.Site.link);
            this.WebsiteId = product.siteId;
        }

        public static double GetProductMinPrice()
        {
            WebscrapEntities repository = new WebscrapEntities();
            return repository.Products.Min(item => item.price.Value);
        }

        public static double GetProductMaxPrice()
        {
            WebscrapEntities repository = new WebscrapEntities();
            return repository.Products.Max(item => item.price.Value);
        }
        
        public static ProductChartItem GetProductChart(int productID)
        {
            ProductChartItem productChart = new ProductChartItem(99999999, 0);
            List<ChartItem> chartItems = new List<ChartItem>();
            WebscrapEntities repository = new WebscrapEntities();
            var productScans = repository.ProductScans.Where(item => item.idProduct == productID);                        
            foreach (var pScan in productScans)
            {
                if (pScan.date.HasValue && pScan.price.HasValue)
                {
                    if (productChart.minValue > pScan.price.Value)
                    {
                        productChart.minValue = pScan.price.Value;
                        productChart.minDate = pScan.date.Value.ToString("yyyy-MM-dd");
                    }

                    if (productChart.maxValue < pScan.price.Value)
                    {
                        productChart.maxValue = pScan.price.Value;
                        productChart.maxDate = pScan.date.Value.ToString("yyyy-MM-dd");
                    }

                    chartItems.Add(new ChartItem(pScan.date.Value.ToString("yyyy-MM-dd"), pScan.price.Value));
                }
            }

            productChart.chartItems = chartItems.ToArray();
            return productChart;
        }

        public static List<ProductModel> SearchProducts(SearchProduct search)
        {
            List<ProductModel> productList = new List<ProductModel>();
            WebscrapEntities repository = new WebscrapEntities();
            if (search == null)
                return productList;
            
            List<string> splitSearchName = new List<string>();
            if (!String.IsNullOrEmpty(search.SearchName))
                splitSearchName = search.SearchName.Trim().Split(' ').ToList();

            var products = repository.Products.Where(item => 
                (item.price.HasValue ? (item.price.Value >= search.MinPrice && item.price.Value <= search.MaxPrice) : false)
                && (search.WebsiteID > 0 ? (item.Category.idSite == search.WebsiteID) : true)
                && (search.CategoryID > 0 ? (item.idCategory == search.CategoryID) : true)
                && (splitSearchName.Count > 0 ? splitSearchName.All(name => item.name.Contains(name)) : true));
            int i = 0;
            foreach(var product in products)
            {
                productList.Add(new ProductModel(product));
                i++;
                if (i >= 1000)
                    break;
            }

            return productList;
        }       
    }

    public class ProductChartItem
    {
        public double minValue { get; set; }
        public double maxValue { get; set; }
        public string minDate { get; set; }
        public string maxDate { get; set; }
        public ChartItem[] chartItems { get; set; }

        public ProductChartItem()
        {            
        }

        public ProductChartItem(double min, double max)
        {
            this.minValue = min;
            this.maxValue = max;
        }
    }

    public class ChartItem
    {
        public string date { get; set; }
        public double value { get; set; }

        public ChartItem()
        {

        }

        public ChartItem(string date, double val)
        {
            this.date = date;
            this.value = val;
        }
    }

    #endregion

    #region View Models

    public class SearchProduct
    {
        [Display(Name = "Product Name")]
        public string SearchName { get; set; }

        [Display(Name = "Website")]
        public int WebsiteID { get; set; }

        [Display(Name = "Category")]
        public int CategoryID { get; set; }

        [Display(Name = "Minimum Price")]
        public double MinPrice { get; set; }

        [Display(Name = "Maximum Price")]
        public double MaxPrice { get; set; }

        public List<SearchProductResult> SearchResult { get; set; }

        public SearchProduct()
        {

        }

        public void LoadSearchData()
        {
            this.MinPrice = ProductModel.GetProductMinPrice();
            this.MaxPrice = ProductModel.GetProductMaxPrice();
        }
    }

    public class SearchProductResult
    {
        public int ProductID { get; set; }        
        public string CategoryName { get; set; }
        public string SiteName { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public double Price { get; set; }
        public string PhotoLink { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool Active { get; set; }
        public string WebsiteId { get; set; }

        public SearchProductResult()
        {

        }
    }

    #endregion
}