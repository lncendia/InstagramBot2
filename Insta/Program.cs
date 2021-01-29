using System;

namespace Insta
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Bot.Start();
            Console.WriteLine("The bot has started, press any button to turn it off.");
            Console.ReadKey();
        }
    }
}
