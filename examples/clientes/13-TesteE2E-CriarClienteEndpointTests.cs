// Código real, vivo em tests/BaseDotnet.Api.Tests/Clientes/CriarClienteEndpointTests.cs — este arquivo é cópia de leitura, não edite aqui.
using System.Net;
using System.Net.Http.Json;
using BaseDotnet.Api.Tests.Infrastructure;
using BaseDotnet.Domain.Commands.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace BaseDotnet.Api.Tests.Clientes
{
    public class CriarClienteEndpointTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly ApiWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CriarClienteEndpointTests(ApiWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CriarAsync_ComDadosValidos_DeveRetornar201EPersistirCliente()
        {
            var command = new CriarClienteCommand
            {
                Nome = "Cliente E2E",
                Email = $"e2e-{Guid.NewGuid():N}@teste.com"
            };

            var resposta = await _client.PostAsJsonAsync("/clientes", command);

            try
            {
                resposta.StatusCode.ShouldBe(HttpStatusCode.Created);

                var resultado = await resposta.Content.ReadFromJsonAsync<CriarClienteCommandResult>();

                resultado.ShouldNotBeNull();
                resultado.Id.ShouldNotBe(Guid.Empty);
                resultado.Nome.ShouldBe(command.Nome);
                resultado.Email.ShouldBe(command.Email);
            }
            finally
            {
                await RemoverClientePorEmailAsync(command.Email);
            }
        }

        [Fact]
        public async Task CriarAsync_ComNomeVazio_DeveRetornar422ComNotificacao()
        {
            var command = new CriarClienteCommand
            {
                Nome = string.Empty,
                Email = $"e2e-{Guid.NewGuid():N}@teste.com"
            };

            var resposta = await _client.PostAsJsonAsync("/clientes", command);

            resposta.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

            var corpo = await resposta.Content.ReadAsStringAsync();
            corpo.ShouldContain("Nome");
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
