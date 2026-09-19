using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using Crate.Core.Domain;

namespace Crate.Core.Application;

public class HtmlRenderer
{
    private const string NoReleasedMessage = "no music out this week my g - go touch grass";
    private const string NoUpcomingMessage = "no upcoming music this week g, go listen to hood classics.";

    private static readonly string EmailTemplate = LoadTemplate("Email.html");
    private static readonly string RowTemplate = LoadTemplate("Row.html");

    private static string LoadTemplate(string name)
    {
        var resourceName = $"Crate.Core.Application.Templates.{name}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string Render(IEnumerable<Release> releases)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var list = releases.ToList();

        var released = RenderSection(
            list.Where(r => !r.IsUpcoming(today)).OrderByDescending(r => r.Date).ThenBy(r => r.ArtistName),
            NoReleasedMessage);
        var upcoming = RenderSection(
            list.Where(r => r.IsUpcoming(today)).OrderBy(r => r.Date).ThenBy(r => r.ArtistName),
            NoUpcomingMessage);

        return EmailTemplate
            .Replace("{{RELEASED_CONTENT}}", released)
            .Replace("{{UPCOMING_CONTENT}}", upcoming);
    }

    private static string RenderSection(IEnumerable<Release> releases, string emptyMessage)
    {
        var list = releases.ToList();
        if (list.Count == 0) return Row("t-line", emptyMessage);

        var sb = new StringBuilder();
        DateOnly? current = null;

        foreach (var release in list)
        {
            if (release.Date != current)
            {
                sb.Append(Row("t-date", $"{release.Date.ToString("ddd d MMM", CultureInfo.InvariantCulture)}:"));
                current = release.Date;
            }

            sb.Append(Row("t-line", FormatLine(release)));
        }

        return sb.ToString();
    }

    private static string FormatLine(Release release)
    {
        var artist = HtmlEncoder.Default.Encode(release.ArtistName);
        var title = HtmlEncoder.Default.Encode(release.Title);
        var type = release.Type is null ? "" : $" <i>{HtmlEncoder.Default.Encode(release.Type)}</i>";

        var line = new StringBuilder();
        line.Append($"{artist} - ");
        line.Append(release.Url is null ? title : $"<a href=\"{release.Url}\">{title}</a>");
        line.Append(type);

        if (release.SpotifyUrl is not null)
            line.Append($" <a href=\"{release.SpotifyUrl}\" style=\"font-size:12px\">Spotify</a>");

        return line.ToString();
    }

    private static string Row(string cssClass, string content) =>
        RowTemplate.Replace("{{CLASS}}", cssClass).Replace("{{CONTENT}}", content);
}
