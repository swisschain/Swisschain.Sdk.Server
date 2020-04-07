namespace Swisschain.Sdk.Server.WebApi.Pagination
{
    public class Pagination<T>
    {
        public T Cursor { get; set; }
        public int Count { get; set; }
        public PaginationOrder Order { get; set; }
        public string NextUrl { get; set; }
    }
}
