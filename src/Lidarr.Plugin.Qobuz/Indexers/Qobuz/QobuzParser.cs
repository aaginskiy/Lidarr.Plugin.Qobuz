using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Http;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Plugin.Qobuz.API;
using QobuzApiSharp.Models.Content;

namespace NzbDrone.Core.Indexers.Qobuz
{
    public class QobuzParser : IParseIndexerResponse
    {
        public QobuzIndexerSettings Settings { get; set; }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse response)
        {
            var torrentInfos = new List<ReleaseInfo>();
            var content = new HttpResponse<SearchResult>(response.HttpResponse).Content;

            var jsonResponse = JObject.Parse(content).ToObject<SearchResult>();
            var releases = jsonResponse.Albums.Items.Select(result => ProcessAlbumResult(result)).ToArray();

            foreach (var task in releases)
            {
                torrentInfos.AddRange(task);
            }

            return torrentInfos
                .OrderByDescending(o => o.Size)
                .ToArray();
        }

        private IEnumerable<ReleaseInfo> ProcessAlbumResult(Album result)
        {
            // determine available audio qualities
            List<AudioQuality> qualityList = new() { AudioQuality.MP3320, AudioQuality.FLACLossless };

            if ((result.Hires ?? false) && (result.HiresStreamable ?? false))
            {
                qualityList.Add(AudioQuality.FLACHiRes24Bit192Khz);
                qualityList.Add(AudioQuality.FLACHiRes24Bit96kHz);
            }

            return qualityList.Select(q => ToReleaseInfo(result, q));
        }

        private static ReleaseInfo ToReleaseInfo(Album x, AudioQuality bitrate)
        {
            var publishDate = DateTime.UtcNow;
            var year = 0;
            if (x.ReleaseDateOriginal != null)
            {
                publishDate = x.ReleaseDateOriginal.Value.DateTime;
                year = publishDate.Year;
            }

            var url = x.Url;

            var result = new ReleaseInfo
            {
                Guid = $"Qobuz-{x.Id}-{bitrate}",
                Artist = x.Artist.Name,
                Album = x.CompleteTitle,
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
                    result.Container = "Hi-Res 96kHz";
                    format = "FLAC Hi-Res 96kHz";
                    break;
                case AudioQuality.FLACHiRes24Bit192Khz:
                    result.Codec = "FLAC";
                    result.Container = "Hi-Res 192kHz";
                    format = "FLAC Hi-Res 192kHz";
                    break;
                default:
                    throw new NotImplementedException();
            }

            // estimates as there isn't an efficient way to get sizes
            var size = 0L;
            var bps = bitrate switch
            {
                AudioQuality.MP3320 => 320000,
                AudioQuality.FLACLossless => 1411200,
                AudioQuality.FLACHiRes24Bit96kHz => 4608000,
                AudioQuality.FLACHiRes24Bit192Khz => 9216000,
                _ => 320000
            };
            size = x.Duration.Value * bps;

            result.Size = size;
            result.Title = $"{x.Artist.Name} - {x.CompleteTitle}";

            if (year > 0)
            {
                result.Title += $" ({year})";
            }

            if (x.ParentalWarning.GetValueOrDefault())
            {
                result.Title += " [Explicit]";
            }

            result.Title += $" [{format}] [WEB]";

            return result;
        }
    }
}
