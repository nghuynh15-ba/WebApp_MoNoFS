using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApp_MoNoFS.Models;

namespace WebApp_MoNoFS.Controllers
{
    public class BlogController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();

        // ==========================================================
        // REGION 1: CÁC HÀM HỖ TRỢ (HELPERS)
        // ==========================================================
        #region Helpers

        // Hàm này tải các dữ liệu chung cho Sidebar (dùng cho cả trang Index và Detail)
        // Giúp tránh việc viết lặp lại code query database nhiều lần
        private void LoadSidebarData()
        {
            // 1. Lấy 3 sản phẩm mới nhất (theo ID giảm dần) để gợi ý ở Sidebar
            ViewBag.FeaturedProducts = db.Products
                                         .OrderByDescending(p => p.ProId)
                                         .Take(3)
                                         .ToList();

            // 2. Lấy danh mục cha (Parent Categories) cho Menu bên cạnh
            ViewBag.ParentCategories = db.Categories
                                         .Where(c => c.ParentId == null)
                                         .ToList();

            // 3. Lấy danh sách bài viết (thường dùng cho widget "Bài viết gần đây")
            // Sắp xếp theo ngày tạo mới nhất
            ViewBag.AllBlogPosts = db.BlogPosts
                                     .OrderByDescending(p => p.CreatedAt)
                                     .ToList();
        }

        #endregion

        // ==========================================================
        // REGION 2: BLOG ACTIONS
        // ==========================================================
        #region Actions

        // GET: Blog (Trang chủ Blog)
        public ActionResult Index()
        {
            ViewBag.Title = "Blog";

            // Lấy danh sách bài viết chính để hiển thị nội dung (Main Content)
            var allPosts = db.BlogPosts.OrderByDescending(p => p.CreatedAt).ToList();

            // Gọi hàm Helper để nạp dữ liệu cho Sidebar
            LoadSidebarData();

            return View(allPosts);
        }

        // GET: Blog/Detail/5 (Chi tiết bài viết)
        public ActionResult Detail(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            // Tìm bài viết theo ID
            var post = db.BlogPosts.SingleOrDefault(p => p.BlogId == id);

            if (post == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = post.Title;

            // Gọi hàm Helper để nạp dữ liệu cho Sidebar (tái sử dụng logic)
            LoadSidebarData();

            return View(post);
        }

        #endregion

        // ==========================================================
        // REGION 3: CLEANUP
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