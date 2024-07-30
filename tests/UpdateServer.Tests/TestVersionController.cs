using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using UpdateServer.Controllers;
using UpdateServer.Models;
using UpdateServer.ViewModel;

namespace UpdateServer.Tests
{
    public class Tests
    {
        private const string _testProgramName = "Test Program";
        private const string _testProgramVersion = "0.0.0.1";
        private const string _changelogText = "test";
        private const string _testFileName = "\\test.txt";
        private const string _testHashCode = "098f6bcd4621d373cade4e832627b4f6";

        [SetUp]
        public void Setup()
        {
            VersionController versionController = GetController();

            var loginDetail = new LoginDetails("test", "test");
            IFormFile sourceFile = ReadFormFile("MockFiles\\test.zip");
            IFormFile installFile = ReadFormFile("MockFiles\\test.exe");
            IFormFile changelogFile = ReadFormFile("MockFiles\\Changelog.txt");


            var versionData = new NewVersionData(_testProgramName, _testProgramVersion, sourceFile, installFile, changelogFile);
            versionController.Upload(loginDetail, versionData).Wait();
        }

        [Test, Order(1)]
        public async Task CheckUpload()
        {
            VersionController versionController = GetController();

            var loginDetail = new LoginDetails("test", "test");
            IFormFile sourceFile = ReadFormFile("MockFiles\\test.zip");
            IFormFile installFile = ReadFormFile("MockFiles\\test.exe");
            IFormFile changelogFile = ReadFormFile("MockFiles\\Changelog.txt");


            var versionData = new NewVersionData(_testProgramName, _testProgramVersion, sourceFile, installFile, changelogFile);
            var result = await versionController.Upload(loginDetail, versionData);
            Assert.That(result, Is.InstanceOf<OkResult>());
        }


        [Test, Order(2)]
        public void CheckPrograms()
        {
            var versionController = GetController();
            var result = versionController.GetPrograms().Result;
            var isProgramCorrect = result is OkObjectResult ok && ok.Value is IList<ProgramInfo> programs && programs.Any(p => p.Program == _testProgramName);
            Assert.That(isProgramCorrect, Is.True);
        }



        [Test, Order(2)]
        public void CheckVersion()
        {
            var versionController = GetController();
            var result = versionController.GetVersions(_testProgramName).Result;
            var isVersionCorrect = result is OkObjectResult ok && 
                ok.Value is VersionViewModel program &&
                program.Program == _testProgramName && program.Versions.Any(vi => vi == new ProgramVersionInfo(_testProgramVersion, _changelogText));
            Assert.That(isVersionCorrect, Is.True);
        }    
        

        [Test, Order(2)]
        public async Task CheckFiles()
        {
            var versionController = GetController();
            var result = await versionController.GetProgramFiles(_testProgramName, _testProgramVersion);
            //_ = result.Result is OkObjectResult ok && ok.Value is String filesHash;
            var filesHash = (result.Result as OkObjectResult)?.Value as string;
            Assert.That(filesHash, Is.Not.EqualTo(null));

            var allFilesVersionInfo = JsonSerializer.Deserialize<AllFilesVersionInfo>(filesHash);

            Assert.That(allFilesVersionInfo?.Any(f=> f.FileName == _testFileName && f.Md5Hash == _testHashCode), Is.True);
        }    


        
        [Test, Order(10)]
        public void CheckDeleteVersion()
        {
            var versionController = GetController();
            var loginDetail = new LoginDetails("test", "test");

            var result = versionController.DeleteVersion(loginDetail, _testProgramName, _testProgramVersion);
            Assert.That(result, Is.InstanceOf<OkResult>());
        }
         
         
        [Test, Order(11)]
        public void CheckDeleteProgram()
        {
            var versionController = GetController();
            var loginDetail = new LoginDetails("test", "test");

            var result = versionController.DeleteProgram(loginDetail, _testProgramName);
            Assert.That(result, Is.InstanceOf<OkResult>());
        }



        private VersionController GetController()
        {
            var logger = Mock.Of<ILogger<VersionController>>();
            var conficuration = MockConfiguration();
            var httpContextAccessor = Mock.Of<IHttpContextAccessor>();

            var versionController = new VersionController(logger, conficuration, httpContextAccessor);
            return versionController;
        }


        private IConfiguration MockConfiguration()
        {
            var configForSmsApi = new Dictionary<string, string>
                {
                    {"login", "test"},
                    {"password", "test"},
                };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configForSmsApi)
                .Build();
        }

        private IFormFile ReadFormFile(string path)
        {
            byte[] filebytes = File.ReadAllBytes(path);
            return new FormFile(new MemoryStream(filebytes), 0, filebytes.Length, "Data", Path.GetFileName(path));
        }
    }
}