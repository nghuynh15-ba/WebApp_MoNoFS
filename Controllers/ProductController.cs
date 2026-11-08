using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using WebApp_MoNoFS.Models;

namespace WebApp_MoNoFS.Controllers
{
    public class ProductController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();

        // GET: Product
        public ActionResult Index(int? categoryId, int page=1)
        {
            ViewBag.Title = "Products";

            //6 sản phẩm mỗi trang 
            int pageSize = 6;

            // Lấy tất cả sản phẩm, kèm theo thông tin Category
            var products = db.Products.Include(p => p.Category).AsQueryable();
           
            // Lọc theo Category
            if (categoryId.HasValue)
            {
                // Tìm tất cả CategoryId cần lọc (bao gồm CategoryId cha và tất cả CategoryId con)
                var relatedCategoryIds = db.Categories 
                    .Where(c => c.CatId == categoryId.Value || c.ParentId == categoryId.Value)
                    .Select(c => c.CatId)
                    .ToList();

                // Áp dụng bộ lọc cho sản phẩm (dựa trên cột CatId trong bảng Product)
                products = products.Where(p => relatedCategoryIds.Contains(p.CatId));
            }
            //tính toán phân trang 
            int totalRecord = products.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecord / pageSize);

            //Lưu biến vào ViewBag
            ViewBag.TotalProducts = totalRecord;
            ViewBag.TotalPages= totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentCatId = categoryId;


            //truy vấn dữ liệu trang hiện tại 
            products = products.OrderBy(p => p.ProId)
                       .Skip((page - 1) * pageSize)
                       .Take(pageSize);


            // Lấy danh mục cha (dùng cho menu/sidebar)
            ViewBag.ParentCategories = db.Categories
                .Where(c => c.ParentId == null)
                .Include(c => c.Category1)
                .ToList();

            //WISHLIST 
            List<int> wishlistProIds = new List<int>();
            if (Session["UserId"] != null)
            {
                int userId = (int)Session["UserId"];
                // Lấy list ProId từ bảng Wishlists
                wishlistProIds = db.Wishlists
                                   .Where(w => w.UserId == userId)
                                   .Select(w => w.ProductId)
                                   .ToList();
            }
            // Truyền sang View để check (Tô tim đỏ hay trắng)
            ViewBag.WishlistProIds = wishlistProIds;



            return View(products.ToList());
        }



        // GET: Product/Detail
        public ActionResult Detail(int id)
        {
            // Truy vấn lấy sản phẩm theo ID và nạp kèm Reviews, Customer (dùng cho Reviews) và Category
            var product = db.Products
                .Include(p => p.Reviews.Select(r => r.Customer))
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .SingleOrDefault(p => p.ProId == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.Title = product.ProName;

            // Lấy CategoryId của sản phẩm hiện tại để tìm sản phẩm liên quan
            int currentCatId = product.CatId;

            // Lấy các sản phẩm liên quan (Cùng Category)
            var relatedProducts = db.Products
                .Where(p => p.ProId != id && p.CatId == currentCatId)
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }


        // 1. Action hiển thị trang kết quả tìm kiếm (Khi bấm Enter hoặc nút Search)
        public ActionResult Search(string Name)
        {
            var products = db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(Name))
            {
                // Tìm kiếm theo tên sản phẩm (gần đúng)
                products = products.Where(p => p.ProName.Contains(Name));
            }

            ViewBag.Keyword = Name;
            return View("Index", products.ToList()); // Tái sử dụng View Index để hiện kết quả
                                                     // Hoặc return View(products.ToList()); nếu bạn tạo view Search.cshtml riêng
        }


        // 2. Action trả về JSON cho gợi ý tìm kiếm (Live Search)
        [HttpGet]
        public JsonResult GetSearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var products = db.Products
                .Where(p => p.ProName.Contains(term))
                .Select(p => new {
                    id = p.ProId,
                    name = p.ProName,
                    price = p.ProPrice,
                    image = p.ProImage
                })
                .Take(5) // Chỉ lấy 5 kết quả đầu tiên 
                .ToList();

            return Json(products, JsonRequestBehavior.AllowGet);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}