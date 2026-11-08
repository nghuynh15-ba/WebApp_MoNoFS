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
    public class CustomerController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();

        // ==========================================================
        // CASE 1: DANH SÁCH & TÌM KIẾM (INDEX)
        // ==========================================================
        #region List & Search

        // GET: Admin/Customer
        // Hiển thị danh sách khách hàng có hỗ trợ tìm kiếm
        public ActionResult Index(string searchString)
        {
            // 1. Sử dụng AsQueryable để tối ưu hiệu năng
            
            var customers = db.Customers.AsQueryable();

            // 2. Logic Tìm kiếm đa năng
            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm kiếm tương đối: Chứa trong Tên HOẶC SĐT HOẶC Email
                customers = customers.Where(c => c.Fullname.Contains(searchString) ||
                                                 c.Phone.Contains(searchString) ||
                                                 c.Email.Contains(searchString));
            }

            // Lưu lại từ khóa tìm kiếm để hiển thị lại trên ô Input của View
            ViewBag.CurrentFilter = searchString;

            // Sắp xếp người mới đăng ký lên đầu tiên và trả về danh sách
            return View(customers.OrderByDescending(c => c.UserId).ToList());
        }
        #endregion

        // ==========================================================
        // CASE 2: CHI TIẾT & LỊCH SỬ ĐƠN HÀNG (DETAILS)
        // ==========================================================
        #region Details

        // GET: Admin/Customer/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Kỹ thuật Eager Loading:
            // Dùng .Include("Orders") để lấy luôn danh sách đơn hàng của khách này
            // Giúp Admin xem được lịch sử mua sắm ngay trong trang chi tiết khách hàng
            var customer = db.Customers.Include("Orders")
                                       .FirstOrDefault(c => c.UserId == id);

            if (customer == null) return HttpNotFound();

            return View(customer);
        }
        #endregion

        // ==========================================================
        // CASE 3: CHỈNH SỬA & UPLOAD ẢNH (EDIT)
        // ==========================================================
        #region Edit

        // GET: Admin/Customer/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var customer = db.Customers.Find(id);
            if (customer == null) return HttpNotFound();

            return View(customer);
        }

        // POST: Admin/Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Thêm tham số HttpPostedFileBase ImageFile để nhận file ảnh từ Form
        public ActionResult Edit(Customer customer, HttpPostedFileBase ImageFile)
        {
            // Tìm bản ghi cũ trong Database
            var existingCustomer = db.Customers.Find(customer.UserId);

            if (existingCustomer != null)
            {
                // 1. Cập nhật thông tin cơ bản
                existingCustomer.Fullname = customer.Fullname;
                existingCustomer.Phone = customer.Phone;
                existingCustomer.Address = customer.Address;
                existingCustomer.Email = customer.Email;
                existingCustomer.Activated = customer.Activated; // Admin có quyền khóa/mở tài khoản

                // 2. XỬ LÝ UPLOAD ẢNH (Nếu Admin chọn ảnh mới)
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    // a. Lấy tên file gốc
                    string fileName = System.IO.Path.GetFileName(ImageFile.FileName);

                    // b. Tạo đường dẫn lưu trữ trên Server (~/Images/CUSTOMERS/)
                    string path = System.IO.Path.Combine(Server.MapPath("~/Images/CUSTOMERS/"), fileName);

                    // c. Lưu file vật lý vào ổ cứng Server
                    ImageFile.SaveAs(path);

                    // d. Cập nhật tên file vào Database
                    existingCustomer.Photo = fileName;
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(customer);
        }
        #endregion

        // ==========================================================
        // CASE 4: XÓA KHÁCH HÀNG (DELETE)
        // ==========================================================
        #region Delete

        // GET: Admin/Customer/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var customer = db.Customers.Find(id);
            if (customer == null) return HttpNotFound();

            return View(customer);
        }

        // POST: Admin/Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var customer = db.Customers.Find(id);
            try
            {
                // KIỂM TRA RÀNG BUỘC DỮ LIỆU:
                // Nếu khách hàng đã từng đặt hàng, KHÔNG ĐƯỢC XÓA để bảo toàn lịch sử doanh thu.
                if (customer.Orders.Any())
                {
                    ViewBag.Error = "Cannot delete this customer because they have order history. Please deactivate their account instead.";
                    return View(customer);
                }

                db.Customers.Remove(customer);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error deleting customer: " + ex.Message;
                return View(customer);
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