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
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading;
namespace TCPServer
{
    class Program
    {
        static Int32 MAX_SIZE = 5*1024*1024;
        static string BaseAddress = "http://localhost:12000/";//"http://192.168.1.130:8080/";
        static string baseLogin;
        static string basePassword;
        static string key = "abcabcaabcabcabc";
        static int encryptionType = 0;
        
        static void Main(string[] args)
        {
            string[] str = File.ReadAllLines("BaseKey.txt");
            baseLogin = str[0];
            basePassword = str[1];
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
                    
                    byte[] encrypt = new byte[MAX_SIZE];
                    byte[] shifr = new byte[1];
                    handler = listener.AcceptSocket();
                    NetworkStream ns = new NetworkStream(handler);
                    ns.Read(shifr, 0, 1);
                    int bytesRec = ns.Read(encrypt, 0, MAX_SIZE);
                    byte[] bytes = new byte[bytesRec];
                    if (shifr[0] == 0)
                    {
                        Array.Copy(encrypt, bytes, bytesRec);
                        bytes = AES.AES_Decrypt(bytes, Encoding.UTF8.GetBytes(key));
                    }
                    else if (shifr[0] == 1)
                    {
                        Array.Copy(encrypt, bytes, bytesRec);
                        string PublicKey, PrivateKey;
                        RSA.GetPublicAndPrivateKey(out PrivateKey,out PublicKey);
                        bytes = RSA.DecryptData(PrivateKey,bytes);
                    }
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
                        byte[] msg_length = new byte[4];
                        ns.Read(msg_length,0,4);
                        int length = BitConverter.ToInt32(msg_length,0);
                        int readed = 0,counter =0;
                        List<byte> msg_b = new List<byte>();
                        while (counter<length)
                        {
                            readed = ns.Read(buffer, 0, (Int32)MAX_SIZE);
                            byte[] received = new byte[readed];
                            Array.Copy(buffer,received,readed);
                            msg_b.AddRange(received);
                            counter += readed;
                        }

                        if (shifr[0] == 0)
                        {
                            buffer = AES.AES_Decrypt(msg_b.ToArray(), Encoding.UTF8.GetBytes(key));
                        }
                        else if (shifr[0] == 1)
                        {
                            string PublicKey, PrivateKey;
                            RSA.GetPublicAndPrivateKey(out PrivateKey, out PublicKey);
                            buffer = RSA.DecryptData(PrivateKey, msg_b.ToArray());
                        }

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
                        byte[] image = new byte[leng];
                        Array.Copy(buffer, i, image, 0, leng);
                        ImageConverter conv = new ImageConverter();
                        Image img = (Image)conv.ConvertFrom(image);
                        switch (type)
                        {
                            case 0:
                                img.Save(@"result.png", System.Drawing.Imaging.ImageFormat.Png);
                                break;
                            case 1:
                                img.Save(@"result.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
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
            request.Headers.Add("login",baseLogin);
            request.Headers.Add("password",basePassword);
            //request.Headers.Add("encryption_type:" + encryptionType);
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
    public class EncruptAlg
    {
        public enum EncAlgType
        {
            AES,
            RSA,
            MAGMA
        }
        public static string keystr = "abcabcabcaabcabc";
        public static byte[] key = Encoding.UTF8.GetBytes(keystr);

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
                AES.Mode = CipherMode.CBC;
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
                AES.Mode = CipherMode.CBC;
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
