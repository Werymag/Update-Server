﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UpdateServer.Models;
using UpdateServer.ViewModel;
using FileVersionInfo = UpdateServer.Models.FileVersionInfo;

namespace UpdateServer.Controllers
{
    /// <summary>
    /// Api контроллер для работы с файлами версий программ
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class VersionController : Controller
    {
        private readonly ILogger<VersionController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static readonly Logger _downloadLogger = NLog.LogManager.GetLogger("FileDownloadLogger");
        private static readonly Logger _updaterLogger = NLog.LogManager.GetLogger("UpdateDownloadLogger");
        private IPAddress? _iPAddress = null;
        public VersionController(ILogger<VersionController> logger, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {

            this._logger = logger;
            this._configuration = configuration;
            this._httpContextAccessor = httpContextAccessor;
        }


        /// <summary>
        /// Get a list of available programs
        /// </summary>
        [HttpGet("GetPrograms")]
        public ActionResult<List<ProgramInfo>> GetPrograms()
        {
            _logger.LogInformation($"Client {GetClientData()} getting programs List");

            try
            {
                // The list of programs corresponding to the list of directories in the Programs folder
                var directoryInfo = Directory.CreateDirectory($"programs");
                var programs = directoryInfo.GetDirectories().ToArray();

                var programInforms = new List<ProgramInfo>();
                foreach (var program in programs)
                {
                    var versions = Directory
                        .GetDirectories(program.FullName, "*.*")
                        .Select(d => new Version(new DirectoryInfo(d).Name))
                        .Order().ToList();

                    if (versions.Count == 0) continue;
                    var actualVersion = versions.Last().ToString(4);
                    programInforms.Add(new(program.Name, actualVersion));
                }

                return Ok(programInforms.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Problem(e.Message);
            }
        }

        /// <summary>
        /// Get a list of available version of program
        /// </summary>
        [HttpGet("GetVersions")]
        public ActionResult<VersionViewModel> GetVersions(string program)
        {
            _logger.LogInformation($"Client {GetClientData()} getting all versions for program: {program}");

            try
            {
                // The list of programs corresponding to the list of directories in the programs folder
                if (!Path.Exists($"programs/{program}")) return BadRequest();

                var versions = Directory
                     .GetDirectories($"programs/{program}", "*.*")
                     .OrderBy(d => new Version(new DirectoryInfo(d).Name));

                var versionViewModel = new VersionViewModel(program);

                foreach (var version in versions)
                {
                    var changeLogFilePath = $"{version}/Changelog.txt";
                    var changelog = System.IO.File.Exists(changeLogFilePath) ? System.IO.File.ReadAllText(changeLogFilePath, Encoding.Default) : "";

                    var installFile = new ProgramVersionInfo(new DirectoryInfo(version).Name, changelog);
                    versionViewModel.Versions.Add(installFile);
                }

                return Ok(versionViewModel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Problem(e.Message);
            }
        }

        /// <summary>
        /// Actual Version
        /// </summary>
        /// <param name="program">Program Name</param>
        /// <returns></returns>
        [HttpGet("GetActualVersion")]
        public ActionResult<string> GetActualVersionInfo(string program)
        {
            var clientData = GetClientData(); //If direct request
            _logger.LogInformation($"Client {clientData} getting actual version for program: {program}");
            _updaterLogger.Info($"Client {clientData} getting actual version for program: {program}");

            try
            {
                if (!Path.Exists($"programs/{program}")) return BadRequest("Program not found");
                var actualVersion = Directory
                        .GetDirectories($"programs/{program}", "*.*")
                        .Select(d => new Version(new DirectoryInfo(d).Name))
                        .Order().Last();
                return Ok(actualVersion.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Problem(e.Message);
            }

        }

        /// <summary>
        /// Get files list with MD5 hash
        /// </summary>
        /// <param name="program">Program name</param>
        /// <param name="version">Version</param>
        [HttpGet("GetFilesListWithHash")]
        public async Task<ActionResult<string>> GetProgramFiles(string program, string version)
        {
            _logger.LogInformation($"Client {GetClientData()} getting programs files for program: {program}");

            try
            {
                var hashFileListPath = $"programs/{program}/{version}/FilesHash.json";
                if (!System.IO.File.Exists(hashFileListPath)) BadRequest();
                var hashFileList = await System.IO.File.ReadAllTextAsync(hashFileListPath);
                return Ok(hashFileList);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Problem(e.Message);
            }
        }

        /// <summary>
        /// Download file from server
        /// </summary>
        /// <param name="fileInfo">Information about file</param>
        [HttpGet("GetFile")]
        public async Task<ActionResult> GetFile([FromBody] DownloadFileInfo fileInfo)
        {
            string program = fileInfo.Program; string version = fileInfo.Version; string filePath = fileInfo.FilePath;

            var versionFolder = $"programs/{program}/{version}/src/";
            if (!System.IO.File.Exists(versionFolder + filePath))
                return BadRequest();

            var stream = new FileInfo(versionFolder + filePath).OpenRead();    // Открываем поток.
            return File(stream, "application/octet-stream", Path.GetFileName(versionFolder + filePath));
        }

        /// <summary>
        /// Download install file
        /// </summary>
        /// <param name="program">Program nave</param>
        /// <param name="version">Program version</param>
        /// <returns>Install File</returns>
        [HttpGet("GetInstallFile")]
        public async Task<ActionResult> GetInstallFile(string program, string version)
        {
            var clientData = GetClientData(); //If direct request

            _downloadLogger.Info($"Client {clientData} get install file {program}:{version}");
            // info about new client :)
            var headers = _httpContextAccessor?.HttpContext?.Request.Headers.Select(h => $"{h.Key} - {h.Value}");
            _logger.LogInformation("Someone download new install file:\n\t" + String.Join("\n\t", headers));

            try
            {
                var versionFolder = $"programs/{program}/{version}/";
                if (!Directory.Exists(versionFolder)) return BadRequest();
                var installFilePath = Directory.GetFiles(versionFolder).FirstOrDefault(fn => Path.GetExtension(fn) == ".exe");
                if (installFilePath is null) return BadRequest();

                var stream = new FileInfo(installFilePath).OpenRead();    // Открываем поток.
                return File(stream, "application/octet-stream", Path.GetFileName(installFilePath));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Problem(e.Message);
            }
        }

        /// <summary>
        /// Upload new program version
        /// </summary>
        /// <param name="loginDetail">Login and password</param>
        /// <param name="newVersionData">New version info</param>
        /// <returns></returns>
        [HttpPost("PostVersion")]
        [RequestSizeLimit(4294967295)]
        public async Task<ActionResult> Upload([FromForm] LoginDetails loginDetail,
        [FromForm] NewVersionData newVersionData)
        {
            _logger.LogInformation($"Client {GetClientData()} upload new version program:{newVersionData.Program}, version: {newVersionData.Version}");

            try
            {
                if (!CheckLogin(loginDetail)) return Unauthorized();

                if (string.IsNullOrEmpty(newVersionData.Version) && string.IsNullOrEmpty(newVersionData.Program)) return BadRequest("File name or version isn't correct");

                var result = await SaveVersionAsync(newVersionData.SourceFile, newVersionData.InstallFile, newVersionData.Changelog, newVersionData.Version, newVersionData.Program);

                if (result.IsSuccess) { return Ok(); }
                return Problem(result.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Problem(e.Message);
            }
        }


        /// <summary>
        /// Delete program
        /// </summary>    
        [Authorize]
        [HttpGet("DeleteProgram")]
        public IActionResult DeleteProgram([FromForm] LoginDetails loginDetail, string? program)
        {
            _logger.LogInformation($"Client {GetClientData()} deleted program: {program}");

            try
            {
                if (!CheckLogin(loginDetail)) return Unauthorized();

                if (!Directory.Exists($"programs/{program}/")) return BadRequest();
                try
                {
                    Directory.Delete($"programs/{program}", true);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    return Problem(e.Message);
                }
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Problem(e.Message);
            }
        }

        /// <summary>
        /// Delete the program version
        /// </summary>
        /// <param name="program">Program name</param>
        /// <param name="version">Version</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("DeleteVersion")]
        public ActionResult DeleteVersion([FromForm] LoginDetails loginDetail, string? program, string? version)
        {
            if (!CheckLogin(loginDetail)) return Unauthorized();
            _logger.LogInformation($"Client {GetClientData()}  deleted program version: {program}/{version}");

            if (!Directory.Exists($"programs/{program}/{version}")) return BadRequest();
            try
            {
                Directory.Delete($"programs/{program}/{version}", true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Problem(e.Message);
            }
            return Ok();
        }

        /// <summary>
        /// Save new version
        /// </summary>
        /// <param name="sourceFile">Archive with program files</param>
        /// <param name="installFile">Install file</param>
        /// <param name="changelog">List of changes</param>
        /// <param name="program">Program name</param>
        /// <param name="version">Version</param>
        /// <returns></returns>
        [NonAction]
        private async Task<(bool IsSuccess, string Message)> SaveVersionAsync
            (IFormFile sourceFile, IFormFile installFile, IFormFile changelog, string? version, string? program)
        {
            // Version directory
            var versionDirectory = $"programs/{program}/{version}";

            // Temp non-indexable directory
            var downloadDirectory = $"programs/{program}/Download";
            try
            {
                // Program directory
                Directory.CreateDirectory($"programs/{program}"); ;
                if (Directory.Exists(versionDirectory)) Directory.Delete(versionDirectory, true);
                if (Directory.Exists(downloadDirectory)) Directory.Delete(downloadDirectory, true);

                Directory.CreateDirectory($"{downloadDirectory}/src");

                // Save file to uploads
                var sourceFilePath = $"{downloadDirectory}/Archive.zip";
                await using (var fileStream = new FileStream(sourceFilePath, FileMode.Create))
                { await sourceFile.CopyToAsync(fileStream); }
                ZipFile.ExtractToDirectory(sourceFilePath, $"{downloadDirectory}/src");
                System.IO.File.Delete(sourceFilePath);

                // Save install file
                await using (var fileStream = new FileStream($"{downloadDirectory}/{installFile.FileName}", FileMode.Create))
                { await installFile.CopyToAsync(fileStream); }

                // Save changlog
                await using (var fileStream = new FileStream($"{downloadDirectory}/{changelog.FileName}", FileMode.Create))
                { await changelog.CopyToAsync(fileStream); }

                // Rename directory
                Directory.Move(downloadDirectory, versionDirectory);

                // Create hash list file
                await CreateHashFileListAsync($"programs/{program}/{version}");
                return (true, "Ok");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                // Clear incorrect data
                if (Directory.Exists($"{downloadDirectory}")) { Directory.Delete($"{downloadDirectory}", true); }
                if (Directory.Exists($"{versionDirectory}")) { Directory.Delete($"{versionDirectory}", true); }
                if (Directory.GetDirectories($"programs/{program}").Length == 0) { Directory.Delete($"programs/{program}", true); }
                return (false, e.Message);
            }
        }

        /// <summary>
        /// Files list
        /// </summary>       
        [NonAction]
        public List<string> FilesFromDirectory(string directory, List<string>? files = null)
        {
            try
            {
                files ??= new List<string>();
                files.AddRange(Directory.GetFiles(directory));
                foreach (var innerDirectory in Directory.GetDirectories(directory))
                { FilesFromDirectory(innerDirectory, files); }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            return files ?? new List<string>();
        }

        /// <summary>
        /// Check login data
        /// </summary>
        [NonAction]
        public bool CheckLogin(LoginDetails? loginDetail)
        {
            var isAutorize = false;
            if (string.IsNullOrEmpty(loginDetail?.Login) && string.IsNullOrEmpty(loginDetail?.Password)) isAutorize = false;
            if (loginDetail?.Login == _configuration["login"]?.ToLower() && loginDetail?.Password == _configuration["password"]) isAutorize = true;

            if (!isAutorize) _logger.LogInformation($"User {loginDetail?.Login} filed atorization");
            return isAutorize;
        }

        /// <summary>
        /// Create files list with md5 hash
        /// </summary>

        private async Task CreateHashFileListAsync(string programDirectory)
        {
            if (!Directory.Exists(programDirectory)) BadRequest();
            var source = $"{programDirectory}/src";
            var versionInfo = new AllFilesVersionInfo();
            var files = FilesFromDirectory(source);
            foreach (string fileName in files)
            {
                var hashString = await CreateHashStringForFileAsync(fileName);
                versionInfo.Add(new FileVersionInfo(fileName.Replace(source, ""), hashString));
            }

            // сохранение данных
            await using var fileStream = new FileStream($"{programDirectory}/FilesHash.json", FileMode.OpenOrCreate);
            await JsonSerializer.SerializeAsync(fileStream, versionInfo);
        }

        /// <summary>
        /// Get md5 hash as string
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Has md5 string</returns>

        private async Task<string> CreateHashStringForFileAsync(string filePath)
        {
            using var md5 = MD5.Create();
            await using var stream = System.IO.File.OpenRead(filePath);
            var hash = await md5.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Create files lists with md5 hash
        /// </summary>
        private async void RecreateAllHashFilesAsync()
        {
            var programs = Directory.GetDirectories("programs");
            foreach (var program in programs)
            {
                var versions = Directory.GetDirectories($"{program}");
                foreach (var version in versions)
                {
                    await CreateHashFileListAsync($"{version}");
                }
            }
        }

        /// <summary>
        /// Get Mime type
        /// </summary>
        /// <param name="fileName">Path to file</param>
        /// <returns>MIME Type</returns>
        private string GetMIMEType(string fileName)
        {
            var provider =
                new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(fileName, out string? contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }

        /// <summary>
        /// Return Client Data
        /// </summary>
        /// <returns></returns>
        private string? GetClientData()
        {
            var ip = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress;
            if (ip is null) return null;

            var xForwardIp = _httpContextAccessor?.HttpContext?.Request.Headers["X-Forwarded-For"].ToString();            
            var xRealIpAgent = _httpContextAccessor?.HttpContext?.Request.Headers["X-Real-IP"].ToString();
            var userAgent = _httpContextAccessor?.HttpContext?.Request.Headers["User-Agent"].ToString();

            var realIp = !String.IsNullOrEmpty(xRealIpAgent) ? xRealIpAgent :
                         !String.IsNullOrEmpty(xForwardIp) ? xForwardIp :
                          string.Join(", ", Dns.GetHostEntry(ip).AddressList.ToList());

            return $"IPs: {realIp}, UserAgent:{userAgent}";
        }

       
    }
}
