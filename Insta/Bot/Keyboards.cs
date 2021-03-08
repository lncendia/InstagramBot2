using System.Collections.Generic;
using System.Linq;
using Insta.Entities;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = Insta.Entities.User;

namespace Insta.Bot
{
    public static class Keyboards
    {
        public static readonly ReplyKeyboardMarkup MainKeyboard = new(new List<List<KeyboardButton>>
        {
            new() {new KeyboardButton("🌇 Мои аккаунты"), new KeyboardButton("❤ Отработка")},
            new() {new KeyboardButton("💰 Оплатить подписку"), new KeyboardButton("⏱ Мои подписки")},
            new() {new KeyboardButton("📄 Инструкция"), new KeyboardButton("🤝 Поддержка")}
        });

        public static readonly string[] Emodji =
            {"🏞", "🏔", "🏖", "🌋", "🏜", "🏕", "🌎", "🗽", "🌃", "☘", "🐲", "🌸", "🌓", "🍃", "☀", "☁"};

        public static readonly InlineKeyboardMarkup Back =
            new(InlineKeyboardButton.WithCallbackData("🔙 Назад", "back"));

        public static readonly InlineKeyboardMarkup Main =
            new(InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu"));

        public static readonly InlineKeyboardMarkup Working = new(
            new List<List<InlineKeyboardButton>>
            {
                new() {InlineKeyboardButton.WithCallbackData("🏃 Начать отработку", "startWorking")},
                new() {InlineKeyboardButton.WithCallbackData("⚙ Активные отработки", "stopWorking")}
            });

        public static readonly InlineKeyboardMarkup SelectMode = new(
            new List<List<InlineKeyboardButton>>
            {
                new()
                {
                    InlineKeyboardButton.WithCallbackData("❤ Лайки", "startLike"),
                    InlineKeyboardButton.WithCallbackData("💾 Сохранения", "startSave")
                },
                new() {InlineKeyboardButton.WithCallbackData("☑ Лайки + сохранения", "startAll")},
                new() {InlineKeyboardButton.WithCallbackData("➕ Подписки", "startFollowing")},
                new() {InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
            });

        public static readonly InlineKeyboardMarkup StartWork = new(
            new List<List<InlineKeyboardButton>>
            {
                new()
                {
                    InlineKeyboardButton.WithCallbackData("🏃 Начать сейчас", "startNow"),
                    InlineKeyboardButton.WithCallbackData("⌛ Начать позже", "startLater")
                },
                new() {InlineKeyboardButton.WithCallbackData("🛑 Отмена", "mainMenu")}
            });


        public static InlineKeyboardMarkup Select(User user)
        {
            List<List<InlineKeyboardButton>> accounts = user.Instagrams.Select(inst => new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData($"{Emodji[MainBot.Rnd.Next(0, Emodji.Length)]} {inst.Username}",
                    $"select_{inst.Id}")
            }).ToList();

            accounts.Add(new List<InlineKeyboardButton>()
                {InlineKeyboardButton.WithCallbackData("🗒 Выбрать все аккаунты", "selectAll")});
            accounts.Add(new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("👈 Выбрать режим", "selectMode"),
                InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")
            });

            return new InlineKeyboardMarkup(accounts);
        }

        public static InlineKeyboardMarkup NewSelect(List<IEnumerable<InlineKeyboardButton>> keyboard,
            CallbackQuery query)
        {
            keyboard.Remove(keyboard.FirstOrDefault(_ => _.FirstOrDefault()?.CallbackData == query.Data));
            if (keyboard.Count == 2)
            {
                keyboard.Remove(keyboard.FirstOrDefault(_ => _.FirstOrDefault()?.CallbackData == "selectAll"));
            }

            return new InlineKeyboardMarkup(keyboard);
        }

        public static readonly InlineKeyboardMarkup EnterData = new(
            InlineKeyboardButton.WithCallbackData("🖊 Ввести данные", "enterData"));

        public static InlineKeyboardMarkup ChangeProxy(Instagram instagram)
        {
            if (instagram == null) return null;
            return new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("♻ Сменить прокси", $"changeProxy_{instagram.Id}"));
        }

        public static InlineKeyboardMarkup ChangeProxyAndExit(Instagram instagram)
        {
            if (instagram == null) return null;
            return new InlineKeyboardMarkup(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("♻ Сменить прокси", $"changeProxy_{instagram.Id}"),
                InlineKeyboardButton.WithCallbackData("🚪 Выйти", $"exit_{instagram.Id}")
            });
        }

        public static InlineKeyboardMarkup Cancel(long id)
        {
            return new(InlineKeyboardButton.WithCallbackData("🛑 Отменить", $"cancel_{id}"));
        }

        public static InlineKeyboardMarkup Exit(long id)
        {
            return new(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🚪 Выйти", $"exit_{id}"),
                InlineKeyboardButton.WithCallbackData("♻ Перезайти", $"reLogIn_{id}")
            });
        }

        public static InlineKeyboardMarkup CheckBill(string id)
        {
            return new(
                InlineKeyboardButton.WithCallbackData("Проверить оплату",
                    $"bill_{id}"));
        }

        public static InlineKeyboardMarkup Email(string email)
        {
            return new(new List<List<InlineKeyboardButton>>()
            {
                new()
                    {InlineKeyboardButton.WithCallbackData($"✉ Эл. адресс ({email})", "challengeEmail")},
                new()
                    {InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry")},
                new() {InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
            });
        }

        public static InlineKeyboardMarkup Phone(string number)
        {
            return new(new List<List<InlineKeyboardButton>>()
            {
                new()
                    {InlineKeyboardButton.WithCallbackData($"📲 Телефон ({number})", "challengePhone")},
                new()
                    {InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry")},
                new() {InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
            });
        }

        public static InlineKeyboardMarkup PhoneAndEmail(string email, string number)
        {
            return new(new List<List<InlineKeyboardButton>>()
            {
                new()
                    {InlineKeyboardButton.WithCallbackData($"📲 Телефон ({number})", "challengePhone")},
                new()
                    {InlineKeyboardButton.WithCallbackData($"✉ Эл. адресс ({email})", "challengeEmail")},
                new()
                    {InlineKeyboardButton.WithCallbackData("✅ Я подтвердил вход в инстаграме", "acceptEntry")},
                new() {InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")}
            });
        }
    }

}