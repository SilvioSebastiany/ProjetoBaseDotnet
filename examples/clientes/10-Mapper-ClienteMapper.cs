// Código real, vivo em src/api/mappers/ClienteMapper.cs — este arquivo é cópia de leitura, não edite aqui.
using BaseDotnet.Api.Responses;
using BaseDotnet.Domain.Entities;

namespace BaseDotnet.Api.Mappers
{
    public static class ClienteMapper
    {
        public static ClienteResponse Map(Cliente cliente)
        {
            return new ClienteResponse
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                Email = cliente.Email,
                DataInclusao = cliente.DataInclusao
            };
        }
    }
}
