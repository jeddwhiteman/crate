using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Crate.Core.Application;
using Crate.Core.Persistence;
using Crate.Core.Sources;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]


namespace Crate.Lambda;

public class Function
{
    
    private static readonly IAmazonSimpleSystemsManagement Ssm = new AmazonSimpleSystemsManagementClient();
    private static readonly IAmazonS3 S3 = new AmazonS3Client();
    private static readonly IAmazonSimpleEmailServiceV2 Ses = new AmazonSimpleEmailServiceV2Client();
    
    private static readonly string Bucket = Environment.GetEnvironmentVariable("STATE_BUCKET")!;
    private static readonly string MailFrom = Environment.GetEnvironmentVariable("MAIL_FROM")!;
    private static readonly string MailTo = Environment.GetEnvironmentVariable("MAIL_TO")!;

    public async Task<string> FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogInformation("Crate starting");
        
        // Read insider handler not statically.
        // containers, so a rotated value wouldn't be picked up.

        var contact = await SecretAsync("contact_email");

        var blobs = new S3BlobStore(S3, Bucket);
        var repo = new JsonArtistRepository(blobs);
        var seen = new JsonSeenReleaseRepository(blobs);
        var musicBrainzClient = new MusicBrainzClient("1.0", contact);
        var source = new MusicBrainzReleaseSource(musicBrainzClient);
        var result = await CrateRunner.RunAsync(source, repo, seen);
        
        context.Logger.LogInformation($"Found={result.TotalFound} fresh={result.FreshCount}");

        if (result.FreshCount == 0)
        {
            context.Logger.LogInformation("Nothing new, no email sent my g.");
            return "no releases";
        }

        await SendAsync($"crate - {result.FreshCount} new releases", result.Html);
        context.Logger.LogInformation("Email sent.");

        return $"sent {result.FreshCount}";


    }
    
    private static async Task<string> SecretAsync(string name)
    {
        var res = await Ssm.GetParameterAsync(new GetParameterRequest
        {
            Name           = $"/crate/{name}",
            WithDecryption = true
        });
        return res.Parameter.Value;
    }

    private static async Task SendAsync(String subject, string html)
    {
        await Ses.SendEmailAsync(new SendEmailRequest
        {
            FromEmailAddress = MailFrom,
            Destination = new Destination { ToAddresses = [MailTo] },
            Content = new EmailContent()
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body
                    {
                        Html = new Content { Data = $"<html><body>{html}</body></html>" }
                    }
                }
            }
        });
    }

}