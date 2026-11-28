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
        /// <summary>Posted by an external application through the API (API key).</summary>
        External,
        /// <summary>Financial year closing: moves the year's profit/loss to retained earnings. Excluded from the P&amp;L.</summary>
        Closing,
    }

    public enum RuleMatchField { Description, Reference }
    public enum RuleMatchType { Contains, StartsWith, Equals }
    public enum RuleDirection { Any, MoneyIn, MoneyOut }

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
        /// <summary>Who posted it, for display: the user's email, "API: key name" or "Regla: name".</summary>
        [MaxLength(256)] public string? CreatedBy { get; set; }

        /// <summary>System that sent the entry (e.g. "pos", "payroll"). With <see cref="ExternalId"/> it makes posting idempotent.</summary>
        [MaxLength(50)] public string? ExternalSource { get; set; }
        /// <summary>The sender's own id for this entry; unique per company + <see cref="ExternalSource"/>.</summary>
        [MaxLength(100)] public string? ExternalId { get; set; }
        /// <summary>API key that posted it, when it came from an external app.</summary>
        public int? ApiKeyId { get; set; }

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

    /// <summary>
    /// Credential for an external application, scoped to one company client. Only the SHA-256 of the key is stored;
    /// the plain key is shown once when it is created.
    /// </summary>
    public class ApiKey
    {
        [Key] public int Id { get; set; }
        public int CompanyClientId { get; set; }
        [JsonIgnore] public CompanyClient? CompanyClient { get; set; }

        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        /// <summary>First characters of the key, to recognise it in the list (e.g. "ar_k3J9x2").</summary>
        [Required, MaxLength(16)] public string Prefix { get; set; } = string.Empty;
        [JsonIgnore, Required, MaxLength(64)] public string KeyHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        [JsonIgnore, MaxLength(450)] public string? CreatedByUserId { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }

    /// <summary>Who did what and when. Written in the same SaveChanges as the change it describes.</summary>
    public class AuditLog
    {
        [Key] public long Id { get; set; }
        public int? CompanyClientId { get; set; }
        [JsonIgnore] public CompanyClient? CompanyClient { get; set; }

        public DateTime At { get; set; }
        [MaxLength(450)] public string? UserId { get; set; }
        public int? ApiKeyId { get; set; }
        /// <summary>Display name of the actor: email, "API: key name" or "Sistema".</summary>
        [Required, MaxLength(256)] public string Actor { get; set; } = string.Empty;
        /// <summary>e.g. JournalCreated, JournalDeleted, Reconciled, Unreconciled, PeriodLocked, YearClosed…</summary>
        [Required, MaxLength(50)] public string Action { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string EntityType { get; set; } = string.Empty;
        [MaxLength(50)] public string? EntityId { get; set; }
        [Required, MaxLength(500)] public string Summary { get; set; } = string.Empty;
        /// <summary>Optional JSON snapshot (e.g. the lines of a deleted journal).</summary>
        public string? Details { get; set; }
    }

    /// <summary>Per-company accounting settings. One row per company (created on first use).</summary>
    public class CompanyAccountingSettings
    {
        [Key] public int CompanyClientId { get; set; }
        [JsonIgnore] public CompanyClient? CompanyClient { get; set; }
        /// <summary>Nothing dated on or before this date can be posted, changed, reconciled or undone.</summary>
        public DateOnly? LockDate { get; set; }
    }

    /// <summary>"If the description contains TELSTRA → 489 with GST". Optionally reconciles by itself on import.</summary>
    public class BankRule
    {
        [Key] public int Id { get; set; }
        public int CompanyClientId { get; set; }
        [JsonIgnore] public CompanyClient? CompanyClient { get; set; }
        /// <summary>Null = applies to every bank account of the company.</summary>
        public int? BankAccountId { get; set; }
        [JsonIgnore] public BankAccount? BankAccount { get; set; }

        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        /// <summary>Lower runs first.</summary>
        public int Priority { get; set; }
        public RuleMatchField MatchField { get; set; }
        public RuleMatchType MatchType { get; set; }
        [Required, MaxLength(200)] public string MatchText { get; set; } = string.Empty;
        public RuleDirection Direction { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? AmountMax { get; set; }

        public int AccountId { get; set; }
        [JsonIgnore] public Account? Account { get; set; }
        public TaxType TaxType { get; set; }
        [MaxLength(500)] public string? LineDescription { get; set; }
        /// <summary>Reconcile matching lines automatically when a statement is imported.</summary>
        public bool AutoReconcile { get; set; }
        public bool IsActive { get; set; } = true;

        public bool Matches(BankTransaction t)
        {
            if (!IsActive) return false;
            if (Direction == RuleDirection.MoneyIn && t.Amount <= 0) return false;
            if (Direction == RuleDirection.MoneyOut && t.Amount >= 0) return false;
            var abs = Math.Abs(t.Amount);
            if (AmountMin != null && abs < AmountMin) return false;
            if (AmountMax != null && abs > AmountMax) return false;
            var field = (MatchField == RuleMatchField.Reference ? t.Reference : t.Description) ?? string.Empty;
            var text = MatchText.Trim();
            return MatchType switch
            {
                RuleMatchType.Equals => string.Equals(field.Trim(), text, StringComparison.OrdinalIgnoreCase),
                RuleMatchType.StartsWith => field.TrimStart().StartsWith(text, StringComparison.OrdinalIgnoreCase),
                _ => field.Contains(text, StringComparison.OrdinalIgnoreCase),
            };
        }
    }
}
