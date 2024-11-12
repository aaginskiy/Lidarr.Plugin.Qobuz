using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NzbDrone.Core.Download.Clients.Qobuz.Queue;

namespace NzbDrone.Plugin.Qobuz.API;

public class QobuzURL(string url, EntityType type, string id)
{
    private static readonly Regex[] DownloadUrlRegExes = {
            new("https:\\/\\/(?:.*?).qobuz.com\\/(?<Type>.*?)\\/(?<id>.*?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new("https:\\/\\/(?:.*?).qobuz.com\\/(?:.*?)\\/(?<Type>.*?)\\/(?<Slug>.*?)\\/(?<AlbumsTag>download-streaming-albums)\\/(?<id>.*?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new("https:\\/\\/(?:.*?).qobuz.com\\/(?:.*?)\\/(?<Type>.*?)\\/(?<Slug>.*?)\\/(?<id>.*?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };
    private static readonly string[] LinkTypes = { "album", "track", "artist", "label", "user", "playlist", "interpreter" };

    public string Url { get; init; } = url;
    public EntityType EntityType { get; init; } = type;
    public string Id { get; init; } = id;

    public static bool TryParse(string url, out QobuzURL tidalUrl)
    {
        try
        {
            tidalUrl = Parse(url);
            return true;
        }
        catch
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            tidalUrl = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            return false;
        }
    }

    public static QobuzURL Parse(string url)
    {
        int paramStart = url.IndexOf('?');
        if (paramStart != -1)
            url = url[..paramStart];

        EntityType? type = null;
        string id = null;

        foreach (Regex regEx in DownloadUrlRegExes)
        {
            Match matches = regEx.Match(url);

            if (matches.Success)
            {
                if (!LinkTypes.Contains(matches.Result("${Type}"))) continue;

                // Valid Type found, set DownloadItem values
                string typeStr = matches.Result("${Type}");
                id = matches.Result("${id}")?.TrimEnd('/');

                // In store links, "interpreter" = "artist"
                if (typeStr == "interpreter") typeStr = "artist";
                type = Enum.Parse<EntityType>(typeStr, true);

                break;
            }
        }

        if (type == null || id == null)
            throw new Exception("Invalid URL provided.");

        return new QobuzURL(url, type.Value, id);
    }
}

public enum EntityType
{
    Track,
    Playlist,
    Album,
    Artist,
    Label,
    User
}
