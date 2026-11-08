using System;
using System.Web;
using WebApp_MoNoFS.Models; 

namespace WebApp_MoNoFS.Helpers
{
    public static class LogHelper
    {
        // Hàm này nhận vào: ID khách, Loại hành động (Order/Login), và Nội dung
        public static void WriteLog(int userId, string actionType, string description)
        {
            try
            {
                using (var db = new MoNoFSDBEntities())
                {
                    var log = new CustomerLog();
                    log.CustomerId = userId; 
                    log.ActionType = actionType;
                    log.Description = description;
                    log.CreatedDate = DateTime.Now;

                    // Lấy IP người dùng (để biết họ đăng nhập ở đâu)
                    log.IPAddress = HttpContext.Current?.Request?.UserHostAddress;

                    db.CustomerLogs.Add(log);
                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                // Nếu lỗi ghi log thì im lặng bỏ qua, không làm crash web
            }
        }
    }
}