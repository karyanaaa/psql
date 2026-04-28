using System;
using System.Windows;
using System.Windows.Controls;
using FinUchetClient.Services;
using Newtonsoft.Json.Linq;  // Добавьте эту директиву

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

        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedUser = UsersGrid.SelectedItem;
            if (_selectedUser != null)
            {
                try
                {
                    string username = _selectedUser.username.ToString();
                    bool isBlocked = Convert.ToBoolean(_selectedUser.is_blocked);
                    SelectedUserText.Text = $"Выбран: {username} | Статус: {(isBlocked ? "Заблокирован" : "Активен")}";
                }
                catch (Exception ex)
                {
                    SelectedUserText.Text = $"Ошибка: {ex.Message}";
                }
            }
            else
            {
                SelectedUserText.Text = "Пользователь не выбран";
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
                    // Добавим отладочный вывод
                    System.Diagnostics.Debug.WriteLine($"Attempting to block user {userId}, hours: {hours}");

                    var success = await _apiService.BlockUserAsync(userId, true, hours);

                    System.Diagnostics.Debug.WriteLine($"Block result: {success}");

                    if (success)
                    {
                        MessageBox.Show($"Пользователь '{username}' заблокирован");
                        await LoadData();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка блокировки. Проверьте консоль сервера.");
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