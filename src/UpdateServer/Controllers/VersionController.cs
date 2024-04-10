using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UpdateServer.Model;
using UpdateServer.ViewModel;
using FileVersionInfo = UpdateServer.Model.FileVersionInfo;

namespace UpdateServer.Controllers
{
    /// <summary>
    /// Api контроллер для работы с файлами версий программ
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class VersionController : Controller
    {
        private readonly ILogger<ProgramsController> _logger;
        private readonly IConfiguration _configuration;
        public VersionController(ILogger<ProgramsController> logger, IConfiguration configuration)
        {
            this._logger = logger;
            this._configuration = configuration;
        }


        /// <summary>
        /// Get a list of available programs
        /// </summary>
        [HttpGet("GetPrograms")]
        public ActionResult<List<ProgramInfo>> GetPrograms()
        {
            ///TODO файл для скачивания могут быть не во всех каталогах, обыграть!
            // The list of programs corresponding to the list of directories in the Programs folder
            var directoryInfo = Directory.CreateDirectory($"Programs");
            var programs = directoryInfo.GetDirectories().ToArray();

            var programInforms = new List<ProgramInfo>();
            foreach (var program in programs)
            {

                var versions = Directory
                    .GetDirectories(program.FullName, "???.???.???.???")
                    .Select(d => new Version(new DirectoryInfo(d).Name))
                    .Order();
                //Directory.GetDirectories(program.FullName).Select(d => new Version(Path.GetDirectoryName(d))).Order().Last();
                var actualVersion = versions.Last().ToString(4);

                var installFilePath = Directory.GetFiles($"{program.FullName}/{actualVersion}/").FirstOrDefault(fn => Path.GetExtension(fn) == ".exe");
                if (installFilePath is null) return BadRequest();
                programInforms.Add(new(program.Name, actualVersion, installFilePath));
            }

            return Ok(programInforms.ToArray());
        }

        /// <summary>
        /// Get a list of available version of program
        /// </summary>
        [HttpGet("GetVersions")]
        public ActionResult<List<ProgramInfo>> GetVersions(string program)
        {           
            // The list of programs corresponding to the list of directories in the Programs folder
            if (!Path.Exists($"Programs/{program}")) return BadRequest();

            var versions = Directory
                 .GetDirectories($"Programs/{program}", "???.???.???.???")              
                 .OrderBy(d => new Version(new DirectoryInfo(d).Name));

            var versionViewModel = new VersionViewModel(program);

            foreach (var version in versions)
            {
                var changeLogFilePath = $"{version}/changelog.txt";
                var changelog = System.IO.File.Exists(changeLogFilePath) ? System.IO.File.ReadAllText(changeLogFilePath, Encoding.Default) : "";

                var installFilePath = Directory.GetFiles(version).FirstOrDefault(fn => Path.GetExtension(fn) == ".exe");

                if (installFilePath != null)
                {
                    var installFile = new ProgramInstallFile(Path.GetFileName(installFilePath), installFilePath, changelog, new DirectoryInfo(version).Name);
                    versionViewModel.Files.Add(installFile);
                }           
            }

            return Ok(versionViewModel);           
        }

        /// <summary>
        /// Actual Version
        /// </summary>
        /// <param name="program">Program Name</param>
        /// <returns></returns>
        [HttpGet("GetActualVersion")]
        public ActionResult<string> GetActualVersionInfo(string program)
        {
            if (!Path.Exists($"Programs\\{program}")) return BadRequest("Program not found");
            var actualVersion = Directory
                    .GetDirectories($"Programs\\{program}", "???.???.???.???")
                    .Select(d => new Version(new DirectoryInfo(d).Name))
                    .Order().Last();
            return Ok(actualVersion.ToString());
        }

        /// <summary>
        /// Get files list with MD5 hash
        /// </summary>
        /// <param name="program">Program name</param>
        /// <param name="version">Version</param>
        [HttpGet("GetFilesListWithHash")]
        public async Task<ActionResult<string>> GetProgramFiles(string program, string version)
        {
            var hashFileListPath = $"Programs\\{program}\\{version}\\FilesHash.json";
            if (!System.IO.File.Exists(hashFileListPath)) BadRequest();
            var hashFileList = await System.IO.File.ReadAllTextAsync(hashFileListPath);
            return Ok(hashFileList);
        }

        /// <summary>
        /// Download file from server
        /// </summary>
        /// <param name="fileInfo">Information about file</param>
        [HttpGet("GetFile")]
        public ActionResult GetFile([FromBody] DownloadFileInfo fileInfo)
        {
            string program = fileInfo.program; string version = fileInfo.Version; string filePath = fileInfo.FilePath;

            var versionFolder = $"Programs/{program}/{version}/src/";
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
        public ActionResult GetInstallFile(string program, string version)
        {
            var versionFolder = $"Programs/{program}/{version}/";
            if (!Directory.Exists(versionFolder)) return BadRequest();
            var installFilePath = Directory.GetFiles(versionFolder).FirstOrDefault(fn => Path.GetExtension(fn) == ".exe");
            if (installFilePath is null) return BadRequest();

            var stream = new FileInfo(installFilePath).OpenRead();    // Открываем поток.
            return File(stream, "application/octet-stream", Path.GetFileName(installFilePath)); //new FileStreamResult(stream, "application/octet-stream");
        }

        /// <summary>
        /// Upload new version files
        /// </summary>
        /// <param name="sourceFile">Archive with program files</param>
        /// <param name="installFile">Install file</param>
        /// <param name="changelog">List of changes</param>
        /// <param name="authenticationData">Login and password</param>
        /// <param name="uploadFileInfo">Information about version</param>
        /// <returns></returns>
        [HttpPost("PostVersion")]
        [RequestSizeLimit(4294967295)]
        public async Task<ActionResult> Upload([FromForm] AuthenticationData authenticationData,
         [FromForm] UploadFileInfo uploadFileInfo,
         IFormFile sourceFile, IFormFile installFile, IFormFile changelog)
        {
            try
            {
                var login = authenticationData.Login;
                var password = authenticationData.Password;

                if (login is null && password is null) return Unauthorized();

                var isAuthorize = (authenticationData.Login == _configuration["login"]
                    && authenticationData.Password == _configuration["password"]);

                if (!isAuthorize) return Unauthorized();

                var version = uploadFileInfo.Version;
                var program = uploadFileInfo.ProgramName;

                if (version is null && program is null) return BadRequest();

                await SaveVersion(sourceFile, installFile, changelog, version, program);

                return Ok();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// Delete program
        /// </summary>    
        [Authorize]
        [HttpGet("DeleteProgram")]
        public IActionResult DeleteProgram([FromForm] AuthenticationData authenticationData, string? program)
        {
            if (!Directory.Exists($"Programs/{program}/")) return BadRequest();
            Directory.Delete($"Programs/{program}", true);
            return Ok();
        }

        /// <summary>
        /// Delete the program version
        /// </summary>
        /// <param name="program">Program name</param>
        /// <param name="version">Version</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("DeleteVersion")]
        public ActionResult DeleteVersion([FromForm] AuthenticationData authenticationData, string? program, string? version)
        {
            if (!Directory.Exists($"Programs/{program}/{version}")) return BadRequest();
            Directory.Delete($"Programs/{program}/{version}", true);
            return Ok();
        }

        /// <summary>
        /// Сохранение новой версии на диск
        /// </summary>
        /// <param name="sourceFile">Архив с файлами программы</param>
        /// <param name="installFile">Установочный файл</param>
        /// <param name="changelog">Список изменений</param>
        /// <param name="version">Версия</param>
        /// <param name="program">Имя программы</param>
        /// <returns></returns>
        [NonAction]
        private async Task SaveVersion(IFormFile sourceFile, IFormFile installFile, IFormFile changelog, string? version, string? program)
        {
            // Каталог программы
            Directory.CreateDirectory($"Programs/{program}"); ;

            // Каталог версии
            var versionDirectory = $"Programs/{program}/{version}";

            // Временный неидексируемый каталог
            var downloadDirectory = $"Programs/{program}/Download";

            if (Directory.Exists(versionDirectory)) Directory.Delete(versionDirectory, true);
            if (Directory.Exists(downloadDirectory)) Directory.Delete(downloadDirectory, true);

            Directory.CreateDirectory($"{downloadDirectory}/src");

            // Сохраняем файл в папку uploads
            var sourceFilePath = $"{downloadDirectory}/Archive.zip";
            await using (var fileStream = new FileStream(sourceFilePath, FileMode.Create))
            { await sourceFile.CopyToAsync(fileStream); }
            ZipFile.ExtractToDirectory(sourceFilePath, $"{downloadDirectory}/src");
            System.IO.File.Delete(sourceFilePath);

            // Файл установщик
            await using (var fileStream = new FileStream($"{downloadDirectory}/{installFile.FileName}", FileMode.Create))
            { await installFile.CopyToAsync(fileStream); }

            // чейнжлог
            await using (var fileStream = new FileStream($"{downloadDirectory}/{changelog.FileName}", FileMode.Create))
            { await changelog.CopyToAsync(fileStream); }

            // Переименовываю временную директорию в директорию версии
            Directory.Move(downloadDirectory, versionDirectory);

            ///Создаю файл с хешами всех файлов
            CreateHashFileList($"Programs\\{program}\\{version}");
        }

        /// <summary>
        /// Список файлов в папке на всю глубину
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
                Debug.WriteLine(e.Message);
            }

            return files ?? new List<string>();
        }

        /// <summary>
        /// Создать файл списка файлов с хешами MD5
        /// </summary>
        [NonAction]
        private async void CreateHashFileList(string programDirectory)
        {
            if (!Directory.Exists(programDirectory)) BadRequest();
            var source = $"{programDirectory}\\src";
            var versionInfo = new AllFilesVersionInfo();
            var files = FilesFromDirectory(source);
            foreach (string fileName in files)
            {
                var hashString = await CreateHashStringForFile(fileName);
                versionInfo.Add(new FileVersionInfo(fileName.Replace(source, ""), hashString));
            }

            // сохранение данных
            await using var fileStream = new FileStream($"{programDirectory}/FilesHash.json", FileMode.OpenOrCreate);
            await JsonSerializer.SerializeAsync(fileStream, versionInfo);
        }

        /// <summary>
        /// Получить хеш Md5 в виде строки
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Хеш в виде строки</returns>
        [NonAction]
        private async Task<string> CreateHashStringForFile(string filePath)
        {
            using var md5 = MD5.Create();
            await using var stream = System.IO.File.OpenRead(filePath);
            var hash = await md5.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Создаю файлы с хешами всех файлов для всех программ и версий
        /// </summary>
        [NonAction]
        private async void RecreateAllHashFiles()
        {
            var programs = Directory.GetDirectories("Programs");
            foreach (var program in programs)
            {
                var versions = Directory.GetDirectories($"{program}");
                foreach (var version in versions)
                {
                    CreateHashFileList($"{version}");
                }
            }
        }
    }
}
