using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace Result_Transfer
{
    class Program
    {
        static string BaseAddress = "http://localhost:12001/httpserver/";
        static string baseLogin;
        static string basePassword;
        static int encryptionType = 0;
        static void Main(string[] args)
        {
            string[] str = File.ReadAllLines("BaseKey.txt");
            baseLogin = Convert.ToBase64String(AesEncruptAlg.Encrypt(Encoding.UTF8.GetBytes(str[0]), Encoding.UTF8.GetBytes(str[2])));
            basePassword = Convert.ToBase64String(AesEncruptAlg.Encrypt(Encoding.UTF8.GetBytes(str[1]), Encoding.UTF8.GetBytes(str[2])));
            Thread t = new Thread(ListenCompareSystem);
            t.Start();
            /*IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11001);*/
            TcpListener listener = new TcpListener(11001);
            listener.Start();
            while(true)
            {
                try
                {
                    Socket handler = listener.AcceptSocket();
                    byte[] bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    int msg_type = bytes[1];
                    int i = 2;
                    string login = Encoding.UTF8.GetString(bytes, i+1 , bytes[i]);
                    i += login.Length + 1;
                    string password = Encoding.UTF8.GetString(bytes, i+1 , bytes[i]);
                    if (msg_type == 1)
                    {
                        Console.WriteLine("Пользователь "+login + " запрашивает список подписок...");
                        bool access;
                        // запрос к базе
                        Dictionary<int, string> subscribers = GetSubs(login,password,out access);
                        if (access == true)
                        {

                            List<byte> answer = new List<byte>();
                            answer.Add((byte)0);
                            answer.Add((byte)1);
                            for (int j = 0; j < subscribers.Count; j++)
                            {
                                byte[] list = BitConverter.GetBytes(subscribers.ElementAt(j).Key);
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(list);
                                answer.AddRange(list);
                                byte leng = (byte)subscribers.ElementAt(j).Value.Length;
                                answer.Add(leng);
                                answer.AddRange(Encoding.UTF8.GetBytes(subscribers.ElementAt(j).Value));
                            }
                            byte[] msg = answer.ToArray();
                            int bytesSend = handler.Send(msg);
                            if (bytesSend < msg.Length)
                                Console.WriteLine("Сообщение передалось не полностью!");
                            Console.WriteLine("Список отправлен. передано " + bytesSend + " байт");
                        }
                        else
                        {
                            byte[] msg = new byte[2];
                            msg[1] = (byte)101;
                            int bytesSend = handler.Send(msg);
                            if (bytesSend < msg.Length)
                                Console.WriteLine("Сообщение передалось не полностью!");
                            Console.WriteLine("Доступ запрещен");
                        }
                    }
                    else if (msg_type == 3)
                    {
                        
                        i += password.Length + 1;
                        string user = Encoding.UTF8.GetString(bytes, i+1 , bytes[i]);
                        Console.WriteLine("Пользователь "+login + " запрашивает список результатов пользователя "+ user);
                        //запрос к базе
                        bool access;
                        string[] references = GetPrevResults(login,password,user,out access);
                        if (access == true)
                        {
                            List<byte> msg_list = new List<byte>();
                            msg_list.Add((byte)0);
                            msg_list.Add((byte)3);
                            if (references != null)
                            {
                                foreach (string refer in references)
                                {
                                    byte[] list = BitConverter.GetBytes(refer.Length);
                                    if (BitConverter.IsLittleEndian)
                                        Array.Reverse(list);
                                    msg_list.AddRange(list);
                                    msg_list.AddRange(Encoding.UTF8.GetBytes(refer));
                                }
                            }
                            byte[] msg = msg_list.ToArray();
                            //шифрование

                            int bytesSend = handler.Send(msg);
                            if (bytesSend < msg.Length)
                                Console.WriteLine("Сообщение передалось не полностью!");
                        }
                        else
                        {
                            byte[] msg = new byte[2];
                            msg[1] = (byte)103;
                            int bytesSend = handler.Send(msg);
                            if (bytesSend < msg.Length)
                                Console.WriteLine("Сообщение передалось не полностью!");
                            Console.WriteLine("Доступ запрещен");
                        }
                    }
                    



                    /*if (data.IndexOf("<TheEnd>") > -1)
                    {
                        Console.WriteLine("Сервер завершил соединение с клиентом.");
                        break;
                    }*/
                    //handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                }
            }
            
        }
        static void ListenCompareSystem()
        {
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11003);
            TcpListener listener = new TcpListener(ipEndPoint);
            listener.Start();
            while (true)
            {
                try
                {
                    Socket handler = listener.AcceptSocket();
                    byte[] bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    int leng = bytes[0];
                    string login = Encoding.UTF8.GetString(bytes,1,leng);
                    long[] address = GetIpAddress(login);
                    List<byte> msgList = new List<byte>();
                    msgList.Add((byte)0);
                    msgList.Add((byte)2);
                    msgList.AddRange(bytes);
                    if(address != null)
                    {
                        foreach(long receiver in address)
                        {
                            IPAddress addr = new IPAddress(receiver);
                            IPEndPoint receiverEndPoint = new IPEndPoint(ipAddr, 11002);
                            try
                            {
                                Socket sender = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                                sender.Connect(receiverEndPoint);
                                int byteSends = sender.Send(msgList.ToArray());
                                if (byteSends < msgList.Count)
                                    Console.WriteLine("Сообщение для " + addr.ToString() + "отправлено неполностью");
                                sender.Close();
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine(ipAddr.ToString() + "недоступен!");
                            }
                        }
                    }
                    msgList.Clear();
                    handler.Close();
                }
                catch (Exception ex)
                {

                }
            }
        }
        static Dictionary<int,string> GetSubs(string login, string pass,out bool access)
        {
            string url = BaseAddress + "users/subscriptions/" + login + "/" + pass;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("login:" + baseLogin);
            request.Headers.Add("password:" + basePassword);
            request.Headers.Add("encryption_type:" + encryptionType);
            request.ContentType = "application/json";
            using (HttpWebResponse responce = (HttpWebResponse)request.GetResponse())
            {
                Stream s = responce.GetResponseStream();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ListSubsAnswer));
                ListSubsAnswer ans = (ListSubsAnswer)ser.ReadObject(s);
                responce.Close();
                access = ans.access;
                return ans.subs;
            }
        }
        static string[] GetPrevResults(string login, string pass,string user, out bool access)
        {
            string url = BaseAddress + "users/lastresults/" + login + "/" + pass + "/" + user;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("login:" + baseLogin);
            request.Headers.Add("password:" + basePassword);
            request.Headers.Add("encryption_type:" + encryptionType);
            request.ContentType = "application/json";
            using (HttpWebResponse responce = (HttpWebResponse)request.GetResponse())
            {
                Stream s = responce.GetResponseStream();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResultsAnswer));
                ResultsAnswer ans = (ResultsAnswer)ser.ReadObject(s);
                responce.Close();
                access = ans.access;
                return ans.results;
            }
        }
        static long[] GetIpAddress(string login)
        {
            string url = BaseAddress + "users/getips/" + login;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("login:" + baseLogin);
            request.Headers.Add("password:" + basePassword);
            request.Headers.Add("encryption_type:" + encryptionType);
            request.ContentType = "application/json";
            using (HttpWebResponse responce = (HttpWebResponse)request.GetResponse())
            {
                Stream s = responce.GetResponseStream();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IpsAnswer));
                IpsAnswer ans = (IpsAnswer)ser.ReadObject(s);
                responce.Close();
                return ans.address;
            }
        }
    }
    public class ListSubsAnswer
    {
        public bool access;
        public Dictionary<int, string> subs;
    }
    /*public class ListSubsQuery
    {
        public string login;
        public string password;
    }*/
    public class ResultsAnswer
    {
        public bool access;
        public string[] results;
    }
    /*public class ResultsQuery
    {
        public string login;
        public string password;
        public string neededUser;
    }*/
    public class IpsAnswer
    {
        public long[] address;
    }
    /*public class IpsQuery
    {
        public string login;
        
    }*/
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
