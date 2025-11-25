using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Kaleidescape;
using static System.Console;

class Program
{
    static readonly bool isPi = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
        || RuntimeInformation.ProcessArchitecture == Architecture.Arm;

    static async Task Main(string[] args)
    {
        string host;
        WriteLine("Kaleidescape test and configuration tool");
        if (isPi)
        {
            if (!File.Exists("kaleidescape"))
            {
                WriteLine("Kaleidescape address file not found.");
                return;
            }
            host = File.ReadAllText("kaleidescape").Trim();
        }
        else
        {
            host = "192.168.1.149";
        }
        WriteLine($"Kaleidescape address: {host}");
        int deviceId = 1;
        WriteLine();

        var client = new KaleidescapeClient();
        var terminated = false;

        // Subscribe to events
        client.OnConnect += (sender, e) => WriteLine("** connected **");
        client.OnDisconnect += (sender, e) =>
        {
            WriteLine("** disconnected **");
            terminated = true;
        };
        client.OnTimeout += (sender, e) =>
        {
            WriteLine("** connection timeout **");
            terminated = true;
        };
        client.OnMessage += (sender, message) => WriteLine($"   {message}");
        client.OnWait += (sender, e) => WriteLine($"** waiting **");

        WriteLine($"Connecting to {host}");
        await client.ConnectAsync(host);
        if (!client.IsConnected)
        {
            WriteLine("Failed to connect");
            return;
        }
        await Task.Delay(1000);
        WriteLine($"Using device ID: {deviceId:D2}");
        WriteLine("Enter commands (or 'q' to quit):");
        WriteLine("Examples: PLAY, PAUSE, STOP");
        WriteLine();
        WriteLine("GET_SYSTEM_VERSION");
        int tag = await client.SendCommandAsync(deviceId, "GET_SYSTEM_VERSION");
        await Task.Delay(1000);
        WriteLine("GET_AVAILABLE_DEVICES");
        tag = await client.SendCommandAsync(deviceId, "GET_AVAILABLE_DEVICES");
        await Task.Delay(1000);
        WriteLine("GET_AVAILABLE_DEVICES_BY_SERIAL_NUMBER");
        tag = await client.SendCommandAsync(deviceId, "GET_AVAILABLE_DEVICES_BY_SERIAL_NUMBER");
        await Task.Delay(1000);

        void Script()
        {

        }

        // Command history
        var history = LoadHistory();
        int historyIndex = -1;

        while (!terminated)
        {
            string input = ReadLineWithHistory(history, ref historyIndex, ref terminated).Trim();
            if (string.IsNullOrWhiteSpace(input))
                continue;
            if (input.Equals("q", StringComparison.CurrentCultureIgnoreCase))
                break;

            // Remove duplicate from history if it exists, then add to end
            int existingIndex = history.IndexOf(input);
            if (existingIndex >= 0)
            {
                history.RemoveAt(existingIndex);
            }
            history.Add(input);
            SaveHistory(history);

            historyIndex = -1; // Reset history index
            if (input.Equals("script", StringComparison.CurrentCultureIgnoreCase))
            {
                Script();
                continue;
            }
            try
            {
                string command = input;
                string parameters = string.Empty;
                int c = input.IndexOf(':');
                if (c != -1)
                {
                    command = input.Substring(0, c);
                    parameters = input.Substring(c + 1);
                }
                string[] parts = input.Split([':'], 2);
                WriteLine($"{deviceId:D2}/{tag}/{command}:{parameters}");
                tag = await client.SendCommandAsync(deviceId, command, parameters);
            }
            catch (Exception ex)
            {
                WriteLine($"Error: {ex.Message}");
                terminated = true;
            }
        }
        client.Dispose();
    }

    static List<string> LoadHistory()
    {
        var history = new List<string>();
        try
        {
            if (File.Exists("history"))
            {
                var lines = File.ReadAllLines("history");
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        history.Add(trimmed);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            WriteLine($"Warning: Could not load history file: {ex.Message}");
        }
        return history;
    }

    static void SaveHistory(List<string> history)
    {
        try
        {
            File.WriteAllLines("history", history);
        }
        catch (Exception ex)
        {
            WriteLine($"Warning: Could not save history file: {ex.Message}");
        }
    }

    static string ReadLineWithHistory(List<string> history, ref int historyIndex, ref bool terminated)
    {
        var input = new StringBuilder();
        int cursorPos = 0;
        int startColumn = CursorLeft; // Track where the input line starts
        string tempInput = string.Empty; // Store current input when navigating history
        while (!terminated)
        {
            var key = ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    WriteLine();
                    return input.ToString();

                case ConsoleKey.UpArrow:
                    if (history.Count > 0)
                    {
                        // Save current input before first history navigation
                        if (historyIndex == -1)
                        {
                            tempInput = input.ToString();
                            historyIndex = history.Count;
                        }

                        if (historyIndex > 0)
                        {
                            historyIndex--;
                            ClearAndRewrite(input.ToString(), history[historyIndex], startColumn);
                            input.Clear();
                            input.Append(history[historyIndex]);
                            cursorPos = input.Length;
                        }
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (historyIndex != -1)
                    {
                        if (historyIndex < history.Count - 1)
                        {
                            historyIndex++;
                            ClearAndRewrite(input.ToString(), history[historyIndex], startColumn);
                            input.Clear();
                            input.Append(history[historyIndex]);
                            cursorPos = input.Length;
                        }
                        else
                        {
                            // Return to the original input
                            historyIndex = -1;
                            ClearAndRewrite(input.ToString(), tempInput, startColumn);
                            input.Clear();
                            input.Append(tempInput);
                            cursorPos = input.Length;
                        }
                    }
                    break;

                case ConsoleKey.Backspace:
                    if (cursorPos > 0)
                    {
                        input.Remove(cursorPos - 1, 1);
                        cursorPos--;
                        RedrawLine(input.ToString(), cursorPos, startColumn);
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursorPos < input.Length)
                    {
                        input.Remove(cursorPos, 1);
                        RedrawLine(input.ToString(), cursorPos, startColumn);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cursorPos > 0)
                    {
                        cursorPos--;
                        if (CursorLeft > 0)
                            SetCursorPosition(CursorLeft - 1, CursorTop);
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (cursorPos < input.Length)
                    {
                        cursorPos++;
                        SetCursorPosition(CursorLeft + 1, CursorTop);
                    }
                    break;

                case ConsoleKey.Home:
                    if (cursorPos > 0)
                    {
                        SetCursorPosition(startColumn, CursorTop);
                        cursorPos = 0;
                    }
                    break;

                case ConsoleKey.End:
                    if (cursorPos < input.Length)
                    {
                        SetCursorPosition(startColumn + input.Length, CursorTop);
                        cursorPos = input.Length;
                    }
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        input.Insert(cursorPos, key.KeyChar);
                        cursorPos++;
                        RedrawLine(input.ToString(), cursorPos, startColumn);
                    }
                    break;
            }
        }
        return string.Empty;
    }

    static void ClearAndRewrite(string oldText, string newText, int startColumn)
    {
        // Move to start of line
        SetCursorPosition(startColumn, CursorTop);

        // Clear old text by writing spaces
        int maxLength = Math.Max(oldText.Length, newText.Length);
        Write(new string(' ', maxLength));

        // Move back to start and write new text
        SetCursorPosition(startColumn, CursorTop);
        Write(newText);
    }

    static void RedrawLine(string line, int cursorPos, int startColumn)
    {
        int currentTop = CursorTop;

        // Move to start of input
        SetCursorPosition(startColumn, currentTop);

        // Clear and redraw (add extra space to clear any trailing chars)
        Write(line + " ");

        // Position cursor at the correct location
        SetCursorPosition(startColumn + cursorPos, currentTop);
    }
}