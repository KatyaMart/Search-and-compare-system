using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;

namespace HttpServerForResult
{
    public partial class Form1 : Form
    {
        HttpListener server;
        bool flag = true;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string uri = @"http://localhost:12001/httpserver/";
            StartServer(uri);
        }
        private void StartServer(string uri)
        {
            server = new HttpListener();
            server.Prefixes.Add(uri);
            server.Start();
            this.Text = "Сервер запустился";
            while (server.IsListening)
            {
                HttpListenerContext context = server.GetContext();
                HttpListenerRequest request = context.Request;
                if (request.HttpMethod == "POST")
                {
                    if (!request.HasEntityBody)
                        break;
                    using (Stream body = request.InputStream)
                    {
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResultsQuery));
                        ResultsQuery newQuery = (ResultsQuery)ser.ReadObject(body);
                        if (newQuery.password == null)
                        {
                            IpsAnswer ans = new IpsAnswer();
                            long[] addrs = getIPs(newQuery.login);
                            HttpListenerResponse response = context.Response;
                            response.ContentType = "application/json";
                            MemoryStream stream = new MemoryStream();
                            ser = new DataContractJsonSerializer(typeof(IpsAnswer));
                            ser.WriteObject(stream, ans);
                            response.ContentLength64 = stream.Length;
                            using (Stream output = response.OutputStream)
                            {
                                output.Write(stream.ToArray(), 0, (Int32)stream.Length);
                                //this.Text = "Ответ отправлен: " + ans.results.Length;
                            }
                        }
                        else if (newQuery.neededUser != null)
                        {
                            ResultsAnswer ans = new ResultsAnswer();
                            if ((newQuery.login == "Sergey" && newQuery.password == "Dunaev") || (newQuery.login == "Katya" && newQuery.password == "Martynova"))
                            {
                                ans.access = true;
                                ans.results = getResults(newQuery.neededUser);
                            }
                            else
                            {
                                ans.access = false;
                                ans.results = null;
                            }
                            HttpListenerResponse response = context.Response;
                            response.ContentType = "application/json";
                            MemoryStream stream = new MemoryStream();
                            ser = new DataContractJsonSerializer(typeof(ResultsAnswer));
                            ser.WriteObject(stream, ans);
                            response.ContentLength64 = stream.Length;
                            using (Stream output = response.OutputStream)
                            {
                                output.Write(stream.ToArray(), 0, (Int32)stream.Length);
                                //this.Text = "Ответ отправлен: " + ans.results.Length;
                            }
                        }
                        else
                        {
                            //ser = new DataContractJsonSerializer(typeof(ListSubsQuery));
                            ListSubsQuery newSubQuery = new ListSubsQuery();
                            newSubQuery.login = newQuery.login;
                            newSubQuery.password = newQuery.password;
                            ListSubAnswer ans = new ListSubAnswer();
                            if ((newSubQuery.login == "Sergey" && newSubQuery.password == "Dunaev") || (newSubQuery.login == "Katya" && newSubQuery.password == "Martynova"))
                            {
                                ans.access = true;
                                ans.subs = getSubscribes(newSubQuery.login);
                            }
                            else
                            {
                                ans.access = false;
                                ans.subs = null;
                            }
                            HttpListenerResponse response = context.Response;
                            response.ContentType = "application/json";
                            MemoryStream stream = new MemoryStream();
                            ser = new DataContractJsonSerializer(typeof(ListSubAnswer));
                            ser.WriteObject(stream, ans);
                            response.ContentLength64 = stream.Length;
                            using (Stream output = response.OutputStream)
                            {
                                output.Write(stream.ToArray(), 0, (Int32)stream.Length);
                                //this.Text = "Ответ отправлен: " + ans.subs.Count;
                            }
                        }
                    }
                }
            }
        }
        private Dictionary<int, string> getSubscribes(string login)
        {
            Dictionary<int, string> subs = new Dictionary<int, string>();
            if (login == "Sergey")
            {
                subs.Add(321, "Katya");
                subs.Add(1001, "Gleb Andreevich");
                subs.Add(4071, "Alexey");
            }
            else if (login == "Katya")
            {
                subs.Add(322, "Sergey");
                subs.Add(1001, "Gleb Andreevich");
                subs.Add(4071, "Alexey");
                subs.Add(677, "Oleg");
            }
            return subs;
        }
        private string[] getResults(string user)
        {
            List<string> results = new List<string>();
            if (user == "Katya")
            {
                results.Add("vk.com/sutil");
                results.Add("vk.com/aleksey2093");
            }
            else if (user == "Sergey")
            {
                results.Add("vk.com/sutil");
                results.Add("vk.com/aleksey2093");
            }
            else if (user == "Gleb Andreevich")
            {
                results.Add("vk.com/id338659466");
                results.Add("vk.com/aleksey2093");
                results.Add("facebook.com/sutil");
            }
            else if (user == "Alexey")
                results.Add("vk.com/id338659466");
            else if (user == "Oleg")
                results.Add("facebook.com/OlegIvanov");
            if (results.Count > 0)
                return results.ToArray();
            return null;
        }
        private long[] getIPs(string login)
        {
            long[] addrs = null;
            if (login == "Sergey")
            {
                addrs = new long[2];
                byte[] bytes = { 192,168,60,25};
                IPAddress addr = new IPAddress(bytes);
                addrs[0] = addr.Address;
                byte[] bytes1 = { 192, 168, 60, 144 };
                addr = new IPAddress(bytes1);
                addrs[1] = addr.Address;
            }
            else if (login == "Katya")
            {
                addrs = new long[3];
                byte[] bytes = { 192, 168, 60, 25 };
                IPAddress addr = new IPAddress(bytes);
                addrs[0] = addr.Address;
                byte[] bytes1 = { 192, 168, 60, 121 };
                addr = new IPAddress(bytes1);
                addrs[1] = addr.Address;
                byte[] bytes2 = { 192, 168, 60, 54 };
                addr = new IPAddress(bytes2);
                addrs[2] = addr.Address;
            }
            return addrs;
        }
    }
    public class ListSubAnswer
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
    public class IpsQuery
    {
        public string login;
    }
    public class IpsAnswer
    {
        public long[] address;
    }
}