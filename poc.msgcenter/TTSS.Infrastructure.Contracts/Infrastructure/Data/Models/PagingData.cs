namespace TTSS.Infrastructure.Data.Models
{
    public class PagingData<T>
    {
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public IEnumerable<T>? Result { get; set; }
        public int TotalCount { get; set; }
        public int PageCount { get; set; }
        public int NextPage { get; set; }
        public int PreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
