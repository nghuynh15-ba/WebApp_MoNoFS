using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApp_MoNoFS.Models;

namespace WebApp_MoNoFS.Areas.Admin.Controllers
{
    public class ProductController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();

        // ==========================================================
        // CASE 1: DANH SÁCH & TÌM KIẾM (INDEX)
        // ==========================================================
        #region List & Search

        // GET: Admin/Product

        public ActionResult Index(string searchString, int? searchId, int? catId, double? minPrice, double? maxPrice)
        {
            // 1. Khởi tạo Query
            var products = db.Products.Include(p => p.Category).AsQueryable();

            // --- MỚI: LOGIC TÌM THEO ID RIÊNG BIỆT ---
            if (searchId != null)
            {
                // Tìm chính xác theo ID
                products = products.Where(p => p.ProId == searchId);
            }

            // 2. LOGIC TÌM KIẾM TÊN (Giữ nguyên hoặc bỏ phần check số nếu muốn ô này chỉ tìm tên)
            if (!string.IsNullOrEmpty(searchString))
            {
                string keyword = searchString.Trim();
                // Nếu đã có ô tìm ID riêng, ô này chỉ nên tìm theo Tên để tránh nhầm lẫn
                products = products.Where(p => p.ProName.Contains(keyword));
            }

            // 3. LOGIC LỌC DANH MỤC
            if (catId != null)
            {
                products = products.Where(p => p.CatId == catId);
            }

            // 4. LOGIC LỌC GIÁ
            if (minPrice != null)
            {
                products = products.Where(p => p.ProPrice >= (decimal)minPrice);
            }
            if (maxPrice != null)
            {
                products = products.Where(p => p.ProPrice <= (decimal)maxPrice);
            }

            // 5. VIEW BAGS
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentSearchId = searchId; // <--- Thêm dòng này để giữ giá trị trên form
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;

            // Dropdown Categories
            var categoryList = from c in db.Categories
                               where c.ParentId != null
                               join p in db.Categories on c.ParentId equals p.CatId
                               select new
                               {
                                   CatId = c.CatId,
                                   CatName = p.CatName + " > " + c.CatName
                               };

            ViewBag.CatId = new SelectList(categoryList.OrderBy(x => x.CatName), "CatId", "CatName", catId);

            // 6. Trả về View
            return View(products.OrderByDescending(p => p.ProId).ToList());
        }
        #endregion

        // ==========================================================
        // CASE 2: THÊM MỚI SẢN PHẨM (CREATE)
        // ==========================================================
        #region Create

        // GET: Admin/Product/Create
        public ActionResult Create()
        {
           
            var categoryList = from c in db.Categories
                               where c.ParentId != null // Chỉ lấy category con
                               join p in db.Categories on c.ParentId equals p.CatId 
                               select new
                               {
                                   CatId = c.CatId,
                                   CatName = p.CatName + " > " + c.CatName 
                               };

            // Sắp xếp theo tên cho dễ nhìn
            ViewBag.CatId = new SelectList(categoryList.OrderBy(x => x.CatName), "CatId", "CatName");

            var product = new Product { ProPrice = 0 };
            return View(product);
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Cho phép nhập HTML trong mô tả
        public ActionResult Create(Product product, HttpPostedFileBase UploadImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // --- XỬ LÝ UPLOAD ẢNH ---
                    if (UploadImage != null && UploadImage.ContentLength > 0)
                    {
                        // 1. Lấy tên file
                        string fileName = Path.GetFileName(UploadImage.FileName);

                        // 2. Tạo đường dẫn lưu ảnh (~/Images/PRODUCTS/)
                        string path = Path.Combine(Server.MapPath("~/Images/PRODUCTS/"), fileName);

                        // 3. Lưu file vào server
                        UploadImage.SaveAs(path);

                        // 4. Lưu tên file vào Model
                        product.ProImage = fileName;
                    }
                    else
                    {
                        // Nếu không chọn ảnh -> Dùng ảnh mặc định
                        product.ProImage = "no-image.png";
                    }

                    db.Products.Add(product);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi lưu dữ liệu: " + ex.Message;
                }
            }

            var categoryList = from c in db.Categories
                               where c.ParentId != null
                               join p in db.Categories on c.ParentId equals p.CatId
                               select new
                               {
                                   CatId = c.CatId,
                                   CatName = p.CatName + " > " + c.CatName
                               };

            ViewBag.CatId = new SelectList(categoryList.OrderBy(x => x.CatName), "CatId", "CatName", product.CatId);

            return View(product);

        }
        #endregion

        // ==========================================================
        // CASE 3: CHỈNH SỬA SẢN PHẨM (EDIT)
        // ==========================================================
        #region Edit

        // GET: Admin/Product/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();

            // Chọn sẵn Category hiện tại trong Dropdown
            ViewBag.CatId = new SelectList(db.Categories, "CatId", "CatName", product.CatId);

            return View(product);
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult Edit(Product product, HttpPostedFileBase UploadImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // --- LOGIC CẬP NHẬT ẢNH ---
                    if (UploadImage != null && UploadImage.ContentLength > 0)
                    {
                        // Nếu user chọn ảnh mới -> Upload và ghi đè tên file
                        string fileName = Path.GetFileName(UploadImage.FileName);
                        string path = Path.Combine(Server.MapPath("~/Images/PRODUCTS/"), fileName);
                        UploadImage.SaveAs(path);
                        product.ProImage = fileName;
                    }
                    // Nếu user KHÔNG chọn ảnh mới -> product.ProImage sẽ giữ nguyên giá trị cũ (nhờ Hidden Field bên View)

                    db.Entry(product).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi cập nhật: " + ex.Message;
                }
            }

            ViewBag.CatId = new SelectList(db.Categories, "CatId", "CatName", product.CatId);
            return View(product);
        }
        #endregion

        // ==========================================================
        // CASE 4: CHI TIẾT & XÓA (DETAILS & DELETE)
        // ==========================================================
        #region Details & Delete

        // GET: Admin/Product/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        // GET: Admin/Product/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            Product product = db.Products.Find(id);
            if (product == null) return HttpNotFound();
            return View(product);
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            try
            {
                // Kiểm tra ràng buộc: Nếu sản phẩm đã nằm trong đơn hàng -> Không được xóa
                // (Cần bổ sung Include hoặc check thủ công nếu chưa cấu hình Cascade Delete)

                db.Products.Remove(product);
                db.SaveChanges();
            }
            catch (Exception)
            {
                // Bắt lỗi khóa ngoại nếu sản phẩm đang được tham chiếu
                ModelState.AddModelError("", "Cannot delete this product because it is included in existing orders.");
                return View(product);
            }
            return RedirectToAction("Index");
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