﻿
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpdateServer.Models;

namespace UpdateServer.Controllers
{
    /// <summary>
    /// Program list controller
    /// </summary>
    public class ProgramsController : Controller
    {
        private readonly ILogger<ProgramsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly VersionController _versionController;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProgramsController(ILogger<ProgramsController> logger, IConfiguration configuration, VersionController versionController, IHttpContextAccessor httpContextAccessor)
        {
            this._logger = logger;
            this._configuration = configuration;
            this._versionController = versionController;
            this._httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Index()
        {
            var programs = _versionController.GetPrograms();
            /// TODO проверить не надо ли поменять на OkResult
            if (programs.Result is OkObjectResult okResult)
                return View(okResult.Value);
            return BadRequest();
        }

        /// <summary>
        /// Versions page
        /// </summary>
        public IActionResult Versions(string program)
        {
            var programVersions = _versionController.GetVersions(program);
            if (programVersions.Result is OkObjectResult okResult)
                return View(okResult.Value);
            return BadRequest();
        }

        [Authorize]
        public IActionResult Upload()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> DeleteProgram(string program)
        {
            var result = await Task.Run(() => _versionController
                .DeleteProgram(new(_configuration["login"] ?? "", _configuration["password"] ?? ""), program));
            if (result is OkResult)
                return RedirectToAction("Index", "Programs");
            return BadRequest();
        }

        [Authorize]
        public async Task<IActionResult> DeleteVersion(string program, string version)
        {
            /// TODO добавить удаление папке при удалении всех версий
            var result = await Task.Run(() => _versionController
                 .DeleteVersion(new(_configuration["login"] ?? "", _configuration["password"] ?? ""), program, version)
            );
            if (result is OkResult)
                return RedirectToAction("Versions", "Programs", new { program });
            return BadRequest();
        }

        [Authorize]
        public async Task<IActionResult> Logs()
        {
            var test =  Directory.GetDirectories(".");
            if (!System.IO.Directory.Exists("logs")) BadRequest();
            var logFiles = Directory.GetFiles("logs");
            return View(logFiles.Select(fn => Path.GetFileName(fn)).ToArray());
        }

        [Authorize]
        public async Task<IActionResult> Log(string logName)
        {
            if (!System.IO.File.Exists($"logs/{logName}")) BadRequest();
            var log = System.IO.File.ReadLines($"logs/{logName}").ToArray();
            ViewBag.LogName = logName;  
            return View(log);
        }

        /// <summary>
        /// Upload new version
        /// </summary>
        /// <param name="newVersionData">New version info</param>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Upload(NewVersionData newVersionData)
        {
            if (newVersionData is null) return BadRequest("Data is incorrect");

            var result = await _versionController
                    .Upload(new(_configuration["login"] ?? "", _configuration["password"] ?? ""), newVersionData);
            if (result is OkObjectResult)
                return RedirectToAction("Version", "Programs", new { newVersionData.Program });

            return BadRequest((result as ObjectResult)?.Value);
        }

    }
}
