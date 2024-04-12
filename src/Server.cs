using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;

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
            Byte[] bytes = new Byte[256];
            String data = null;

            while(true)
            {
                using TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                data = null;

                NetworkStream stream = client.GetStream();

                int i;

                while((i = stream.Read(bytes, 0, bytes.Length))!=0)
                {
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("Received: {0}", data);

                    Dictionary<string, string> parsedData = ParseHeaders(data);

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
                        default:
                            data = "HTTP/1.1 404 Not Found\r\n\r\n";
                            break;
                    }

                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                    stream.Write(msg, 0, msg.Length);
                    Console.WriteLine("Sent: {0}", data);
                }
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

        for (int i = 1; i < requestLines.Length; i++)
        {
            var parts = requestLines[i].Split(':');
            if (parts.Length == 2)
            {
                parsedData.Add(parts[0].Trim(), parts[1].Trim());
            }
        }
        return parsedData;
    }
}