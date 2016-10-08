using AutoMapper;
using CrawlerWebMVCApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CrawlerWebMVCApp.Controllers
{
    public class ProductController : Controller
    {
        // GET: Product
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Search()
        {
            SearchProduct model = new SearchProduct();
            model.LoadSearchData();
            return View("Search", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Search(SearchProduct model)
        {           
            model.SearchResult = Mapper.Map<List<ProductModel>, List<SearchProductResult>>(ProductModel.SearchProducts(model));
            return View(model);
        }

        public ActionResult ShowProductChart(int productID)
        {
            return Json(ProductModel.GetProductChart(productID), JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateCategorySelectList(int websiteId, string controlId)
        {
            var model = CategoryModel.GetCategorySelectList(websiteId, 0);
            ViewBag.ControlId = controlId;
            return PartialView("_DDown", model);
        }
    }
}