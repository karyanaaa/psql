using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace FinUchetClient.Services
{
    public static class ThemeManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FinUchetClient", "theme.xml");

        public static bool IsDarkTheme { get; private set; }

        public static void ApplyTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            SaveThemePreference(isDark);

            var app = Application.Current;
            if (app == null) return;

            // Применяем тему ко всем открытым окнам
            foreach (Window window in app.Windows)
            {
                UpdateWindowTheme(window);
            }
        }

        private static void UpdateWindowTheme(Window window)
        {
            if (IsDarkTheme)
            {
                window.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));

                // Если это MainWindow, обновляем боковую панель
                if (window is Views.MainWindow mainWindow)
                {
                    var sidebar = mainWindow.FindName("SidebarPanel") as System.Windows.Controls.Border;
                    if (sidebar != null)
                    {
                        sidebar.Background = new SolidColorBrush(Color.FromRgb(30, 30, 35));
                    }

                    // Обновляем цвет кнопок в боковой панели
                    var stackPanel = sidebar?.Child as System.Windows.Controls.StackPanel;
                    if (stackPanel != null)
                    {
                        foreach (var child in stackPanel.Children)
                        {
                            if (child is System.Windows.Controls.Button btn)
                            {
                                btn.Background = System.Windows.Media.Brushes.Transparent;
                                btn.Foreground = System.Windows.Media.Brushes.White;
                            }
                        }
                    }
                }
            }
            else
            {
                window.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));

                if (window is Views.MainWindow mainWindow)
                {
                    var sidebar = mainWindow.FindName("SidebarPanel") as System.Windows.Controls.Border;
                    if (sidebar != null)
                    {
                        sidebar.Background = new SolidColorBrush(Color.FromRgb(44, 62, 80));
                    }

                    var stackPanel = sidebar?.Child as System.Windows.Controls.StackPanel;
                    if (stackPanel != null)
                    {
                        foreach (var child in stackPanel.Children)
                        {
                            if (child is System.Windows.Controls.Button btn)
                            {
                                btn.Background = System.Windows.Media.Brushes.Transparent;
                                btn.Foreground = System.Windows.Media.Brushes.White;
                            }
                        }
                    }
                }
            }
        }

        public static void LoadThemePreference()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var content = File.ReadAllText(SettingsPath);
                    IsDarkTheme = content.Contains("Dark");
                }
                else
                {
                    IsDarkTheme = false;
                }
                ApplyTheme(IsDarkTheme);
            }
            catch
            {
                ApplyTheme(false);
            }
        }

        private static void SaveThemePreference(bool isDark)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(SettingsPath, isDark ? "Dark" : "Light");
            }
            catch { }
        }
    }
}