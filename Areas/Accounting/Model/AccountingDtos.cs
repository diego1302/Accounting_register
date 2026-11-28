using System.ComponentModel.DataAnnotations;

namespace Accounting_Register_BE.Areas.Accounting.Model
{
    public class AccountInput
    {
        [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
        [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public TaxType TaxType { get; set; } = TaxType.BASExcluded;
        [MaxLength(500)] public string? Description { get; set; }
        public bool IsArchived { get; set; }
    }

    public class BankAccountInput
    {
        [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
        [MaxLength(100)] public string? BankName { get; set; }
        [MaxLength(10)] public string? BSB { get; set; }
        [MaxLength(30)] public string? AccountNumber { get; set; }
        [MaxLength(3)] public string? Currency { get; set; }
        /// <summary>Ledger code for the new Bank account; auto-assigned (090, 091…) when empty. Ignored on update.</summary>
        [MaxLength(20)] public string? AccountCode { get; set; }
    }

    public record BankAccountDto(
        int Id, string Name, string? BankName, string? BSB, string? AccountNumber, string Currency,
        int AccountId, string AccountCode,
        decimal StatementBalance, DateOnly? StatementBalanceDate, decimal LedgerBalance,
        int UnreconciledCount, DateTime? LastImportAt);

    public record ImportResultDto(int? StatementId, int Imported, int Duplicates, DateOnly? FromDate, DateOnly? ToDate, List<Services.ParseError> Errors, int AutoReconciled = 0);

    public record JournalLineDto(int AccountId, string AccountCode, string AccountName, string? Description, decimal Debit, decimal Credit, TaxType? TaxType);

    public record JournalEntryDto(int Id, DateOnly Date, string? Reference, string Narration, JournalSource Source, List<JournalLineDto> Lines, string? CreatedBy = null);

    /// <summary>Proposed coding: from a bank rule (RuleId set) or from the last identical reconciled line.</summary>
    public record SuggestionDto(int AccountId, TaxType TaxType, int? RuleId = null, string? RuleName = null, string? Description = null);

    /// <summary>An existing journal (manual, external or the other side of a transfer) that already records this bank line.</summary>
    public record MatchCandidateDto(int JournalEntryId, DateOnly Date, string Narration, string? Reference, JournalSource Source);

    /// <summary>A pending line in another bank account of the company with the opposite amount (likely the other side of a transfer).</summary>
    public record TransferCandidateDto(int TransactionId, int BankAccountId, string BankAccountName, DateOnly Date, string Description, decimal Amount);

    public record BankTransactionDto(
        int Id, int BankAccountId, int BankStatementId, DateOnly Date, string Description, string? Reference,
        decimal Amount, decimal? Balance, BankTransactionStatus Status, JournalEntryDto? JournalEntry, SuggestionDto? Suggestion,
        List<MatchCandidateDto> MatchCandidates, List<TransferCandidateDto> TransferCandidates);

    public class TransferInput
    {
        /// <summary>The other bank account of the company the money went to / came from.</summary>
        public int ToBankAccountId { get; set; }
        /// <summary>The matching line in the other account's statement, if already imported: both get reconciled with one journal.</summary>
        public int? CounterpartTransactionId { get; set; }
        [MaxLength(500)] public string? Narration { get; set; }
    }

    public class MatchInput
    {
        public int JournalEntryId { get; set; }
    }

    public class BulkReconcileItem
    {
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public TaxType TaxType { get; set; }
        [MaxLength(500)] public string? Description { get; set; }
    }

    public class BulkReconcileInput
    {
        [Required] public List<BulkReconcileItem> Items { get; set; } = new();
    }

    public record BulkReconcileResult(int TransactionId, bool Ok, string? Error);

    public class BankRuleInput
    {
        public int? BankAccountId { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        public int Priority { get; set; }
        public RuleMatchField MatchField { get; set; }
        public RuleMatchType MatchType { get; set; }
        [Required, MaxLength(200)] public string MatchText { get; set; } = string.Empty;
        public RuleDirection Direction { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? AmountMax { get; set; }
        public int AccountId { get; set; }
        public TaxType TaxType { get; set; }
        [MaxLength(500)] public string? LineDescription { get; set; }
        public bool AutoReconcile { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ReconcileLineInput
    {
        public int AccountId { get; set; }
        /// <summary>Gross amount (GST inclusive), always positive. Lines must add up to the transaction amount.</summary>
        public decimal Amount { get; set; }
        public TaxType TaxType { get; set; }
        [MaxLength(500)] public string? Description { get; set; }
    }

    public class ReconcileInput
    {
        [MaxLength(100)] public string? Reference { get; set; }
        [MaxLength(500)] public string? Narration { get; set; }
        [Required, MinLength(1)] public List<ReconcileLineInput> Lines { get; set; } = new();
    }
}
