using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PlatformStatusTracker.Core.Repository;
using PlatformStatusTracker.Web.ViewModels.Home;
using PlatformStatusTracker.Web.Filters;

namespace PlatformStatusTracker.Web.Controllers
{
    public class HomeController : Controller
    {
        private IChangeSetRepository _changeSetRepository;

        public HomeController(IChangeSetRepository changeSetRepository)
        {
            _changeSetRepository = changeSetRepository;
        }

        [StaleWhileRevalidate(CacheProfileName = "DefaultCache", StaleWhileRevalidateDuration = 60 * 60)]
        public async Task<IActionResult> Index()
        {
            return View(await HomeIndexViewModel.CreateAsync(_changeSetRepository));
        }

        [StaleWhileRevalidate(CacheProfileName = "DefaultCache", StaleWhileRevalidateDuration = 60 * 60)]
        public async Task<IActionResult> Feed(bool shouldFilterIncomplete = true)
        {
            var result = View(await HomeIndexViewModel.CreateAsync(_changeSetRepository, shouldFilterIncomplete));
            result.ContentType = "application/atom+xml";
            return result;
        }

        [StaleWhileRevalidate(CacheProfileName = "DefaultCache", StaleWhileRevalidateDuration = 60 * 60)]
        public async Task<IActionResult> Changes(String date)
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

            var viewModel = await ChangesViewModel.CreateAsync(_changeSetRepository, dateTime);
            return View(viewModel);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
