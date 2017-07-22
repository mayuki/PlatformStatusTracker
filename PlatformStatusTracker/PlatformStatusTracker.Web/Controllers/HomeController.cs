using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PlatformStatusTracker.Core.Configuration;
using PlatformStatusTracker.Core.Repository;
using PlatformStatusTracker.Core.Model;
using PlatformStatusTracker.Core.Enum;
using PlatformStatusTracker.Web.ViewModels.Home;

namespace PlatformStatusTracker.Web.Controllers
{
    public class HomeController : Controller
    {
        private IChangeSetRepository _changeSetRepository;
        private IStatusRawDataRepository _statusRawDataRepository;
        private ConnectionStringOptions _connectionString;

        public HomeController(IChangeSetRepository changeSetRepository, IStatusRawDataRepository statusRawDataRepository, IOptions<ConnectionStringOptions> connectionString)
        {
            _changeSetRepository = changeSetRepository;
            _statusRawDataRepository = statusRawDataRepository;
            _connectionString = connectionString.Value;
        }

        public async Task<IActionResult> Index()
        {
            return View(await HomeIndexViewModel.CreateAsync(_changeSetRepository));
        }

        public async Task<IActionResult> Feed(bool shouldFilterIncomplete = false)
        {
            var result = View(await HomeIndexViewModel.CreateAsync(_changeSetRepository));
            result.ContentType = "application/atom+xml";
            return result;
        }

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

        public async Task<IActionResult> UpdateStatus(String key)
        {
            if (String.IsNullOrWhiteSpace(_connectionString.UpdateKey) || key != _connectionString.UpdateKey)
            {
                return StatusCode(403);
            }

            await new DataUpdateAgent(_changeSetRepository, _statusRawDataRepository).UpdateAllAsync();

            return Content("OK", "text/plain");
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
