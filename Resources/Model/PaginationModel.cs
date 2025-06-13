namespace Accounting_Register_BE.Resources.Model
{
    public class PaginationModel
    {
        public int count { get; set; }
        public int limit { get; set; }
        public int page { get; set; }
        

        public PaginationModel(int limit, int page, int count)
        {
            this.count = count;
            this.limit = limit;
            this.page = page;
        }
    }
}
