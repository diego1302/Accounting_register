using Accounting_Register_BE.Areas.Accounting.Model;

namespace Accounting_Register_BE.Areas.Accounting.Services
{
    /// <summary>Starter chart of accounts for an Australian small business (Xero-style codes).</summary>
    public static class DefaultChartOfAccounts
    {
        public const string GstSystemKey = "GST";

        public static readonly IReadOnlyList<(string Code, string Name, AccountType Type, TaxType Tax, string? SystemKey)> Accounts = new[]
        {
            ("200", "Ventas", AccountType.Revenue, TaxType.GST, (string?)null),
            ("260", "Otros ingresos", AccountType.Revenue, TaxType.GST, null),
            ("270", "Intereses ganados", AccountType.OtherIncome, TaxType.GSTFree, null),
            ("310", "Costo de ventas", AccountType.DirectCosts, TaxType.GST, null),
            ("400", "Publicidad", AccountType.Expense, TaxType.GST, null),
            ("404", "Comisiones bancarias", AccountType.Expense, TaxType.GSTFree, null),
            ("408", "Limpieza", AccountType.Expense, TaxType.GST, null),
            ("412", "Honorarios contables y consultoría", AccountType.Expense, TaxType.GST, null),
            ("416", "Depreciación", AccountType.Expense, TaxType.BASExcluded, null),
            ("420", "Representación", AccountType.Expense, TaxType.GST, null),
            ("425", "Fletes y mensajería", AccountType.Expense, TaxType.GST, null),
            ("429", "Gastos generales", AccountType.Expense, TaxType.GST, null),
            ("433", "Seguros", AccountType.Expense, TaxType.GST, null),
            ("437", "Intereses pagados", AccountType.Expense, TaxType.GSTFree, null),
            ("441", "Gastos legales", AccountType.Expense, TaxType.GST, null),
            ("445", "Electricidad y gas", AccountType.Expense, TaxType.GST, null),
            ("449", "Vehículos", AccountType.Expense, TaxType.GST, null),
            ("453", "Gastos de oficina", AccountType.Expense, TaxType.GST, null),
            ("461", "Imprenta y papelería", AccountType.Expense, TaxType.GST, null),
            ("469", "Arriendo", AccountType.Expense, TaxType.GST, null),
            ("473", "Reparaciones y mantenimiento", AccountType.Expense, TaxType.GST, null),
            ("477", "Sueldos y salarios", AccountType.Expense, TaxType.BASExcluded, null),
            ("478", "Superannuation", AccountType.Expense, TaxType.BASExcluded, null),
            ("485", "Suscripciones", AccountType.Expense, TaxType.GST, null),
            ("489", "Teléfono e internet", AccountType.Expense, TaxType.GST, null),
            ("493", "Viajes nacionales", AccountType.Expense, TaxType.GST, null),
            ("494", "Viajes internacionales", AccountType.Expense, TaxType.GSTFree, null),
            ("610", "Cuentas por cobrar", AccountType.CurrentAsset, TaxType.BASExcluded, null),
            ("620", "Gastos pagados por anticipado", AccountType.CurrentAsset, TaxType.BASExcluded, null),
            ("710", "Equipos de oficina", AccountType.NonCurrentAsset, TaxType.GST, null),
            ("720", "Equipos computacionales", AccountType.NonCurrentAsset, TaxType.GST, null),
            ("800", "Cuentas por pagar", AccountType.CurrentLiability, TaxType.BASExcluded, null),
            ("820", "GST", AccountType.CurrentLiability, TaxType.BASExcluded, GstSystemKey),
            ("825", "Retenciones PAYG por pagar", AccountType.CurrentLiability, TaxType.BASExcluded, null),
            ("826", "Superannuation por pagar", AccountType.CurrentLiability, TaxType.BASExcluded, null),
            ("880", "Retiros del dueño", AccountType.Equity, TaxType.BASExcluded, null),
            ("881", "Aportes del dueño", AccountType.Equity, TaxType.BASExcluded, null),
            ("960", "Resultados acumulados", AccountType.Equity, TaxType.BASExcluded, null),
            ("970", "Capital", AccountType.Equity, TaxType.BASExcluded, null),
        };
    }
}
