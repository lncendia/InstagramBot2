using Telegram.Bot;

namespace Insta.Bot
{
    public static class BotSettings
    {
        public static TelegramBotClient Get()
        {
            return _client ??= new TelegramBotClient("1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o");
        }

        private static TelegramBotClient _client;
    }
}