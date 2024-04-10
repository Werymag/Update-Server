
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Text;
using UpdateServer.ViewModel;

namespace UpdateServer.Controllers
{
    /// <summary>
    /// Контроллер страницы списка доступных программ
    /// </summary>
    public class ProgramsController : Controller
	{
		private readonly ILogger<ProgramsController> _logger;
        private readonly VersionController _versionController;

        public ProgramsController(ILogger<ProgramsController> logger, VersionController versionController)
        {
			this._logger = logger; 
            this._versionController = versionController;
        }


        /// <summary>
        /// Страница выбранной программы
        /// </summary>
		public IActionResult Index(string program)
        {
            //        var versionViewModel = new VersionViewModel(program);

            //        if (!Path.Exists($"Programs/{program}")) return BadRequest();

            //        // Список версий для программы (только папки соответствующего шаблона N.N.N.N)
            //        var versions = Directory.GetDirectories($"Programs/{program}", "???.???.???.???");

            //        // Получение списка установочных файлов для каждой версии
            //        foreach (var versionFolder in versions)
            //        {
            //var version = Path.GetFileName(versionFolder);

            //            var changeLogFilePath = $"{versionFolder}/changelog.txt";				
            //            var changelog  = System.IO.File.Exists(changeLogFilePath) ? System.IO.File.ReadAllText(changeLogFilePath, Encoding.Default) : "";

            //            var installFilePath = Directory.GetFiles(versionFolder).FirstOrDefault(fn => Path.GetExtension(fn) ==".exe");

            //            if (installFilePath != null)
            //            {
            //                var installFile = new ProgramInstallFile(Path.GetFileName(installFilePath), installFilePath, changelog, version);
            //                programViewModel.Files.Add(installFile);
            //            }
            //        }

            var programVersions = _versionController.GetVersions(program);
            if (programVersions.Result is OkObjectResult okResult)
                return View(okResult.Value);
            return BadRequest();
		}  
	}
}
