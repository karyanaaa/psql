using System.Windows;
using System.Windows.Controls;
using FinUchetClient.Services;

namespace FinUchetClient.Views
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly AuthService _authService;
        private readonly bool _isAdmin;

        public MainWindow(ApiService apiService, AuthService authService, bool isAdmin = false)
        {
            InitializeComponent();
            _apiService = apiService;
            _authService = authService;
            _isAdmin = isAdmin;

            ThemeManager.LoadThemePreference();

            if (ThemeToggleButton != null)
            {
                ThemeToggleButton.Content = ThemeManager.IsDarkTheme ? "☀️ Светлая тема" : "🌙 Тёмная тема";
            }

            UserNameText.Text = authService.CurrentUsername ?? "Пользователь";

            // Если админ - показываем только админ панель
            if (_isAdmin)
            {
                ShowAdminOnlyMode();
            }
        }

        private void ShowAdminOnlyMode()
        {
            // Скрываем все обычные кнопки
            var sidebarStack = SidebarPanel.Child as StackPanel;
            if (sidebarStack != null)
            {
                // Проходим по всем дочерним элементам и скрываем обычные кнопки
                foreach (var child in sidebarStack.Children)
                {
                    if (child is Button btn)
                    {
                        string content = btn.Content?.ToString() ?? "";
                        // Скрываем обычные кнопки, оставляем только Выход и Тему
                        if (content != "🚪 Выход" &&
                            content != "🌙 Тёмная тема" &&
                            content != "☀️ Светлая тема" &&
                            content != "👑 Админ панель")
                        {
                            btn.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }

            // Показываем админ панель
            MainContentArea.Content = new AdminView();
        }

        private void ShowTransactions_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
                MainContentArea.Content = new TransactionsView();
        }

        private void ShowCategories_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
                MainContentArea.Content = new CategoriesView();
        }

        private void ShowStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
                MainContentArea.Content = new StatisticsView();
        }

        private void ShowInvestments_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
                MainContentArea.Content = new InvestmentsView();
        }

        private void ShowUseful_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
                MainContentArea.Content = new UsefulView();
        }

        private void ShowInstructions_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
                MainContentArea.Content = new InstructionsView();
        }

        private void ShowFeedback_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdmin)
                MainContentArea.Content = new FeedbackView();
        }

        private void ShowAdminPanel_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = new AdminView();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _authService.Logout();

                var loginWindow = new LoginWindow(_authService, _apiService);
                loginWindow.Show();
                this.Close();
            }
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            bool isDark = !ThemeManager.IsDarkTheme;
            ThemeManager.ApplyTheme(isDark);
            ThemeToggleButton.Content = isDark ? "☀️ Светлая тема" : "🌙 Тёмная тема";
        }
    }
}