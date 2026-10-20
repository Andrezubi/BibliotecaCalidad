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

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repositoryMock = new();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(_repositoryMock.Object);
    }

    private static CreateCategoryDto CrearDto(
        string nombre = "  Historia  ",
        string? descripcion = "  Libros de historia  ",
        bool incluirUsuario = true)
    {
        return new CreateCategoryDto
        {
            Name = nombre,
            Description = descripcion,
            UserId = incluirUsuario ? 7 : null
        };
    }

    private static Category CrearCategoria(
        byte id = 5,
        string nombre = "Historia",
        bool activa = true)
    {
        return new Category
        {
            Id = id,
            Name = nombre,
            IsActive = activa
        };
    }

    // GET ALL

    [Fact]
    public async Task GetAllAsync_SinCategorias_DevuelveColeccionVacia()
    {
        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Category>());

        var resultado = (await _service.GetAllAsync()).ToList();

        Assert.Empty(resultado);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ConCategorias_DevuelveTodas()
    {
        var categorias = new List<Category>
        {
            CrearCategoria(1, "Historia"),
            CrearCategoria(2, "Ciencia")
        };
        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(categorias);

        var resultado = (await _service.GetAllAsync()).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Same(categorias[0], resultado[0]);
        Assert.Same(categorias[1], resultado[1]);
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // GET BY ID

    [Fact]
    public async Task GetByIdAsync_CategoriaInexistente_DevuelveNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync((Category?)null);

        var resultado = await _service.GetByIdAsync(5);

        Assert.Null(resultado);
        _repositoryMock.Verify(r => r.GetByIdAsync((byte)5), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_CategoriaInactiva_DevuelveNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(CrearCategoria(activa: false));

        var resultado = await _service.GetByIdAsync(5);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task GetByIdAsync_CategoriaActiva_DevuelveCategoria()
    {
        var categoria = CrearCategoria();
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(categoria);

        var resultado = await _service.GetByIdAsync(5);

        Assert.Same(categoria, resultado);
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

        Assert.Equal("El nombre de la categoría es obligatorio.", error.Message);
        _repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NombreDuplicado_LanzaInvalidOperationException()
    {
        _repositoryMock.Setup(r => r.ExistsAsync("Historia"))
            .ReturnsAsync(true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(CrearDto()));

        Assert.Equal("Ya existe una categoría activa con ese nombre.", error.Message);
        _repositoryMock.Verify(r => r.ExistsAsync("Historia"), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DatosValidos_NormalizaYAgregaCategoria()
    {
        _repositoryMock.Setup(r => r.ExistsAsync("Historia"))
            .ReturnsAsync(false);
        var antes = DateTime.Now;

        var resultado = await _service.CreateAsync(CrearDto());

        var despues = DateTime.Now;
        Assert.Equal("Historia", resultado.Name);
        Assert.Equal("Libros de historia", resultado.Description);
        Assert.Equal((ushort?)7, resultado.UserId);
        Assert.True(resultado.IsActive == true);
        Assert.InRange(resultado.CreatedAt, antes, despues);
        _repositoryMock.Verify(r => r.ExistsAsync("Historia"), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<Category>(c => ReferenceEquals(c, resultado))), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_DescripcionNoInformada_GuardaNull(string? descripcion)
    {
        var resultado = await _service.CreateAsync(
            CrearDto(descripcion: descripcion));

        Assert.Null(resultado.Description);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UserIdNull_GuardaUsuarioNull()
    {
        var resultado = await _service.CreateAsync(
            CrearDto(incluirUsuario: false));

        Assert.Null(resultado.UserId);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ErrorEnRepositorio_PropagaExcepcion()
    {
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Category>()))
            .ThrowsAsync(new InvalidOperationException("Error al agregar"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(CrearDto()));

        Assert.Equal("Error al agregar", error.Message);
    }

    // UPDATE

    [Fact]
    public async Task UpdateAsync_CategoriaInexistente_DevuelveNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync((Category?)null);

        var resultado = await _service.UpdateAsync(5, CrearDto());

        Assert.Null(resultado);
        _repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CategoriaInactiva_DevuelveNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(CrearCategoria(activa: false));

        var resultado = await _service.UpdateAsync(5, CrearDto());

        Assert.Null(resultado);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MismoNombre_NoConsultaDuplicados()
    {
        var categoria = CrearCategoria(nombre: "Historia");
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(categoria);

        var resultado = await _service.UpdateAsync(5, CrearDto());

        Assert.Same(categoria, resultado);
        _repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(categoria), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NombreCambiadoYDuplicado_LanzaExcepcion()
    {
        var categoria = CrearCategoria(nombre: "Historia");
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(categoria);
        _repositoryMock.Setup(r => r.ExistsAsync("Ciencia"))
            .ReturnsAsync(true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(5, CrearDto(nombre: "  Ciencia  ")));

        Assert.Equal("Ya existe una categoría activa con ese nombre.", error.Message);
        Assert.Equal("Historia", categoria.Name);
        _repositoryMock.Verify(r => r.ExistsAsync("Ciencia"), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NombreCambiadoYDisponible_ActualizaCategoria()
    {
        var categoria = CrearCategoria(nombre: "Historia");
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(categoria);
        _repositoryMock.Setup(r => r.ExistsAsync("Ciencia"))
            .ReturnsAsync(false);
        var antes = DateTime.Now;

        var resultado = await _service.UpdateAsync(
            5, CrearDto(nombre: "  Ciencia  ", descripcion: "  Nuevos libros  "));

        var despues = DateTime.Now;
        Assert.Same(categoria, resultado);
        Assert.Equal("Ciencia", categoria.Name);
        Assert.Equal("Nuevos libros", categoria.Description);
        Assert.Equal((ushort?)7, categoria.UserId);
        Assert.True(categoria.UpdatedAt >= antes && categoria.UpdatedAt <= despues);
        _repositoryMock.Verify(r => r.ExistsAsync("Ciencia"), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(categoria), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_DescripcionNoInformada_AsignaNull(string? descripcion)
    {
        var categoria = CrearCategoria();
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(categoria);

        var resultado = await _service.UpdateAsync(
            5, CrearDto(descripcion: descripcion));

        Assert.Same(categoria, resultado);
        Assert.Null(categoria.Description);
        _repositoryMock.Verify(r => r.UpdateAsync(categoria), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UserIdNull_AsignaNull()
    {
        var categoria = CrearCategoria();
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(categoria);

        await _service.UpdateAsync(5, CrearDto(incluirUsuario: false));

        Assert.Null(categoria.UserId);
        _repositoryMock.Verify(r => r.UpdateAsync(categoria), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ErrorEnRepositorio_PropagaExcepcion()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(CrearCategoria());
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Category>()))
            .ThrowsAsync(new InvalidOperationException("Error al actualizar"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(5, CrearDto()));

        Assert.Equal("Error al actualizar", error.Message);
    }

    // DELETE

    [Fact]
    public async Task DeleteAsync_CategoriaInexistente_DevuelveFalse()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync((Category?)null);

        var resultado = await _service.DeleteAsync(5);

        Assert.False(resultado);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CategoriaInactiva_DevuelveFalse()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(CrearCategoria(activa: false));

        var resultado = await _service.DeleteAsync(5);

        Assert.False(resultado);
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CategoriaActiva_LlamaRepositorioYDevuelveTrue()
    {
        var categoria = CrearCategoria();
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(categoria);

        var resultado = await _service.DeleteAsync(5);

        Assert.True(resultado);
        _repositoryMock.Verify(r => r.DeleteAsync(categoria), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ErrorEnRepositorio_PropagaExcepcion()
    {
        var categoria = CrearCategoria();
        _repositoryMock.Setup(r => r.GetByIdAsync((byte)5))
            .ReturnsAsync(categoria);
        _repositoryMock.Setup(r => r.DeleteAsync(categoria))
            .ThrowsAsync(new InvalidOperationException("Error al eliminar"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(5));

        Assert.Equal("Error al eliminar", error.Message);
    }
}
