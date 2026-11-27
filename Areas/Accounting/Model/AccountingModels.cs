using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Accounting_Register_BE.Areas.Config.Users.Model;

namespace Accounting_Register_BE.Areas.Accounting.Model
{
    /// <summary>Xero-style account types. Bank accounts are always <see cref="Bank"/>.</summary>
    public enum AccountType
    {
        Bank,
        CurrentAsset,
        NonCurrentAsset,
        CurrentLiability,
        NonCurrentLiability,
        Equity,
        Revenue,
        OtherIncome,
        DirectCosts,
        Expense,
    }

    /// <summary>Australian GST treatment. Only <see cref="GST"/> splits 1/11 of the gross amount to the GST account.</summary>
    public enum TaxType
    {
        GST,
        GSTFree,
        BASExcluded,
        InputTaxed,
    }

    public enum BankTransactionStatus
    {
        Unreconciled,
        Reconciled,
    }

    public enum JournalSource
    {
        Bank,
        Manual,
    }

    /// <summary>Chart of accounts entry. Each company client has its own chart.</summary>
    public class Account
    {
        [Key] public int Id { get; set; }
        public int CompanyClientId { get; set; }
        [JsonIgnore] public CompanyClient? CompanyClient { get; set; }

        [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
        [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public TaxType TaxType { get; set; } = TaxType.BASExcluded;
        [MaxLength(500)] public string? Description { get; set; }
        public bool IsArchived { get; set; }

        /// <summary>Marks accounts the system posts to automatically (e.g. "GST"). Not editable from the API.</summary>
        [MaxLength(30)] public string? SystemKey { get; set; }
    }

    public class BankAccount
    {
        [Key] public int Id { get; set; }
        public int CompanyClientId { get; set; }
        [JsonIgnore] public CompanyClient? CompanyClient { get; set; }

        [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
        [MaxLength(100)] public string? BankName { get; set; }
        [MaxLength(10)] public string? BSB { get; set; }
        [MaxLength(30)] public string? AccountNumber { get; set; }
        [MaxLength(3)] public string Currency { get; set; } = "AUD";

        /// <summary>Ledger account (type Bank) that receives every reconciled movement.</summary>
        public int AccountId { get; set; }
        [JsonIgnore] public Account? Account { get; set; }

        /// <summary>Last CSV column mapping used, as JSON, so the next import is pre-filled.</summary>
        [JsonIgnore] public string? CsvMappingJson { get; set; }
    }

    /// <summary>One uploaded statement file.</summary>
    public class BankStatement
    {
        [Key] public int Id { get; set; }
        public int BankAccountId { get; set; }
        [JsonIgnore] public BankAccount? BankAccount { get; set; }

        [Required, MaxLength(260)] public string FileName { get; set; } = string.Empty;
        [MaxLength(10)] public string Format { get; set; } = "csv";
        public DateTime ImportedAt { get; set; }
        [MaxLength(450)] public string? ImportedByUserId { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int ImportedCount { get; set; }
        public int DuplicateCount { get; set; }
    }

    /// <summary>A single statement line. Amount &gt; 0 is money in (deposit), &lt; 0 money out.</summary>
    public class BankTransaction
    {
        [Key] public int Id { get; set; }
        public int BankAccountId { get; set; }
        [JsonIgnore] public BankAccount? BankAccount { get; set; }
        public int BankStatementId { get; set; }
        [JsonIgnore] public BankStatement? BankStatement { get; set; }

        public DateOnly Date { get; set; }
        [Required, MaxLength(500)] public string Description { get; set; } = string.Empty;
        [MaxLength(100)] public string? Reference { get; set; }
        public decimal Amount { get; set; }
        public decimal? Balance { get; set; }

        /// <summary>Bank-provided unique id (OFX FITID) when available.</summary>
        [MaxLength(100)] public string? ExternalId { get; set; }
        /// <summary>Duplicate-detection key, unique per bank account.</summary>
        [Required, MaxLength(64)] public string DedupeKey { get; set; } = string.Empty;

        public BankTransactionStatus Status { get; set; } = BankTransactionStatus.Unreconciled;
        public int? JournalEntryId { get; set; }
        [JsonIgnore] public JournalEntry? JournalEntry { get; set; }
    }

    /// <summary>Double-entry journal. Sum of debits always equals sum of credits.</summary>
    public class JournalEntry
    {
        [Key] public int Id { get; set; }
        public int CompanyClientId { get; set; }
        [JsonIgnore] public CompanyClient? CompanyClient { get; set; }

        public DateOnly Date { get; set; }
        [MaxLength(100)] public string? Reference { get; set; }
        [Required, MaxLength(500)] public string Narration { get; set; } = string.Empty;
        public JournalSource Source { get; set; }
        public DateTime CreatedAt { get; set; }
        [MaxLength(450)] public string? CreatedByUserId { get; set; }

        public List<JournalLine> Lines { get; set; } = new();
    }

    public class JournalLine
    {
        [Key] public int Id { get; set; }
        public int JournalEntryId { get; set; }
        [JsonIgnore] public JournalEntry? JournalEntry { get; set; }

        public int AccountId { get; set; }
        [JsonIgnore] public Account? Account { get; set; }

        [MaxLength(500)] public string? Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        /// <summary>Tax treatment chosen for this line (informational, the GST itself is its own line).</summary>
        public TaxType? TaxType { get; set; }
    }
}
