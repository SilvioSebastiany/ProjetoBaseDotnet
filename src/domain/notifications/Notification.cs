namespace BaseDotNet.Domain.Notifications
{
    public class Notification
    {
        public string Chave { get; }
        public string Mensagem { get; }

        public Notification(string chave, string mensagem)
        {
            Chave = chave;
            Mensagem = mensagem;
        }
    }
}
