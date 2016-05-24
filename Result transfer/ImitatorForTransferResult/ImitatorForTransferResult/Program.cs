using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Imitator_for_transfer_result
{
    class Program
    {
        
        static void Main(string[] args)
        {
            Thread t = new Thread(Listen2msg);
            t.Start();
            
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11001);

            Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                sender.Connect(ipEndPoint);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Невозможно подключиться к серверу!");
                Console.ReadLine();
                return;
            }
            try
            {
                List<byte> list_byte = new List<byte>();
                list_byte.Add(0);           //Шифрование
                list_byte.Add(1);           //Тип сообщения
                string login;
                do
                {
                    Console.WriteLine("Введите логин: ");
                    login = Console.ReadLine();
                    if (login.Length > 127)
                        Console.WriteLine("Слишком длинный логин! Повторите");
                } while (login.Length > 127);
                list_byte.Add((byte)login.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(login));
                Console.WriteLine("Введите пароль: ");
                string password = Console.ReadLine();
                list_byte.Add((byte)password.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(password));
                byte[] msg = list_byte.ToArray();
                sender.Send(msg);
                list_byte.Clear();

                //Ожидание ответа
                byte[] bytes = new byte[10240];
                int bytesRec = sender.Receive(bytes);

                if (bytes[1] == 1)
                {
                    int i = 2;
                    while (i < bytesRec)
                    {
                        string userName;
                        int id = BitConverter.ToInt32(bytes, i);
                        i += sizeof(Int32);
                        int leng = bytes[i];
                        userName = Encoding.UTF8.GetString(bytes, i + 1, leng);
                        i += 1 + leng;
                        Console.WriteLine(id + ": " + userName);
                    }
                }
                else if (bytes[1] == 101)
                {
                    Console.WriteLine("Отказано в доступе!");
                }
                sender.Close();

                //=================================отправка 3его типа сообщения====================

                list_byte.Add((byte)0);           //Шифрование
                list_byte.Add((byte)3);           //Тип сообщения
                do
                {
                    Console.WriteLine("Введите логин: ");
                    login = Console.ReadLine();
                    if (login.Length > 127)
                        Console.WriteLine("Слишком длинный логин! Повторите");
                } while (login.Length > 127);
                list_byte.Add((byte)login.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(login));
                Console.WriteLine("Введите пароль: ");
                password = Console.ReadLine();
                list_byte.Add((byte)password.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(password));
                Console.WriteLine("Введите логин нужного пользователя: ");
                string user = Console.ReadLine();
                list_byte.Add((byte)user.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(user));
                msg = list_byte.ToArray();
                sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(ipEndPoint);
                sender.Send(msg);


                bytes = new byte[10240];
                bytesRec = sender.Receive(bytes);
                if (bytes[1] == 3)
                {
                    int i = 2;
                    while (i < bytesRec)
                    {
                        int length = BitConverter.ToInt32(bytes, i);
                        i += sizeof(Int32);
                        string reference = Encoding.UTF8.GetString(bytes, i, length);
                        i += length;
                        Console.WriteLine(reference);
                    }
                }
                else if (bytes[1] == 103)
                {
                    Console.WriteLine("Отказано в доступе");
                }

                sender.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.ReadLine();
        }
        static void Listen2msg()
        {
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11002);
            TcpListener listener = new TcpListener(ipEndPoint);
            listener.Start();

            try
            {
                Socket handler = listener.AcceptSocket();
                byte[] bytes = new byte[10240];
                int bytesRec = handler.Receive(bytes);
                if (bytes[1] == 2)
                {
                    int i=2;
                    string login = Encoding.UTF8.GetString(bytes,i+1,bytes[i]);
                    i += login.Length + 1;
                    Console.WriteLine("Пользователь "+login+" добавид результаты:");
                    while(i<bytesRec)
                    {
                        string refer = Encoding.UTF8.GetString(bytes, i + 1, bytes[i]);
                        i += refer.Length + 1;
                        Console.WriteLine(refer);
                    }
                }
                else
                {
                    Console.WriteLine("Посторонее сообщение");
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
