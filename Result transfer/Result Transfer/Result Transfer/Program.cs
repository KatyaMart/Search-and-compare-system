using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Result_Transfer
{
    class Program
    {
        static void Main(string[] args)
        {
            //IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            //IPAddress ipAddr = ipHost.AddressList[0];
            //IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11001);

            //Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            TcpListener listener = new TcpListener(11001);
            listener.Start();
            try
            {
                Socket handler = listener.AcceptSocket();
                //sListener.Bind(ipEndPoint);
                //sListener.Listen(10);
                while (true)
                {
                    //handler = sListener.Accept();
                    //string data = null;
                    byte[] bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    int msg_type = bytes[0];
                    int i=1;
                    if (msg_type == 1)
                    {
                        string login = Encoding.UTF8.GetString(bytes, i + 1, bytes[1]);
                        i += login.Length + 1;
                        string password = Encoding.UTF8.GetString(bytes, i + 1, bytes[1]);

                    // запрос к базе
                        List<byte> list_byte = new List<byte>();
                        Dictionary<int, string> subscribers = new Dictionary<int, string>();
                        for (int j = 0; j < subscribers.Count; j++)
                        {
                            list_byte.AddRange(BitConverter.GetBytes(subscribers.ElementAt(j).Key));
                            int leng = subscribers.ElementAt(j).Value.Length;
                            list_byte.Add((byte)leng);
                            list_byte.AddRange(Encoding.UTF8.GetBytes(subscribers.ElementAt(j).Value));
                        }
                        byte[] msg = list_byte.ToArray();
                        int bytesSend = handler.Send(msg);
                        if (bytesSend < msg.Length)
                            Console.WriteLine("Сообщение передалось не полностью!");
                    }
                    else if (msg_type == 3)
                    {
                        string login = Encoding.UTF8.GetString(bytes, i + 1, bytes[1]);
                        i += login.Length + 1;
                        string password = Encoding.UTF8.GetString(bytes, i + 1, bytes[1]);
                        i += password.Length + 1;
                        int key = BitConverter.ToInt32(bytes,i);

                        //запрос к базе
                        List<string> references = new List<string>();
                        List<byte> msg_list = new List<byte>();
                        msg_list.Add(3);
                        foreach (string refer in references)
                        {
                            msg_list.AddRange(BitConverter.GetBytes(refer.Length));
                            msg_list.AddRange(Encoding.UTF8.GetBytes(refer));
                        }
                        byte[] msg = msg_list.ToArray();
                        //шифрование

                        int bytesSend = handler.Send(msg);
                        if (bytesSend < msg.Length)
                            Console.WriteLine("Сообщение передалось не полностью!");
                    }
                    //======================2 тип сообщения===============================
                    /*foreach(IPAddress addr in addr_list)
                    {
                        List<string> references = new List<string>();
                        List<byte> msg_list = new List<byte>();
                        msg_list.Add(2);
                        foreach (string refer in references)
                        {
                            msg_list.AddRange(BitConverter.GetBytes(refer.Length));
                            msg_list.AddRange(Encoding.UTF8.GetBytes(refer));
                        }
                        byte[] msg = msg_list.ToArray();
                        //шифрование

                        int bytesSend = handler.Send(msg);
                        if (bytesSend < msg.Length)
                            Console.WriteLine("Сообщение передалось не полностью!");
                    }*/



                    /*if (data.IndexOf("<TheEnd>") > -1)
                    {
                        Console.WriteLine("Сервер завершил соединение с клиентом.");
                        break;
                    }*/
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }
    }
}
