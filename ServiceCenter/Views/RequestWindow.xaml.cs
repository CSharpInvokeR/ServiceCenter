using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ServiceCenter.Models;
using ServiceCenter.Services;

namespace ServiceCenter.Views
{
    public partial class RequestWindow : Window
    {
        private DatabaseService _dbService;
        private Polzovatel _currentUser;
        private Zayavka _currentRequest;
        private List<Oborudovanie> _equipment;
        private List<TipNeispravnosti> _faultTypes;
        private List<Polzovatel> _employees;
        private List<Polzovatel> _clients;

        public RequestWindow(DatabaseService dbService, Polzovatel currentUser, Zayavka request = null)
        {
            InitializeComponent();
            _dbService = dbService;
            _currentUser = currentUser;
            _currentRequest = request;
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                _equipment = await _dbService.PoluchitVseOborudovanie();
                _faultTypes = await _dbService.PoluchitVseTipyNeispravnostey();
                _employees = await _dbService.PoluchitVsehSotrudnikov();
                _clients = await _dbService.PoluchitVsehKlientov();

                cmbEquipment.ItemsSource = null;
                cmbFaultType.ItemsSource = null;
                cmbClient.ItemsSource = null;
                cmbEmployee.ItemsSource = null;

                cmbEquipment.ItemsSource = _equipment;
                cmbEquipment.DisplayMemberPath = "Nazvanie";
                cmbEquipment.SelectedValuePath = "OborudovanieId";

                cmbFaultType.ItemsSource = _faultTypes;
                cmbFaultType.DisplayMemberPath = "NazvanieTipa";
                cmbFaultType.SelectedValuePath = "TipNeispravnostiId";

                cmbClient.ItemsSource = _clients;
                cmbClient.DisplayMemberPath = "PolnoeImya";
                cmbClient.SelectedValuePath = "PolzovatelId";

                var employeeItems = new List<dynamic>();
                employeeItems.Add(new { PolzovatelId = 0, PolnoeImya = "Не назначен" });
                foreach (var emp in _employees)
                {
                    employeeItems.Add(new { emp.PolzovatelId, emp.PolnoeImya });
                }
                cmbEmployee.ItemsSource = employeeItems;
                cmbEmployee.DisplayMemberPath = "PolnoeImya";
                cmbEmployee.SelectedValuePath = "PolzovatelId";

                if (_currentRequest != null)
                {
                    txtTitle.Text = $"Редактирование заявки №{_currentRequest.NomerZayavki}";
                    txtNumber.Text = _currentRequest.NomerZayavki;
                    txtDate.Text = _currentRequest.DataSozdaniya.ToString("dd.MM.yyyy HH:mm");
                    txtDescription.Text = _currentRequest.Opisanie;

                    cmbEquipment.SelectedValue = _currentRequest.OborudovanieId;
                    cmbFaultType.SelectedValue = _currentRequest.TipNeispravnostiId;
                    cmbClient.SelectedValue = _currentRequest.KlientId;

                    if (_currentRequest.NaznachenoKomu.HasValue && _currentRequest.NaznachenoKomu.Value > 0)
                    {
                        cmbEmployee.SelectedValue = _currentRequest.NaznachenoKomu.Value;
                    }
                    else
                    {
                        cmbEmployee.SelectedValue = 0;
                    }

                    foreach (ComboBoxItem item in cmbStatus.Items)
                    {
                        if (item.Content.ToString() == _currentRequest.Status)
                        {
                            cmbStatus.SelectedItem = item;
                            break;
                        }
                    }

                    await LoadComments();

                    if (_currentUser.Rol == "Клиент")
                    {
                        cmbStatus.IsEnabled = false;
                        cmbEmployee.IsEnabled = false;
                    }
                }
                else
                {
                    txtTitle.Text = "Новая заявка";
                    txtNumber.Text = "ЗАК-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    txtDate.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                    txtDescription.Text = "";
                    cmbStatus.SelectedIndex = 0;
                    cmbEmployee.SelectedValue = 0;

                    if (_currentUser.Rol == "Клиент")
                    {
                        cmbClient.SelectedValue = _currentUser.PolzovatelId;
                        cmbClient.IsEnabled = false;
                        cmbStatus.IsEnabled = false;
                        cmbEmployee.IsEnabled = false;
                    }

                    lvComments.Visibility = Visibility.Collapsed;
                }

                btnDelete.Visibility = (_currentUser.Rol == "Мастер" || _currentUser.Rol == "Менеджер")
                    ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadComments()
        {
            try
            {
                var comments = await _dbService.PoluchitKomentarii(_currentRequest.ZayavkaId);

                foreach (var comment in comments)
                {
                    comment.MozhnoUdolit = (_currentUser.Rol == "Мастер" ||
                                            _currentUser.Rol == "Менеджер" ||
                                            comment.PolzovatelId == _currentUser.PolzovatelId);

                    if (comment.TekstKomentariya != null && comment.TekstKomentariya.Contains("[QR:"))
                    {
                        comment.ImeetQRCode = true;

                        int qrIndex = comment.TekstKomentariya.IndexOf("[QR:");
                        if (qrIndex > 0)
                        {
                            comment.TekstDlyaOtobrazheniya = comment.TekstKomentariya.Substring(0, qrIndex).Trim();
                        }
                        else
                        {
                            comment.TekstDlyaOtobrazheniya = "✅ ЗАЯВКА ВЫПОЛНЕНА";
                        }

                        int startIndex = comment.TekstKomentariya.IndexOf("http");
                        if (startIndex > 0)
                        {
                            int endIndex = comment.TekstKomentariya.IndexOf("\n", startIndex);
                            if (endIndex == -1) endIndex = comment.TekstKomentariya.IndexOf("[QR:", startIndex);
                            if (endIndex == -1) endIndex = comment.TekstKomentariya.Length;

                            if (endIndex > startIndex)
                            {
                                comment.SsylkaFormi = comment.TekstKomentariya.Substring(startIndex, endIndex - startIndex).Trim();
                            }
                        }

                        string qrTag = "[QR:";
                        int qrStart = comment.TekstKomentariya.IndexOf(qrTag);
                        if (qrStart > 0)
                        {
                            int qrEnd = comment.TekstKomentariya.IndexOf("]", qrStart);
                            if (qrEnd > qrStart + qrTag.Length)
                            {
                                string base64 = comment.TekstKomentariya.Substring(qrStart + qrTag.Length,
                                                                                   qrEnd - (qrStart + qrTag.Length));
                                try
                                {
                                    byte[] imageBytes = Convert.FromBase64String(base64);
                                    BitmapImage bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.StreamSource = new System.IO.MemoryStream(imageBytes);
                                    bitmap.EndInit();
                                    comment.QRCodeImage = bitmap;
                                }
                                catch (FormatException)
                                {
                                    comment.ImeetQRCode = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        comment.ImeetQRCode = false;
                        comment.TekstDlyaOtobrazheniya = comment.TekstKomentariya ?? "";
                    }
                }

                lvComments.ItemsSource = comments;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки комментариев: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbEquipment.SelectedValue == null || (int)cmbEquipment.SelectedValue == 0)
                {
                    MessageBox.Show("Выберите оборудование", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbFaultType.SelectedValue == null || (int)cmbFaultType.SelectedValue == 0)
                {
                    MessageBox.Show("Выберите тип неисправности", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbClient.SelectedValue == null || (int)cmbClient.SelectedValue == 0)
                {
                    MessageBox.Show("Выберите клиента", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDescription.Text))
                {
                    MessageBox.Show("Введите описание проблемы", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var request = new Zayavka
                {
                    NomerZayavki = txtNumber.Text,
                    DataSozdaniya = DateTime.Parse(txtDate.Text),
                    OborudovanieId = (int)cmbEquipment.SelectedValue,
                    TipNeispravnostiId = (int)cmbFaultType.SelectedValue,
                    Opisanie = txtDescription.Text,
                    KlientId = (int)cmbClient.SelectedValue,
                    Sozdal = _currentRequest?.Sozdal ?? _currentUser.PolzovatelId
                };

                if (_currentRequest != null)
                {
                    request.ZayavkaId = _currentRequest.ZayavkaId;

                    if (_currentUser.Rol == "Клиент")
                    {
                        request.Status = _currentRequest.Status;
                        request.NaznachenoKomu = _currentRequest.NaznachenoKomu;
                    }
                    else
                    {
                        request.Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
                        if (cmbEmployee.SelectedValue != null && (int)cmbEmployee.SelectedValue > 0)
                        {
                            request.NaznachenoKomu = (int)cmbEmployee.SelectedValue;
                        }
                    }
                }
                else
                {
                    if (_currentUser.Rol == "Клиент")
                    {
                        request.Status = "В ожидании";
                        request.NaznachenoKomu = null;
                    }
                    else
                    {
                        request.Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
                        if (cmbEmployee.SelectedValue != null && (int)cmbEmployee.SelectedValue > 0)
                        {
                            request.NaznachenoKomu = (int)cmbEmployee.SelectedValue;
                        }
                    }
                }

                bool result;
                if (_currentRequest == null)
                {
                    result = await _dbService.DobavitZayavku(request);
                    if (result) MessageBox.Show("Заявка успешно добавлена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    result = await _dbService.ObnovitZayavku(request);
                    if (result) MessageBox.Show("Заявка успешно обновлена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (result)
                {
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRequest == null) return;

            var result = MessageBox.Show("Вы уверены, что хотите удалить заявку?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool deleted = await _dbService.UdalitZayavku(_currentRequest.ZayavkaId);
                    if (deleted)
                    {
                        MessageBox.Show("Заявка удалена", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        this.DialogResult = true;
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnAddComment_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRequest == null)
            {
                MessageBox.Show("Сначала сохраните заявку", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNewComment.Text))
            {
                MessageBox.Show("Введите текст комментария", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var comment = new Komentariy
                {
                    ZayavkaId = _currentRequest.ZayavkaId,
                    PolzovatelId = _currentUser.PolzovatelId,
                    TekstKomentariya = txtNewComment.Text
                };

                bool result = await _dbService.DobavitKomentariy(comment);
                if (result)
                {
                    txtNewComment.Text = "";
                    await LoadComments();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления комментария: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDeleteComment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null) return;

            int commentId = (int)button.Tag;

            var result = MessageBox.Show("Удалить комментарий?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool deleted = await _dbService.UdalitKomentariy(commentId);
                    if (deleted)
                    {
                        await LoadComments();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления комментария: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}