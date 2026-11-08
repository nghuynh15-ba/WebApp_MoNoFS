using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApp_MoNoFS.Models;

namespace WebApp_MoNoFS.Controllers
{
    public class OrderController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();
        private const string CartSessionKey = "WebApp_MoNoFS_Cart";

        // --- HELPER: CÁC HÀM HỖ TRỢ ---
        #region Helpers
        // Helper: Lấy giỏ hàng từ Session
        private List<CartItem> GetCart()
        {
            var cart = Session[CartSessionKey] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                Session[CartSessionKey] = cart;
            }
            return cart;
        }

        // Helper: Lưu chi tiết đơn hàng vào Database
        private void SaveOrderDetails(int orderId, List<CartItem> cart)
        {
            foreach (var item in cart)
            {
                var detail = new OrderDetail
                {
                    OrderId = orderId,
                    ProductId = item.ProId,
                    Price = item.UnitPrice,
                    Quantity = item.Quantity
                };
                db.OrderDetails.Add(detail);
            }
            db.SaveChanges();
        }
        #endregion

        // =========================================================
        // CASE 1: CHECKOUT (GUEST HOẶC NHẬP ĐỊA CHỈ MỚI)
        // =========================================================
        public ActionResult Checkout()
        {
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(Order order, bool? isForcedGuest)
        {
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            if (!ModelState.IsValid) return View(order);

            // 1. Logic kiểm tra khách trùng (Bỏ qua nếu đang đăng nhập hoặc ép buộc mua ẩn danh)
            if (Session["UserId"] == null && isForcedGuest != true)
            {
                var existingCustomer = db.Customers.FirstOrDefault(c =>
                                        c.Phone == order.CustomerPhone &&
                                        c.Email == order.CustomerEmail);
                if (existingCustomer != null)
                {
                    // Trả về View kèm khách hàng tìm thấy để hiện Modal cảnh báo
                    ViewBag.FoundExistingCustomer = existingCustomer;
                    return View(order);
                }
            }

            // 2. Thiết lập thông tin đơn hàng (System Info)
            order.OrderDate = DateTime.Now;
            order.Status = "Pending";
            order.PaymentType = "Cash";

            if (Session["UserId"] != null)
                order.UserId = (int)Session["UserId"];

            // 3. Lưu vào Database (Header & Details)
            db.Orders.Add(order);
            db.SaveChanges(); // Lưu để sinh OrderId
            SaveOrderDetails(order.OrderId, cart);

            // 4. Dọn dẹp Session & Thông báo
            Session.Remove(CartSessionKey);
            TempData["OrderSuccess"] = true;
            TempData["NewOrderId"] = order.OrderId;
            TempData["NewOrderPhone"] = order.CustomerPhone;

            return RedirectToAction("Index", "Home");
        }

        // =========================================================
        // CASE 2: CHECKOUT DEFAULT (DÙNG THÔNG TIN CÓ SẴN)
        // =========================================================
        [HttpPost]
        public ActionResult CheckoutDefault()
        {
            // Yêu cầu đăng nhập
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction("Index", "Cart");

            int userId = (int)Session["UserId"];
            var user = db.Customers.Find(userId);
            if (user == null) return RedirectToAction("Checkout");

            // Tạo Order mới và map dữ liệu từ User
            var order = new Order
            {
                OrderDate = DateTime.Now,
                Status = "Pending",
                PaymentType = "Cash",
                UserId = userId,
                CustomerName = user.Fullname,
                CustomerPhone = user.Phone,
                CustomerEmail = user.Email,
                CustomerAddress = user.Address
            };

            // Lưu vào Database
            db.Orders.Add(order);
            db.SaveChanges();

            // Ghi log hành động đặt hàng
            if (Session["UserId"] != null)
            {
                int uid = (int)Session["UserId"];
                WebApp_MoNoFS.Helpers.LogHelper.WriteLog(uid, "Order", "Placed order #" + order.OrderId + " successfully");
            }

            // Lưu chi tiết đơn hàng
            SaveOrderDetails(order.OrderId, cart);

            // Hoàn tất
            Session.Remove(CartSessionKey);
            TempData["OrderSuccess"] = true;
            TempData["NewOrderId"] = order.OrderId;
            TempData["NewOrderPhone"] = order.CustomerPhone;

            return RedirectToAction("Index", "Home");
        }

        public ActionResult OrderSuccess()
        {
            return View();
        }

        // =========================================================
        // CASE 3: MY ORDERS (TRACKING CHO THÀNH VIÊN)
        // =========================================================
        public ActionResult MyOrders()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];
            // Lấy danh sách đơn hàng của User, sắp xếp mới nhất lên đầu
            var orders = db.Orders.Where(o => o.UserId == userId)
                                  .OrderByDescending(o => o.OrderDate)
                                  .ToList();
            return View(orders);
        }

        // =========================================================
        // CASE 4: GUEST TRACKING (TRACKING CHO KHÁCH VÃNG LAI)
        // =========================================================
        [HttpGet]
        public ActionResult GuestTracking()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuestTracking(int? searchOrderId, string searchPhone)
        {
            // Validate dữ liệu đầu vào
            if (searchOrderId == null || string.IsNullOrEmpty(searchPhone))
            {
                ModelState.AddModelError("", "Please enter Order ID and Phone Number.");
                return View();
            }

            // Tìm đơn hàng khớp ID và SĐT
            var order = db.Orders.FirstOrDefault(o => o.OrderId == searchOrderId &&
                                                      o.CustomerPhone == searchPhone);
            if (order == null)
            {
                ModelState.AddModelError("", "Order not found or information does not match.");
                return View();
            }

            ViewBag.OrderResult = order;
            return View();
        }

        // =========================================================
        // CASE 5: CANCEL ORDER (HỦY ĐƠN HÀNG)
        // =========================================================
        [HttpPost]
        public ActionResult CancelOrder(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null) return Json(new { success = false, message = "Order not found" });

            // 1. XỬ LÝ LOGIC ĐỔI TRẠNG THÁI
            switch (order.Status)
            {
                case "Pending":
                case "Confirmed":
                    order.Status = "RequestCancel";
                    db.SaveChanges();

                    // Ghi log hành động hủy đơn hàng
                    if (Session["UserId"] != null)
                    {
                        int uid = (int)Session["UserId"];
                        WebApp_MoNoFS.Helpers.LogHelper.WriteLog(uid, "Order", "Requested to cancel order #" + id);
                    }

                    break;
                // Các case lỗi khác xử lý tùy ý, ở đây mình tập trung case thành công
                default:
                    return Json(new { success = false, message = "Cannot cancel this order state." });
            }

            // 2. TẠO URL ĐỂ CHUYỂN HƯỚNG SAU KHI KHÁCH ĐỌC XONG POPUP
            string redirectUrl = "";
            if (Session["UserId"] != null)
            {
                redirectUrl = Url.Action("MyOrders", "Order");
            }
            else
            {
                // Chuyển sang GuestTracking kèm tham số để hiện đơn hàng luôn
                redirectUrl = Url.Action("GuestTracking", "Order", new { searchOrderId = order.OrderId, searchPhone = order.CustomerPhone });
            }

            // 3. TRẢ VỀ JSON THAY VÌ REDIRECT NGAY
            return Json(new { success = true, redirectUrl = redirectUrl });
        }

        // =========================================================
        // CASE 6: PRINT INVOICE (IN HÓA ĐƠN)
        // =========================================================
        public ActionResult PrintInvoice(int id)
        {
            var order = db.Orders.Find(id);
            if (order == null) return HttpNotFound();

            // Trả về View in ấn 
            return View(order);
        }
    }
}