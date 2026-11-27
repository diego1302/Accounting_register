using Accounting_Register_BE.Areas.Config.Users.Model;
using Accounting_Register_BE.Areas.Security_Issuer.Model;
using Accounting_Register_BE.Areas.Users.Model;
using Accounting_Register_BE.Resources.Model;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting_Register_BE.DataBase
{
    public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
        : base(options)
        {
        }

        public DbSet<SecuritiesIssuer> SecurtiesIssuer { get; set; }

        public DbSet<CompanyClient> CompanyClients { get; set; }

        public DbSet<Countries> Countries { get; set; }
        public DbSet<SecuritiesIssuer> securities_Issuer { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);            

            builder.Entity<SecuritiesIssuer>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();

            builder.Entity<CompanyClient>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();

            // Addresses are stored inline (table splitting). All their columns are optional,
            // so the navigations are marked as required to let EF always materialise them.
            ConfigureAddress(builder.Entity<SecuritiesIssuer>(), x => x.registered_office_address);
            ConfigureAddress(builder.Entity<SecuritiesIssuer>(), x => x.postal_office_address);
            ConfigureAddress(builder.Entity<CompanyClient>(), x => x.registered_office_address);
            ConfigureAddress(builder.Entity<CompanyClient>(), x => x.postal_office_address);
        }

        private static void ConfigureAddress<T>(EntityTypeBuilder<T> entity, Expression<Func<T, Addreses?>> navigation) where T : class
        {
            entity.OwnsOne(navigation, a =>
            {
                a.OwnsOne(x => x.australian_address);
                a.OwnsOne(x => x.OverseasAddress);
                a.Navigation(x => x.australian_address).IsRequired();
                a.Navigation(x => x.OverseasAddress).IsRequired();
            });
            entity.Navigation(navigation!).IsRequired();

        }
    }
}
