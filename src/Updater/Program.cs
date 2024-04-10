using Updater;

Console.WriteLine("Start!");

if (args.Length < 3) return;


var program = args[0];
var currentVersion = new Version(args[1]);
var programPath = args[2];

var versionHistory = 4;

while (true)
{
    Console.ForegroundColor = ConsoleColor.White;

    Console.ReadLine();

    var downloader = new Downloader();
    await downloader.DownloadNewVersionAsync(program, currentVersion, programPath);

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("Next!");
}







