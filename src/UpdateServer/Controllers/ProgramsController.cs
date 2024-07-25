
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
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
        private static readonly Logger _downloadLogger = NLog.LogManager.GetLogger("FileDownloadLogger");
        private static readonly Logger _updaterLogger = NLog.LogManager.GetLogger("UpdateDownloadLogger");

        public ProgramsController(ILogger<ProgramsController> logger, IConfiguration configuration, VersionController versionController)
        {
			this._logger = logger;
            this._configuration = configuration;
            this._versionController = versionController;
        }

        public IActionResult Index()
        {
            _logger.LogInformation($"Ip {Request?.HttpContext?.Connection?.RemoteIpAddress} getting programs List");

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
            _logger.LogInformation($"Ip {Request?.HttpContext?.Connection?.RemoteIpAddress} getting programs files for program: {program}");

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
            _logger.LogInformation($"Ip {Request?.HttpContext?.Connection?.RemoteIpAddress} deleting {program}");

            var result = await Task.Run(() => _versionController
                .DeleteProgram(new(_configuration["login"] ?? "", _configuration["password"] ?? ""), program));
            if (result is OkResult)
                return RedirectToAction("Index", "Programs");
            return BadRequest();
        }

        [Authorize]
        public async Task<IActionResult> DeleteVersion(string program, string version)
        {
            _logger.LogInformation($"Ip {Request?.HttpContext?.Connection?.RemoteIpAddress} deleted program version: {program}/{version}");

            /// TODO добавить удаление папке при удалении всех версий
            var result = await Task.Run(() => _versionController
                 .DeleteVersion(new(_configuration["login"] ?? "", _configuration["password"] ?? ""), program, version)
            );
            if (result is OkResult)
                return RedirectToAction("Versions", "Programs", new { program }); 
            return BadRequest();
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

            _logger.LogInformation($"User {Request?.HttpContext?.Connection?.RemoteIpAddress} uploading new version program:{newVersionData.Program}, version: {newVersionData.Version}");

            var result = await _versionController
                    .Upload(new(_configuration["login"] ?? "", _configuration["password"] ?? ""), newVersionData);
            if (result is OkObjectResult)
                return RedirectToAction("Version", "Programs", new { newVersionData.Program });

            return BadRequest((result as ObjectResult)?.Value);         
        }


	}
}
