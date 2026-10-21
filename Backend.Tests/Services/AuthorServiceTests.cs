using Backend.Application.DTOs;
using Backend.Application.Services;
using Backend.Domain.Interfaces;
using Backend.Models;
using Moq;
using Xunit;

namespace Backend.Tests.Services;

public class AuthorServiceTests
{
    private readonly Mock<IAuthorRepository> _authorRepositoryMock;
    private readonly AuthorService _service;

    public AuthorServiceTests()
    {
        _authorRepositoryMock =
            new Mock<IAuthorRepository>();

        _service =
            new AuthorService(
                _authorRepositoryMock.Object);
    }

    // ============================================================
    // GET ALL ASYNC
    // V(G) = 1
    // ============================================================

    // Caso 1 - Camino: I,F
    [Fact]
    public async Task GetAllAsync_RepositoryReturnsAuthors_ReturnsSameAuthors()
    {
        // Arrange
        var authors = new List<Author>
        {
            new Author
            {
                Id = 1,
                FirstName = "Gabriel",
                LastName = "García",
                IsActive = true
            },
            new Author
            {
                Id = 2,
                FirstName = "Isabel",
                LastName = "Allende",
                IsActive = true
            }
        };

        _authorRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(authors);

        // Act
        var result =
            await _service.GetAllAsync();

        // Assert
        Assert.Same(authors, result);

        _authorRepositoryMock.Verify(
            r => r.GetAllAsync(),
            Times.Once);
    }


    // ============================================================
    // GET BY ID ASYNC
    // V(G) = 3
    // ============================================================

    // Caso 1 - Camino: I,1,3,F
    [Fact]
    public async Task GetByIdAsync_AuthorDoesNotExist_ReturnsNull()
    {
        // Arrange
        const uint id = 999;

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync((Author?)null);

        // Act
        var result =
            await _service.GetByIdAsync(id);

        // Assert
        Assert.Null(result);

        _authorRepositoryMock.Verify(
            r => r.GetByIdAsync(id),
            Times.Once);
    }


    // Caso 2 - Camino: I,1,2,3,F
    [Fact]
    public async Task GetByIdAsync_AuthorIsInactive_ReturnsNull()
    {
        // Arrange
        const uint id = 2;

        var author = new Author
        {
            Id = id,
            FirstName = "Gabriel",
            LastName = "García",
            IsActive = false
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        // Act
        var result =
            await _service.GetByIdAsync(id);

        // Assert
        Assert.Null(result);
    }


    // Caso 3 - Camino: I,1,2,4,F
    [Fact]
    public async Task GetByIdAsync_AuthorIsActive_ReturnsAuthor()
    {
        // Arrange
        const uint id = 1;

        var author = new Author
        {
            Id = id,
            FirstName = "Gabriel",
            LastName = "García",
            IsActive = true
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        // Act
        var result =
            await _service.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Same(author, result);
        Assert.Equal("Gabriel", result.FirstName);
        Assert.Equal("García", result.LastName);
    }


    // ============================================================
    // CREATE ASYNC
    // V(G) = 5
    // ============================================================

    // Caso 1 - Camino: I,1,2,F
    [Fact]
    public async Task CreateAsync_EmptyFirstName_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateAuthorDto
        {
            FirstName = "   ",
            LastName = "García",
            UserId = null
        };

        // Act
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(dto));

        // Assert
        Assert.Equal(
            "El nombre del autor es obligatorio.",
            exception.Message);

        _authorRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Author>()),
            Times.Never);
    }


    // Caso 2 - Camino: I,1,3,4,F
    [Fact]
    public async Task CreateAsync_EmptyLastName_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriel",
            LastName = "   ",
            UserId = null
        };

        // Act
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CreateAsync(dto));

        // Assert
        Assert.Equal(
            "El apellido del autor es obligatorio.",
            exception.Message);

        _authorRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Author>()),
            Times.Never);
    }


    // Caso 3 - Camino: I,1,3,5,6,7,F
    [Fact]
    public async Task CreateAsync_AuthorAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriel",
            LastName = "García",
            UserId = null
        };

        _authorRepositoryMock
            .Setup(r => r.ExistsAsync(
                "Gabriel",
                "García"))
            .ReturnsAsync(true);

        // Act
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(dto));

        // Assert
        Assert.Equal(
            "Ya existe un autor activo con ese nombre.",
            exception.Message);

        _authorRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Author>()),
            Times.Never);
    }


    // Caso 4 - Camino: I,1,3,5,6,8,9,11,F
    [Fact]
    public async Task CreateAsync_ValidDataWithUserId_CreatesAuthor()
    {
        // Arrange
        var dto = new CreateAuthorDto
        {
            FirstName = " Gabriel ",
            LastName = " García ",
            UserId = 5
        };

        _authorRepositoryMock
            .Setup(r => r.ExistsAsync(
                "Gabriel",
                "García"))
            .ReturnsAsync(false);

        // Act
        var result =
            await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Gabriel", result.FirstName);
        Assert.Equal("García", result.LastName);
        Assert.Equal((ushort)5, result.UserId);
        Assert.True(result.IsActive);
        Assert.NotEqual(default, result.CreatedAt);

        _authorRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Author>(a =>
                    a.FirstName == "Gabriel" &&
                    a.LastName == "García" &&
                    a.UserId == 5 &&
                    a.IsActive == true)),
            Times.Once);
    }


    // Caso 5 - Camino: I,1,3,5,6,8,10,11,F
    [Fact]
    public async Task CreateAsync_ValidDataWithoutUserId_CreatesAuthorWithNullUserId()
    {
        // Arrange
        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriel",
            LastName = "García",
            UserId = null
        };

        _authorRepositoryMock
            .Setup(r => r.ExistsAsync(
                "Gabriel",
                "García"))
            .ReturnsAsync(false);

        // Act
        var result =
            await _service.CreateAsync(dto);

        // Assert
        Assert.Equal("Gabriel", result.FirstName);
        Assert.Equal("García", result.LastName);
        Assert.Null(result.UserId);
        Assert.True(result.IsActive);

        _authorRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Author>(a =>
                    a.UserId == null)),
            Times.Once);
    }


    // ============================================================
    // UPDATE ASYNC
    // V(G) = 7
    // ============================================================

    // Caso 1 - Camino: I,1,3,F
    [Fact]
    public async Task UpdateAsync_AuthorDoesNotExist_ReturnsNull()
    {
        // Arrange
        const uint id = 999;

        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriel",
            LastName = "García"
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync((Author?)null);

        // Act
        var result =
            await _service.UpdateAsync(id, dto);

        // Assert
        Assert.Null(result);

        _authorRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Author>()),
            Times.Never);
    }


    // Caso 2 - Camino: I,1,2,3,F
    [Fact]
    public async Task UpdateAsync_AuthorIsInactive_ReturnsNull()
    {
        // Arrange
        const uint id = 2;

        var author = new Author
        {
            Id = id,
            FirstName = "Gabriel",
            LastName = "García",
            IsActive = false
        };

        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriel",
            LastName = "García"
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        // Act
        var result =
            await _service.UpdateAsync(id, dto);

        // Assert
        Assert.Null(result);

        _authorRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Author>()),
            Times.Never);
    }


    // Caso 3 - Camino: I,1,2,4,5,7,8,9,F
    [Fact]
    public async Task UpdateAsync_FirstNameChangedAndDuplicateExists_ThrowsException()
    {
        // Arrange
        const uint id = 1;

        var author = CreateActiveAuthor(id);

        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriela",
            LastName = "García"
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        _authorRepositoryMock
            .Setup(r => r.ExistsAsync(
                "Gabriela",
                "García"))
            .ReturnsAsync(true);

        // Act
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(id, dto));

        // Assert
        Assert.Equal(
            "Ya existe un autor activo con ese nombre.",
            exception.Message);

        _authorRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Author>()),
            Times.Never);
    }


    // Caso 4 - Camino: I,1,2,4,5,7,8,10,11,12,14,F
    [Fact]
    public async Task UpdateAsync_FirstNameChangedAndNoDuplicate_UpdatesAuthor()
    {
        // Arrange
        const uint id = 1;

        var author = CreateActiveAuthor(id);

        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriela",
            LastName = "García",
            UserId = 5
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        _authorRepositoryMock
            .Setup(r => r.ExistsAsync(
                "Gabriela",
                "García"))
            .ReturnsAsync(false);

        // Act
        var result =
            await _service.UpdateAsync(id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Gabriela", result.FirstName);
        Assert.Equal("García", result.LastName);
        Assert.Equal((ushort)5, result.UserId);
        Assert.NotNull(result.UpdatedAt);

        _authorRepositoryMock.Verify(
            r => r.UpdateAsync(author),
            Times.Once);
    }


    // Caso 5 - Camino: I,1,2,4,5,6,7,8,10,11,13,14,F
    [Fact]
    public async Task UpdateAsync_LastNameChangedAndNoDuplicate_UpdatesAuthor()
    {
        // Arrange
        const uint id = 1;

        var author = CreateActiveAuthor(id);

        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriel",
            LastName = "Márquez",
            UserId = null
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        _authorRepositoryMock
            .Setup(r => r.ExistsAsync(
                "Gabriel",
                "Márquez"))
            .ReturnsAsync(false);

        // Act
        var result =
            await _service.UpdateAsync(id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Gabriel", result.FirstName);
        Assert.Equal("Márquez", result.LastName);
        Assert.Null(result.UserId);

        _authorRepositoryMock.Verify(
            r => r.UpdateAsync(author),
            Times.Once);
    }


    // Caso 6 - Camino: I,1,2,4,5,6,10,11,12,14,F
    [Fact]
    public async Task UpdateAsync_NameDidNotChange_DoesNotCheckDuplicate()
    {
        // Arrange
        const uint id = 1;

        var author = CreateActiveAuthor(id);

        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriel",
            LastName = "García",
            UserId = 8
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        // Act
        var result =
            await _service.UpdateAsync(id, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal((ushort)8, result.UserId);

        _authorRepositoryMock.Verify(
            r => r.ExistsAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        _authorRepositoryMock.Verify(
            r => r.UpdateAsync(author),
            Times.Once);
    }


    // Caso 7 - Camino: I,1,2,4,5,6,7,8,9,F
    [Fact]
    public async Task UpdateAsync_LastNameChangedAndDuplicateExists_ThrowsException()
    {
        // Arrange
        const uint id = 1;

        var author = CreateActiveAuthor(id);

        var dto = new CreateAuthorDto
        {
            FirstName = "Gabriel",
            LastName = "Márquez"
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        _authorRepositoryMock
            .Setup(r => r.ExistsAsync(
                "Gabriel",
                "Márquez"))
            .ReturnsAsync(true);

        // Act
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(id, dto));

        // Assert
        Assert.Equal(
            "Ya existe un autor activo con ese nombre.",
            exception.Message);

        _authorRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Author>()),
            Times.Never);
    }


    // ============================================================
    // DELETE ASYNC
    // V(G) = 3
    // ============================================================

    // Caso 1 - Camino: I,1,3,F
    [Fact]
    public async Task DeleteAsync_AuthorDoesNotExist_ReturnsFalse()
    {
        // Arrange
        const uint id = 999;

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync((Author?)null);

        // Act
        var result =
            await _service.DeleteAsync(id);

        // Assert
        Assert.False(result);

        _authorRepositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<Author>()),
            Times.Never);
    }


    // Caso 2 - Camino: I,1,2,3,F
    [Fact]
    public async Task DeleteAsync_AuthorIsInactive_ReturnsFalse()
    {
        // Arrange
        const uint id = 2;

        var author = new Author
        {
            Id = id,
            FirstName = "Gabriel",
            LastName = "García",
            IsActive = false
        };

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        // Act
        var result =
            await _service.DeleteAsync(id);

        // Assert
        Assert.False(result);

        _authorRepositoryMock.Verify(
            r => r.DeleteAsync(author),
            Times.Never);
    }


    // Caso 3 - Camino: I,1,2,4,5,F
    [Fact]
    public async Task DeleteAsync_AuthorIsActive_DeletesAndReturnsTrue()
    {
        // Arrange
        const uint id = 1;

        var author = CreateActiveAuthor(id);

        _authorRepositoryMock
            .Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(author);

        // Act
        var result =
            await _service.DeleteAsync(id);

        // Assert
        Assert.True(result);

        _authorRepositoryMock.Verify(
            r => r.DeleteAsync(author),
            Times.Once);
    }


    // ============================================================
    // HELPERS
    // ============================================================

    private static Author CreateActiveAuthor(uint id)
    {
        return new Author
        {
            Id = id,
            FirstName = "Gabriel",
            LastName = "García",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
    }
}