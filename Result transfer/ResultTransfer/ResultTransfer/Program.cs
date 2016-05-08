using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Result_Transfer
{
    class Program
    {
        static void Main(string[] args)
        {
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11001);
            TcpListener listener = new TcpListener(ipEndPoint);
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
                                answer.AddRange(BitConverter.GetBytes(subscribers.ElementAt(j).Key));
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
                                    msg_list.AddRange(BitConverter.GetBytes(refer.Length));
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
        static Dictionary<int,string> GetSubs(string login, string pass,out bool access)
        {
            string url = "http://localhost:12001/httpserver/";
            ListSubsQuery newQuery = new ListSubsQuery();
            newQuery.login = login;
            newQuery.password = pass;
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ListSubsQuery));
            ser.WriteObject(stream, newQuery);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = stream.Length;
            using (Stream postStream = request.GetRequestStream())
            {
                postStream.Write(stream.ToArray(), 0, (Int32)stream.Length);
                postStream.Close();
            }
            using (HttpWebResponse responce = (HttpWebResponse)request.GetResponse())
            {
                Stream s = responce.GetResponseStream();
                ser = new DataContractJsonSerializer(typeof(ListSubsAnswer));
                ListSubsAnswer ans = (ListSubsAnswer)ser.ReadObject(s);
                responce.Close();
                access = ans.access;
                return ans.subs;
            }
        }
        static string[] GetPrevResults(string login, string pass,string user, out bool access)
        {
            string url = "http://localhost:12001/httpserver/";
            ResultsQuery newQuery = new ResultsQuery();
            newQuery.login = login;
            newQuery.password = pass;
            newQuery.neededUser = user;
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResultsQuery));
            ser.WriteObject(stream, newQuery);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = stream.Length;
            using (Stream postStream = request.GetRequestStream())
            {
                postStream.Write(stream.ToArray(), 0, (Int32)stream.Length);
                postStream.Close();
            }
            using (HttpWebResponse responce = (HttpWebResponse)request.GetResponse())
            {
                Stream s = responce.GetResponseStream();
                ser = new DataContractJsonSerializer(typeof(ResultsAnswer));
                ResultsAnswer ans = (ResultsAnswer)ser.ReadObject(s);
                responce.Close();
                access = ans.access;
                return ans.results;
            }
        }
    }
    public class ListSubsAnswer
    {
        public bool access;
        public Dictionary<int, string> subs;
    }
    public class ListSubsQuery
    {
        public string login;
        public string password;
    }
    public class ResultsAnswer
    {
        public bool access;
        public string[] results;
    }
    public class ResultsQuery
    {
        public string login;
        public string password;
        public string neededUser;
    }
}
