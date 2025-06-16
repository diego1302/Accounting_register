using System.ComponentModel.DataAnnotations;

using Accounting_Register_BE.Areas.Config.Resources.Model;
using Accounting_Register_BE.Areas.Config.Users.Model;

namespace Accounting_Register_BE.Areas.Config.Security_Issuer.Model
{
    public class SecuritiesIssuer
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
        public string webpage {  get; set; }

        public Addreses registered_office_address { get; set; }
        public Addreses postal_office_address { get; set; }
        public int CompanyClientId { get; set; }
        public CompanyClient CompanyClient { get; set; }


    }
}
