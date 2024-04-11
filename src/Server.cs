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

                    string httpStartLine = data.Split("\r\n")[0];
                    string requestPath = httpStartLine.Split(" ")[1];

                    if (requestPath == "/")
                    {
                        data = "HTTP/1.1 200 OK\r\n\r\n";
                    } else
                    {
                        data = "HTTP/1.1 404 Not Found\r\n\r\n";
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
}