using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Controllers;
using Backend.Domain.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers
{
    public class BookControllerTests
    {
        private readonly Mock<IBookService> _bookServiceMock;
        private readonly BookController _controller;

        public BookControllerTests()
        {
            _bookServiceMock = new Mock<IBookService>();
            _controller = new BookController(_bookServiceMock.Object);

            // ValidationProblem(...) necesita un ProblemDetailsFactory; sin HttpContext sería null.
            var factoryMock = new Mock<ProblemDetailsFactory>();
            factoryMock
                .Setup(f => f.CreateValidationProblemDetails(
                    It.IsAny<HttpContext>(),
                    It.IsAny<ModelStateDictionary>(),
                    It.IsAny<int?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()))
                .Returns((HttpContext? _, ModelStateDictionary ms, int? __, string? ___, string? ____, string? _____, string? ______) =>
                    new ValidationProblemDetails(ms) { Status = 400 });
            _controller.ProblemDetailsFactory = factoryMock.Object;
        }

        // Lee una propiedad de un tipo anónimo (body de Conflict) por reflexión
        private static object? GetProp(object? body, string name)
        {
            Assert.NotNull(body);
            var prop = body!.GetType().GetProperty(name);
            Assert.NotNull(prop);
            return prop!.GetValue(body);
        }

        // ===================== GetById =====================

        // Caso 1 - Camino: I,1,2,F
        [Fact]
        public async Task GetById_BookNotFound_ReturnsNotFound()
        {
            const int id = 999;

            _bookServiceMock
                .Setup(s => s.GetByIdAsync((uint)id))
                .ReturnsAsync((BookDto?)null);

            var actionResult = await _controller.GetById(id);

            var notFound = Assert.IsType<NotFoundResult>(actionResult.Result);
            Assert.Equal(404, notFound.StatusCode);
            _bookServiceMock.Verify(s => s.GetByIdAsync((uint)id), Times.Once);
        }

        // Caso 2 - Camino: I,1,F
        [Fact]
        public async Task GetById_BookFound_ReturnsOkWithBook()
        {
            const int id = 1;
            var expected = new BookDto
            {
                Id = id,
                Title = "Libro de prueba",
                ISBN = "9781234567897",
                EditionNumber = 1,
                AvailableCopies = 2,
                IsActive = true,
                AuthorIds = new List<int> { 1 },
                CategoryIds = new List<int> { 1 }
            };

            _bookServiceMock
                .Setup(s => s.GetByIdAsync((uint)id))
                .ReturnsAsync(expected);

            var actionResult = await _controller.GetById(id);

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(200, ok.StatusCode);
            Assert.Same(expected, ok.Value);
            _bookServiceMock.Verify(s => s.GetByIdAsync((uint)id), Times.Once);
        }

        // ===================== Create =====================

        // Caso 1 - Camino: I,1,2,F
        [Fact]
        public async Task Create_ServiceSucceeds_ReturnsCreatedAtActionWithBook()
        {
            var dto = new CreateBookDto();
            var book = new BookDto { Id = 5, Title = "Libro nuevo" };

            _bookServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ReturnsAsync(book);

            var actionResult = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            Assert.Equal(201, created.StatusCode);
            Assert.Equal("GetById", created.ActionName);
            Assert.Equal(5, created.RouteValues!["id"]);
            Assert.Same(book, created.Value);
            _bookServiceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        // Caso 2 - Camino: I,1,3,8,10,F  (excepción no controlada: se propaga)
        [Fact]
        public async Task Create_UnhandledException_PropagatesException()
        {
            var dto = new CreateBookDto();

            _bookServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new Exception("Error inesperado"));

            var ex = await Assert.ThrowsAsync<Exception>(() => _controller.Create(dto));
            Assert.Equal("Error inesperado", ex.Message);
        }

        // Caso 3 - Camino: I,1,3,8,10,11,F
        [Fact]
        public async Task Create_InvalidOperationException_ReturnsConflictWithMessage()
        {
            var dto = new CreateBookDto();

            _bookServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new InvalidOperationException("Ya existe un libro activo con ese ISBN."));

            var actionResult = await _controller.Create(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(actionResult.Result);
            Assert.Equal(409, conflict.StatusCode);
            Assert.Equal("Ya existe un libro activo con ese ISBN.", GetProp(conflict.Value, "message") as string);
        }

        // Caso 4 - Camino: I,1,3,8,9,F
        [Fact]
        public async Task Create_ArgumentException_ReturnsValidationProblemWithMessage()
        {
            var dto = new CreateBookDto();

            _bookServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new ArgumentException("Datos inválidos."));

            var actionResult = await _controller.Create(dto);

            var result = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
            Assert.IsType<ValidationProblemDetails>(result.Value);
            Assert.Equal(1, _controller.ModelState.ErrorCount);
            Assert.Equal("Datos inválidos.", _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        // Caso 5 - Camino: I,1,3,4,7,F  (Errors vacío: 0 iteraciones del foreach externo)
        [Fact]
        public async Task Create_BookValidationException_EmptyErrors_ReturnsValidationProblemWithoutErrors()
        {
            var dto = new CreateBookDto();
            var errors = new Dictionary<string, string[]>();

            _bookServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new BookValidationException(errors));

            var actionResult = await _controller.Create(dto);

            var result = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
            Assert.IsType<ValidationProblemDetails>(result.Value);
            Assert.Equal(0, _controller.ModelState.ErrorCount);
        }

        // Caso 6 - Camino: I,1,3,4,5,4,7,F  (1 clave sin mensajes: foreach interno con 0 iteraciones)
        [Fact]
        public async Task Create_BookValidationException_KeyWithoutMessages_ReturnsValidationProblemWithoutErrors()
        {
            var dto = new CreateBookDto();
            var errors = new Dictionary<string, string[]> { ["Title"] = new string[0] };

            _bookServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new BookValidationException(errors));

            var actionResult = await _controller.Create(dto);

            var result = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
            Assert.IsType<ValidationProblemDetails>(result.Value);
            Assert.Equal(0, _controller.ModelState.ErrorCount);
        }

        // Caso 7 - Camino: I,1,3,4,5,6,5,4,7,F  (1 clave con 1 mensaje)
        [Fact]
        public async Task Create_BookValidationException_KeyWithMessage_ReturnsValidationProblemWithError()
        {
            var dto = new CreateBookDto();
            var errors = new Dictionary<string, string[]>
            {
                ["Title"] = new[] { "El título es obligatorio" }
            };

            _bookServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new BookValidationException(errors));

            var actionResult = await _controller.Create(dto);

            var result = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
            Assert.IsType<ValidationProblemDetails>(result.Value);
            Assert.Equal(1, _controller.ModelState.ErrorCount);
            Assert.Equal("El título es obligatorio", _controller.ModelState["Title"]!.Errors[0].ErrorMessage);
        }
        // ===================== Update =====================

        // Caso 1 - Camino: I,1,2,3,F
        [Fact]
        public async Task Update_BookNotFound_ReturnsNotFound()
        {
            const int id = 999;
            var dto = new UpdateBookDto();

            _bookServiceMock
                .Setup(s => s.UpdateAsync((uint)id, dto))
                .ReturnsAsync((BookDto?)null);

            var actionResult = await _controller.Update(id, dto);

            var notFound = Assert.IsType<NotFoundResult>(actionResult.Result);
            Assert.Equal(404, notFound.StatusCode);
            _bookServiceMock.Verify(s => s.UpdateAsync((uint)id, dto), Times.Once);
        }

        // Caso 2 - Camino: I,1,2,4,F
        [Fact]
        public async Task Update_BookUpdated_ReturnsOkWithBook()
        {
            const int id = 1;
            var dto = new UpdateBookDto();
            var expected = new BookDto { Id = id, Title = "Libro actualizado", IsActive = true };

            _bookServiceMock
                .Setup(s => s.UpdateAsync((uint)id, dto))
                .ReturnsAsync(expected);

            var actionResult = await _controller.Update(id, dto);

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(200, ok.StatusCode);
            Assert.Same(expected, ok.Value);
            _bookServiceMock.Verify(s => s.UpdateAsync((uint)id, dto), Times.Once);
        }

        // Caso 3 - Camino: I,1,5,10,12,F  (excepción no controlada: se propaga)
        [Fact]
        public async Task Update_UnhandledException_PropagatesException()
        {
            const int id = 1;
            var dto = new UpdateBookDto();

            _bookServiceMock
                .Setup(s => s.UpdateAsync((uint)id, dto))
                .ThrowsAsync(new Exception("Error inesperado"));

            var ex = await Assert.ThrowsAsync<Exception>(() => _controller.Update(id, dto));
            Assert.Equal("Error inesperado", ex.Message);
        }

        // Caso 4 - Camino: I,1,5,10,12,13,F
        [Fact]
        public async Task Update_InvalidOperationException_ReturnsConflictWithMessage()
        {
            const int id = 1;
            var dto = new UpdateBookDto();

            _bookServiceMock
                .Setup(s => s.UpdateAsync((uint)id, dto))
                .ThrowsAsync(new InvalidOperationException("Ya existe un libro activo con ese ISBN."));

            var actionResult = await _controller.Update(id, dto);

            var conflict = Assert.IsType<ConflictObjectResult>(actionResult.Result);
            Assert.Equal(409, conflict.StatusCode);
            Assert.Equal("Ya existe un libro activo con ese ISBN.", GetProp(conflict.Value, "message") as string);
        }

        // Caso 5 - Camino: I,1,5,10,11,F
        [Fact]
        public async Task Update_ArgumentException_ReturnsValidationProblemWithMessage()
        {
            const int id = 1;
            var dto = new UpdateBookDto();

            _bookServiceMock
                .Setup(s => s.UpdateAsync((uint)id, dto))
                .ThrowsAsync(new ArgumentException("Datos inválidos."));

            var actionResult = await _controller.Update(id, dto);

            var result = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
            Assert.IsType<ValidationProblemDetails>(result.Value);
            Assert.Equal(1, _controller.ModelState.ErrorCount);
            Assert.Equal("Datos inválidos.", _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        }

        // Caso 6 - Camino: I,1,5,6,9,F  (Errors vacío: 0 iteraciones del foreach externo)
        [Fact]
        public async Task Update_BookValidationException_EmptyErrors_ReturnsValidationProblemWithoutErrors()
        {
            const int id = 1;
            var dto = new UpdateBookDto();
            var errors = new Dictionary<string, string[]>();

            _bookServiceMock
                .Setup(s => s.UpdateAsync((uint)id, dto))
                .ThrowsAsync(new BookValidationException(errors));

            var actionResult = await _controller.Update(id, dto);

            var result = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
            Assert.IsType<ValidationProblemDetails>(result.Value);
            Assert.Equal(0, _controller.ModelState.ErrorCount);
        }

        // Caso 7 - Camino: I,1,5,6,7,6,9,F  (1 clave sin mensajes: foreach interno con 0 iteraciones)
        [Fact]
        public async Task Update_BookValidationException_KeyWithoutMessages_ReturnsValidationProblemWithoutErrors()
        {
            const int id = 1;
            var dto = new UpdateBookDto();
            var errors = new Dictionary<string, string[]> { ["Title"] = new string[0] };

            _bookServiceMock
                .Setup(s => s.UpdateAsync((uint)id, dto))
                .ThrowsAsync(new BookValidationException(errors));

            var actionResult = await _controller.Update(id, dto);

            var result = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
            Assert.IsType<ValidationProblemDetails>(result.Value);
            Assert.Equal(0, _controller.ModelState.ErrorCount);
        }

        // Caso 8 - Camino: I,1,5,6,7,8,7,6,9,F  (1 clave con 1 mensaje)
        [Fact]
        public async Task Update_BookValidationException_KeyWithMessage_ReturnsValidationProblemWithError()
        {
            const int id = 1;
            var dto = new UpdateBookDto();
            var errors = new Dictionary<string, string[]>
            {
                ["Title"] = new[] { "El título es obligatorio" }
            };

            _bookServiceMock
                .Setup(s => s.UpdateAsync((uint)id, dto))
                .ThrowsAsync(new BookValidationException(errors));

            var actionResult = await _controller.Update(id, dto);

            var result = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
            Assert.IsType<ValidationProblemDetails>(result.Value);
            Assert.Equal(1, _controller.ModelState.ErrorCount);
            Assert.Equal("El título es obligatorio", _controller.ModelState["Title"]!.Errors[0].ErrorMessage);
        }
        // ===================== Delete =====================

        // Caso 1 - Camino: I,1,2,F
        [Fact]
        public async Task Delete_BookNotFound_ReturnsNotFound()
        {
            const int id = 999;

            _bookServiceMock
                .Setup(s => s.DeleteAsync((uint)id))
                .ReturnsAsync(false);

            var actionResult = await _controller.Delete(id);

            var notFound = Assert.IsType<NotFoundResult>(actionResult);
            Assert.Equal(404, notFound.StatusCode);
            _bookServiceMock.Verify(s => s.DeleteAsync((uint)id), Times.Once);
        }

        // Caso 2 - Camino: I,1,F
        [Fact]
        public async Task Delete_BookDeleted_ReturnsNoContent()
        {
            const int id = 1;

            _bookServiceMock
                .Setup(s => s.DeleteAsync((uint)id))
                .ReturnsAsync(true);

            var actionResult = await _controller.Delete(id);

            var noContent = Assert.IsType<NoContentResult>(actionResult);
            Assert.Equal(204, noContent.StatusCode);
            _bookServiceMock.Verify(s => s.DeleteAsync((uint)id), Times.Once);
        }
        // ===================== Search =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task Search_ReturnsOkWithBooks()
        {
            const string phrase = "libro";
            IEnumerable<BookDto> expected = new List<BookDto>
            {
                new BookDto { Id = 1, Title = "Libro de prueba", IsActive = true }
            };

            _bookServiceMock
                .Setup(s => s.SearchAsync(phrase))
                .ReturnsAsync(expected);

            var actionResult = await _controller.Search(phrase);

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(200, ok.StatusCode);
            Assert.Same(expected, ok.Value);
            _bookServiceMock.Verify(s => s.SearchAsync(phrase), Times.Once);
        }

        // ===================== GetAvailable =====================

        // Caso 1 - Camino: I,F
        [Fact]
        public async Task GetAvailable_ReturnsOkWithBooks()
        {
            IEnumerable<BookDto> expected = new List<BookDto>
            {
                new BookDto { Id = 1, Title = "Libro disponible", AvailableCopies = 2, IsActive = true }
            };

            _bookServiceMock
                .Setup(s => s.GetAvailableAsync())
                .ReturnsAsync(expected);

            var actionResult = await _controller.GetAvailable();

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(200, ok.StatusCode);
            Assert.Same(expected, ok.Value);
            _bookServiceMock.Verify(s => s.GetAvailableAsync(), Times.Once);
        }

        // ===================== AddCopy =====================

        // Caso 1 - Camino: I,1,2,F
        [Fact]
        public async Task AddCopy_ServiceReturnsError_ReturnsBadRequestWithMessage()
        {
            const int id = 1;
            const string error = "Ya existe una copia con ese código interno.";
            var request = new BookController.AddCopyRequest("COD-001");

            _bookServiceMock
                .Setup(s => s.AddCopyAsync((uint)id, request.InternalCode))
                .ReturnsAsync(error);

            var actionResult = await _controller.AddCopy(id, request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
            Assert.Equal(400, badRequest.StatusCode);
            Assert.Equal(error, GetProp(badRequest.Value, "message") as string);
            _bookServiceMock.Verify(s => s.AddCopyAsync((uint)id, request.InternalCode), Times.Once);
        }

        // Caso 2 - Camino: I,1,F
        [Fact]
        public async Task AddCopy_ServiceReturnsNull_ReturnsOkWithMessage()
        {
            const int id = 1;
            var request = new BookController.AddCopyRequest("COD-001");

            _bookServiceMock
                .Setup(s => s.AddCopyAsync((uint)id, request.InternalCode))
                .ReturnsAsync((string?)null);

            var actionResult = await _controller.AddCopy(id, request);

            var ok = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal(200, ok.StatusCode);
            Assert.Equal("Copia agregada correctamente.", GetProp(ok.Value, "message") as string);
            _bookServiceMock.Verify(s => s.AddCopyAsync((uint)id, request.InternalCode), Times.Once);
        }
    }
}