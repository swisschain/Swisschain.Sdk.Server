using System.Collections.Generic;

namespace Swisschain.Sdk.Server.WebApi.Pagination
{
    public class Paginated<TItem, TId>
    {
        public Pagination<TId> Pagination { get; set; }
        public IReadOnlyCollection<TItem> Items { get; set; }
    }
}
