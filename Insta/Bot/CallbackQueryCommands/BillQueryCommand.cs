using System;
using System.Threading.Tasks;
using Insta.Interfaces;
using Insta.Payments;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class BillQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        if (new Payment().CheckPay(user, query.Data[5..]))
        {
            var message = query.Message.Text;
            message = message.Replace("❌ Статус: Не оплачено", "✔ Статус: Оплачено");
            message = message.Remove(message.IndexOf("Оплачено", StringComparison.Ordinal) + 8);
            await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                message);
            await client.AnswerCallbackQueryAsync(query.Id, "Успешно оплачено.");
            if (user.Referal == null) return;
            await using var db = new Db();
            db.UpdateRange(user, user.Referal);
            user.Referal.Bonus += BotSettings.Cfg.Bonus;
            try
            {
                await client.SendTextMessageAsync(user.Referal.Id,
                    $"По вашей реферальной ссылке перешел пользователь. Вам зачисленно {BotSettings.Cfg.Bonus} бонусных рублей.");
            }
            catch
            {
                //ignored
            }

            user.Referal = null;
            await db.SaveChangesAsync();
            return;
        }

        await client.AnswerCallbackQueryAsync(query.Id, "Не оплачено.");
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data.StartsWith("bill");
    }
}