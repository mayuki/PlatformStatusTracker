using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.ServiceLocation;
using PlatformStatusTracker.Core.Model;
using PlatformStatusTracker.Core.Repository;
using PlatformStatusTracker.Web.Infrastracture;
using PlatformStatusTracker.Web.ViewModels.Home;

namespace PlatformStatusTracker.Web.Controllers
{
    public class HomeController : BaseController
    {
        [OutputCache(CacheProfile = "Home_IndexAndFeed")]
        public async Task<ActionResult> Index()
        {
            var viewModel = await HomeIndexViewModel.CreateAsync(ServiceLocator.GetInstance<IChangeSetRepository>());

            return View(viewModel);
        }

        [OverrideContentType("application/atom+xml")]
        [Route("Feed")]
        [OutputCache(CacheProfile = "Home_IndexAndFeed")]
        public async Task<ActionResult> Feed()
        {
            var viewModel = await HomeIndexViewModel.CreateAsync(ServiceLocator.GetInstance<IChangeSetRepository>());

            return View(viewModel);
        }

        [Route(@"Changes/{date:regex(^\d{4}-\d{1,2}-\d{1,2})}")]
        [OutputCache(CacheProfile = "Home_Changes")]
        public async Task<ActionResult> Changes(String date)
        {
            if (String.IsNullOrWhiteSpace(date))
            {
                return RedirectToAction("Index");
            }

            DateTime dateTime;
            if (!DateTime.TryParse(date, out dateTime))
            {
                return RedirectToAction("Index");
            }

            var viewModel = await ChangesViewModel.CreateAsync(ServiceLocator.GetInstance<IChangeSetRepository>(), dateTime);
            return View(viewModel);
        }

        public async Task<ActionResult> UpdateStatus(String key)
        {
            var appKey = ConfigurationManager.AppSettings["PlatformStatusTracker:UpdateKey"];
            if (String.IsNullOrWhiteSpace(appKey) || key != appKey)
            {
                return new HttpStatusCodeResult(403);
            }

            await Using<DataUpdateAgent>().UpdateAllAsync();

            return Content("OK", "text/plain");
        }
    }
}