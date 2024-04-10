namespace UpdateServer.ViewModel
{
    public class VersionViewModel
    {
        public VersionViewModel(string program)
        {
            program = program;
        }

        public string program { get; set; }
        public List<ProgramInstallFile> Files { get; set; } = new List<ProgramInstallFile>();
    }

    public record class ProgramInstallFile(string FileName, string FilePath, string Changelog, string Version);

}
