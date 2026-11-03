namespace fptu_cup.Common
{
    /// <summary>Tham số phân trang chung: ?page=&amp;pageSize=&amp;search=&amp;sortBy= (mặc định 10, tối đa 50).</summary>
    public class PagedQuery
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 50;

        private int _page = 1;
        private int _pageSize = DefaultPageSize;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
        }

        public string? Search { get; set; }
        public string? SortBy { get; set; }

        public int Skip => (Page - 1) * PageSize;
    }
}
