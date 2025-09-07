using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;

namespace Citec_Test_Task_Back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DirectoryController: ControllerBase
{
    private readonly HttpClient _client;

    public DirectoryController(HttpClient client)
    {
        _client = client;
    }
    
    [HttpPost("unzip_file")]
    public async Task<IActionResult> UnzipArchive(string urlPathFile, CancellationToken token)
    {
        var unzippedFileContents = new Dictionary<string, byte[]>();

        // ШАГ 1: Скачиваем архив в память
        var archiveBytes = await _client.GetByteArrayAsync(urlPathFile, token);

        // Создаем MemoryStream из скачанных байтов
        await using var archiveStream = new MemoryStream(archiveBytes);

        // ШАГ 2: Создаем ZipArchive для работы с потоком из памяти
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read))
        {
            // ШАГ 3: Проходим по каждому файлу в архиве
            foreach (var entry in archive.Entries)
            {
                // Пропускаем папки или пустые файлы
                if (string.IsNullOrEmpty(entry.Name) || entry.Length == 0) continue;
                if (!(entry.Name.Contains("as_addr_obj", StringComparison.InvariantCultureIgnoreCase)
                      || entry.Name.Contains("as_object_levels", StringComparison.InvariantCultureIgnoreCase))) 
                    continue;
                
                // ШАГ 4: Распаковываем каждый файл в отдельный поток в памяти
                await using var entryStream = entry.Open(); // Открываем поток для чтения содержимого файла
                await using var memoryStream = new MemoryStream();
                
                await entryStream.CopyToAsync(memoryStream, token);
                
                // Сохраняем результат в словарь
                unzippedFileContents.Add(entry.FullName, memoryStream.ToArray());
            }
        }
        
        Console.WriteLine($"Распаковано {unzippedFileContents.Count} файлов в память.");
        //return unzippedFileContents;
        
        return Ok(unzippedFileContents);
    }
    
    // private async Task<Dictionary<string, byte[]>> DownloadAndUnzipFile(string urlPathFile, CancellationToken token)
    // {
    //     
    // }
}