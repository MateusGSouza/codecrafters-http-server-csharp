using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

class Server
{
    static void Main(string[] args)
    {
        TcpListener server = null;
        int port = 4221;
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            Console.WriteLine($"Started server at port {port}");

            while(true)
            {
                server.BeginAcceptSocket(HandleConnect, server);
            }
         }
        catch(SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            server.Stop();
        }

    }

    static Dictionary<string, string> ParseHeaders(string requestData)
    {   
        var requestLines = requestData.Split("\r\n");
        string[] httpStartLine = requestLines[0].Split(" ");

        var parsedData = new Dictionary<string, string>();
        parsedData.Add("Method", httpStartLine[0]);
        parsedData.Add("Path", httpStartLine[1]);
        parsedData.Add("Protocol", httpStartLine[2]);

        bool hasBody = false;
        var stringBuilder = new StringBuilder();
        for (int i = 1; i < requestLines.Length; i++)
        {
            var parts = requestLines[i].Split(':');
            if (parts.Length == 2)
            {
                parsedData.Add(parts[0].Trim(), parts[1].Trim());
            }
            else if (string.IsNullOrWhiteSpace(requestLines[i]) && hasBody == false)
            {
                hasBody = true;
                continue;
            }

            if (hasBody == true)
            {
                stringBuilder.Append(requestLines[i]);
            }
        }
        parsedData.Add("Body", stringBuilder.ToString().Replace("\0", ""));
        return parsedData;
    }

    static void HandleConnect(IAsyncResult result)
    {
        try
        {
            var listener = (TcpListener)result.AsyncState;
            var socket = listener.EndAcceptSocket(result);

            var buffer = new byte[1024];
            var bytes = socket.Receive(buffer);

            var data = Encoding.UTF8.GetString(buffer);

            Console.WriteLine("Received: {0}", data);

            Dictionary<string, string> parsedData = ParseHeaders(data);

            bool sendFile = false;
            string fullPath = null;
            switch (parsedData["Path"])
            {
                case "/":
                    data = "HTTP/1.1 200 OK\r\n\r\n";
                    break;

                case string path when path.Contains("/echo"):
                    string echoText = parsedData["Path"].Substring("/echo/".Length);
                    data = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {echoText.Length}\r\n\r\n{echoText}\r\n\r\n";
                    break;

                case "/user-agent":
                    if (parsedData.ContainsKey("User-Agent"))
                    {
                        string userAgent = parsedData["User-Agent"];
                        data = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n{userAgent}\r\n\r\n";
                    }
                    break;

                case string path when path.Contains("/files"):
                    string fileName = parsedData["Path"].Substring("/files/".Length);
                    string[] args = Environment.GetCommandLineArgs();

                    fullPath = Path.Join(args[2], fileName);

                    if (parsedData["Method"] == "POST")
                    {
                        System.IO.File.WriteAllText(fullPath, parsedData["Body"]);
                        data = $"HTTP/1.1 201 Created\r\n\r\n";
                        break;
                    }

                    Console.WriteLine($"File full path: {fullPath}");
                    if (File.Exists(fullPath))
                    {
                        byte[] fileContent = File.ReadAllBytes(fullPath);
                        data = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileContent.Length}\r\n\r\n";
                        sendFile = true;
                    }
                    else
                    {
                        data = "HTTP/1.1 404 File Not Found\r\n\r\n";
                    }
                    break;

                default:
                    data = "HTTP/1.1 404 Not Found\r\n\r\n";
                    break;
            }
            var msg = Encoding.UTF8.GetBytes(data);

            socket.Send(msg);
            if (sendFile)
            {
                socket.SendFile(fullPath);
            }
            socket.Close();
            Console.WriteLine($"Sent: {data}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e}");
        }
    }
}