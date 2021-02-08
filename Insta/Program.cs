using System;

namespace Insta
{
    internal static class Program
    {
        public const string Token = "1485092461:AAGcPpPwxfSTnQ8cM3FWPFirvGIDjs84Pto";//"1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o";

        static void Main(string[] args)
        {
            Bot.Start();
            Console.WriteLine("The bot has started, press any button to turn it off.");
            Console.ReadKey();
        }
    }
}
