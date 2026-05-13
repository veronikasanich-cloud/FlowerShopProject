using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace FlowerShopWPF
{
    public partial class AdminWindow : Window
    {
        public AdminWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtWelcome.Text = $"Добро пожаловать, {CurrentUser.Login}!";
            LoadProducts();
            LoadOrders();
        }

        // ============== УПРАВЛЕНИЕ ТОВАРАМИ ==============

        private void LoadProducts()
        {
            try
            {
                string query = @"
                    SELECT p.ProductID, p.Name, p.Price, p.Stock,
                           c.Name AS CategoryName, s.Name AS SupplierName
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    LEFT JOIN Suppliers s ON p.SupplierID = s.SupplierID
                    ORDER BY p.ProductID";

                DataTable dt = SqlHelper.ExecuteQuery(query);
                dgProducts.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            AddProductWindow addWindow = new AddProductWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadProducts();
                MessageBox.Show("Товар успешно добавлен в каталог!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар для удаления!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView row = (DataRowView)dgProducts.SelectedItem;
            int productID = Convert.ToInt32(row["ProductID"]);
            string productName = row["Name"].ToString();

            try
            {
                // Проверяем, используется ли товар в заказах
                string checkQuery = "SELECT COUNT(*) FROM OrderDetails WHERE ProductID = @ProductID";
                SqlParameter[] checkParams = {
                    new SqlParameter("@ProductID", productID)
                };

                object result = SqlHelper.ExecuteScalar(checkQuery, checkParams);
                int orderCount = Convert.ToInt32(result);

                // Формируем сообщение в зависимости от наличия заказов
                string message;
                MessageBoxImage icon;

                if (orderCount > 0)
                {
                    message = $"⚠ ВНИМАНИЕ! Товар '{productName}' используется в {orderCount} заказах.\n\n" +
                              $"При удалении товара будут автоматически удалены:\n" +
                              $"• Все связанные записи из таблицы заказов (OrderDetails)\n" +
                              $"• История продаж этого товара\n\n" +
                              $"Это действие НЕОБРАТИМО!\n\n" +
                              $"Вы уверены, что хотите продолжить?";
                    icon = MessageBoxImage.Warning;
                }
                else
                {
                    message = $"Вы уверены, что хотите удалить товар '{productName}'?\n\n" +
                              $"Это действие нельзя отменить.";
                    icon = MessageBoxImage.Question;
                }

                var confirmResult = MessageBox.Show(message, "Подтверждение удаления",
                    MessageBoxButton.YesNo, icon);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                // Удаляем товар (связанные записи удалятся автоматически благодаря CASCADE)
                string deleteQuery = "DELETE FROM Products WHERE ProductID = @ProductID";
                SqlParameter[] deleteParams = {
                    new SqlParameter("@ProductID", productID)
                };

                SqlHelper.ExecuteNonQuery(deleteQuery, deleteParams);

                MessageBox.Show($"✔ Товар '{productName}' успешно удален из системы!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadProducts();
                LoadOrders(); // Обновляем заказы, т.к. могли измениться
            }
            catch (SqlException sqlEx)
            {
                // Обработка специфичных SQL ошибок
                if (sqlEx.Number == 547) // Ошибка внешнего ключа
                {
                    MessageBox.Show(
                        $"❌ Невозможно удалить товар '{productName}'.\n\n" +
                        $"Причина: Товар связан с другими данными в системе.\n\n" +
                        $"Решение:\n" +
                        $"1. Выполните SQL-скрипт для настройки CASCADE DELETE\n" +
                        $"2. Или удалите сначала все связанные заказы\n\n" +
                        $"Техническая информация:\n{sqlEx.Message}",
                        "Ошибка удаления",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Ошибка базы данных: {sqlEx.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неожиданная ошибка при удалении товара:\n\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefreshProducts_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
            MessageBox.Show("Список товаров обновлен!", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ============== УПРАВЛЕНИЕ ЗАКАЗАМИ ==============

        private void LoadOrders()
        {
            try
            {
                string query = @"
                    SELECT o.OrderID, 
                           ISNULL(c.Name, 'Не указан') AS CustomerName, 
                           ISNULL(e.Name, 'Не указан') AS EmployeeName,
                           o.OrderDate, 
                           o.TotalAmount
                    FROM Orders o
                    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                    ORDER BY o.OrderDate DESC";

                DataTable dt = SqlHelper.ExecuteQuery(query);
                dgOrders.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
            MessageBox.Show("Список заказов обновлен!", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ============== ВЫХОД ИЗ СИСТЕМЫ ==============

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CurrentUser.Clear();
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
