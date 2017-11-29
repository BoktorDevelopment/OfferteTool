﻿using System;
using System.Collections.Generic;
using OffertTemplateTool.DAL.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OffertTemplateTool.Models
{
    public class OfferRenderViewModel
    {
        public FrontPageViewModel FrontPage { get; set; }
        public Page3ViewModel Page3 { get; set; }
        public ContentPages ContentPages { get; set; }
        public EstimateTablePage EstimateTablePage { get; set; }
    }

    public class Page3ViewModel
    {
        public string CustomerCompany { get; set; }
        public string CustomerName { get; set; }
        public string CustomerStreet { get; set; }
        public string CustomerZipCode { get; set; }
    }

    public class FrontPageViewModel
    {
        public string CustomerCompany { get; set; }
        public string CustomerName { get; set; }
        public string CustomerStreet { get; set; }
        public string CustomerZipCode { get; set; }
        public string CustomerCountry { get; set; }
        public string ProjectName { get; set; }
        public string LastUpdated { get; set; }
        public string ProductCode { get; set; }
        public string CreatedBy { get; set; }
    }
    public class IndexPage
    {
        public List<string> IndexItems { get; set; }
    }

    public class ContentPages
    {
        public List<string> Alineas { get; set; }
    }
    public class EstimateTablePage
    {
        public ICollection<EstimateConnects> EstimateConnects { get; set; }

        public string ExclBtw { get; set; }
        public string BTW { get; set; }
        public string Totaal { get; set; }
    }
}