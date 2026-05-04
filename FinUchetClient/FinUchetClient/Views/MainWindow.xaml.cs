using FinUchetClient.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    this.DragMove();
            };
            // Загружаем сохраненную тему
            ThemeManager.LoadThemePreference();

            // Обновляем текст кнопки в зависимости от текущей темы
            UpdateThemeButtonText();

            UserNameText.Text = authService.CurrentUsername ?? "Пользователь";

            if (_isAdmin)
            {
                ShowAdminOnlyMode();
            }
        }

        private void UpdateThemeButtonText()
        {
            if (ThemeToggleButton != null)
            {
                ThemeToggleButton.Content = ThemeManager.IsDarkTheme ? "☀️ Светлая тема" : "🌙 Тёмная тема";
            }
        }

        private void ShowAdminOnlyMode()
        {
            var sidebarStack = SidebarPanel.Child as StackPanel;
            if (sidebarStack != null)
            {
                foreach (var child in sidebarStack.Children)
                {
                    if (child is Button btn)
                    {
                        string content = btn.Content?.ToString() ?? "";
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
            UpdateThemeButtonText();

            // Обновляем текущее содержимое, чтобы применить тему к дочерним контролам
            var currentContent = MainContentArea.Content;
            if (currentContent != null)
            {
                MainContentArea.Content = null;
                MainContentArea.Content = currentContent;
            }
        }

        private void ShowRules_Click(object sender, RoutedEventArgs e)
        {
            var rulesWindow = new Window
            {
                Title = "Правила использования",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Content = new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = @"ПРАВИЛА ИСПОЛЬЗОВАНИЯ СЕРВИСА ФинУчет

1. Общие положения
   - Используйте сервис только для личного финансового учета
   - Запрещено использовать сервис для незаконных целей

2. Что запрещено:
   - Оскорбления и угрозы в адрес администратора
   - Создание множественных аккаунтов
   - Попытки взлома или несанкционированного доступа
   - Распространение спама и рекламы
   - Нарушение законодательства РФ

3. Блокировка аккаунта:
   - За первое нарушение - предупреждение
   - При повторных нарушениях - временная блокировка (1-168 часов)
   - За грубые нарушения - перманентная блокировка

4. Удаление аккаунта:
   - По собственному желанию (отправьте запрос администратору)
   - После перманентной блокировки
   - По требованию уполномоченных органов

5. Администратор имеет право:
   - Блокировать пользователей при нарушении правил
   - Удалять неактивные аккаунты (более 6 месяцев без входа)
   - Изменять правила с уведомлением пользователей

Последнее обновление: 26.04.2026",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(20),
                        FontSize = 14
                    }
                }
            };
            rulesWindow.ShowDialog();
        }
    }
}