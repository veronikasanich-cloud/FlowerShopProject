using System;
using System.Data.SqlClient;
using System.Windows;

namespace FlowerShopWPF
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();
            string name = txtName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string email = txtEmail.Text.Trim();
            string address = txtAddress.Text.Trim();

            // Валидация
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Заполните обязательные поля: Логин, Пароль, ФИО!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SqlConnection conn = null;
            SqlTransaction transaction = null;

            try
            {
                conn = SqlHelper.GetConnection();
                conn.Open();
                transaction = conn.BeginTransaction();

                // 1. Проверяем, не занят ли логин
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Login = @Login";
                SqlCommand checkCmd = new SqlCommand(checkQuery, conn, transaction);
                checkCmd.Parameters.AddWithValue("@Login", login);
                int count = (int)checkCmd.ExecuteScalar();

                if (count > 0)
                {
                    MessageBox.Show("Логин уже занят! Выберите другой.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    transaction.Rollback();
                    return;
                }

                // 2. Добавляем клиента в таблицу Customers
                string insertCustomerQuery = @"
                    INSERT INTO Customers (Name, Phone, Email, Address) 
                    VALUES (@Name, @Phone, @Email, @Address);
                    SELECT SCOPE_IDENTITY();";

                SqlCommand insertCustomerCmd = new SqlCommand(insertCustomerQuery, conn, transaction);
                insertCustomerCmd.Parameters.AddWithValue("@Name", name);
                insertCustomerCmd.Parameters.AddWithValue("@Phone",
                    string.IsNullOrEmpty(phone) ? (object)DBNull.Value : phone);
                insertCustomerCmd.Parameters.AddWithValue("@Email",
                    string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                insertCustomerCmd.Parameters.AddWithValue("@Address",
                    string.IsNullOrEmpty(address) ? (object)DBNull.Value : address);

                int newCustomerID = Convert.ToInt32(insertCustomerCmd.ExecuteScalar());

                // 3. Добавляем пользователя в таблицу Users
                string insertUserQuery = @"
                    INSERT INTO Users (Login, Password, Role, CustomerID) 
                    VALUES (@Login, @Password, 'Customer', @CustomerID)";

                SqlCommand insertUserCmd = new SqlCommand(insertUserQuery, conn, transaction);
                insertUserCmd.Parameters.AddWithValue("@Login", login);
                insertUserCmd.Parameters.AddWithValue("@Password", password);
                insertUserCmd.Parameters.AddWithValue("@CustomerID", newCustomerID);
                insertUserCmd.ExecuteNonQuery();

                // Фиксируем транзакцию
                transaction.Commit();

                MessageBox.Show("Регистрация успешна! Теперь вы можете войти в систему.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    transaction.Rollback();

                MessageBox.Show($"Ошибка регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    conn.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
