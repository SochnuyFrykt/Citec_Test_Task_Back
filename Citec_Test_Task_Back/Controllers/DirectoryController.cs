using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Citec_Test_Task_Back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DirectoryController : ControllerBase
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

        var archiveBytes = await _client.GetByteArrayAsync(urlPathFile, token);

        await using var archiveStream = new MemoryStream(archiveBytes);

        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read))
        {
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name) || entry.Length == 0) continue;
                if (!(entry.Name.Contains("as_addr_obj", StringComparison.InvariantCultureIgnoreCase)
                      || entry.Name.Contains("as_object_levels", StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                await using var entryStream = entry.Open();
                await using var memoryStream = new MemoryStream();

                await entryStream.CopyToAsync(memoryStream, token);

                unzippedFileContents.Add(entry.FullName, memoryStream.ToArray());
            }
        }

        Console.WriteLine($"Файлов в памяти {unzippedFileContents.Count}");

        return await ParseFile(unzippedFileContents, token);
    }

    private async Task<IActionResult> ParseFile(
        Dictionary<string, byte[]> unzippedFileContents,
        CancellationToken token)
    {
        var objLevelsFile = unzippedFileContents
            .FirstOrDefault(l => l.Key.Contains("AS_OBJECT_LEVELS"));

        using var stream = new MemoryStream(objLevelsFile.Value);
        var levels = await ParseObjectLevelsFile(stream, token);

        var allAddressObjects = new List<AddressObject>();
        var addressObjectEntries = unzippedFileContents
            .Where(kvp => kvp.Key.Contains("AS_ADDR_OBJ", StringComparison.InvariantCultureIgnoreCase));

        foreach (var entry in addressObjectEntries)
        {
            var parsedObjects = await ParseAddressObjects(
                new MemoryStream(entry.Value), token
            );
            
            allAddressObjects.AddRange(parsedObjects);
        }

        return Ok(new
        {
            levels,
            allAddressObjects
        });
    }

    private async Task<Dictionary<int, string>> ParseObjectLevelsFile(
        MemoryStream fileContent,
        CancellationToken token)
    {
        var doc = await XDocument.LoadAsync(fileContent, LoadOptions.None, token);
        return doc.Descendants("OBJECTLEVEL")
            .Where(el => el.Attribute("ISACTIVE")?.Value == "true")
            .ToDictionary(
                el => int.Parse(el.Attribute("LEVEL").Value),
                el => el.Attribute("NAME").Value
            );
    }

    private async Task<IEnumerable<AddressObject>> ParseAddressObjects(
        MemoryStream fileContent,
        CancellationToken token)
    {
        var doc = await XDocument.LoadAsync(fileContent, LoadOptions.None, token);
        var objects = doc.Descendants("OBJECT")
            .Where(el => el.Attribute("ISACTIVE")?.Value == "1")
            .Select(el => new AddressObject(
                Id: Guid.Parse(el.Attribute("OBJECTGUID").Value),
                Level: int.Parse(el.Attribute("LEVEL").Value),
                Name: el.Attribute("NAME").Value,
                TypeName: el.Attribute("TYPENAME").Value
            ));

        return objects;
    }
}

internal record AddressObject(Guid Id, int Level, string Name, string TypeName);

public record ReportLevel
{
    public int Level { get; set; }
    public string LevelName { get; set; }
    public List<ReportObject> Objects { get; set; }
}

public record ReportObject(string Type, string Name);