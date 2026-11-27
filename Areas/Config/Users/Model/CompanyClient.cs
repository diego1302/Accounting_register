using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Accounting_Register_BE.Areas.Users.Model;

using Accounting_Register_BE.Resources.Model;

namespace Accounting_Register_BE.Areas.Config.Users.Model
{
    public class CompanyClient
    {
        [Key]        
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        
        public long? ABN { get; set; }
        public long? ACN { get; set; }

        public string? Code { get; set; }

        public int? phone_country_code { get; set; }
        public long? phone_number { get; set; }

        public string? email { get; set; }
        public string? webpage {  get; set; }

        public Addreses registered_office_address { get; set; } = new();
        public Addreses postal_office_address { get; set; } = new();
        [JsonIgnore]
        public List<ApplicationUser> registered_users { get; set; } = new();


    }
}
