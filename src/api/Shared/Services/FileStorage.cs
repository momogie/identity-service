using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using FluentFTP;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Shared;

public static class FileStorageExtensions
{
    public static void AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration["FileStorage:Provider"] == FileStorageProvider.S3)
        {
            services.AddAWSService<IAmazonS3>(new AWSOptions
            {
                Credentials = new BasicAWSCredentials(configuration["FileStorage:S3:AccessKey"], configuration["FileStorage:S3:AccessSecret"]),
                Region = RegionEndpoint.USEast1, // need to replace
            });
            services.AddSingleton<IFileStorage, AwsS3FileStorage>();
        }

        if (configuration["FileStorage:Provider"] == FileStorageProvider.Local)
        {
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        }

        if (configuration["FileStorage:Provider"] == FileStorageProvider.Google)
        {
            services.AddSingleton<IFileStorage>(p => new GoogleFileStorage(new GoogleConfig
            {
                CredentialFile = configuration["FileStorage:Google:CredentialFile"],
                BucketName = configuration["FileStorage:Google:BucketName"],
                ProjectId = configuration["FileStorage:Google:ProjectId"],
            }));
        }
    }
}

public class FileStorageProvider
{
    public const string S3 = "S3";
    public const string Google = "GOOGLE";
    public const string Local = "LOCAL";
}

public interface IFileStorage
{
    void SaveToImports(long workspaceId, string key, IFormFile file);
    void SaveToImports(long workspaceId, string key, Stream stream);
    Stream GetFromImports(long workspaceId, string key);
    void RemoveFromImports(long workspaceId, string key);
    bool ImportsExists(long workspaceId, string key);

    void SaveToExports(long workspaceId, string key, IFormFile file);
    void SaveToExports(long workspaceId, string key, Stream stream);
    Stream GetFromExports(long workspaceId, string key);
    void RemoveFromExports(long workspaceId, string key);
    bool ExportsExists(long workspaceId, string key);

    void SaveToAttachments(long workspaceId, string key, IFormFile file);
    void SaveToAttachments(long workspaceId, string key, Stream stream);
    Stream GetFromAttachments(long workspaceId, string key);
    void RemoveFromAttachments(long workspaceId, string key);
    bool AttachmentsExists(long workspaceId, string key);

    void SaveToImages(long workspaceId, string key, IFormFile file);
    void SaveToImages(long workspaceId, string key, Stream stream);
    Stream GetFromImages(long workspaceId, string key);
    Stream GetFromImagesThumbnail(long workspaceId, string key);
    void RemoveFromImages(long workspaceId, string key);
    bool ImagesExists(long workspaceId, string key);
    //void RemoveFromImagesThumbnail(long workspaceId, string key);
}

public class LocalFileStorage : IFileStorage
{
    protected IConfiguration Configuration;
    protected string BaseDirectory;

    public LocalFileStorage(IConfiguration configuration)
    {
        Configuration = configuration;
        BaseDirectory = Configuration["FileStorage:LocalStoragePath"];
    }

    public bool AttachmentsExists(long workspaceId, string key)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "attachments");
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        return File.Exists(filePath);
    }

    public bool ExportsExists(long workspaceId, string key)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "exports");
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        return File.Exists(filePath);
    }

    public Stream GetFromAttachments(long workspaceId, string id)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "attachments");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(id).ToLower() + ".bin");
        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }

    public Stream GetFromExports(long workspaceId, string id)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "exports");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(id).ToLower() + ".bin");
        var fileStream = new FileStream(filePath, FileMode.Open);
        return fileStream;
    }

    public Stream GetFromImages(long workspaceId, string id)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "images");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(id).ToLower() + ".bin");
        var fileStream = new FileStream(filePath, FileMode.Open);
        return fileStream;
    }

    public Stream GetFromImagesThumbnail(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public Stream GetFromImagesThumnail(long workspaceId, string id)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "images", "thumbnail");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(id).ToLower() + ".bin");
        var fileStream = new FileStream(filePath, FileMode.Open);
        return fileStream;
    }

    public Stream GetFromImports(long workspaceId, string id)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "imports");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(id).ToLower() + ".bin");
        var fileStream = new FileStream(filePath, FileMode.Open);
        return fileStream;
    }

    public bool ImagesExists(long workspaceId, string key)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "images");
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        return File.Exists(filePath);
    }

    public bool ImportsExists(long workspaceId, string key)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "imports");
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        return File.Exists(filePath);
    }

    public void RemoveFromAttachments(long workspaceId, string key)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "attachments");
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void RemoveFromExports(long workspaceId, string key)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "exports");
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void RemoveFromImages(long workspaceId, string key)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "images");
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void RemoveFromImports(long workspaceId, string key)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "imports");
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void SaveToAttachments(long workspaceId, string key, IFormFile file)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "attachments");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        var stream = new FileStream(filePath, FileMode.Create);
        file.CopyTo(stream);
        stream.Flush();
        stream.Dispose();
        stream.Close();
    }

    public void SaveToAttachments(long workspaceId, string key, Stream stream)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "attachments");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        using var fileStream = new FileStream(filePath, FileMode.Create);
        stream.CopyTo(fileStream);

        fileStream.Dispose();
        stream.Dispose();
    }

    public void SaveToExports(long workspaceId, string key, IFormFile file)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "exports");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        var stream = new FileStream(filePath, FileMode.Create);
        file.CopyTo(stream);

        stream.Dispose();
    }

    public void SaveToExports(long workspaceId, string key, Stream stream)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "exports");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        using var fileStream = new FileStream(filePath, FileMode.Create);
        stream.CopyTo(fileStream);
        fileStream.Dispose();
    }

    public void SaveToImages(long workspaceId, string key, IFormFile file)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "images");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        using var stream = new FileStream(filePath, FileMode.Create);
        file.CopyTo(stream);
        stream.Dispose();
    }

    public void SaveToImages(long workspaceId, string key, Stream stream)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "images");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        using var fileStream = new FileStream(filePath, FileMode.Create);
        stream.CopyTo(fileStream);
        fileStream.Dispose();
    }

    public void SaveToImports(long workspaceId, string key, IFormFile file)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "imports");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        using var stream = new FileStream(filePath, FileMode.Create);
        file.CopyTo(stream);
        stream.Dispose();
    }

    public void SaveToImports(long workspaceId, string key, Stream stream)
    {
        var location = Path.Combine(BaseDirectory, workspaceId.ToString().PadLeft(10, '0'), "imports");
        if (!Directory.Exists(location))
        {
            Directory.CreateDirectory(location);
        }
        var filePath = Path.Combine(location, Cryptography.SHA256Hash(key).ToLower() + ".bin");
        using var fileStream = new FileStream(filePath, FileMode.Create);
        stream.CopyTo(fileStream);
        fileStream.Dispose();
    }
}

public class GoogleFileStorage : IFileStorage
{

    protected string BucketName { get; }
    protected StorageClient Client { get; }
    protected GoogleConfig Config { get; }
    //protected IConfiguration Configuration { get; }

    public GoogleFileStorage(GoogleConfig config)
    {
        var credential = GoogleCredential.FromFile(config.CredentialFile);
        Client = StorageClient.Create(credential);
        BucketName = config.BucketName;
        Config = config;
        //Configuration = configuration;
    }

    public async Task CreateBucket(string name)
    {
        try
        {
            var asd = await Client.GetBucketAsync(BucketName);
            if (asd != null)
                throw new Exception("The bucket already exists");

            var bucket = await Client.CreateBucketAsync(Config.ProjectId, name);
        }
        catch
        {
            throw;
        }
    }

    public async Task DeleteBucket(string name)
    {

        try
        {
            var asd = await Client.GetBucketAsync(BucketName);
            if (asd == null)
                throw new Exception("The bucket does not exists");

            await Client.DeleteBucketAsync(name);
        }
        catch
        {
            throw;
        }
    }

    public async Task AddFileToBucket(string key, Stream file)
    {
        await AddFileToBucket(key, file, BucketName);
    }

    public async Task AddFileToBucket(string key, Stream file, string bucketName)
    {
        await Client.UploadObjectAsync(bucketName, key, null, file);
    }

    protected async Task DeleteFileFromBucket(string key)
    {
        await DeleteFileFromBucket(key, BucketName);
    }

    protected async Task DeleteFileFromBucket(string key, string bucketName)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        await Client.DeleteObjectAsync(bucketName, key);
    }

    public Stream GetFromAttachments(long workspaceId, string key)
    {
        var stream = new MemoryStream();
        var down = Client.DownloadObject(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/attachments/{key.Sha256Hex()}", stream);
        return stream;
    }

    public Stream GetFromExports(long workspaceId, string key)
    {
        var stream = new MemoryStream();
        var down = Client.DownloadObjectAsync(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/exports/{key.Sha256Hex()}", stream).Result;
        return stream;
    }

    public Stream GetFromImages(long workspaceId, string key)
    {
        var stream = new MemoryStream();
        var down = Client.DownloadObjectAsync(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/images/{key.Sha256Hex()}", stream).Result;
        return stream;
    }

    public Stream GetFromImagesThumnail(long workspaceId, string key)
    {
        var stream = new MemoryStream();
        var down = Client.DownloadObjectAsync(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/images/thumbnails/{key.Sha256Hex()}", stream).Result;
        return stream;
    }

    public Stream GetFromImports(long workspaceId, string key)
    {
        var stream = new MemoryStream();
        var down = Client.DownloadObjectAsync(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/imports/{key.Sha256Hex()}", stream).Result;
        return stream;
    }

    public void SaveToAttachments(long workspaceId, string key, IFormFile file)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/attachments/{key.Sha256Hex()}", file.OpenReadStream()).Wait();
    }

    public void SaveToAttachments(long workspaceId, string key, Stream stream)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/attachments/{key.Sha256Hex()}", stream).Wait();
    }

    public void SaveToExports(long workspaceId, string key, IFormFile file)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/exports/{key.Sha256Hex()}", file.OpenReadStream()).Wait();
    }

    public void SaveToExports(long workspaceId, string key, Stream stream)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/exports/{key.Sha256Hex()}", stream).Wait();
    }

    public void SaveToImages(long workspaceId, string key, IFormFile file)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/images/{key.Sha256Hex()}", file.OpenReadStream()).Wait();
    }

    public void SaveToImages(long workspaceId, string key, Stream stream)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/images/{key.Sha256Hex()}", stream).Wait();
    }

    public void SaveToImports(long workspaceId, string key, IFormFile file)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/imports/{key.Sha256Hex()}", file.OpenReadStream()).Wait();
    }

    public void SaveToImports(long workspaceId, string key, Stream stream)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/imports/{key.Sha256Hex()}", stream).Wait();
    }

    public void RemoveFromImports(long workspaceId, string key)
    {
        DeleteFileFromBucket($"{workspaceId.ToString().PadLeft(10, '0')}/imports/{key.Sha256Hex()}").Wait();
    }

    public void RemoveFromExports(long workspaceId, string key)
    {
        DeleteFileFromBucket($"{workspaceId.ToString().PadLeft(10, '0')}/exports/{key.Sha256Hex()}").Wait();
    }

    public void RemoveFromAttachments(long workspaceId, string key)
    {
        DeleteFileFromBucket($"{workspaceId.ToString().PadLeft(10, '0')}/attachments/{key.Sha256Hex()}").Wait();
    }

    public Stream GetFromImagesThumbnail(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public void RemoveFromImages(long workspaceId, string key)
    {
        DeleteFileFromBucket($"{workspaceId.ToString().PadLeft(10, '0')}/images/{key.Sha256Hex()}").Wait();
    }

    public void RemoveFromImagesThumbnail(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public bool ImportsExists(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public bool ExportsExists(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public bool AttachmentsExists(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public bool ImagesExists(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

}

public class AwsS3FileStorage : IFileStorage
{
    protected string BucketName { get; }
    protected IAmazonS3 Client { get; }
    protected IConfiguration Configuration { get; }

    public AwsS3FileStorage(IAmazonS3 client, IConfiguration configuration)
    {
        BucketName = configuration["FileStorage:S3:BucketName"];
        Client = client;
        Configuration = configuration;
    }

    public async Task<AwsS3Response> CreateBucket(string name)
    {
        try
        {
            var existBucket = await AmazonS3Util.DoesS3BucketExistV2Async(Client, Configuration["AWS:S3:BucketPrefix"] + name);

            if (existBucket)
                return new AwsS3Response { Message = "The bucket already exists", Status = HttpStatusCode.Conflict };

            var putBucketRequest = new PutBucketRequest
            {
                BucketName = Configuration["AWS:S3:BucketPrefix"] + name,
                UseClientRegion = true,
            };

            var response = await Client.PutBucketAsync(putBucketRequest);

            return new AwsS3Response { Message = $"Bucket with name {Configuration["AWS:S3:BucketPrefix"] + name} was successfully created", Status = response.HttpStatusCode };
        }
        catch (AmazonS3Exception e)
        {
            return new AwsS3Response
            {
                Message = e.Message,
                Status = e.StatusCode
            };
        }
        catch (Exception e)
        {
            return new AwsS3Response
            {
                Message = e.Message,
                Status = HttpStatusCode.InternalServerError
            };
        }
    }

    public async Task<AwsS3Response> DeleteBucket(string name)
    {
        try
        {
            var existBucket = await AmazonS3Util.DoesS3BucketExistV2Async(Client, Configuration["AWS:S3:BucketPrefix"] + name);

            if (!existBucket)
                return new AwsS3Response { Message = $"Bucket with name {Configuration["AWS:S3:BucketPrefix"] + name} was not found", Status = HttpStatusCode.NotFound };

            var response = await Client.DeleteBucketAsync(Configuration["AWS:S3:BucketPrefix"] + name);

            return new AwsS3Response { Message = response.ResponseMetadata.RequestId, Status = response.HttpStatusCode };
        }
        catch (AmazonS3Exception e)
        {
            return new AwsS3Response { Message = e.Message, Status = e.StatusCode };
        }
        catch (Exception e)
        {
            return new AwsS3Response { Message = e.Message, Status = HttpStatusCode.InternalServerError };
        }
    }
    protected async Task<AwsS3Response> AddFileToBucket(string fileNames)
    {
        return await AddFileToBucket(fileNames, BucketName);
    }

    protected async Task<AwsS3Response> AddFileToBucket(string fileNames, string bucketName)
    {
        try
        {
            var fileTransferUtility = new TransferUtility(Client);
            await fileTransferUtility.UploadAsync(fileNames.Sha256Hex(), bucketName);
        }
        catch (AmazonS3Exception e)
        {
            return new AwsS3Response { Message = e.Message, Status = e.StatusCode };
        }
        catch (Exception e)
        {
            return new AwsS3Response { Message = e.Message, Status = HttpStatusCode.InternalServerError };
        }

        return new AwsS3Response { Message = "File uploaded Successfully", Status = HttpStatusCode.OK };
    }

    protected async Task<AwsS3Response> AddFileToBucket(string key, Stream file)
    {
        return await AddFileToBucket(key, file, BucketName);
    }

    protected async Task<AwsS3Response> AddFileToBucket(string key, Stream file, string bucketName)
    {
        try
        {
            var fileTransferUtility = new TransferUtility(Client);

            // Option 1 (Upload an existing file in your computer to the S3)
            await fileTransferUtility.UploadAsync(file, bucketName, key);
        }
        catch (AmazonS3Exception e)
        {
            return new AwsS3Response { Message = e.Message, Status = e.StatusCode };
        }
        catch (Exception e)
        {
            return new AwsS3Response { Message = e.Message, Status = HttpStatusCode.InternalServerError };
        }

        return new AwsS3Response { Message = "File uploaded Successfully", Status = HttpStatusCode.OK };
    }

    protected async Task<AwsS3Response> DeleteFileFromBucket(string key)
    {
        return await DeleteFileFromBucket(key, BucketName);
    }

    protected async Task<AwsS3Response> DeleteFileFromBucket(string key, string bucketName)
    {
        if (string.IsNullOrEmpty(key))
            key = "test.txt";

        try
        {
            var request = new GetObjectRequest { BucketName = bucketName, Key = key };

            var response = await Client.GetObjectAsync(request);

            if (response == null || response.HttpStatusCode != HttpStatusCode.OK)
                return new AwsS3Response { Message = "Error getting the object from the bucket", Status = HttpStatusCode.NotFound };

            await Client.DeleteObjectAsync(bucketName, key);

            return new AwsS3Response { Message = "The file was successfully deleted", Status = HttpStatusCode.OK };
        }
        catch (AmazonS3Exception e)
        {
            return new AwsS3Response { Message = e.Message, Status = e.StatusCode };
        }
        catch (Exception e)
        {
            return new AwsS3Response { Message = e.Message, Status = HttpStatusCode.InternalServerError };
        }
    }

    protected string GetSignedUrl(string fileName, TimeSpan expiresIn, string method = "GET")
    {
        return GetSignedUrl(fileName, BucketName, expiresIn, method);
    }

    protected string GetSignedUrl(string key, string bucketName, TimeSpan expiresIn, string method = "GET")
    {
        var urlRequest = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddSeconds(expiresIn.TotalSeconds),
            Verb = method.ToUpper() == "GET" ? HttpVerb.GET : HttpVerb.PUT,
            //ResponseHeaderOverrides = headers,
        };

        return Client.GetPreSignedURL(urlRequest);
    }

    public Stream GetFromAttachments(long workspaceId, string key)
    {
        return Client.GetObjectAsync(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/attachments/{key.Sha256Hex()}").Result.ResponseStream;
    }

    public Stream GetFromExports(long workspaceId, string key)
    {
        return Client.GetObjectAsync(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/exports/{key.Sha256Hex()}").Result.ResponseStream;
    }

    public Stream GetFromImages(long workspaceId, string key)
    {
        return Client.GetObjectAsync(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/images/{key.Sha256Hex()}").Result.ResponseStream;
    }

    public Stream GetFromImagesThumnail(long workspaceId, string key)
    {
        return Client.GetObjectAsync(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/images/thumbnails/{key.Sha256Hex()}").Result.ResponseStream;
    }

    public Stream GetFromImports(long workspaceId, string key)
    {
        return Client.GetObjectAsync(BucketName, $"{workspaceId.ToString().PadLeft(10, '0')}/imports/{key.Sha256Hex()}").Result.ResponseStream;
    }

    public void SaveToAttachments(long workspaceId, string key, IFormFile file)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/attachments/{key.Sha256Hex()}", file.OpenReadStream()).Wait();
    }

    public void SaveToAttachments(long workspaceId, string key, Stream stream)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/attachments/{key.Sha256Hex()}", stream).Wait();
    }

    public void SaveToExports(long workspaceId, string key, IFormFile file)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/exports/{key.Sha256Hex()}", file.OpenReadStream()).Wait();
    }

    public void SaveToExports(long workspaceId, string key, Stream stream)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/exports/{key.Sha256Hex()}", stream).Wait();
    }

    public void SaveToImages(long workspaceId, string key, IFormFile file)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/images/{key.Sha256Hex()}", file.OpenReadStream()).Wait();
    }

    public void SaveToImages(long workspaceId, string key, Stream stream)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/images/{key.Sha256Hex()}", stream).Wait();
    }

    public void SaveToImports(long workspaceId, string key, IFormFile file)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/imports/{key.Sha256Hex()}", file.OpenReadStream()).Wait();
    }

    public void SaveToImports(long workspaceId, string key, Stream stream)
    {
        AddFileToBucket($"{workspaceId.ToString().PadLeft(10, '0')}/imports/{key.Sha256Hex()}", stream).Wait();
    }

    public void RemoveFromImports(long workspaceId, string key)
    {
        DeleteFileFromBucket($"{workspaceId.ToString().PadLeft(10, '0')}/imports/{key.Sha256Hex()}").Wait();
    }

    public void RemoveFromExports(long workspaceId, string key)
    {
        DeleteFileFromBucket($"{workspaceId.ToString().PadLeft(10, '0')}/exports/{key.Sha256Hex()}").Wait();
    }

    public void RemoveFromAttachments(long workspaceId, string key)
    {
        DeleteFileFromBucket($"{workspaceId.ToString().PadLeft(10, '0')}/attachments/{key.Sha256Hex()}").Wait();
    }

    public Stream GetFromImagesThumbnail(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public void RemoveFromImages(long workspaceId, string key)
    {
        DeleteFileFromBucket($"{workspaceId.ToString().PadLeft(10, '0')}/images/{key.Sha256Hex()}").Wait();
    }

    public void RemoveFromImagesThumbnail(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public bool ImportsExists(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public bool ExportsExists(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public bool AttachmentsExists(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }

    public bool ImagesExists(long workspaceId, string key)
    {
        throw new NotImplementedException();
    }
}

public class AwsS3Response
{
    public string Message { get; set; }
    public HttpStatusCode Status { get; set; }
    public bool IsHttpStatusCodeSuccess => Status == HttpStatusCode.OK;
}

public class GoogleConfig
{
    public string CredentialFile { get; set; }
    public string BucketName { get; set; }
    public string ProjectId { get; set; }
}
