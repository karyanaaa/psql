using FinUchetClient.Models;
using FinUchetClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FinUchetClient.Views
{
    public partial class InvestmentsView : UserControl
    {
        private ApiService _apiService;
        private List<InvestmentModel> _investments;

        public InvestmentsView()
        {
            InitializeComponent();
           
            _apiService = new ApiService();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            try
            {
                var token = ((App)Application.Current).Token;
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetToken(token);

                _investments = await _apiService.GetInvestmentsAsync();

                if (_investments == null)
                    _investments = new List<InvestmentModel>();

                // Отладка: выводим в консоль что пришло с сервера
                System.Diagnostics.Debug.WriteLine($"=== ЗАГРУЖЕНО ИНВЕСТИЦИЙ: {_investments.Count} ===");
                foreach (var inv in _investments)
                {
                    System.Diagnostics.Debug.WriteLine($"ID: {inv.Id}, Name: {inv.Name}, Quantity: {inv.Quantity}, PurchasePrice: {inv.PurchasePrice}, CurrentPrice: {inv.CurrentPrice}");
                }

                UpdateStatistics();
                UpdateGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки инвестиций: {ex.Message}");
            }
        }

        private void UpdateStatistics()
        {
            if (_investments == null || _investments.Count == 0)
            {
                TotalInvestedText.Text = "0 ₽";
                CurrentValueText.Text = "0 ₽";
                ProfitLossText.Text = "0 ₽";
                ProfitPercentText.Text = "0%";
                return;
            }

            double totalInvested = 0;
            double currentValue = 0;

            foreach (var inv in _investments)
            {
                double invested = inv.Quantity * inv.PurchasePrice;
                double current = inv.Quantity * inv.CurrentPrice;

                totalInvested += invested;
                currentValue += current;

                System.Diagnostics.Debug.WriteLine($"Инвестиция {inv.Name}: вложено={invested}, тек.стоимость={current}");
            }

            double profitLoss = currentValue - totalInvested;
            double profitPercent = totalInvested > 0 ? (profitLoss / totalInvested) * 100 : 0;

            TotalInvestedText.Text = $"{totalInvested:N2} ₽";
            CurrentValueText.Text = $"{currentValue:N2} ₽";

            if (profitLoss >= 0)
            {
                ProfitLossText.Text = $"+{profitLoss:N2} ₽";
                ProfitLossText.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                ProfitPercentText.Text = $"+{profitPercent:F1}%";
                ProfitPercentText.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
            }
            else
            {
                ProfitLossText.Text = $"{profitLoss:N2} ₽";
                ProfitLossText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                ProfitPercentText.Text = $"{profitPercent:F1}%";
                ProfitPercentText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
        }

        private void UpdateGrid()
        {
            if (_investments == null) return;

            var displayList = _investments.Select(i => new
            {
                i.Id,
                i.Name,
                Тип = i.TypeDisplay,
                Количество = i.Quantity,
                Цена_покупки = $"{i.PurchasePrice:N2} ₽",
                Текущая_цена = $"{i.CurrentPrice:N2} ₽",
                Вложено = $"{i.TotalInvested:N2} ₽",
                Текущая_стоимость = $"{i.CurrentValue:N2} ₽",
                Прибыль = i.ProfitLoss >= 0 ? $"+{i.ProfitLoss:N2} ₽" : $"{i.ProfitLoss:N2} ₽",
                Доходность = i.ProfitLossPercent >= 0 ? $"+{i.ProfitLossPercent:F1}%" : $"{i.ProfitLossPercent:F1}%"
            }).ToList();

            InvestmentsGrid.ItemsSource = displayList;
        }

        private async void AddInvestment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка названия
                if (string.IsNullOrWhiteSpace(InvestmentNameBox.Text))
                {
                    MessageBox.Show("Введите название инвестиции (например: 'Сбербанк', 'Bitcoin')",
                        "Подсказка", MessageBoxButton.OK, MessageBoxImage.Information);
                    InvestmentNameBox.Focus();
                    return;
                }

                // Проверка количества
                if (!double.TryParse(QuantityBox.Text, out double quantity) || quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество (например: 10, 1.5)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    QuantityBox.Focus();
                    return;
                }

                // Проверка цены покупки
                if (!double.TryParse(PurchasePriceBox.Text, out double purchasePrice) || purchasePrice <= 0)
                {
                    MessageBox.Show("Введите корректную цену покупки (например: 150.50)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PurchasePriceBox.Focus();
                    return;
                }

                // Проверка текущей цены
                if (!double.TryParse(CurrentPriceBox.Text, out double currentPrice) || currentPrice <= 0)
                {
                    MessageBox.Show("Введите корректную текущую цену (например: 180.00)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CurrentPriceBox.Focus();
                    return;
                }

                string selectedType = ((ComboBoxItem)InvestmentTypeBox.SelectedItem).Content.ToString();
                string typeCode = selectedType switch
                {
                    "📈 Акции" => "stock",
                    "📉 Облигации" => "bond",
                    "₿ Криптовалюта" => "crypto",
                    "🏠 Недвижимость" => "realty",
                    "🏦 Депозит" => "deposit",
                    _ => "other"
                };

                // Показываем предварительный расчет
                double totalInvested = quantity * purchasePrice;
                double currentValue = quantity * currentPrice;
                double profitLoss = currentValue - totalInvested;
                double profitPercent = totalInvested > 0 ? (profitLoss / totalInvested) * 100 : 0;

                var confirmResult = MessageBox.Show(
                    $"📊 ПРЕДВАРИТЕЛЬНЫЙ РАСЧЕТ:\n\n" +
                    $"💰 Вложено: {totalInvested:N2} ₽\n" +
                    $"📈 Текущая стоимость: {currentValue:N2} ₽\n" +
                    $"{(profitLoss >= 0 ? "✅" : "❌")} Прибыль: {(profitLoss >= 0 ? "+" : "")}{profitLoss:N2} ₽\n" +
                    $"{(profitLoss >= 0 ? "📈" : "📉")} Доходность: {(profitPercent >= 0 ? "+" : "")}{profitPercent:F1}%\n\n" +
                    $"Добавить инвестицию '{InvestmentNameBox.Text}'?",
                    "Подтверждение", MessageBoxButton.YesNo,
                    profitLoss >= 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                var investment = new InvestmentModel
                {
                    Name = InvestmentNameBox.Text.Trim(),
                    Type = typeCode,
                    Quantity = quantity,
                    PurchasePrice = purchasePrice,
                    CurrentPrice = currentPrice,
                    PurchaseDate = DateTime.Now,
                    Currency = "RUB",
                    Amount = totalInvested
                };

                System.Diagnostics.Debug.WriteLine($"Добавляем инвестицию: Name={investment.Name}, Quantity={investment.Quantity}, PurchasePrice={investment.PurchasePrice}, CurrentPrice={investment.CurrentPrice}");

                var success = await _apiService.AddInvestmentAsync(investment);
                if (success)
                {
                    await LoadData();
                    MessageBox.Show($"✅ Инвестиция '{investment.Name}' успешно добавлена!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очищаем поля
                    InvestmentNameBox.Text = "";
                    QuantityBox.Text = "1";
                    PurchasePriceBox.Text = "0";
                    CurrentPriceBox.Text = "0";
                    InvestmentNameBox.Focus();
                }
                else
                {
                    MessageBox.Show("❌ Ошибка добавления инвестиции. Попробуйте позже.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AddInvestment error: {ex.Message}");
            }
        }

        // Обновить статистику вручную
        private async void RefreshStats_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }
    }
}