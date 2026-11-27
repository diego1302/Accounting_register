namespace Accounting_Register_BE.Areas.Accounting.Model
{
    /// <summary>
    /// Amounts in reports use each account's natural sign: debit-normal accounts (assets, costs, expenses)
    /// are positive when debit &gt; credit; credit-normal accounts (liabilities, equity, income) when credit &gt; debit.
    /// </summary>
    public record TrialBalanceRow(int AccountId, string Code, string Name, AccountType Type, decimal Debit, decimal Credit);

    public record TrialBalanceDto(DateOnly AsOf, List<TrialBalanceRow> Rows, decimal TotalDebit, decimal TotalCredit);

    /// <summary>AccountId is null for computed rows (e.g. current year earnings).</summary>
    public record ReportLine(int? AccountId, string? Code, string Name, decimal Amount);

    public record ReportSection(string Key, string Title, List<ReportLine> Lines, decimal Total);

    public record ProfitAndLossDto(
        DateOnly From, DateOnly To,
        ReportSection Income, ReportSection CostOfSales, decimal GrossProfit,
        ReportSection OtherIncome, ReportSection Expenses, decimal NetProfit);

    public record BalanceSheetDto(
        DateOnly AsOf, DateOnly FinancialYearStart,
        List<ReportSection> Assets, decimal TotalAssets,
        List<ReportSection> Liabilities, decimal TotalLiabilities,
        decimal NetAssets,
        ReportSection Equity, decimal TotalEquity);

    public record LedgerLine(int JournalEntryId, DateOnly Date, string? Reference, string Narration, string? Description, JournalSource Source, decimal Debit, decimal Credit, decimal Balance);

    public record LedgerAccount(int AccountId, string Code, string Name, AccountType Type, decimal Opening, List<LedgerLine> Lines, decimal TotalDebit, decimal TotalCredit, decimal Closing);

    public record GeneralLedgerDto(DateOnly From, DateOnly To, List<LedgerAccount> Accounts, bool Truncated);
}
