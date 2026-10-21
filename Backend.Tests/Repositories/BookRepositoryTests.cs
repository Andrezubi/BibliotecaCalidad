using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Infraestructure.Repositories;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories
{
    public class BookRepositoryTests : IDisposable
    {
        private readonly LibraryDbcontext _context;
        private readonly BookRepository _repository;

        public BookRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<LibraryDbcontext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new LibraryDbcontext(options);
            _repository = new BookRepository(_context);

            SeedCopyStatuses();
        }

        // Estados que usa el repository: 1 = Available, 2 = Loaned
        private void SeedCopyStatuses()
        {
            _context.Copystatuses.AddRange(
                new Copystatus { Id = 1, Name = "Available" },
                new Copystatus { Id = 2, Name = "Loaned" });
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        // Crea un libro mínimo. InMemory no aplica los DEFAULT de SQL, por eso IsActive va explícito.
        private static Book CreateBook(string title, bool isActive = true, string? isbn = null)
        {
            return new Book
            {
                Title = title,
                Isbn = isbn,
                EditionNumber = 1,
                IsActive = isActive,
                CreatedAt = DateTime.Now
            };
        }

        // ===================== GetAllAsync =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task GetAllAsync_ReturnsOnlyActiveBooksOrderedByTitle_WithoutTracking()
        {
            // Arrange
            _context.Books.AddRange(
                CreateBook("Zebra", isActive: true),
                CreateBook("Alpha", isActive: true),
                CreateBook("Inactivo", isActive: false));
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = (await _repository.GetAllAsync()).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { "Alpha", "Zebra" }, result.Select(b => b.Title).ToArray());
            Assert.Empty(_context.ChangeTracker.Entries()); // AsNoTracking
        }
        // ===================== GetByIdAsync =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task GetByIdAsync_ExistingBook_ReturnsBookWithAuthorsAndCategories()
        {
            // Arrange
            var target = CreateBook("Libro buscado");
            var other = CreateBook("Otro libro");
            _context.Books.AddRange(target, other);
            await _context.SaveChangesAsync();

            _context.Bookauthors.Add(new Bookauthor
            {
                BookId = target.Id,
                AuthorId = 1,
                CreatedAt = DateTime.Now
            });
            _context.Bookcategories.Add(new Bookcategory
            {
                BookId = target.Id,
                CategoryId = 1,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear(); // fuerza a que las relaciones vengan del Include, no de la caché

            // Act
            var result = await _repository.GetByIdAsync(target.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(target.Id, result!.Id);
            Assert.Equal("Libro buscado", result.Title);
            Assert.Single(result.Bookauthors);
            Assert.Single(result.Bookcategories);
        }

        // Complementario (mismo camino I,F) - id inexistente
        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            _context.Books.Add(CreateBook("Libro existente"));
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        // Complementario (mismo camino I,F) - el repository NO filtra por IsActive
        [Fact]
        public async Task GetByIdAsync_InactiveBook_StillReturnsBook()
        {
            // Arrange
            var inactive = CreateBook("Libro inactivo", isActive: false);
            _context.Books.Add(inactive);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(inactive.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(false, result!.IsActive);
        }
        // ===================== ExistsByIsbnAsync =====================

        // Caso 1 - Camino: I,1,2,F
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExistsByIsbnAsync_BlankIsbn_ReturnsFalseWithoutQueryingDatabase(string isbn)
        {
            // Arrange: existe un libro activo con ese mismo valor; si se consultara la BD daría true
            _context.Books.Add(CreateBook("Libro con ISBN en blanco", isActive: true, isbn: isbn));
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsByIsbnAsync(isbn);

            // Assert
            Assert.False(result);
        }

        // Caso 2 - Camino: I,1,F
        [Fact]
        public async Task ExistsByIsbnAsync_ActiveBookWithIsbn_ReturnsTrue()
        {
            // Arrange
            const string isbn = "9781234567897";
            _context.Books.Add(CreateBook("Libro activo", isActive: true, isbn: isbn));
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsByIsbnAsync(isbn);

            // Assert
            Assert.True(result);
        }

        // Complementario (mismo camino I,1,F) - ISBN inexistente
        [Fact]
        public async Task ExistsByIsbnAsync_IsbnNotInDatabase_ReturnsFalse()
        {
            // Arrange
            _context.Books.Add(CreateBook("Libro activo", isActive: true, isbn: "9781234567897"));
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsByIsbnAsync("9780000000000");

            // Assert
            Assert.False(result);
        }

        // Complementario (mismo camino I,1,F) - el libro existe pero está inactivo
        [Fact]
        public async Task ExistsByIsbnAsync_InactiveBookWithIsbn_ReturnsFalse()
        {
            // Arrange
            const string isbn = "9781234567897";
            _context.Books.Add(CreateBook("Libro inactivo", isActive: false, isbn: isbn));
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsByIsbnAsync(isbn);

            // Assert
            Assert.False(result);
        }
        // ===================== DeleteAsync =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task DeleteAsync_ActiveBook_MarksAsInactiveAndPersistsChange()
        {
            // Arrange
            var book = CreateBook("Libro a eliminar", isActive: true);
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            var before = DateTime.Now;

            // Act
            await _repository.DeleteAsync(book);

            // Assert: se relee desde la BD para confirmar que el cambio se guardó
            _context.ChangeTracker.Clear();
            var saved = await _context.Books.AsNoTracking().SingleAsync(b => b.Id == book.Id);

            Assert.Equal(false, saved.IsActive);
            Assert.NotNull(saved.UpdatedAt);
            Assert.True(saved.UpdatedAt >= before);
            Assert.Equal(1, await _context.Books.CountAsync()); // eliminación lógica: el registro sigue existiendo
        }
        // ===================== Helpers para SearchAsync =====================

        private static Author CreateAuthor(string firstName, string lastName, bool isActive = true)
        {
            return new Author
            {
                FirstName = firstName,
                LastName = lastName,
                IsActive = isActive,
                CreatedAt = DateTime.Now
            };
        }

        private static Category CreateCategory(string name, bool isActive = true)
        {
            return new Category
            {
                Name = name,
                IsActive = isActive,
                CreatedAt = DateTime.Now
            };
        }

        private static Publisher CreatePublisher(string name)
        {
            return new Publisher
            {
                Name = name,
                CreatedAt = DateTime.Now
            };
        }

        private async Task<List<string>> SearchTitlesAsync(string? phrase)
        {
            var result = await _repository.SearchAsync(phrase);
            return result.Select(b => b.Title).ToList();
        }

        // ===================== SearchAsync =====================

        // Caso 1 - Camino: I,1,2,F
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchAsync_BlankPhrase_ReturnsAllActiveBooksOrderedByTitle(string? phrase)
        {
            // Arrange
            _context.Books.AddRange(
                CreateBook("Zebra", isActive: true),
                CreateBook("Alpha", isActive: true),
                CreateBook("Inactivo", isActive: false));
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync(phrase);

            // Assert
            Assert.Equal(new[] { "Alpha", "Zebra" }, titles);
        }

        // Caso 2 - Camino: I,1,3,F
        [Fact]
        public async Task SearchAsync_PhraseWithSpaces_TrimsAndReturnsMatchingBooksOrderedByTitle()
        {
            // Arrange
            _context.Books.AddRange(
                CreateBook("Beta Guide"),
                CreateBook("Alpha Guide"),
                CreateBook("Otro libro"));
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync("  Guide  ");

            // Assert
            Assert.Equal(new[] { "Alpha Guide", "Beta Guide" }, titles);
        }

        // Complementario (mismo camino I,1,3,F) - coincide por ISBN
        [Fact]
        public async Task SearchAsync_MatchesByIsbn()
        {
            // Arrange
            _context.Books.AddRange(
                CreateBook("Libro con ISBN", isbn: "9781234567897"),
                CreateBook("Libro sin ISBN", isbn: null));
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync("12345");

            // Assert
            Assert.Equal(new[] { "Libro con ISBN" }, titles);
        }

        // Complementario (mismo camino I,1,3,F) - coincide por descripción
        [Fact]
        public async Task SearchAsync_MatchesByDescription()
        {
            // Arrange
            var withDescription = CreateBook("Libro A");
            withDescription.Description = "Una historia de piratas en el Caribe";
            _context.Books.AddRange(withDescription, CreateBook("Libro B"));
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync("piratas");

            // Assert
            Assert.Equal(new[] { "Libro A" }, titles);
        }

        // Complementario (mismo camino I,1,3,F) - coincide por nombre de la editorial
        [Fact]
        public async Task SearchAsync_MatchesByPublisherName()
        {
            // Arrange
            var withPublisher = CreateBook("Libro A");
            withPublisher.Publisher = CreatePublisher("Editorial Planeta");
            _context.Books.AddRange(withPublisher, CreateBook("Libro B"));
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync("Planeta");

            // Assert
            Assert.Equal(new[] { "Libro A" }, titles);
        }

        // Complementario (mismo camino I,1,3,F) - coincide por nombre o apellido del autor activo
        [Theory]
        [InlineData("Gabriel")]
        [InlineData("Garcia")]
        public async Task SearchAsync_MatchesByActiveAuthorFirstOrLastName(string phrase)
        {
            // Arrange
            var withAuthor = CreateBook("Libro A");
            withAuthor.Bookauthors.Add(new Bookauthor
            {
                Author = CreateAuthor("Gabriel", "Garcia"),
                CreatedAt = DateTime.Now
            });
            _context.Books.AddRange(withAuthor, CreateBook("Libro B"));
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync(phrase);

            // Assert
            Assert.Equal(new[] { "Libro A" }, titles);
        }

        // Complementario (mismo camino I,1,3,F) - coincide por nombre de categoría activa
        [Fact]
        public async Task SearchAsync_MatchesByActiveCategoryName()
        {
            // Arrange
            var withCategory = CreateBook("Libro A");
            withCategory.Bookcategories.Add(new Bookcategory
            {
                Category = CreateCategory("Novela"),
                CreatedAt = DateTime.Now
            });
            _context.Books.AddRange(withCategory, CreateBook("Libro B"));
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync("Novela");

            // Assert
            Assert.Equal(new[] { "Libro A" }, titles);
        }

        // Complementario (mismo camino I,1,3,F) - un libro inactivo no aparece aunque coincida
        [Fact]
        public async Task SearchAsync_InactiveBookMatchingPhrase_IsExcluded()
        {
            // Arrange
            _context.Books.AddRange(
                CreateBook("Guia activa", isActive: true),
                CreateBook("Guia inactiva", isActive: false));
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync("Guia");

            // Assert
            Assert.Equal(new[] { "Guia activa" }, titles);
        }

        // Complementario (mismo camino I,1,3,F) - un autor inactivo no cuenta como coincidencia
        [Fact]
        public async Task SearchAsync_InactiveAuthor_DoesNotMatch()
        {
            // Arrange
            var book = CreateBook("Libro A");
            book.Bookauthors.Add(new Bookauthor
            {
                Author = CreateAuthor("Gabriel", "Garcia", isActive: false),
                CreatedAt = DateTime.Now
            });
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync("Gabriel");

            // Assert
            Assert.Empty(titles);
        }

        // Complementario (mismo camino I,1,3,F) - una categoría inactiva no cuenta como coincidencia
        [Fact]
        public async Task SearchAsync_InactiveCategory_DoesNotMatch()
        {
            // Arrange
            var book = CreateBook("Libro A");
            book.Bookcategories.Add(new Bookcategory
            {
                Category = CreateCategory("Novela", isActive: false),
                CreatedAt = DateTime.Now
            });
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync("Novela");

            // Assert
            Assert.Empty(titles);
        }

        // Complementario (mismo camino I,1,3,F) - sin coincidencias
        [Fact]
        public async Task SearchAsync_NoMatches_ReturnsEmptyList()
        {
            // Arrange
            _context.Books.Add(CreateBook("Libro A"));
            await _context.SaveChangesAsync();

            // Act
            var titles = await SearchTitlesAsync("zzz-inexistente");

            // Assert
            Assert.Empty(titles);
        }
        // ===================== Helper para copias =====================

        // statusId: 1 = Available, 2 = Loaned (sembrados en el constructor)
        private static Copy CreateCopy(string internalCode, byte statusId = 1)
        {
            return new Copy
            {
                InternalCode = internalCode,
                StatusId = statusId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
        }

        // ===================== GetAvailableAsync =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task GetAvailableAsync_ReturnsOnlyActiveBooksWithAvailableCopies_OrderedAndWithoutTracking()
        {
            // Arrange
            var zebra = CreateBook("Zebra");
            zebra.Copies.Add(CreateCopy("Z-001", statusId: 1));

            var alpha = CreateBook("Alpha");
            alpha.Copies.Add(CreateCopy("A-001", statusId: 1));
            alpha.Copies.Add(CreateCopy("A-002", statusId: 2));

            var loaned = CreateBook("Prestado");
            loaned.Copies.Add(CreateCopy("P-001", statusId: 2));

            var noCopies = CreateBook("SinCopias");

            var inactive = CreateBook("Inactivo", isActive: false);
            inactive.Copies.Add(CreateCopy("I-001", statusId: 1));

            _context.Books.AddRange(zebra, alpha, loaned, noCopies, inactive);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = (await _repository.GetAvailableAsync()).ToList();

            // Assert
            Assert.Equal(new[] { "Alpha", "Zebra" }, result.Select(b => b.Title).ToArray());
            Assert.Empty(_context.ChangeTracker.Entries()); // AsNoTracking
        }

        // Complementario (mismo camino I,F) - varias copias disponibles: el libro aparece una sola vez
        [Fact]
        public async Task GetAvailableAsync_BookWithSeveralAvailableCopies_AppearsOnlyOnce()
        {
            // Arrange
            var book = CreateBook("Libro popular");
            book.Copies.Add(CreateCopy("L-001", statusId: 1));
            book.Copies.Add(CreateCopy("L-002", statusId: 1));
            book.Copies.Add(CreateCopy("L-003", statusId: 1));
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _repository.GetAvailableAsync()).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("Libro popular", result[0].Title);
        }

        // Complementario (mismo camino I,F) - ningún libro disponible
        [Fact]
        public async Task GetAvailableAsync_NoAvailableCopies_ReturnsEmptyList()
        {
            // Arrange
            var book = CreateBook("Libro prestado");
            book.Copies.Add(CreateCopy("L-001", statusId: 2));
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAvailableAsync();

            // Assert
            Assert.Empty(result);
        } 
                // ===================== InternalCodeExistsAsync =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task InternalCodeExistsAsync_ExistingCode_ReturnsTrue()
        {
            // Arrange
            var book = CreateBook("Libro A");
            book.Copies.Add(CreateCopy("COD-001"));
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.InternalCodeExistsAsync("COD-001");

            // Assert
            Assert.True(result);
        }

        // Complementario (mismo camino I,F) - código inexistente
        [Fact]
        public async Task InternalCodeExistsAsync_NonExistingCode_ReturnsFalse()
        {
            // Arrange
            var book = CreateBook("Libro A");
            book.Copies.Add(CreateCopy("COD-001"));
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.InternalCodeExistsAsync("COD-999");

            // Assert
            Assert.False(result);
        }

        // ===================== AddCopyAsync =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task AddCopyAsync_NewCopy_PersistsCopyInDatabase()
        {
            // Arrange
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            var copy = CreateCopy("COD-001", statusId: 1);
            copy.BookId = book.Id;

            // Act
            await _repository.AddCopyAsync(copy);

            // Assert: se relee desde la BD para confirmar que el cambio se guardó
            _context.ChangeTracker.Clear();
            var saved = await _context.Copies.AsNoTracking().SingleAsync(c => c.InternalCode == "COD-001");

            Assert.True(saved.Id > 0);
            Assert.Equal(book.Id, saved.BookId);
            Assert.Equal(1, saved.StatusId);
        }

        // ===================== CountAvailableCopiesAsync =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task CountAvailableCopiesAsync_CountsOnlyAvailableCopiesOfThatBook()
        {
            // Arrange
            var bookA = CreateBook("Libro A");
            bookA.Copies.Add(CreateCopy("A-001", statusId: 1));
            bookA.Copies.Add(CreateCopy("A-002", statusId: 1));
            bookA.Copies.Add(CreateCopy("A-003", statusId: 2)); // prestada: no cuenta

            var bookB = CreateBook("Libro B");
            bookB.Copies.Add(CreateCopy("B-001", statusId: 1)); // de otro libro: no cuenta

            _context.Books.AddRange(bookA, bookB);
            await _context.SaveChangesAsync();

            // Act
            var count = await _repository.CountAvailableCopiesAsync(bookA.Id);

            // Assert
            Assert.Equal(2, count);
        }

        // Complementario (mismo camino I,F) - libro sin copias
        [Fact]
        public async Task CountAvailableCopiesAsync_BookWithoutCopies_ReturnsZero()
        {
            // Arrange
            var book = CreateBook("Libro sin copias");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var count = await _repository.CountAvailableCopiesAsync(book.Id);

            // Assert
            Assert.Equal(0, count);
        }
        // ===================== GetAuthorIdsAsync =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task GetAuthorIdsAsync_ReturnsOnlyActiveAuthorsOfThatBook()
        {
            // Arrange
            var activeAuthor = CreateAuthor("Gabriel", "Garcia", isActive: true);
            var inactiveAuthor = CreateAuthor("Inactivo", "Autor", isActive: false);
            var otherBookAuthor = CreateAuthor("Otro", "Autor", isActive: true);

            var bookA = CreateBook("Libro A");
            bookA.Bookauthors.Add(new Bookauthor { Author = activeAuthor, CreatedAt = DateTime.Now });
            bookA.Bookauthors.Add(new Bookauthor { Author = inactiveAuthor, CreatedAt = DateTime.Now });

            var bookB = CreateBook("Libro B");
            bookB.Bookauthors.Add(new Bookauthor { Author = otherBookAuthor, CreatedAt = DateTime.Now });

            _context.Books.AddRange(bookA, bookB);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _repository.GetAuthorIdsAsync(bookA.Id);

            // Assert
            Assert.Equal(new[] { activeAuthor.Id }, result.ToArray());
        }

        // Complementario (mismo camino I,F) - libro sin autores
        [Fact]
        public async Task GetAuthorIdsAsync_BookWithoutAuthors_ReturnsEmptyList()
        {
            // Arrange
            var book = CreateBook("Libro sin autores");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAuthorIdsAsync(book.Id);

            // Assert
            Assert.Empty(result);
        }

        // ===================== GetCategoryIdsAsync =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task GetCategoryIdsAsync_ReturnsOnlyActiveCategoriesOfThatBook()
        {
            // Arrange
            var activeCategory = CreateCategory("Novela", isActive: true);
            var inactiveCategory = CreateCategory("Obsoleta", isActive: false);
            var otherBookCategory = CreateCategory("Ensayo", isActive: true);

            var bookA = CreateBook("Libro A");
            bookA.Bookcategories.Add(new Bookcategory { Category = activeCategory, CreatedAt = DateTime.Now });
            bookA.Bookcategories.Add(new Bookcategory { Category = inactiveCategory, CreatedAt = DateTime.Now });

            var bookB = CreateBook("Libro B");
            bookB.Bookcategories.Add(new Bookcategory { Category = otherBookCategory, CreatedAt = DateTime.Now });

            _context.Books.AddRange(bookA, bookB);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _repository.GetCategoryIdsAsync(bookA.Id);

            // Assert
            Assert.Equal(new[] { activeCategory.Id }, result.ToArray());
        }

        // Complementario (mismo camino I,F) - libro sin categorías
        [Fact]
        public async Task GetCategoryIdsAsync_BookWithoutCategories_ReturnsEmptyList()
        {
            // Arrange
            var book = CreateBook("Libro sin categorías");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetCategoryIdsAsync(book.Id);

            // Assert
            Assert.Empty(result);
        }
        // ===================== UpdateAuthorsAsync =====================

        // Caso 1 - Camino: I,1,5,6,F
        [Fact]
        public async Task UpdateAuthorsAsync_NoRelationsAndNoIds_DoesNothing()
        {
            // Arrange
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAuthorsAsync(book.Id, new List<uint>(), (ushort)7);

            // Assert
            Assert.Empty(await _context.Bookauthors.ToListAsync());
        }

        // Caso 2 - Camino: I,1,5,6,7,9,6,F
        [Fact]
        public async Task UpdateAuthorsAsync_NewAuthorId_AddsRelation()
        {
            // Arrange
            const ushort userId = 7;
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAuthorsAsync(book.Id, new List<uint> { 5 }, userId);

            // Assert
            _context.ChangeTracker.Clear();
            var relation = Assert.Single(await _context.Bookauthors.AsNoTracking().ToListAsync());
            Assert.Equal(book.Id, relation.BookId);
            Assert.Equal(5u, relation.AuthorId);
            Assert.Equal(userId, relation.UserId);
            Assert.Null(relation.UpdatedAt);
        }

        // Caso 3 - Camino: I,1,2,4,1,5,6,F
        [Fact]
        public async Task UpdateAuthorsAsync_RelationNotInSelection_RemovesRelation()
        {
            // Arrange
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            _context.Bookauthors.Add(new Bookauthor { BookId = book.Id, AuthorId = 1, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _repository.UpdateAuthorsAsync(book.Id, new List<uint>(), (ushort)7);

            // Assert
            _context.ChangeTracker.Clear();
            Assert.Empty(await _context.Bookauthors.AsNoTracking().ToListAsync());
        }

        // Caso 4 - Camino: I,1,2,3,1,5,6,7,8,6,F
        [Fact]
        public async Task UpdateAuthorsAsync_RelationInSelection_UpdatesRelationWithoutDuplicating()
        {
            // Arrange
            const ushort userId = 7;
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            _context.Bookauthors.Add(new Bookauthor { BookId = book.Id, AuthorId = 1, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _repository.UpdateAuthorsAsync(book.Id, new List<uint> { 1 }, userId);

            // Assert
            _context.ChangeTracker.Clear();
            var relation = Assert.Single(await _context.Bookauthors.AsNoTracking().ToListAsync());
            Assert.Equal(1u, relation.AuthorId);
            Assert.Equal(userId, relation.UserId);
            Assert.NotNull(relation.UpdatedAt);
        }

        // Complementario - mezcla: conserva el 1, elimina el 2 y agrega el 3
        [Fact]
        public async Task UpdateAuthorsAsync_MixedSelection_KeepsRemovesAndAddsCorrectly()
        {
            // Arrange
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            _context.Bookauthors.AddRange(
                new Bookauthor { BookId = book.Id, AuthorId = 1, CreatedAt = DateTime.Now },
                new Bookauthor { BookId = book.Id, AuthorId = 2, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _repository.UpdateAuthorsAsync(book.Id, new List<uint> { 1, 3 }, (ushort)7);

            // Assert
            _context.ChangeTracker.Clear();
            var ids = (await _context.Bookauthors.AsNoTracking().ToListAsync())
                .Select(r => r.AuthorId)
                .OrderBy(id => id)
                .ToArray();
            Assert.Equal(new[] { 1u, 3u }, ids);
        }

        // Complementario - se descartan los ids 0 y los duplicados
        [Fact]
        public async Task UpdateAuthorsAsync_ZeroAndDuplicateIds_AreDiscarded()
        {
            // Arrange
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAuthorsAsync(book.Id, new List<uint> { 0, 5, 5 }, (ushort)7);

            // Assert
            _context.ChangeTracker.Clear();
            var relation = Assert.Single(await _context.Bookauthors.AsNoTracking().ToListAsync());
            Assert.Equal(5u, relation.AuthorId);
        }

        // ===================== UpdateCategoriesAsync =====================

        // Caso 1 - Camino: I,1,5,6,F
        [Fact]
        public async Task UpdateCategoriesAsync_NoRelationsAndNoIds_DoesNothing()
        {
            // Arrange
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateCategoriesAsync(book.Id, new List<byte>(), (ushort)7);

            // Assert
            Assert.Empty(await _context.Bookcategories.ToListAsync());
        }

        // Caso 2 - Camino: I,1,5,6,7,9,6,F
        [Fact]
        public async Task UpdateCategoriesAsync_NewCategoryId_AddsRelation()
        {
            // Arrange
            const ushort userId = 7;
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateCategoriesAsync(book.Id, new List<byte> { 5 }, userId);

            // Assert
            _context.ChangeTracker.Clear();
            var relation = Assert.Single(await _context.Bookcategories.AsNoTracking().ToListAsync());
            Assert.Equal(book.Id, relation.BookId);
            Assert.Equal((byte)5, relation.CategoryId);
            Assert.Equal(userId, relation.UserId);
            Assert.Null(relation.UpdatedAt);
        }

        // Caso 3 - Camino: I,1,2,4,1,5,6,F
        [Fact]
        public async Task UpdateCategoriesAsync_RelationNotInSelection_RemovesRelation()
        {
            // Arrange
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            _context.Bookcategories.Add(new Bookcategory { BookId = book.Id, CategoryId = 1, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _repository.UpdateCategoriesAsync(book.Id, new List<byte>(), (ushort)7);

            // Assert
            _context.ChangeTracker.Clear();
            Assert.Empty(await _context.Bookcategories.AsNoTracking().ToListAsync());
        }

        // Caso 4 - Camino: I,1,2,3,1,5,6,7,8,6,F
        [Fact]
        public async Task UpdateCategoriesAsync_RelationInSelection_UpdatesRelationWithoutDuplicating()
        {
            // Arrange
            const ushort userId = 7;
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            _context.Bookcategories.Add(new Bookcategory { BookId = book.Id, CategoryId = 1, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _repository.UpdateCategoriesAsync(book.Id, new List<byte> { 1 }, userId);

            // Assert
            _context.ChangeTracker.Clear();
            var relation = Assert.Single(await _context.Bookcategories.AsNoTracking().ToListAsync());
            Assert.Equal((byte)1, relation.CategoryId);
            Assert.Equal(userId, relation.UserId);
            Assert.NotNull(relation.UpdatedAt);
        }

        // Complementario - mezcla: conserva el 1, elimina el 2 y agrega el 3
        [Fact]
        public async Task UpdateCategoriesAsync_MixedSelection_KeepsRemovesAndAddsCorrectly()
        {
            // Arrange
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            _context.Bookcategories.AddRange(
                new Bookcategory { BookId = book.Id, CategoryId = 1, CreatedAt = DateTime.Now },
                new Bookcategory { BookId = book.Id, CategoryId = 2, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _repository.UpdateCategoriesAsync(book.Id, new List<byte> { 1, 3 }, (ushort)7);

            // Assert
            _context.ChangeTracker.Clear();
            var ids = (await _context.Bookcategories.AsNoTracking().ToListAsync())
                .Select(r => r.CategoryId)
                .OrderBy(id => id)
                .ToArray();
            Assert.Equal(new byte[] { 1, 3 }, ids);
        }

        // Complementario - se descartan los ids 0 y los duplicados
        [Fact]
        public async Task UpdateCategoriesAsync_ZeroAndDuplicateIds_AreDiscarded()
        {
            // Arrange
            var book = CreateBook("Libro A");
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateCategoriesAsync(book.Id, new List<byte> { 0, 5, 5 }, (ushort)7);

            // Assert
            _context.ChangeTracker.Clear();
            var relation = Assert.Single(await _context.Bookcategories.AsNoTracking().ToListAsync());
            Assert.Equal((byte)5, relation.CategoryId);
        }
    }
}