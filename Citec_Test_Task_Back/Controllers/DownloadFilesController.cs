using System.IO.Compression;
using System.Text.Json;
using Citec_Test_Task_Back.DataAccess;
using Microsoft.AspNetCore.Mvc;

namespace Citec_Test_Task_Back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadFilesController : ControllerBase
{
    private readonly HttpClient _client;

    private const string _updatedFile = "https://fias.nalog.ru/WebServices/Public/GetLastDownloadFileInfo";

    public DownloadFilesController(HttpClient client)
    {
        _client = client;
    }

    [HttpGet("LastDownloadFileInfo")]
    public async Task<IActionResult> GetLastFileInfo(CancellationToken token)
    {
        try
        {
            var response = await _client.GetAsync(_updatedFile, token);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(token);
            var opt = new JsonSerializerOptions();
            opt.PropertyNameCaseInsensitive = true;

            var data = JsonSerializer.Deserialize<ArchiveData>(jsonString, opt);

            //var unzipFile = await DownloadAndUnzipFile(data.GarXMLDeltaURL, token);

            return Ok(data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return NotFound($"{e}");
        }
    }
}