using System;
using Insta.Bot;
using System.IO;

namespace Insta
{
    internal static class Program
    {
        public const string Token = "1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o";

        static void Main(string[] args)
        {
            MainBot.Start();
            Console.WriteLine("The bot has started. Enter \"1\" to load proxy.");
            while (true)
            {
                try
                {
                    var key = Console.ReadKey();
                    if (key.KeyChar != '1') continue;
                    Console.WriteLine("\nJust drag and drop the file to the console.");
                    var path = Console.ReadLine();
                    if (path == null) return;
                    var proxy = File.ReadAllLines(path);
                    foreach (var p in proxy)
                    {
                        string successful = Working.Operation.AddProxy(p)
                            ? $"{p} загружена успешно."
                            : $"{p} не загружена.";
                        Console.WriteLine(successful);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
