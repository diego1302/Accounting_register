namespace Accounting_Register_BE.Resources.Model
{
    /// <summary>Standard envelope for paginated list endpoints.</summary>
    public class PagedResult<T>
    {
        public List<T> items { get; set; } = new();
        public PaginationModel pagination { get; set; }

        public PagedResult(List<T> items, PaginationModel pagination)
        {
            this.items = items;
            this.pagination = pagination;
        }
    }
}
