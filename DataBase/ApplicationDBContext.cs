using Accounting_Register_BE.Areas.Accounting.Model;
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

        // Accounting (per company client)
        public DbSet<Account> Accounts { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<BankStatement> BankStatements { get; set; }
        public DbSet<BankTransaction> BankTransactions { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalLine> JournalLines { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<CompanyAccountingSettings> AccountingSettings { get; set; }
        public DbSet<BankRule> BankRules { get; set; }

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

            ConfigureAccounting(builder);
        }

        // Every company-level FK is Restrict: SQL Server rejects multiple cascade paths, and a company's books
        // should never disappear by accident. Child rows are deleted explicitly by the controllers.
        private static void ConfigureAccounting(ModelBuilder builder)
        {
            builder.Entity<Account>(e =>
            {
                e.HasOne(x => x.CompanyClient).WithMany().HasForeignKey(x => x.CompanyClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.CompanyClientId, x.Code }).IsUnique();
                e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
                e.Property(x => x.TaxType).HasConversion<string>().HasMaxLength(20);
            });

            builder.Entity<BankAccount>(e =>
            {
                e.HasOne(x => x.CompanyClient).WithMany().HasForeignKey(x => x.CompanyClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => x.AccountId).IsUnique();
            });

            builder.Entity<BankStatement>(e =>
            {
                e.HasOne(x => x.BankAccount).WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BankTransaction>(e =>
            {
                e.HasOne(x => x.BankAccount).WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.BankStatement).WithMany().HasForeignKey(x => x.BankStatementId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.JournalEntry).WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.BankAccountId, x.DedupeKey }).IsUnique();
                e.HasIndex(x => new { x.BankAccountId, x.Status, x.Date });
                e.Property(x => x.Amount).HasPrecision(18, 2);
                e.Property(x => x.Balance).HasPrecision(18, 2);
                e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            });

            builder.Entity<JournalEntry>(e =>
            {
                e.HasOne(x => x.CompanyClient).WithMany().HasForeignKey(x => x.CompanyClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.CompanyClientId, x.Date });
                e.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
                // Idempotency for external apps: the same (source, id) can only be posted once per company.
                e.HasIndex(x => new { x.CompanyClientId, x.ExternalSource, x.ExternalId }).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
            });

            builder.Entity<AuditLog>(e =>
            {
                e.HasOne(x => x.CompanyClient).WithMany().HasForeignKey(x => x.CompanyClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.CompanyClientId, x.At });
                e.HasIndex(x => new { x.CompanyClientId, x.EntityType, x.EntityId });
            });

            builder.Entity<CompanyAccountingSettings>(e =>
            {
                e.HasOne(x => x.CompanyClient).WithOne().HasForeignKey<CompanyAccountingSettings>(x => x.CompanyClientId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BankRule>(e =>
            {
                e.HasOne(x => x.CompanyClient).WithMany().HasForeignKey(x => x.CompanyClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.BankAccount).WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
                e.Property(x => x.MatchField).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.MatchType).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.TaxType).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.AmountMin).HasPrecision(18, 2);
                e.Property(x => x.AmountMax).HasPrecision(18, 2);
            });

            builder.Entity<ApiKey>(e =>
            {
                e.HasOne(x => x.CompanyClient).WithMany().HasForeignKey(x => x.CompanyClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => x.KeyHash).IsUnique();
            });

            builder.Entity<JournalLine>(e =>
            {
                e.HasOne(x => x.JournalEntry).WithMany(x => x.Lines).HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
                e.Property(x => x.Debit).HasPrecision(18, 2);
                e.Property(x => x.Credit).HasPrecision(18, 2);
                e.Property(x => x.TaxType).HasConversion<string>().HasMaxLength(20);
            });
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
