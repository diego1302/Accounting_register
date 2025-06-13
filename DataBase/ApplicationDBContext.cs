using Accounting_Register_BE.Areas.Security_Issuer.Model;
using Accounting_Register_BE.Areas.Users.Model;
using Accounting_Register_BE.Resources.Model;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Accounting_Register_BE.DataBase
{
    public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
        : base(options)
        {
        }

        public DbSet<SecuritiesIssuer> SecurtiesIssuer { get; set; }

        public DbSet<Countries> Countries { get; set; }
        public DbSet<SecuritiesIssuer> securities_Issuer { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);            

            builder.Entity<SecuritiesIssuer>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();

        }
    }
}
