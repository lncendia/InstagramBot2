using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LoadProxy
{
    class Program
    {
        private static readonly List<string> NotLoadedProxy = new();
        static void Main(string[] args)
        {
            Console.WriteLine("Drag the file to the console.");
            var path = Console.ReadLine();
            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine("Вы не ввели путь к файлу.");
            }
            else
            {
                foreach (var data in File.ReadAllLines(path))
                {
                    Console.WriteLine(AddProxy(data) ? $"{data} - загружена." : $"{data} не загружена.");
                }
            }

            if (NotLoadedProxy.Any())
            {
                Console.WriteLine("The unloaded proxies will be in the file log.txt");
                using var sw = new StreamWriter("log.txt", false);
                NotLoadedProxy.ForEach(async _=>
                {
                    await sw.WriteLineAsync(_);
                });
            }
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }

        private static bool AddProxy(string credentials)
        {
            try
            {
                var data = credentials.Split(':');
                if (data.Length != 4) return false;
                using var db = new Db();
                var proxy = new Proxy
                {
                    Host = data[0],
                    Port = int.Parse(data[1]),
                    Login = data[2],
                    Password = data[3]
                };
                db.Add(proxy);
                db.SaveChanges();
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                NotLoadedProxy.Add(credentials);
                return false;
            }
        }
    }
}