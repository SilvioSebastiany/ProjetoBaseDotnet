using System.Net;
using System.Net.Http.Json;
using BaseDotnet.Api.Responses;
using BaseDotnet.Api.Tests.Infrastructure;
using BaseDotnet.Domain.Commands.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace BaseDotnet.Api.Tests.Clientes
{
    public class ListarClientesEndpointTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly ApiWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ListarClientesEndpointTests(ApiWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListarAsync_ComClienteExistente_DeveRetornar200ComCliente()
        {
            var command = new CriarClienteCommand
            {
                Nome = "Cliente Listagem",
                Email = $"listagem-{Guid.NewGuid():N}@teste.com"
            };

            try
            {
                await _client.PostAsJsonAsync("/clientes", command);

                var resposta = await _client.GetAsync("/clientes");

                resposta.StatusCode.ShouldBe(HttpStatusCode.OK);

                var clientes = await resposta.Content.ReadFromJsonAsync<List<ClienteResponse>>();

                clientes.ShouldNotBeNull();
                clientes.ShouldContain(c => c.Email == command.Email);
            }
            finally
            {
                await RemoverClientePorEmailAsync(command.Email);
            }
        }

        private async Task RemoverClientePorEmailAsync(string email)
        {
            using var escopo = _factory.Services.CreateScope();
            var contexto = escopo.ServiceProvider
                .GetRequiredService<BaseDotnet.Infra.Data.DbContext.BaseDotnetContext>();

            var cliente = await contexto.Clientes.FirstOrDefaultAsync(c => c.Email == email);
            if (cliente is null)
                return;

            contexto.Clientes.Remove(cliente);
            await contexto.SaveChangesAsync();
        }
    }
}
