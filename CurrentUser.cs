namespace FlowerShopWPF
{
    public static class CurrentUser
    {
        public static int UserID { get; set; }
        public static string Login { get; set; }
        public static string Role { get; set; }
        public static int? EmployeeID { get; set; }
        public static int? CustomerID { get; set; }

        public static void Clear()
        {
            UserID = 0;
            Login = null;
            Role = null;
            EmployeeID = null;
            CustomerID = null;
        }
    }
}
