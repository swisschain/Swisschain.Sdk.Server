using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Swisschain.Sdk.Server.WebApi.Pagination
{
    public class PaginationRequest<T>
    {
        [FromQuery(Name = "order")]
        public PaginationOrder Order { get; set; }

        [FromQuery(Name = "cursor")]
        public T Cursor { get; set; }

        [FromQuery(Name = "limit")]
        public int Limit { get; set; } = 50;
    }
}
