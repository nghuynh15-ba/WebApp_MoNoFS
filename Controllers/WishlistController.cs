using System;
using System.Linq;
using System.Web.Mvc;
using WebApp_MoNoFS.Models;

namespace WebApp_MoNoFS.Controllers
{
    public class WishlistController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();

        // ==========================================================
        // CASE 1: HIỂN THỊ DANH SÁCH (DISPLAY)
        // ==========================================================
        #region Display Actions

        // GET: /Wishlist
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            var wishlist = db.Wishlists
                             .Include("Product")
                             .Where(w => w.UserId == userId)
                             .ToList();

            return View(wishlist);
        }
        #endregion

        // ==========================================================
        // CASE 2: THAO TÁC THÊM / XÓA (ACTIONS)
        // ==========================================================
        #region Main Actions

        // Action: Thêm sản phẩm vào Wishlist
        public ActionResult AddToWishlist(int proId, string returnUrl)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];

            // Kiểm tra trùng lặp
            var existingItem = db.Wishlists
                                 .FirstOrDefault(w => w.UserId == userId && w.ProductId == proId);

            if (existingItem == null)
            {
                Wishlist newItem = new Wishlist
                {
                    UserId = userId,
                    ProductId = proId
                    // CreatedAt = DateTime.Now 
                };

                db.Wishlists.Add(newItem);
                db.SaveChanges();

                TempData["Message"] = "Product added to your wishlist successfully!";

                // --- GHI LOG: THÊM YÊU THÍCH ---
                try
                {
                    var product = db.Products.Find(proId);
                    string proName = product != null ? product.ProName : "Product #" + proId;
                    WriteLog(userId, "AddToWishlist", $"Added '{proName}' to wishlist.");
                }
                catch { /* Ignore log error */ }
                // -------------------------------------
            }
            else
            {
                TempData["Message"] = "This product is already in your wishlist.";
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // Action: Xóa sản phẩm khỏi Wishlist
        public ActionResult Remove(int proId, string returnUrl)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserId"];

            var item = db.Wishlists.FirstOrDefault(w => w.UserId == userId && w.ProductId == proId);

            if (item != null)
            {
                // Lấy thông tin sản phẩm trước để ghi log cho đẹp
                var product = db.Products.Find(proId);
                string proName = product != null ? product.ProName : "Product #" + proId;

                db.Wishlists.Remove(item);
                db.SaveChanges();

                TempData["Message"] = "Item removed from your wishlist successfully.";

                // --- GHI LOG: XÓA YÊU THÍCH ---
                WriteLog(userId, "RemoveFromWishlist", $"Removed '{proName}' from wishlist.");
            }

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);

            return RedirectToAction("Index");
        }
        #endregion

        // ==========================================================
        // CASE 3: HELPERS & CLEANUP
        // ==========================================================
        #region Helpers & Cleanup

       
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
                // Silent fail: Không để lỗi ghi log làm crash app
            }
        }

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