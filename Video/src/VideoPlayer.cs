namespace Video;

using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public class VideoPlayer
{
    private readonly string _socketPath;

    public VideoPlayer(string socketPath = "/tmp/mpvsocket")
    {
        _socketPath = socketPath;
    }

    private JsonDocument SendCommand(params object[] command)
    {
        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        socket.Connect(new UnixDomainSocketEndPoint(_socketPath));
        var json = JsonSerializer.Serialize(new { command });
        var data = Encoding.UTF8.GetBytes(json + "\n");
        socket.Send(data);
        var buffer = new byte[4096];
        var bytesReceived = socket.Receive(buffer);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
        return JsonDocument.Parse(response);
    }

    public void Pause()
    {
        SendCommand("set_property", "pause", true);
    }

    public void Resume()
    {
        SendCommand("set_property", "pause", false);
    }

    public void TogglePause()
    {
        SendCommand("cycle", "pause");
    }

    public void Stop()
    {
        SendCommand("stop");
    }

    public void Quit()
    {
        SendCommand("quit");
    }

    public void SeekRelative(int seconds)
    {
        SendCommand("seek", seconds, "relative");
    }

    public void SeekAbsolute(int seconds)
    {
        SendCommand("seek", seconds, "absolute");
    }

    public double GetPosition()
    {
        var response = SendCommand("get_property", "time-pos");
        return response.RootElement.GetProperty("data").GetDouble();
    }

    public double GetDuration()
    {
        var response = SendCommand("get_property", "duration");
        return response.RootElement.GetProperty("data").GetDouble();
    }

    public void SetVolume(int volume)
    {
        SendCommand("set_property", "volume", volume);
    }
}