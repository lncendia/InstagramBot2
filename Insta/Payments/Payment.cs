using System;
using System.Linq;
using Insta.Entities;
using Newtonsoft.Json.Linq;
using Qiwi.BillPayments.Client;
using Qiwi.BillPayments.Model;
using Qiwi.BillPayments.Model.In;
using RestSharp;

namespace Insta.Payments
{
    internal static class Payment
    {
        private const string SecretKey =
            "eyJ2ZXJzaW9uIjoiUDJQIiwiZGF0YSI6eyJwYXlpbl9tZXJjaGFudF9zaXRlX3VpZCI6Imk0ZmVmNS0wMCIsInVzZXJfaWQiOiI3OTI2NjY2Mzk0MSIsInNlY3JldCI6ImUzZjI1YWNjZDJhZTA5YzJjZjNlMDUwMGI1YjM4NDdmMTljZjA4YjU4MjNjYmQ3M2IyY2Q2Mzk4ODE4NGE1YTcifX0=";

        private static readonly BillPaymentsClient Client = BillPaymentsClientFactory.Create(SecretKey);

        public static string AddTransaction(int sum, User user, ref string billId)
        {
            try
            {
                var response = Client.CreateBill(
                    info: new CreateBillInfo
                    {
                        BillId = Guid.NewGuid().ToString(),
                        Amount = new MoneyAmount
                        {
                            ValueDecimal = sum,
                            CurrencyEnum = CurrencyEnum.Rub
                        },
                        ExpirationDateTime = DateTime.Now.AddDays(5),
                        Customer = new Customer
                        {
                            Account = user.Id.ToString()
                        }
                    });
                billId = response.BillId;
                return response.PayUrl.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static readonly RestClient HttpClient = new();

        public static bool CheckPay(User user, string billId)
        {
            try
            {
                HttpClient.BaseUrl = new Uri($"https://api.qiwi.com/partner/bill/v1/bills/{billId}");
                var request = new RestRequest(Method.GET);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", $"Bearer {SecretKey}");
                IRestResponse response1 = HttpClient.Execute(request);
                dynamic jObject = JObject.Parse(response1.Content);
                if (jObject.status.value != "PAID") return false;
                int amount = (int) decimal.Parse(jObject.amount.value.ToString().Replace('.', ','));
                using var db = new Db();
                db.Update(user);
                db.Add(new Transaction
                {
                    Amount = amount, User = user, DateTime = DateTime.Parse(jObject.status.changedDateTime.ToString())
                });
                db.SaveChanges();
                for (var i = amount / 120; i > 0; i--)
                {
                    db.Add(new Subscribe {User = user});
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
}
