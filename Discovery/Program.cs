using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

static class Program
{
    private const string MulticastAddress = "239.255.255.250";
    private const int MulticastPort = 1900;
    private const int ListenTimeoutSeconds = 5;

    static async Task Main(string[] args)
    {
        Console.WriteLine("SSDP Discovery Tool");
        Console.WriteLine("==================");
        Console.WriteLine($"Searching for devices on local network...\n");

        var devices = new HashSet<string>();

        using (var client = new UdpClient())
        {
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            // Build M-SEARCH request
            string searchMessage =
                "M-SEARCH * HTTP/1.1\r\n" +
                $"HOST: {MulticastAddress}:{MulticastPort}\r\n" +
                "MAN: \"ssdp:discover\"\r\n" +
                "MX: 3\r\n" +
                "ST: ssdp:all\r\n" +
                "\r\n";

            byte[] searchBytes = Encoding.UTF8.GetBytes(searchMessage);
            IPEndPoint multicastEndpoint = new IPEndPoint(IPAddress.Parse(MulticastAddress), MulticastPort);

            // Send M-SEARCH request
            await client.SendAsync(searchBytes, searchBytes.Length, multicastEndpoint);
            Console.WriteLine("M-SEARCH request sent. Listening for responses...\n");

            // Listen for responses
            client.Client.ReceiveTimeout = ListenTimeoutSeconds * 1000;
            DateTime startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < ListenTimeoutSeconds)
            {
                try
                {
                    UdpReceiveResult result = await client.ReceiveAsync();
                    string response = Encoding.UTF8.GetString(result.Buffer);
                    string deviceKey = $"{result.RemoteEndPoint.Address}:{response}";

                    if (devices.Add(deviceKey))
                    {
                        Console.WriteLine($"Device Found at {result.RemoteEndPoint.Address}");
                        Console.WriteLine(new string('-', 60));
                        ParseAndDisplayResponse(response);
                        Console.WriteLine();
                    }
                }
                catch (SocketException)
                {
                    // Timeout or no more responses
                    break;
                }
            }
        }

        Console.WriteLine($"\nDiscovery complete. Found {devices.Count} unique device(s).");
    }

    static void ParseAndDisplayResponse(string response)
    {
        var lines = response.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
                continue;

            if (line.Contains(":"))
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    // Highlight important fields
                    if (key.Equals("LOCATION", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("SERVER", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("USN", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("ST", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"  {key,-15}: {value}");
                    }
                }
            }
        }
    }
}