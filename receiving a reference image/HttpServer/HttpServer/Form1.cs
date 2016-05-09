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

namespace HttpServer
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
            string uri = @"http://localhost:12000/httpserver/";
            StartServer(uri);
        }
        private void StartServer(string uri)
        {
            server = new HttpListener();
            server.Prefixes.Add(uri);
            server.Start();
            this.Text = "Сервер запустился";
            while(server.IsListening)
            {
                HttpListenerContext context = server.GetContext();
                HttpListenerRequest request = context.Request;
                if (request.HttpMethod == "POST")
                {
                    if (!request.HasEntityBody)
                        break;
                    using (Stream body = request.InputStream)
                    {
                        
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(Query));
                        Query newQuery = (Query)ser.ReadObject(body);
                        Answer ans = new Answer();
                        if ((newQuery.login == "Katya" && newQuery.password == "Martynova")||(newQuery.login == "Sergey" && newQuery.password == "Dunaev"))
                            ans.answer = 1;
                        else
                            ans.answer = 0;
                        HttpListenerResponse response = context.Response;
                        response.ContentType = "application/json";
                        MemoryStream stream = new MemoryStream();
                        ser = new DataContractJsonSerializer(typeof(Answer));
                        ser.WriteObject(stream, ans);
                        response.ContentLength64 = stream.Length;
                        using (Stream output = response.OutputStream)
                        {
                            output.Write(stream.ToArray(), 0, (Int32)stream.Length);
                            this.Text = "Ответ отправлен: " + ans.answer;
                        }
                    }
                }
            }
        }
    }
    public class Answer
    {
        public int answer;
    }
    public class Query
    {
        public string login;
        public string password;
    }
}
