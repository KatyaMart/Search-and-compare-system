using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Imitator_for_transfer_result
{
    class Program
    {
        static void Main(string[] args)
        {
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11001);

            Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(ipEndPoint);

            List<byte> list_byte = new List<byte>();
            list_byte.Add(0);           //Шифрование
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
            list_byte.AddRange(BitConverter.GetBytes(password.Length));
            list_byte.AddRange(Encoding.UTF8.GetBytes(password));
            byte[] msg = list_byte.ToArray();
            sender.Send(msg);
            list_byte.Clear();

            //Ожидание ответа

            list_byte.Add(0);           //Шифрование
            list_byte.Add(3);           //Тип сообщения
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
            list_byte.AddRange(BitConverter.GetBytes(password.Length));
            list_byte.AddRange(Encoding.UTF8.GetBytes(password));
            Console.WriteLine("Введите ключ пользователя: ");
            int key = Convert.ToInt32(Console.ReadLine());
            list_byte.AddRange(BitConverter.GetBytes(key));
            msg = list_byte.ToArray();
            sender.Send(msg);
        }
    }
}
