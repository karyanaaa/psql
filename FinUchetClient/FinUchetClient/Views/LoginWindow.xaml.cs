using FinUchetClient.Services;
using System.Windows;
using System.Windows.Controls;

namespace FinUchetClient.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private RegisterWindow _registerWindow;
        private ForgotPasswordWindow _forgotWindow;

        public LoginWindow(AuthService authService, ApiService apiService)
        {
            InitializeComponent();
            _authService = authService;
            _apiService = apiService;
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите логин и пароль");
                return;
            }

            try
            {
                var result = await _apiService.LoginAsync(username, password);
                string token = result.token;
                bool isAdmin = result.isAdmin;
                string errorMessage = result.errorMessage;

                if (!string.IsNullOrEmpty(token))
                {
                    ((App)Application.Current).Token = token;
                    ((App)Application.Current).IsAdmin = isAdmin;
                    _apiService.SetToken(token);
                    await _authService.LoginAsync(username, password);

                    // Для всех пользователей открываем одно окно MainWindow
                    // Админ-панель будет доступна через кнопку в меню
                    var mainWindow = new MainWindow(_apiService, _authService, isAdmin);
                    mainWindow.Show();
                    this.Close();
                }
                else if (!string.IsNullOrEmpty(errorMessage))
                {
                    // Показываем сообщение о блокировке
                    ShowError(errorMessage);
                }
                else
                {
                    ShowError("Неверный логин или пароль");
                }
            }
            catch (System.Exception ex)
            {
                ShowError($"Ошибка подключения к серверу: {ex.Message}");
            }
        }

        private void RegisterHyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (_registerWindow == null || !_registerWindow.IsVisible)
            {
                _registerWindow = new RegisterWindow();
                _registerWindow.Closed += (s, args) => { this.Show(); _registerWindow = null; };
            }
            this.Hide();
            _registerWindow.Show();
        }

        private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_forgotWindow == null || !_forgotWindow.IsVisible)
            {
                _forgotWindow = new ForgotPasswordWindow();
                _forgotWindow.Closed += (s, args) => { this.Show(); _forgotWindow = null; };
            }
            this.Hide();
            _forgotWindow.Show();
        }
    }
}