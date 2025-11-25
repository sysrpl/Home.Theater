using System.Text.Json;

static class Program
{
    static void Send(object[]  command)
    {
        var age = 17;
        var color = "red";
        var options = new { allow = false, volume = 0.75, song = "music.mp3" };
        var s = JsonSerializer.Serialize(new { command, age, color, weight = 34.8, ticked = false,
            options});
        if (JsonSerializer.Deserialize<object>(s) is JsonElement obj)
        {
            Console.WriteLine(obj.GetProperty("command").ToString());
        }
        Console.WriteLine(s);
    }

    static void Main(string[] args)
    {
        var hello = new object[] {"hello", 12};
        Send(hello);
    }
}