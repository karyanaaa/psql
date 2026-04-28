using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FinUchetClient.Models;
using FinUchetClient.Services;

namespace FinUchetClient.Views
{
    public partial class FeedbackView : UserControl
    {
        private ApiService _apiService;
        private List<MessageModel> _messages;

        public FeedbackView()
        {
            InitializeComponent();
            _apiService = new ApiService();
            Loaded += async (s, e) => await LoadMessages();
        }

        private async System.Threading.Tasks.Task LoadMessages()
        {
            try
            {
                var token = ((App)Application.Current).Token;
                if (!string.IsNullOrEmpty(token))
                    _apiService.SetToken(token);

                _messages = await _apiService.GetMessagesAsync();

                var displayList = _messages?.Select(m => new
                {
                    m.Id,
                    m.Subject,
                    m.Message,
                    Отправлено = m.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    Ответ = string.IsNullOrEmpty(m.Response) ? "Ожидает ответа" : m.Response,
                    Дата_ответа = m.RespondedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-"
                }).ToList();

                MessagesGrid.ItemsSource = displayList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SubjectBox.Text))
            {
                MessageBox.Show("Введите тему сообщения");
                return;
            }

            if (string.IsNullOrWhiteSpace(MessageText.Text))
            {
                MessageBox.Show("Введите текст сообщения");
                return;
            }

            try
            {
                var success = await _apiService.SendMessageAsync(SubjectBox.Text, MessageText.Text);
                if (success)
                {
                    MessageBox.Show("Сообщение отправлено!");
                    SubjectBox.Text = "";
                    MessageText.Text = "";
                    await LoadMessages();
                }
                else
                {
                    MessageBox.Show("Ошибка отправки");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}