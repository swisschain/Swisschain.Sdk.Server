using System.ComponentModel;

namespace Swisschain.Sdk.Server.WebApi.Pagination
{
    public class Pagination<T>
    {
        public T Cursor { get; set; }
        public int Count { get; set; }
        public ListSortDirection Order { get; set; }
        public string NextUrl { get; set; }
    }
}
