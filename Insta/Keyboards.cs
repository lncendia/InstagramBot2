using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;

namespace Insta
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


        public static InlineKeyboardMarkup Select(long id)
        {
            return new(InlineKeyboardButton.WithCallbackData("➕", $"select_{id}"));
        }

        public static readonly InlineKeyboardMarkup SelectAll =
            new(new List<List<InlineKeyboardButton>>
            {
                new() {InlineKeyboardButton.WithCallbackData("🗒 Выбрать все аккаунты", "selectAll")},
                new() {InlineKeyboardButton.WithCallbackData("🛑 Отмена", "mainMenu")}
            });

        public static readonly ReplyKeyboardMarkup EndSelection = new(new KeyboardButton("➡ Продолжить"));
        public static readonly InlineKeyboardMarkup EnterData = new(
            InlineKeyboardButton.WithCallbackData("🖊 Ввести данные", "enterData"));

        public static InlineKeyboardMarkup Cancel(long id)
        {
            return new(InlineKeyboardButton.WithCallbackData("🛑 Отменить", $"cancel_{id}"));
        }

        public static InlineKeyboardMarkup Exit(long id)
        {
            return new(InlineKeyboardButton.WithCallbackData("🚪 Выйти", $"exit_{id}"));
        }

        public static InlineKeyboardMarkup CheckBill(string id)
        {
            return new(
                new List<List<InlineKeyboardButton>>()
                {
                    new()
                    {
                        InlineKeyboardButton.WithCallbackData("Проверить оплату",
                            $"bill_{id}")
                    },
                    new()
                    {
                        InlineKeyboardButton.WithCallbackData("⭐ В главное меню", "mainMenu")
                    }
                });
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