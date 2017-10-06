﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OffertTemplateTool.DAL.Models;
using System.ComponentModel.DataAnnotations;

namespace OffertTemplateTool.Models
{
    public class OfferViewModel
    {
        public Guid Id { get; set; }
        public int DebtorNumber { get; set; } // voor WeFact API
        public int DocumentCode { get; set; } // ophalen vanuit WeFact
        [Required]
        public string ProjectName { get; set; }
        public Users CreatedBy { get; set; } // Welke user maakt 't aan
        public Users UpdatedBy { get; set; } // Laatst geedit door?                         
        public DateTime? CreatedAt { get; set; } // Gemaakt op
        public DateTime? LastUpdatedAt { get; set; } // Elke x updaten
        public string IndexContent { get; set; } // Html text veld
        public string Estimate { get; set; } // Html text veld
        public ICollection<EstimateLinesViewModel> EstimateLines { get; set; }
       
    }
}
