using System;
using System.Linq;
using System.Threading.Tasks;
using Insta.Bot;
using Insta.Model;
using Newtonsoft.Json.Linq;
using Qiwi.BillPayments.Client;
using Qiwi.BillPayments.Model;
using Qiwi.BillPayments.Model.In;
using RestSharp;

namespace Insta.Payments;

internal class PaymentService
{
    private readonly BillPaymentsClient _client = BillPaymentsClientFactory.Create(BotSettings.Cfg.QiwiToken);

    public async Task<Payment> AddTransaction(int sum, User user)
    {
        try
        {
            var response = await _client.CreateBillAsync(
                new CreateBillInfo
                {
                    BillId = Guid.NewGuid().ToString(),
                    Amount = new MoneyAmount
                    {
                        ValueDecimal = sum,
                        CurrencyEnum = CurrencyEnum.Rub
                    },
                    ExpirationDateTime = DateTime.Now.AddDays(5),
                    Comment = $"Оплата подписок в LikeBot."
                });
            return new Payment(response.BillId, response.PayUrl.ToString());
        }
        catch
        {
            return null;
        }
    }

    private readonly RestClient _httpClient = new();

    public bool CheckPay(User user, string billId)
    {
        try
        {
            var request = new RestRequest($"https://api.qiwi.com/partner/bill/v1/bills/{billId}");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", $"Bearer {BotSettings.Cfg.QiwiToken}");
            var response = _httpClient.Execute(request);
            dynamic jObject = JObject.Parse(response.Content!);
            if (jObject.status.value != "PAID") return false;
            var amount = (int)decimal.Parse(jObject.amount.value.ToString().Replace('.', ','));
            using var db = new Db();
            int count = int.Parse(jObject.comment.ToString().Split(' ')[1]);
            db.Update(user);
            db.Add(new Transaction
            {
                Amount = amount, User = user, DateTime = DateTime.Parse(jObject.status.changedDateTime.ToString())
            });
            for (var i = count; i > 0; i--)
            {
                db.Add(new Subscribe { User = user });
                var inst = user.Instagrams.ToList().FirstOrDefault(x => x.IsDeactivated);
                if (inst == null) continue;
                inst.IsDeactivated = false;
            }

            db.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }
}