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
    public class OrderController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();

        // 1. DANH SÁCH ĐƠN HÀNG
      
        // GET: Admin/Order
        public ActionResult Index(string searchKeyword)
        {
            var orders = db.Orders.Include("OrderDetails").AsQueryable();

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                string keyword = searchKeyword.Trim();
                int orderId;
                bool isNumber = int.TryParse(keyword, out orderId);

                if (isNumber)
                {
                    // --- SỬA Ở ĐÂY ---
                    // Nếu nhập số: CHỈ so sánh bằng chính xác ID
                    // Bỏ qua tìm kiếm theo Tên và SĐT ở trường hợp này
                    orders = orders.Where(o => o.OrderId == orderId);
                }
                else
                {
                    // Nếu nhập chữ: Tìm gần đúng theo Tên hoặc SĐT
                    orders = orders.Where(o => o.CustomerName.Contains(keyword)
                                            || o.CustomerPhone.Contains(keyword));
                }
            }

            ViewBag.CurrentKeyword = searchKeyword;

            // Sắp xếp: Ưu tiên đơn yêu cầu hủy lên đầu
            return View(orders.OrderByDescending(o => o.Status == "RequestCancel")
                              .ThenByDescending(o => o.OrderId)
                              .ToList());
        }
        


        // 2. CHI TIẾT ĐƠN HÀNG
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // Lấy đơn hàng và nạp luôn chi tiết đơn hàng + thông tin sản phẩm
            var order = db.Orders.Include("OrderDetails").Include("OrderDetails.Product")
                          .FirstOrDefault(o => o.OrderId == id);

            if (order == null) return HttpNotFound();

            return View(order);
        }

        // 3. CẬP NHẬT TRẠNG THÁI (GET)
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var order = db.Orders.Find(id);
            if (order == null) return HttpNotFound();

            // Tạo danh sách trạng thái để chọn
            ViewBag.StatusList = new List<string> { "Pending", "Confirmed", "Shipping", "Completed", "Cancelled", "RequestCancel" };

            return View(order);
        }

        // 3. CẬP NHẬT TRẠNG THÁI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderId,Status")] Order order)
        {
            // Chúng ta chỉ update trạng thái, các thông tin khác giữ nguyên
            // Để an toàn, nên load lại order từ db và chỉ sửa field Status
            var orderInDb = db.Orders.Find(order.OrderId);

            if (orderInDb != null)
            {
                orderInDb.Status = order.Status;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(order);
        }

        //4. XỬ LÝ YÊU CẦU HỦY ĐƠN HÀNG
        [HttpPost]
        public ActionResult ApproveCancel(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null) return HttpNotFound();

            if (order.Status == "RequestCancel")
            {
                order.Status = "Cancelled"; // Chuyển thành Đã hủy

                // TODO: Nếu bạn có quản lý kho (Product Quantity), hãy cộng lại số lượng tồn kho ở đây
                // Ví dụ: foreach(var item in order.OrderDetails) { ... }

                db.SaveChanges();
            }

            // Quay lại trang danh sách
            return RedirectToAction("Index");
        }

        // Admin TỪ CHỐI hủy (Bắt buộc đơn đi tiếp)
        [HttpPost]
        public ActionResult RejectCancel(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null) return HttpNotFound();

            if (order.Status == "RequestCancel")
            {
                // Khôi phục lại trạng thái cũ
                // Thường là quay về "Confirmed" (Đã xác nhận) để tiếp tục quy trình đóng gói
                order.Status = "Confirmed";
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}