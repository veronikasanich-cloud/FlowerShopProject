using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace FlowerShopWPF
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string query = "SELECT UserID, Login, Role, EmployeeID, CustomerID FROM Users WHERE Login = @Login AND Password = @Password";
                SqlParameter[] parameters = {
                    new SqlParameter("@Login", login),
                    new SqlParameter("@Password", password)
                };

                DataTable dt = SqlHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    // Сохраняем данные текущего пользователя
                    CurrentUser.UserID = Convert.ToInt32(row["UserID"]);
                    CurrentUser.Login = row["Login"].ToString();
                    CurrentUser.Role = row["Role"].ToString();
                    CurrentUser.EmployeeID = row["EmployeeID"] != DBNull.Value
                        ? Convert.ToInt32(row["EmployeeID"]) : (int?)null;
                    CurrentUser.CustomerID = row["CustomerID"] != DBNull.Value
                        ? Convert.ToInt32(row["CustomerID"]) : (int?)null;

                    // Открываем окно в зависимости от роли
                    Window nextWindow = null;
                    switch (CurrentUser.Role)
                    {
                        case "Admin":
                            nextWindow = new AdminWindow();
                            break;
                        case "Employee":
                            nextWindow = new SellerWindow();
                            break;
                        case "Customer":
                            nextWindow = new CustomerWindow();
                            break;
                        default:
                            MessageBox.Show("Неизвестная роль пользователя!", "Ошибка");
                            return;
                    }

                    nextWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }
    }
}
