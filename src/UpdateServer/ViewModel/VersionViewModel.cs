namespace UpdateServer.ViewModel
{
    public class VersionViewModel
    {
        public VersionViewModel(string program)
        {
            this.Program = program;
        }

        public string Program { get; set; }
        public List<ProgramInstallFile> Files { get; set; } = new List<ProgramInstallFile>();
    }

    public record class ProgramInstallFile(string FileName, string FilePath, string Changelog, string Version);

}
