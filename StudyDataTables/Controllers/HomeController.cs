using DataTables.Mvc;
using Helper;
using StudyDataTables.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace StudyDataTables.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (EntityContext db = new EntityContext())
            {
                return View(db.Category.ToList());
            }
        }

        public ActionResult MasterDetails()
        {
            return View();
        }
        public string GetMasterData([ModelBinder(typeof(DataTablesBinder))] IDataTablesRequest param)
        {
            using (EntityContext db = new EntityContext())
            {

                IQueryable<Category> category = db.Category;
                var count = category.Count();
                return DataTablesHelper.GetQuery(param, count, category);
            }
        }

        public string GetDetailsData([ModelBinder(typeof(DataTablesBinder))] IDataTablesRequest param)
        {
            using (EntityContext db = new EntityContext())
            {
                IQueryable<Product> product = db.Product;
                var totalRecords = product.Count();
                var id = Convert.ToInt32(Request.QueryString["id"]);
                product = product.Where(c => c.Category.Id.Equals(id));

                return DataTablesHelper.GetQuery(param, totalRecords, product);
            }
        }

        // Add new Category
        public ActionResult Add(FormCollection form)
        {
            using (EntityContext db = new EntityContext())
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        Category category = new Category();
                        category.Name = form["categoryName"].Trim();
                        db.Category.Add(category);
                        db.SaveChanges();
                        return Json(new { msg = "success" });
                    }
                    catch (Exception ex)
                    {
                        return Json(new { msg = "操作失败！" + ex.Message });
                    }
                }
                return Content("");
            }
        }

        // Delete Category
        public ActionResult Delete(FormCollection form)
        {
            using (EntityContext db = new EntityContext())
            {
                try
                {
                    Category category = db.Category.Find(Convert.ToInt32(form["Id"]));
                    db.Category.Remove(category);
                    db.SaveChanges();
                    return Json(new { msg = "success" });
                }
                catch (Exception ex)
                {
                    return Json(new { msg = "操作失败！" + ex.Message });
                }
            }
        }

        // Edit Category
        public ActionResult Edit(FormCollection form)
        {
            using (EntityContext db = new EntityContext())
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        Category category = db.Category.Find(Convert.ToInt32(form["Id"]));
                        category.Name = form["Name"].Trim();

                        db.SaveChanges();
                        return Json(new { msg = "success" });
                    }
                    catch (Exception ex)
                    {
                        return Json(new { msg = "操作失败！" + ex.Message });
                    }
                }
                return Content("");
            }
        }

        // TODO:显示subGrid
    }
}