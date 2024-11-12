using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Http;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Plugin.Qobuz.API;

namespace NzbDrone.Core.Indexers.Qobuz
{
    public class QobuzParser : IParseIndexerResponse
    {
        public QobuzIndexerSettings Settings { get; set; }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse response)
        {
            // TODO: parse releases
            return [];
            /*var torrentInfos = new List<ReleaseInfo>();
            var content = new HttpResponse<QobuzSearchResponse>(response.HttpResponse).Content;

            var jsonResponse = JObject.Parse(content).ToObject<QobuzSearchResponse>();
            var releases = jsonResponse.AlbumResults.Items.Select(result => ProcessAlbumResult(result)).ToArray();

            foreach (var task in releases)
            {
                torrentInfos.AddRange(task);
            }

            foreach (var track in jsonResponse.TrackResults.Items)
            {
                // make sure the album hasn't already been processed before doing this
                if (!jsonResponse.AlbumResults.Items.Any(a => a.Id == track.Album.Id))
                {
                    var processTrackTask = ProcessTrackAlbumResultAsync(track);
                    processTrackTask.Wait();
                    torrentInfos.AddRange(processTrackTask.Result);
                }
            }

            return torrentInfos
                .OrderByDescending(o => o.Size)
                .ToArray();*/
        }

        private static ReleaseInfo ToReleaseInfo(QobuzSearchResponse.Album x, AudioQuality bitrate)
        {
            var publishDate = DateTime.UtcNow;
            var year = 0;
            if (DateTime.TryParse(x.ReleaseDate, out var digitalReleaseDate))
            {
                publishDate = digitalReleaseDate;
                year = publishDate.Year;
            }

            var url = x.Url;

            var result = new ReleaseInfo
            {
                Guid = $"Qobuz-{x.Id}-{bitrate}",
                Artist = x.Artists.First().Name,
                Album = x.Title,
                DownloadUrl = url,
                InfoUrl = url,
                PublishDate = publishDate,
                DownloadProtocol = nameof(QobuzDownloadProtocol)
            };

            string format;
            switch (bitrate)
            {
                case AudioQuality.MP3320:
                    result.Codec = "MP3";
                    result.Container = "320";
                    format = "MP3 320kbps";
                    break;
                case AudioQuality.FLACLossless:
                    result.Codec = "FLAC";
                    result.Container = "Lossless";
                    format = "FLAC Lossless";
                    break;
                case AudioQuality.FLACHiRes24Bit96kHz:
                    result.Codec = "FLAC";
                    result.Container = "Hi-Res";
                    format = "FLAC Hi-Res 96kHz";
                    break;
                case AudioQuality.FLACHiRes24Bit192Khz:
                    result.Codec = "FLAC";
                    result.Container = "Hi-Res";
                    format = "FLAC Hi-Res 192kHz";
                    break;
                default:
                    throw new NotImplementedException();
            }

            // TODO: determine if getting size is possible
            var size = 0;
            /*var bps = bitrate switch
            {
                AudioQuality.HI_RES_LOSSLESS => 1152000,
                AudioQuality.HI_RES => 576000,
                AudioQuality.LOSSLESS => 176400,
                AudioQuality.HIGH => 40000,
                AudioQuality.LOW => 12000,
                _ => 40000
            };
            var size = x.Duration * bps;*/

            result.Size = size;
            result.Title = $"{x.Artists.First().Name} - {x.Title}";

            if (year > 0)
            {
                result.Title += $" ({year})";
            }

            if (x.Explicit)
            {
                result.Title += " [Explicit]";
            }

            result.Title += $" [{format}] [WEB]";

            return result;
        }
    }
}
