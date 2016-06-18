using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Security.AccessControl;

namespace TCPClient
{
    class Program
    {
        private static string key = "abcabcaabcabcabc";
        static void Main(string[] args)
        {
            try
            {
                SendMessageFromSocket(11000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }
        static void SendMessageFromSocket(int port)
        {
            byte[] bytes = new byte[1024];
            byte shf = 1;
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

            Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            sender.Connect(ipEndPoint);
            Console.WriteLine("Введите логин для аутентификации: ");
            string login_auth = Console.ReadLine();
            Console.WriteLine("Введите пароль для аутентификации: ");
            string pass_auth = Console.ReadLine();

            // Console.WriteLine("Сокет соединяется с {0}",sender.RemoteEndPoint.ToString());
            byte[] log = Encoding.UTF8.GetBytes(login_auth);
            int log_len = log.Length;
            byte[] password = Encoding.UTF8.GetBytes(pass_auth);
            int pas_len = password.Length;
            byte[] mes = new byte[log_len + pas_len + 8];
            BitConverter.GetBytes(log_len).CopyTo(mes,0);
            log.CopyTo(mes, 4);
            BitConverter.GetBytes(pas_len).CopyTo(mes, 4+log.Length);
            password.CopyTo(mes, log.Length+8);
            List<byte> msg_list = new List<byte>();
            msg_list.Add(shf);
            if (shf == 0)
            {
                msg_list.AddRange(AES.AES_Encrypt(mes,Encoding.UTF8.GetBytes(key)));
            }
            else if (shf == 1)
            {
                string PrivateKey, PublicKey;
                RSA.GetPublicAndPrivateKey(out PrivateKey,out PublicKey);
                msg_list.AddRange(RSA.EncryptData(PublicKey,mes));
            }
            int bytesSent = sender.Send(msg_list.ToArray());

            int bytesRec = sender.Receive(bytes);
            if (bytes[0] == 1)
            {
                Console.WriteLine("Доступ разрешен!\n");
                
                Console.WriteLine("Введите '1' для задание количества результата и любой другой символ для задания точности:");
                string numRes = Console.ReadLine();
                double accurancy = 0.0f;
                int num_res = 0;
                if (numRes != "1")
                {
                    Console.WriteLine("Укажите точность:");
                    accurancy = Convert.ToDouble(Console.ReadLine());
                }
                else
                {
                    Console.WriteLine("Укажите количество результатов:");
                    num_res = Convert.ToInt32(Console.ReadLine());
                }
                
                Image img = Image.FromFile(@"cart.jpg");
                ImageConverter conv = new ImageConverter();
                byte[] img_byte;
                    img_byte = (byte[])conv.ConvertTo(img,typeof(byte[]));

                int msg_len = 1 + sizeof(int) + 1+ img_byte.Length;
                if (accurancy == 0)
                    msg_len += sizeof(int) ;
                else
                    msg_len+= sizeof(double);
                byte[] msg = new byte[msg_len];
                int i = 0;
                if (accurancy == 0)
                {
                    msg[i] = 1;
                    i++;
                    BitConverter.GetBytes(num_res).CopyTo(msg, i);
                    i += sizeof(int);
                }
                else
                {
                    msg[i] = 0;
                    i++;
                    BitConverter.GetBytes(accurancy).CopyTo(msg, i);
                    i += sizeof(double);
                }
                BitConverter.GetBytes(img_byte.Length).CopyTo(msg, i);
                i += sizeof(int);
                msg[i] = 1;
                i++;
                img_byte.CopyTo(msg, i);
                msg_list = new List<byte>();
                msg_list.Add(shf);
                if (shf == 0)
                {
                    msg_list.AddRange(AES.AES_Encrypt(msg, Encoding.UTF8.GetBytes(key)));
                }
                else if (shf == 1)
                {
                    string PrivateKey, PublicKey;
                    RSA.GetPublicAndPrivateKey(out PrivateKey, out PublicKey);
                    msg_list.AddRange(RSA.EncryptData(PublicKey, msg));
                }
                bytesSent = sender.Send(msg_list.ToArray());
                Console.WriteLine("Сообщение отправлено, было отправлено "+ bytesSent + " байт\n");
            }
            else
            {
                Console.WriteLine("Отказано в доступе");
            }



            /*int bytesRec = sender.Receive(bytes);
            if (bytes[0] == 1)
            {
                Console.WriteLine("Вам надо 1 результат или несколько? нажмите '1' для одного и остальное для нескольких:");
                string numRes = Console.ReadLine();
                if (numRes != "1")
                {
                    Console.WriteLine("Укажите точность:");
                    int accurancy = Console.Read();
                }
                    
                Image img = Image.FromFile(@"cart.png");
                MemoryStream memoryStream = new MemoryStream();
                img.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] msg = memoryStream.ToArray();
                bytesSent = sender.Send(msg);
                bytesRec = sender.Receive(bytes);
                Console.WriteLine("\nОтвет от сервера: {0}\n\n", Encoding.UTF8.GetString(bytes, 0, bytesRec));
            }
            else if (bytes[0] == 0)
            {
                Console.WriteLine("Неверный логин!\n");
            }*/

            //if (message.IndexOf("<TheEnd>") == -1)
            //   SendMessageFromSocket(port);
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
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
