using FinUchetClient.Models;
using FinUchetClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FinUchetClient.Views
{
    public partial class AdminView : UserControl
    {
        private ApiService _apiService;
        private dynamic _selectedUser;
        private dynamic _selectedMessage;

        public AdminView()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null)
                {
                    parentWindow.MouseLeftButtonDown += (sender, args) =>
                    {
                        if (args.ButtonState == MouseButtonState.Pressed)
                            parentWindow.DragMove();
                    };
                }
            };
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

                var users = await _apiService.GetAdminUsersAsync();
                if (users != null)
                {
                    UsersGrid.ItemsSource = users;
                }

                var messages = await _apiService.GetAdminMessagesAsync();
                if (messages != null)
                {
                    MessagesGrid.ItemsSource = messages;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedUser = UsersGrid.SelectedItem;
            if (_selectedUser != null)
            {
                try
                {
                    int userId = Convert.ToInt32(_selectedUser.id);
                    string username = _selectedUser.username.ToString();
                    bool isBlocked = Convert.ToBoolean(_selectedUser.is_blocked);

                    SelectedUserText.Text = $"👤 {username} | {(isBlocked ? "🔒 Заблокирован" : "🟢 Активен")}";

                    UserDetailsText.Text =
                        $"👤 Имя: {username}\n" +
                        $"📧 Email: {(_selectedUser.email ?? "—")}\n" +
                        $"👑 Админ: {(Convert.ToBoolean(_selectedUser.is_admin) ? "Да" : "Нет")}\n" +
                        $"🔒 Статус: {(isBlocked ? "Заблокирован" : "Активен")}\n" +
                        $"🆔 ID: {userId}";

                    await LoadUserData(userId);
                    await LoadUserStats(userId);
                }
                catch (Exception ex)
                {
                    SelectedUserText.Text = $"Ошибка: {ex.Message}";
                }
            }
            else
            {
                SelectedUserText.Text = "Пользователь не выбран";
                UserDetailsText.Text = "Выберите пользователя";
                UserTotalIncome.Text = "0 ₽";
                UserTotalExpense.Text = "0 ₽";
                UserTotalInvested.Text = "0 ₽";
                UserCurrentValue.Text = "0 ₽";
                UserTransactionsGrid.ItemsSource = null;
                UserInvestmentsGrid.ItemsSource = null;
                UserMessagesGrid.ItemsSource = null;
            }
        }

        private async System.Threading.Tasks.Task LoadUserData(int userId)
        {
            try
            {
                var transactions = await _apiService.GetTransactionsByUserAsync(userId);
                if (transactions != null)
                {
                    var displayTransactions = transactions.Select(t => new
                    {
                        t.Id,
                        t.Description,
                        Сумма = t.Amount,
                        Тип = t.Type == "income" ? "Доход" : "Расход",
                        Категория = t.CategoryName,
                        Дата = t.Date.ToString("dd.MM.yyyy")
                    }).ToList();
                    UserTransactionsGrid.ItemsSource = displayTransactions;
                }

                var investments = await _apiService.GetInvestmentsByUserAsync(userId);
                if (investments != null)
                {
                    var displayInvestments = investments.Select(i => new
                    {
                        i.Id,
                        i.Name,
                        Тип = i.Type,
                        Количество = i.Quantity,
                        Цена_покупки = i.PurchasePrice,
                        Текущая_цена = i.CurrentPrice,
                        Прибыль = i.ProfitLoss
                    }).ToList();
                    UserInvestmentsGrid.ItemsSource = displayInvestments;
                }

                var messages = await _apiService.GetMessagesByUserAsync(userId);
                if (messages != null)
                {
                    var displayMessages = messages.Select(m => new
                    {
                        m.Id,
                        m.Subject,
                        m.Message,
                        Дата = m.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                        Ответ = string.IsNullOrEmpty(m.Response) ? "—" : m.Response
                    }).ToList();
                    UserMessagesGrid.ItemsSource = displayMessages;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadUserData error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task LoadUserStats(int userId)
        {
            try
            {
                var stats = await _apiService.GetUserStatsAsync(userId);
                if (stats != null)
                {
                    UserTotalIncome.Text = $"{Convert.ToDouble(stats.total_income):N2} ₽";
                    UserTotalExpense.Text = $"{Convert.ToDouble(stats.total_expense):N2} ₽";
                    UserTotalInvested.Text = $"{Convert.ToDouble(stats.total_invested):N2} ₽";
                    UserCurrentValue.Text = $"{Convert.ToDouble(stats.current_value):N2} ₽";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadUserStats error: {ex.Message}");
            }
        }

        private async void ChangeUserPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }

            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmNewPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Введите новый пароль");
                return;
            }

            if (newPassword.Length < 4)
            {
                MessageBox.Show("Пароль должен быть не менее 4 символов");
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают");
                return;
            }

            int userId = Convert.ToInt32(_selectedUser.id);
            string username = _selectedUser.username.ToString();

            var result = MessageBox.Show($"Сменить пароль пользователю '{username}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _apiService.ChangeUserPasswordAsync(userId, newPassword);
                    if (success)
                    {
                        MessageBox.Show($"Пароль пользователя '{username}' успешно изменен!");
                        NewPasswordBox.Password = "";
                        ConfirmNewPasswordBox.Password = "";
                    }
                    else
                    {
                        MessageBox.Show("Ошибка смены пароля");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private async void BlockUser_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }

            try
            {
                bool isAdmin = Convert.ToBoolean(_selectedUser.is_admin);
                if (isAdmin)
                {
                    MessageBox.Show("Нельзя заблокировать администратора");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка определения прав: {ex.Message}");
                return;
            }

            var selectedItem = BlockTimeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Выберите время блокировки");
                return;
            }

            int hours = 0;
            if (selectedItem.Tag != null)
            {
                int.TryParse(selectedItem.Tag.ToString(), out hours);
            }

            int userId = Convert.ToInt32(_selectedUser.id);
            string username = _selectedUser.username.ToString();

            string message = hours == 0 ?
                $"Заблокировать пользователя '{username}' навсегда?" :
                $"Заблокировать пользователя '{username}' на {selectedItem.Content}?";

            var result = MessageBox.Show(message, "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _apiService.BlockUserAsync(userId, true, hours);
                    if (success)
                    {
                        MessageBox.Show($"Пользователь '{username}' заблокирован");
                        await LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка блокировки");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private async void UnblockUser_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }

            int userId = Convert.ToInt32(_selectedUser.id);
            string username = _selectedUser.username.ToString();

            var result = MessageBox.Show($"Разблокировать пользователя '{username}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _apiService.BlockUserAsync(userId, false, 0);
                    if (success)
                    {
                        MessageBox.Show($"Пользователь '{username}' разблокирован");
                        await LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка разблокировки");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }

            try
            {
                bool isAdmin = Convert.ToBoolean(_selectedUser.is_admin);
                if (isAdmin)
                {
                    MessageBox.Show("Нельзя удалить администратора");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка определения прав: {ex.Message}");
                return;
            }

            int userId = Convert.ToInt32(_selectedUser.id);
            string username = _selectedUser.username.ToString();

            var result = MessageBox.Show($"Удалить пользователя '{username}' и ВСЕ его данные?\nЭто действие необратимо!",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var success = await _apiService.DeleteUserAsync(userId);
                    if (success)
                    {
                        MessageBox.Show($"Пользователь '{username}' удален");
                        await LoadData();
                        UserDetailsText.Text = "Выберите пользователя";
                        UserTotalIncome.Text = "0 ₽";
                        UserTotalExpense.Text = "0 ₽";
                        UserTransactionsGrid.ItemsSource = null;
                        UserInvestmentsGrid.ItemsSource = null;
                        UserMessagesGrid.ItemsSource = null;
                    }
                    else
                    {
                        MessageBox.Show("Ошибка удаления");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                }
            }
        }

        private async void ExportUserData_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }

            int userId = Convert.ToInt32(_selectedUser.id);
            string username = _selectedUser.username.ToString();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = $"Экспорт данных пользователя {username}",
                Filter = "CSV файлы (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = $"user_{username}_data_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _apiService.ExportUserDataToCsvAsync(userId, dialog.FileName);
                    MessageBox.Show($"Данные пользователя '{username}' экспортированы!\n{dialog.FileName}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}");
                }
            }
        }

        private async void SendNotification_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя");
                return;
            }

            int userId = Convert.ToInt32(_selectedUser.id);
            string username = _selectedUser.username.ToString();

            var subjectBox = new TextBox { Height = 30, Margin = new Thickness(0, 0, 0, 10) };
            var messageBox = new TextBox { Height = 80, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Margin = new Thickness(0, 0, 0, 10) };

            var dialog = new Window
            {
                Title = $"Отправка уведомления пользователю {username}",
                Width = 450,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Children =
                    {
                        new TextBlock { Text = "Тема:", Margin = new Thickness(0, 0, 0, 5) },
                        subjectBox,
                        new TextBlock { Text = "Сообщение:", Margin = new Thickness(0, 0, 0, 5) },
                        messageBox,
                        new Button { Content = "📤 Отправить", Height = 35, Background = System.Windows.Media.Brushes.DodgerBlue, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 10, 0, 0) }
                    }
                }
            };

            var sendButton = (dialog.Content as StackPanel).Children[4] as Button;
            sendButton.Click += async (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(subjectBox.Text) || string.IsNullOrWhiteSpace(messageBox.Text))
                {
                    MessageBox.Show("Заполните тему и сообщение");
                    return;
                }

                var success = await _apiService.SendNotificationToUserAsync(userId, subjectBox.Text, messageBox.Text);

                if (success)
                {
                    MessageBox.Show("Уведомление отправлено!");
                    dialog.Close();
                }
                else
                {
                    MessageBox.Show("Ошибка отправки");
                }
            };

            dialog.ShowDialog();
        }

        private void MessagesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedMessage = MessagesGrid.SelectedItem;
        }

        private async void SendResponse_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMessage == null)
            {
                MessageBox.Show("Выберите сообщение");
                return;
            }

            if (string.IsNullOrWhiteSpace(ResponseBox.Text))
            {
                MessageBox.Show("Введите ответ");
                return;
            }

            try
            {
                int messageId = Convert.ToInt32(_selectedMessage.id);
                var success = await _apiService.RespondToMessageAsync(messageId, ResponseBox.Text);

                if (success)
                {
                    MessageBox.Show("Ответ отправлен!");
                    ResponseBox.Text = "";
                    await LoadData();
                }
                else
                {
                    MessageBox.Show("Ошибка отправки ответа");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}