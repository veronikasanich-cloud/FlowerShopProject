using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace FlowerShopWPF
{
    public partial class CustomerWindow : Window
    {
        private List<CartItem> cart = new List<CartItem>();

        public CustomerWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtWelcome.Text = $"Привет, {CurrentUser.Login}!";
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                string query = @"
                    SELECT p.ProductID, p.Name, p.Price, p.Stock, c.Name AS CategoryName
                    FROM Products p
                    LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                    WHERE p.Stock > 0
                    ORDER BY p.Name";

                DataTable dt = SqlHelper.ExecuteQuery(query);
                dgProducts.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки каталога: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (dgProducts.SelectedItem == null)
            {
                MessageBox.Show("Выберите товар из каталога!", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество (больше 0)!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView row = (DataRowView)dgProducts.SelectedItem;
            int productID = Convert.ToInt32(row["ProductID"]);
            string productName = row["Name"].ToString();
            decimal price = Convert.ToDecimal(row["Price"]);
            int stock = Convert.ToInt32(row["Stock"]);

            // Проверяем текущее количество в корзине
            var existing = cart.FirstOrDefault(c => c.ProductID == productID);
            int currentInCart = existing != null ? existing.Quantity : 0;

            if (currentInCart + quantity > stock)
            {
                MessageBox.Show($"Недостаточно товара на складе! Доступно: {stock}, в корзине: {currentInCart}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductID = productID,
                    ProductName = productName,
                    Price = price,
                    Quantity = quantity
                });
            }

            UpdateCart();
            MessageBox.Show($"Товар '{productName}' добавлен в корзину!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Корзина уже пуста!", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Очистить корзину?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                cart.Clear();
                UpdateCart();
                MessageBox.Show("Корзина очищена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateCart()
        {
            lstCart.Items.Clear();
            decimal total = 0;

            foreach (var item in cart)
            {
                decimal itemTotal = item.Price * item.Quantity;
                lstCart.Items.Add($"{item.ProductName} × {item.Quantity} = ${itemTotal:F2}");
                total += itemTotal;
            }

            txtTotal.Text = $"Итого: ${total:F2}";
        }

        private void btnCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста! Добавьте товары перед оформлением заказа.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CurrentUser.CustomerID == null)
            {
                MessageBox.Show("Ошибка: ID клиента не найден!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            decimal totalAmount = cart.Sum(c => c.Price * c.Quantity);
            var result = MessageBox.Show($"Оформить заказ на сумму ${totalAmount:F2}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            SqlConnection conn = null;
            SqlTransaction transaction = null;

            try
            {
                conn = SqlHelper.GetConnection();
                conn.Open();
                transaction = conn.BeginTransaction();

                // 1. Создаем заказ
                string insertOrderQuery = @"
                    INSERT INTO Orders (CustomerID, EmployeeID, OrderDate, TotalAmount) 
                    VALUES (@CustomerID, NULL, GETDATE(), @TotalAmount);
                    SELECT SCOPE_IDENTITY();";

                SqlCommand orderCmd = new SqlCommand(insertOrderQuery, conn, transaction);
                orderCmd.Parameters.AddWithValue("@CustomerID", CurrentUser.CustomerID.Value);
                orderCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);

                int newOrderID = Convert.ToInt32(orderCmd.ExecuteScalar());

                // 2. Добавляем детали заказа и обновляем остатки
                foreach (var item in cart)
                {
                    // Добавляем детали заказа
                    string insertDetailQuery = @"
                        INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice) 
                        VALUES (@OrderID, @ProductID, @Quantity, @UnitPrice)";

                    SqlCommand detailCmd = new SqlCommand(insertDetailQuery, conn, transaction);
                    detailCmd.Parameters.AddWithValue("@OrderID", newOrderID);
                    detailCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                    detailCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    detailCmd.Parameters.AddWithValue("@UnitPrice", item.Price);
                    detailCmd.ExecuteNonQuery();

                    // Обновляем остатки
                    string updateStockQuery = @"
                        UPDATE Products 
                        SET Stock = Stock - @Quantity 
                        WHERE ProductID = @ProductID";

                    SqlCommand stockCmd = new SqlCommand(updateStockQuery, conn, transaction);
                    stockCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    stockCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                    stockCmd.ExecuteNonQuery();
                }

                transaction.Commit();

                MessageBox.Show($"✔ Заказ №{newOrderID} успешно оформлен!\n\nСумма: ${totalAmount:F2}\n\nСпасибо за покупку!",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                cart.Clear();
                UpdateCart();
                LoadProducts();
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    transaction.Rollback();

                MessageBox.Show($"Ошибка оформления заказа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count > 0)
            {
                var result = MessageBox.Show("В корзине есть товары. Вы уверены, что хотите выйти?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            CurrentUser.Clear();
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }

    // Вспомогательный класс для корзины
    public class CartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
