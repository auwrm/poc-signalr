using TTSS.Infrastructure.Data.Models;

namespace TTSS.Infrastructure.Data
{
    public class PagingResult<T>
    {
        private int pageSize;
        private Task<IEnumerable<T>> result;
        private readonly Func<int> fnTotalCount;

        private int? _totalCount;
        public int CurrentPage { get; }
        public int TotalCount => (_totalCount ?? (_totalCount = fnTotalCount())).Value;

        public PagingResult(Task<IEnumerable<T>> result, int pageSize, int currentPage, Func<int> fnTotalCount)
        {
            this.pageSize = pageSize;
            CurrentPage = currentPage;
            this.result = result;
            this.fnTotalCount = fnTotalCount;
        }

        public Task<IEnumerable<T>> GetDataAsync()
            => result;

        public async Task<PagingData<T>> ToPagingData()
        {
            var result = await this.result;
            return new PagingData<T>
            {
                PageSize = pageSize,
                CurrentPage = CurrentPage,
                Result = result,
                TotalCount = TotalCount,
                PageCount = PageCount,
                HasNextPage = HasNextPage,
                HasPreviousPage = HasPreviousPage,
                NextPage = NextPage,
                PreviousPage = PreviousPage,
            };
        }

        private bool hasComputeParameters = false;
        private void ComputeParameters()
        {
            if (hasComputeParameters)
            {
                return;
            }

            const int MinimumPage = 0;
            hasComputeParameters = true;
            var totalCount = TotalCount;
            pageCount = pageSize <= MinimumPage ? MinimumPage : ((totalCount / pageSize) + ((totalCount % pageSize > MinimumPage) ? 1 : MinimumPage));
            var lastPage = Math.Max(MinimumPage, pageCount - 1);
            hasNextPage = CurrentPage < pageCount - 1;
            nextPage = HasNextPage ? Math.Min(lastPage, Math.Max(-1, CurrentPage) + 1) : lastPage;
            hasPreviousPage = CurrentPage > MinimumPage;
            previousPage = HasPreviousPage ? Math.Max(MinimumPage, Math.Min(lastPage + 1, CurrentPage) - 1) : MinimumPage;
        }

        private int pageCount;
        private int nextPage;
        private int previousPage;
        private bool hasNextPage;
        private bool hasPreviousPage;

        public int PageCount { get { ComputeParameters(); return pageCount; } }
        public int NextPage { get { ComputeParameters(); return nextPage; } }
        public int PreviousPage { get { ComputeParameters(); return previousPage; } }
        public bool HasNextPage { get { ComputeParameters(); return hasNextPage; } }
        public bool HasPreviousPage { get { ComputeParameters(); return hasPreviousPage; } }
    }
}
