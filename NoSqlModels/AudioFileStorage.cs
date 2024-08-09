namespace NoSqlModels;

public class AudioFileStorage
{
    public string Id { get; set; }

    public string FileName { get; set; }

    public AudioFileType AudioType { get; set; }

    public DateTime UploadDate { get; set; }

    public string UploadedBy { get; set; }

    public byte[] Contents { get; set; }
}

public enum AudioFileType
{
    Audio1, Audio2, Audio3
}
