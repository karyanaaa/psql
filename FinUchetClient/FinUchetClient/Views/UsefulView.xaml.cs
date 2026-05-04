using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using FinUchetClient.Services;

namespace FinUchetClient.Views
{
    public partial class UsefulView : UserControl
    {
        private CurrencyService _currencyService;
        private readonly string _checklistPath;

        public UsefulView()
        {
            InitializeComponent();
            _currencyService = new CurrencyService();
            _checklistPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FinUchetClient", "checklist.json");

            // Устанавливаем валюты по умолчанию
            if (FromCurrencyBox.Items.Count > 0)
                FromCurrencyBox.SelectedIndex = 0;
            if (ToCurrencyBox.Items.Count > 0)
                ToCurrencyBox.SelectedIndex = 1;

            // Загружаем сохраненное состояние чек-листа
            LoadChecklistState();

            // Обновляем курсы в фоне
            _ = UpdateRatesAsync();
        }

        private void LoadChecklistState()
        {
            try
            {
                if (File.Exists(_checklistPath))
                {
                    var json = File.ReadAllText(_checklistPath);
                    var states = System.Text.Json.JsonSerializer.Deserialize<bool[]>(json);

                    if (states != null && states.Length >= 8)
                    {
                        CheckBox1.IsChecked = states[0];
                        CheckBox2.IsChecked = states[1];
                        CheckBox3.IsChecked = states[2];
                        CheckBox4.IsChecked = states[3];
                        CheckBox5.IsChecked = states[4];
                        CheckBox6.IsChecked = states[5];
                        CheckBox7.IsChecked = states[6];
                        CheckBox8.IsChecked = states[7];
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadChecklist error: {ex.Message}");
            }
        }

        private void SaveChecklistState()
        {
            try
            {
                var states = new bool[]
                {
                    CheckBox1.IsChecked ?? false,
                    CheckBox2.IsChecked ?? false,
                    CheckBox3.IsChecked ?? false,
                    CheckBox4.IsChecked ?? false,
                    CheckBox5.IsChecked ?? false,
                    CheckBox6.IsChecked ?? false,
                    CheckBox7.IsChecked ?? false,
                    CheckBox8.IsChecked ?? false
                };

                var directory = Path.GetDirectoryName(_checklistPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = System.Text.Json.JsonSerializer.Serialize(states);
                File.WriteAllText(_checklistPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveChecklist error: {ex.Message}");
            }
        }

        // Подписываемся на событие изменения состояния чек-боксов
        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            SaveChecklistState();
        }

        private void CalculateSavings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double monthly = double.Parse(MonthlyAmountBox.Text);
                double periodValue = double.Parse(PeriodValueBox.Text);
                string unit = (PeriodUnitBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Месяцев";

                double total = 0;
                string periodText = "";

                switch (unit)
                {
                    case "Дней":
                        total = monthly * (periodValue / 30.0);
                        periodText = $"{periodValue} дней";
                        break;
                    case "Месяцев":
                        total = monthly * periodValue;
                        periodText = $"{periodValue} месяцев";
                        break;
                    case "Лет":
                        total = monthly * periodValue * 12;
                        periodText = $"{periodValue} лет";
                        break;
                }

                SavingsResult.Text = $"💰 За {periodText} вы накопите: {total:N0} ₽";

                if (total > 0)
                {
                    SavingsDetail.Visibility = Visibility.Visible;
                    SavingsDetail.Text = $"📊 Ежемесячный взнос: {monthly:N0} ₽\n" +
                                        $"💡 Совет: если увеличить взнос на 20%, вы накопите {(total * 1.2):N0} ₽\n" +
                                        $"⭐ В день это: {(monthly / 30):N2} ₽";
                }
            }
            catch
            {
                SavingsResult.Text = "❌ Введите корректные числа";
                SavingsDetail.Visibility = Visibility.Collapsed;
            }
        }

        private async void Currency_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConvertCurrency_Click(sender, null);
        }

        private async void ConvertCurrency_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromCurrencyBox.SelectedItem == null || ToCurrencyBox.SelectedItem == null)
                    return;

                double amount = double.Parse(AmountToConvert.Text);

                string fromCode = ((ComboBoxItem)FromCurrencyBox.SelectedItem).Tag.ToString();
                string toCode = ((ComboBoxItem)ToCurrencyBox.SelectedItem).Tag.ToString();

                var fromCurrency = _currencyService.GetCurrency(fromCode);
                var toCurrency = _currencyService.GetCurrency(toCode);

                if (fromCurrency == null || toCurrency == null) return;

                double result = 0;
                string conversionText = "";

                if (fromCode == "RUB")
                {
                    result = amount / toCurrency.RateToRub;
                    conversionText = $"{amount:N2} ₽ → {result:N2} {toCurrency.Symbol}";
                }
                else if (toCode == "RUB")
                {
                    result = amount * fromCurrency.RateToRub;
                    conversionText = $"{amount:N2} {fromCurrency.Symbol} → {result:N2} ₽";
                }
                else
                {
                    double inRub = amount * fromCurrency.RateToRub;
                    result = inRub / toCurrency.RateToRub;
                    conversionText = $"{amount:N2} {fromCurrency.Symbol} → {result:N2} {toCurrency.Symbol}";
                }

                ConversionResult.Text = $"{result:N2} {toCurrency.Symbol}";
                ExchangeRateInfo.Text = $"Курс: 1 {fromCurrency.Symbol} = {fromCurrency.RateToRub:N2} ₽ | " +
                                       $"1 {toCurrency.Symbol} = {toCurrency.RateToRub:N2} ₽";
            }
            catch (FormatException)
            {
                ConversionResult.Text = "❌ Введите корректную сумму";
            }
            catch (Exception ex)
            {
                ConversionResult.Text = $"❌ Ошибка: {ex.Message}";
            }
        }

        private async void UpdateRates_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.IsEnabled = false;
            button.Content = "Обновление...";

            try
            {
                await _currencyService.UpdateRatesAsync();
                MessageBox.Show("Курсы валют успешно обновлены!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ConvertCurrency_Click(sender, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления курсов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                button.Content = "🔄 Обновить курсы валют";
            }
        }

        private async Task UpdateRatesAsync()
        {
            try
            {
                await _currencyService.UpdateRatesAsync();
            }
            catch { }
        }
    }
}