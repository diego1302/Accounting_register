using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using Accounting_Register_BE.Areas.Config.Resources.Model;
using Accounting_Register_BE.Areas.Config.Security_Issuer.Model;

namespace Accounting_Register_BE.Areas.Config.Users.Model
{
    public class CompanyClient
    {
        [Key]        
        public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Description { get; set; }
        
        public long ABN { get; set; }
        public long ACN { get; set; }

        public string Code { get; set; }

        public int phone_country_code { get; set; }
        public int phone_number { get; set; }

        public string email { get; set; }
        [AllowNull] public string? webpage {  get; set; }

        public Addreses registered_office_address { get; set; }
        public Addreses postal_office_address { get; set; }
        [AllowNull] public List<ApplicationUser>? registered_users { get; set; } 
        [AllowNull] public List<SecuritiesIssuer>? registered_companies { get; set; }
    }
}
