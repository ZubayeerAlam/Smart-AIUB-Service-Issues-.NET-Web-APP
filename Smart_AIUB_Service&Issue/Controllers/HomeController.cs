using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_AIUB_Service_Issue.Models;
using System.Diagnostics;

namespace Smart_AIUB_Service_Issue.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Logged-in users go straight to their own dashboard; visitors see the welcome page.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Shown for unhandled exceptions (outside Development). The exception itself is already
        /// logged by the exception-handler middleware; the user only sees the request id.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Friendly page for 400 / 403 / 404 / ... (see UseStatusCodePagesWithReExecute).
        /// The page is re-run with the original request's method, so for a rejected POST it is
        /// itself a POST. It only displays text and changes nothing, so it is the one action
        /// allowed to skip the anti-forgery check (otherwise a CSRF rejection would show a blank page).
        /// </summary>
        [IgnoreAntiforgeryToken]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult HttpError(int id)
        {
            ViewData["Code"] = id;
            return View();
        }
    }
}
