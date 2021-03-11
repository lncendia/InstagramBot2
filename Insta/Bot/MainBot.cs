using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Insta.Entities;
using Insta.Enums;
using Insta.Working;
using InstagramApiSharp.Classes;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Insta.Payments;
using Task = System.Threading.Tasks.Task;

namespace Insta.Bot
{
    internal static class MainBot
    {
        public static readonly TelegramBotClient Tgbot = BotSettings.Get();

        public static List<User> Users;
        public static readonly Random Rnd = new();

        public static async Task Start()
        {
            await using Db db = new Db();
            Users = db.Users.Include(i => i.Instagrams).Include(i => i.Subscribes).ToList();
            Operation.CheckSubscribeAsync(Users);
            Operation.LoadProxy(db.Proxies.ToList());
            await Operation.LoadUsersStateDataAsync(db.Instagrams.Include(i => i.User).ToList());
            await Operation.LoadWorksAsync(db.Works.Include(_ => _.Instagram).ToList());
            Tgbot.OnMessage += Tgbot_OnMessage;
            Tgbot.OnMessage += Admin.Admin_OnMessage;
            Tgbot.OnCallbackQuery += Tgbot_OnCallbackQuery;
            Tgbot.StartReceiving();
        }

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
                        message = message.Remove(message.IndexOf("Оплачено", StringComparison.Ordinal) + 8);
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            message);
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Успешно оплачено.");
                    }

                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Не оплачено.");
                    return;
                }

                if (cb.StartsWith("exit"))
                {
                    var instagram = user.Instagrams.FirstOrDefault(_ => _.Id == int.Parse(cb[5..]));
                    if (instagram == null)
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Инстаграм не найден.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    }
                    else
                    {
                        string message = (await Operation.LogOutAsync(user, instagram)) ? "Успешно." : "Ошибка.";
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, message);
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    }

                    return;
                }

                if (cb.StartsWith("changeProxy"))
                {
                    var instagram = user.Instagrams.FirstOrDefault(_ => _.Id == int.Parse(cb[12..]));
                    if (instagram == null)
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Инстаграм не найден.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    }
                    else
                    {
                        string message = (Operation.ChangeProxy(instagram)) ? "Успешно." : "Ошибка.";
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, message);
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    }

                    return;
                }

                IResult<InstaLoginResult> login;
                if (cb.StartsWith("reLogIn"))
                {
                    if (user.State != State.main)
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Вы должны быть в главное меню.");
                        return;
                    }

                    user.State = State.block;
                    Instagram inst = user.Instagrams.FirstOrDefault(_ => _.Id == int.Parse(cb[8..]));
                    if (inst == null)
                    {
                        user.State = State.main;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Инстаграм не найден.");
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        return;
                    }

                    if (inst.IsDeactivated)
                    {
                        user.State = State.main;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Инстаграм деактивирован.");
                        return;
                    }

                    if (!await Operation.LogOutAsync(user, inst))
                    {
                        {
                            user.State = State.main;
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Ошибка. Возможно на аккаунте запущены отложенные отработки.", showAlert: true);
                            return;
                        }
                    }

                    var currentUser = inst.Api.GetLoggedUser();
                    user.EnterData = new Instagram
                        {User = user, Username = currentUser.UserName, Password = currentUser.Password};
                    login = await Operation.CheckLoginAsync(user.EnterData);
                    if (await Login(user, login))
                    {
                        await using Db db = new Db();
                        db.Update(user);
                        db.Add(user.EnterData);
                        user.EnterData = null;
                        user.State = State.main;
                        await db.SaveChangesAsync();
                    }
                }

                if (cb.StartsWith("select_"))
                {
                    if (user.State != State.selectAccounts) return;
                    user.State = State.block;
                    Instagram inst = user.Instagrams.Find(x => x.Id == int.Parse(cb[7..]));
                    if (inst == null)
                    {
                        user.State = State.selectAccounts;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Инстаграм не найден.");
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            e.CallbackQuery.Message.Text,
                            replyMarkup: Keyboards.NewSelect(
                                e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList(),
                                e.CallbackQuery));
                        return;
                    }

                    if (inst.IsDeactivated)
                    {
                        user.State = State.selectAccounts;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                            $"Аккаунт {inst.Username} деактивирован. Купите подписку, чтобы активировать аккаунт.");
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            e.CallbackQuery.Message.Text,
                            replyMarkup: Keyboards.NewSelect(
                                e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList(),
                                e.CallbackQuery));
                        return;
                    }

                    if (user.CurrentWorks.FirstOrDefault(x => x.Instagram.Username == inst.Username) != null)
                    {
                        user.State = State.selectAccounts;
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                            $"Аккаунт {inst.Username} уже добавлен.");
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            e.CallbackQuery.Message.Text,
                            replyMarkup: Keyboards.NewSelect(
                                e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList(),
                                e.CallbackQuery));
                        return;
                    }

                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                        $"Инстаграм {inst.Username} успешно добавлен.");
                    await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                        e.CallbackQuery.Message.Text,
                        replyMarkup: Keyboards.NewSelect(e.CallbackQuery.Message.ReplyMarkup.InlineKeyboard.ToList(),
                            e.CallbackQuery));
                    var work = new Work(user.Works.Count, inst, user);
                    user.CurrentWorks.Add(work);
                    user.State = State.selectAccounts;
                    return;
                }

                if (cb.StartsWith("cancel"))
                {
                    if (user.State != State.main)
                    {
                        await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Вы должны быть в главное меню.");
                        return;
                    }

                    user.State = State.block;
                    Work work = user.Works.Find(x => x.Id == int.Parse(cb[7..]));
                    if (work == null)
                    {
                        user.State = State.main;
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
                        await work.TimerDisposeAsync();
                    }

                    user.Works.Remove(work);
                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Отработка отменена.");
                    await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                        e.CallbackQuery.Message.MessageId);
                    user.State = State.main;
                    return;
                }

                switch (cb)
                {
                    case "selectAll":
                        if (user.State != State.selectAccounts) return;
                        try
                        {
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
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
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    $"Аккаунт {inst.Username} деактивирован. Купите подписку, чтобы активировать аккаунт.");
                                continue;
                            }

                            if (user.CurrentWorks.FirstOrDefault(x => x.Instagram.Username == inst.Username) != null)
                            {
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    $"Аккаунт {inst.Username} уже добавлен.");
                                continue;
                            }

                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                $"Инстаграм {inst.Username} добавлен.");
                            var work = new Work(user.Works.Count, inst, user);
                            user.CurrentWorks.Add(work);
                        }

                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Выберите режим.", replyMarkup: Keyboards.SelectMode);
                        user.State = State.selectMode;
                        break;
                    case "startLike":
                        if (user.State != State.selectMode) return;
                        user.CurrentWorks.ForEach(x => x.SetMode(Mode.like));
                        user.State = State.selectHashtag;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            "Введите хештег без #.", replyMarkup: Keyboards.Main);
                        break;
                    case "startSave":
                        if (user.State != State.selectMode) return;
                        user.CurrentWorks.ForEach(x => x.SetMode(Mode.save));
                        user.State = State.selectHashtag;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            "Введите хештег без #.", replyMarkup: Keyboards.Main);
                        break;
                    case "startFollowing":
                        if (user.State != State.selectMode) return;
                        user.CurrentWorks.ForEach(x => x.SetMode(Mode.follow));
                        user.State = State.selectHashtag;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            "Введите хештег без #.", replyMarkup: Keyboards.Main);
                        break;
                    case "startAll":
                        if (user.State != State.selectMode) return;
                        user.CurrentWorks.ForEach(x => x.SetMode(Mode.likeAndSave));
                        user.State = State.selectHashtag;
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId,
                            "Введите хештег без #.", replyMarkup: Keyboards.Main);
                        break;
                    case "selectMode":
                        if (user.State != State.selectAccounts) return;
                        if (user.CurrentWorks.Count == 0)
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Вы не выбрали ни одного аккаунта.");
                            return;
                        }

                        user.State = State.selectMode;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Выберите режим.", replyMarkup: Keyboards.SelectMode);
                        break;
                    case "startWorking":
                        if (user.State != State.main)
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Вы должны быть в главное меню.");
                            return;
                        }

                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        if (user.Instagrams.Count == 0)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "У вас нет аккаунтов.");
                            break;
                        }

                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Нажмите на нужные аккаунты.", replyMarkup: Keyboards.Select(user));
                        user.State = State.selectAccounts;
                        break;
                    case "stopWorking":
                        if (user.State != State.main)
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Вы должны быть в главное меню.");
                            return;
                        }

                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        if (user.Works.Count == 0)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "У вас нет активных отработок.");
                            break;
                        }

                        foreach (var x in user.Works.ToList())
                        {
                            var str = x.IsStarted ? "Уже началась" : "Еще не началась";
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                $"Аккаунт {x.Instagram.Username}. Хештег #{x.Hashtag}. {str}. {x.GetInformation()}",
                                replyMarkup: Keyboards.Cancel(x.Id));
                        }

                        break;
                    case "startNow":
                        if (user.State != State.setTimeWork) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        user.CurrentWorks.ForEach(async x => await x.StartAsync());
                        user.CurrentWorks.Clear();
                        user.State = State.main;
                        break;
                    case "startLater":
                        if (user.State != State.setTimeWork) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        user.State = State.setDate;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Введите время запуска по МСК в формате ЧЧ:мм. (<strong>Пример:</strong> <em>13:30</em>).",
                            replyMarkup: Keyboards.Back, parseMode: ParseMode.Html);
                        break;
                    case "enterData":
                        if (user.State != State.main)
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Вы должны быть в главное меню.");
                            return;
                        }

                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        if (user.Instagrams.Count >= user.Subscribes.Count)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Увы... Так не работает.");
                            return;
                        }

                        user.EnterData = new Instagram() {User = user};
                        user.State = State.login;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Введите логин", replyMarkup: Keyboards.Main);
                        break;
                    case "acceptEntry":
                        if (user.State != State.challengeRequired) return;
                        login = await Operation.CheckLoginAsync(user.EnterData, true);
                        if (login?.Value == InstaLoginResult.Success)
                        {
                            if (user.EnterData != null)
                            {
                                user.EnterData.StateData = await user.EnterData.Api.GetStateDataAsStringAsync();
                                await using Db db = new Db();
                                db.Update(user);
                                db.Add(user.EnterData);
                                user.EnterData = null;
                                await db.SaveChangesAsync();
                                await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                    "Успешно.");
                            }
                            else
                            {
                                await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                    "Ошибка.");
                            }

                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                e.CallbackQuery.Message.MessageId);
                            user.State = State.main;
                        }
                        else
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id,
                                "Произошла ошибка, попробуйте еще раз.");
                        }

                        break;
                    case "challengeEmail":
                        if (user.State != State.challengeRequired) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        var response = await user.EnterData.Api.RequestVerifyCodeToEmailForChallengeRequireAsync();
                        if (!response.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Ошибка. Попробуйте войти снова.");
                            user.State = State.main;
                            return;
                        }

                        user.State = State.challengeRequiredAccept;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Код отправлен. Введите код из сообщения.", replyMarkup: Keyboards.Main);
                        break;
                    case "challengePhone":
                        if (user.State != State.challengeRequired) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        var response2 = await user.EnterData.Api.RequestVerifyCodeToSMSForChallengeRequireAsync();
                        if (!response2.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                "Ошибка. Попробуйте войти снова.");
                            user.State = State.main;
                        }

                        user.State = State.challengeRequiredAccept;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Код отправлен. Введите код из сообщения.", replyMarkup: Keyboards.Main);
                        break;
                    case "back":
                        switch (user.State)
                        {
                            case State.password:
                                user.State = State.login;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Введите логин", replyMarkup: Keyboards.Main);
                                break;
                            case State.twoFactor:
                                user.State = State.password;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Теперь введите пароль", replyMarkup: Keyboards.Back);
                                break;
                            case State.selectHashtag:
                                user.State = State.selectMode;
                                await Tgbot.EditMessageTextAsync(e.CallbackQuery.InlineMessageId,
                                    "Выберите режим.", replyMarkup: Keyboards.SelectMode);
                                break;
                            case State.setDuration:
                                user.State = State.selectHashtag;
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Введите хештег. Решётку писать не нужно.", replyMarkup: Keyboards.Back);
                                break;
                            case State.setDate:
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                                    "Выбирете, когда хотите начать.", replyMarkup: Keyboards.StartWork);
                                await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                                    e.CallbackQuery.Message.MessageId);
                                user.State = State.setTimeWork;
                                break;

                        }

                        break;
                    case "mainMenu":
                        if (user.State == State.block) return;
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id,
                            e.CallbackQuery.Message.MessageId);
                        foreach (var work in user.CurrentWorks)
                        {
                            user.Works.Remove(work);
                        }

                        user.CurrentWorks.Clear();
                        user.EnterData = null;
                        user.State = State.main;
                        await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id,
                            "Вы в главном меню.", replyMarkup: Keyboards.MainKeyboard);
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
                if (message.Type != MessageType.Text) return;
                var user = Users.FirstOrDefault(x => x.Id == message.From.Id);
                if (user == null)
                {
                    await using Db db = new Db();
                    user = new User {Id = e.Message.From.Id, State = State.main};
                    Users.Add(user);
                    db.Add(user);
                    await db.SaveChangesAsync();
                    await Tgbot.SendStickerAsync(message.From.Id,
                        new InputOnlineFile("CAACAgIAAxkBAAK_HGAQINBHw7QKWWRV4LsEU4nNBxQ3AAKZAAPZvGoabgceWN53_gIeBA"),
                        replyMarkup: Keyboards.MainKeyboard);
                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                        "Добро пожаловать.\nДля дальнейшей работы тебе необходимо оплатить подписку и ввести данные своего instagram.");
                    return;
                }

                switch (message.Text)
                {
                    case "/start":
                        if (user.State == State.block) return;
                        foreach (var work in user.CurrentWorks)
                        {
                            user.Works.Remove(work);
                        }

                        user.CurrentWorks.Clear();
                        user.EnterData = null;
                        user.State = State.main;
                        await Tgbot.SendTextMessageAsync(message.From.Id,
                            "Вы в главном меню.");
                        break;
                    case "🌇 Мои аккаунты":
                        if (user.State != State.main) return;
                        foreach (var x in user.Instagrams)
                        {
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                $"{Keyboards.Emodji[Rnd.Next(0, Keyboards.Emodji.Length)]} Аккаунт {x.Username}",
                                replyMarkup: Keyboards.Exit(x.Id));
                        }

                        if (user.Instagrams.Count < user.Subscribes.Count)
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Вы можете добавить аккаунт",
                                replyMarkup: new InlineKeyboardMarkup(
                                    InlineKeyboardButton.WithCallbackData("➕ Добавить аккаунт", "enterData")));
                        else
                        {
                            await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                "Оплатите подписку, чтобы добавить аккаунт.", replyMarkup: Keyboards.MainKeyboard);
                        }

                        break;
                    case "❤ Отработка":
                        if (user.State != State.main) return;
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Выберите, что вы хотите сделать.", replyMarkup: Keyboards.Working);
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
                        if (user.State != State.main) return;
                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            "Введите количество аккаунтов, которые хотите добавить. Цена одного аккаунта - 120 рублей/30 дней.",
                            replyMarkup: Keyboards.Main);
                        user.State = State.enterCountToBuy;
                        break;
                    case "⏱ Мои подписки":
                        if (user.State != State.main) return;
                        string subscribes = $"У вас {user.Subscribes.Count} подписки(ок).\n";
                        int i = 0;
                        foreach (var sub in user.Subscribes.ToList())
                        {
                            i++;
                            subscribes += $"Подписка {i}. Истекает {sub.EndSubscribe:D}\n";
                        }

                        await Tgbot.SendTextMessageAsync(message.Chat.Id,
                            subscribes);
                        break;
                    default:
                        switch (user.State)
                        {
                            case State.login:
                                if (user.Instagrams.Find(instagram => instagram.Username == message.Text) != null)
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Вы уже добавили этот аккаунт.");
                                    user.State = State.main;
                                    break;
                                }

                                user.EnterData.Username = message.Text;
                                user.State = State.password;
                                await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                    "Теперь введите пароль.", replyMarkup: Keyboards.Back);
                                break;
                            case State.password:
                                if (e.Message.Text.Length < 6)
                                {
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id,
                                        "Пароль не может быть меньше 6 символов!");
                                    return;
                                }

                                user.State = State.block;
                                user.EnterData.Password = message.Text;
                                var login = await Operation.CheckLoginAsync(user.EnterData);
                                if (await Login(user, login))
                                {
                                    await using Db db = new Db();
                                    db.Update(user);
                                    db.Add(user.EnterData);
                                    user.EnterData = null;
                                    user.State = State.main;
                                    await db.SaveChangesAsync();
                                }

                                break;
                            case State.twoFactor:
                                var x = await Operation.SendCodeTwoFactorAsync(user.EnterData.Api, message.Text);
                                if (x == null || !x.Succeeded)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Ошибка. Попробуйте войти ещё раз.");
                                    user.State = State.main;
                                    return;
                                }

                                switch (x.Value)
                                {
                                    case InstaLoginTwoFactorResult.Success:
                                    {
                                        user.EnterData.StateData = await user.EnterData.Api.GetStateDataAsStringAsync();
                                        await using Db db = new Db();
                                        db.Update(user);
                                        db.Add(user.EnterData);
                                        user.EnterData = null;
                                        await db.SaveChangesAsync();
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Инстаграм успешно добавлен.");
                                        user.State = State.main;
                                        break;
                                    }
                                    case InstaLoginTwoFactorResult.InvalidCode:
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Неверный код, попробуйте еще раз.", replyMarkup: Keyboards.Main);
                                        break;
                                    default:
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            "Ошибка. Попробуйте войти ещё раз.");
                                        user.State = State.main;
                                        break;
                                }

                                break;
                            case State.challengeRequiredAccept:
                                var y = await Operation.SendCodeChallengeRequiredAsync(user.EnterData.Api,
                                    message.Text);
                                if (await Login(user, y))
                                {
                                    await using Db db = new Db();
                                    db.Update(user);
                                    db.Add(user.EnterData);
                                    user.EnterData = null;
                                    await db.SaveChangesAsync();
                                }

                                break;
                            case State.challengeRequiredPhoneCall:
                                bool isRight = await Operation.SubmitPhoneChallengeRequiredAsync(user.EnterData.Api,
                                    message.Text);
                                if (isRight)
                                {
                                    user.State = State.challengeRequiredAccept;
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Код отправлен. Введите код из сообщения.", replyMarkup: Keyboards.Main);
                                }
                                else
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Ошибка. Неверный номер.", replyMarkup: Keyboards.Main);
                                }

                                break;
                            case State.selectHashtag:
                                if (!message.Text.All(_ => char.IsLetterOrDigit(_) || "_".Contains(_)))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Хештег может содержать только буквы и цифры!", replyMarkup: Keyboards.Main);
                                    return;
                                }

                                user.CurrentWorks.ForEach(_ => _.SetHashtag(message.Text));
                                user.State = State.setDuration;
                                await Tgbot.SendTextMessageAsync(message.From.Id,
                                    "Введите пределы интервала в секундах. (<strong>Пример:</strong> <em>30:120</em>).\nРекомендуемые параметры нижнего предела:\nНоввый аккаунт: <code>120 секунд.</code>\n3 - 6 месяцев: <code>90 секунд.</code>\nБольше года: <code>72 секунды.</code>\n",
                                    replyMarkup: Keyboards.Back, parseMode: ParseMode.Html);
                                break;
                            case State.setDuration:
                                if (!e.Message.Text.Contains(':'))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Неверный формат!");
                                    return;
                                }

                                if (!int.TryParse(message.Text.Split(':')[0], out var lowerDelay))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Неверный формат!");
                                    return;
                                }

                                if (!int.TryParse(message.Text.Split(':')[1], out var upperDelay))
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Неверный формат!");
                                    return;
                                }

                                if (upperDelay < lowerDelay)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Верхняя граница не может быть больше нижней!");
                                    return;
                                }

                                if (upperDelay > 300)
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Интервал не может быть больше 5 минут!");
                                    return;
                                }

                                user.CurrentWorks.ForEach(_ => _.SetDuration(lowerDelay, upperDelay));
                                await Tgbot.SendTextMessageAsync(message.From.Id,
                                    "Выбирете, когда хотите начать.", replyMarkup: Keyboards.StartWork);
                                user.State = State.setTimeWork;
                                break;
                            case State.setDate:
                                if (TimeSpan.TryParse(message.Text, out var timeSpan))
                                {
                                    var timeEnter = DateTime.Today.Add(timeSpan);
                                    if (timeEnter.CompareTo(DateTime.Now) <= 0)
                                    {
                                        timeEnter = timeEnter.AddDays(1);
                                    }

                                    user.CurrentWorks.ForEach(_ => _?.StartAtTimeAsync(timeEnter));
                                    foreach (var work in user.CurrentWorks.Where(work => work != null))
                                    {
                                        await Tgbot.SendTextMessageAsync(message.From.Id,
                                            $"Отработка на аккаунте {work.Instagram.Username} по хештегу #{work.Hashtag} начнётся в {timeEnter:HH:mm:ss}.",
                                            replyMarkup: Keyboards.Cancel(work.Id));
                                    }

                                    user.CurrentWorks.Clear();
                                    user.State = State.main;
                                }
                                else
                                {
                                    await Tgbot.SendTextMessageAsync(message.From.Id,
                                        "Неверный формат времени.");
                                }

                                break;
                            case State.enterCountToBuy:
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

                                user.State = State.main;
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
                                    replyMarkup: Keyboards.CheckBill(billId));
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
                    user.EnterData = null;
                    user.State = State.main;
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
                            user.EnterData = null;
                            user.State = State.main;
                            return false;
                        }

                        user.EnterData.StateData = await user.EnterData.Api.GetStateDataAsStringAsync();
                        await Tgbot.SendTextMessageAsync(user.Id, "Инстаграм успешно добавлен.");
                        user.State = State.main;
                        return true;
                    }
                    case InstaLoginResult.BadPassword:
                        await Tgbot.SendTextMessageAsync(user.Id, "Неверный пароль. Попробуйте ввести снова.",
                            replyMarkup: Keyboards.Back);
                        user.State = State.password;
                        break;
                    case InstaLoginResult.InvalidUser:
                        user.State = State.main;
                        await Tgbot.SendTextMessageAsync(user.Id, "Пользователь не найден. Попробуйте еще раз.",
                            replyMarkup: Keyboards.EnterData);
                        break;
                    case InstaLoginResult.LimitError:
                        user.State = State.main;
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Слишком много запросов. Подождите несколько минут и попробуйте снова.",
                            replyMarkup: Keyboards.EnterData);
                        break;
                    case InstaLoginResult.TwoFactorRequired:
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Необходима двухфакторная аутентификация. Введите код из сообщения.",
                            replyMarkup: Keyboards.Main);
                        user.State = State.twoFactor;
                        break;
                    case InstaLoginResult.ChallengeRequired:
                    {
                        var challenge = await user.EnterData.Api.GetChallengeRequireVerifyMethodAsync();
                        if (!challenge.Succeeded)
                        {
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Ошибка. Попробуйте войти ещё раз.");
                            user.EnterData = null;
                            user.State = State.main;
                            return false;
                        }

                        if (challenge.Value.SubmitPhoneRequired)
                        {
                            user.State = State.challengeRequiredPhoneCall;
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Инстаграм просит подтверждение. Введите подключенный к аккаунту номер.",
                                replyMarkup: Keyboards.Main);
                            break;
                        }

                        InlineKeyboardMarkup key;
                        if (string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
                        {
                            key = Keyboards.Email(challenge.Value.StepData.Email);
                        }
                        else if (string.IsNullOrEmpty(challenge.Value.StepData.Email))
                        {
                            key = Keyboards.Phone(challenge.Value.StepData.PhoneNumber);
                        }
                        else
                        {
                            key = Keyboards.PhoneAndEmail(challenge.Value.StepData.Email,
                                challenge.Value.StepData.PhoneNumber);
                        }

                        user.State = State.challengeRequired;
                        await Tgbot.SendTextMessageAsync(user.Id,
                            "Инстаграм просит подтверждение. Выбирете, каким образом вы хотите подтвердить код:",
                            replyMarkup: key);
                    }
                        break;
                    case InstaLoginResult.Exception:
                        if (login.Info.Exception is HttpRequestException ||
                            login.Info.Exception is NullReferenceException)
                        {
                            Operation.CheckProxy(user.EnterData.Proxy);
                            await Tgbot.SendTextMessageAsync(user.Id,
                                "Ошибка при отправке запроса. Возможно проблема с прокси, попробуйте войти ещё раз.");
                            user.EnterData = null;
                            user.State = State.main;
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

        public static async void Error(User user)
        {
            if (user == null) return;
            user.CurrentWorks.ForEach(x => user.Works.Remove(x));
            user.EnterData = null;
            user.State = State.main;
            try
            {
                await Tgbot.SendTextMessageAsync(user.Id,
                    "Произошла ошибка.", replyMarkup: Keyboards.MainKeyboard);
            }
            catch
            {
                // ignored
            }
        }
    }
}
