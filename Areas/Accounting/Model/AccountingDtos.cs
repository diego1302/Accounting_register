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

    public record ImportResultDto(int? StatementId, int Imported, int Duplicates, DateOnly? FromDate, DateOnly? ToDate, List<Services.ParseError> Errors);

    public record JournalLineDto(int AccountId, string AccountCode, string AccountName, string? Description, decimal Debit, decimal Credit, TaxType? TaxType);

    public record JournalEntryDto(int Id, DateOnly Date, string? Reference, string Narration, JournalSource Source, List<JournalLineDto> Lines);

    public record SuggestionDto(int AccountId, TaxType TaxType);

    public record BankTransactionDto(
        int Id, int BankAccountId, int BankStatementId, DateOnly Date, string Description, string? Reference,
        decimal Amount, decimal? Balance, BankTransactionStatus Status, JournalEntryDto? JournalEntry, SuggestionDto? Suggestion);

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
