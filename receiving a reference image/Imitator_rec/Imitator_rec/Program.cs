using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.IO;

namespace TCPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                SendMessageFromSocket(11000);
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
        static void SendMessageFromSocket(int port)
        {
            byte[] bytes = new byte[1024];

            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

            Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            sender.Connect(ipEndPoint);
            Console.WriteLine("Введите логин для аутентификации: ");
            string login_auth = Console.ReadLine();
            Console.WriteLine("Введите пароль для аутентификации: ");
            string pass_auth = Console.ReadLine();

            // Console.WriteLine("Сокет соединяется с {0}",sender.RemoteEndPoint.ToString());
            byte[] log = Encoding.UTF8.GetBytes(login_auth);
            int log_len = log.Length;
            byte[] password = Encoding.UTF8.GetBytes(pass_auth);
            int pas_len = password.Length;
            byte[] mes = new byte[log_len + pas_len + 2];
            mes[0] = (byte)log_len;
            log.CopyTo(mes, 1);
            mes[log_len + 1] = (byte)pas_len;
            password.CopyTo(mes, log_len + 2);
            int bytesSent = sender.Send(mes);

            int bytesRec = sender.Receive(bytes);
            if (bytes[0] == 1)
            {
                Console.WriteLine("Доступ разрешен!\n");
                /*Console.WriteLine("Введите логин: ");
                string login = Console.ReadLine();
                Console.WriteLine("Введите пароль: ");
                string pass = Console.ReadLine();*/
                Console.WriteLine("Введите '1' для задание количества результата и любой другой символ для задания точности:");
                string numRes = Console.ReadLine();
                double accurancy = 0.0f;
                int num_res = 0;
                if (numRes != "1")
                {
                    Console.WriteLine("Укажите точность:");
                    accurancy = Convert.ToDouble(Console.ReadLine());
                }
                else
                {
                    Console.WriteLine("Укажите количество результатов:");
                    num_res = Convert.ToInt32(Console.ReadLine());
                }
                
                Image img = Image.FromFile(@"cart.png");
                ImageConverter conv = new ImageConverter();
                byte[] img_byte;
                    img_byte = (byte[])conv.ConvertTo(img,typeof(byte[]));
                //MemoryStream memoryStream = new MemoryStream();
                //memoryStream.Position = 0;
                //img.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                
                //byte[] img_byte = memoryStream.ToArray();

                int msg_len = 1 + 1 + sizeof(int) + 1+ img_byte.Length;
                if (accurancy == 0)
                    msg_len += sizeof(int) ;
                else
                    msg_len+= sizeof(double);
                byte[] msg = new byte[msg_len];
                int i = 1;
                if (accurancy == 0)
                {
                    msg[i] = 1;
                    i++;
                    BitConverter.GetBytes(num_res).CopyTo(msg, i);
                    i += sizeof(int);
                }
                else
                {
                    msg[i] = 0;
                    i++;
                    BitConverter.GetBytes(accurancy).CopyTo(msg, i);
                    i += sizeof(double);
                }
                BitConverter.GetBytes(img_byte.Length).CopyTo(msg, i);
                i += sizeof(int);
                msg[i] = 0;
                i++;
                img_byte.CopyTo(msg, i);
                bytesSent = sender.Send(msg);
                Console.WriteLine("Сообщение отправлено, было отправлено "+ bytesSent + " байт\n");
            }
            else
            {
                Console.WriteLine("Отказано в доступе");
            }



            /*int bytesRec = sender.Receive(bytes);
            if (bytes[0] == 1)
            {
                Console.WriteLine("Вам надо 1 результат или несколько? нажмите '1' для одного и остальное для нескольких:");
                string numRes = Console.ReadLine();
                if (numRes != "1")
                {
                    Console.WriteLine("Укажите точность:");
                    int accurancy = Console.Read();
                }
                    
                Image img = Image.FromFile(@"cart.png");
                MemoryStream memoryStream = new MemoryStream();
                img.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] msg = memoryStream.ToArray();
                bytesSent = sender.Send(msg);
                bytesRec = sender.Receive(bytes);
                Console.WriteLine("\nОтвет от сервера: {0}\n\n", Encoding.UTF8.GetString(bytes, 0, bytesRec));
            }
            else if (bytes[0] == 0)
            {
                Console.WriteLine("Неверный логин!\n");
            }*/

            //if (message.IndexOf("<TheEnd>") == -1)
            //   SendMessageFromSocket(port);
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }
    }
}
