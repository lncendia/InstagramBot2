namespace LoadProxy
{
    public class Proxy
    {
        public int Id { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int CountErrors { get; set; }
    }
}