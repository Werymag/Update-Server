using Microsoft.AspNetCore.Mvc;

namespace UpdateServer.Models
{
    public record class NewVersionData(string Program, string Version,
         IFormFile SourceFile, IFormFile InstallFile, IFormFile Changelog);
}
