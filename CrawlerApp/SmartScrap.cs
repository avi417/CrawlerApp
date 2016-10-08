using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrawlerApp.Helpers;
using System.Threading;
using System.Drawing;
using Newtonsoft.Json;
using CrawlerData;
using Newtonsoft.Json.Linq;

namespace CrawlerApp
{    
    public class SmartScrap
    {
        #region Constants

        private const string ATTRIBUTE_NOT_FOUND = "AttrNotFound";

        private const int SCAN_RUNNING = 1;
        private const int SCAN_PAUSED = 2;
        private const int SCAN_ERROR = 3;
        private const int SCAN_FINISHED = 4;

        #endregion

        #region Variables
        //de definit ceva parametrii... poate caracte... sau alti parametrii smart
        string _pageParam;
        public string PageParam
        {
            get { return _pageParam; }
            set { _pageParam = value; }
        }

        int _minSleepTime;
        public int MinSleepTime
        {
            get { return _minSleepTime; }
            set { _minSleepTime = value; }
        }

        int _maxSleepTime;
        public int MaxSleepTime
        {
            get { return _maxSleepTime; }
            set { _maxSleepTime = value; }
        }

        public delegate void CallbackEventHandler(string textToLog, Color? color = null);
        public event CallbackEventHandler LogCallback;
        #endregion

        #region constructor

        public SmartScrap(string pageParam, int minSleepTime = 5000, int maxSleepTime = 100000)
        {
            this._pageParam = pageParam;
            this._minSleepTime = minSleepTime;
            this._maxSleepTime = maxSleepTime;
        }

        #endregion
       
        public string ProcessTask()
        {            
            WebscrapEntities repository = new WebscrapEntities();
            //get all active websites
            var websites = repository.Sites.Where(item => item.active.HasValue && item.active == true).ToList();          
            Random randomGenerator = new Random();
            Category category = null;
            int step = 0;
            string error = null;
            foreach (var site in websites)
            {                
                ColorConverter converter = new ColorConverter();
                Color websiteColor = (Color)converter.ConvertFromString(site.color != null ? site.color : "#FFFFFF");
                LogCallback("Starting scan for website " + site.name + "(" + site.link + ")", websiteColor);

                //get all categories of website
                var categories = site.Categories.Where(item => item.active.HasValue && item.active == true).ToList();
                var products = categories.SelectMany(item => item.Products);


                //get last paused or error scan for that site!
                ScanHistory scanHistory = repository.ScanHistories.FirstOrDefault(item => item.Category.idSite == site.id && (item.status == SCAN_PAUSED || item.status == SCAN_ERROR));
                int categoryIndex = 0;
                int stepIndex = 1;
                if(scanHistory == null)
                    scanHistory = ScanHistoryInsert(ref repository, categories.ToList(), "0");
                else
                {
                    LogCallback("Detected unfinished scan! Status:" + scanHistory.status + " text:" + scanHistory.statusText + " At category:" + scanHistory.Category.name + "(" + scanHistory.idCategory + ") step:" + scanHistory.step, Color.Orange);
                    Category historyCategory = categories.FirstOrDefault(item => item.id == (scanHistory.idCategory ?? 0));
                    if (historyCategory != null)
                    {
                        categoryIndex = categories.IndexOf(historyCategory);
                        stepIndex = scanHistory.step.Convert<int>();
                    }
                    LogCallback("Scanning will resume from category:" + categories[categoryIndex].name + "(" + categories[categoryIndex].id + ")  at step:" + stepIndex.ToString(), Color.Orange);
                }
                //foreach category scan pages until error found or no product found            
                for (int j = categoryIndex; j < categories.Count(); j++)
                {
                    category = categories[j];
                    LogCallback("Scanning category " + category.name + "(" + category.link + ")", websiteColor);
                    step = stepIndex;
                    //set the stepindex back to 1...we need the info only for if we found a scanhistory
                    stepIndex = 1;
                    string webPage = category.link;                    
                    bool continueScan = true;
                    List<Product> productList = new List<Product>();
                    do
                    {                        
                        LogCallback("\tProcess at page " + step.ToString(), websiteColor);                        
                        int scannedObjects = 0;
                        string link = webPage.Replace(_pageParam, step.ToString());
                        productList = CompareProductLists(SmartScrapPage(site.id, link, ref error), productList);
                        if(error != null)
                        {
                            ScanHistoryUpdate(ref scanHistory, category.id, step.ToString(), SCAN_ERROR, error);
                            repository.SaveChanges();
                            LogCallback("Error Encountered in website: " + site.name + " at category:" + category.name + "(" +  category.id.ToString() + ") on page" + step.ToString() + "! ERR:" + error, Color.Red);
                            return error;
                        }
                        //some sleep before stepping forward
                        int sleepTime = randomGenerator.Next(_minSleepTime, _maxSleepTime);
                        Thread.Sleep(sleepTime);

                        if (productList != null && productList.Count > 0)
                        {
                            foreach (Product product in productList)
                            {
                                Product existingProduct = products.Where(item => item.link == product.link).FirstOrDefault();
                                DateTime now = DateTime.Now;
                                ProductScan pScan = new ProductScan()
                                    {
                                        date = now
                                    };
                                if (existingProduct == null)
                                {
                                    //save product if not found in database!
                                    product.insertDate = now;
                                    product.updateDate = now;
                                    product.idCategory = category.id;
                                    product.active = true;
                                    pScan.price = product.price;
                                    product.ProductScans.Add(pScan);
                                    repository.Products.Add(product);
                                    scannedObjects++;
                                    //category.Products.Add(product);
                                }
                                else
                                {
                                    if (existingProduct.active.GetValueOrDefault(false) == true)
                                    {
                                        //save scan to database
                                        existingProduct.updateDate = now;
                                        existingProduct.price = product.price;
                                        if (product.photoLink != null)
                                            existingProduct.photoLink = product.photoLink;
                                        if (product.siteId != null)
                                            existingProduct.siteId = product.siteId;
                                        pScan.price = product.price;
                                        pScan.idProduct = existingProduct.id;
                                        repository.ProductScans.Add(pScan);
                                        scannedObjects++;
                                        //existingProduct.ProductScans.Add(pScan);
                                    }
                                }
                            }
                        }
                        else
                            continueScan = false;

                        LogCallback("\tPage " + step.ToString() + " finished with " + scannedObjects + " objects scanned.", websiteColor);

                        if (CheckStopSignal())
                        {
                            ScanHistoryUpdate(ref scanHistory, category.id, (step + 1).ToString(), SCAN_PAUSED, "Paused");
                            repository.SaveChanges();
                            LogCallback("Scan with id: " + scanHistory.id.ToString() + " has paused at category:" + category.name + "(" + category.id.ToString() + ") on page" + (step + 1).ToString() + "" + error, Color.Orange);
                            return null;
                        }

                        if (continueScan)
                        {
                            ScanHistoryUpdate(ref scanHistory, category.id, (step + 1).ToString(), SCAN_RUNNING, "Running");
                            repository.SaveChanges();                            
                        }
                        else
                        {                            
                            LogCallback("Category " + category.name + " finished with " + step.ToString() + " pages scanned.", websiteColor);
                        }                                                
                        step++;
                        
                    } while (continueScan);
                }

                ScanHistoryUpdate(ref scanHistory, category.id, step.ToString(), SCAN_FINISHED, "Finished!");
                repository.SaveChanges();     
            }               
            return null;
        }

        #region Managing scanning history
        
        private ScanHistory ScanHistoryInsert(ref WebscrapEntities repository, List<Category> categories, string step, int status = SCAN_RUNNING, string statusText = null)
        {
            //create command
            List<int> commandList = new List<int>();
            foreach(Category category in categories)
            {
                commandList.Add(category.id);
            }

            string command = JsonConvert.SerializeObject(commandList, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            int categoryId = commandList.FirstOrDefault();
            ScanHistory history = repository.ScanHistories.Create();            
            history.command = command;
            history.idCategory = categoryId;
            history.step = step;
            history.status = status;
            history.statusText = statusText;
            history.insertDate = DateTime.Now;
            history.updateDate = DateTime.Now;
            
            if (repository == null)
                repository = new WebscrapEntities();

            repository.ScanHistories.Add(history);
            repository.SaveChanges();
            return history;
        }

        private void ScanHistoryUpdate(ref ScanHistory scanHistory, int categoryId, string step, int status, string errorText = null)
        {            
            scanHistory.idCategory = categoryId;
            scanHistory.step = step;
            scanHistory.updateDate = DateTime.Now;
            scanHistory.status = status;
            if (errorText != null)
                scanHistory.statusText = errorText;
        }

        #endregion
       
        #region Helper methods

        private bool CheckStopSignal()
        {
            return MainForm.GetStopSignal();
        }

        private bool DetectCaptcha(HtmlDocument document)
        {
            return document.DocumentNode.Descendants().Any(item => item.Id == "captchaForm") &&
                document.DocumentNode.Descendants().Any(item => item.Name == "div" && item.GetAttributeValue("class", "") == "g-recaptcha");
        }
        
        private List<Product> CompareProductLists(List<Product> currentList, List<Product> previousList)
        {            
            if (currentList == null || previousList == null || currentList.Count != previousList.Count)
                return currentList;

            for (int i = 0; i < currentList.Count; i++)
            {
                if (currentList[i].link != previousList[i].link)
                    return currentList;
            }

            return null;
        }
        
        #endregion

        #region Page scanning logic

        private List<Product> SmartScrapPage(int websiteId, string webPage, ref string error)
        {
            switch(websiteId)
            {
                case 2:
                    {
                        return SmartScrapCELPage(webPage, ref error);
                    }
                default:
                    {
                        return SmartScrapEmagPage(webPage, ref error);
                    }
            }
        }

        /// <summary>
        /// Scanning a page from the website emag.ro ^^
        /// </summary>
        /// <param name="webPage">link of a web page to be scanned</param>
        /// <param name="error">string error to log</param>
        /// <returns></returns>
        private List<Product> SmartScrapEmagPage(string webPage, ref string error)
        {
            var webGet = new HtmlWeb();
            var document = webGet.Load(webPage);
            if (webGet.StatusCode != System.Net.HttpStatusCode.OK)
            {
                error = "Status code=" + webGet.StatusCode + " detected on page!";
                return null;
            }

            var productsHolder = document.DocumentNode.Descendants().
                FirstOrDefault(item => item.Id == "products-holder");
            List<Product> products = new List<Product>();
            
            if (productsHolder != null)
            {
                var productGridList = productsHolder.Descendants().Where(item => item.Name == "div" && item.GetAttributeValue("class", "") == "product-holder-grid");
                foreach (var productGrid in productGridList)
                {
                    var product = new Product();
                    var pictureContainer = productGrid.Descendants().FirstOrDefault(item => item.Name == "div" && item.GetAttributeValue("class", "").Contains("poza-produs"));
                    if(pictureContainer != null)
                    {
                        var picture = pictureContainer.Descendants().FirstOrDefault(item => item.Name == "img");
                        if(picture != null)
                        {
                            product.photoLink = picture.GetAttributeValue("data-src", null);
                        }

                        var productId = pictureContainer.Descendants().FirstOrDefault(item => item.Name == "input" && item.GetAttributeValue("class", "") == "dl_info");
                        if(productId != null)
                        {
                            string jsonDetails = productId.GetAttributeValue("value", null);
                            if (jsonDetails != null)
                            {
                                dynamic data = JObject.Parse(System.Net.WebUtility.HtmlDecode(jsonDetails));
                                if(data.ecommerce != null && data.ecommerce.click != null
                                    && data.ecommerce.click.products != null 
                                    && data.ecommerce.click.products[0] != null)
                                {
                                    product.siteId = data.ecommerce.click.products[0].id;
                                }
                            }

                        }
                    }

                    var middleContainer = productGrid.Descendants().FirstOrDefault(item => item.Name == "div" && item.GetAttributeValue("class", "") == "middle-container");
                    if (middleContainer != null)
                    {
                        var linkContainer = middleContainer.Descendants().FirstOrDefault(item => item.Name == "a");
                        if (linkContainer != null)
                        {
                            product.title = linkContainer.GetAttributeValue("title", ATTRIBUTE_NOT_FOUND);
                            product.name = linkContainer.InnerText;
                            product.link = linkContainer.GetAttributeValue("href", ATTRIBUTE_NOT_FOUND);
                        }
                    }

                    var priceOver = productGrid.Descendants().FirstOrDefault(item => item.Name == "span" && item.GetAttributeValue("class", "") == "price-over");
                    if (priceOver != null)
                    {
                        var moneyInt = priceOver.Descendants().FirstOrDefault(item => item.GetAttributeValue("class", "") == "money-int");
                        var moneyDecimal = priceOver.Descendants().FirstOrDefault(item => item.GetAttributeValue("class", "") == "money-decimal");
                        string money = String.Empty;
                        product.price = -1;
                        if (moneyInt != null)
                            money = moneyInt.InnerHtml.PrepareMoney();

                        if (moneyDecimal != null)
                            money += "." + moneyDecimal.InnerHtml.PrepareMoney();

                        product.price = money.Convert<double>();
                    }

                    products.Add(product);
                }                
            }   
        
            if(products.Count == 0)
            {
                if (DetectCaptcha(document))
                    error = "Captcha detected on page:" + webPage;                
            }

            return products;
        }
    
        private List<Product> SmartScrapCELPage(string webPage, ref string error)
        {
            var webGet = new HtmlWeb();
            var document = webGet.Load(webPage);
            if (webGet.StatusCode != System.Net.HttpStatusCode.OK)
                return null;

            var productListing = document.DocumentNode.Descendants().
                FirstOrDefault(item => item.Name == "div" && item.GetAttributeValue("class", "") == "productlisting");
            List<Product> products = new List<Product>();

            if (productListing != null)
            {
                var productDataList = productListing.Descendants().Where(item => item.Name == "div" && item.GetAttributeValue("class", "").Contains("product_data productListing-tot"));
                foreach (var productData in productDataList)
                {
                    var product = new Product();
                    product.siteId = productData.GetAttributeValue("pid_prod", null);

                    var pictureContainer = productData.Descendants().FirstOrDefault(item => item.Name == "div" && item.GetAttributeValue("class", "") == "productListing-poza");
                    if(pictureContainer != null)
                    {
                        var picture = pictureContainer.Descendants().FirstOrDefault(item => item.Name == "img");
                        if (picture != null)
                        {
                            product.photoLink = picture.GetAttributeValue("src", null);
                        }
                    }

                    var productContainer = productData.Descendants().FirstOrDefault(item => item.Name == "div" && item.GetAttributeValue("class", "") == "productListing-nume");
                    if (productContainer != null)
                    {
                        var linkContainer = productContainer.Descendants().FirstOrDefault(item => item.Name == "a" && item.GetAttributeValue("class", "").Contains("product_link"));
                        if (linkContainer != null)
                        {
                            product.title = linkContainer.GetAttributeValue("title", ATTRIBUTE_NOT_FOUND);
                            product.name = linkContainer.InnerText;
                            product.link = linkContainer.GetAttributeValue("href", ATTRIBUTE_NOT_FOUND);
                        }
                        /*
                        var priceContainer = productContainer.Descendants().FirstOrDefault(item => item.Name == "div" && item.GetAttributeValue("class", "") == "price_part");
                        if(priceContainer != null)
                        {
                            var pretNou = priceContainer.Descendants().FirstOrDefault(item => item.Name == "div" && item.GetAttributeValue("class", "") == "pret_n");
                            if(pretNou != null)
                            {
                                var pretBContainer = pretNou.Descendants().FirstOrDefault(item => item.Name == "b" && item.GetAttributeValue("style", "") == "" && item.GetAttributeValue("class", "") == "");
                                if(pretBContainer != null)
                                    product.price = pretBContainer.InnerHtml.Convert<double>();
                            }
                        }*/
                        var pretNou = productContainer.Descendants().FirstOrDefault(item => item.Name == "div" && item.GetAttributeValue("class", "") == "pret_n");
                        if (pretNou != null)
                        {
                            var pretBContainer = pretNou.Descendants().FirstOrDefault(item => item.Name == "b" && item.GetAttributeValue("style", "") == "" && item.GetAttributeValue("class", "") == "");
                            if (pretBContainer != null)
                                product.price = pretBContainer.InnerHtml.Convert<double>();
                        }
                    }                    

                    products.Add(product);
                }
            }

            return products;
        }

        #endregion
    }
}
