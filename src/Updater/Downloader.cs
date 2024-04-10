using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace Updater
{
    public class Downloader
    {
        private const int versionHistory = 4;

        /// <summary>
        /// Загрузка новой версии в AppData/Roaming/{program} и последующее обновление файлов программы в каталоге программы
        /// </summary>
        /// <param name="program">Имя программы для обновления</param>
        /// <param name="currentVersion">Текущая версия</param>
        /// <param name="programPath">Путь к файлам программы</param>      
        public async Task DownloadNewVersionAsync(string program, Version currentVersion, string programPath)
        {
            var lastVersion = await GetLastVersionAsync(program);
            if (lastVersion is null || lastVersion <= currentVersion) return;

            string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var versionsDirectory = $"{appDataDirectory}\\{program}\\Versions\\";
            if (!Path.Exists(versionsDirectory)) Directory.CreateDirectory(versionsDirectory);

            // Версии хранящиеся в каталоге старых версий
            var downloadedVersion = Directory.GetDirectories(versionsDirectory)
                .Where(s => Version.TryParse(Path.GetFileName(s), out _))
                .Select(s => new Version(Path.GetFileName(s)))
                .Order().ToList();

            // Если текущая версия уже существует, но не обновлена - лучше удалить и скачать заново, что бы избежать проблем
            if (downloadedVersion.Contains(lastVersion)) Directory.Delete($"{versionsDirectory}\\{lastVersion.ToString(4)}", true);

            // Удаляю самые старые версии
            for (int i = 0; i < downloadedVersion.Count - (versionHistory + 1); i++)
                Directory.Delete($"{versionsDirectory}\\{downloadedVersion[i].ToString(4)}", true);

            var downloadDirectory = $"{versionsDirectory}\\Download\\";
            var lastVersionDirectory = $"{versionsDirectory}\\{lastVersion.ToString(4)}\\";
            if (Directory.Exists(downloadDirectory)) Directory.Delete(downloadDirectory, true);
            Directory.CreateDirectory(downloadDirectory);

            // Резервная копия текущей версии, если не существует
            var backupOldVersionDirectory = $"{versionsDirectory}\\{currentVersion.ToString(4)}\\";
            if (!Path.Exists(backupOldVersionDirectory))
            {
                CopyAllDataToDirectory(programPath, backupOldVersionDirectory, true);
            }

            var fileList = await GetFilesListWithHashAsync(program, lastVersion);

            if (fileList is null) return;
            foreach (var file in fileList)
            {
                var oldVersionFilePath = $"{programPath}\\{file.FileName}";
                var downloadFilePath = $"{downloadDirectory}\\{file.FileName}";
                if (File.Exists(oldVersionFilePath))
                {
                    // Если файл текущей версии актуален - копирую его, а не скачиваю
                    var md5Hash = await CreateMd5HashStringForFileAsync(oldVersionFilePath);
                    if (md5Hash == file.Md5Hash)
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine($"Файл {file.FileName} имеет актуальную версию");
                        var directoryName = Path.GetDirectoryName(downloadFilePath);
                        if (directoryName is not null) Directory.CreateDirectory(directoryName);
                        File.Copy(oldVersionFilePath, downloadFilePath);
                        continue;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("Файл неактуален и будет обновлен");
                    }
                }

                bool isDownloadSucceed = await DownloadFileFromUrlAsync(program, lastVersion, file.FileName, downloadDirectory);
                if (isDownloadSucceed)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Файл {file.FileName} успешно скачан");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Файл {file.FileName} не удалось скачать");
                }
            }

            ClearDirectory(programPath);
            Directory.Move(downloadDirectory, lastVersionDirectory);
            CopyAllDataToDirectory(lastVersionDirectory, programPath, true);
        }

        /// <summary>
        /// Получить актуальную версию указанной программы
        /// </summary>
        /// <param name="program">Имя запрашиваемой программы</param>
        /// <returns>Строка версии или null если программы не существует или ответа нет</returns>
        private async Task<Version?> GetLastVersionAsync(string program)
        {
            var httpClient = new HttpClient();
            using HttpRequestMessage request =
                new(HttpMethod.Get, $"http://localhost:5228/Version/GetActualVersion?program={program}");
            var response = await httpClient.SendAsync(request);
            var version = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode ? new Version(version) : null;
        }

        /// <summary>
        /// Получение списка файлов с хешем с сервера
        /// </summary>
        /// <param name="program">Имя запрашиваемой программы</param>
        /// <param name="version">Запрашиваемая версия</param>
        /// <returns>Список файлов с хешем</returns>
        private async Task<AllFilesVersionInfo?> GetFilesListWithHashAsync(string program, Version version)
        {
            HttpClient httpClient = new HttpClient();
            using HttpRequestMessage request =
                new(HttpMethod.Get, $"http://localhost:5228/Version/GetFilesListWithHash?program={program}&version={version.ToString()}");
            var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AllFilesVersionInfo>() : null;
        }

        /// <summary>
        /// Скачивание файла с сервера
        /// </summary>
        /// <param name="program">Программа</param>
        /// <param name="version">Версия</param>
        /// <param name="filePath">Путь к файлу на сервере или программе</param>
        /// <param name="directoryPath">Каталог назначения</param>
        /// <returns>Успешность скачивания файла</returns>
        private async Task<bool> DownloadFileFromUrlAsync(string program, Version version, string filePath, string directoryPath)
        {
            var httpClient = new HttpClient();

            using var request =
                new HttpRequestMessage(HttpMethod.Get, $"http://localhost:5228/Version/GetFile");

            request.Content = JsonContent.Create(new DownloadFileInfo(program, version.ToString(), filePath));

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ошибка скачивания файла");
                return false;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var directory = Path.GetDirectoryName(filePath);
            if (directory is not null && directory != "\\")
                Directory.CreateDirectory(directoryPath + directory);

            // запись в файл
            var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(directoryPath + filePath);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(fileStream);
            return true;
        }

        /// <summary>
        /// Получить хеш Md5 для файла в виде строки
        /// </summary>
        /// <param name="filePath">путь к файл</param>
        /// <returns>Хеш Md5 в виде строки</returns>
        private async Task<string> CreateMd5HashStringForFileAsync(string filePath)
        {
            using var md5 = MD5.Create();
            await using var stream = File.OpenRead(filePath);
            var hash = await md5.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Полное копирование директории
        /// </summary>
        /// <param name="sourceDirectory">Из</param>
        /// <param name="destinationDirectory">В</param>
        private void CopyAllDataToDirectory(string sourceDirectory, string destinationDirectory, bool recursive = true)
        {

            var directory = new DirectoryInfo(sourceDirectory);

            if (!directory.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {directory.FullName}");

            DirectoryInfo[] directories = directory.GetDirectories();

            Directory.CreateDirectory(destinationDirectory);

            foreach (FileInfo file in directory.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDirectory, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDirectories in directories)
                {
                    string newDestinationDirectory = Path.Combine(destinationDirectory, subDirectories.Name);
                    CopyAllDataToDirectory(subDirectories.FullName, newDestinationDirectory, true);
                }
            }
        }

        /// <summary>
        /// Очистить каталог от всех файлов
        /// </summary>
        private void ClearDirectory(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);

            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {directoryInfo.FullName}");

            directoryInfo.GetFiles().ToList().ForEach(file => file.Delete());
            directoryInfo.GetDirectories().ToList().ForEach(directory => directory.Delete(true));
        }
    }

    /// <summary>
    /// Структура запроса на скачивание файла с сервера
    /// </summary>
    internal record class DownloadFileInfo(string program, string Version, string FilePath);

    /// <summary>
    /// Список файлов с хешем Md5
    /// </summary>
    internal class AllFilesVersionInfo : List<FileVersionInfo> { }

    /// <summary>
    /// Информация о одном скачиваемом файле
    /// </summary>
    internal record class FileVersionInfo(string FileName, string Md5Hash);
}
