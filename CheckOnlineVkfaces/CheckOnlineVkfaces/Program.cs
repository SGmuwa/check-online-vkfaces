using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using System.Collections;

namespace CheckOnlineVkfaces
{
    class Program
    {
        static void Main(string[] args)
        {
            string old = GetDate("id302186940");
            Console.Beep(2000, 500);
            string young = null;
            Console.WriteLine(old);
            do
            {
                Thread.Sleep(1000 * 20);
                young = GetDate("id302186940");
            } while (young.Equals(old));
            Console.WriteLine(young);
            Console.Beep(2000, 1000 * 10);
            Console.ReadLine();
        }


        class Manager
        {
            public Manager()
            {

            }

            private IList<Client> Clients = new List<Client>();

            public void Add(Client client)
            {
                client.Change += Update;
                Clients.Add(client);
                Console.WriteLine(DateTime.Now + " ADD: " + client);
            }

            private void Update(Client client, string newDate)
            {
                Console.WriteLine(DateTime.Now + " UPDATE: " + client + ": " + newDate);
                Console.Beep(2000, 1000 * 10);
            }
        }

        class Client
        {
            public Client(string vkid)
            {
                Vkid = vkid;
                old = GetDate();
                timer.Elapsed += Update;
            }

            public string Vkid { get; }

            private string old;
            private string young;
            private event Action<Client, string> _change;
            public event Action<Client, string> Change {
                add
                {
                    _change += value;
                }
                remove
                {
                    _change -= value;
                }
            }

            System.Timers.Timer timer = new System.Timers.Timer(1000);

            public void Start(int updateTime = 1000)
            {
                timer.Interval = updateTime;
                timer.Start();
            }

            private void Update(object sender, ElapsedEventArgs e)
            {
                if(IsChange())
                {
                    _change(this, young);
                }
            }

            public bool IsChange()
            {
                young = GetDate();
                if(young.Equals(old))
                {
                    return false;
                }
                else
                {
                    old = young;
                    return true;
                }
            }

            public string GetDate()
            {
                string data = "";
                using (var client = new WebClient())
                {
                    byte[] bytes = client.DownloadData("https://vkfaces.com/vk/user/" + Vkid);
                    char[] outchars = new char[bytes.Length * 2];
                    Encoding.UTF8.GetDecoder().Convert(bytes, 0, bytes.Length, outchars, 0, outchars.Length, true, out int a, out a, out bool completed);
                    data = new string(outchars).Replace("\0", "").Trim();
                }
                int begin = data.IndexOf("Последнее посещение");
                if (begin > 0)
                    data = data.Remove(0, begin);
                begin = data.IndexOf("user-field__value");
                if (begin > 0)
                    data = data.Remove(0, begin + "user-field__value".Length + "\" >\n          ".Length);
                begin = data.IndexOf("\n");
                if (begin > 0)
                    data = data.Remove(begin, data.Length - begin);
                return data;
            }

            public override string ToString()
            {
                return Vkid;
            }
        }

    }
}
