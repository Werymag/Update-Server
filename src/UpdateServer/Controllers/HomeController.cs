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
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly VersionController _versionController;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, VersionController versionController)
        {
            this._logger = logger;
            this._configuration = configuration;
            this._versionController = versionController;
        }

        public IActionResult Index()
        {
            return View();
        }


        ///// <summary>
        ///// Upload new version files
        ///// </summary>
        ///// <param name="sourceFile">Archive with program files</param>
        ///// <param name="installFile">Install file</param>
        ///// <param name="changelog">List of changes</param>
        ///// <param name="loginDetails">Login and password</param>
        ///// <param name="uploadFileInfo">Information about version</param>
        ///// <returns></returns>
        //[HttpPost("PostVersion")]
        //[RequestSizeLimit(4294967295)]
        //public IActionResult Upload([FromForm] LoginDetails loginDetails,
        // [FromForm] UploadFileInfo uploadFileInfo,
        // IFormFile sourceFile, IFormFile installFile, IFormFile changelog)
        //{
        //    var programs = _versionController.Upload(loginDetails, uploadFileInfo, sourceFile, installFile, changelog);

        //    if (programs.Result is OkObjectResult)
        //        return RedirectToAction("Index", "Programs", new { program = uploadFileInfo.ProgramName });
        //    return BadRequest();
        //}


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
