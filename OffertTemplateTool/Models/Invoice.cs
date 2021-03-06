﻿using Newtonsoft.Json;
using System.Collections.Generic;
using OffertTemplateTool.Models.wefactModels;

namespace OffertTemplateTool.Models
{
    public class Invoice
    {
        public Invoice()
        {

        }
        public string api_key { get; set; }
        public string controller { get; set; }
        public string action { get; set; }
        public string InvoiceCode { get; set; }
        public string Debtor { get; set; }
        [JsonProperty(PropertyName = "Concept")]
        public int Concept { get; set; }
        public int Identifier { get; set; }
        public List<Dictionary<string, string>> InvoiceLines { get; set; }
    }
}
