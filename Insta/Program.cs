using System;
using Insta.Bot;
using System.IO;
using System.Threading.Tasks;

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
