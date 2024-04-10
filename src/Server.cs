using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;

class Server
{
    static void Main(string[] args)
    {
        TcpListener server = null;
        try
        {
            server = new TcpListener(IPAddress.Any, 4221);
            server.Start();

            Console.WriteLine("Started server at port 4221");
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

                    // data = data.ToUpper();
                    data = "HTTP/1.1 200 OK\r\n\r\n"

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

// TODO send TCP request