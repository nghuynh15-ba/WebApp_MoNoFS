using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApp_MoNoFS.Models;

namespace WebApp_MoNoFS.Areas.Admin.Controllers
{
    public class CategoryController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();

        // ==========================================================
        // CASE 1: HIỂN THỊ DANH SÁCH (INDEX)
        // ==========================================================
        #region List

        // GET: Admin/Category
        public ActionResult Index()
        {
            // Lấy toàn bộ danh sách danh mục để hiển thị dạng bảng
            // Có thể nâng cấp thêm: Sắp xếp theo ParentId để gom nhóm cha-con
            return View(db.Categories.ToList());
        }
        #endregion

        // ==========================================================
        // CASE 2: TẠO MỚI (CREATE)
        // ==========================================================
        #region Create

        // GET: Admin/Category/Create
        public ActionResult Create()
        {
            // Logic Dropdown: Chỉ lấy các danh mục gốc (ParentId == null) để làm danh mục cha
            // Tránh việc danh mục con cấp 2, cấp 3 làm cha (nếu muốn giới hạn 2 cấp)
            var parentCategories = db.Categories.Where(c => c.ParentId == null).ToList();

            // Tạo SelectList để View hiển thị Dropdown
            ViewBag.ParentList = new SelectList(parentCategories, "CatId", "CatName");

            return View();
        }

        // POST: Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CatId,CatName,ParentId")] Category category)
        {
            if (ModelState.IsValid)
            {
                db.Categories.Add(category);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu Validate lỗi (vd: chưa nhập tên), cần load lại Dropdown cha để không bị mất dữ liệu
            var parentCategories = db.Categories.Where(c => c.ParentId == null).ToList();
            ViewBag.ParentList = new SelectList(parentCategories, "CatId", "CatName", category.ParentId);

            return View(category);
        }
        #endregion

        // ==========================================================
        // CASE 3: CHỈNH SỬA (EDIT)
        // ==========================================================
        #region Edit

        // GET: Admin/Category/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Category category = db.Categories.Find(id);
            if (category == null) return HttpNotFound();

            
            var parentCategories = db.Categories
                                     .Where(c => c.ParentId == null && c.CatId != id)
                                     .ToList();

            // Chọn sẵn giá trị ParentId hiện tại
            ViewBag.ParentList = new SelectList(parentCategories, "CatId", "CatName", category.ParentId);

            return View(category);
        }

        // POST: Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CatId,CatName,ParentId")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Validation Logic nâng cao: Kiểm tra vòng lặp vô tận
                // Một danh mục không thể chọn chính nó làm cha
                if (category.ParentId == category.CatId)
                {
                    ModelState.AddModelError("", "A category cannot be its own parent.");
                }
                else
                {
                    db.Entry(category).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            // Load lại Dropdown nếu có lỗi
            var parentCategories = db.Categories
                                     .Where(c => c.ParentId == null && c.CatId != category.CatId)
                                     .ToList();
            ViewBag.ParentList = new SelectList(parentCategories, "CatId", "CatName", category.ParentId);

            return View(category);
        }
        #endregion

        // ==========================================================
        // CASE 4: XÓA (DELETE)
        // ==========================================================
        #region Delete

        // GET: Admin/Category/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Category category = db.Categories.Find(id);
            if (category == null) return HttpNotFound();

            return View(category);
        }

        // POST: Admin/Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Category category = db.Categories.Find(id);
            try
            {
                db.Categories.Remove(category);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // Bắt lỗi Foreign Key (Khóa ngoại)
                // Nếu danh mục này đang chứa sản phẩm, Database sẽ không cho xóa
                // Ta bắt lỗi này để hiện thông báo thân thiện cho người dùng thay vì trang lỗi vàng
                ModelState.AddModelError("", "Cannot delete this category because it contains products.");
                return View(category);
            }
        }
        #endregion

        // ==========================================================
        // CASE 5: CLEANUP
        // ==========================================================
        #region Cleanup
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}