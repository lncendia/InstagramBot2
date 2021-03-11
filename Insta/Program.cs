using System;
using Insta.Bot;
using System.IO;
using System.Threading.Tasks;

namespace Insta
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            await MainBot.Start();
            Console.WriteLine("The bot has started. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
