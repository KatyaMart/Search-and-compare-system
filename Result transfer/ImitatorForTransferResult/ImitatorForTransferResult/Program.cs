using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Security.AccessControl;
using System.IO;

namespace Imitator_for_transfer_result
{
    class Program
    {
        static string key = "abcabcaabcabcabc";
        static byte shf = 0;
        static void Main(string[] args)
        {
            Thread t = new Thread(Listen2msg);
            t.Start();
            
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11001);

            Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                sender.Connect(ipEndPoint);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Невозможно подключиться к серверу!");
                Console.ReadLine();
                return;
            }
            try
            {
                List<byte> list_byte = new List<byte>();
                list_byte.Add(1);           //Тип сообщения
                string login;
                do
                {
                    Console.WriteLine("Введите логин: ");
                    login = Console.ReadLine();
                    if (login.Length > 127)
                        Console.WriteLine("Слишком длинный логин! Повторите");
                } while (login.Length > 127);
                list_byte.Add((byte)login.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(login));
                Console.WriteLine("Введите пароль: ");
                string password = Console.ReadLine();
                list_byte.Add((byte)password.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(password));
                List<byte> msg = new List<byte>();
                msg.Add(shf);
                if (shf == 0)
                    msg.AddRange(AES.AES_Encrypt(list_byte.ToArray(),Encoding.UTF8.GetBytes(key)));
                else if (shf == 1)
                {
                    string PublicKey, PrivateKey;
                    RSA.GetPublicAndPrivateKey(out PrivateKey, out PublicKey);
                    msg.AddRange(RSA.EncryptData(PublicKey,list_byte.ToArray()));
                }
                sender.Send(msg.ToArray());
                list_byte.Clear();
                msg.Clear();
                //Ожидание ответа
                
                byte[] decrypted_bytes = new byte[10240];
                int bytesRec = sender.Receive(decrypted_bytes);
                byte[] bytes = new byte[bytesRec - 1];
                if (decrypted_bytes[0] == 0)
                {
                    Array.Copy(decrypted_bytes, 1, bytes, 0, bytesRec - 1);
                    bytes = AES.AES_Decrypt(bytes,Encoding.UTF8.GetBytes(key));
                }
                else if (decrypted_bytes[0] == 1)
                {
                    Array.Copy(decrypted_bytes, 1, bytes, 0, bytesRec - 1);
                    string PublicKey, PrivateKey;
                    RSA.GetPublicAndPrivateKey(out PrivateKey, out PublicKey);
                    bytes = RSA.DecryptData(PrivateKey,bytes);
                }
                if (bytes[0] == 1)
                {
                    int i = 1;
                    while (i < bytes.Length)
                    {
                        string userName;
                        int id = BitConverter.ToInt32(bytes, i);
                        i += sizeof(Int32);
                        int leng = bytes[i];
                        userName = Encoding.UTF8.GetString(bytes, i + 1, leng);
                        i += 1 + leng;
                        Console.WriteLine(id + ": " + userName);
                    }
                }
                else if (bytes[0] == 101)
                {
                    Console.WriteLine("Отказано в доступе!");
                }
                sender.Close();

                //=================================отправка 3его типа сообщения====================

                list_byte.Add((byte)3);           //Тип сообщения
                do
                {
                    Console.WriteLine("Введите логин: ");
                    login = Console.ReadLine();
                    if (login.Length > 127)
                        Console.WriteLine("Слишком длинный логин! Повторите");
                } while (login.Length > 127);
                list_byte.Add((byte)login.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(login));
                Console.WriteLine("Введите пароль: ");
                password = Console.ReadLine();
                list_byte.Add((byte)password.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(password));
                Console.WriteLine("Введите логин нужного пользователя: ");
                string user = Console.ReadLine();
                list_byte.Add((byte)user.Length);
                list_byte.AddRange(Encoding.UTF8.GetBytes(user));
                msg = new List<byte>();
                msg.Add(shf);
                if (shf == 0)
                    msg.AddRange(AES.AES_Encrypt(list_byte.ToArray(), Encoding.UTF8.GetBytes(key)));
                else if (shf == 1)
                {
                    string PublicKey, PrivateKey;
                    RSA.GetPublicAndPrivateKey(out PrivateKey, out PublicKey);
                    msg.AddRange(RSA.EncryptData(PublicKey, list_byte.ToArray()));
                }
                sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sender.Connect(ipEndPoint);
                sender.Send(msg.ToArray());


                decrypted_bytes = new byte[10240];
                bytesRec = sender.Receive(decrypted_bytes);
                bytes = new byte[bytesRec - 1];
                if (decrypted_bytes[0] == 0)
                {
                    Array.Copy(decrypted_bytes, 1, bytes, 0, bytesRec - 1);
                    bytes = AES.AES_Decrypt(bytes, Encoding.UTF8.GetBytes(key));
                }
                else if (decrypted_bytes[0] == 1)
                {
                    Array.Copy(decrypted_bytes, 1, bytes, 0, bytesRec - 1);
                    string PrivateKey, PublicKey;
                    RSA.GetPublicAndPrivateKey(out PrivateKey,out PublicKey);
                    bytes = RSA.DecryptData(PrivateKey,bytes);
                }
                if (bytes[0] == 3)
                {
                    int i = 1;
                    while (i < bytes.Length)
                    {
                        int length = BitConverter.ToInt32(bytes, i);
                        i += sizeof(Int32);
                        string reference = Encoding.UTF8.GetString(bytes, i, length);
                        i += length;
                        Console.WriteLine(reference);
                    }
                }
                else if (bytes[0] == 103)
                {
                    Console.WriteLine("Отказано в доступе");
                }

                sender.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.ReadLine();
        }
        static void Listen2msg()
        {
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11002);
            TcpListener listener = new TcpListener(ipEndPoint);
            listener.Start();

            try
            {
                Socket handler = listener.AcceptSocket();
                byte[] bytes = new byte[10240];
                int bytesRec = handler.Receive(bytes);
                if (bytes[1] == 2)
                {
                    int i=2;
                    string login = Encoding.UTF8.GetString(bytes,i+1,bytes[i]);
                    i += login.Length + 1;
                    Console.WriteLine("Пользователь "+login+" добавид результаты:");
                    while(i<bytesRec)
                    {
                        string refer = Encoding.UTF8.GetString(bytes, i + 1, bytes[i]);
                        i += refer.Length + 1;
                        Console.WriteLine(refer);
                    }
                }
                else
                {
                    Console.WriteLine("Посторонее сообщение");
                }
            }
            catch (Exception ex)
            {

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
