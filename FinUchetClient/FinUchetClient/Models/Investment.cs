using System;
using Newtonsoft.Json;

namespace FinUchetClient.Models
{
    public class InvestmentModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = "stock";

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("purchase_price")]
        public double PurchasePrice { get; set; }

        [JsonProperty("current_price")]
        public double CurrentPrice { get; set; }

        [JsonProperty("quantity")]
        public double Quantity { get; set; }

        [JsonProperty("purchase_date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [JsonProperty("currency")]
        public string Currency { get; set; } = "RUB";

        [JsonProperty("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonProperty("user_id")]
        public int UserId { get; set; }

        // Вычисляемые свойства (не отправляются на сервер)
        public double CurrentValue => Quantity * CurrentPrice;
        public double TotalInvested => Quantity * PurchasePrice;
        public double ProfitLoss => CurrentValue - TotalInvested;
        public double ProfitLossPercent => TotalInvested > 0 ? (ProfitLoss / TotalInvested) * 100 : 0;

        public string TypeDisplay
        {
            get
            {
                return Type switch
                {
                    "stock" => "📈 Акции",
                    "bond" => "📉 Облигации",
                    "crypto" => "₿ Криптовалюта",
                    "realty" => "🏠 Недвижимость",
                    "deposit" => "🏦 Депозит",
                    _ => "💰 Другое"
                };
            }
        }
    }
}