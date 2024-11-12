using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NzbDrone.Core.Download.Clients.Qobuz
{
    internal static class MetadataUtilities
    {
        public static string GetFilledTemplate(string template, string ext, JObject qobuzPage, JObject qobuzAlbum)
        {
            var releaseDate = DateTime.Parse(qobuzAlbum["releaseDate"]!.ToString(), CultureInfo.InvariantCulture);
            return GetFilledTemplate_Internal(template,
                qobuzPage["title"]!.ToString(),
                qobuzPage["album"]!["title"]!.ToString(),
                qobuzAlbum["artist"]!["name"]!.ToString(),
                qobuzPage["artist"]!["name"]!.ToString(),
                qobuzAlbum["artists"]!.Select(a => a["name"]!.ToString()).ToArray(),
                qobuzPage!["artists"]!.Select(a => a["name"]!.ToString()).ToArray(),
                $"{(int)qobuzPage["trackNumber"]!:00}",
                qobuzAlbum["numberOfTracks"]!.ToString(),
                releaseDate.Year.ToString(CultureInfo.InvariantCulture),
                ext);
        }

        private static string GetFilledTemplate_Internal(string template, string title, string album, string albumArtist, string artist, string[] albumArtists, string[] artists, string track, string trackCount, string year, string ext)
        {
            StringBuilder t = new(template);
            ReplaceC("%title%", title);
            ReplaceC("%album%", album);
            ReplaceC("%albumartist%", albumArtist);
            ReplaceC("%artist%", artist);
            ReplaceC("%albumartists%", string.Join("; ", albumArtists));
            ReplaceC("%artists%", string.Join("; ", artists));
            ReplaceC("%track%", track);
            ReplaceC("%trackcount%", trackCount);
            ReplaceC("%ext%", ext);
            ReplaceC("%year%", year);

            return t.ToString();

            void ReplaceC(string o, string r)
            {
                t.Replace(o, CleanPath(r));
            }
        }

        public static string CleanPath(string str)
        {
            var invalid = Path.GetInvalidFileNameChars();
            for (var i = 0; i < invalid.Length; i++)
            {
                var c = invalid[i];
                str = str.Replace(c, '_');
            }
            return str;
        }
    }
}
