using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands;

public class ChallengeEmailQueryCommand : ICallbackQueryCommand
{
    public async Task Execute(ITelegramBotClient client, User user, CallbackQuery query)
    {
        try
        {
            await client.DeleteMessageAsync(query.From.Id, query.Message.MessageId);
        }
        catch
        {
            //ignored
        }

        var response = await user.EnterData.Api.RequestVerifyCodeToEmailForChallengeRequireAsync();
        if (!response.Succeeded)
        {
            await client.SendTextMessageAsync(query.From.Id,
                "Ошибка. Попробуйте войти снова.");
            user.State = State.main;
            return;
        }

        user.State = State.challengeRequiredAccept;
        await client.SendTextMessageAsync(query.From.Id,
            "Код отправлен. Введите код из сообщения.", replyMarkup: Keyboards.Main);
    }

    public bool Compare(CallbackQuery query, User user)
    {
        return query.Data == "challengeEmail" && user.State == State.challengeRequired;
    }
}