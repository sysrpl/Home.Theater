using System.Net;
using Kaleidescape;
using static System.Console;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Kaleidescape test and configuration tool");
        WriteLine();
        Write("Host address: ");
        string host = ReadLine()?.Trim() ?? "";
        var valid = IPAddress.TryParse(host, out IPAddress ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        if (!valid)
        {
            WriteLine("Invalid host address");
            return;
        }
        Write("Device id [1]: ");
        string deviceInput = ReadLine()?.Trim() ?? "";
        int deviceId = string.IsNullOrEmpty(deviceInput) ? 1 : int.Parse(deviceInput);
        WriteLine();

        var client = new KaleidescapeClient();

        // Subscribe to events
        client.OnConnect += (sender, e) => WriteLine("** connected **");
        client.OnDisconnect += (sender, e) => WriteLine("** disconnected **");
        client.OnTimeout += (sender, e) => WriteLine("** connection timeout **");
        client.OnMessage += (sender, message) => WriteLine($"<< {message}");

        // Try to connect
        WriteLine($"Connecting to {host}");
        await client.ConnectAsync(host);

        if (!client.IsConnected)
        {
            WriteLine("Failed to connect");
            return;
        }

        WriteLine($"Using device ID: {deviceId:D2}");
        WriteLine("Enter commands (or 'q' to quit):");
        WriteLine("Examples: PLAY, PAUSE, STOP");
        WriteLine();
        WriteLine("> GET_SYSTEM_VERSION");
        int tag = await client.SendCommandAsync(deviceId, "GET_SYSTEM_VERSION");
        await Task.Delay(1000);
        WriteLine("> GET_AVAILABLE_DEVICES");
        tag = await client.SendCommandAsync(deviceId, "GET_AVAILABLE_DEVICES");
        await Task.Delay(1000);
        WriteLine("> GET_AVAILABLE_DEVICES_BY_SERIAL_NUMBER");
        tag = await client.SendCommandAsync(deviceId, "GET_AVAILABLE_DEVICES_BY_SERIAL_NUMBER");
        await Task.Delay(1000);

        while (true)
        {
            Write("> ");
            string input = ReadLine().Trim();
            if (string.IsNullOrWhiteSpace(input))
                continue;
            if (input.Equals("q", StringComparison.CurrentCultureIgnoreCase))
                break;
            try
            {
                // Parse command and optional parameters
                string[] parts = input.Split([' '], 2);
                string command = parts[0].ToUpper();
                string parameters = parts.Length > 1 ? parts[1] : string.Empty;
                WriteLine($">> {deviceId:D2}/{tag}/{command}:{parameters}");
                tag = await client.SendCommandAsync(deviceId, command, parameters);
            }
            catch (Exception ex)
            {
                WriteLine($"Error: {ex.Message}");
            }
        }

        WriteLine("Disconnecting...");
        client.Dispose();
    }
}