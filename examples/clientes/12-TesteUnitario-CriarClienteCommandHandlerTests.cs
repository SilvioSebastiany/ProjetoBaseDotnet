// Código real, vivo em tests/BaseDotnet.Domain.Tests/Clientes/CriarClienteCommandHandlerTests.cs — este arquivo é cópia de leitura, não edite aqui.
using AutoFixture;
using AutoFixture.AutoMoq;
using BaseDotnet.Domain.Commands.Handlers;
using BaseDotnet.Domain.Interfaces;
using BaseDotnet.Domain.Notifications;
using Moq;
using Shouldly;
using Xunit;

namespace BaseDotnet.Domain.Tests.Clientes
{
    public class CriarClienteCommandHandlerTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IClienteRepository> _clienteRepositoryMock;
        private readonly Mock<INotificationContext> _notificationContextMock;
        private readonly CriarClienteCommandHandler _handler;

        public CriarClienteCommandHandlerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _clienteRepositoryMock = _fixture.Freeze<Mock<IClienteRepository>>();
            _notificationContextMock = _fixture.Freeze<Mock<INotificationContext>>();
            _handler = _fixture.Create<CriarClienteCommandHandler>();
        }

        [Fact]
        public async Task Handle_QuandoDadosValidos_DeveCriarClienteERetornarResultado()
        {
            // Arrange
            var command = new CriarClienteCommand
            {
                Nome = "Joao Silva",
                Email = "joao@email.com"
            };

            _notificationContextMock
                .Setup(x => x.HasNotifications)
                .Returns(false);

            // Act
            var resultado = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultado.ShouldNotBeNull();
            resultado.Nome.ShouldBe(command.Nome);
            resultado.Email.ShouldBe(command.Email);
            resultado.Id.ShouldNotBe(Guid.Empty);

            _clienteRepositoryMock.Verify(
                x => x.AdicionarAsync(It.IsAny<Entities.Cliente>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_QuandoHaNotificacoes_DeveRetornarDefault()
        {
            // Arrange
            var command = _fixture.Create<CriarClienteCommand>();

            _notificationContextMock
                .Setup(x => x.HasNotifications)
                .Returns(true);

            // Act
            var resultado = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultado.ShouldBeNull();

            _clienteRepositoryMock.Verify(
                x => x.AdicionarAsync(It.IsAny<Entities.Cliente>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
