namespace fptu_cup.Common
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, PagedQuery query) =>
            new() { Items = items, TotalCount = totalCount, Page = query.Page, PageSize = query.PageSize };
    }
}
