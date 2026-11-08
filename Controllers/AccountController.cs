using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using WebApp_MoNoFS.Models;
using System.Web.Security;

namespace WebApp_MoNoFS.Controllers
{
    public class AccountController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();
        private const string CartSessionKey = "WebApp_MoNoFS_Cart";

        // ===== CÁC HÀM HỖ TRỢ GIỎ HÀNG =====
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

        private void ClearCart()
        {
            Session.Remove(CartSessionKey);
        }

        // ==================================================
        // CASE1: ĐĂNG KÝ (REGISTER)
        // ==================================================
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(Customer model)
        {
            if (ModelState.IsValid)
            {
                if (db.Customers.Any(c => c.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View(model);
                }
                if (db.Customers.Any(c => c.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already taken.");
                    return View(model);
                }

                // Mã hóa mật khẩu trước khi lưu
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

                model.Activated = true;
                // Gán ảnh mặc định nếu chưa có
                if (string.IsNullOrEmpty(model.Photo)) model.Photo = "avt.png";

                db.Customers.Add(model);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        //===================================================
        // CASE 2: ĐĂNG NHẬP (LOGIN)
        // ==================================================

        // GET: Hiển thị form đăng nhập
        
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            // 1. Xóa sạch dấu vết cũ để tránh lỗi "Redirect Loop"
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();

            // 2. Lưu lại đường dẫn muốn vào (nếu có)
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        // ==================================================
        // POST: Xử lý đăng nhập
        // ==================================================
        [HttpPost]
        [AllowAnonymous]
        // [ValidateAntiForgeryToken] // Tạm thời TẮT dòng này để test
        public ActionResult Login(WebApp_MoNoFS.Models.Customer model, string returnUrl)
        {
            // BƯỚC 1: Tìm user trong DB
            var user = db.Customers
                .SingleOrDefault(u => u.Email == model.Email || u.Username == model.Username);

            if (user != null)
            {
                // BƯỚC 2: Kiểm tra mật khẩu
                bool isPassOk = false;

                // Logic check pass
                if (user.Password != null && user.Password.StartsWith("$") && user.Password.Length >= 60)
                {
                    try { isPassOk = BCrypt.Net.BCrypt.Verify(model.Password, user.Password); }
                    catch { isPassOk = false; }
                }
                else
                {
                    // Pass thường
                    if (user.Password == model.Password)
                    {
                        isPassOk = true;
                        // Tự động nâng cấp mật khẩu
                        try
                        {
                            user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                            db.SaveChanges();
                        }
                        catch { }
                    }
                }

                // BƯỚC 3: NẾU ĐĂNG NHẬP THÀNH CÔNG
                if (isPassOk)
                {
                    // --- KHỬ TRÙNG DỮ LIỆU ---
                    string rawRole = user.Role ?? "";
                    // QUAN TRỌNG: Biến cleanRole này mới là chuẩn nhất
                    string cleanRole = rawRole.Trim();

                    // === BẪY LỖI (Giữ lại để debug nếu cần) ===
                    if (cleanRole != "Admin" && cleanRole != "Customer")
                    {
                        // Lưu ý: Nếu user là Customer thì không sao, nhưng nếu là Admin mà viết sai thì sẽ báo lỗi
                        // Bạn có thể bỏ qua đoạn if này nếu muốn cho Customer đăng nhập bình thường
                    }

                    // --- A. TẠO COOKIE ---
                    FormsAuthentication.SetAuthCookie(user.Username, false);

                    // --- B. TẠO SESSION ---
                    Session["User"] = user;
                    Session["UserId"] = user.UserId;
                    Session["Username"] = user.Username;
                    Session["Fullname"] = user.Fullname;
                    Session["Photo"] = user.Photo;

                    // --- SỬA LỖI Ở ĐÂY ---
                    // Dùng luôn cleanRole, KHÔNG ĐƯỢC lấy lại user.Role nữa
                    Session["Role"] = cleanRole;

                    // --- C. XỬ LÝ GIỎ HÀNG ---
                    try
                    {
                        Session["WebApp_MoNoFS_Cart"] = null;
                        var dbCart = db.Orders.Include(o => o.OrderDetails.Select(d => d.Product))
                            .FirstOrDefault(o => o.UserId == user.UserId && o.Status == "CART");

                        List<CartItem> cartItems = new List<CartItem>();
                        if (dbCart != null)
                        {
                            cartItems = dbCart.OrderDetails.Select(d => new CartItem
                            {
                                ProId = d.ProductId,
                                ProName = d.Product.ProName,
                                ProImage = d.Product.ProImage,
                                UnitPrice = d.Price.GetValueOrDefault(),
                                Quantity = d.Quantity.GetValueOrDefault(),
                                Size = d.Size
                            }).ToList();
                        }
                        Session["WebApp_MoNoFS_Cart"] = cartItems;
                        WebApp_MoNoFS.Helpers.LogHelper.WriteLog(user.UserId, "Login", "Logged in successfully");
                    }
                    catch { }

                    // --- D. ĐIỀU HƯỚNG (CHỐT HẠ) ---

                    // Ưu tiên 1: Dùng biến cleanRole để kiểm tra
                    if (cleanRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }

                    // Ưu tiên 2: Về trang chủ
                    return RedirectToAction("Index", "Home");
                }
            }

            // Nếu sai
            ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác.");
            return View(model);
        }
        // ==================================================
        // CASE 3: ĐĂNG XUẤT (LOGOFF)
        // ==================================================
        public ActionResult Logoff()
        {
            ClearCart();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        // ==================================================
        // CASE 4: TRANG THÔNG TIN (INFO)
        // ==================================================
        [HttpGet]
        public ActionResult Info()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login");

            int userId = (int)Session["UserId"];
            var user = db.Customers.Find(userId);
            return View(user);
        }

        // ==================================================
        // CASE 5: CẬP NHẬT HỒ SƠ (UPDATE PROFILE) 
        // ==================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(int UserId, HttpPostedFileBase ImageFile)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login");

            var user = db.Customers.Find(UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found!";
                return RedirectToAction("Info");
            }

            // Lấy dữ liệu từ Form
            string fullname = Request.Form["Fullname"];
            string username = Request.Form["Username"];
            string email = Request.Form["Email"];
            string phone = Request.Form["Phone"];
            string address = Request.Form["Address"];

            // Validate cơ bản
            if (string.IsNullOrWhiteSpace(fullname) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Fullname, Username, and Email cannot be empty!";
                return RedirectToAction("Info");
            }

            // Kiểm tra trùng lặp (Duplicate check)
            if (db.Customers.Any(c => c.Username == username && c.UserId != UserId))
            {
                TempData["ErrorMessage"] = "Username already exists!";
                return RedirectToAction("Info");
            }
            if (db.Customers.Any(c => c.Email == email && c.UserId != UserId))
            {
                TempData["ErrorMessage"] = "Email already exists!";
                return RedirectToAction("Info");
            }

            // Cập nhật thông tin text
            user.Fullname = fullname.Length > 50 ? fullname.Substring(0, 50) : fullname;
            user.Username = username.Length > 50 ? username.Substring(0, 50) : username;
            user.Email = email.Length > 50 ? email.Substring(0, 50) : email;
            user.Phone = phone;
            user.Address = address;

            // Xử lý Upload Ảnh
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                // 1. Định nghĩa đường dẫn thư mục chứa ảnh
                
                string folderPath = Server.MapPath("~/Images/CUSTOMERS/");

                // 2. Kiểm tra nếu thư mục chưa có thì tạo mới
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                // 3. Tạo tên file 
                string fileExtension = System.IO.Path.GetExtension(ImageFile.FileName);
                string fileName = $"{UserId}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{fileExtension}";

                // 4. Đường dẫn đầy đủ để lưu file
                string fullPath = System.IO.Path.Combine(folderPath, fileName);

                // 5. Lưu file
                ImageFile.SaveAs(fullPath);

                // 6. Cập nhật object User và Session
                user.Photo = fileName;
                Session["Photo"] = fileName;
            }

            try
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                // Ghi log cập nhật hồ sơ
                WebApp_MoNoFS.Helpers.LogHelper.WriteLog(UserId, "Profile", "Updated profile information");

                // Cập nhật lại các Session khác phòng trường hợp đổi tên
                Session["Fullname"] = user.Fullname;
                Session["Username"] = user.Username;

                // Đảm bảo Session Photo luôn đúng giá trị trong DB
                if (!string.IsNullOrEmpty(user.Photo))
                {
                    Session["Photo"] = user.Photo;
                }

                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                string errorMsg = "";
                foreach (var eve in ex.EntityValidationErrors)
                    foreach (var ve in eve.ValidationErrors)
                        errorMsg += $"{ve.PropertyName}: {ve.ErrorMessage}\n";

                TempData["ErrorMessage"] = errorMsg;
            }

            return RedirectToAction("Info");
        }

        // ==================================================
        // CASE 6: XEM LOG HOẠT ĐỘNG (ACTIVITY LOG)
        // ==================================================
        public ActionResult ActivityLog()
        {
            // 1. Lấy ID user đang đăng nhập 
            
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int userId = (int)Session["UserId"];

            // 2. Lấy dữ liệu từ Database
           
            var logs = db.CustomerLogs
                         .Where(x => x.CustomerId == userId)
                         .OrderByDescending(x => x.CreatedDate) 
                         .ToList();

            // 3. Trả về View cùng Model
            return View(logs);
        }
    }
}