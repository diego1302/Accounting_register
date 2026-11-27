using Microsoft.AspNetCore.Identity;

namespace Accounting_Register_BE.Areas.Users.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string? user_first_name { get; set; }
        public string? user_middle_name { get; set; }
        public string? user_last_name { get; set; }
    }
}
