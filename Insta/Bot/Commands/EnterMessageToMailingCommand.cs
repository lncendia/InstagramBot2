using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Insta.Enums;
using Insta.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = Insta.Model.User;

namespace Insta.Bot.Commands;

public class EnterMessageToMailingCommand : ITextCommand
{
    public async Task Execute(ITelegramBotClient client, User user, Message message)
    {
        var users = BotSettings.Users;
        switch (message.Type)
        {
            case MessageType.Text:
                foreach (var member in users.ToList())
                {
                    try
                    {
                        await client.SendTextMessageAsync(member.Id, message.Text!);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                break;
            case MessageType.Photo:
                foreach (var member in users.ToList())
                {
                    try
                    {
                        await client.SendPhotoAsync(member.Id, new InputFileId(message.Photo!.Last().FileId),
                            caption: message.Caption);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                break;
            case MessageType.Audio:
                foreach (var member in users.ToList())
                {
                    try
                    {
                        await client.SendAudioAsync(member.Id, new InputFileId(message.Audio!.FileId));
                    }
                    catch
                    {
                        // ignored
                    }
                }

                break;
            case MessageType.Video:
                foreach (var member in users.ToList())
                {
                    try
                    {
                        await client.SendVideoAsync(member.Id, new InputFileId(message.Video!.FileId),
                            caption: message.Caption);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                break;
            case MessageType.Voice:
                foreach (var member in users.ToList())
                {
                    try
                    {
                        await client.SendVoiceAsync(member.Id, new InputFileId(message.Voice!.FileId));
                    }
                    catch
                    {
                        // ignored
                    }
                }

                break;
            case MessageType.Document:
                foreach (var member in users.ToList())
                {
                    try
                    {
                        await client.SendDocumentAsync(member.Id,
                            new InputFileId(message.Document!.FileId));
                    }
                    catch
                    {
                        // ignored
                    }
                }

                break;
            case MessageType.Sticker:
                foreach (var member in users.ToList())
                {
                    try
                    {
                        await client.SendPhotoAsync(member.Id, new InputFileId(message.Sticker!.FileId));
                    }
                    catch
                    {
                        // ignored
                    }
                }

                break;
        }

        await client.SendTextMessageAsync(user.Id,
            "Сообщение было успешно отправлено. Вы в главном меню.",
            replyMarkup: Keyboards.MainKeyboard);
        user.State = State.main;
    }

    public bool Compare(Message message, User user)
    {
        return user.State == State.mailingAdmin;
    }
}