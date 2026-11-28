using System.ComponentModel.DataAnnotations;

namespace Accounting_Register_BE.Areas.Accounting.Model
{
    /// <summary>One debit or credit line. Identify the account by <see cref="AccountCode"/> (e.g. "200") or <see cref="AccountId"/>.</summary>
    public class JournalLineInput
    {
        public int? AccountId { get; set; }
        [MaxLength(20)] public string? AccountCode { get; set; }
        [MaxLength(500)] public string? Description { get; set; }
        /// <summary>Exactly one of Debit / Credit must be greater than zero (max. 2 decimals).</summary>
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        /// <summary>Informational tax treatment; GST must be posted as its own line (account 820 or the company's GST account).</summary>
        public TaxType? TaxType { get; set; }
    }

    public class JournalEntryInput
    {
        public DateOnly Date { get; set; }
        [MaxLength(100)] public string? Reference { get; set; }
        [Required, MaxLength(500)] public string Narration { get; set; } = string.Empty;
        /// <summary>Sending system (e.g. "pos"). Defaults to the API key name.</summary>
        [MaxLength(50)] public string? ExternalSource { get; set; }
        /// <summary>Sender's own id. Posting the same ExternalSource + ExternalId again returns the existing entry instead of duplicating it.</summary>
        [MaxLength(100)] public string? ExternalId { get; set; }
        [Required] public List<JournalLineInput> Lines { get; set; } = new();
    }

    public class JournalBatchInput
    {
        [Required] public List<JournalEntryInput> Entries { get; set; } = new();
    }

    public record JournalEntryDetailDto(
        int Id, DateOnly Date, string? Reference, string Narration, JournalSource Source,
        string? ExternalSource, string? ExternalId, DateTime CreatedAt,
        decimal TotalDebit, decimal TotalCredit, List<JournalLineDto> Lines);

    /// <summary>Status: "created" or "existing" (idempotent replay of an ExternalId already posted).</summary>
    public record JournalBatchItemResult(int Index, string Status, JournalEntryDetailDto Entry);

    public record JournalBatchResult(int Created, int Existing, List<JournalBatchItemResult> Results);

    public class ApiKeyInput
    {
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    }

    /// <summary>Returned once, on creation: the only time the plain key is visible.</summary>
    public record ApiKeyCreatedDto(int Id, string Name, string Prefix, DateTime CreatedAt, string Key);
}
