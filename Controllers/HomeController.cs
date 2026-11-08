using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApp_MoNoFS.Models;

namespace WebApp_MoNoFS.Controllers
{
    public class HomeController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();

        // ==========================================================
        // CASE 1: LOGIC TRANG CHỦ (INDEX)
        // ==========================================================
        #region Homepage Logic

        public ActionResult Index()
        {
            // Định nghĩa các nhóm Category ID cho từng phân khu trên trang chủ
            // Lưu ý: Đây là cách Hardcode (gán cứng) ID danh mục. 
            // Trong thực tế nên thêm cột "IsHomepage" hoặc "Group" trong database để linh hoạt hơn.

            var womenIds = new List<int> { 4, 5, 6, 7, 8 };       // Danh mục Nữ
            var menIds = new List<int> { 9, 10, 11, 12 };         // Danh mục Nam
            var accessoriesIds = new List<int> { 13, 14, 15 };    // Phụ kiện

            // Gọi hàm Helper để lấy 3 sản phẩm đắt nhất cho từng nhóm
            ViewBag.womenList = GetTopProducts(womenIds, 3);
            ViewBag.menList = GetTopProducts(menIds, 3);
            ViewBag.accesoriesList = GetTopProducts(accessoriesIds, 3);

            return View();
        }

        // --- HELPER METHOD: Hàm hỗ trợ truy vấn ---
        // Giúp code ngắn gọn, không phải viết lại câu lệnh LINQ 3 lần
        private List<Product> GetTopProducts(List<int> categoryIds, int takeCount)
        {
            return db.Products
                     .Where(p => categoryIds.Contains(p.CatId)) // Lọc theo danh sách ID danh mục
                     .OrderByDescending(p => p.ProPrice)        // Sắp xếp giá giảm dần (Sản phẩm nổi bật/đắt tiền)
                     .Take(takeCount)                           // Chỉ lấy số lượng nhất định (Top 3)
                     .ToList();
        }

        #endregion

        // ==========================================================
        // CASE 2: CÁC TRANG TĨNH (STATIC PAGES)
        // ==========================================================
        #region Static Pages

        public ActionResult About()
        {
            ViewBag.Title = "About us";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Title = "Contact";
            return View();
        }
        #endregion

        // ==========================================================
        // CASE 3: CLEANUP
        // ==========================================================
        #region Cleanup
        // Đảm bảo đóng kết nối Database khi Controller xử lý xong request
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