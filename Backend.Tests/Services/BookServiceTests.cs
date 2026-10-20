using Backend.Application.DTOs;
using Backend.Application.Services;
using Backend.Domain.Interfaces;
using Backend.Domain.Validators;
using Backend.Models;
using Backend.Infraestructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Reflection;
using Xunit;

namespace Backend.Tests.Services
{
    public class BookServiceTests
    {
        private readonly Mock<IBookRepository> _bookRepository = new();
        private readonly Mock<IAuthorRepository> _authorRepository = new();
        private readonly Mock<ICategoryRepository> _categoryRepository = new();
        private readonly Mock<IWebHostEnvironment> _environment = new();
        private readonly Mock<LibraryDbcontext> _context = new();
        private readonly BookService _sut;

        public BookServiceTests()
        {
            // Setup context for transactions
            var mockDatabase = new Mock<DatabaseFacade>(_context.Object);
            var mockTransaction = new Mock<IDbContextTransaction>();
            mockDatabase.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockTransaction.Object);
            _context.Setup(c => c.Database).Returns(mockDatabase.Object);
            
            _sut = new BookService(
                _bookRepository.Object,
                _authorRepository.Object,
                _categoryRepository.Object,
                _environment.Object,
                _context.Object);
        }

        #region ValidateCoverFile Tests
        public class ValidateCoverFileTests
        {
            private static void InvokeValidateCoverFile(IFormFile? file)
            {
                var method = typeof(BookService).GetMethod(
                    "ValidateCoverFile",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);
                method!.Invoke(null, new object?[] { file });
            }

            private static Mock<IFormFile> CreateMockFile(string fileName, long length, string contentType = "image/jpeg")
            {
                var mockFile = new Mock<IFormFile>();
                mockFile.Setup(f => f.FileName).Returns(fileName);
                mockFile.Setup(f => f.Length).Returns(length);
                mockFile.Setup(f => f.ContentType).Returns(contentType);
                return mockFile;
            }

            // Camino 1: I,1,2,F - file == null
            [Fact]
            public void Camino1_FileNull_ReturnsNormally()
            {
                var exception = Record.Exception(() => InvokeValidateCoverFile(null));
                exception.Should().BeNull();
            }

            // Camino 2: I,1,3,4,F - file.Length <= 0
            [Fact]
            public void Camino2_FileEmpty_ThrowsArgumentException()
            {
                var file = CreateMockFile("cover.jpg", 0, "image/jpeg");
                var exception = Assert.Throws<TargetInvocationException>(() => InvokeValidateCoverFile(file.Object));
                exception.InnerException.Should().BeOfType<ArgumentException>();
                exception.InnerException!.Message.Should().Be("La imagen de portada está vacía.");
            }

            // Camino 3: I,1,3,5,6,F - file.Length > MaxFileSize
            [Fact]
            public void Camino3_FileTooLarge_ThrowsArgumentException()
            {
                var file = CreateMockFile("cover.jpg", 5 * 1024 * 1024 + 1, "image/jpeg");
                var exception = Assert.Throws<TargetInvocationException>(() => InvokeValidateCoverFile(file.Object));
                exception.InnerException.Should().BeOfType<ArgumentException>();
                exception.InnerException!.Message.Should().Be("La imagen de portada no puede superar los 5 MB.");
            }

            // Camino 4: I,1,3,5,7,8,9,F - invalid extension
            [Fact]
            public void Camino4_InvalidExtension_ThrowsArgumentException()
            {
                var file = CreateMockFile("cover.gif", 1024, "image/gif");
                var exception = Assert.Throws<TargetInvocationException>(() => InvokeValidateCoverFile(file.Object));
                exception.InnerException.Should().BeOfType<ArgumentException>();
                exception.InnerException!.Message.Should().Be("El formato de la portada debe ser JPG, JPEG, PNG o WEBP.");
            }

            // Camino 5: I,1,3,5,7,8,10,11,12,F - invalid content type
            [Fact]
            public void Camino5_InvalidContentType_ThrowsArgumentException()
            {
                var file = CreateMockFile("cover.jpg", 1024, "application/pdf");
                var exception = Assert.Throws<TargetInvocationException>(() => InvokeValidateCoverFile(file.Object));
                exception.InnerException.Should().BeOfType<ArgumentException>();
                exception.InnerException!.Message.Should().Be("El tipo de archivo de la portada no es válido.");
            }

            // Camino 6: I,1,3,5,7,8,10,F - content type whitespace
            [Fact]
            public void Camino6_ContentTypeWhitespace_ReturnsNormally()
            {
                var file = CreateMockFile("cover.jpg", 1024, " ");
                var exception = Record.Exception(() => InvokeValidateCoverFile(file.Object));
                exception.Should().BeNull();
            }

            // Camino 7: I,1,3,5,7,8,10,11,F - valid file
            [Fact]
            public void Camino7_ValidFile_ReturnsNormally()
            {
                var file = CreateMockFile("cover.jpg", 1024, "image/jpeg");
                var exception = Record.Exception(() => InvokeValidateCoverFile(file.Object));
                exception.Should().BeNull();
            }
        }
        #endregion

        #region ValidateIsbnAsync Tests
        public class ValidateIsbnAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly BookService _sut;

            public ValidateIsbnAsyncTests()
            {
                var mockAuthorRepository = new Mock<IAuthorRepository>();
                var mockCategoryRepository = new Mock<ICategoryRepository>();
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    mockAuthorRepository.Object,
                    mockCategoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            private async Task InvokeValidateIsbnAsync(string? isbn)
            {
                var method = typeof(BookService).GetMethod(
                    "ValidateIsbnAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                var task = (Task)method!.Invoke(_sut, new object?[] { isbn })!;
                await task;
            }

            // Camino 1: isbn = null
            [Fact]
            public async Task Camino1_IsbnNull_ReturnsNormally()
            {
                await InvokeValidateIsbnAsync(null);
                _bookRepository.Verify(r => r.ExistsByIsbnAsync(It.IsAny<string>()), Times.Never);
            }

            // Camino 2: isbn valid, doesn't exist
            [Fact]
            public async Task Camino2_IsbnValid_DoesNotExist_ReturnsNormally()
            {
                _bookRepository.Setup(r => r.ExistsByIsbnAsync("9781234567890"))
                    .ReturnsAsync(false);
                
                await InvokeValidateIsbnAsync("9781234567890");
                
                _bookRepository.Verify(r => r.ExistsByIsbnAsync("9781234567890"), Times.Once);
            }

            // Camino 3: isbn exists
            [Fact]
            public async Task Camino3_IsbnExists_ThrowsInvalidOperationException()
            {
                _bookRepository.Setup(r => r.ExistsByIsbnAsync("9781234567890"))
                    .ReturnsAsync(true);
                
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => InvokeValidateIsbnAsync("9781234567890"));
                
                exception.Message.Should().Be("Ya existe un libro activo con ese ISBN.");
                _bookRepository.Verify(r => r.ExistsByIsbnAsync("9781234567890"), Times.Once);
            }
        }
        #endregion

        #region ValidateUpdateIsbnAsync Tests
        public class ValidateUpdateIsbnAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly BookService _sut;

            public ValidateUpdateIsbnAsyncTests()
            {
                var mockAuthorRepository = new Mock<IAuthorRepository>();
                var mockCategoryRepository = new Mock<ICategoryRepository>();
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    mockAuthorRepository.Object,
                    mockCategoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            private async Task InvokeValidateUpdateIsbnAsync(string? newIsbn, string? currentIsbn)
            {
                var method = typeof(BookService).GetMethod(
                    "ValidateUpdateIsbnAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                var task = (Task)method!.Invoke(_sut, new object?[] { newIsbn, currentIsbn })!;
                await task;
            }

            // Camino 1: newIsbn is null
            [Fact]
            public async Task Camino1_NewIsbnNull_ReturnsNormally()
            {
                await InvokeValidateUpdateIsbnAsync(null, "123");
                _bookRepository.Verify(r => r.ExistsByIsbnAsync(It.IsAny<string>()), Times.Never);
            }

            // Camino 2: newIsbn equals currentIsbn
            [Fact]
            public async Task Camino2_NewIsbnEqualsCurrent_ReturnsNormally()
            {
                await InvokeValidateUpdateIsbnAsync("123", "123");
                _bookRepository.Verify(r => r.ExistsByIsbnAsync(It.IsAny<string>()), Times.Never);
            }

            // Camino 3: newIsbn different, doesn't exist
            [Fact]
            public async Task Camino3_NewIsbnDifferent_DoesNotExist_ReturnsNormally()
            {
                _bookRepository.Setup(r => r.ExistsByIsbnAsync("456"))
                    .ReturnsAsync(false);
                
                await InvokeValidateUpdateIsbnAsync("456", "123");
                
                _bookRepository.Verify(r => r.ExistsByIsbnAsync("456"), Times.Once);
            }

            // Camino 4: newIsbn different, exists
            [Fact]
            public async Task Camino4_NewIsbnDifferent_Exists_ThrowsInvalidOperationException()
            {
                _bookRepository.Setup(r => r.ExistsByIsbnAsync("456"))
                    .ReturnsAsync(true);
                
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => InvokeValidateUpdateIsbnAsync("456", "123"));
                
                exception.Message.Should().Be("Ya existe un libro activo con ese ISBN.");
                _bookRepository.Verify(r => r.ExistsByIsbnAsync("456"), Times.Once);
            }
        }
        #endregion

        #region ValidateAuthorsAsync Tests
        public class ValidateAuthorsAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly BookService _sut;

            public ValidateAuthorsAsyncTests()
            {
                var mockCategoryRepository = new Mock<ICategoryRepository>();
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    mockCategoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            private async Task InvokeValidateAuthorsAsync(IEnumerable<uint> authorIds)
            {
                var method = typeof(BookService).GetMethod(
                    "ValidateAuthorsAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                var task = (Task)method!.Invoke(_sut, new object[] { authorIds })!;
                await task;
            }

            // Camino 1: empty list
            [Fact]
            public async Task Camino1_EmptyList_CompletesNormally()
            {
                await InvokeValidateAuthorsAsync(new List<uint>());
                _authorRepository.Verify(r => r.ExistsActiveByIdAsync(It.IsAny<uint>()), Times.Never);
            }

            // Camino 2: author exists and is active
            [Fact]
            public async Task Camino2_AuthorExistsAndActive_CompletesNormally()
            {
                _authorRepository.Setup(r => r.ExistsActiveByIdAsync(10))
                    .ReturnsAsync(true);
                
                await InvokeValidateAuthorsAsync(new List<uint> { 10 });
                
                _authorRepository.Verify(r => r.ExistsActiveByIdAsync(10), Times.Once);
            }

            // Camino 3: author does not exist or is inactive
            [Fact]
            public async Task Camino3_AuthorDoesNotExist_ThrowsInvalidOperationException()
            {
                _authorRepository.Setup(r => r.ExistsActiveByIdAsync(10))
                    .ReturnsAsync(false);
                
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => InvokeValidateAuthorsAsync(new List<uint> { 10 }));
                
                exception.Message.Should().Be("El autor con Id 10 no existe o está inactivo.");
                _authorRepository.Verify(r => r.ExistsActiveByIdAsync(10), Times.Once);
            }
        }
        #endregion

        #region ValidateCategoriesAsync Tests
        public class ValidateCategoriesAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly BookService _sut;

            public ValidateCategoriesAsyncTests()
            {
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            private async Task InvokeValidateCategoriesAsync(IEnumerable<byte> categoryIds)
            {
                var method = typeof(BookService).GetMethod(
                    "ValidateCategoriesAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                var task = (Task)method!.Invoke(_sut, new object[] { categoryIds })!;
                await task;
            }

            // Camino 1: empty list
            [Fact]
            public async Task Camino1_EmptyList_CompletesNormally()
            {
                await InvokeValidateCategoriesAsync(new List<byte>());
                _categoryRepository.Verify(r => r.ExistsActiveByIdAsync(It.IsAny<byte>()), Times.Never);
            }

            // Camino 2: category exists and is active
            [Fact]
            public async Task Camino2_CategoryExistsAndActive_CompletesNormally()
            {
                _categoryRepository.Setup(r => r.ExistsActiveByIdAsync((byte)2))
                    .ReturnsAsync(true);
                
                await InvokeValidateCategoriesAsync(new List<byte> { 2 });
                
                _categoryRepository.Verify(r => r.ExistsActiveByIdAsync((byte)2), Times.Once);
            }

            // Camino 3: category does not exist or is inactive
            [Fact]
            public async Task Camino3_CategoryDoesNotExist_ThrowsInvalidOperationException()
            {
                _categoryRepository.Setup(r => r.ExistsActiveByIdAsync((byte)2))
                    .ReturnsAsync(false);
                
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => InvokeValidateCategoriesAsync(new List<byte> { 2 }));
                
                exception.Message.Should().Be("La categoría con Id 2 no existe o está inactiva.");
                _categoryRepository.Verify(r => r.ExistsActiveByIdAsync((byte)2), Times.Once);
            }
        }
        #endregion

        #region GetByIdAsync Tests
        public class GetByIdAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly BookService _sut;

            public GetByIdAsyncTests()
            {
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            // Camino 1: book not found
            [Fact]
            public async Task Camino1_BookNotFound_ReturnsNull()
            {
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync((Book?)null);
                
                var result = await _sut.GetByIdAsync(1);
                
                result.Should().BeNull();
                _bookRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
            }

            // Camino 2: book exists but is inactive
            [Fact]
            public async Task Camino2_BookInactive_ReturnsNull()
            {
                var book = new Book { Id = 1, Title = "Test", IsActive = false };
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(book);
                
                var result = await _sut.GetByIdAsync(1);
                
                result.Should().BeNull();
                _bookRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
            }

            // Camino 3: book exists and is active
            [Fact]
            public async Task Camino3_BookActive_ReturnsDto()
            {
                var book = new Book { Id = 1, Title = "Test Book", IsActive = true, Isbn = "123" };
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(book);
                _bookRepository.Setup(r => r.GetAuthorIdsAsync(1))
                    .ReturnsAsync(new List<uint>());
                _bookRepository.Setup(r => r.GetCategoryIdsAsync(1))
                    .ReturnsAsync(new List<byte>());
                
                var result = await _sut.GetByIdAsync(1);
                
                result.Should().NotBeNull();
                result!.Id.Should().Be(1);
                result.Title.Should().Be("Test Book");
            }
        }
        #endregion

        #region SearchAsync Tests
        public class SearchAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly BookService _sut;

            public SearchAsyncTests()
            {
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            // Camino 1: empty collection
            [Fact]
            public async Task Camino1_EmptyResult_ReturnsEmptyCollection()
            {
                _bookRepository.Setup(r => r.SearchAsync(null))
                    .ReturnsAsync(new List<Book>());
                
                var result = await _sut.SearchAsync(null);
                
                result.Should().BeEmpty();
            }

            // Camino 2: one book
            [Fact]
            public async Task Camino2_OneBook_ReturnsCollectionWithOneDto()
            {
                var books = new List<Book> { new Book { Id = 1, Title = "Test", IsActive = true } };
                _bookRepository.Setup(r => r.SearchAsync(null))
                    .ReturnsAsync(books);
                _bookRepository.Setup(r => r.GetAuthorIdsAsync(1))
                    .ReturnsAsync(new List<uint>());
                _bookRepository.Setup(r => r.GetCategoryIdsAsync(1))
                    .ReturnsAsync(new List<byte>());
                
                var result = await _sut.SearchAsync(null);
                
                result.Should().HaveCount(1);
            }
        }
        #endregion

        #region GetAllAsync Tests
        public class GetAllAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly BookService _sut;

            public GetAllAsyncTests()
            {
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            // Camino 1: collection of books
            [Fact]
            public async Task Camino1_BooksReturned_ReturnsMappedCollection()
            {
                var books = new List<Book> { new Book { Id = 1, Title = "Test", IsActive = true } };
                _bookRepository.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(books);
                _bookRepository.Setup(r => r.GetAuthorIdsAsync(1))
                    .ReturnsAsync(new List<uint>());
                _bookRepository.Setup(r => r.GetCategoryIdsAsync(1))
                    .ReturnsAsync(new List<byte>());
                _bookRepository.Setup(r => r.CountAvailableCopiesAsync(1))
                    .ReturnsAsync(2);
                
                var result = await _sut.GetAllAsync();
                
                result.Should().NotBeNull();
                result.Should().HaveCount(1);
            }
        }
        #endregion

        #region GetAvailableAsync Tests
        public class GetAvailableAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly BookService _sut;

            public GetAvailableAsyncTests()
            {
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            // Camino 1: available books
            [Fact]
            public async Task Camino1_AvailableBooks_ReturnsMappedCollection()
            {
                var books = new List<Book> { new Book { Id = 1, Title = "Test", IsActive = true } };
                _bookRepository.Setup(r => r.GetAvailableAsync())
                    .ReturnsAsync(books);
                _bookRepository.Setup(r => r.GetAuthorIdsAsync(1))
                    .ReturnsAsync(new List<uint>());
                _bookRepository.Setup(r => r.GetCategoryIdsAsync(1))
                    .ReturnsAsync(new List<byte>());
                _bookRepository.Setup(r => r.CountAvailableCopiesAsync(1))
                    .ReturnsAsync(3);
                
                var result = await _sut.GetAvailableAsync();
                
                result.Should().NotBeNull();
                result.Should().HaveCount(1);
            }
        }
        #endregion

        #region AddCopyAsync Tests
        public class AddCopyAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly BookService _sut;

            public AddCopyAsyncTests()
            {
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            // Camino 1: missing or whitespace-only internal code
            [Fact]
            public async Task Camino1_WhitespaceInternalCode_ReturnsRequiredError()
            {
                var result = await _sut.AddCopyAsync(1, "   ");
                result.Should().Be("El código interno es obligatorio.");
            }

            // Camino 2: internal code too long
            [Fact]
            public async Task Camino2_InternalCodeTooLong_ReturnsLengthError()
            {
                var longCode = new string('A', 51);
                var result = await _sut.AddCopyAsync(1, longCode);
                result.Should().Be("El código interno no puede superar los 50 caracteres.");
            }

            // Camino 3: book not found
            [Fact]
            public async Task Camino3_BookNotFound_ReturnsNotExistsError()
            {
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync((Book?)null);
                
                var result = await _sut.AddCopyAsync(1, "COPY-001");
                result.Should().Be("El libro no existe.");
            }

            // Camino 4: book inactive
            [Fact]
            public async Task Camino4_BookInactive_ReturnsNotExistsError()
            {
                var book = new Book { Id = 1, IsActive = false };
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(book);
                
                var result = await _sut.AddCopyAsync(1, "COPY-001");
                result.Should().Be("El libro no existe.");
            }

            // Camino 5: duplicate internal code
            [Fact]
            public async Task Camino5_DuplicateInternalCode_ReturnsDuplicateError()
            {
                var book = new Book { Id = 1, IsActive = true };
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(book);
                _bookRepository.Setup(r => r.InternalCodeExistsAsync("COPY-001"))
                    .ReturnsAsync(true);
                
                var result = await _sut.AddCopyAsync(1, "COPY-001");
                result.Should().Be("Ya existe una copia con ese código interno.");
            }

            // Camino 6: valid code trimmed and persisted
            [Fact]
            public async Task Camino6_ValidCode_ReturnsNullAndPersists()
            {
                var book = new Book { Id = 1, IsActive = true };
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(book);
                _bookRepository.Setup(r => r.InternalCodeExistsAsync("COPY-001"))
                    .ReturnsAsync(false);
                
                var result = await _sut.AddCopyAsync(1, " COPY-001 ");
                result.Should().BeNull();
                _bookRepository.Verify(r => r.AddCopyAsync(It.IsAny<Copy>()), Times.Once);
            }
        }
        #endregion

        #region DeleteAsync Tests
        public class DeleteAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly BookService _sut;

            public DeleteAsyncTests()
            {
                var mockEnvironment = new Mock<IWebHostEnvironment>();
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    mockEnvironment.Object,
                    mockContext.Object);
            }

            // Camino 1: book not found
            [Fact]
            public async Task Camino1_BookNotFound_ReturnsFalse()
            {
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync((Book?)null);
                
                var result = await _sut.DeleteAsync(1);
                result.Should().BeFalse();
            }

            // Camino 2: book inactive
            [Fact]
            public async Task Camino2_BookInactive_ReturnsFalse()
            {
                var book = new Book { Id = 1, IsActive = false };
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(book);
                
                var result = await _sut.DeleteAsync(1);
                result.Should().BeFalse();
            }

            // Camino 3: book active - delete successfully
            [Fact]
            public async Task Camino3_BookActive_ReturnsTrue()
            {
                var book = new Book { Id = 1, IsActive = true };
                _bookRepository.Setup(r => r.GetByIdAsync(1))
                    .ReturnsAsync(book);
                
                var result = await _sut.DeleteAsync(1);
                result.Should().BeTrue();
                _bookRepository.Verify(r => r.DeleteAsync(book), Times.Once);
            }
        }
        #endregion

        #region SaveCoverAsync Tests
        public class SaveCoverAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly Mock<IWebHostEnvironment> _environment = new();
            private readonly BookService _sut;

            public SaveCoverAsyncTests()
            {
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                _environment.Setup(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "wwwroot"));
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    _environment.Object,
                    mockContext.Object);
            }

            private async Task<string?> InvokeSaveCoverAsync(IFormFile? file)
            {
                var method = typeof(BookService).GetMethod(
                    "SaveCoverAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                var task = (Task<string?>)method!.Invoke(_sut, new object?[] { file })!;
                return await task;
            }

            // Camino 1: file is null
            [Fact]
            public async Task Camino1_FileNull_ReturnsNull()
            {
                var result = await InvokeSaveCoverAsync(null);
                result.Should().BeNull();
            }

            // Camino 2: file length is 0
            [Fact]
            public async Task Camino2_FileEmpty_ReturnsNull()
            {
                var mockFile = new Mock<IFormFile>();
                mockFile.Setup(f => f.Length).Returns(0);
                mockFile.Setup(f => f.FileName).Returns("test.jpg");
                
                var result = await InvokeSaveCoverAsync(mockFile.Object);
                result.Should().BeNull();
            }

            // Camino 3: valid file
            [Fact]
            public async Task Camino3_ValidFile_ReturnsFileUrl()
            {
                var mockFile = new Mock<IFormFile>();
                mockFile.Setup(f => f.Length).Returns(1024);
                mockFile.Setup(f => f.FileName).Returns("cover.JPG");
                mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                
                var result = await InvokeSaveCoverAsync(mockFile.Object);
                result.Should().StartWith("/uploads/books/");
                result.Should().EndWith(".jpg");
            }
        }
        #endregion

        #region DeleteCoverImage Tests
        public class DeleteCoverImageTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly Mock<IWebHostEnvironment> _environment = new();
            private readonly BookService _sut;

            public DeleteCoverImageTests()
            {
                var mockContext = new Mock<LibraryDbcontext>();
                var mockDatabase = new Mock<DatabaseFacade>(mockContext.Object);
                mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);
                _environment.Setup(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "wwwroot"));
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    _environment.Object,
                    mockContext.Object);
            }

            private void InvokeDeleteCoverImage(string? coverImage, string? exceptPath = null)
            {
                var method = typeof(BookService).GetMethod(
                    "DeleteCoverImage",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                method!.Invoke(_sut, new object?[] { coverImage, exceptPath });
            }

            // Camino 1: coverImage null
            [Fact]
            public void Camino1_CoverImageNull_ReturnsNormally()
            {
                var exception = Record.Exception(() => InvokeDeleteCoverImage(null));
                exception.Should().BeNull();
            }

            // Camino 2: coverImage equals exceptPath ignoring case
            [Fact]
            public void Camino2_PathsEqual_ReturnsNormally()
            {
                var exception = Record.Exception(() => InvokeDeleteCoverImage("/uploads/books/cover.jpg", "/UPLOADS/BOOKS/COVER.JPG"));
                exception.Should().BeNull();
            }

            // Camino 3: file name is empty
            [Fact]
            public void Camino3_FileNameEmpty_ReturnsNormally()
            {
                var exception = Record.Exception(() => InvokeDeleteCoverImage("/"));
                exception.Should().BeNull();
            }

            // Camino 4: file does not exist
            [Fact]
            public void Camino4_FileDoesNotExist_ReturnsNormally()
            {
                var exception = Record.Exception(() => InvokeDeleteCoverImage("/uploads/books/nonexistent.jpg"));
                exception.Should().BeNull();
            }

            // Camino 5: file exists - delete
            [Fact]
            public void Camino5_FileExists_DeletesFile()
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "wwwroot", "uploads", "books");
                Directory.CreateDirectory(tempPath);
                var filePath = Path.Combine(tempPath, "testcover.jpg");
                File.WriteAllText(filePath, "test");
                
                try
                {
                    var exception = Record.Exception(() => InvokeDeleteCoverImage("/uploads/books/testcover.jpg"));
                    exception.Should().BeNull();
                    File.Exists(filePath).Should().BeFalse();
                }
                finally
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
            }
        }
        #endregion

        #region UpdateBookTransactionAsync Tests
        public class UpdateBookTransactionAsyncTests
        {
            private readonly Mock<IBookRepository> _bookRepository = new();
            private readonly Mock<IAuthorRepository> _authorRepository = new();
            private readonly Mock<ICategoryRepository> _categoryRepository = new();
            private readonly Mock<IWebHostEnvironment> _environment = new();
            private readonly Mock<LibraryDbcontext> _context = new();
            private readonly BookService _sut;

            public UpdateBookTransactionAsyncTests()
            {
                var mockDatabase = new Mock<DatabaseFacade>(_context.Object);
                var mockTransaction = new Mock<IDbContextTransaction>();
                mockDatabase.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockTransaction.Object);
                _context.Setup(c => c.Database).Returns(mockDatabase.Object);
                _environment.Setup(e => e.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "wwwroot"));
                
                _sut = new BookService(
                    _bookRepository.Object,
                    _authorRepository.Object,
                    _categoryRepository.Object,
                    _environment.Object,
                    _context.Object);
            }

            private async Task<string?> InvokeUpdateBookTransactionAsync(Book book, UpdateBookDto dto, IEnumerable<uint> authorIds, IEnumerable<byte> categoryIds)
            {
                var method = typeof(BookService).GetMethod(
                    "UpdateBookTransactionAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(method);
                var task = (Task<string?>)method!.Invoke(_sut, new object[] { book, dto, authorIds, categoryIds })!;
                return await task;
            }

            // Camino 1: no cover, userId null
            [Fact]
            public async Task Camino1_NoCover_UserIdNull_ReturnsNull()
            {
                var book = new Book { Id = 1 };
                var dto = new UpdateBookDto { Title = "Test", UserId = null, AuthorIds = new List<int>(), CategoryIds = new List<int>() };
                
                var result = await InvokeUpdateBookTransactionAsync(book, dto, new List<uint>(), new List<byte>());
                result.Should().BeNull();
            }

            // Camino 2: no cover, userId 12
            [Fact]
            public async Task Camino2_NoCover_UserId12_ReturnsNull()
            {
                var book = new Book { Id = 1 };
                var dto = new UpdateBookDto { Title = "Test", UserId = 12, AuthorIds = new List<int>(), CategoryIds = new List<int>() };
                
                var result = await InvokeUpdateBookTransactionAsync(book, dto, new List<uint>(), new List<byte>());
                result.Should().BeNull();
                _bookRepository.Verify(r => r.UpdateAuthorsAsync(1, It.IsAny<List<uint>>(), (ushort)12), Times.Once);
                _bookRepository.Verify(r => r.UpdateCategoriesAsync(1, It.IsAny<List<byte>>(), (ushort)12), Times.Once);
            }

            // Camino 3: with cover, userId null
            [Fact]
            public async Task Camino3_WithCover_UserIdNull_ReturnsCoverPath()
            {
                var book = new Book { Id = 1 };
                var mockFile = new Mock<IFormFile>();
                mockFile.Setup(f => f.Length).Returns(1024);
                mockFile.Setup(f => f.FileName).Returns("cover.jpg");
                mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                
                var dto = new UpdateBookDto { Title = "Test", UserId = null, CoverImage = mockFile.Object, AuthorIds = new List<int>(), CategoryIds = new List<int>() };
                
                var result = await InvokeUpdateBookTransactionAsync(book, dto, new List<uint>(), new List<byte>());
                result.Should().NotBeNull();
                result.Should().StartWith("/uploads/books/");
            }

            // Camino 4: with cover, userId 12
            [Fact]
            public async Task Camino4_WithCover_UserId12_ReturnsCoverPath()
            {
                var book = new Book { Id = 1 };
                var mockFile = new Mock<IFormFile>();
                mockFile.Setup(f => f.Length).Returns(1024);
                mockFile.Setup(f => f.FileName).Returns("cover.jpg");
                mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                
                var dto = new UpdateBookDto { Title = "Test", UserId = 12, CoverImage = mockFile.Object, AuthorIds = new List<int>(), CategoryIds = new List<int>() };
                
                var result = await InvokeUpdateBookTransactionAsync(book, dto, new List<uint>(), new List<byte>());
                result.Should().NotBeNull();
            }
        }
        #endregion
    }
}
