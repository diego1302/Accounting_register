using Microsoft.AspNetCore.Identity;

namespace Accounting_Register_BE.Areas.Config.Users.Model
{
    public class ApplicationUser : IdentityUser
    {
        public string user_first_name { get; set; }
        public string? user_middle_name { get; set; }
        public string user_last_name { get; set; }

        public int CompanyClientId { get; set; }
        public CompanyClient CompanyClient { get; set; }
    }
}
