using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.Resources.Model
{
    public enum AustralianState
    {
        NSW,
        VIC,
        QLD,
        NT,
        WA,
        TAS,
        ACT
    }

    [Owned]
    public class Addreses
    {

        public string postal_delivery_number_line_1 { get; set; }
        public string postal_delivery_number_line_2 { get; set; }
        public string postal_delivery_number_line_3 { get; set; }

        public string country { get; set; }

        public string sub_unit_and_or_Level { get; set; }

        public int street_number { get; set; }

        public int street_name { get; set; }

        public AustralianAddress australian_address { get; set; }
        public OverseasAddress OverseasAddress { get; set; }
    }

    [Owned]
    public class AustralianAddress
    {
        public string postal_delivery_street_type { get; set; }

        public string suburb { get; set; }
        public AustralianState state { get; set; }

        public int australia_post_code { get; set; }
    }

    [Owned]
    public class OverseasAddress
    {
        public string overseas_post_code { get; set; }
    }
}
