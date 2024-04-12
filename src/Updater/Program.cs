using Updater;


//string? programPath = args.Select(s => s.Split("=")).FirstOrDefault(s=> s[0].ToLower() == "ProgramPath")?[1];

if (args.Length < 2) return;

string programPath = args[0];
string url = args[1];
if (!File.Exists(programPath)) return;


while (true)
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Clear();

    var downloader = new Downloader(programPath, url);
    downloader.FileDownload += Downloader_FileDownload;
    downloader.IsAgreeToKillProcess += Downloader_IsAgreeToKillProcess;
    var IsNeed = await downloader.IsUpdateNeedAsync();

    if (IsNeed == true)
    {
        Console.Clear();
        Console.CursorVisible = false;
        Console.Write("  00.0%   ");
        Console.Write(string.Join("", Enumerable.Range(0, 100).Select(i => "_")));

        await downloader.UpdateProgramAsync();
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Программа успешно обновлена");
    Console.ReadLine();
}

bool Downloader_IsAgreeToKillProcess()
{
    while (true)
    {
        Console.WriteLine("Внимание, все запущенные версии программы будут закрыты (Y/N)");
        var answer = Console.ReadKey().Key;
        if (answer == ConsoleKey.Y)
        {
            return true;
        }
        if (answer == ConsoleKey.N) return false;
    }
}

void Downloader_FileDownload(bool? isDownload, string fileName, int currentFile, int filesCount)
{

    //Console.SetCursorPosition(0, cursorTPosition.Top);
    //Console.Write(new string(' ', Console.BufferWidth));

    Console.SetCursorPosition(0, Console.CursorTop);
    Console.Write(((double)currentFile / filesCount).ToString("  00.0%   "));


    Console.SetCursorPosition(10 + 100 * currentFile / filesCount, Console.CursorTop);
    Console.Write("#");

    //if (isDownload == true)
    //{
    //    Console.ForegroundColor = ConsoleColor.Blue;
    //    Console.Write($"Файл {fileName} скачан");
    //}
    //if (isDownload == false)
    //{
    //    Console.ForegroundColor = ConsoleColor.Green;
    //    Console.Write($"Файл {fileName} актуален");
    //}
    //if (isDownload is null)
    //{
    //    Console.ForegroundColor = ConsoleColor.Red;
    //    Console.Write($"Ошибка скачивания файла {fileName}");
    //}
}