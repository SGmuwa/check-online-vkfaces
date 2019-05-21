using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;
using System.Speech.Synthesis;

namespace CheckOnlineVkfaces
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Инициализация интерфейсом -------
            Console.WriteLine($"Нужно выводить сообщение Windows? Напишите {false.ToString()} или {true.ToString()}");
            bool writeWindows = false;
            while (!bool.TryParse(Console.ReadLine(), out writeWindows)) ;
            if (writeWindows)
                MessageBox.Show("Hello world!"); // Тест, что окошко может выводиться.
            Console.WriteLine($"Нужно ли голосове оповещение? Напишите {false.ToString()} или {true.ToString()}");
            bool alarm = false;
            while (!bool.TryParse(Console.ReadLine(), out alarm)) ;
            if (alarm)
                Console.Beep(1000, 200);
            Manager manager = new Manager(writeWindows, alarm);
            manager.Start();
            // Добавление пользователей -------
            Console.WriteLine("Напишите ид ВК пользователей через новую строку каждый:");
            do
            {
                manager.Add(Console.ReadLine());
            } while (true);
        }

        /// <summary>
        /// Класс отвечает за управление пользователями.
        /// </summary>
        public class Manager
        {
            /// <summary>
            /// Инициализация менеджера.
            /// </summary>
            /// <param name="writeWindows">True, если надо вывести окошко при смене состояния. Иначе - False.</param>
            /// <param name="alarm">Нужно ли звуковое оповещение?</param>
            public Manager(bool writeWindows = false, bool alarm = true)
            {
                this.writeWindows = writeWindows;
                this.alarm = alarm;
                synth.SetOutputToDefaultAudioDevice();
            }

            /// <summary>
            /// True, если надо вывести окошко при смене состояния. Иначе - False.
            /// </summary>
            private readonly bool writeWindows;

            /// <summary>
            /// True, если нужно звуковое оповещение при смене состояния. Иначе - False.
            /// </summary>
            private readonly bool alarm;

            /// <summary>
            /// Список пользователей.
            /// </summary>
            private ISet<Client> Clients = new HashSet<Client>();

            private Queue<Client> ToAdd = new Queue<Client>();

            /// <summary>
            /// Количество клиентов под управлением.
            /// </summary>
            public int Count => Clients.Count;

            /// <summary>
            /// Добавление пользователя в список проверок.
            /// </summary>
            /// <param name="Vkid">Ид интересного пользователя.</param>
            public void Add(string Vkid)
            {
                try
                {
                    Add(new Client(Vkid));
                    System.Threading.Thread.Sleep(500);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Не удалось добавить пользователя: " + e.Message);
                }
            }

            /// <summary>
            /// Добавление пользователя в список проверок.
            /// </summary>
            /// <param name="client">Интересующий нас пользователь.</param>
            public void Add(Client client)
            {
                client.Change += Update;
                ToAdd.Enqueue(client);
            }

            private SpeechSynthesizer synth = new SpeechSynthesizer();

            public void Start()
            {
                new System.Threading.Thread(() =>
                {
                    while (true)
                    {
                        lock (Clients)
                        {
                            foreach (Client client in Clients)
                            {
                                client.Update();
                                System.Threading.Thread.Sleep(500);
                            }
                            while (ToAdd.Count > 0)
                            {
                                Client cl = ToAdd.Dequeue();
                                Clients.Add(cl);
                                Console.WriteLine(DateTime.Now + " ADD: " + cl);
                                System.Threading.Thread.Sleep(500);
                            }
                        }
                    }
                }).Start();
            }

            /// <summary>
            /// Происходит при изменении статуса пользователя.
            /// </summary>
            /// <param name="client">Пользователь, который изменил статус.</param>
            /// <param name="newDate">Обновлённое текстовое представление статуса пользователя.</param>
            private void Update(Client client, string newDate)
            {
                string message = DateTime.Now + " UPDATE: " + client;
                Console.WriteLine(message);
                if (alarm)
                {
                    var name = client.Name.Split(' ');
                    string toSay = "";
                    for(int i = 0; i < 2 && i < name.Length; i++)
                    {
                        toSay += name[i] + " ";
                    }
                    if (newDate.Contains("онлайн"))
                    {
                        synth.Speak("Зашёл: " + toSay);
                    }
                    else
                    {
                        synth.Speak("Вышел: " + toSay);
                    }
                }
                if(writeWindows)
                    MessageBox.Show(message);
            }
        }

        /// <summary>
        /// Представляет одного пользователя ВК.
        /// </summary>
        public class Client
        {
            /// <summary>
            /// Инициализация клиента.
            /// </summary>
            /// <param name="vkid">Ид вк пользователя или его вк-ник, который нас интересует.</param>
            public Client(string vkid)
            {
                Vkid = vkid;
                Old = GetDate_unsafe();
            }

            /// <summary>
            /// Текущий vkid пользователя.
            /// </summary>
            public string Vkid { get; }

            private string Name_ = null;

            /// <summary>
            /// Имя пользователя.
            /// </summary>
            public string Name {
                get
                {
                    if(Name_ == null)
                        updateName(ReadAsXNetForce("https://vkfaces.com/", "vk/user/" + Vkid));
                    return Name_;
                }
            }

            private void updateName(string httpFileContent)
            {
                int begin = httpFileContent.IndexOf("<title>") + "<title>".Length;
                int end = httpFileContent.IndexOf("</title>");
                Name_ = httpFileContent.Substring(begin, end - begin);
            }

            /// <summary>
            /// Текстовое представление даты захода пользователя.
            /// Поле меняется при вызове <see cref="GetDate"/> или <see cref="IsChange"/>.
            /// </summary>
            public string Old { get; private set; }
            private event Action<Client, string> _change;
            /// <summary>
            /// Происходит при изменении состояния пользователя.
            /// </summary>
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

            /// <summary>
            /// Метод проверяет, изменился ли пользователь. Если да, то вызываются методы в <see cref="Change"/>.
            /// </summary>
            public void Update()
            {
                if(IsChange())
                {
                    _change(this, Old);
                }
            }

            /// <summary>
            /// True, если пользователь изменился. Иначе - false.
            /// </summary>
            private bool IsChange()
            {
                string young = GetDate();
                if(young.Equals(Old))
                {
                    return false;
                }
                else
                {
                    Old = young;
                    return true;
                }
            }

            /// <summary>
            /// Получение данных о пользователе из интернета.
            /// </summary>
            /// <returns>Текстовое представление о последней активности заходов и выходов пользователя.</returns>
            private string GetDate()
            {
                try
                {
                    return GetDate_unsafe();
                } catch(Exception e)
                {
                    return e.Message;
                }
            }

            /// <summary>
            /// Получение данных о пользователе из интернета. Не гарантирует успешный отчёт об пользователе.
            /// </summary>
            /// <returns>Текстовое представление о последней активности заходов и выходов пользователя.</returns>
            private string GetDate_unsafe()
            {
                string data = ReadAsXNetForce("https://vkfaces.com/", "vk/user/" + Vkid);
                
                if(Name_ == null)
                    updateName(data);

                int begin = data.IndexOf("Последнее посещение");
                if (begin > 0)
                    data = data.Remove(0, begin);
                else
                    throw new Exception("Не найдена строка \"Последнее посещение\"");
                begin = data.IndexOf("user-field__value");
                if (begin > 0)
                    data = data.Remove(0, begin + "user-field__value".Length + "\" >\n          ".Length);
                else
                    throw new Exception("Не найдено что-то где-то рядом с \"user-field__value\"");
                begin = data.IndexOf("\n");
                if (begin > 0)
                    data = data.Remove(begin, data.Length - begin);
                else
                    throw new Exception("Новая строка не найдена.");
                return data.Trim();
            }

            private string ReadAsXNet(string server, string addr)
            {
                using (var client = new xNet.HttpRequest(server))
                {
                    return client.Get(addr).ToString();
                }
            }

            private string ReadAsXNetForce(string server, string addr)
            {
                using (var client = new xNet.HttpRequest(server))
                {
                    client.IgnoreProtocolErrors = true;
                    return client.Get(addr).ToString();
                }
            }

            private string ReadAsDotnet(string server, string addr)
            {
                string data;
                var client = WebRequest.CreateHttp(server + addr);
                client.KeepAlive = true;
                client.ReadWriteTimeout = 200000;
                client.Timeout = 200000;
                var resp = client.GetResponse();
                var srm = resp.GetResponseStream();
                var rsrm = new StreamReader(srm);
                data = rsrm.ReadToEnd();
                rsrm.Close();
                srm.Close();
                resp.Close();
                return data;
            }

            private string ReadAsWebClient(string server, string addr)
            {
                using (var client = new WebClient())
                {
                    byte[] bytes = client.DownloadData(server + addr);
                    char[] outchars = new char[bytes.Length * 2];
                    Encoding.UTF8.GetDecoder().Convert(bytes, 0, bytes.Length, outchars, 0, outchars.Length, true, out int a, out a, out bool completed);
                    return new string(outchars).Replace("\0", "").Trim();
                }
            }

            /// <summary>
            /// Текстовое представление об пользователе.
            /// </summary>
            public override string ToString()
            {
                return Vkid + " (" + Name + ")" + ": " + Old;
            }

            /// <summary>
            /// Отвечает на вопрос, являются ли два клиента эквивалентными.
            /// Сравнение идёт по <see cref="Vkid"/>.
            /// </summary>
            /// <param name="obj">Другой экземпляр <see cref="Client"/>, с которым идёт сравнение.</param>
            /// <returns>True, если они эквивалентны. Иначе - False.</returns>
            public override bool Equals(object obj)
            {
                if (!(obj is Client))
                    return false;
                return Vkid.Equals(((Client)obj).Vkid);
            }

            /// <summary>
            /// Получение Hash code пользователя.
            /// Фактически hash code от <see cref="Vkid"/>.
            /// </summary>
            /// <returns>Hash code от <see cref="Vkid"/>.</returns>
            public override int GetHashCode()
            {
                return Vkid.GetHashCode();
            }
        }

    }
}
