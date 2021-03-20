using System;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Payments;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands
{
    public class EnterCountSubscribesCommand : ITextCommand
    {
        public async Task Execute(TelegramBotClient client, User user, Message message)
        {
            int count;
            if (!int.TryParse(message.Text, out count))
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Введите число!");
                return;
            }

            if (count > 100)
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Слишком большое количество!");
                return;
            }

            user.State = State.main;
            string billId = "";
            var payUrl = Payment.AddTransaction(count * 120, user, ref billId);
            if (payUrl == null)
            {
                await client.SendTextMessageAsync(message.From.Id,
                    "Произошла ошибка при создании счета. Попробуйте еще раз.");
                return;
            }

            await client.SendTextMessageAsync(message.From.Id,
                $"💸 Оплата подписки на сумму {count * 120} р.\n📆 Дата: {DateTime.Now:dd.MMM.yyyy}\n❌ Статус: Не оплачено.\n\n💳 Оплатите счет по ссылке.\n{payUrl}",
                replyMarkup: Keyboards.CheckBill(billId));
        }

        public bool Compare(Message message, User user)
        {
            return message.Type == MessageType.Text && user.State == State.enterCountToBuy;
        }
    }
}