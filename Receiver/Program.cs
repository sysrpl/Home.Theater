using System.Net.Sockets;
using System.Text;
using static System.Console;

namespace Receiver;

public static class Program
{
    static string receiverAddress;
    static int receiverPort;

    static string pjlinkAddress;
    static int pjlinkPort;

    static string ReceiverSend(string command)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(receiverAddress, receiverPort, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
            if (!success)
            {
                WriteLine("Unable to connect to receiver: timeout.");
                return string.Empty;
            }
            client.EndConnect(result);

            using var stream = client.GetStream();
            stream.ReadTimeout = 3000;
            stream.WriteTimeout = 3000;

            byte[] data = Encoding.UTF8.GetBytes(command + "\r");
            stream.Write(data, 0, data.Length);

            if (command.EndsWith("?"))
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                try
                {
                    string response = reader.ReadLine();
                    return response?.Trim() ?? string.Empty;
                }
                catch (IOException)
                {
                    WriteLine("No response received from receiver.");
                    return string.Empty;
                }
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            WriteLine($"Receiver error: {ex.Message}");
            return string.Empty;
        }
    }

    static void PJLinkSend(string command)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(pjlinkAddress, pjlinkPort, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
            if (!success)
            {
                WriteLine("Unable to connect to projector: timeout.");
                return;
            }
            client.EndConnect(result);
            //Thread.Sleep(1000);
            Thread.Sleep(500);
            using var stream = client.GetStream();
            stream.ReadTimeout = 3000;
            stream.WriteTimeout = 3000;
            using var reader = new StreamReader(stream, Encoding.ASCII);
            byte[] data = Encoding.ASCII.GetBytes(command + "\r");
            stream.Write(data, 0, data.Length);
            string response = reader.ReadLine();
            response = response?.Trim() ?? string.Empty;
            WriteLine("<< " + response);
            //Thread.Sleep(4000);
            Thread.Sleep(500);
            stream.Write(data, 0, data.Length);
            if (command.Contains("?"))
            {
                response = reader.ReadLine();
                response = response?.Trim() ?? string.Empty;
                WriteLine("<< " + response);
            }
        }
        catch
        {
            WriteLine("Error reading response from projector.");
        }
    }

    static void ReceiverMenu()
    {
        while (true)
        {
            WriteLine();
            WriteLine("Receiver Menu:");
            WriteLine("1) Query Volume");
            WriteLine("2) Set Volume");
            WriteLine("3) Query Input Source");
            WriteLine("4) Set Input Source");
            WriteLine("5) Mute");
            WriteLine("6) Unmute");
            WriteLine("7) Query Mute Status");
            WriteLine("8) Power On");
            WriteLine("9) Power Off");
            WriteLine("10) Query Power Status");
            WriteLine("0) Back to Main Menu");
            Write("Select option: ");

            var choice = ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    WriteLine("Volume: " + ReceiverSend("MV?"));
                    break;
                case "2":
                    Write("Enter volume (00-98): ");
                    string vol = ReadLine()?.Trim() ?? "50";
                    ReceiverSend($"MV{vol}");
                    WriteLine("Volume set.");
                    break;
                case "3":
                    WriteLine("Input Source: " + ReceiverSend("SI?"));
                    break;
                case "4":
                    Write("Enter source (DVD, BD, GAME, TV, CBL, SAT, MEDIA, TUNER, AUX1, AUX2, BT): ");
                    string src = ReadLine()?.Trim() ?? "DVD";
                    ReceiverSend($"SI{src}");
                    WriteLine("Input set.");
                    break;
                case "5":
                    ReceiverSend("MUON");
                    WriteLine("Muted.");
                    break;
                case "6":
                    ReceiverSend("MUOFF");
                    WriteLine("Unmuted.");
                    break;
                case "7":
                    WriteLine("Mute Status: " + ReceiverSend("MU?"));
                    break;
                case "8":
                    ReceiverSend("PWON");
                    WriteLine("Power On command sent.");
                    break;
                case "9":
                    ReceiverSend("PWSTANDBY");
                    WriteLine("Power Off command sent.");
                    break;
                case "10":
                    WriteLine("Power Status: " + ReceiverSend("PW?"));
                    break;
                case "0":
                    return;
                default:
                    WriteLine("Invalid option.");
                    break;
            }
        }
    }

    static void ProjectorMenu()
    {
        while (true)
        {
            WriteLine();
            WriteLine("Projector Menu:");
            WriteLine("1) Power On");
            WriteLine("2) Power Off");
            WriteLine("3) Query Power Status");
            WriteLine("4) Query Input Source");
            WriteLine("5) Set Input Source");
            WriteLine("6) Query Lamp Hours");
            WriteLine("7) Query Device Name");
            WriteLine("8) Query Device Info");
            WriteLine("9) Query PJLink Class");
            WriteLine("0) Back to Main Menu");
            Write("Select option: ");

            var choice = ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    WriteLine("Sending Power On command");
                    PJLinkSend("%1POWR 1");
                    break;
                case "2":
                    WriteLine("Sending Power Off command");
                    PJLinkSend("%1POWR 0");
                    break;
                case "3":
                    WriteLine("Sending Power Status query");
                    PJLinkSend("%1POWR ?");
                    break;
                case "4":
                    WriteLine("Sending Input Source query");
                    PJLinkSend("%1INPT ?");
                    break;
                case "5":
                    Write("Enter input source code (e.g., 11=HDMI1, 12=HDMI2): ");
                    string src = ReadLine()?.Trim() ?? "11";
                    WriteLine("Sending Input command");
                    PJLinkSend($"%1INPT {src}");
                    break;
                case "6":
                    WriteLine("Sending Lamp Hours query");
                    PJLinkSend("%1LAMP ?");
                    break;
                case "7":
                    WriteLine("Sending Device Name query");
                    PJLinkSend("%1NAME ?");
                    break;
                case "8":
                    WriteLine("Sending Device Info query");
                    PJLinkSend("%1INF1 ?");
                    break;
                case "9":
                    WriteLine("Sending PJLink Class query");
                    PJLinkSend("%1CLSS ?");
                    break;
                case "0":
                    return;
                default:
                    WriteLine("Invalid option.");
                    break;
            }
        }
    }

    public static void Main()
    {
        if (!File.Exists("receiver"))
        {
            WriteLine("Receiver address file not found.");
            return;
        }
        receiverAddress = File.ReadAllText("receiver").Trim();
        receiverPort = 23;
        WriteLine($"Receiver address and port: {receiverAddress}:{receiverPort}");
        if (!File.Exists("projector"))
        {
            WriteLine("Projector address file not found.");
            return;
        }
        pjlinkAddress = File.ReadAllText("projector").Trim();
        pjlinkPort = 4352;
        WriteLine($"Projector addess and port: {pjlinkAddress}:{pjlinkPort}");
        while (true)
        {
            WriteLine();
            WriteLine("Main Menu:");
            WriteLine("1) Test Receiver");
            WriteLine("2) Test Projector");
            WriteLine("0) Quit");
            Write("Select option: ");
            var choice = ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    ReceiverMenu();
                    break;
                case "2":
                    ProjectorMenu();
                    break;
                case "0":
                    return;
                default:
                    WriteLine("Invalid option.");
                    break;
            }
        }
    }
}
