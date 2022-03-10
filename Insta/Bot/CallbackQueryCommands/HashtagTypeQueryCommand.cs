using System;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Insta.Model.User;

namespace Insta.Bot.CallbackQueryCommands
{
    public class HashtagTypeQueryCommand : ICallbackQueryCommand
    {
        public async Task Execute(TelegramBotClient client, User user, CallbackQuery query)
        {
            var type = (HashtagType) Enum.Parse(typeof(HashtagType), query.Data[6..]);
            user.CurrentWorks.ForEach(x => x.SetHashtagType(type));


            user.State = State.setMode;
            await client.EditMessageTextAsync(query.From.Id, query.Message.MessageId,
                "Введите тип работы.", replyMarkup: Keyboards.SelectMode);
        }

        public bool Compare(CallbackQuery query, User user)
        {
            return query.Data.StartsWith("htype") && user.State == State.setHashtagType;
        }
    }
}