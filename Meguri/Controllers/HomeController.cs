using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Meguri.Models;

namespace Meguri.Controllers {
    public class HomeController : Controller {
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;

        public HomeController(IStringLocalizer<SharedResource> sharedLocalizer) {
            _sharedLocalizer = sharedLocalizer;
        }

        public IActionResult Index() {
            return View();
        }

        public IActionResult About() {
            ViewData["Message"] = _sharedLocalizer["Controller_About_Message"];

            return View();
        }

        public IActionResult Contact() {
            ViewData["Message"] = _sharedLocalizer["Controller_Contact_Message"];

            return View();
        }

        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
