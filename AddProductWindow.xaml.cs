using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace FlowerShopWPF
{
    public partial class AddProductWindow : Window
    {
        public AddProductWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            LoadSuppliers();
        }

        private void LoadCategories()
        {
            try
            {
                DataTable dt = SqlHelper.ExecuteQuery("SELECT CategoryID, Name FROM Categories ORDER BY Name");
                cmbCategory.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка");
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                DataTable dt = SqlHelper.ExecuteQuery("SELECT SupplierID, Name FROM Suppliers ORDER BY Name");
                cmbSupplier.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки поставщиков: {ex.Message}", "Ошибка");
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text) || cmbCategory.SelectedValue == null ||
                string.IsNullOrEmpty(txtPrice.Text))
            {
                MessageBox.Show("Заполните все обязательные поля (Название, Категория, Цена)!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string query = @"INSERT INTO Products (Name, CategoryID, SupplierID, Price, Stock) 
                                VALUES (@Name, @CategoryID, @SupplierID, @Price, @Stock)";

                SqlParameter[] parameters = {
                    new SqlParameter("@Name", txtName.Text.Trim()),
                    new SqlParameter("@CategoryID", cmbCategory.SelectedValue),
                    new SqlParameter("@SupplierID", cmbSupplier.SelectedValue ?? (object)DBNull.Value),
                    new SqlParameter("@Price", Convert.ToDecimal(txtPrice.Text)),
                    new SqlParameter("@Stock", string.IsNullOrEmpty(txtStock.Text) ? 0 : Convert.ToInt32(txtStock.Text))
                };

                SqlHelper.ExecuteNonQuery(query, parameters);
                MessageBox.Show("Товар успешно добавлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (FormatException)
            {
                MessageBox.Show("Проверьте правильность введенных числовых значений!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления товара: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
