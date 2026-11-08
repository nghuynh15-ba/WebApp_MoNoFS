using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using WebApp_MoNoFS.Models;

namespace WebApp_MoNoFS.Controllers
{
    public class CartController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();
        private const string CartSessionKey = "WebApp_MoNoFS_Cart";

        // =========================================================================
        // CASE 1: CÁC HÀM HỖ TRỢ (HELPERS)
        // =========================================================================
        #region Helpers

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

        private void SaveCart(List<CartItem> cart)
        {
            Session[CartSessionKey] = cart;
        }

        private bool IsUserLoggedIn() => Session["UserId"] != null;

        // --- LOGIC ĐỒNG BỘ GIỎ HÀNG ---
        private void SyncSessionCartToDb(List<CartItem> cart)
        {
            if (!IsUserLoggedIn()) return;

            int userId = (int)Session["UserId"];

            var dbCart = db.Orders.Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.UserId == userId && o.Status == "CART");

            if (dbCart == null)
            {
                dbCart = new Order
                {
                    UserId = userId,
                    Status = "CART",
                    OrderDate = DateTime.Now
                };
                db.Orders.Add(dbCart);
            }

            foreach (var item in cart)
            {
                // QUAN TRỌNG: Tìm chi tiết đơn hàng khớp cả ID và SIZE
                var detail = dbCart.OrderDetails.FirstOrDefault(d => d.ProductId == item.ProId && d.Size == item.Size);

                if (detail != null)
                {
                    detail.Quantity = item.Quantity;
                }
                else
                {
                    var product = db.Products.Find(item.ProId);
                    if (product != null && product.ProPrice != null)
                    {
                        dbCart.OrderDetails.Add(new OrderDetail
                        {
                            ProductId = item.ProId,
                            Quantity = item.Quantity,
                            Price = product.ProPrice.Value,
                            Size = item.Size // Lưu Size vào DB
                        });
                    }
                }
            }
            db.SaveChanges();
        }

        // --- HÀM XÓA  ---
        private void RemoveItemFromDb(int productId, string size)
        {
            if (!IsUserLoggedIn()) return;

            int userId = (int)Session["UserId"];
            var dbCart = db.Orders.Include(o => o.OrderDetails)
                .FirstOrDefault(o => o.UserId == userId && o.Status == "CART");

            if (dbCart != null)
            {
                // Tìm đúng sản phẩm có Size đó để xóa
                var detail = dbCart.OrderDetails.FirstOrDefault(d => d.ProductId == productId && d.Size == size);
                if (detail != null)
                {
                    db.OrderDetails.Remove(detail);
                    db.SaveChanges();
                }
            }
        }

        // --- HÀM GHI LOG HOẠT ĐỘNG ---
        private void WriteLog(int customerId, string actionType, string description)
        {
            try
            {
                var newLog = new CustomerLog();
                newLog.CustomerId = customerId;
                newLog.ActionType = actionType;
                newLog.Description = description;
                newLog.CreatedDate = DateTime.Now;

                db.CustomerLogs.Add(newLog);
                db.SaveChanges();
            }
            catch
            {
                // Nếu lỗi ghi log thì bỏ qua, không làm gián đoạn trải nghiệm mua hàng
            }
        }

        #endregion

        // =========================================================================
        // CASE 2: CÁC CHỨC NĂNG CHÍNH (ACTIONS)
        // =========================================================================
        #region Actions
        public ActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST: /Cart/Add
        [HttpPost]
        public ActionResult Add(int id, int quantity, string size, string returnUrl)
        {
            // Xử lý nếu size bị null (ví dụ sản phẩm ko có size)
            if (string.IsNullOrEmpty(size)) size = "FreeSize";

            var product = db.Products.Find(id);
            if (product == null || product.ProPrice == null)
                return RedirectToAction("Index", "Home");

            var cart = GetCart();

            
            var item = cart.FirstOrDefault(p => p.ProId == id && p.Size == size);

            if (item == null)
            {
                // Thêm mới hoàn toàn
                cart.Add(new CartItem
                {
                    ProId = product.ProId,
                    ProName = product.ProName,
                    UnitPrice = product.ProPrice.Value,
                    Quantity = quantity,
                    ProImage = product.ProImage,
                    Size = size // Gán Size nhận được từ View
                });
            }
            else
            {
                // Đã có sản phẩm cùng Size -> Cộng dồn
                item.Quantity += quantity;
            }

            SaveCart(cart);

            if (IsUserLoggedIn())
            {
                SyncSessionCartToDb(cart);

                // --- [MỚI] GHI LOG: THÊM VÀO GIỎ ---
                int userId = (int)Session["UserId"];
                string desc = $"Added product '{product.ProName}' (Size: {size}) to cart.";
                WriteLog(userId, "AddToCart", desc);
            }

            return RedirectToAction("Index");
        }

        // POST: /Cart/Update
        [HttpPost]
        public ActionResult Update(int id, string size, int quantity)
        {
            if (quantity < 1) quantity = 1;
            if (string.IsNullOrEmpty(size)) size = "FreeSize";

            var cart = GetCart();
            // Tìm đúng dòng có ID và Size khớp
            var item = cart.FirstOrDefault(p => p.ProId == id && p.Size == size);

            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);

                if (IsUserLoggedIn())
                    SyncSessionCartToDb(cart);
            }
            return RedirectToAction("Index");
        }

        // GET: /Cart/Remove
        public ActionResult Remove(int id, string size)
        {
            // 1. Xử lý size null hoặc rỗng
            if (string.IsNullOrEmpty(size)) size = "FreeSize";

            var cart = GetCart();

            // 2. Tìm item (Sử dụng Equals để KHÔNG phân biệt hoa thường)
            var item = cart.FirstOrDefault(p => p.ProId == id &&
                       string.Equals(p.Size, size, StringComparison.OrdinalIgnoreCase));

            if (item != null)
            {
                // Lưu tên sản phẩm để log
                string productName = item.ProName;

                // Xóa khỏi Session
                cart.Remove(item);
                SaveCart(cart);

                // Xóa khỏi Database nếu đã đăng nhập
                if (IsUserLoggedIn())
                {
                    RemoveItemFromDb(id, item.Size);

                    // Ghi Log
                    int userId = (int)Session["UserId"];
                    string desc = $"Removed '{productName}' (Size: {size}) from cart.";
                    WriteLog(userId, "RemoveFromCart", desc);
                }
            }

            // 3. Quan trọng: Redirect về Index để load lại trang
            return RedirectToAction("Index");
        }

        // GET: /Cart/HeaderCounts
        [HttpGet]
        public ActionResult GetHeaderCounts()
        {
            int cartCount = 0;
            var cart = Session[CartSessionKey] as List<CartItem>;
            if (cart != null)
            {
                cartCount = cart.Sum(x => x.Quantity);
            }

            int wishlistCount = 0;
            if (Session["UserId"] != null)
            {
                try
                {
                    int userId = (int)Session["UserId"];
                    wishlistCount = db.Wishlists.Count(w => w.UserId == userId);
                }
                catch { wishlistCount = 0; }
            }

            return Json(new { CartCount = cartCount, WishlistCount = wishlistCount }, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}