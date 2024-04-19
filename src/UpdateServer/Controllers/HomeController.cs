using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Security.Claims;
using UpdateServer.Model;
using UpdateServer.ViewModel;

namespace UpdateServer.Controllers
{
    /// <summary>
    /// Home page controllers
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            this._logger = logger;
            this._configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Login page
        /// </summary>
        /// <param name="returnUrl">url to redirect</param>
        /// <param name="authorizationData">login and password</param>
        /// <returns></returns>
        public async Task<IActionResult> Login(string? returnUrl, LoginVewModel authorizationData)
        {
            if (authorizationData.Login == _configuration["login"]
                && authorizationData.Password == _configuration["password"])
            {
                var claims = new List<Claim>
                { new (ClaimTypes.Name, authorizationData.Login) };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claimsPrincipal);
                return Redirect(returnUrl ?? "/Home");
            }
            return View();
        }


        public async Task<IActionResult> LogOut(string? returnUrl)
        {
            await HttpContext.SignOutAsync();
            return Redirect(returnUrl ?? "/Home");
        }

        public IActionResult Privacy()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
