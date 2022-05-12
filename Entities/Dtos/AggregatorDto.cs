using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Dtos
{
    public class AggregatorDto : IDto
    {
        public int pageSize { get; set; }
        public int pageCount { get; set; }
    }
}
