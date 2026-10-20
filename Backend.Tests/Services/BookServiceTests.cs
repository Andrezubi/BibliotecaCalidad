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


        public class CreateAsyncTests : IDisposable
        {
            private readonly Mock<IBookRepository> _bookRepo = new();
            private readonly Mock<IAuthorRepository> _authorRepo = new();
            private readonly Mock<ICategoryRepository> _categoryRepo = new();
            private readonly Mock<IWebHostEnvironment> _env = new();
            private readonly Mock<LibraryDbcontext> _context;
            private readonly Mock<DatabaseFacade> _database;
            private readonly Mock<IDbContextTransaction> _transaction = new();

            private readonly string _webRoot;
            private readonly BookService _sut;

            public CreateAsyncTests()
            {
                _webRoot = Path.Combine(Path.GetTempPath(), "bookservice-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_webRoot);
                _env.Setup(e => e.WebRootPath).Returns(_webRoot);

                // DbContext + DatabaseFacade simulados para controlar la transacción
                var options = new DbContextOptionsBuilder<LibraryDbcontext>().Options;
                _context = new Mock<LibraryDbcontext>(options);
                _database = new Mock<DatabaseFacade>(_context.Object);
                _context.Setup(c => c.Database).Returns(_database.Object);
                _database.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_transaction.Object);

                // Validaciones previas exitosas
                _authorRepo.Setup(r => r.ExistsActiveByIdAsync(It.IsAny<uint>())).ReturnsAsync(true);
                _categoryRepo.Setup(r => r.ExistsActiveByIdAsync(It.IsAny<byte>())).ReturnsAsync(true);
                _bookRepo.Setup(r => r.ExistsByIsbnAsync(It.IsAny<string>())).ReturnsAsync(false);

                // Datos que usa MapToDtoAsync al final
                _bookRepo.Setup(r => r.GetAuthorIdsAsync(It.IsAny<uint>()))
                         .ReturnsAsync(new List<uint> { 1 });
                _bookRepo.Setup(r => r.GetCategoryIdsAsync(It.IsAny<uint>()))
                         .ReturnsAsync(new List<byte> { 1 });

                _sut = new BookService(
                    _bookRepo.Object,
                    _authorRepo.Object,
                    _categoryRepo.Object,
                    _env.Object,
                    _context.Object);
            }

            public void Dispose()
            {
                if (Directory.Exists(_webRoot))
                {
                    Directory.Delete(_webRoot, recursive: true);
                }
            }

            // ---------- Helpers ----------

            private static CreateBookDto BuildDto(IFormFile? cover = null) => new CreateBookDto
            {
                Title = "Clean Code",
                EditionNumber = 1,
                ISBN = "9780132350884",
                PublicationYear = 2008,
                PublisherId = 1,
                PageCount = 464,
                Description = "A handbook of agile software craftsmanship",
                UserId = 1,
                AuthorIds = new List<int> { 1 },
                CategoryIds = new List<int> { 1 },
                CoverImage = cover
            };

            /// <summary>Portada JPG válida; si copyFailure != null, CopyToAsync lanza esa excepción.</summary>
            private static IFormFile BuildCover(Exception? copyFailure = null)
            {
                var content = new byte[] { 1, 2, 3 };
                var file = new Mock<IFormFile>();
                file.Setup(f => f.FileName).Returns("cover.jpg");
                file.Setup(f => f.Length).Returns(content.Length);
                file.Setup(f => f.ContentType).Returns("image/jpeg");

                var copy = file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()));
                if (copyFailure != null)
                {
                    copy.ThrowsAsync(copyFailure);
                }
                else
                {
                    copy.Returns<Stream, CancellationToken>((stream, _) =>
                        stream.WriteAsync(content, 0, content.Length));
                }

                return file.Object;
            }

            private List<string> UploadedFiles()
            {
                var uploads = Path.Combine(_webRoot, "uploads", "books");
                return Directory.Exists(uploads)
                    ? Directory.EnumerateFiles(uploads).ToList()
                    : new List<string>();
            }

            private void VerifyRolledBack() =>
                _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);

            // ---------- Caso 1: I,1,2,3,4,5,6,7,9,10,F ----------
            [Fact]
            public async Task Caso1_SinPortada_CreaLibroYConfirmaTransaccion()
            {
                // Arrange
                Book? savedBook = null;
                _bookRepo.Setup(r => r.AddAsync(It.IsAny<Book>()))
                         .Callback<Book>(b => { b.Id = 100; savedBook = b; });

                // Act
                var result = await _sut.CreateAsync(BuildDto(cover: null));

                // Assert: DTO devuelto
                Assert.NotNull(result);
                Assert.Equal(100, result.Id);
                Assert.Equal("Clean Code", result.Title);
                Assert.Null(result.CoverImage);
                Assert.Equal(new List<int> { 1 }, result.AuthorIds);
                Assert.Equal(new List<int> { 1 }, result.CategoryIds);

                // Assert: libro persistido sin portada y sin archivos en disco
                Assert.NotNull(savedBook);
                Assert.Null(savedBook!.CoverImage);
                Assert.Empty(UploadedFiles());

                // Assert: relaciones y transacción
                _authorRepo.Verify(r => r.AddBookAuthorAsync(It.IsAny<Bookauthor>()), Times.Once);
                _categoryRepo.Verify(r => r.AddBookCategoryAsync(It.IsAny<Bookcategory>()), Times.Once);
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
                _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
            }

            // ---------- Caso 2: I,1,2,3,4,5,6,7,8,9,10,F ----------
            [Fact]
            public async Task Caso2_ConPortada_GuardaPortadaAsignaRutaYConfirma()
            {
                // Arrange
                Book? savedBook = null;
                _bookRepo.Setup(r => r.AddAsync(It.IsAny<Book>()))
                         .Callback<Book>(b => { b.Id = 100; savedBook = b; });

                // Act
                var result = await _sut.CreateAsync(BuildDto(BuildCover()));

                // Assert: la portada se guardó en disco y se asignó al libro
                var files = UploadedFiles();
                Assert.Single(files);

                Assert.NotNull(savedBook);
                Assert.False(string.IsNullOrWhiteSpace(savedBook!.CoverImage));
                Assert.StartsWith("/uploads/books/", savedBook.CoverImage);
                Assert.EndsWith(".jpg", savedBook.CoverImage);
                Assert.Equal(Path.GetFileName(files[0]), Path.GetFileName(savedBook.CoverImage));

                // Assert: DTO devuelto
                Assert.Equal(savedBook.CoverImage, result.CoverImage);
                Assert.Equal(100, result.Id);

                // Assert: relaciones y transacción
                _authorRepo.Verify(r => r.AddBookAuthorAsync(It.IsAny<Bookauthor>()), Times.Once);
                _categoryRepo.Verify(r => r.AddBookCategoryAsync(It.IsAny<Bookcategory>()), Times.Once);
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
                _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
            }

            // ---------- Caso 3: I,1,2,3,4,5,6,11 ----------
            [Fact]
            public async Task Caso3_SaveCoverLanzaExcepcion_HaceRollbackYPropagaExcepcion()
            {
                // Arrange: CopyToAsync falla dentro de SaveCoverAsync => no se devuelve ninguna ruta
                var failure = new IOException("Error al guardar la portada");

                // Act
                var ex = await Assert.ThrowsAsync<IOException>(
                    () => _sut.CreateAsync(BuildDto(BuildCover(failure))));

                // Assert
                Assert.Same(failure, ex);
                VerifyRolledBack();
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
                _bookRepo.Verify(r => r.AddAsync(It.IsAny<Book>()), Times.Never);
            }

            // ---------- Caso 4: I,1,2,3,4,5,6,7,8,9,11 ----------
            [Fact]
            public async Task Caso4_AddAsyncLanzaExcepcion_HaceRollbackEliminaPortadaYPropagaExcepcion()
            {
                // Arrange
                var failure = new InvalidOperationException("Error al agregar el libro");
                _bookRepo.Setup(r => r.AddAsync(It.IsAny<Book>())).ThrowsAsync(failure);

                // Act
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _sut.CreateAsync(BuildDto(BuildCover())));

                // Assert
                Assert.Same(failure, ex);
                VerifyRolledBack();
                Assert.Empty(UploadedFiles());      // la portada guardada fue eliminada
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
                _authorRepo.Verify(r => r.AddBookAuthorAsync(It.IsAny<Bookauthor>()), Times.Never);
            }

            // ---------- Caso 5a: I,1,2,3,4,5,6,7,8,9,11 (falla AddAuthorsAsync) ----------
            [Fact]
            public async Task Caso5a_AddAuthorsLanzaExcepcion_HaceRollbackEliminaPortadaYPropagaExcepcion()
            {
                // Arrange
                var failure = new InvalidOperationException("Error al agregar autores");
                _bookRepo.Setup(r => r.AddAsync(It.IsAny<Book>()))
                         .Callback<Book>(b => b.Id = 100);
                _authorRepo.Setup(r => r.AddBookAuthorAsync(It.IsAny<Bookauthor>())).ThrowsAsync(failure);

                // Act
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _sut.CreateAsync(BuildDto(BuildCover())));

                // Assert
                Assert.Same(failure, ex);
                VerifyRolledBack();
                Assert.Empty(UploadedFiles());
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            }

            // ---------- Caso 5b: I,1,2,3,4,5,6,7,8,9,11 (falla AddCategoriesAsync) ----------
            [Fact]
            public async Task Caso5b_AddCategoriesLanzaExcepcion_HaceRollbackEliminaPortadaYPropagaExcepcion()
            {
                // Arrange
                var failure = new InvalidOperationException("Error al agregar categorías");
                _bookRepo.Setup(r => r.AddAsync(It.IsAny<Book>()))
                         .Callback<Book>(b => b.Id = 100);
                _categoryRepo.Setup(r => r.AddBookCategoryAsync(It.IsAny<Bookcategory>())).ThrowsAsync(failure);

                // Act
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _sut.CreateAsync(BuildDto(BuildCover())));

                // Assert
                Assert.Same(failure, ex);
                VerifyRolledBack();
                Assert.Empty(UploadedFiles());
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            }

            // ---------- Caso 6: I,1,2,3,4,5,6,7,8,9,10,11 ----------
            [Fact]
            public async Task Caso6_CommitLanzaExcepcion_HaceRollbackEliminaPortadaYPropagaExcepcion()
            {
                // Arrange
                var failure = new InvalidOperationException("Error al confirmar la transacción");
                _bookRepo.Setup(r => r.AddAsync(It.IsAny<Book>()))
                         .Callback<Book>(b => b.Id = 100);
                _transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);

                // Act
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => _sut.CreateAsync(BuildDto(BuildCover())));

                // Assert
                Assert.Same(failure, ex);
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
                VerifyRolledBack();
                Assert.Empty(UploadedFiles());
            }
        }






        public class UpdateAsyncTests : IDisposable
        {
            private const string OldCoverFileName = "old-cover.jpg";
            private const string OldCoverPath = "/uploads/books/" + OldCoverFileName;

            private readonly Mock<IBookRepository> _bookRepo = new();
            private readonly Mock<IAuthorRepository> _authorRepo = new();
            private readonly Mock<ICategoryRepository> _categoryRepo = new();
            private readonly Mock<IWebHostEnvironment> _env = new();
            private readonly Mock<LibraryDbcontext> _context;
            private readonly Mock<DatabaseFacade> _database;
            private readonly Mock<IDbContextTransaction> _transaction = new();

            private readonly string _webRoot;
            private readonly BookService _sut;

            public UpdateAsyncTests()
            {
                _webRoot = Path.Combine(Path.GetTempPath(), "bookservice-tests-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_webRoot);
                _env.Setup(e => e.WebRootPath).Returns(_webRoot);

                // DbContext + DatabaseFacade simulados para controlar la transacción
                var options = new DbContextOptionsBuilder<LibraryDbcontext>().Options;
                _context = new Mock<LibraryDbcontext>(options);
                _database = new Mock<DatabaseFacade>(_context.Object);
                _context.Setup(c => c.Database).Returns(_database.Object);
                _database.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_transaction.Object);

                // Validaciones previas exitosas
                _authorRepo.Setup(r => r.ExistsActiveByIdAsync(It.IsAny<uint>())).ReturnsAsync(true);
                _categoryRepo.Setup(r => r.ExistsActiveByIdAsync(It.IsAny<byte>())).ReturnsAsync(true);
                _bookRepo.Setup(r => r.ExistsByIsbnAsync(It.IsAny<string>())).ReturnsAsync(false);

                // Datos que usa MapToDtoAsync al final
                _bookRepo.Setup(r => r.GetAuthorIdsAsync(It.IsAny<uint>()))
                         .ReturnsAsync(new List<uint> { 1 });
                _bookRepo.Setup(r => r.GetCategoryIdsAsync(It.IsAny<uint>()))
                         .ReturnsAsync(new List<byte> { 1 });

                _sut = new BookService(
                    _bookRepo.Object,
                    _authorRepo.Object,
                    _categoryRepo.Object,
                    _env.Object,
                    _context.Object);
            }

            public void Dispose()
            {
                if (Directory.Exists(_webRoot))
                {
                    Directory.Delete(_webRoot, recursive: true);
                }
            }

            // ---------- Helpers ----------

            private static UpdateBookDto ValidDto(IFormFile? cover = null) => new UpdateBookDto
            {
                Title = "Clean Architecture",
                EditionNumber = 2,
                ISBN = "9780134494166",
                PublicationYear = 2017,
                PublisherId = 1,
                PageCount = 432,
                Description = "A craftsman's guide to software structure and design",
                UserId = 1,
                AuthorIds = new List<int> { 1 },
                CategoryIds = new List<int> { 1 },
                CoverImage = cover
            };

            private static Book BuildBook(bool isActive, string? coverImage = null) => new Book
            {
                Id = 1,
                Title = "Clean Code",
                EditionNumber = 1,
                Isbn = "9780132350884",
                PublicationYear = 2008,
                PublisherId = 1,
                PageCount = 464,
                Description = "Descripción original",
                CoverImage = coverImage,
                IsActive = isActive,
                CreatedAt = new DateTime(2026, 1, 1)
            };

            private static IFormFile BuildCover()
            {
                var content = new byte[] { 1, 2, 3 };
                var file = new Mock<IFormFile>();
                file.Setup(f => f.FileName).Returns("cover.jpg");
                file.Setup(f => f.Length).Returns(content.Length);
                file.Setup(f => f.ContentType).Returns("image/jpeg");
                file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Returns<Stream, CancellationToken>((stream, _) =>
                        stream.WriteAsync(content, 0, content.Length));
                return file.Object;
            }

            private void CreateOldCoverOnDisk()
            {
                var uploads = Path.Combine(_webRoot, "uploads", "books");
                Directory.CreateDirectory(uploads);
                File.WriteAllBytes(Path.Combine(uploads, OldCoverFileName), new byte[] { 9, 9, 9 });
            }

            private List<string> UploadedFiles()
            {
                var uploads = Path.Combine(_webRoot, "uploads", "books");
                return Directory.Exists(uploads)
                    ? Directory.EnumerateFiles(uploads).ToList()
                    : new List<string>();
            }

            // ---------- Caso 1: I,1,2,3,F ----------
            [Fact]
            public async Task Caso1_LibroNoExiste_RetornaNull()
            {
                // Arrange
                _bookRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Book?)null);

                // Act
                var result = await _sut.UpdateAsync(1, ValidDto());

                // Assert
                Assert.Null(result);
                _bookRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
                _bookRepo.Verify(r => r.UpdateAsync(It.IsAny<Book>()), Times.Never);
                _database.Verify(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
                _authorRepo.VerifyNoOtherCalls();
                _categoryRepo.VerifyNoOtherCalls();
            }

            // ---------- Caso 2: I,1,2,3,4,F ----------
            [Fact]
            public async Task Caso2_LibroInactivo_RetornaNull()
            {
                // Arrange
                _bookRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildBook(isActive: false));

                // Act
                var result = await _sut.UpdateAsync(1, ValidDto());

                // Assert
                Assert.Null(result);
                _bookRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
                _bookRepo.Verify(r => r.UpdateAsync(It.IsAny<Book>()), Times.Never);
                _database.Verify(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
                _authorRepo.VerifyNoOtherCalls();
                _categoryRepo.VerifyNoOtherCalls();
            }

            // ---------- Caso 3: I,1,2,3,4,5,6,7,8,9,10,11,12,F ----------
            [Fact]
            public async Task Caso3_LibroActivoYTodoValido_ActualizaYRetornaDto()
            {
                // Arrange: libro activo con portada anterior en disco y una portada nueva en el DTO
                var book = BuildBook(isActive: true, coverImage: OldCoverPath);
                CreateOldCoverOnDisk();
                _bookRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(book);

                var dto = ValidDto(BuildCover());

                // Act
                var result = await _sut.UpdateAsync(1, dto);

                // Assert: DTO devuelto
                Assert.NotNull(result);
                Assert.Equal(1, result!.Id);
                Assert.Equal("Clean Architecture", result.Title);
                Assert.Equal("9780134494166", result.ISBN);
                Assert.Equal(new List<int> { 1 }, result.AuthorIds);
                Assert.Equal(new List<int> { 1 }, result.CategoryIds);

                // Assert: la entidad se actualizó con los datos del DTO
                Assert.Equal("Clean Architecture", book.Title);
                Assert.Equal("9780134494166", book.Isbn);
                Assert.NotNull(book.UpdatedAt);

                // Assert: portada nueva guardada y portada anterior eliminada (DeleteCoverImage)
                var files = UploadedFiles();
                Assert.Single(files);
                Assert.NotEqual(OldCoverFileName, Path.GetFileName(files[0]));
                Assert.StartsWith("/uploads/books/", book.CoverImage);
                Assert.NotEqual(OldCoverPath, book.CoverImage);
                Assert.Equal(book.CoverImage, result.CoverImage);

                // Assert: persistencia y transacción
                _bookRepo.Verify(r => r.UpdateAsync(book), Times.Once);
                _bookRepo.Verify(r => r.UpdateAuthorsAsync(
                    1, It.IsAny<List<uint>>(), It.IsAny<ushort?>()), Times.Once);
                _bookRepo.Verify(r => r.UpdateCategoriesAsync(
                    1, It.IsAny<List<byte>>(), It.IsAny<ushort?>()), Times.Once);
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
                _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
            }
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
            public async Task Camino1_EmptyList_ThrowsBookValidationException()
            {
                var exception = await Assert.ThrowsAsync<BookValidationException>(
                    () => InvokeValidateAuthorsAsync(new List<uint>()));

                exception.Should().NotBeNull();

                _authorRepository.Verify(
                    r => r.ExistsActiveByIdAsync(It.IsAny<uint>()),
                    Times.Never);
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
            public async Task Camino1_EmptyList_ThrowsArgumentException()
            {
                var exception = await Assert.ThrowsAsync<BookValidationException>(
                    () => InvokeValidateCategoriesAsync(new List<byte>()));

                _categoryRepository.Verify(
                    r => r.ExistsActiveByIdAsync(It.IsAny<byte>()),
                    Times.Never);
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
                var book = new Book
                {
                    Id = 1,
                    Title = "Test",
                    EditionNumber = 1
                };

                var mockFile = new Mock<IFormFile>();
                mockFile.Setup(f => f.Length).Returns(1024);
                mockFile.Setup(f => f.FileName).Returns("cover.jpg");
                mockFile.Setup(f => f.CopyToAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                var dto = new UpdateBookDto
                {
                    Title = "Test",
                    UserId = null,
                    CoverImage = mockFile.Object,
                    AuthorIds = new List<int>(),
                    CategoryIds = new List<int>()
                };

                var result = await InvokeUpdateBookTransactionAsync(
                    book,
                    dto,
                    new List<uint>(),
                    new List<byte>());

                result.Should().NotBeNull();
                result.Should().StartWith("/uploads/books/");
            }

            // Camino 4: with cover, userId 12
            [Fact]
            public async Task Camino4_WithCover_UserId12_ReturnsCoverPath()
            {
                var book = new Book
                {
                    Id = 1,
                    Title = "Test",
                    EditionNumber = 1
                };

                var mockFile = new Mock<IFormFile>();
                mockFile.Setup(f => f.Length).Returns(1024);
                mockFile.Setup(f => f.FileName).Returns("cover.jpg");
                mockFile.Setup(f => f.CopyToAsync(
                        It.IsAny<Stream>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                var dto = new UpdateBookDto
                {
                    Title = "Test",
                    UserId = 12,
                    CoverImage = mockFile.Object,
                    AuthorIds = new List<int>(),
                    CategoryIds = new List<int>()
                };

                var result = await InvokeUpdateBookTransactionAsync(
                    book,
                    dto,
                    new List<uint>(),
                    new List<byte>());

                result.Should().NotBeNull();
                result.Should().StartWith("/uploads/books/");

                _bookRepository.Verify(
                    r => r.UpdateAuthorsAsync(
                        1,
                        It.IsAny<List<uint>>(),
                        (ushort)12),
                    Times.Once);

                _bookRepository.Verify(
                    r => r.UpdateCategoriesAsync(
                        1,
                        It.IsAny<List<byte>>(),
                        (ushort)12),
                    Times.Once);
            }
        }
        #endregion
    }
}
