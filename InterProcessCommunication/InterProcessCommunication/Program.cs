using InterProcessCommunication;
using InterProcessCommunication.Extensions;
using System.Net;
using System.Net.Sockets;
using System.Text;


var fileStream = new InterProcessFileStream("messages.txt");

Console.WriteLine("Podaj adres IP aplikacji");
var addressIpRaw = Console.ReadLine();
if (!IPAddress.TryParse(addressIpRaw, out var ipAddress))
{
    Console.WriteLine("Błędny adres IP");
    return;
}

Console.WriteLine("Podaj numer portu");
var portRaw = Console.ReadLine();
if (!int.TryParse(portRaw, out var destinationPort))
{
    Console.WriteLine("Błędny numer portu");
    Console.ReadKey();
    return;
}

fileStream.CreateFile();

Console.WriteLine("Wyczyścić plik z wiadomościami? (y/n)");
var line = Console.ReadLine();
if (line.HasReplyForQuestion("y", "n") && line![0] == 'y')
{
    fileStream.ClearFile();
}

var publicIp = await GetPublicIpAddress();
if (publicIp is null)
{
    Console.WriteLine("Nie udało się uzyskać publicznego adresu IP");
    Console.ReadKey();
    return;
}

using TcpClient tcpClient = new();
await tcpClient.ConnectAsync(ipAddress, destinationPort);
using NetworkStream netStream = tcpClient.GetStream();

var authCode = GetCode();
if (string.IsNullOrEmpty(authCode))
{
    Console.WriteLine("Nie udało się uzyskać kodu z wiadomości powitalnej");
    Console.ReadKey();
    return;
}

SendHash();

var thread = new Thread(ProcessMessages) { IsBackground = true };
thread.Start();

Console.WriteLine("Wciśnij dowolny klawisz aby zakończyć");
Console.ReadKey();


async Task<IPAddress?> GetPublicIpAddress()
{
    var httpClient = new HttpClient();
    string externalIpString = await httpClient.GetStringAsync("http://icanhazip.com");
    externalIpString = externalIpString.Replace("\\r\\n", "").Replace("\\n", "").Trim();

    IPAddress.TryParse(externalIpString, out var externalIp);
    return externalIp;
}

string? GetCode()
{
    byte[] receiveBuffer = new byte[1024];
    int bytesReceived = netStream.Read(receiveBuffer);
    string data = Encoding.UTF8.GetString(receiveBuffer.AsSpan(0, bytesReceived));
    netStream.Flush();
    data = data.Trim();
    var codeIndex = data.Length - 4;
    var code = data[codeIndex..];

    return code;
}

void SendHash()
{
    string source = $"{publicIp}{DateTime.Now.GetUnixTimeStamp()}{authCode}";
    var hash = source.CreateSHA512();
    var bytes = Encoding.UTF8.GetBytes(hash);
    netStream.Write(bytes);
}

void ProcessMessages()
{
    if (netStream.DataAvailable)
    {
        byte[] receiveBuffer = new byte[1024];
        int bytesReceived = netStream.Read(receiveBuffer);
        var recievedData = receiveBuffer.AsSpan(0, bytesReceived);

        fileStream.AppendToFile(recievedData);
    }
    Task.Delay(TimeSpan.FromMilliseconds(50), CancellationToken.None)
                    .GetAwaiter()
                    .OnCompleted(() => ProcessMessages());
}


