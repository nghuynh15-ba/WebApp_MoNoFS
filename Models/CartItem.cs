using System; 
using System.Collections.Generic; 
using System.Linq; 
using System.Web; 


namespace WebApp_MoNoFS.Models
{
    public class CartItem
    {
        public int ProId { get; set; }
        public string ProName { get; set; }
        public string ProImage { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public string Size { get; set; }
        public decimal SubTotal
        {
            get { return UnitPrice * Quantity; }
        }
    }
}