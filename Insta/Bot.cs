using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InstagramApiSharp.Classes;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InputFiles;

namespace Insta
{
    class Bot
    {
        private static readonly TelegramBotClient Tgbot =
            new TelegramBotClient("1682222171:AAGw4CBCJ875NRn1rFnh0sBncYkev5KIa4o");

        public static List<User> Users;

        private static readonly ReplyKeyboardMarkup Keyboard = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>
        {
            new List<KeyboardButton>() {new KeyboardButton("🌇 Мои аккаунты"), new KeyboardButton("❤ Отработка") },
            new List<KeyboardButton>() {new KeyboardButton("💰 Оплатить подписку") },
            new List<KeyboardButton>() {new KeyboardButton("📄 Инструкция"),new KeyboardButton("🤝 Поддержка") }
        });

        private static readonly string[] Emodji = { "🏞","🏔","🏖","🌋","🏜","🏕","🌎","🗽","🌃", "☘", "🐲", "🌸", "🌓", "🍃", "☀", "☁" };
        private static Random rnd = new Random();
        public static void Start()
        {
            using DB db = new DB();
            Users = db.Users.Include(i => i.Instagrams).Include(i=>i.Subscribes).ToList();
            db.Dispose();
            Operation.SubscribeToEvent(Users);
            Tgbot.OnMessage += Tgbot_OnMessage;
            Tgbot.OnCallbackQuery += Tgbot_OnCallbackQuery;
            Tgbot.StartReceiving();
        }

        private static readonly InlineKeyboardMarkup KeyBack =
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🔙 Назад", "back"));
        private static readonly InlineKeyboardMarkup KeyMain =
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu"));

        private static readonly InlineKeyboardMarkup Keys = new InlineKeyboardMarkup(
            new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("🏃 Начать отработку", "startWorking")},
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("⚙ Активные отработки", "stopWorking")}
            });
        private static readonly InlineKeyboardMarkup StartWork = new InlineKeyboardMarkup(
            new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("🏃 Начать сейчас", "startNow"),InlineKeyboardButton.WithCallbackData("⌛ Начать позже", "startLater")},
                new List<InlineKeyboardButton>
                    {InlineKeyboardButton.WithCallbackData("🛑 Отмена", "mainMenu")}
            });

        private static async void Tgbot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            try
            {
                var cb = e.CallbackQuery.Data;
                var user = Users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                if (user == null) return;

                if (cb.StartsWith("bill"))
                {
                    if (Payment.CheckPay(user, cb[5..]))
                    {
                        string message = e.CallbackQuery.Message.Text;
                        message = message.Replace("❌ Статус: Не оплачено", "✔ Статус: Оплачено");
                        message=message.Remove(message.IndexOf("Оплачено", StringComparison.Ordinal)+8);
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            message);
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Успешно оплачено.");
                    }

                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Не оплачено.");
                    return;
                }

                if (cb.StartsWith("exit"))
                {
                    if (user.state != User.State.main) return;
                    if (!await Operation.LogOut(user, int.Parse(cb[5..])))
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Инстаграм не найден.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    }
                    else
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Успешно.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    }
                    return;
                }
                if (cb.StartsWith("select"))
                {
                    if (user.state != User.State.main) return;
                    user.state = User.State.block;
                    Instagram inst = user.Instagrams.Find(x => x.Id == int.Parse(cb[7..]));
                    if (inst == null)
                    {
                        user.state = User.State.main;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Инстаграм не найден.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        return;
                    }

                    if (inst.IsDeactivated)
                    {
                        user.state = User.State.main;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Ошибка.");
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, 
                            "Аккаунт деактивирован. Купите подписку, чтобы активировать аккаунт.");
                        return;
                    }
                    if (inst.api == null || !inst.api.IsUserAuthenticated)
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                            "Нvеобходима аутентификация. Пожалуйста, подождите...");
                        user.enterData = inst;
                        var login = await Operation.CheckLoginAsync(user.enterData);
                        if (login?.Value != InstaLoginResult.Success)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, 
                                "Не удалось войти. Инстаграм будет удален, попробуйте войти в него заново.");
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                            user.enterData = null;
                            await using DB db = new DB();
                            db.UpdateRange(user, inst);
                            user.Instagrams.Remove(inst);
                            db.Remove(inst);
                            await db.SaveChangesAsync();
                            user.state = User.State.main;
                            return;
                        }
                    }
                    var work = new Work(user.Works.Count, inst.api, user);
                    user.Works.Add(work);
                    user.CurrentWork = work;
                    user.state = User.State.selectHashtag;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Введите хештег без #.", replyMarkup: KeyMain);
                        return;
                }

                if (cb.StartsWith("cancel"))
                {
                    if (user.state != User.State.main) return;
                    user.state = User.State.block;
                    Work work = user.Works.Find(x => x.Id == int.Parse(cb[7..]));
                    if (work == null)
                    {
                        user.state = User.State.main;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Отработка не найдена.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        return;
                    }

                    if (work.IsStarted)
                    {
                        work.CancelTokenSource.Cancel();
                    }
                    else
                    {
                        work.TimerDispose();
                    }

                    user.Works.Remove(work);
                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Отработка отменена.");
                    await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                        e.CallbackQuery.Message.MessageId);
                    user.state = User.State.main;
                    return;
                }

                switch (cb)
                {
                    case "startWorking":
                        if(user.state!=User.State.main) return;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Выберите аккаунт.");
                        foreach (var x in user.Instagrams)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                $"{Emodji[rnd.Next(0, Emodji.Length)]} Аккаунт {x.Username}",
                                replyMarkup: new InlineKeyboardMarkup(
                                    InlineKeyboardButton.WithCallbackData("Выбрать", $"select_{x.Id}")));
                        }
                        break;
                    case "stopWorking":
                        if (user.state != User.State.main) return;
                        foreach (var x in user.Works.ToList())
                        {
                            var str = x.IsStarted ? "Уже началась" : "Еще не началась";
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                $"Аккаунт {x.GetUsername()}. Хештег {x.Hashtag}. {str}.",
                                replyMarkup: new InlineKeyboardMarkup(
                                    InlineKeyboardButton.WithCallbackData("🛑 Отменить", $"cancel_{x.Id}")));
                        }
                        break;
                    case "startNow":
                        if (user.state != User.State.setTimeWork) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        user.CurrentWork?.Start();
                        user.CurrentWork = null;
                        user.state = User.State.main;
                        break;
                    case "startLater":
                        if (user.state != User.State.setTimeWork) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        user.state = User.State.setDate;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Введите время запуска по МСК в формате ЧЧ:мм. (Пример: 13:30).",replyMarkup: KeyBack);
                        break;
                    case "enterData":
                        if(user.state!=User.State.main) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        if (user.Instagrams.Count >=user.Subscribes.Count)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Увы... Так не работает.", replyMarkup: KeyMain);
                            return;
                        }
                        user.enterData = new Instagram(){User=user};
                        user.state = User.State.login;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Введите логин", replyMarkup: KeyMain);
                        break;
                    case "acceptEntry":
                        if(user.state!=User.State.challengeRequired) return;
                        var login = await Operation.CheckLoginAsync(user.enterData);
                        if (await Login(user, login))
                        {
                            await using DB db = new DB();
                            db.Update(user);
                            db.Add(user.enterData);
                            user.enterData = null;
                            await db.SaveChangesAsync();
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                e.CallbackQuery.Message.MessageId);
                            user.state = User.State.main;
                        }
                        break;
                    case "challengeEmail":
                        if (user.state != User.State.challengeRequired) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        var response = await user.enterData.api.RequestVerifyCodeToEmailForChallengeRequireAsync();
                        if (!response.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Ошибка. Попробуйте войти снова.");
                            user.state = User.State.main;
                            return;
                        }
                        user.state = User.State.challengeRequiredAccept;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Код отправлен. Введите код из сообщения.", replyMarkup: KeyMain);
                        break;
                    case "challengePhone":
                        if (user.state != User.State.challengeRequired) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        var response2 = await user.enterData.api.RequestVerifyCodeToSMSForChallengeRequireAsync();
                        if (!response2.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Ошибка. Попробуйте войти снова.");
                            user.state = User.State.main;
                        }
                        user.state = User.State.challengeRequiredAccept;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Код отправлен. Введите код из сообщения.",replyMarkup:KeyMain);
                        break;
                    case "back":
                        switch (user.state)
                        {
                            case User.State.password:
                                user.state = User.State.login;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Введите логин", replyMarkup: KeyMain);
                                break;
                            case User.State.twofactor:
                                user.state = User.State.password;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Теперь введите пароль", replyMarkup: KeyBack);
                                break;
                            case User.State.setDuration:
                                user.state = User.State.selectHashtag;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Введите хештег. Решётку писать не нужно.", replyMarkup: KeyMain);
                                break;
                            case User.State.setDate:
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Выбирете, когда хотите начать.", replyMarkup: StartWork);
                                user.state = User.State.setTimeWork;
                                break;

                        }
                        break;
                    case "mainMenu":
                        if(user.state == User.State.block) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        user.Works.Remove(user.CurrentWork);
                        user.CurrentWork = null;
                        user.enterData = null;
                        user.state = User.State.main;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Вы в главном меню.", replyMarkup: Keyboard);
                        break;

                }
            }
            catch
            {
                var user = Users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                Error(user);
            }
        }

        private static async void Tgbot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                var message = e.Message;
                if (message.Type != MessageType.Text && message.Type != MessageType.Photo) return;
                var user = Users.FirstOrDefault(x => x.Id == message.From.Id);
                if (user == null)
                {
                    await using DB db = new DB();
                    user = new User {Id = e.Message.From.Id, state = User.State.main};
                    Users.Add(user);
                    db.Add(user);
                    await db.SaveChangesAsync();
                    await Tgbot.SendStickerAsync(message.From.Id,
                        new InputOnlineFile("CAACAgIAAxkBAAK_HGAQINBHw7QKWWRV4LsEU4nNBxQ3AAKZAAPZvGoabgceWN53_gIeBA"),
                        replyMarkup: Keyboard);
                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                        "Добро пожаловать.\nДля дальнейшей работы тебе необходимо оплатить подписку и ввести данные своего instagram.");
                    return;
                }

                switch (message.Text)
                {
                    case "/start":
                        if (user.state != User.State.main) break;
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Вы в главном меню.", replyMarkup: Keyboard);
                        break;
                    case "🌇 Мои аккаунты":
                        if (user.state != User.State.main) break;
                        foreach (var x in user.Instagrams)
                        {
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                $"{Emodji[rnd.Next(0,Emodji.Length)]} Аккаунт {x.Username}",
                                replyMarkup: new InlineKeyboardMarkup(
                                    InlineKeyboardButton.WithCallbackData("🚪 Выйти", $"exit_{x.Id}")));
                        }
                        if(user.Instagrams.Count<user.Subscribes.Count)
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Вы можете добавить аккаунт",
                                replyMarkup: new InlineKeyboardMarkup(
                                    InlineKeyboardButton.WithCallbackData("➕ Добавить аккаунт", "enterData")));
                        else
                        {
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Оплатите подписку, чтобы добавить аккаунт.", replyMarkup: Keyboard);
                        }
                        break;
                    case "❤ Отработка":
                        if (user.state != User.State.main) return;
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Выберите, что вы хотите сделать.", replyMarkup: Keys);
                        break;
                    case "📄 Инструкция":
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Всю инструкцию вы можете прочитать в канале @likebotgid.");
                        break;
                    case "🤝 Поддержка":
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "За поддержкой вы можете обратиться к @Per4at.");
                        break;
                    case "💰 Оплатить подписку":
                        if (user.state != User.State.main) return;
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Введите количество аккаунтов, которые хотите добавить. Цена одного аккаунта - 120 рублей/30 дней.",
                            replyMarkup: KeyMain);
                        user.state = User.State.entetCountToBuy;
                        break;
                    default:
                        switch (user.state)
                        {
                            case User.State.login:
                                if (user.Instagrams.Find(instagram => instagram.Username == message.Text) != null)
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Вы уже добавили этот аккаунт.");
                                    user.state = User.State.main;
                                    break;
                                }

                                user.enterData.Username = message.Text;
                                user.state = User.State.password;
                                await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                    "Теперь введите пароль.", replyMarkup: KeyBack);
                                break;
                            case User.State.password:
                                if (e.Message.Text.Length < 6)
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Пароль не может быть меньше 6 символов!");
                                    return;
                                }

                                user.enterData.Password = message.Text;
                                var login = await Operation.CheckLoginAsync(user.enterData);
                                if (await Login(user, login))
                                {
                                    await using DB db = new DB();
                                    db.Update(user);
                                    db.Add(user.enterData);
                                    user.enterData = null;
                                    await db.SaveChangesAsync();
                                    user.state = User.State.main;
                                }

                                break;
                            case User.State.twofactor:
                                var x = await Operation.SendCodeTwoFactorAsync(user.enterData.api, message.Text);
                                if (x == null || !x.Succeeded)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Ошибка. Попробуйте войти ещё раз.");
                                    user.state = User.State.main;
                                    return;
                                }

                                switch (x.Value)
                                {
                                    case InstaLoginTwoFactorResult.Success:
                                    {
                                        await using DB db = new DB();
                                        db.Update(user);
                                        db.Add(user.enterData);
                                        user.enterData = null;
                                        await db.SaveChangesAsync();
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Инстаграм успешно добавлен.");
                                        user.state = User.State.main;
                                        break;
                                    }
                                    case InstaLoginTwoFactorResult.InvalidCode:
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Неверный код, попробуйте еще раз.", replyMarkup: KeyMain);
                                        break;
                                    default:
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Ошибка. Попробуйте войти ещё раз.");
                                        user.state = User.State.main;
                                        break;
                                }

                                break;
                            case User.State.challengeRequiredAccept:
                                var y = await Operation.SendCodeChallengeRequiredAsync(user.enterData.api,
                                    message.Text);
                                if (await Login(user, y))
                                {
                                    await using DB db = new DB();
                                    db.Update(user);
                                    db.Add(user.enterData);
                                    user.enterData = null;
                                    await db.SaveChangesAsync();
                                }
                                break;
                            case User.State.challengeRequiredPhoneCall:
                                bool isRight = await Operation.SubmitPhoneChallengeRequiredAsync(user.enterData.api,
                                    message.Text);
                                if (isRight)
                                {
                                    user.state = User.State.challengeRequiredAccept;
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Код отправлен. Введите код из сообщения.", replyMarkup: KeyMain);
                                }
                                else
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Ошибка. Неверный номер.", replyMarkup: KeyMain);
                                }
                                break;
                            case User.State.selectHashtag:
                                user.CurrentWork?.SetHashtag(message.Text);
                                user.state = User.State.setDuration;
                                await Tgbot.SendTextMessageAsync(message.From.Id,
                                    "Введите интервал (в секундах).", replyMarkup: KeyBack);
                                break;
                            case User.State.setDuration:
                                int duration;
                                if (!int.TryParse(message.Text, out duration))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Введите число!");
                                    return;
                                }

                                if (duration > 300)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Интервал не может быть больше 5 минут!");
                                    return;
                                }

                                user.CurrentWork?.SetDuration(duration * 1000);
                                await Tgbot.SendTextMessageAsync(message.From.Id,
                                    "Выбирете, когда хотите начать.", replyMarkup: StartWork);
                                user.state = User.State.setTimeWork;
                                break;
                            case User.State.setDate:
                                if (TimeSpan.TryParse(message.Text, out var timeSpan))
                                {
                                    var timeEnter = DateTime.Today.Add(timeSpan);
                                    if (timeEnter.CompareTo(DateTime.Now) <= 0)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Введите корректное время.");
                                        return;
                                    }

                                    user.CurrentWork?.StartAtTime(timeEnter.Subtract(DateTime.Now));
                                    if (user.CurrentWork != null)
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            $"Отработка на аккаунте {user.CurrentWork.GetUsername()} по хештегу #{user.CurrentWork.Hashtag} начнётся в {timeEnter:HH:mm:ss}.",
                                            replyMarkup: new InlineKeyboardMarkup(
                                                InlineKeyboardButton.WithCallbackData("🛑 Отмена",
                                                    $"cancel_{user.CurrentWork.Id}")));
                                    user.CurrentWork = null;
                                    user.state = User.State.main;
                                }
                                else
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Неверный формат времени.");
                                }
                                break;
                            case User.State.entetCountToBuy:
                                int count;
                                if (!int.TryParse(message.Text, out count))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Введите число!");
                                    return;
                                }

                                if (count > 100)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Слишком большое количество!");
                                    return;
                                }

                                user.state = User.State.main;
                                string billId = "";
                                var payUrl = Payment.AddTransaction(count * 120, user, ref billId);
                                if (payUrl == null)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Произошла ошибка при создании счета. Попробуйте еще раз.");
                                    return;
                                }

                                await Tgbot.SendTextMessageAsync(message.From.Id,
                                    $"💸 Оплата подписки на сумму {count * 120} р.\n📆 Дата: {DateTime.Now:dd.MMM.yyyy}\n❌ Статус: Не оплачено.\n\n💳 Оплатите счет по ссылке.\n{payUrl}",
                                    replyMarkup: new InlineKeyboardMarkup(
                                        new List<List<InlineKeyboardButton>>()
                                        {
                                            new List<InlineKeyboardButton>()
                                            {
                                                InlineKeyboardButton.WithCallbackData("Проверить оплату",
                                                    $"bill_{billId}")
                                            },
                                        }));
                                break;
                        }
                        break;
                }
            }
            catch
            {
                var user = Users.FirstOrDefault(x => x.Id == e.Message.From.Id);
                Error(user);
            }
        }

        private static async Task<bool> Login(User user, IResult<InstaLoginResult> login)
        {
            try
            {
                if (login == null)
                {
                    await Tgbot.SendTextMessageAsync(user.Id,
                        "Ошибка. Данные введены неверно.");
                    user.state = User.State.main;
                    return false;
                }
                switch (login.Value)
                {
                    case InstaLoginResult.Success:
                    {
                        if (!login.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Ошибка. Данные введены неверно.");
                            user.state = User.State.main;
                        }
                        await Tgbot.SendTextMessageAsync(user.Id, "Инстаграм успешно добавлен.");
                        return true;
                    }
                    case InstaLoginResult.BadPassword:
                        await Tgbot.SendTextMessageAsync(user.Id, "Неверный пароль. Попробуйте ввести снова.",
                            replyMarkup: KeyBack);
                        break;
                    case InstaLoginResult.InvalidUser:
                        user.state = User.State.main;
                        await Tgbot.SendTextMessageAsync(user.Id, "Пользователь не найден. Попробуйте еще раз.",
                            replyMarkup: new InlineKeyboardMarkup(
                                InlineKeyboardButton.WithCallbackData("🖊 Ввести данные", "enterData")));
                        break;
                    case InstaLoginResult.LimitError:
                        user.state = User.State.main;
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Слишком много запросов. Подождите несколько минут и попробуйте снова.",
                            replyMarkup: new InlineKeyboardMarkup(
                                InlineKeyboardButton.WithCallbackData("🖊 Ввести данные", "enterData")));
                        break;
                    case InstaLoginResult.TwoFactorRequired:
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Необходима двухфакторная аутентификация. Введите код из сообщения.", replyMarkup: KeyMain);
                        user.state = User.State.twofactor;
                        break;
                    case InstaLoginResult.ChallengeRequired:
                    {
                        var challenge = await user.enterData.api.GetChallengeRequireVerifyMethodAsync();
                        if (!challenge.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Ошибка. Попробуйте войти ещё раз.");
                            user.state = User.State.main;
                            return false;
                        }
                        if (challenge.Value.SubmitPhoneRequired)
                        {
                            user.state = User.State.challengeRequiredPhoneCall;
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Инстаграм просит подтверждение. Введите подключенный к аккаунту номер.", replyMarkup: KeyMain);
                            break;
                        }

                        InlineKeyboardMarkup key;
                        if (string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
                        {
                            key = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>()
                            {
                                new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData($"✉ Эл. адресс ({challenge.Value.StepData.Email})", "challengeEmail")},
                                new List<InlineKeyboardButton>() {InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry")},
                                new List<InlineKeyboardButton>{InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
                            });
                        }
                        else if (string.IsNullOrEmpty(challenge.Value.StepData.Email))
                        {
                            key = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
                            {
                                new List<InlineKeyboardButton>(){InlineKeyboardButton.WithCallbackData($"📲 Телефон ({challenge.Value.StepData.PhoneNumber})", "challengePhone")},
                                new List<InlineKeyboardButton>() {InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry")},
                                new List<InlineKeyboardButton>{InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
                            });
                        }
                        else
                        {
                            key = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>()
                            {
                                new List<InlineKeyboardButton>() {InlineKeyboardButton.WithCallbackData($"📲 Телефон ({challenge.Value.StepData.PhoneNumber})", "challengePhone"),},
                                new List<InlineKeyboardButton>(){InlineKeyboardButton.WithCallbackData($"✉ Эл. адресс ({challenge.Value.StepData.Email})",  "challengeEmail")},
                                new List<InlineKeyboardButton>() {InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry")},
                                new List<InlineKeyboardButton>{InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
                            });
                        }
                        user.state = User.State.challengeRequired;
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Инстаграм просит подтверждение. Выбирете, каким образом вы хотите подтвердить код:",
                            replyMarkup: key);
                    }
                        break;
                }

                return false;
            }
            catch
            {
                Error(user);
                return false;
            }
        }

        private static async void Error(User user)
        {
            if (user == null) return;
            user.Works.Remove(user.CurrentWork);
            user.CurrentWork = null;
            user.enterData = null;
            user.state = User.State.main;
            try
            {
                await Tgbot.SendTextMessageAsync(user.Id,
                    "Произошла ошибка.", replyMarkup: Keyboard);
            }
            catch
            {
                // ignored
            }
        }
    }
}
