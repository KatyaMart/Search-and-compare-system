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
using System.Security.Cryptography;

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
                if (request.HttpMethod == "GET")
                {
                    char[] separator = { '/' };
                    string[] param = request.Url.ToString().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    
                        if (param[4] == "getips")
                        {
                            IpsAnswer ans = new IpsAnswer();
                            long[] addrs = getIPs(param[4]);
                            HttpListenerResponse response = context.Response;
                            response.ContentType = "application/json";
                            MemoryStream stream = new MemoryStream();
                            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IpsAnswer));
                            ser.WriteObject(stream, ans);
                            response.ContentLength64 = stream.Length;
                            using (Stream output = response.OutputStream)
                            {
                                output.Write(stream.ToArray(), 0, (Int32)stream.Length);
                                //this.Text = "Ответ отправлен: " + ans.results.Length;
                            }
                        }
                        else if (param[4] == "lastresults")
                        {
                            ResultsAnswer ans = new ResultsAnswer();
                            if ((param[5] == "Sergey" && param[6] == "Dunaev") || (param[5] == "Katya" && param[6] == "Martynova"))
                            {
                                ans.access = true;
                                ans.results = getResults(param[7]);
                            }
                            else
                            {
                                ans.access = false;
                                ans.results = null;
                            }
                            HttpListenerResponse response = context.Response;
                            response.ContentType = "application/json";
                            MemoryStream stream = new MemoryStream();
                            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResultsAnswer));
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
                            ListSubAnswer ans = new ListSubAnswer();
                            if ((param[5] == "Sergey" && param[6] == "Dunaev") || (param[5] == "Katya" && param[6] == "Martynova"))
                            {
                                ans.access = 1;
                                ans.subs = getSubscribes(param[5]);
                            }
                            else
                            {
                                ans.access = 0;
                                ans.subs = null;
                            }
                            HttpListenerResponse response = context.Response;
                            response.ContentType = "application/json";
                            /*MemoryStream stream = new MemoryStream();
                            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ListSubAnswer));
                            ser.WriteObject(stream, ans);
                            response.ContentLength64 = stream.Length;*/
                            string str = "{\"access\":1,\"subs\":{\"test1\":100}}";
                            using (Stream output = response.OutputStream)
                            {
                                output.Write(Encoding.UTF8.GetBytes(str),0,str.Length);
                                //this.Text = "Ответ отправлен: " + ans.subs.Count;
                            }
                        }
                    
                }
            }
        }
        private Dictionary<string, int> getSubscribes(string login)
        {
            Dictionary<string, int> subs = new Dictionary<string, int>();
            if (login == "Sergey")
            {
                subs.Add("Katya",3122);
                subs.Add("Gleb Andreevich",1001);
                subs.Add( "Alexey",4071);
            }
            else if (login == "Katya")
            {
                subs.Add( "Sergey",2141);
                subs.Add("Gleb Andreevich",1001);
                subs.Add("Alexey",4071);
                subs.Add("Oleg",677);
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
            {
                List<string> res= new List<string>();
                foreach(string str in results)
                {
                    res.Add(Convert.ToBase64String(AesEncruptAlg.Encrypt(Encoding.UTF8.GetBytes(str),Encoding.UTF8.GetBytes("abcabcaabcabcabc"))));
                }
                return res.ToArray();
            }
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
        public int access;
        public Dictionary<string, int> subs;
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