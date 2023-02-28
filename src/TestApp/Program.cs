using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

var account = configuration["StorageAccount"];
var blobServiceClient = GetBlobServiceClient(account);

var testContainer = blobServiceClient.GetBlobContainerClient("test");
testContainer.CreateIfNotExists();

var content = new BinaryData("Hello World!");

var blobClient = testContainer.GetAppendBlobClient("test.txt");
blobClient.CreateIfNotExists(metadata: new Dictionary<string, string> { { "status", "pending" } });


testContainer.GetBlobs(BlobTraits.Metadata).ToList().ForEach(b =>
{
    Console.WriteLine($"Blob: {b.Name}");
    // Write out each metadata key and value
    foreach (var metadata in b.Metadata)
    {
        Console.WriteLine($"  {metadata.Key}: {metadata.Value}");
    }
});

static BlobServiceClient GetBlobServiceClient(string account)
{
    var accountUri = new Uri($"https://{account}.blob.core.windows.net");
    var credential = new AzureCliCredential();
    var blobServiceClient = new BlobServiceClient(accountUri, credential);
    
    return blobServiceClient;
}
