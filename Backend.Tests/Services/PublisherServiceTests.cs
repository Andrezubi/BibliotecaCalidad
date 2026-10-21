using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Application.DTOs;
using Backend.Application.Services;
using Backend.Domain.Interfaces;
using Backend.Models;
using Moq;
using Xunit;

namespace Backend.Tests.Services;

public class PublisherServiceTests
{
    private readonly Mock<IPublisherRepository> _repositoryMock = new();
    private readonly PublisherService _service;

    public PublisherServiceTests()
    {
        _service = new PublisherService(_repositoryMock.Object);
    }

    private static CreatePublisherDto CrearDto(
        string nombre = "  Editorial Andina  ",
        bool incluirUsuario = true)
    {
        return new CreatePublisherDto
        {
            Name = nombre,
            UserId = incluirUsuario ? 7 : null
        };
    }

    private static Publisher CrearEditorial(
        uint id = 5,
        string nombre = "Editorial Andina")
    {
        return new Publisher
        {
            Id = id,
            Name = nombre
        };
    }

    // GET ALL

    [Fact]
    public async Task GetAllAsync_SinEditoriales_DevuelveColeccionVacia()
    {
        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Publisher>());

        var resultado = (await _service.GetAllAsync()).ToList();

        Assert.Empty(resultado);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ConEditoriales_DevuelveTodas()
    {
        var editoriales = new List<Publisher>
        {
            CrearEditorial(1, "Editorial Andina"),
            CrearEditorial(2, "Editorial Universitaria")
        };
        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(editoriales);

        var resultado = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Same(editoriales[0], resultado[0]);
        Assert.Same(editoriales[1], resultado[1]);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ErrorEnRepositorio_PropagaExcepcion()
    {
        _repositoryMock.Setup(r => r.GetAllAsync())
            .ThrowsAsync(new InvalidOperationException("Error al consultar editoriales"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetAllAsync());

        Assert.Equal("Error al consultar editoriales", error.Message);
    }

    // GET BY ID

    [Fact]
    public async Task GetByIdAsync_EditorialInexistente_DevuelveNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync((Publisher?)null);

        var resultado = await _service.GetByIdAsync(5u);

        Assert.Null(resultado);
        _repositoryMock.Verify(r => r.GetByIdAsync(5u), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_EditorialExistente_DevuelveEditorial()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);

        var resultado = await _service.GetByIdAsync(5u);

        Assert.Same(editorial, resultado);
        _repositoryMock.Verify(r => r.GetByIdAsync(5u), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ErrorEnRepositorio_PropagaExcepcion()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ThrowsAsync(new InvalidOperationException("Error al buscar editorial"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetByIdAsync(5u));

        Assert.Equal("Error al buscar editorial", error.Message);
    }

    // CREATE

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task CreateAsync_NombreVacio_LanzaArgumentException(string nombre)
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(CrearDto(nombre: nombre)));

        Assert.Equal("El nombre de la editorial es obligatorio.", error.Message);
        _repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Publisher>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NombreDuplicado_LanzaInvalidOperationException()
    {
        _repositoryMock.Setup(r => r.ExistsAsync("Editorial Andina"))
            .ReturnsAsync(true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(CrearDto()));

        Assert.Equal("Ya existe una editorial activa con ese nombre.", error.Message);
        _repositoryMock.Verify(r => r.ExistsAsync("Editorial Andina"), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Publisher>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DatosValidos_NormalizaYAgregaEditorial()
    {
        _repositoryMock.Setup(r => r.ExistsAsync("Editorial Andina"))
            .ReturnsAsync(false);
        var antes = DateTime.Now;

        var resultado = await _service.CreateAsync(CrearDto());

        var despues = DateTime.Now;
        Assert.Equal("Editorial Andina", resultado.Name);
        Assert.Equal((ushort?)7, resultado.UserId);
        Assert.True(resultado.CreatedAt >= antes && resultado.CreatedAt <= despues);
        _repositoryMock.Verify(r => r.ExistsAsync("Editorial Andina"), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<Publisher>(p => ReferenceEquals(p, resultado))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UserIdNull_GuardaUsuarioNull()
    {
        var resultado = await _service.CreateAsync(
            CrearDto(incluirUsuario: false));

        Assert.Null(resultado.UserId);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Publisher>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ConsultaDeDuplicadosFalla_PropagaExcepcion()
    {
        _repositoryMock.Setup(r => r.ExistsAsync("Editorial Andina"))
            .ThrowsAsync(new InvalidOperationException("Error al verificar nombre"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(CrearDto()));

        Assert.Equal("Error al verificar nombre", error.Message);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Publisher>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ErrorAlAgregar_PropagaExcepcion()
    {
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Publisher>()))
            .ThrowsAsync(new InvalidOperationException("Error al agregar"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(CrearDto()));

        Assert.Equal("Error al agregar", error.Message);
    }

    // UPDATE

    [Fact]
    public async Task UpdateAsync_EditorialInexistente_DevuelveNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync((Publisher?)null);

        var resultado = await _service.UpdateAsync(5u, CrearDto());

        Assert.Null(resultado);
        _repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Publisher>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MismoNombre_NoConsultaDuplicados()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);

        var resultado = await _service.UpdateAsync(5u, CrearDto());

        Assert.Same(editorial, resultado);
        Assert.Equal("Editorial Andina", editorial.Name);
        _repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(editorial), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NombreCambiadoYDuplicado_LanzaExcepcion()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);
        _repositoryMock.Setup(r => r.ExistsAsync("Editorial Nueva"))
            .ReturnsAsync(true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(5u, CrearDto("  Editorial Nueva  ")));

        Assert.Equal("Ya existe una editorial activa con ese nombre.", error.Message);
        Assert.Equal("Editorial Andina", editorial.Name);
        _repositoryMock.Verify(r => r.ExistsAsync("Editorial Nueva"), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Publisher>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NombreCambiadoYDisponible_ActualizaEditorial()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);
        _repositoryMock.Setup(r => r.ExistsAsync("Editorial Nueva"))
            .ReturnsAsync(false);
        var antes = DateTime.Now;

        var resultado = await _service.UpdateAsync(
            5u, CrearDto("  Editorial Nueva  "));

        var despues = DateTime.Now;
        Assert.Same(editorial, resultado);
        Assert.Equal("Editorial Nueva", editorial.Name);
        Assert.Equal((ushort?)7, editorial.UserId);
        Assert.True(editorial.UpdatedAt >= antes && editorial.UpdatedAt <= despues);
        _repositoryMock.Verify(r => r.ExistsAsync("Editorial Nueva"), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(editorial), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UserIdNull_AsignaNull()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);

        var resultado = await _service.UpdateAsync(
            5u, CrearDto(incluirUsuario: false));

        Assert.Same(editorial, resultado);
        Assert.Null(editorial.UserId);
        _repositoryMock.Verify(r => r.UpdateAsync(editorial), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NombreVacio_ActualmentePermiteActualizar()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);
        _repositoryMock.Setup(r => r.ExistsAsync(""))
            .ReturnsAsync(false);

        var resultado = await _service.UpdateAsync(5u, CrearDto("   "));

        Assert.Same(editorial, resultado);
        Assert.Equal("", editorial.Name);
        _repositoryMock.Verify(r => r.ExistsAsync(""), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(editorial), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ConsultaDeDuplicadosFalla_PropagaExcepcion()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);
        _repositoryMock.Setup(r => r.ExistsAsync("Editorial Nueva"))
            .ThrowsAsync(new InvalidOperationException("Error al verificar nombre"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(5u, CrearDto("Editorial Nueva")));

        Assert.Equal("Error al verificar nombre", error.Message);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Publisher>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ErrorAlActualizar_PropagaExcepcion()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Publisher>()))
            .ThrowsAsync(new InvalidOperationException("Error al actualizar"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(5u, CrearDto()));

        Assert.Equal("Error al actualizar", error.Message);
    }

    // DELETE

    [Fact]
    public async Task DeleteAsync_EditorialInexistente_DevuelveFalse()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync((Publisher?)null);

        var resultado = await _service.DeleteAsync(5u);

        Assert.False(resultado);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Publisher>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_EditorialExistente_LlamaRepositorioYDevuelveTrue()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);

        var resultado = await _service.DeleteAsync(5u);

        Assert.True(resultado);
        _repositoryMock.Verify(r => r.DeleteAsync(editorial), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ErrorAlEliminar_PropagaExcepcion()
    {
        var editorial = CrearEditorial();
        _repositoryMock.Setup(r => r.GetByIdAsync(5u))
            .ReturnsAsync(editorial);
        _repositoryMock.Setup(r => r.DeleteAsync(editorial))
            .ThrowsAsync(new InvalidOperationException("Error al eliminar"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(5u));

        Assert.Equal("Error al eliminar", error.Message);
    }
}
