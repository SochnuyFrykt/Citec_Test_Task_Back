namespace Citec_Test_Task_Back.DataAccess;

public class ArchiveData
{
    public long VersionId { get; set; }
    public string TextVersion { get; set; }
    public string GarXMLFullURL { get; set; }
    public string GarXMLDeltaURL { get; set; }
    public DateTime ExpDate { get; set; }
    public string Date { get; set; }
}