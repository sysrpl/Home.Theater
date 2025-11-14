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

    static string PJLinkSend(string command)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(pjlinkAddress, pjlinkPort, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
            if (!success)
            {
                WriteLine("Unable to connect to projector: timeout.");
                return string.Empty;
            }
            client.EndConnect(result);

            using var stream = client.GetStream();
            stream.ReadTimeout = 3000;
            stream.WriteTimeout = 3000;

            byte[] data = Encoding.ASCII.GetBytes(command + "\r");
            stream.Write(data, 0, data.Length);

            using var reader = new StreamReader(stream, Encoding.ASCII);
            try
            {
                string response = reader.ReadLine();
                return response?.Trim() ?? string.Empty;
            }
            catch (IOException)
            {
                WriteLine("No response received from projector.");
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            WriteLine($"Projector error: {ex.Message}");
            return string.Empty;
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
                    PJLinkSend("%1POWR 1");
                    WriteLine("Power On command sent.");
                    break;
                case "2":
                    PJLinkSend("%1POWR 0");
                    WriteLine("Power Off command sent.");
                    break;
                case "3":
                    WriteLine("Power Status: " + PJLinkSend("%1POWR ?"));
                    break;
                case "4":
                    WriteLine("Input Source: " + PJLinkSend("%1INPT ?"));
                    break;
                case "5":
                    Write("Enter input source code (e.g., 11=HDMI1, 12=HDMI2): ");
                    string src = ReadLine()?.Trim() ?? "11";
                    PJLinkSend($"%1INPT {src}");
                    WriteLine("Input set.");
                    break;
                case "6":
                    string lampHours = PJLinkSend("%1LAMP ?");
                    WriteLine("Lamp Hours: " + lampHours);
                    break;
                case "7":
                    string name = PJLinkSend("%1NAME ?");
                    WriteLine("Device Name: " + name);
                    break;
                case "8":
                    string info = PJLinkSend("%1INF1 ?");
                    WriteLine("Device Info: " + info);
                    break;
                case "9":
                    string pjclass = PJLinkSend("%1CLSS ?");
                    WriteLine("PJLink Class: " + pjclass);
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

        if (!File.Exists("projector"))
        {
            WriteLine("Projector address file not found.");
            return;
        }
        pjlinkAddress = File.ReadAllText("projector").Trim();
        pjlinkPort = 4352;

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
