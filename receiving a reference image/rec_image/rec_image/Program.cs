using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace TCPServer
{
    class Program
    {
        static long MAX_SIZE = 5*1024*1024;
        static string BaseAddress = "http://localhost:12000/";
        static string baseLogin;
        static string basePassword;
        static int encryptionType = 0;
        
        static void Main(string[] args)
        {
            string[] str = File.ReadAllLines("BaseKey.txt");
            baseLogin = Convert.ToBase64String(AesEncruptAlg.Encrypt(Encoding.UTF8.GetBytes(str[0]), Encoding.UTF8.GetBytes(str[2])));
            basePassword = Convert.ToBase64String(AesEncruptAlg.Encrypt(Encoding.UTF8.GetBytes(str[1]), Encoding.UTF8.GetBytes(str[2])));

            
            /*IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11000);*/
            TcpListener listener = new TcpListener(11000);
            listener.Start();

            IPHostEntry CompareSysHost = Dns.GetHostEntry("localhost");
            IPAddress CompareSysAddr = CompareSysHost.AddressList[1];
            IPEndPoint CompareSysEndPoint = new IPEndPoint(CompareSysAddr, 11004);
            Socket handler = null;
            while (true)
            {
                try
                {
                    string data = null;
                    byte[] bytes = new byte[1024];
                    byte[] shifr = new byte[1];
                    handler = listener.AcceptSocket();
                    NetworkStream ns = new NetworkStream(handler);
                    ns.Read(shifr, 0, 1);
                    ns.Read(bytes, 0, 1024 - 1);

                    int log_len = BitConverter.ToInt32(bytes, 0);
                    int index = 4;
                    string login_auth = Encoding.UTF8.GetString(bytes, 4, log_len);
                    index += log_len;
                    int password_leng = BitConverter.ToInt32(bytes, index);
                    string password_auth = Encoding.UTF8.GetString(bytes, index + 4, password_leng);
                    byte[] ans = new byte[1];
                    int signed = signIn(login_auth, password_auth);
                    if (signed == 1)
                    {

                        ans[0] = 1;
                        handler.Send(ans);
                        byte[] buffer = new byte[MAX_SIZE];
                        shifr = new byte[1];
                        ns = new NetworkStream(handler);
                        ns.Read(shifr, 0, 1);
                        ns.Read(buffer, 0, (Int32)MAX_SIZE - 1);


                        int i = 0;
                        byte priznak = buffer[i];
                        i++;
                        double accurancy = 0.0f;
                        int res_amount = 0;
                        if (priznak != 1)
                        {
                            accurancy = BitConverter.ToDouble(buffer, i);
                            i += sizeof(double);
                        }
                        else
                        {
                            res_amount = BitConverter.ToInt32(buffer, i);
                            i += sizeof(Int32);
                        }
                        int leng = BitConverter.ToInt32(buffer, i);
                        i += sizeof(int);
                        byte type = buffer[i];
                        i++;

                        MemoryStream ms = new MemoryStream();
                        ms.Write(buffer, i, leng);
                        Image img = Image.FromStream(ms);
                        switch (type)
                        {
                            case 0:
                                img.Save(@"result.png", System.Drawing.Imaging.ImageFormat.Png);
                                break;
                            case 1:
                                img.Save(@"result.jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
                                break;
                            case 2:
                                img.Save(@"result.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                                break;
                            case 3:
                                img.Save(@"result.tiff", System.Drawing.Imaging.ImageFormat.Tiff);
                                break;
                            case 4:
                                img.Save(@"result.gif", System.Drawing.Imaging.ImageFormat.Gif);
                                break;
                            default:
                                break;
                        }
                        handler.Close();
                        Console.WriteLine("Логин: " + login_auth);
                        Console.WriteLine("Пароль:" + password_auth);
                        Console.WriteLine("Вид результата: " + priznak);
                        Console.WriteLine("Точность" + accurancy);
                        Console.WriteLine("Размер картинки" + leng);
                        Console.WriteLine("Тип картинки" + type);

                        Socket sender = new Socket(CompareSysAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        byte[] msg = new byte[buffer.Length + bytes.Length];
                        bytes.CopyTo(msg, 0);
                        buffer.CopyTo(msg, bytes.Length);
                        sender.Connect(CompareSysEndPoint);

                        sender.Send(msg);
                        sender.Close();
                    }
                    else
                    {
                        ans[0] = 0;
                        handler.Send(ans);
                        handler.Close();
                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    if (handler != null && handler.Connected)
                        handler.Close();
                }
            }
        }
        static int signIn(string login,string pass)
        {
            string url = BaseAddress + "users/authorize/"+login+"/"+pass;
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("login:" + baseLogin);
            request.Headers.Add("password:" + basePassword);
            request.Headers.Add("encryption_type:" + encryptionType);
            request.ContentType = "application/json";
            
            using (HttpWebResponse responce = (HttpWebResponse)request.GetResponse())
            {
                Stream s = responce.GetResponseStream();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Answer));
                Answer ans  = (Answer)ser.ReadObject(s);
                responce.Close();
                return ans.answer;
            }
            return 1;
        }
    }
    
    public class Answer
    {
        public int answer;
    }
    public class AesEncruptAlg
    {
        public static byte[] Encrypt(byte[] ENC, byte[] AES_KEY)
        {
            byte[] tmp;
            byte[] buf;
            byte[] AES_IV;
            using (Aes AES = Aes.Create())
            {
                AES.KeySize = 256;
                AES.BlockSize = 128;
                AES.Key = AES_KEY;
                AES.GenerateIV();
                AES_IV = AES.IV;
                AES.Padding = PaddingMode.ANSIX923;  //!!!The ANSIX923 padding string consists of a sequence of bytes filled with zeros before the length. 

                MemoryStream MS = new MemoryStream();
                CryptoStream CS = new CryptoStream(MS, AES.CreateEncryptor(AES.Key, AES.IV), CryptoStreamMode.Write);

                CS.Write(ENC, 0, ENC.Length);
                CS.Close();

                tmp = MS.ToArray();
                buf = new byte[tmp.Length + 16];
                AES_IV.CopyTo(buf, 0);
                tmp.CopyTo(buf, 16);
                return buf;
            }
        }
        public static byte[] Decrypt(byte[] DEC, byte[] AES_KEY)
        {
            byte[] AES_IV = new byte[16];
            byte[] tmp = new byte[DEC.Length - 16];
            for (int i = 0; i < 16; i++)
            {
                AES_IV[i] = DEC[i];
            }
            for (int i = 0; i < DEC.Length - 16; i++)
            {
                tmp[i] = DEC[i + 16];
            }
            using (Aes AES = Aes.Create())
            {
                AES.KeySize = 256;
                AES.BlockSize = 128;
                AES.Key = AES_KEY;
                AES.IV = AES_IV;
                AES.Padding = PaddingMode.ANSIX923;

                MemoryStream MS = new MemoryStream();
                CryptoStream CS = new CryptoStream(MS, AES.CreateDecryptor(AES.Key, AES.IV), CryptoStreamMode.Write);

                CS.Write(tmp, 0, tmp.Length);
                CS.Close();

                return MS.ToArray();
            }
        }

    }

}
