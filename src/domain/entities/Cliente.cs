namespace BaseDotNet.Domain.Entities
{
    public class Cliente
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public string Email { get; private set; }
        public DateTime DataInclusao { get; private set; }

        protected Cliente() { }

        public Cliente(string nome, string email)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            Email = email;
            DataInclusao = DateTime.UtcNow;
        }
    }
}
