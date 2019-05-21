using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using System.Windows;

namespace CheckOnlineVkfaces
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Инициализация интерфейсом -------
            Console.Write("Интервал в секундах: ");
            if (int.TryParse(Console.ReadLine(), out int sec) == false || sec < 3)
            {
                Console.WriteLine("Интервал задан не верно, интервал будет установлен по-умолчанию = 20");
                sec = 20;
            }
            Console.WriteLine($"Нужно выводить сообщение Windows? {false.ToString()} - нет, {true.ToString()} - да");
            bool writeWindows = false;
            while (!bool.TryParse(Console.ReadLine(), out writeWindows)) ;
            if(writeWindows)
                MessageBox.Show("Hello world!"); // Тест, что окошко может выводиться.

            Manager manager = new Manager(sec, writeWindows);
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
            /// <param name="sec">Количество секунд на обновление.</param>
            /// <param name="writeWindows">True, если надо вывести окошко при смене состояния. Иначе - False.</param>
            public Manager(int sec = 20, bool writeWindows = false)
            {
                IntervalSec = sec;
                this.writeWindows = writeWindows;
            }

            /// <summary>
            /// Количество секунд на обновление.
            /// </summary>
            private readonly int IntervalSec;

            /// <summary>
            /// True, если надо вывести окошко при смене состояния. Иначе - False.
            /// </summary>
            private readonly bool writeWindows;

            /// <summary>
            /// Список пользователей.
            /// </summary>
            private ISet<Client> Clients = new HashSet<Client>();

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
                Clients.Add(client);
                client.Start(IntervalSec * 1000);
                Console.WriteLine(DateTime.Now + " ADD: " + client);
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
                if (newDate.Contains("онлайн"))
                {
                    Console.Beep(1000, 500);
                }
                else
                {
                    Console.Beep(1000, 200);
                    Console.Beep(37, 100);
                    Console.Beep(1000, 200);
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
                timer.Elapsed += Update;
            }

            /// <summary>
            /// Текущий vkid пользователя.
            /// </summary>
            public string Vkid { get; }

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
            /// Таймер, отвечающий за вызов обновления.
            /// </summary>
            private Timer timer = new Timer();

            /// <summary>
            /// Запускает слежку за активностью входов и выходов пользователя.
            /// </summary>
            /// <param name="updateTime">Интервал обновления за пользователем.</param>
            public void Start(int updateTime = 20 * 1000)
            {
                timer.Interval = updateTime;
                timer.Start();
            }

            /// <summary>
            /// Вызывается таймером <see cref="timer"/>.
            /// Метод проверяет, изменился ли пользователь. Если да, то вызываются методы в <see cref="Change"/>.
            /// </summary>
            private void Update(object sender, ElapsedEventArgs e)
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
                string data = ReadAsDotnet("https://vkfaces.com/", "vk/user/" + Vkid);
                
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
                return data;
            }

            private string ReadAsXNet(string server, string addr)
            {
                using (var client = new xNet.HttpRequest(server))
                {
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

            /// <summary>
            /// Текстовое представление об пользователе.
            /// </summary>
            public override string ToString()
            {
                return Vkid + ": " + Old;
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
