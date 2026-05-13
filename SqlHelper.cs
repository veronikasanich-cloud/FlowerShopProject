using System;
using System.Data;
using System.Data.SqlClient;

namespace FlowerShopWPF
{
    public static class SqlHelper
    {
        // ВАЖНО: Замените на свою строку подключения!
        private static string connectionString =
            @"Server=DESKTOP-F61G585;Database=FlowerShopDB;Integrated Security=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        // Выполнение SELECT запросов
        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);

                        conn.Open();
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка выполнения запроса: " + ex.Message);
            }
            return dt;
        }

        // Выполнение INSERT, UPDATE, DELETE
        public static int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            int rowsAffected = 0;
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);

                        conn.Open();
                        rowsAffected = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка выполнения команды: " + ex.Message);
            }
            return rowsAffected;
        }

        // Получение одного значения (например, SCOPE_IDENTITY)
        public static object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            object result = null;
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        if (parameters != null)
                            cmd.Parameters.AddRange(parameters);

                        conn.Open();
                        result = cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка выполнения скалярного запроса: " + ex.Message);
            }
            return result;
        }
    }
}
