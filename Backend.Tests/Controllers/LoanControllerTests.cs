using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Application.DTOs;
using Backend.Application.Interfaces;
using Backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class LoansControllerTests
{
    private readonly Mock<ILoanService> _loanServiceMock;
    private readonly LoansController _controller;

    public LoansControllerTests()
    {
        _loanServiceMock = new Mock<ILoanService>();
        _controller = new LoansController(_loanServiceMock.Object);
    }

    // Lee una propiedad de un tipo anónimo (body de BadRequest/Ok) por reflexión
    private static object? GetProp(object? body, string name)
    {
        Assert.NotNull(body);
        var prop = body!.GetType().GetProperty(name);
        Assert.NotNull(prop);
        return prop!.GetValue(body);
    }

    // ===================== RegisterReturn =====================

    // Caso 1 - Camino: I,1,2,F
    [Fact]
    public async Task RegisterReturn_ServiceReturnsNull_ReturnsBadRequestWithMessage()
    {
        const int copyId = 999;
        const bool confirm = true;

        _loanServiceMock
            .Setup(s => s.RegisterReturnAsync((uint)copyId, confirm))
            .ReturnsAsync((ReturnResultDto?)null);

        var actionResult = await _controller.RegisterReturn(copyId, confirm);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(
            "No se pudo procesar la operación. La copia no existe o no está prestada.",
            GetProp(badRequest.Value, "message") as string);
        _loanServiceMock.Verify(s => s.RegisterReturnAsync((uint)copyId, confirm), Times.Once);
    }

    // Caso 2 - Camino: I,1,F
    [Fact]
    public async Task RegisterReturn_ServiceReturnsResult_ReturnsOkWithResult()
    {
        const int copyId = 1;
        const bool confirm = true;
        var expected = new ReturnResultDto(); // <-- reemplaza por el tipo real que devuelve RegisterReturnAsync

        _loanServiceMock
            .Setup(s => s.RegisterReturnAsync((uint)copyId, confirm))
            .ReturnsAsync(expected);

        var actionResult = await _controller.RegisterReturn(copyId, confirm);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, ok.StatusCode);
        Assert.Same(expected, ok.Value);
        _loanServiceMock.Verify(s => s.RegisterReturnAsync((uint)copyId, confirm), Times.Once);
    }

    // ===================== RegisterLoan =====================

    // Caso 1 - Camino: I,1,2,F
    [Fact]
    public async Task RegisterLoan_BorrowerIdInvalid_ReturnsBadRequestWithoutCallingService()
    {
        const int bookId = 1;
        const int borrowerId = 0;

        var actionResult = await _controller.RegisterLoan(bookId, borrowerId);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(
            "Debe indicar borrowerId como parámetro de consulta.",
            GetProp(badRequest.Value, "message") as string);
        _loanServiceMock.Verify(
            s => s.RegisterLoanAsync(
                It.IsAny<uint>(), It.IsAny<IEnumerable<uint>>(), It.IsAny<uint>(), It.IsAny<int>()),
            Times.Never);
    }

    // Caso 2 - Camino: I,1,3,4,5,F
    [Fact]
    public async Task RegisterLoan_ServiceFails_ReturnsBadRequestWithServiceError()
    {
        const int bookId = 1;
        const int borrowerId = 5;
        const string error = "El libro no está disponible";

        _loanServiceMock
            .Setup(s => s.RegisterLoanAsync(
                (uint)borrowerId,
                It.Is<IEnumerable<uint>>(b => b.Count() == 1 && b.First() == (uint)bookId),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .ReturnsAsync((false, error, 0));

        var actionResult = await _controller.RegisterLoan(bookId, borrowerId);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(error, GetProp(badRequest.Value, "message") as string);
    }

    // Caso 3 - Camino: I,1,3,4,F
    [Fact]
    public async Task RegisterLoan_ServiceSucceeds_ReturnsOkWithLoanId()
    {
        const int bookId = 1;
        const int borrowerId = 5;

        _loanServiceMock
            .Setup(s => s.RegisterLoanAsync(
                (uint)borrowerId,
                It.Is<IEnumerable<uint>>(b => b.Count() == 1 && b.First() == (uint)bookId),
                It.IsAny<uint>(),
                It.IsAny<int>()))
            .ReturnsAsync((true, null, 10));

        var actionResult = await _controller.RegisterLoan(bookId, borrowerId);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, ok.StatusCode);
        Assert.Equal("Préstamo registrado.", GetProp(ok.Value, "message") as string);
        Assert.Equal(10, System.Convert.ToInt32(GetProp(ok.Value, "loanId")));
    }
    // ===================== Create =====================

    // Caso 1 - Camino: I,1,4,F
    [Fact]
    public async Task Create_RequestNull_ReturnsBadRequest()
    {
        var actionResult = await _controller.Create(null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(
            "Solicitud inválida. Se requieren BorrowerId y al menos un BookId.",
            GetProp(badRequest.Value, "message") as string);
        _loanServiceMock.Verify(
            s => s.RegisterLoanAsync(
                It.IsAny<uint>(), It.IsAny<IEnumerable<uint>>(), It.IsAny<uint>(), It.IsAny<int>()),
            Times.Never);
    }

    // Caso 2 - Camino: I,1,2,4,F
    [Fact]
    public async Task Create_BookIdsNull_ReturnsBadRequest()
    {
        var request = new LoanCreateRequest { BorrowerId = 1, BookIds = null! };

        var actionResult = await _controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(
            "Solicitud inválida. Se requieren BorrowerId y al menos un BookId.",
            GetProp(badRequest.Value, "message") as string);
        _loanServiceMock.Verify(
            s => s.RegisterLoanAsync(
                It.IsAny<uint>(), It.IsAny<IEnumerable<uint>>(), It.IsAny<uint>(), It.IsAny<int>()),
            Times.Never);
    }

    // Caso 3 - Camino: I,1,2,3,4,F
    [Fact]
    public async Task Create_BookIdsEmpty_ReturnsBadRequest()
    {
        var request = new LoanCreateRequest { BorrowerId = 1, BookIds = new List<uint>() };

        var actionResult = await _controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(
            "Solicitud inválida. Se requieren BorrowerId y al menos un BookId.",
            GetProp(badRequest.Value, "message") as string);
        _loanServiceMock.Verify(
            s => s.RegisterLoanAsync(
                It.IsAny<uint>(), It.IsAny<IEnumerable<uint>>(), It.IsAny<uint>(), It.IsAny<int>()),
            Times.Never);
    }

    // Caso 4 - Camino: I,1,2,3,5,6,7,F  (LibrarianId=7 -> se usa 7)
    [Fact]
    public async Task Create_ServiceFails_ReturnsBadRequestWithServiceError()
    {
        const string error = "El libro no está disponible";
        var request = new LoanCreateRequest
        {
            BorrowerId = 1,
            BookIds = new List<uint> { 1 },
            LibrarianId = 7,
            LoanDays = 14
        };

        _loanServiceMock
            .Setup(s => s.RegisterLoanAsync(request.BorrowerId, request.BookIds, 7, request.LoanDays))
            .ReturnsAsync((false, error, 0));

        var actionResult = await _controller.Create(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal(error, GetProp(badRequest.Value, "message") as string);
        _loanServiceMock.Verify(
            s => s.RegisterLoanAsync(request.BorrowerId, request.BookIds, 7, request.LoanDays),
            Times.Once);
    }

    // Caso 5 - Camino: I,1,2,3,5,6,F  (LibrarianId=null -> se usa 1)
    [Fact]
    public async Task Create_ServiceSucceeds_ReturnsOkWithLoanId()
    {
        var request = new LoanCreateRequest
        {
            BorrowerId = 1,
            BookIds = new List<uint> { 1 },
            LibrarianId = null,
            LoanDays = 14
        };

        _loanServiceMock
            .Setup(s => s.RegisterLoanAsync(request.BorrowerId, request.BookIds, 1, request.LoanDays))
            .ReturnsAsync((true, null, 10));

        var actionResult = await _controller.Create(request);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, ok.StatusCode);
        Assert.Equal("Préstamo creado", GetProp(ok.Value, "message") as string);
        Assert.Equal(10, System.Convert.ToInt32(GetProp(ok.Value, "loanId")));
        _loanServiceMock.Verify(
            s => s.RegisterLoanAsync(request.BorrowerId, request.BookIds, 1, request.LoanDays),
            Times.Once);
    }
    // ===================== GetById =====================

    // Caso 1 - Camino: I,1,2,F
    [Fact]
    public async Task GetById_LoanNotFound_ReturnsNotFound()
    {
        const int id = 999;

        _loanServiceMock
            .Setup(s => s.GetLoanByIdAsync((uint)id))
            .ReturnsAsync((LoanDto?)null);

        var actionResult = await _controller.GetById(id);

        var notFound = Assert.IsType<NotFoundResult>(actionResult);
        Assert.Equal(404, notFound.StatusCode);
        _loanServiceMock.Verify(s => s.GetLoanByIdAsync((uint)id), Times.Once);
    }

    // Caso 2 - Camino: I,1,F
    [Fact]
    public async Task GetById_LoanFound_ReturnsOkWithLoan()
    {
        const int id = 1;
        var expected = new LoanDto
        {
            Id = id,
            BorrowerId = 5,
            BorrowerName = "Juan Perez",
            LibrarianId = 1,
            LibrarianName = "Ana Lopez",
            Status = "Active",
            StatusDisplayName = "Activo"
        };

        _loanServiceMock
            .Setup(s => s.GetLoanByIdAsync((uint)id))
            .ReturnsAsync(expected);

        var actionResult = await _controller.GetById(id);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, ok.StatusCode);
        Assert.Same(expected, ok.Value);
        _loanServiceMock.Verify(s => s.GetLoanByIdAsync((uint)id), Times.Once);
    }
    // ===================== UpdateLoan =====================

    // Caso 1 - Camino: I,1,2,F
    [Fact]
    public async Task UpdateLoan_LoanNotFound_ReturnsNotFoundWithMessage()
    {
        const int id = 999;
        var dto = new UpdateLoanDto
        {
            DueDate = new System.DateTime(2026, 12, 31),
            Status = "Active"
        };

        _loanServiceMock
            .Setup(s => s.UpdateLoanAsync((uint)id, dto))
            .ReturnsAsync(false);

        var actionResult = await _controller.UpdateLoan(id, dto);

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(404, notFound.StatusCode);
        Assert.Equal("Préstamo no encontrado.", GetProp(notFound.Value, "message") as string);
        _loanServiceMock.Verify(s => s.UpdateLoanAsync((uint)id, dto), Times.Once);
    }

    // Caso 2 - Camino: I,1,F
    [Fact]
    public async Task UpdateLoan_LoanUpdated_ReturnsOkWithMessage()
    {
        const int id = 1;
        var dto = new UpdateLoanDto
        {
            DueDate = new System.DateTime(2026, 12, 31),
            Status = "Active"
        };

        _loanServiceMock
            .Setup(s => s.UpdateLoanAsync((uint)id, dto))
            .ReturnsAsync(true);

        var actionResult = await _controller.UpdateLoan(id, dto);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, ok.StatusCode);
        Assert.Equal("Préstamo actualizado exitosamente.", GetProp(ok.Value, "message") as string);
        _loanServiceMock.Verify(s => s.UpdateLoanAsync((uint)id, dto), Times.Once);
    }
}