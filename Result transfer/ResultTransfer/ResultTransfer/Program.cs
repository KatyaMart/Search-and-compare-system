using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Security.AccessControl;

namespace Result_Transfer
{
    class Program
    {
        static string BaseAddress = "http://localhost:12001/httpserver/";/*"http://192.168.1.130:8080/";*/
        static string baseLogin;
        static string basePassword;
        static string key = "abcabcaabcabcabc";
        static int encryptionType = 0;
        static void Main(string[] args)
        {
            string[] str = File.ReadAllLines("BaseKey.txt");
            baseLogin = str[0];
            basePassword = str[1];
            Thread t = new Thread(ListenCompareSystem);
            t.Start();
            TcpListener listener = new TcpListener(11001);
            listener.Start();
            while(true)
            {
                try
                {
                    Socket handler = listener.AcceptSocket();
                    byte[] bytes = new byte[1024];
                    
                    int bytesRec = handler.Receive(bytes);
                    byte shifr = bytes[0];
                    byte[] buffer = new byte[bytesRec-1];
                    Array.Copy(bytes,1,buffer,0,bytesRec-1);
                    if (shifr == 0)
                    {
                        bytes = AES.AES_Decrypt(buffer,Encoding.UTF8.GetBytes(key));
                    }
                    else if(shifr==1)
                    {
                        string PrivateKey, PublicKey;
                        RSA.GetPublicAndPrivateKey(out PrivateKey,out PublicKey);
                        bytes = RSA.DecryptData(PrivateKey,buffer);
                    }
                    int msg_type = bytes[0];
                    int i = 1;
                    string login = Encoding.UTF8.GetString(bytes, i+1 , bytes[i]);
                    i += login.Length + 1;
                    string password = Encoding.UTF8.GetString(bytes, i+1 , bytes[i]);
                    if (msg_type == 1)
                    {
                        Console.WriteLine("Пользователь "+login + " запрашивает список подписок...");
                        int access;
                        // запрос к базе
                        Dictionary<string, int> subscribers = GetSubs(login,password,out access);
                        if (access == 1)
                        {

                            List<byte> answer = new List<byte>();
                            answer.Add((byte)1);
                            for (int j = 0; j < subscribers.Count; j++)
                            {
                                byte[] list = BitConverter.GetBytes(subscribers.ElementAt(j).Value);
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(list);
                                answer.AddRange(list);
                                //answer.AddRange(BitConverter.GetBytes(subscribers.ElementAt(j).Value));
                                byte leng = (byte)subscribers.ElementAt(j).Key.Length;
                                answer.Add(leng);
                                answer.AddRange(Encoding.UTF8.GetBytes(subscribers.ElementAt(j).Key));
                            }
                            List<byte> msg = new List<byte>();
                            if (shifr == 0)
                            {
                                msg.Add((byte)0);
                                msg.AddRange(AES.AES_Encrypt(answer.ToArray(),Encoding.UTF8.GetBytes(key)));
                            }
                            else if (shifr == 1)
                            {
                                msg.Add((byte)1);
                                string PublicKey,PrivateKey;
                                RSA.GetPublicAndPrivateKey(out PrivateKey,out PublicKey);
                                msg.AddRange(RSA.EncryptData(PublicKey,answer.ToArray()));
                            }
                            int bytesSend = handler.Send(msg.ToArray());
                            if (bytesSend < msg.Count)
                                Console.WriteLine("Сообщение передалось не полностью!");
                            Console.WriteLine("Список отправлен. передано " + bytesSend + " байт");
                        }
                        else
                        {
                            List<byte> msg = new List<byte>();
                            if (shifr == 0)
                            {
                                msg.Add((byte)0);
                                byte[] buf = new byte[1];
                                buf[0] = 101;
                                msg.AddRange(AES.AES_Encrypt(buf, Encoding.UTF8.GetBytes(key)));
                            }
                            int bytesSend = handler.Send(msg.ToArray());
                            if (bytesSend < msg.Count)
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
                            msg_list.Add((byte)3);
                            if (references != null)
                            {
                                foreach (string refer in references)
                                {
                                    
                                    byte[] decription = AesEncruptAlg.Decrypt(Convert.FromBase64String(refer),Encoding.UTF8.GetBytes(key));
                                    byte[] list = BitConverter.GetBytes(decription.Length);
                                    if (BitConverter.IsLittleEndian)
                                        Array.Reverse(list);
                                    msg_list.AddRange(list);
                                    //msg_list.AddRange(BitConverter.GetBytes(decription.Length));
                                    msg_list.AddRange(decription);
                                }
                            }
                            //шифрование
                            List<byte> msg = new List<byte>();
                            if(shifr==0 )
                            {
                                msg.Add((byte)0);
                                msg.AddRange(AES.AES_Encrypt(msg_list.ToArray(),Encoding.UTF8.GetBytes(key)));
                            }
                            else if (shifr == 1)
                            {
                                msg.Add((byte)1);
                                string PublicKey, PrivateKey;
                                RSA.GetPublicAndPrivateKey(out PrivateKey, out PublicKey);
                                msg.AddRange(RSA.EncryptData(PublicKey,msg_list.ToArray()));
                            }
                           

                            int bytesSend = handler.Send(msg.ToArray());
                            if (bytesSend < msg.Count)
                                Console.WriteLine("Сообщение передалось не полностью!");
                        }
                        else
                        {
                            List<byte> msg = new List<byte>();
                            if (shifr == 0)
                            {
                                msg.Add((byte)0);
                                byte[] buf = new byte[1];
                                buf[0] = 103;
                                msg.AddRange(AES.AES_Encrypt(buf, Encoding.UTF8.GetBytes(key)));
                            }
                            int bytesSend = handler.Send(msg.ToArray());
                            if (bytesSend < msg.Count)
                                Console.WriteLine("Сообщение передалось не полностью!");
                            Console.WriteLine("Доступ запрещен");
                        }
                    }
                    
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

                    byte leng = bytes[0];
                    string login = Encoding.UTF8.GetString(bytes,1,(int)leng);
                    IPAddress[] address = GetIpAddress(login);
                    int i = 1 + login.Length;
                    List<byte> data = new List<byte>();
                    data.Add((byte)2);
                    data.Add(leng);
                    data.AddRange(Encoding.UTF8.GetBytes(login));
                    while (i < bytesRec)
                    {
                        int length = BitConverter.ToInt32(bytes,i);
                        i+=sizeof(Int32);
                        byte[] res = Convert.FromBase64String(Encoding.UTF8.GetString(bytes, i, length));
                        byte[] list = BitConverter.GetBytes(res.Length);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(list);
                            data.AddRange(list);
                        data.AddRange(res);
                        i += length;
                    }
                    List<byte> msgList = new List<byte>();
                    msgList.Add((byte)encryptionType);
                    if (encryptionType == 0)
                    {
                        msgList.AddRange(AES.AES_Encrypt(data.ToArray(),Encoding.UTF8.GetBytes(key)));
                    }
                    else if (encryptionType == 1)
                    {
                        string PrivateKey, PublicKey;
                        RSA.GetPublicAndPrivateKey(out PrivateKey,out PublicKey);
                        msgList.AddRange(RSA.EncryptData(PublicKey,data.ToArray()));
                    }
                    if(address != null)
                    {
                        foreach(IPAddress receiver in address)
                        {
                            IPEndPoint receiverEndPoint = new IPEndPoint(receiver, 11002);
                            try
                            {
                                Socket sender = new Socket(receiver.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                                sender.Connect(receiverEndPoint);
                                int byteSends = sender.Send(msgList.ToArray());
                                if (byteSends < msgList.Count)
                                    Console.WriteLine("Сообщение для " + receiver.ToString() + "отправлено неполностью");
                                sender.Close();
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine(receiver.ToString() + "недоступен!");
                            }
                        }
                    }
                    msgList.Clear();
                    handler.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка отправки результатов");
                }
            }
        }
        static Dictionary<string,int> GetSubs(string login, string pass,out int access)
        {
            string url = BaseAddress + "users/subscriptions/" + login + "/" + pass;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("login","login");
            request.Headers.Add("password", "password");
            request.ContentType = "application/json";
            using (HttpWebResponse responce = (HttpWebResponse)request.GetResponse())
            {
                Stream s = responce.GetResponseStream(); 
                StreamReader sr = new StreamReader(s,Encoding.UTF8);
                char[] str = new char[100];
                sr.Read(str,0,(int)100);
                string so = new string(str);
                so = so.Substring(0,so.IndexOf("\0"));
                var ser = new JavaScriptSerializer(); 
                ListSubsAnswer ans = ser.Deserialize<ListSubsAnswer>(so);
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
            request.Headers.Add("login", "login");
            request.Headers.Add("password" , "password:");
           // request.Headers.Add("encryption_type:" + encryptionType);
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
        static IPAddress[] GetIpAddress(string login)
        {
            string url = BaseAddress + "users/getips/" + login;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("login:",baseLogin);
            request.Headers.Add("password:", basePassword);
            //request.Headers.Add("encryption_type:" + encryptionType);
            request.ContentType = "application/json";
            using (HttpWebResponse responce = (HttpWebResponse)request.GetResponse())
            {
                Stream s = responce.GetResponseStream();
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IpsAnswer));
                IpsAnswer ans = (IpsAnswer)ser.ReadObject(s);
                responce.Close();
                IPAddress[] ips = new IPAddress[ans.address.Length];
                for (int i = 0; i < ans.address.Length; i++)
                    ips[i] = IPAddress.Parse(ans.address[i]);
                return ips;
            }
            return null;
        }
    }
    public class ListSubsAnswer
    {
        public int access;
        public Dictionary<string, int> subs;
    }

    public class ResultsAnswer
    {
        public bool access;
        public string[] results;
    }

    public class IpsAnswer
    {
        public string[] address;
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
    public class AES 
    {
        public static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        public static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }
    }
    public class RSA
    {
        const string DefaultPublicKey = @"<RSAKeyValue><Modulus>npRv8HuLBHYDhQY9weocg52bpy4xPfutFYwiTq7KamdxdfKjJDf6MzYWiJf73neOdJeKG+9aP/lZGn+E7dJCm1+X94D2XHS9wvyNuivqYc9SMCSc1cRO+lvWC2iVtzxw8YYmhPR0w4fzrBv/zWr7E+QsdCwaYr8kI6DlC6dEJx0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        private const string DefaultPrivateKey =
            @"<RSAKeyValue><Modulus>npRv8HuLBHYDhQY9weocg52bpy4xPfutFYwiTq7KamdxdfKjJDf6MzYWiJf73neOdJeKG+9aP/lZGn+E7dJCm1+X94D2XHS9wvyNuivqYc9SMCSc1cRO+lvWC2iVtzxw8YYmhPR0w4fzrBv/zWr7E+QsdCwaYr8kI6DlC6dEJx0=</Modulus><Exponent>AQAB</Exponent><P>0IGxtOeOm6TwhWmzd2ATcTLP1y95bp9tY8U0Y4w23gKIllsDO4SeGwNmvG63vzHWf+Y1D++3xMiT3l0QxMo5Jw==</P><Q>wrNxrPp2CD/jpvP4cMQq4dTI0z0/eW97cjoAYlqTovXyenFjdPUWoEme+2gQ16WhdD7wJ4W7LToxPpTRNWLgGw==</Q><DP>XUw7RTR71l9WlIv4lwjxiixvXd1LW9mQrB0Y1RZvkqXVklnFN4Oe7311Ign0xGO7lF1hDvF37GDH8a75CuVl7w==</DP><DQ>siKtub656RhTN/f1cW75cP9W8nYSMg++mSbaHSKT+0AdJsvBXEu09NgG3iw7ZKIE0y+WWAKx21JnpcNQmhCpyw==</DQ><InverseQ>VT89csWuTdl/QQICf34BXvIZM7K4cunSXbHWw7d1w6suVRk8jidvSfIr+9r4O5XtmMMRsB79fXl7zLW6t0f9dg==</InverseQ><D>A1ntt65UtMZtspz8JyH0ck+dX34Zak7sTH1GqFUHUBJZkn2LNxO7xONKvJ5Bo2TxbMNbFtYLGTkCyg2R2JjN8YQhPoxdmLGANCPQMCz8ffl9dhAN/j4lWHl0ndqYScZ4eEBopCUZpCltCC0rtL9q9TuwW9nNtoemQeIV/HZ8+z0=</D></RSAKeyValue>";
        public static void GetPublicAndPrivateKey(out string privateKey, out string publicKey)
        {
            RSACryptoServiceProvider rsaCryptoServiceProvider = GetRSACryptoServiceProvider();
            privateKey = rsaCryptoServiceProvider.ToXmlString(true);
            publicKey = rsaCryptoServiceProvider.ToXmlString(false);
        }

        public static byte[] EncryptData(string publicKey, byte[] clearText)
        {
            RSACryptoServiceProvider rsaCryptoServiceProvider = GetRSACryptoServiceProvider();
            publicKey = string.IsNullOrWhiteSpace(publicKey) ? DefaultPublicKey : publicKey;
            rsaCryptoServiceProvider.FromXmlString(publicKey);
            byte[] baCipherbytes = rsaCryptoServiceProvider.Encrypt(clearText, false);
            return baCipherbytes;
        }

        public static byte[] DecryptData(string privateKey, byte[] encryptedText)
        {
            try
            {
                RSACryptoServiceProvider rsaCryptoServiceProvider = GetRSACryptoServiceProvider();
                privateKey = string.IsNullOrWhiteSpace(privateKey) ? DefaultPrivateKey : privateKey;
                byte[] baGetPassword = encryptedText;
                rsaCryptoServiceProvider.FromXmlString(privateKey);
                byte[] baPlain = rsaCryptoServiceProvider.Decrypt(baGetPassword, false);
                return baPlain;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void DeleteSavedKeyFromContainer(string containerName)
        {
            var cp = new CspParameters
            {
                KeyContainerName = containerName,
                Flags = CspProviderFlags.UseMachineKeyStore
            };

            using (var rsa = new RSACryptoServiceProvider(cp))
            {
                rsa.PersistKeyInCsp = false;
                rsa.Clear();
            }
        }

        private static RSACryptoServiceProvider GetRSACryptoServiceProvider()
        {
            const int PROVIDER_RSA_FULL = 1;
            const string CONTAINER_NAME = "HintDeskRSAContainer";

            var cspParams = new CspParameters(PROVIDER_RSA_FULL)
            {
                KeyContainerName = CONTAINER_NAME,
                Flags = CspProviderFlags.UseMachineKeyStore,
                ProviderName = "Microsoft Strong Cryptographic Provider"
            };


            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            if (account != null)
            {
                CryptoKeyAccessRule rule = new CryptoKeyAccessRule(account.Value, CryptoKeyRights.FullControl, AccessControlType.Allow);
                cspParams.CryptoKeySecurity = new CryptoKeySecurity();
                cspParams.CryptoKeySecurity.SetAccessRule(rule);
            }


            return new RSACryptoServiceProvider(1024, cspParams);
        }
    }

}
