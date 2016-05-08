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
                        if (newQuery.neededUser != null)
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
            Dictionary<int,string> subs = new Dictionary<int,string>();
            if (login == "Sergey")
            {
                subs.Add(321,"Katya");
                subs.Add(1001, "Gleb Andreevich");
                subs.Add(4071, "Alexey");
            }
            else if(login == "Katya")
            {
                subs.Add(322,"Sergey");
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
                results.Add("vk.com/Katya");
                results.Add("vk.com/Ekaterina");
            }
            else if (user == "Sergey")
            {
                results.Add("vk.com/Sergey");
                results.Add("vk.com/Kto-to");
            }
            else if (user == "Gleb Andreevich")
            {
                results.Add("vk.com/GlebUrvanov");
                results.Add("vk.com/GlebAndreevich");
                results.Add("facebook.com/GlebUrvanov");
            }
            else if (user == "Alexey")
                results.Add("vk.com/AlexeyFrolov");
            else if (user == "Oleg")
                results.Add("facebook.com/OlegIvanov");
            if (results.Count > 0)
                return results.ToArray();
            return null;
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
}
