using System;
using Insta.Bot;

namespace Insta
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            MainBot.Start();
            Console.WriteLine("The bot has started. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
