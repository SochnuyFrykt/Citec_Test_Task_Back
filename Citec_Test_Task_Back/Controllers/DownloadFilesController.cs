using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Citec_Test_Task_Back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadFilesController : ControllerBase
{
    private readonly HttpClient _client;
    private readonly IWebHostEnvironment _hostingEnvironment;

    private const string _updatedFile = "https://fias.nalog.ru/WebServices/Public/GetLastDownloadFileInfo";

    public DownloadFilesController(HttpClient client, IWebHostEnvironment hostingEnvironment)
    {
        _client = client;
        _hostingEnvironment = hostingEnvironment;
    }

    [HttpGet("LastDownloadFileInfo")]
    public async Task<IActionResult> GetLastFileInfo(CancellationToken token)
    {
        try
        {
            var response = await _client.GetAsync(_updatedFile, token);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(token);
            var opt = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var data = JsonSerializer.Deserialize<File>(jsonString, opt);
            
            await DownloadAndUnzipFile(data.GarXMLDeltaURL, token);
            
            return Ok(data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return NotFound($"{e}");
        }
    }

    private async Task DownloadAndUnzipFile(string urlPathFile, CancellationToken token)
    {
        // Используем временный файл для скачивания, чтобы не засорять папку проекта
        var archiveFileName = Path.GetFileName(urlPathFile);
        var downloadPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{archiveFileName}");

        try
        {
            // Блок для скачивания во временный файл
            {
                await using var responseStream = await _client.GetStreamAsync(urlPathFile, token);
                await using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                await responseStream.CopyToAsync(fileStream, token);
            } // Потоки закрываются, файл на диске освобождается

            // --- ШАГ 3: Определяем путь для распаковки ВНУТРИ проекта ---
            var targetFolder = "FiasData"; // Название папки в вашем проекте
            var extractPath = Path.Combine(_hostingEnvironment.ContentRootPath, targetFolder);

            // Убедимся, что директория существует. Если нет - создаем её.
            Directory.CreateDirectory(extractPath);

            // Распаковываем архив по новому пути
            ZipFile.ExtractToDirectory(downloadPath, extractPath, true);

            Console.WriteLine($"Архив успешно распакован в: {extractPath}");
        }
        finally
        {
            // Гарантированно удаляем временный zip-архив после всех операций
            if (System.IO.File.Exists(downloadPath))
            {
                System.IO.File.Delete(downloadPath);
            }
        }
    }
}

public record File(
    long VersionId,
    string TextVersion,
    string GarXMLFullURL,
    string GarXMLDeltaURL,
    DateTime ExpDate,
    string Date
);