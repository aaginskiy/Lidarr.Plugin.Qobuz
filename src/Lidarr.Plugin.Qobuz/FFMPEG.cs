using System;
using System.Diagnostics;

namespace NzbDrone.Core.Plugins;

internal class FFMPEG
{
    public static string[] ProbeCodecs(string filePath)
    {
        var (exitCode, output) = Call("ffprobe", $"-select_streams a -show_entries stream=codec_name:stream_tags=language -of default=nk=1:nw=1 \"{filePath}\"");
        if (exitCode != 0)
            throw new FFMPEGException($"Probing codecs failed\n{output}");

        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static void ConvertWithoutReencode(string input, string output)
    {
        var (exitCode, _) = Call("ffmpeg", $"-i \"{input}\" -vn -acodec copy \"{output}\"");
        if (exitCode != 0)
            throw new FFMPEGException($"Conversion without re-encode failed\n{output}");
    }

    public static void Reencode(string input, string output, int bitrate)
    {
        var (exitCode, _) = Call("ffmpeg", $"-i \"{input}\" -b:a {bitrate}k \"{output}\"");
        if (exitCode != 0)
            throw new FFMPEGException($"Re-encoding failed\n{output}");
    }

    private static (int exitCode, string output) Call(string executable, string arguments)
    {
        using var proc = new Process()
        {
            StartInfo = new()
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        proc.Start();
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        return (proc.ExitCode, output);
    }
}

public class FFMPEGException : Exception
{
    public FFMPEGException() { }
    public FFMPEGException(string message) : base(message) { }
    public FFMPEGException(string message, Exception inner) : base(message, inner) { }
}
