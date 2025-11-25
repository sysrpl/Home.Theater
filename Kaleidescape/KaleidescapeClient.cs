namespace Kaleidescape;

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class KaleidescapeClient : IDisposable
{
    private TcpClient client;
    private NetworkStream stream;
    private CancellationTokenSource cancel;
    private Task readTask;
    private int sequenceId = 1;
    private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);

    public event EventHandler OnConnect;
    public event EventHandler OnDisconnect;
    public event EventHandler OnTimeout;
    public event EventHandler<string> OnMessage;
    public event EventHandler OnWait;

    public bool IsConnected => client?.Connected ?? false;

    public async Task ConnectAsync(string host, int port = 10000)
    {
        const int timeout = 3000;
        client = new TcpClient();
        using (var connect = new CancellationTokenSource(timeout))
        {
            try
            {
                await client.ConnectAsync(host, port).WaitAsync(connect.Token);
            }
            catch
            {
                client?.Dispose();
                OnTimeout?.Invoke(this, EventArgs.Empty);
                return;
            }
        }
        OnConnect?.Invoke(this, EventArgs.Empty);
        stream = client.GetStream();
        cancel = new CancellationTokenSource();
        readTask = Task.Run(() => ReadLoopAsync(cancel.Token), cancel.Token);
    }

    public async Task<int> SendCommandAsync(int deviceId, string command, string parameters = "")
    {
        if (!IsConnected)
            throw new InvalidOperationException("Not connected");
        // Get next sequence ID (wraps from 9 to 1)
        int seqId = GetNextSequenceId();
        // Build message: 01/5/PLAY:
        // Device ID is zero-padded
        string message = $"{deviceId:D2}/{seqId}/{command}:{parameters}\r\n";
        byte[] data = Encoding.ASCII.GetBytes(message);
        // Thread-safe write
        await writeLock.WaitAsync();
        try
        {
            await stream.WriteAsync(data, 0, data.Length);
        }
        finally
        {
            writeLock.Release();
        }

        return seqId;
    }

    private int GetNextSequenceId()
    {
        int current = Interlocked.Increment(ref sequenceId);
        // Wrap from 9 back to 1
        if (current > 9)
        {
            Interlocked.CompareExchange(ref sequenceId, 1, current);
            return 1;
        }
        return current;
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        byte[] buffer = new byte[4096];
        var messageBuilder = new StringBuilder();
        DateTime lastLineReadTime = DateTime.MinValue;
        bool waitEventFired = true;

        // Start a monitoring task
        var monitorTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(10, token);

                if (lastLineReadTime != DateTime.MinValue &&
                    !waitEventFired &&
                    (DateTime.UtcNow - lastLineReadTime).TotalSeconds >= 0.2)
                {
                    OnWait?.Invoke(this, EventArgs.Empty);
                    waitEventFired = true;
                }
            }
        }, token);

        try
        {
            while (!token.IsCancellationRequested && stream != null)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0)
                {
                    // Connection closed
                    break;
                }

                string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                messageBuilder.Append(data);

                // Process complete messages (lines ending with \r\n)
                string accumulated = messageBuilder.ToString();
                int lineEnd;
                bool foundLine = false;

                while ((lineEnd = accumulated.IndexOf("\r\n")) >= 0)
                {
                    string message = accumulated.Substring(0, lineEnd);
                    accumulated = accumulated.Substring(lineEnd + 2);

                    // Fire event with complete message
                    OnMessage?.Invoke(this, message);
                    foundLine = true;
                }

                // Update tracking if we found at least one line
                if (foundLine)
                {
                    lastLineReadTime = DateTime.UtcNow;
                    waitEventFired = false;
                }

                messageBuilder.Clear();
                messageBuilder.Append(accumulated);
            }
        }
        catch
        {
        }

        OnDisconnect?.Invoke(this, EventArgs.Empty);
    }

    public void Disconnect()
    {
        cancel?.Cancel();
        readTask?.Wait(TimeSpan.FromSeconds(2));
        stream?.Close();
        client?.Close();
    }

    public void Dispose()
    {
        Disconnect();
        cancel?.Dispose();
        stream?.Dispose();
        client?.Dispose();
        writeLock?.Dispose();
    }
}