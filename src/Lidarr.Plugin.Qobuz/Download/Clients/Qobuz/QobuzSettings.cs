using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Download.Clients.Qobuz
{
    public class QobuzSettingsValidator : AbstractValidator<QobuzSettings>
    {
        public QobuzSettingsValidator()
        {
            RuleFor(x => x.DownloadPath).IsValidPath();
        }
    }

    public class QobuzSettings : IProviderConfig
    {
        private static readonly QobuzSettingsValidator Validator = new QobuzSettingsValidator();

        [FieldDefinition(0, Label = "Download Path", Type = FieldType.Textbox)]
        public string DownloadPath { get; set; } = "";

        [FieldDefinition(1, Label = "Extract FLAC From M4A", HelpText = "Extracts FLAC data from the Qobuz-provided M4A files.", HelpTextWarning = "This requires FFMPEG and FFProbe to be available to Lidarr.", Type = FieldType.Checkbox)]
        public bool ExtractFlac { get; set; } = false;

        [FieldDefinition(2, Label = "Re-encode AAC into MP3", HelpText = "Re-encodes AAC data from the Qobuz-provided M4A files into MP3s.", HelpTextWarning = "This requires FFMPEG and FFProbe to be available to Lidarr.", Type = FieldType.Checkbox)]
        public bool ReEncodeAAC { get; set; } = false;

        [FieldDefinition(3, Label = "Save Synced Lyrics", HelpText = "Saves synced lyrics to a separate .lrc file if available. Requires .lrc to be allowed under Import Extra Files.", Type = FieldType.Checkbox)]
        public bool SaveSyncedLyrics { get; set; } = false;

        [FieldDefinition(4, Label = "Use LRCLIB as Backup Lyric Provider", HelpText = "If Qobuz does not have plain or synced lyrics for a track, the plugin will attempt to get them from LRCLIB.", Type = FieldType.Checkbox)]
        public bool UseLRCLIB { get; set; } = false;

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
