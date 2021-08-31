using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class PaymentCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            await client.SendTextMessageAsync(message.Chat.Id,
                $"Введите количество аккаунтов, которые хотите добавить. Цена одного аккаунта - {BotSettings.Cfg.Cost} рублей/30 дней.",
                replyMarkup: Keyboards.Main);
            user.State = State.enterCountToBuy;
        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && message.Text == "💰 Оплатить подписку" &&
                   user.State == State.main;
        }
    }
}