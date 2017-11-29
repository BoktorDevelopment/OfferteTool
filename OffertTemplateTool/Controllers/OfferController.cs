﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OffertTemplateTool.Models;
using OffertTemplateTool.DAL.Models;
using OffertTemplateTool.DAL.Repositories;
using OffertTemplateTool.DAL;
using OffertTemplateTool.DAL.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using OffertTemplateTool.Connectors;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using DinkToPdf;
using OffertTemplateTool.TemplateSevice;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Hosting;

namespace OffertTemplateTool.Controllers
{
    [Authorize]
    public class OfferController : Controller
    {
        private OfferRepository OfferRepository { get; set; }
        private UsersRepository UserRepository { get; set; }
        private EstimateRepository EstimateRepository { get; set; }
        private EstimateLinesRepository EstimateLinesRepository { get; set; }
        private EstimateConnectsRepository EstimateConnectsRepository { get; set; }
        private SettingsRepository SettingsRepository { get; set; }
        internal WeFactConnector wefactconnector { get; set; }
        private readonly ITemplateService _templateservice;
        private readonly IHostingEnvironment _env;

        public OfferController(IRepository<Offers> offerrepository, IRepository<Users> userrepository, IRepository<Estimates> estimaterepository,
            IRepository<EstimateLines> estimatlinesrepository, IRepository<EstimateConnects> estimateconnectsrepository,
            IRepository<Settings> settingsrepository,
            IConnector wefactConnector,
            IHostingEnvironment env,
            ITemplateService templateservice)
        {
            OfferRepository = (OfferRepository)offerrepository;
            UserRepository = (UsersRepository)userrepository;
            EstimateRepository = (EstimateRepository)estimaterepository;
            EstimateLinesRepository = (EstimateLinesRepository)estimatlinesrepository;
            EstimateConnectsRepository = (EstimateConnectsRepository)estimateconnectsrepository;
            SettingsRepository = (SettingsRepository)settingsrepository;
            wefactconnector = (WeFactConnector)wefactConnector;
            _templateservice = templateservice;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.Name != null)
            {
                var offertes = await OfferRepository.GetAllAsync();
                ViewData["offertes"] = offertes.Select(x => new OfferViewModel
                {
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    LastUpdatedAt = x.LastUpdatedAt,
                    UpdatedBy = x.UpdatedBy,
                    ProjectName = x.ProjectName,
                    Id = x.Id
                }).ToList();

                List<string> templates = new List<string>();
                var files = Directory.GetFiles(@"wwwroot/OfferteTemplates/")
                    .Select(Path.GetFileName)
                    .ToArray();
                foreach (var item in files)
                {
                    var file = item.Replace(".docx", "");
                    if (file[0].ToString() != "~" && file[1].ToString() != "$")
                    {
                        templates.Add(file);
                    }
                }

                ViewData["templates"] = templates;

                return View();
            }
            else
            {
                return Redirect("~/Account/AccessDenied");
            }
        }

        [HttpPost]
        public async Task<IActionResult> OfferUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return View("File not selected");
            }
            else
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/OfferteTemplates",
                    file.FileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return View();
            }
        }

        [HttpGet]
        public IActionResult OfferUpload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewOffer(OfferViewModel model)
        {
            if (ModelState.IsValid)
            {
                Users user = UserRepository.FindUserByEmail(User.Identity.Name);
                //var customer = wefactconnector.GetCustomerInfo(model.DebtorNumber);
                var settingdocument = SettingsRepository.getSpecificSetting("DocumentCode");
                int documentcode = int.Parse(settingdocument.Value);
                var code = documentcode.ToString("000");

                var offerte = new Offers
                {
                    IndexContent = model.IndexContent,
                    ProjectName = model.ProjectName,
                    //DebtorNumber = model.DebtorNumber,
                    CreatedBy = user,
                    CreatedAt = DateTime.Now,
                    LastUpdatedAt = DateTime.Now,
                    UpdatedBy = user,
                    DocumentCode = "PV" + code
                };

                await OfferRepository.AddAsync(offerte);

                documentcode++;
                settingdocument.Value = documentcode.ToString();
                await SettingsRepository.SaveChangesAsync();

                var est = new Estimates
                {

                };

                await EstimateRepository.AddAsync(est);

                foreach (var line in model.EstimateLines)
                {
                    var lin = new EstimateLines
                    {
                        HourCost = line.HourCost,
                        Hours = line.Hours,
                        Specification = line.Specification,
                        TotalCost = line.TotalCost
                    };

                    var connect = new EstimateConnects
                    {
                        Estimate = est,
                        EstimateLines = lin
                    };

                    await EstimateLinesRepository.AddAsync(lin);
                    await EstimateConnectsRepository.AddAsync(connect);
                }

                offerte.Estimate = est;
                await OfferRepository.UpdateAsync(offerte);

                return Ok();
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        [HttpGet]
        public IActionResult NewOffer()
        {
            OfferViewModel model = new OfferViewModel
            {

            };
            //var debtorinfo = wefactconnector.GetCustomers();
            //ViewData["debtors"] = debtorinfo;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditOffer(OfferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("offermodel", "Form is not valid!");
                return View(model);
            }
            try
            {
                ICollection<EstimateConnects> Connect;
                ICollection<Offers> offers;

                using (var context = new DataBaseContext())
                {
                    offers = context.Offer
                   .Include(offermodel => offermodel.Estimate)
                   .Include(users => users.CreatedBy)
                   .Include(users => users.UpdatedBy)
                    .ToList();

                    Connect = context.EstimateConnects
                    .Include(line => line.EstimateLines)
                    .ToList();
                }

                Offers offer = await OfferRepository.FindAsync((System.Guid)model.Id);
                var dboffer = offers.FirstOrDefault(x => x.Id.Equals(model.Id));
                Users user = UserRepository.FindUserByEmail(User.Identity.Name);
                var estimate = await EstimateRepository.FindAsync(dboffer.Estimate.Id);
                var connectlines = Connect.Where(x => x.Estimate.Id.Equals(dboffer.Estimate.Id)).ToList();
                if (model.EstimateLines != null)
                {
                    foreach (var item in model.EstimateLines)
                    {
                        if (EstimateLinesRepository.AnyLineExist(item.Id) == true)
                        {
                            var line = await EstimateLinesRepository.FindAsync(item.Id);
                            line.Specification = item.Specification;
                            line.Hours = item.Hours;
                            line.HourCost = item.HourCost;
                            line.TotalCost = item.TotalCost;
                            await EstimateLinesRepository.SaveChangesAsync();
                        }
                        else
                        {
                            var newline = new EstimateLines
                            {
                                Specification = item.Specification,
                                Hours = item.Hours,
                                HourCost = item.HourCost,
                                TotalCost = item.TotalCost
                            };
                            await EstimateLinesRepository.AddAsync(newline);

                            var newconnect = new EstimateConnects
                            {
                                Estimate = estimate,
                                EstimateLines = newline
                            };
                            await EstimateConnectsRepository.AddAsync(newconnect);
                        }
                    }
                    foreach (var connectline in connectlines)
                    {
                        if (!model.EstimateLines.Any(x => x.Id == connectline.EstimateLines.Id))
                        {
                            await EstimateConnectsRepository.RemoveAsync(connectline.Id);
                        }
                    }
                }

                offer.IndexContent = model.IndexContent;
                offer.LastUpdatedAt = DateTime.Now;
                offer.UpdatedBy = user;
                offer.ProjectName = model.ProjectName;
                //offer.DebtorNumber = model.DebtorNumber;

                await OfferRepository.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ICollection<Offers> offers;
                ICollection<EstimateConnects> Connect;
                using (var context = new DataBaseContext())
                {
                    offers = context.Offer
                       .Include(offermodel => offermodel.Estimate)
                       .Include(user => user.CreatedBy)
                       .Include(user => user.UpdatedBy)
                        .ToList();

                    Connect = context.EstimateConnects
                        .Include(line => line.EstimateLines)
                        .ToList();
                }

                var offer = offers.FirstOrDefault(x => x.Id.Equals(model.Id));
                var estimate = await EstimateRepository.FindAsync(offer.Estimate.Id.ToString());
                var lines = Connect.Where(x => x.Estimate.Id == estimate.Id).ToList();

                ViewData["estimatelines"] = lines.Select(x => new EstimateConnectViewModel
                {
                    EstimateLines = x.EstimateLines
                }).ToList();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditOffer(System.Guid Id)
        {
            ICollection<Offers> offers;
            ICollection<EstimateConnects> Connect;
            using (var context = new DataBaseContext())
            {
                offers = context.Offer
                   .Include(offermodel => offermodel.Estimate)
                   .Include(user => user.CreatedBy)
                   .Include(user => user.UpdatedBy)
                    .ToList();

                Connect = context.EstimateConnects
                    .Include(line => line.EstimateLines)
                    .ToList();
            }

            var offer = offers.FirstOrDefault(x => x.Id.Equals(Id));
            var estimate = await EstimateRepository.FindAsync(offer.Estimate.Id.ToString());
            var lines = Connect.Where(x => x.Estimate.Id == estimate.Id).ToList();

            ViewData["estimatelines"] = lines.Select(x => new EstimateConnectViewModel
            {
                EstimateLines = x.EstimateLines
            }).ToList();

            OfferViewModel offerte = new OfferViewModel //get selected offer info
            {
                IndexContent = offer.IndexContent,
                ProjectName = offer.ProjectName,
                Estimate = offer.Estimate.Id,
                Id = offer.Id,
                //DebtorNumber = offer.DebtorNumber
            };

            //var debtorinfo = wefactconnector.GetCustomers(); //get debtors off wefact
            //ViewData["debtors"] = debtorinfo;
            return View(offerte);
        }

        public async Task<IActionResult> ExportOffer(Guid Id , string template)
        {
            var viewmodelrender = fillViewModel(Id);
            OfferRenderViewModel render = await viewmodelrender;
            var _converter = new BasicConverter(new PdfTools());
            string documentcontent = await _templateservice.RenderTemplateAsync<OfferRenderViewModel>("Template/NewHeapTemplate", render);
            var output = _converter.Convert(new HtmlToPdfDocument()
            {
                Objects =
                {
                    new ObjectSettings()
                    {
                        HtmlContent = documentcontent
                    }
                }
            });

            Response.Clear();
            Response.ContentType = "Application/pdf";
            Response.Headers.Add("Content-Disposition", string.Format("Attachment;FileName=OfferTEst.pdf;"));
            Response.Headers.Add("Content-Length", output.Length.ToString());
            await Response.Body.WriteAsync(output, 0, output.Length);
            Response.Clear();
            return Redirect(nameof(Index));
        }

        public async Task<IActionResult> DeleteOffer(System.Guid Id)
        {
            ICollection<Offers> offers;
            ICollection<EstimateConnects> Connect;
            using (var context = new DataBaseContext())
            {
                offers = context.Offer
                   .Include(offermodel => offermodel.Estimate)
                   .Include(user => user.CreatedBy)
                   .Include(user => user.UpdatedBy)
                    .ToList();

                Connect = context.EstimateConnects
                    .Include(line => line.EstimateLines)
                    .ToList();
            }
            var offer = offers.FirstOrDefault(x => x.Id.Equals(Id));
            var lines = Connect.Where(x => x.Estimate.Id == offer.Estimate.Id).ToList();

            foreach (var item in lines)
            {
                await EstimateConnectsRepository.RemoveAsync(item.Id);
            }

            await OfferRepository.RemoveAsync(Id);
            await EstimateRepository.RemoveAsync(offer.Estimate.Id);

            return Redirect("../");
        }

        public async Task<OfferRenderViewModel> fillViewModel(Guid Id)
        {
            ICollection<EstimateConnects> Connect;
            ICollection<Offers> offers;

            using (var context = new DataBaseContext())
            {
                offers = context.Offer
               .Include(offermodel => offermodel.Estimate)
               .Include(users => users.CreatedBy)
               .Include(users => users.UpdatedBy)
                .ToList();

                Connect = context.EstimateConnects
                .Include(line => line.EstimateLines)
                .ToList();
            }

            Offers offer = await OfferRepository.FindAsync(Id);
            var dboffer = offers.FirstOrDefault(x => x.Id.Equals(Id));
            var estimate = await EstimateRepository.FindAsync(dboffer.Estimate.Id);
            var connectlines = Connect.Where(x => x.Estimate.Id.Equals(dboffer.Estimate.Id)).ToList();

            var Page3ViewModel = new Page3ViewModel
            {
                CustomerCompany = "",
                CustomerName = "",
                CustomerStreet = "",
                CustomerZipCode = ""
            };

            var FrontPageViewModel = new FrontPageViewModel
            {
                CreatedBy = dboffer.CreatedBy.ToString(),
                LastUpdated = offer.LastUpdatedAt.ToString(),
                ProjectName = offer.ProjectName,
                ProductCode = offer.DocumentCode,
                CustomerZipCode = "",
                CustomerStreet = "",
                CustomerName = "",
                CustomerCompany = "",
                CustomerCountry = ""

            };

            var EstimateTablePage = new EstimateTablePage
            {
                EstimateConnects = connectlines,
                BTW = "21.000,00",
                ExclBtw = "100.000,00",
                Totaal = "121.000,00"
            };

            var ContentPages = new ContentPages
            {
                Alineas = null
            };

            var OfferRenderViewModel = new OfferRenderViewModel
            {
                ContentPages = ContentPages,
                EstimateTablePage = EstimateTablePage,
                FrontPage = FrontPageViewModel,
                Page3 = Page3ViewModel
            };

            return OfferRenderViewModel;
        }
    }
}