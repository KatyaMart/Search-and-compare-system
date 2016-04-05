using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.IO;

namespace TCPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11000);

            Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                sListener.Bind(ipEndPoint);
                sListener.Listen(10);
                while (true)
                {
                    Socket handler = sListener.Accept();
                    string data = null;
                    byte[] bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    int log_len = (int)bytes[0];
                    string login_auth = Encoding.UTF8.GetString(bytes, 1, log_len);
                    string password_auth = Encoding.UTF8.GetString(bytes, log_len + 2, (int)bytes[log_len + 1]);
                    //data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    byte[] ans = new byte[1];
                    if (login_auth == "ddf" && password_auth == "1234")
                    {

                        ans[0] = 1;
                        handler.Send(ans);
                        byte[] buffer = new byte[1048576];
                        byte[] shifr = new byte[1];
                        NetworkStream ns = new NetworkStream(handler);
                        ns.Read(shifr, 0, 1);
                        ns.Read(buffer, 0, 1024 * 1024 - 1);


                        int i = 0;
                        int leng = BitConverter.ToInt32(buffer, 0);
                        string login = Encoding.UTF8.GetString(buffer, 0 + sizeof(int), leng);
                        i += leng + sizeof(int);
                        leng = BitConverter.ToInt32(buffer, i);
                        i += sizeof(int);
                        string password = Encoding.UTF8.GetString(buffer, i, leng);
                        i += leng;
                        byte priznak = buffer[i];
                        i++;
                        double accurancy = 0.0f;
                        if (priznak != 1)
                        {
                            i++;
                            accurancy = BitConverter.ToDouble(buffer, i);
                            i += sizeof(double);
                        }
                        leng = BitConverter.ToInt32(buffer, i);
                        i += sizeof(int);

                        MemoryStream ms = new MemoryStream();
                        ms.Write(buffer, i, leng);
                        Image img = Image.FromStream(ms);
                        img.Save(@"result.png", System.Drawing.Imaging.ImageFormat.Png);

                        Console.WriteLine("Логин: " + login);
                        Console.WriteLine("Пароль:" + password);
                        Console.WriteLine("Вид результата: " + priznak);
                        Console.WriteLine("Точность" + accurancy);
                        Console.WriteLine("Размер картинки" + leng);
                    }
                    /*byte[] bytes = new byte[102400];
                    int bytesRec = handler.Receive(bytes);

                    int log_len = (int)bytes[0];
                    string login = Encoding.UTF8.GetString(bytes, 1, log_len);
                    string password = Encoding.UTF8.GetString(bytes, log_len + 2, (int)bytes[log_len + 1]);
                    //data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    byte[] ans = new byte[1];
                    if (login == "ddf" && password == "1234")
                    {

                        ans[0] = 1;
                        handler.Send(ans);
                        bytesRec = handler.Receive(bytes);
                        MemoryStream ms = new MemoryStream();
                        ms.Write(bytes, 0, bytesRec);
                        Image img = Image.FromStream(ms);
                        img.Save(@"result.png", System.Drawing.Imaging.ImageFormat.Png);
                        Console.Write("Получено изображение\n\n");

                        string reply = "Спасибо за запрос в " + bytesRec.ToString() + "байт";

                        byte[] msg = Encoding.UTF8.GetBytes(reply);
                        handler.Send(msg);
                    }
                    else
                    {
                        ans[0] = 0;
                        handler.Send(ans);
                    }
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
