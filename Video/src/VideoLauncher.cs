namespace Video;

using System.Diagnostics;

public class VideoLauncher
{
    public static Process Start(string streamUrl, string socketPath = "/tmp/mpvsocket")
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "mpv",
            Arguments = $"--input-ipc-server={socketPath} \"{streamUrl}\"",
            UseShellExecute = false,
            CreateNoWindow = false
        };
        return Process.Start(startInfo);
    }
}