using Accounting_Register_BE.Areas.Config.Resources.Model;
using Accounting_Register_BE.Areas.Config.Security_Issuer.Model;
using Accounting_Register_BE.Areas.Config.Users.Model;

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
        public DbSet<CompanyClient> company_clients { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);            

            builder.Entity<SecuritiesIssuer>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();

            // One client has many users
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.CompanyClient)
                .WithMany(c => c.registered_users)
                .HasForeignKey(u => u.CompanyClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // One client has many issuers
            builder.Entity<SecuritiesIssuer>()
                .HasOne(s => s.CompanyClient)
                .WithMany(c => c.registered_companies)
                .HasForeignKey(s => s.CompanyClientId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
