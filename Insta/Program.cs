using System;
using Insta.Bot;
using System.IO;
using System.Threading.Tasks;

namespace Insta
{
    internal static class Program
    {
        public const string Token = "1485092461:AAGcPpPwxfSTnQ8cM3FWPFirvGIDjs84Pto";

        static async Task Main(string[] args)
        {
            await MainBot.Start();
            Console.WriteLine("The bot has started. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
