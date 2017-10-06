﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OffertTemplateTool.DAL.Models
{
    public class Estimates : IDb
    {
        public Guid Id { get; set; }
        public virtual ICollection<EstimateLines> Products { get; set; }

    }
}
