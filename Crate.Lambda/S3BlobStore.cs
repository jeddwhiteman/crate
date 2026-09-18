using Amazon.S3;
using Amazon.S3.Model;
using Crate.Core.Abstractions;

namespace Crate.Lambda;

public class S3BlobStore(IAmazonS3 s3, string bucket) : IBlobStore
{
    public async Task<string?> ReadBlobAsync(string key)
    {
        try
        {
            using var res = await s3.GetObjectAsync(bucket, key);
            using var reader = new StreamReader(res.ResponseStream);
            return await reader.ReadToEndAsync();

        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    public async Task WriteBlobAsync(string key, string content)
    {
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            ContentBody = content
        });
    }
}