using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ServiceCenter.Models;
using ServiceCenter.Services;

namespace ServiceCenter.Views
{
    public partial class LoginWindow : Window
    {
        private DatabaseService _dbService;

        public LoginWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                txtError.Text = "Введите логин и пароль";
                return;
            }

            try
            {
                Polzovatel user = await _dbService.AvtorizovatPolzovatelya(login, password);

                if (user != null)
                {
                    MainWindow mainWindow = new MainWindow(user);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    txtError.Text = "Неверный логин или пароль";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных: " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtRegFullName.Text.Trim();
            string login = txtRegLogin.Text.Trim();
            string password = txtRegPassword.Password.Trim();
            string confirmPassword = txtRegConfirmPassword.Password.Trim();
            string phone = txtRegPhone.Text.Trim();
            string email = txtRegEmail.Text.Trim();

            if (string.IsNullOrEmpty(fullName))
            {
                txtRegError.Text = "Введите ФИО";
                return;
            }

            if (string.IsNullOrEmpty(login))
            {
                txtRegError.Text = "Введите логин";
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                txtRegError.Text = "Введите пароль";
                return;
            }

            if (password != confirmPassword)
            {
                txtRegError.Text = "Пароли не совпадают";
                return;
            }

            if (password.Length < 3)
            {
                txtRegError.Text = "Пароль должен содержать минимум 3 символа";
                return;
            }

            try
            {
                bool registered = await _dbService.ZaregistrirovatKlienta(fullName, login, password, phone, email);

                if (registered)
                {
                    MessageBox.Show("Регистрация успешно завершена! Теперь вы можете войти в систему.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    txtRegFullName.Text = "";
                    txtRegLogin.Text = "";
                    txtRegPassword.Password = "";
                    txtRegConfirmPassword.Password = "";
                    txtRegPhone.Text = "";
                    txtRegEmail.Text = "";
                    txtRegError.Text = "";

                    var tabControl = FindName("MainTabControl") as TabControl;
                    if (tabControl != null)
                    {
                        tabControl.SelectedIndex = 0;
                    }
                }
                else
                {
                    txtRegError.Text = "Ошибка при регистрации";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("уже существует"))
                {
                    txtRegError.Text = "Пользователь с таким логином уже существует";
                }
                else
                {
                    txtRegError.Text = "Ошибка регистрации: " + ex.Message;
                }
            }
        }
    }
}