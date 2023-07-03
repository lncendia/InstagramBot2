using System.Collections.Generic;
using System.Linq;
using Insta.Bot.CallbackQueryCommands;
using Insta.Bot.Commands;
using Insta.Interfaces;
using Insta.Model;
using Insta.Working;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace Insta.Bot;

public static class BotSettings
{
    public static TelegramBotClient Get()
    {
        if (_client != null) return _client;
        Cfg = Configuration.Configuration.Initialise("appsettings.json");
        _client = new TelegramBotClient(Cfg.TelegramToken);
        Commands = InitialiseCommands();
        CallbackQueryCommands = InitialiseCallbackQueryCommands();
        using var db = new Db();
        Users = db.Users.Include(i => i.Instagrams).Include(i => i.Subscribes).Include(_ => _.Referal).ToList();
        Operation.CheckSubscribeAsync(Users);
        Operation.LoadProxy(db.Proxies.ToList());
        Operation.LoadUsersStateDataAsync(db.Instagrams.Include(i => i.User).ToList()).Wait();
        Operation.LoadWorksAsync(db.Works.Include(_ => _.Instagram).ToList()).Wait();
        return _client;
    }

    private static List<ITextCommand> InitialiseCommands()
    {
        return new List<ITextCommand>
        {
            new StartCommand(),
            new AccountsCommand(),
            new HelpCommand(),
            new InstructionCommand(),
            new ProfileCommand(),
            new PaymentCommand(),
            new SendKeyboardCommand(),
            new WorkCommand(),
            new AdminMailingCommand(),
            new AdminSubscribesCommand(),
            new EnterDateCommand(),
            new EnterOffsetCommand(),
            new EnterDurationCommand(),
            new EnterHashtagCommand(),
            new EnterLoginCommand(),
            new EnterPasswordCommand(),
            new EnterCountSubscribesCommand(),
            new EnterPhoneNumberCommand(),
            new EnterTwoFactorCommand(),
            new EnterChallengeRequireCodeCommand(),
            new EnterSubscribeDataCommand(),
            new EnterMessageToMailingCommand(),
        };
    }

    private static List<ICallbackQueryCommand> InitialiseCallbackQueryCommands()
    {
        return new List<ICallbackQueryCommand>
        {
            new BillQueryCommand(),
            new ExitQueryCommand(),
            new CancelWorkQueryCommand(),
            new ChallengeEmailQueryCommand(),
            new ChallengePhoneQueryCommand(),
            new ChangeProxyQueryCommand(),
            new GoBackQueryCommand(),
            new SetOffsetQueryCommand(),
            new StartWithOutOffsetQueryCommand(),
            new MainMenuQueryCommand(),
            new SelectAccountQueryCommand(),
            new SelectModeQueryCommand(),
            new StartLaterQueryCommand(),
            new StartNowQueryCommand(),
            new AcceptLogInQueryCommand(),
            new ListOfWorksQueryCommand(),
            new ReLogInQueryCommand(),
            new SelectAccountsListQueryCommand(),
            new SelectAllAccountsQueryCommand(),
            new SelectHashtagTypeQueryCommand(),
            new HashtagTypeQueryCommand(),
            new ModeQueryCommand(),
            new StartEnterAccountDataQueryCommand(),
            new MySubscribesQueryCommand()
        };
    }

    public static List<User> Users;
    public static Configuration.Configuration Cfg;
    private static TelegramBotClient _client;
    public static List<ITextCommand> Commands;
    public static List<ICallbackQueryCommand> CallbackQueryCommands;
}