using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Insta.Working;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class SelectAllAccountsQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        try
        {
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
        }
        catch
        {
            // ignored
        }

        user.State = State.block;
        foreach (var inst in user.Instagrams)
        {
            if (inst.IsDeactivated)
            {
                await client.SendTextMessageAsync(query.From.Id,
                    $"Аккаунт {inst.Username} деактивирован. Купите подписку, чтобы активировать аккаунт.");
                continue;
            }

            if (user.CurrentWorks.FirstOrDefault(x => x.Instagram.Username == inst.Username) != null)
            {
                await client.SendTextMessageAsync(query.From.Id,
                    $"Аккаунт {inst.Username} уже добавлен.");
                continue;
            }

            await client.SendTextMessageAsync(query.From.Id,
                $"Инстаграм {inst.Username} добавлен.");
            var work = new Work(user.Works.Count, inst, user);
            user.CurrentWorks.Add(work);
        }

        user.State = State.setHashtagType;
        await client.SendTextMessageAsync(query.From.Id, "Выберите тип публикаций.",
            replyMarkup: Keyboards.SelectHashtagMode);
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data == "selectAll" && user.State == State.selectAccounts;
    }
}