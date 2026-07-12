using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using QRCoder;
using ServiceCenter.Models;
using ServiceCenter.Services;

namespace ServiceCenter.Views
{
    public partial class MainWindow : Window
    {
        private DatabaseService _dbService;
        private Polzovatel _currentUser;
        private List<Zayavka> _allRequests;
        private Zayavka _selectedRequest;
        private int _lastNotificationRequestId = 0;

        public MainWindow(Polzovatel user)
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _currentUser = user;

            txtUserInfo.Text = $"{user.PolnoeImya} | {user.Rol}";

            if (user.Rol == "Мастер" || user.Rol == "Менеджер")
            {
                statsPanel.Visibility = Visibility.Visible;
                notificationPanel.Visibility = Visibility.Collapsed;
            }
            else if (user.Rol == "Клиент")
            {
                statsPanel.Visibility = Visibility.Collapsed;
                notificationPanel.Visibility = Visibility.Visible;
                btnAdd.Visibility = Visibility.Visible;
            }
            else if (user.Rol == "Сотрудник")
            {
                statsPanel.Visibility = Visibility.Collapsed;
                notificationPanel.Visibility = Visibility.Collapsed;
                btnAdd.Visibility = Visibility.Collapsed;
            }

            LoadRequests();
        }

        private async void LoadRequests()
        {
            try
            {
                _allRequests = await _dbService.PoluchitVseZayavki();

                if (_currentUser.Rol == "Клиент")
                {
                    _allRequests = _allRequests.Where(r => r.KlientId == _currentUser.PolzovatelId).ToList();

                    var newCompleted = _allRequests.Where(r => r.Status == "Выполнено" && r.ZayavkaId > _lastNotificationRequestId).ToList();
                    if (newCompleted.Any())
                    {
                        var completedRequest = newCompleted.OrderByDescending(r => r.ZayavkaId).First();
                        _lastNotificationRequestId = completedRequest.ZayavkaId;
                        ShowNotification(completedRequest);
                    }
                }
                else if (_currentUser.Rol == "Сотрудник")
                {
                    _allRequests = _allRequests.Where(r =>
                        r.NaznachenoKomu == _currentUser.PolzovatelId ||
                        r.NaznachenoKomu == null).ToList();
                }

                FilterRequests();

                if (statsPanel.Visibility == Visibility.Visible)
                {
                    await LoadStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ShowNotification(Zayavka request)
        {
            txtNotification.Text = $"Заявка №{request.NomerZayavki} выполнена!";

            string ssylka = ConfigHelper.GetFeedbackFormUrl();

            try
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(ssylka, QRCodeGenerator.ECCLevel.Q);
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeBytes = qrCode.GetGraphic(20);
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new System.IO.MemoryStream(qrCodeBytes);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        qrImage.Source = bitmap;
                        qrImage.Visibility = Visibility.Visible;
                    }
                }

                qrLink.Text = ssylka;
                qrLink.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка генерации QR: {ex.Message}");
                qrImage.Visibility = Visibility.Collapsed;
                qrLink.Visibility = Visibility.Collapsed;
            }

            notificationContent.Visibility = Visibility.Visible;
        }

        private void BtnCloseNotification_Click(object sender, RoutedEventArgs e)
        {
            notificationContent.Visibility = Visibility.Collapsed;
        }

        private async System.Threading.Tasks.Task LoadStatistics()
        {
            try
            {
                var stats = await _dbService.PoluchitStatistiku();

                txtTotal.Text = stats.TotalRequests.ToString();
                txtCompleted.Text = stats.CompletedRequests.ToString();
                txtInProgress.Text = stats.InProgressRequests.ToString();
                txtWaiting.Text = stats.WaitingRequests.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Статистика ошибка: {ex.Message}");
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterRequests();
        }

        private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterRequests();
        }

        private void FilterRequests()
        {
            if (_allRequests == null) return;

            var filtered = _allRequests.AsEnumerable();

            string searchText = txtSearch.Text.ToLower();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(r =>
                    (r.NomerZayavki?.ToLower().Contains(searchText) ?? false) ||
                    (r.NazvanieOborudovaniya?.ToLower().Contains(searchText) ?? false) ||
                    (r.Opisanie?.ToLower().Contains(searchText) ?? false) ||
                    (r.ImyaKlienta?.ToLower().Contains(searchText) ?? false));
            }

            if (cmbStatus.SelectedItem is ComboBoxItem item && item.Content.ToString() != "Все статусы")
            {
                filtered = filtered.Where(r => r.Status == item.Content.ToString());
            }

            dgRequests.ItemsSource = filtered.ToList();
        }

        private void DgRequests_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRequest = dgRequests.SelectedItem as Zayavka;

            if (_selectedRequest != null)
            {
                btnEdit.IsEnabled = true;
                btnDelete.IsEnabled = (_currentUser.Rol == "Мастер" || _currentUser.Rol == "Менеджер");
            }
            else
            {
                btnEdit.IsEnabled = false;
                btnDelete.IsEnabled = false;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var requestWindow = new RequestWindow(_dbService, _currentUser, null);
            requestWindow.ShowDialog();
            LoadRequests();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest != null)
            {
                var requestWindow = new RequestWindow(_dbService, _currentUser, _selectedRequest);
                requestWindow.ShowDialog();
                LoadRequests();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRequest == null) return;

            var result = MessageBox.Show($"Удалить заявку {_selectedRequest.NomerZayavki}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool deleted = await _dbService.UdalitZayavku(_selectedRequest.ZayavkaId);
                    if (deleted)
                    {
                        MessageBox.Show("Заявка удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadRequests();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void QrLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock != null && !string.IsNullOrEmpty(textBlock.Text))
            {
                System.Diagnostics.Process.Start(textBlock.Text);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}