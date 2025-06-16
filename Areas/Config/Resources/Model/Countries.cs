using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Accounting_Register_BE.Areas.Config.Resources.Model
{
    
    public class Countries
    {
        [Key]
        public int id { get; set; }
        public string aplha2code { get; set; }
        public string aplha3code { get; set; }
        public string numericcode { get; set; }
        public string shortName { get; set; }
        public string short_name_lower_case { get; set; }
        public string full_name { get; set; }
    }    
}
