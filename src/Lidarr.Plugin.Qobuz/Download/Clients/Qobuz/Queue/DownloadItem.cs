using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Plugins;
using NzbDrone.Plugin.Qobuz.API;

namespace NzbDrone.Core.Download.Clients.Qobuz.Queue
{
    public class DownloadItem
    {
        public static async Task<DownloadItem> From(RemoteAlbum remoteAlbum)
        {
            var url = remoteAlbum.Release.DownloadUrl.Trim();
            var quality = remoteAlbum.Release.Container switch
            {
                "320" => AudioQuality.MP3320,
                "Lossless" => AudioQuality.FLACLossless,
                "Hi-Res 96kHz" => AudioQuality.FLACHiRes24Bit96kHz,
                "Hi-Res 192kHz" => AudioQuality.FLACHiRes24Bit192Khz,
                _ => AudioQuality.MP3320,
            };

            DownloadItem item = null;
            if (url.Contains("qobuz", StringComparison.CurrentCultureIgnoreCase))
            {
                if (QobuzURL.TryParse(url, out var qobuzUrl))
                {
                    item = new()
                    {
                        ID = Guid.NewGuid().ToString(),
                        Status = DownloadItemStatus.Queued,
                        Bitrate = quality,
                        RemoteAlbum = remoteAlbum,
                        _qobuzUrl = qobuzUrl,
                    };

                    await item.SetQobuzData();
                }
            }

            return item;
        }

        public string ID { get; private set; }

        public string Title { get; private set; }
        public string Artist { get; private set; }
        public bool Explicit { get; private set; }

        public RemoteAlbum RemoteAlbum {  get; private set; }

        public string DownloadFolder { get; private set; }

        public AudioQuality Bitrate { get; private set; }
        public DownloadItemStatus Status { get; set; }

        public float Progress { get => DownloadedSize / (float)Math.Max(TotalSize, 1); }
        public long DownloadedSize { get; private set; }
        public long TotalSize { get; private set; }

        public int FailedTracks { get; private set; }

        private (string id, int chunks)[] _tracks;
        private QobuzURL _qobuzUrl;
        private JObject _qobuzAlbum;
        private DateTime _lastARLValidityCheck = DateTime.MinValue;

        public async Task DoDownload(QobuzSettings settings, Logger logger, CancellationToken cancellation = default)
        {
            List<Task> tasks = new();
            using SemaphoreSlim semaphore = new(3, 3);
            foreach (var (trackId, trackSize) in _tracks)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellation);
                    try
                    {
                        await DoTrackDownload(trackId, settings, cancellation);
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        logger.Error("Error while downloading Qobuz track " + trackId);
                        logger.Error(ex.ToString());
                        FailedTracks++;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellation));
            }

            await Task.WhenAll(tasks);
            if (FailedTracks > 0)
                Status = DownloadItemStatus.Failed;
            else
                Status = DownloadItemStatus.Completed;
        }

        private async Task DoTrackDownload(string track, QobuzSettings settings, CancellationToken cancellation = default)
        {
            // TODO: download
            await Task.Delay(100);
            /*var page = await QobuzAPI.Instance.Client.API.GetTrack(track, cancellation);
            var songTitle = page["title"]!.ToString();
            var artistName = page["artist"]!["name"]!.ToString();
            var albumTitle = page["album"]!["title"]!.ToString();
            var duration = page["duration"]!.Value<int>();

            var ext = (await QobuzAPI.Instance.Client.Downloader.GetExtensionForTrack(track, Bitrate)).TrimStart('.');
            var outPath = Path.Combine(settings.DownloadPath, MetadataUtilities.GetFilledTemplate("%albumartist%/%album%/", ext, page, _qobuzAlbum), MetadataUtilities.GetFilledTemplate("%track% - %title%.%ext%", ext, page, _qobuzAlbum));
            var outDir = Path.GetDirectoryName(outPath)!;

            DownloadFolder = outDir;
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            await QobuzAPI.Instance.Client.Downloader.WriteRawTrackToFile(track, Bitrate, outPath, (i) => DownloadedSize++, cancellation);
            outPath = HandleAudioConversion(outPath, settings);

            var plainLyrics = string.Empty;
            string syncLyrics = null;

            var lyrics = await QobuzAPI.Instance.Client.Downloader.FetchLyricsFromQobuz(track, cancellation);
            if (lyrics.HasValue)
            {
                plainLyrics = lyrics.Value.plainLyrics;

                if (settings.SaveSyncedLyrics)
                    syncLyrics = lyrics.Value.syncLyrics;
            }

            if (settings.UseLRCLIB && (string.IsNullOrWhiteSpace(plainLyrics) || (settings.SaveSyncedLyrics && !(syncLyrics?.Any() ?? false))))
            {
                lyrics = await QobuzAPI.Instance.Client.Downloader.FetchLyricsFromLRCLIB("lrclib.net", songTitle, artistName, albumTitle, duration, cancellation);
                if (lyrics.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(plainLyrics))
                        plainLyrics = lyrics.Value.plainLyrics;
                    if (settings.SaveSyncedLyrics && !(syncLyrics?.Any() ?? false))
                        syncLyrics = lyrics.Value.syncLyrics;
                }
            }

            await QobuzAPI.Instance.Client.Downloader.ApplyMetadataToFile(track, outPath, MediaResolution.s640, plainLyrics, token: cancellation);

            if (syncLyrics != null)
                await CreateLrcFile(Path.Combine(outDir, MetadataUtilities.GetFilledTemplate("%track% - %title%.%ext%", "lrc", page, _qobuzAlbum)), syncLyrics);*/

            // TODO: this is currently a waste of resources, if this pr ever gets merged, it can be reenabled
            // https://github.com/Lidarr/Lidarr/pull/4370
            /* try
            {
                string artOut = Path.Combine(outDir, "folder.jpg");
                if (!File.Exists(artOut))
                {
                    byte[] bigArt = await QobuzAPI.Instance.Client.Downloader.GetArtBytes(page["DATA"]!["ALB_PICTURE"]!.ToString(), 1024, cancellation);
                    await File.WriteAllBytesAsync(artOut, bigArt, cancellation);
                }
            }
            catch (UnavailableArtException) { } */
        }

        private string HandleAudioConversion(string filePath, QobuzSettings settings)
        {
            if (!settings.ExtractFlac && !settings.ReEncodeAAC)
                return filePath;

            var codecs = FFMPEG.ProbeCodecs(filePath);
            if (codecs.Contains("flac") && settings.ExtractFlac)
            {
                var newFilePath = Path.ChangeExtension(filePath, "flac");
                try
                {
                    FFMPEG.ConvertWithoutReencode(filePath, newFilePath);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    return newFilePath;
                }
                catch (FFMPEGException)
                {
                    if (File.Exists(newFilePath))
                        File.Delete(newFilePath);
                    return filePath;
                }
            }

            if (codecs.Contains("aac") && settings.ReEncodeAAC)
            {
                var newFilePath = Path.ChangeExtension(filePath, "mp3");
                try
                {
                    var tagFile = TagLib.File.Create(filePath);
                    var bitrate = tagFile.Properties.AudioBitrate;
                    tagFile.Dispose();

                    FFMPEG.Reencode(filePath, newFilePath, bitrate);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    return newFilePath;
                }
                catch (FFMPEGException)
                {
                    if (File.Exists(newFilePath))
                        File.Delete(newFilePath);
                    return filePath;
                }
            }

            return filePath;
        }

        private async Task SetQobuzData(CancellationToken cancellation = default)
        {
            // TODO: data parsing
            await Task.Delay(100);
            /*if (_qobuzUrl.EntityType != EntityType.Album)
                throw new InvalidOperationException();

            var album = await QobuzAPI.Instance.Client.API.GetAlbum(_qobuzUrl.Id, cancellation);
            var albumTracks = await QobuzAPI.Instance.Client.API.GetAlbumTracks(_qobuzUrl.Id, cancellation);

            var tracksTasks = albumTracks["items"]!.Select(async t =>
            {
                var chunks = await QobuzAPI.Instance.Client.Downloader.GetChunksInTrack(t["id"]!.ToString(), Bitrate, cancellation);
                return (t["id"]!.ToString(), chunks);
            }).ToArray();

            var tracks = await Task.WhenAll(tracksTasks);
            _tracks ??= tracks;

            _qobuzAlbum = album;

            Title = album["title"]!.ToString();
            Artist = album["artist"]!["name"]!.ToString();
            Explicit = album["explicit"]!.Value<bool>();
            TotalSize = _tracks.Sum(t => t.chunks);*/
        }

        private static async Task CreateLrcFile(string lrcFilePath, string syncLyrics)
        {
            await File.WriteAllTextAsync(lrcFilePath, syncLyrics);
        }
    }
}
