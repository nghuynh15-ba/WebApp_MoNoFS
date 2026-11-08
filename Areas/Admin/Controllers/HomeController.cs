using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApp_MoNoFS.Models;

namespace WebApp_MoNoFS.Areas.Admin.Controllers
{
    //[Authorize(Roles = "Admin")] 
    public class HomeController : Controller
    {
        private MoNoFSDBEntities db = new MoNoFSDBEntities();

        // ==========================================================
        // CASE 1: DASHBOARD (THỐNG KÊ TỔNG QUAN)
        // ==========================================================
        #region Dashboard Stats

        // GET: Admin/Home
        // Hiển thị các thẻ số liệu thống kê (Revenue, Orders, Customers...)
        public ActionResult Index()
        {
            // --- 1. TÍNH TỔNG DOANH THU (TOTAL REVENUE) ---
            
            var revenue = db.OrderDetails
                            .Where(d => d.Order.Status != "Cancelled")
                            .Sum(d => (decimal?)(d.Price * d.Quantity)) ?? 0;
            

            // --- 2. ĐẾM ĐƠN HÀNG MỚI (NEW ORDERS) ---
            
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var newOrders = db.Orders
                              .Count(o => o.OrderDate.Value.Month == currentMonth &&
                                          o.OrderDate.Value.Year == currentYear);

            // --- 3. ĐẾM TỔNG KHÁCH HÀNG (TOTAL CUSTOMERS) ---
          
            var totalCustomers = db.Customers.Count();

            // --- 4. ĐẾM ĐƠN HÀNG CẦN XỬ LÝ (PENDING) ---
           
            var pendingOrders = db.Orders.Count(o => o.Status == "Pending");

            // --- ĐẨY DỮ LIỆU QUA VIEW ---
            ViewBag.TotalRevenue = revenue;
            ViewBag.NewOrders = newOrders;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.PendingOrders = pendingOrders;

            return View();
        }
        #endregion

        // ==========================================================
        // CASE 2: BIỂU ĐỒ DOANH THU (CHART DATA)
        // ==========================================================
        #region Chart Data API

        // GET: Admin/Home/GetDailyRevenue
        
        public ActionResult GetDailyRevenue()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            // BƯỚC 1: Lấy dữ liệu thô từ Database
            // Chỉ lấy đơn hàng trong tháng này và không bị hủy
            var rawData = db.Orders
                            .Where(o => o.OrderDate.Value.Month == currentMonth &&
                                        o.OrderDate.Value.Year == currentYear &&
                                        o.Status != "Cancelled")
                            .Select(o => new
                            {
                                Date = o.OrderDate.Value.Day,
                                // Tính tổng tiền của từng đơn hàng
                                Revenue = o.OrderDetails.Sum(d => (decimal?)d.Price * d.Quantity) ?? 0
                            })
                            // Gom nhóm theo Ngày (để cộng dồn doanh thu của các đơn trong cùng 1 ngày)
                            .GroupBy(x => x.Date)
                            .Select(g => new { Day = g.Key, Total = g.Sum(x => x.Revenue) })
                            .ToList();

            // BƯỚC 2: Chuẩn hóa dữ liệu 
           
            var daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth); // Lấy số ngày của tháng hiện tại
            var labels = new List<string>();   // Trục hoành: Ngày
            var values = new List<decimal>();  // Trục tung: Tiền

            // Vòng lặp từ ngày 1 đến ngày cuối tháng
            for (int i = 1; i <= daysInMonth; i++)
            {
                labels.Add(i.ToString()); 

                // Tìm trong dữ liệu thô xem ngày 'i' có doanh thu không
                var record = rawData.FirstOrDefault(r => r.Day == i);

                // Nếu có thì lấy Total, nếu không (null) thì gán bằng 0
                values.Add(record != null ? record.Total : 0);
            }

            // Trả về JSON để JavaScript (Chart.js) sử dụng
            return Json(new { labels = labels, values = values }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        // ==========================================================
        // CASE 3: CLEANUP
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