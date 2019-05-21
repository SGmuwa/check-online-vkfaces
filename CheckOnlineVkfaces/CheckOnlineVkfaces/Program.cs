using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CheckOnlineVkfaces
{
    class Program
    {
        static void Main(string[] args)
        {
            string data = "";
            using (var client = new WebClient())
            {
                data = client.DownloadString("https://vkfaces.com/vk/user/id302186940");
            }
            int begin = data.IndexOf("Последнее посещение");
            data = data.Remove(0, begin);
            Console.WriteLine(data);
            Console.ReadLine();
        }
    }
}
