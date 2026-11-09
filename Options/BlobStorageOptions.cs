namespace SocialMediaAppBackend.Options;

public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "auto";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string? ServiceUrl { get; set; }
    public string? PublicBaseUrl { get; set; }
    public bool ForcePathStyle { get; set; } = false;
}
