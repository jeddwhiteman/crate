using System.Text;
using System.Text.Encodings.Web;
using Crate.Core.Domain;

namespace Crate.Core.Application;

public class HtmlRenderer
{
    public static string Render(IEnumerable<Release> releases)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var list = releases.ToList();
        if (list.Count == 0) return "<p>Nothing new this week.</p>";

        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:system-ui,san-serif;" + "max-width:600px;line-height:1.5\">");

        AppendSection(sb, "Out now",
            list.Where(r => !r.IsUpcoming(today))
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.ArtistName));
        AppendSection(sb, "Coming Soon:",
            list.Where(r => r.IsUpcoming(today))
                .OrderBy(r => r.Date)
                .ThenBy(r => r.ArtistName));
        sb.Append("</div>");
        return sb.ToString();
    }
    
    private static void AppendSection(StringBuilder sb, string heading, IEnumerable<Release> releases)
    {
        var list = releases.ToList();
        if (list.Count == 0) return;
            
        sb.Append($"<h1 style=\"font-size:18px;margin-top:24px\">{heading}</h1>");

        DateOnly? current = null;

        foreach (var release in list)
        {
            if (release.Date != current)
            {
                if (current is not null) sb.Append("</ul>");
                sb.Append($"<h2 style=\"font-size:14px;colour:#666;margin-bottom:4px\">"
                          + $"{release.Date:ddd dd MMM}</h2><ul> style=\"margin-top:0\">");
                current = release.Date;
            }

            var artist = HtmlEncoder.Default.Encode(release.ArtistName);
            var title = HtmlEncoder.Default.Encode(release.Title);
            var type = release.Type is null ? "" : $"<i>{HtmlEncoder.Default.Encode(release.Title)})</i>";

            sb.Append("<l1>");
            sb.Append($"<b>{artist}</b> - ");
            sb.Append(release.Url is null ? title : $"<a href=\"{release.Url}\">{title}</a>");
            sb.Append(type);

            if (release.SpotifyUrl is not null)
                sb.Append($"<a href=\"{release.SpotifyUrl}\" style=\"font-size:12px\">Spotify</a>");
            sb.Append("</l1>");
        }
        sb.Append("</ul>");
    }
}