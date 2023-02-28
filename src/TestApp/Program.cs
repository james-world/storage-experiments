using System.Text;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;


var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

var account = configuration["StorageAccount"];
var blobServiceClient = GetBlobServiceClient(account);

var testContainer = blobServiceClient.GetBlobContainerClient("test");
testContainer.CreateIfNotExists();

var userDelegationKey = await blobServiceClient
    .GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));


// check to see if a blob called test.csv exists
var blobClient = testContainer.GetBlockBlobClient("test.csv");
blobClient.DeleteIfExists();
blobClient.Upload(new MemoryStream(new byte[0]));
blobClient.SetMetadata(new Dictionary<string, string> { { "status", "processing" } });

var id = Convert.ToBase64String(Encoding.UTF8.GetBytes("1048"));
blobClient.StageBlock(id, new BinaryData("FirstName,LastName\n").ToStream());
var headers = new BlobHttpHeaders { ContentType = "text/csv", ContentDisposition = "attachment; filename=test2.csv", ContentEncoding = "utf-8" };
blobClient.CommitBlockList(new List<string> { id }, new CommitBlockListOptions { HttpHeaders = headers, Metadata = new Dictionary<string, string> { { "status", "ready" } } });


var id2 = Convert.ToBase64String(Encoding.UTF8.GetBytes("2048"));
var id3 = Convert.ToBase64String(Encoding.UTF8.GetBytes("3048"));
blobClient.StageBlock(id2, new BinaryData("Tom,Smith\n").ToStream());
blobClient.StageBlock(id3, new BinaryData("Harry,Jones\n").ToStream());
headers = new BlobHttpHeaders { ContentType = "text/csv", ContentDisposition = "attachment; filename=test2.csv", ContentEncoding = "utf-8" };
blobClient.CommitBlockList(new List<string> { id, id2, id3 }, new CommitBlockListOptions { HttpHeaders = headers, Metadata = new Dictionary<string, string> { { "status", "ready" } } });



Uri GetBlobDownloadUri(BlockBlobClient blobClient, UserDelegationKey userDelegationKey, string account) {
    var sasBuilder = new BlobSasBuilder {    
        StartsOn = DateTimeOffset.UtcNow,
        ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5),
        BlobContainerName = testContainer.Name,
        BlobName = blobClient.Name,
        Protocol = SasProtocol.Https,
        Resource = "b",
        ContentDisposition = "attachment; filename=test2.csv",
        ContentType = "text/csv",
        ContentEncoding = "utf-8" };
    sasBuilder.SetPermissions(BlobSasPermissions.Read);
    var blobUriBuilder = new BlobUriBuilder(blobClient.Uri) {
        Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, account)
    };
    return blobUriBuilder.ToUri();
}

Console.WriteLine(GetBlobDownloadUri(blobClient, userDelegationKey, account));

// var content = new BinaryData("Hello World!");

//var blobClient = testContainer.GetAppendBlobClient("test.txt");
//blobClient.CreateIfNotExists(metadata: new Dictionary<string, string> { { "status", "pending" } });


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
