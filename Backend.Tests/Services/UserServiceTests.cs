using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Application.Services;
using Backend.Domain.Interfaces;
using Backend.Models;
using Moq;
using Xunit;

namespace Backend.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly Mock<IPasswordService> _passwordMock = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repositoryMock
            .Setup(r => r.UsernameExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.CiExistsAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        _passwordMock
            .Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns("HASH_SIMULADO");

        _service = new UserService(_repositoryMock.Object, _passwordMock.Object);
    }

    private static RegisterUserRequest CrearSolicitud(
        string? nombre = "  Ana  ",
        string? apellido = "  Perez  ",
        string? usuario = "  ana  ",
        string? clave = "Clave123",
        int ci = 12345678,
        string? complemento = "  LP  ",
        string? telefono = "  77777777  ")
    {
        return new RegisterUserRequest
        {
            FirstName = nombre!,
            LastName = apellido!,
            Username = usuario!,
            Password = clave!,
            Ci = ci,
            Complement = complemento,
            Phone = telefono
        };
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_NombreVacio_LanzaExcepcion(string? nombre)
    {
        var solicitud = CrearSolicitud(nombre: nombre);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(solicitud));

        Assert.Equal("El nombre es obligatorio.", error.Message);
        VerificarQueNoSeGuardo();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_ApellidoVacio_LanzaExcepcion(string? apellido)
    {
        var solicitud = CrearSolicitud(apellido: apellido);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(solicitud));

        Assert.Equal("El apellido es obligatorio.", error.Message);
        VerificarQueNoSeGuardo();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_UsuarioVacio_LanzaExcepcion(string? usuario)
    {
        var solicitud = CrearSolicitud(usuario: usuario);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(solicitud));

        Assert.Equal("El nombre de usuario es obligatorio.", error.Message);
        VerificarQueNoSeGuardo();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_ContrasenaVacia_LanzaExcepcion(string? clave)
    {
        var solicitud = CrearSolicitud(clave: clave);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(solicitud));

        Assert.Equal("La contraseña es obligatoria.", error.Message);
        VerificarQueNoSeGuardo();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RegisterAsync_CiNoValido_LanzaExcepcion(int ci)
    {
        var solicitud = CrearSolicitud(ci: ci);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(solicitud));

        Assert.Equal("El CI ingresado no es válido.", error.Message);
        VerificarQueNoSeGuardo();
    }

    [Fact]
    public async Task RegisterAsync_UsuarioDuplicado_LanzaExcepcionYNoConsultaCi()
    {
        _repositoryMock
            .Setup(r => r.UsernameExistsAsync("ana"))
            .ReturnsAsync(true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(CrearSolicitud()));

        Assert.Equal("El nombre de usuario ya está registrado.", error.Message);
        _repositoryMock.Verify(r => r.UsernameExistsAsync("ana"), Times.Once);
        _repositoryMock.Verify(
            r => r.CiExistsAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        VerificarQueNoSeGuardo();
    }

    [Fact]
    public async Task RegisterAsync_CiDuplicado_LanzaExcepcion()
    {
        _repositoryMock
            .Setup(r => r.CiExistsAsync(12345678, "LP"))
            .ReturnsAsync(true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(CrearSolicitud()));

        Assert.Equal("El CI ingresado ya está registrado.", error.Message);
        _repositoryMock.Verify(r => r.UsernameExistsAsync("ana"), Times.Once);
        _repositoryMock.Verify(r => r.CiExistsAsync(12345678, "LP"), Times.Once);
        VerificarQueNoSeGuardo();
    }

    [Fact]
    public async Task RegisterAsync_DatosValidos_GuardaUsuarioYDevuelveId()
    {
        User? usuarioGuardado = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u =>
            {
                usuarioGuardado = u;
                u.Id = 17; // Simula el ID asignado al guardar en la base de datos.
            });

        var antes = DateTime.Now;
        var id = await _service.RegisterAsync(CrearSolicitud());
        var despues = DateTime.Now;

        Assert.Equal(17, id);
        Assert.NotNull(usuarioGuardado);
        Assert.Equal(12345678, usuarioGuardado.Ci);
        Assert.Equal("LP", usuarioGuardado.Complement);
        Assert.Equal("Ana", usuarioGuardado.FirstName);
        Assert.Equal("Perez", usuarioGuardado.LastName);
        Assert.Equal("ana", usuarioGuardado.Username);
        Assert.Equal("77777777", usuarioGuardado.Phone);
        Assert.Equal("HASH_SIMULADO", usuarioGuardado.PasswordHash);
        Assert.Equal("User", usuarioGuardado.Role);
        Assert.Equal("Active", usuarioGuardado.Status);
        Assert.InRange(usuarioGuardado.CreatedAt, antes, despues);

        _passwordMock.Verify(p => p.HashPassword("Clave123"), Times.Once);
        _repositoryMock.Verify(r => r.UsernameExistsAsync("ana"), Times.Once);
        _repositoryMock.Verify(r => r.CiExistsAsync(12345678, "LP"), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task RegisterAsync_TelefonoNoInformado_GuardaNull(string? telefono)
    {
        User? usuarioGuardado = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u);

        await _service.RegisterAsync(CrearSolicitud(telefono: telefono));

        Assert.NotNull(usuarioGuardado);
        Assert.Null(usuarioGuardado.Phone);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ComplementoNull_ConsultaCiConCadenaVacia()
    {
        User? usuarioGuardado = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => usuarioGuardado = u);

        await _service.RegisterAsync(CrearSolicitud(complemento: null));

        _repositoryMock.Verify(r => r.CiExistsAsync(12345678, ""), Times.Once);
        Assert.NotNull(usuarioGuardado);
        Assert.Equal("", usuarioGuardado.Complement);
    }

    [Fact]
    public async Task RegisterAsync_ErrorAlGuardar_PropagaExcepcion()
    {
        _repositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new InvalidOperationException("Error de persistencia"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(CrearSolicitud()));

        Assert.Equal("Error de persistencia", error.Message);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ErrorAlHashear_NoGuardaUsuario()
    {
        _passwordMock
            .Setup(p => p.HashPassword("Clave123"))
            .Throws(new InvalidOperationException("Error de hash"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegisterAsync(CrearSolicitud()));

        Assert.Equal("Error de hash", error.Message);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetReadersAsync_SinLectores_DevuelveListaVacia()
    {
        _repositoryMock
            .Setup(r => r.GetReadersAsync())
            .ReturnsAsync(new List<User>());

        var resultado = (await _service.GetReadersAsync()).ToList();

        Assert.Empty(resultado);
        _repositoryMock.Verify(r => r.GetReadersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetReadersAsync_ConLectores_ProyectaCamposCorrectos()
    {
        _repositoryMock
            .Setup(r => r.GetReadersAsync())
            .ReturnsAsync(new List<User>
            {
                new() { Id = 1, FirstName = "Ana", LastName = "Perez",
                    Username = "ana", PasswordHash = "HASH_1" },
                new() { Id = 2, FirstName = "Luis", LastName = "Rojas",
                    Username = "luis", PasswordHash = "HASH_2" }
            });

        var resultado = (await _service.GetReadersAsync()).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Equal(1, resultado[0].Id);
        Assert.Equal("Ana", resultado[0].FirstName);
        Assert.Equal("Perez", resultado[0].LastName);
        Assert.Equal("ana", resultado[0].Username);
        Assert.Equal(2, resultado[1].Id);
        Assert.Equal("Luis", resultado[1].FirstName);
        Assert.Equal("Rojas", resultado[1].LastName);
        Assert.Equal("luis", resultado[1].Username);
        _repositoryMock.Verify(r => r.GetReadersAsync(), Times.Once);
    }

    private void VerificarQueNoSeGuardo()
    {
        _passwordMock.Verify(
            p => p.HashPassword(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<User>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
