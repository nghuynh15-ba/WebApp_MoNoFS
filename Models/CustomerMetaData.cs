using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;


namespace WebApp_MoNoFS.Models 
{

    [MetadataType(typeof(CustomerMetaData))]
   
    public partial class Customer
    {
        // This class is empty, used only for binding
    }

    
    public class CustomerMetaData
    {
        [DisplayName("Username")]
        [Required(ErrorMessage = "Please enter your desired username.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; set; }

        [DisplayName("Password")]
        [Required(ErrorMessage = "Please enter password.")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; }

        [DisplayName("Full Name")]
        [Required(ErrorMessage = "Please enter full name.")]
        public string Fullname { get; set; }

        [DisplayName("Email")]
        [Required(ErrorMessage = "Please enter email.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [DisplayName("Phone Number")]
        [Required(ErrorMessage = "Please enter phone number.")]
        [StringLength(12, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 12 digits.")]
        public string Phone { get; set; }

        [DisplayName("Address")]
        [Required(ErrorMessage = "Please enter address.")]
        public string Address { get; set; }

        
    }
}