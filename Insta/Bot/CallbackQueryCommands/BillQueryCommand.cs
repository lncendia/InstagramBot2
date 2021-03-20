using System;
using System.Threading.Tasks;
using Insta.Interfaces;
using Insta.Payments;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class BillQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            if (Payment.CheckPay(user, query.Data[5..]))
            {
                string message = query.Message.Text;
                message = message.Replace("❌ Статус: Не оплачено", "✔ Статус: Оплачено");
                message = message.Remove(message.IndexOf("Оплачено", StringComparison.Ordinal) + 8);
                await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                    message);
                await client.AnswerCallbackQueryAsync(query.Id, "Успешно оплачено.");
            }
            await client.AnswerCallbackQueryAsync(query.Id, "Не оплачено.");
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data.StartsWith("bill");
        }
    }
}