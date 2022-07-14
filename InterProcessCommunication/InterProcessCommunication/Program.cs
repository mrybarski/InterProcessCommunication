using InterProcessCommunication;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

const string SHARED_MUTEX_NAME = "INTERPROCESSCOMM_APPS";


Console.WriteLine("Podaj numer aplikacji (1/2)");
var line = Console.ReadLine();
if (string.IsNullOrEmpty(line) && !(line.IndexOf("1") == 0 || line.IndexOf("2") == 0))
{
    Console.WriteLine("Błędny numer aplikacji");
    return;
}

int destinationPort = line[0] == '1' ? 60501 : 60502;
var fileName = CreateLogFile();

var publicIp = await GetPublicIpAddress();
if (publicIp is null)
{
    Console.WriteLine("Nie udało się uzyskać publicznego adresu IP");
    return;
}

using TcpClient tcpClient = new TcpClient();
await tcpClient.ConnectAsync("***REMOVED***", destinationPort);
using NetworkStream netStream = tcpClient.GetStream();

var authCode = GetAuthenticationCode();
if (string.IsNullOrEmpty(authCode))
{
    Console.WriteLine("Nie udało się uzyskać kodu z wiadomości powitalnej");
    return;
}

SendAuthenticationCode();

var thread = new Thread(ProcessMessages) { IsBackground = true };
thread.Start();

Console.WriteLine("Wciśnij dowolny klawisz aby zakończyć");
Console.ReadKey();



string CreateLogFile()
{
    string fileName = $"messages.txt";
    if (!File.Exists(fileName))
    {
        using FileStream fs = System.IO.File.Create(fileName);
    }
    return fileName;
}
async Task<IPAddress?> GetPublicIpAddress()
{
    var httpClient = new HttpClient();
    string externalIpString = await httpClient.GetStringAsync("http://icanhazip.com");
    externalIpString = externalIpString.Replace("\\r\\n", "").Replace("\\n", "").Trim();

    IPAddress.TryParse(externalIpString, out var externalIp);
    return externalIp;
}

string? GetAuthenticationCode()
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

void SendAuthenticationCode()
{
    string source = $"{publicIp}{DateTime.Now.GetUnixTimeStamp()}{authCode}";
    using SHA512 sha512Hash = SHA512.Create();

    byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
    byte[] hashBytes = sha512Hash.ComputeHash(sourceBytes);
    var hash = Convert.ToHexString(hashBytes);
    var test = "1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF1234567890ABCDEF";
    netStream.Write(Encoding.UTF8.GetBytes(test));

    Console.WriteLine(source);
    Console.WriteLine(hash);
    //91.246.215.24616578197598751

}

void ProcessMessages()
{
    byte[] receiveBuffer = new byte[1024];
    EventWaitHandle waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, SHARED_MUTEX_NAME);

    ReadAndWriteToFile(waitHandle);
}

void ReadAndWriteToFile(EventWaitHandle waitHandle)
{
    if (netStream.DataAvailable)
    {
        byte[] receiveBuffer = new byte[1024];
        int bytesReceived = netStream.Read(receiveBuffer);
        var recievedData = receiveBuffer.AsSpan(0, bytesReceived);

        waitHandle.WaitOne();

        using FileStream fs = new(fileName, FileMode.Append);
        fs.Write(recievedData);
        fs.Flush();

        waitHandle.Set();
    }
    Task.Delay(TimeSpan.FromMilliseconds(50), CancellationToken.None)
                    .GetAwaiter()
                    .OnCompleted(() => ReadAndWriteToFile(waitHandle));
}

