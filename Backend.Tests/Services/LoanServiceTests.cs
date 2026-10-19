using Backend.Application.DTOs;
using Backend.Application.Services;
using Backend.Domain.Interfaces;
using Backend.Models;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;


namespace Backend.Tests.Services
{
    public class LoanServiceTests
    {
        public class RegisterReturnTests
        {
            // Valores de las constantes privadas de LoanService.
            // Se declaran const para que se conviertan implícitamente al tipo real de StatusId.
            private const int AvailableStatus = 1;
            private const int LoanedStatus = 2;

            private readonly Mock<ILoanRepository> _repo = new();
            private readonly Mock<IConfiguration> _config = new();
            private readonly LoanService _sut;

            public RegisterReturnTests()
            {
                _config.Setup(c => c["Fine:DailyRate"]).Returns("10");
                _sut = new LoanService(_repo.Object, _config.Object);
            }

            // ---------- Helpers ----------

            private static Copy BuildAvailableCopy(uint id) =>
                new Copy { Id = id, StatusId = AvailableStatus };

            private static Copy BuildLoanedCopy(uint id) =>
                new Copy
                {
                    Id = id,
                    StatusId = LoanedStatus,
                    Book = new Book { Title = "Cien años de soledad" }
                };

            private static Loanitem BuildLoanItem(Copy copy, string status) =>
                new Loanitem { CopyId = copy.Id, Copy = copy, Status = status };

            /// <summary>Préstamo vigente (vence en 7 días) => 0 días de mora.</summary>
            private static Loan BuildLoan(uint id, params Loanitem[] items) =>
                new Loan
                {
                    Id = id,
                    DueDate = DateTime.Now.AddDays(7),
                    Status = "Active",
                    Borrower = new User { FirstName = "Ana", LastName = "Pérez" },
                    Loanitems = new List<Loanitem>(items)
                };

            // ---------- Camino 1: I,1,2,F ----------
            [Fact]
            public async Task Camino1_CopiaNoExiste_RetornaNull()
            {
                _repo.Setup(r => r.GetCopyByIdAsync(999)).ReturnsAsync((Copy?)null);

                var result = await _sut.RegisterReturnAsync(999, confirm: false);

                Assert.Null(result);
                _repo.Verify(r => r.GetCopyByIdAsync(999), Times.Once);
                _repo.VerifyNoOtherCalls();
            }

            // ---------- Camino 2: I,1,3,4,F ----------
            [Fact]
            public async Task Camino2_CopiaNoPrestada_RetornaFallo()
            {
                _repo.Setup(r => r.GetCopyByIdAsync(1)).ReturnsAsync(BuildAvailableCopy(1));

                var result = await _sut.RegisterReturnAsync(1, confirm: false);

                Assert.NotNull(result);
                Assert.False(result!.HasFine);
                Assert.Equal("La copia no está prestada.", result.Message);
                _repo.Verify(r => r.GetCopyByIdAsync(1), Times.Once);
                _repo.VerifyNoOtherCalls();
            }

            // ---------- Camino 3: I,1,3,5,6,7,F ----------
            [Fact]
            public async Task Camino3_SinPrestamoActivo_RetornaFallo()
            {
                _repo.Setup(r => r.GetCopyByIdAsync(2)).ReturnsAsync(BuildLoanedCopy(2));
                _repo.Setup(r => r.GetActiveLoanByCopyIdAsync(2)).ReturnsAsync((Loan?)null);

                var result = await _sut.RegisterReturnAsync(2, confirm: false);

                Assert.NotNull(result);
                Assert.False(result!.HasFine);
                Assert.Equal("No se encontró un préstamo activo para esta copia.", result.Message);
                _repo.Verify(r => r.GetCopyByIdAsync(2), Times.Once);
                _repo.Verify(r => r.GetActiveLoanByCopyIdAsync(2), Times.Once);
                _repo.VerifyNoOtherCalls();
            }

            // ---------- Camino 4: I,1,3,5,6,8,9,10,F ----------
            [Fact]
            public async Task Camino4_EjemplarNoPendiente_RetornaFallo()
            {
                // El ítem de la copia 3 ya fue devuelto => FindPendingLoanItem retorna null
                var copy = BuildLoanedCopy(3);
                var loan = BuildLoan(10, BuildLoanItem(copy, "Returned"));
                _repo.Setup(r => r.GetCopyByIdAsync(3)).ReturnsAsync(copy);
                _repo.Setup(r => r.GetActiveLoanByCopyIdAsync(3)).ReturnsAsync(loan);

                var result = await _sut.RegisterReturnAsync(3, confirm: false);

                Assert.NotNull(result);
                Assert.False(result!.HasFine);
                Assert.Equal("El ejemplar no está pendiente de devolución.", result.Message);
                _repo.Verify(r => r.GetFineByLoanCopyAsync(It.IsAny<uint>(), It.IsAny<uint>()), Times.Never);
                _repo.Verify(r => r.BeginTransactionAsync(), Times.Never);
            }

            // ---------- Camino 5: I,1,3,5,6,8,9,11,12,13,F ----------
            [Fact]
            public async Task Camino5_ConfirmFalse_RetornaVistaPrevia_SinPersistir()
            {
                var copy = BuildLoanedCopy(4);
                var loan = BuildLoan(11, BuildLoanItem(copy, "Active"));
                _repo.Setup(r => r.GetCopyByIdAsync(4)).ReturnsAsync(copy);
                _repo.Setup(r => r.GetActiveLoanByCopyIdAsync(4)).ReturnsAsync(loan);
                _repo.Setup(r => r.GetFineByLoanCopyAsync(11, 4)).ReturnsAsync((Fine?)null);

                var result = await _sut.RegisterReturnAsync(4, confirm: false);

                // Sin mora => BuildReturnPreview responde "Devolución sin multa."
                Assert.NotNull(result);
                Assert.False(result!.HasFine);
                Assert.Equal("Devolución sin multa.", result.Message);

                // La vista previa no modifica entidades ni escribe en el repositorio
                Assert.Equal(LoanedStatus, (int)copy.StatusId);
                Assert.Equal("Active", loan.Loanitems.First().Status);
                _repo.Verify(r => r.GetFineByLoanCopyAsync(11, 4), Times.Once);
                _repo.Verify(r => r.BeginTransactionAsync(), Times.Never);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
                _repo.Verify(r => r.AddFineAsync(It.IsAny<Fine>()), Times.Never);
            }

            // ---------- Camino 6: I,1,3,5,6,8,9,11,12,14,F ----------
            [Fact]
            public async Task Camino6_ConfirmTrue_RegistraDevolucionSinMulta()
            {
                var copy = BuildLoanedCopy(4);
                var item = BuildLoanItem(copy, "Active");
                var loan = BuildLoan(12, item);

                var transaction = new Mock<IDbContextTransaction>();
                _repo.Setup(r => r.GetCopyByIdAsync(4)).ReturnsAsync(copy);
                _repo.Setup(r => r.GetActiveLoanByCopyIdAsync(4)).ReturnsAsync(loan);
                _repo.Setup(r => r.GetFineByLoanCopyAsync(12, 4)).ReturnsAsync((Fine?)null);
                _repo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(transaction.Object);

                var result = await _sut.RegisterReturnAsync(4, confirm: true);

                // Resultado
                Assert.NotNull(result);
                Assert.False(result!.HasFine);
                Assert.Equal("Devolución registrada correctamente.", result.Message);
                Assert.Equal(4, result.CopyId);
                Assert.Equal(0m, result.Amount);
                Assert.Equal(0, result.OverdueDays);
                Assert.Equal("Ana Pérez", result.BorrowerName);
                Assert.Equal("Cien años de soledad", result.BookTitle);

                // Entidades actualizadas por ApplyReturnToEntities
                Assert.Equal(AvailableStatus, (int)copy.StatusId);
                Assert.Equal("Returned", item.Status);
                Assert.NotNull(item.ReturnedAt);
                Assert.Equal("Completed", loan.Status);

                // Persistencia y transacción
                _repo.Verify(r => r.AddFineAsync(It.IsAny<Fine>()), Times.Never);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
                transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
                transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
            }
        }




        public class RegisterLoanTests
        {
            // Valores de las constantes privadas de LoanService.
            private const int AvailableStatus = 1;
            private const int LoanedStatus = 2;

            private readonly Mock<ILoanRepository> _repo = new();
            private readonly Mock<IConfiguration> _config = new();
            private readonly Mock<IDbContextTransaction> _transaction = new();
            private readonly LoanService _sut;

            public RegisterLoanTests()
            {
                _repo.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(_transaction.Object);
                _sut = new LoanService(_repo.Object, _config.Object);
            }

            // ---------- Camino 1: I,1,F ----------
            [Fact]
            public async Task Camino1_ValidacionFalla_RetornaError()
            {
                // Arrange: borrowerId = 0 => ValidateLoanRequest devuelve error
                var bookIds = new List<uint> { 1 };

                // Act
                var result = await _sut.RegisterLoanAsync(0, bookIds, 10, 14);

                // Assert
                Assert.False(result.Success);
                Assert.Equal("BorrowerId inválido.", result.Error);
                Assert.Null(result.LoanId);

                // No se debe abrir transacción ni tocar el repositorio
                _repo.Verify(r => r.BeginTransactionAsync(), Times.Never);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
                _repo.Verify(r => r.AddLoanAsync(It.IsAny<Loan>()), Times.Never);
            }

            // ---------- Camino 2: I,1,2,3,4,5,F ----------
            [Fact]
            public async Task Camino2_CopiaNoDisponible_HaceRollbackYRetornaError()
            {
                // Arrange: no hay copia disponible para el libro 101
                var bookIds = new List<uint> { 101 };
                _repo.Setup(r => r.GetFirstAvailableCopyByBookIdForUpdateAsync(101))
                     .ReturnsAsync((Copy?)null);

                // Act
                var result = await _sut.RegisterLoanAsync(20, bookIds, 10, 14);

                // Assert
                Assert.False(result.Success);
                Assert.Equal("No hay copias disponibles para: 101", result.Error);
                Assert.Null(result.LoanId);

                _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
                _repo.Verify(r => r.AddLoanAsync(It.IsAny<Loan>()), Times.Never);
            }

            // ---------- Camino 3: I,1,2,3,4,6,7,F ----------
            [Fact]
            public async Task Camino3_TodoCorrecto_CreaPrestamoYHaceCommit()
            {
                // Arrange
                var bookIds = new List<uint> { 101 };
                var copy = new Copy { Id = 500, StatusId = AvailableStatus };
                Loan? savedLoan = null;

                _repo.Setup(r => r.GetFirstAvailableCopyByBookIdForUpdateAsync(101))
                     .ReturnsAsync(copy);
                _repo.Setup(r => r.AddLoanAsync(It.IsAny<Loan>()))
                     .Callback<Loan>(loan =>
                     {
                         loan.Id = 42;      // simula el Id generado por la BD
                         savedLoan = loan;
                     });

                // Act
                var result = await _sut.RegisterLoanAsync(20, bookIds, 10, 14);

                // Assert: resultado
                Assert.True(result.Success);
                Assert.Null(result.Error);
                Assert.Equal(42, result.LoanId);

                // Assert: la copia queda reservada como prestada
                Assert.Equal(LoanedStatus, (int)copy.StatusId);

                // Assert: el préstamo creado
                Assert.NotNull(savedLoan);
                Assert.Equal(20, (int)savedLoan!.BorrowerId);
                Assert.Equal(10, (int)savedLoan.LibrarianId);
                Assert.Equal("Active", savedLoan.Status);
                Assert.Single(savedLoan.Loanitems);
                Assert.Equal(500u, savedLoan.Loanitems.First().CopyId);
                Assert.True(savedLoan.DueDate > savedLoan.LoanDate);

                // Assert: persistencia y transacción
                _repo.Verify(r => r.AddLoanAsync(It.IsAny<Loan>()), Times.Once);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
                _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
            }

            // ---------- Camino 4: I,1,2,3,8,F ----------
            [Fact]
            public async Task Camino4_ExcepcionEnReserva_HaceRollbackYRetornaMensaje()
            {
                // Arrange: ReserveCopiesAsync lanza una excepción
                var bookIds = new List<uint> { 101 };
                _repo.Setup(r => r.GetFirstAvailableCopyByBookIdForUpdateAsync(101))
                     .ThrowsAsync(new Exception("Error de prueba"));

                // Act
                var result = await _sut.RegisterLoanAsync(20, bookIds, 10, 14);

                // Assert
                Assert.False(result.Success);
                Assert.Equal("Error de prueba", result.Error);
                Assert.Null(result.LoanId);

                _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
                _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
                _repo.Verify(r => r.AddLoanAsync(It.IsAny<Loan>()), Times.Never);
            }
        }








        public class DetermineLoanStatusTests
        {
            // Valores de las constantes privadas de LoanService
            private const string ActiveStatus = "Active";
            private const string OverdueStatus = "Overdue";
            private const string ReturnedStatus = "Returned";
            private const string CompletedStatus = "Completed";

            private static readonly DateTime ReferenceDate = new DateTime(2026, 10, 15);

            // ---------- Helpers ----------

            private static string InvokeDetermineLoanStatus(Loan loan, DateTime referenceDate)
            {
                var method = typeof(LoanService).GetMethod(
                    "DetermineLoanStatus",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(method);

                return (string)method!.Invoke(null, new object[] { loan, referenceDate })!;
            }

            private static Loan BuildLoan(DateTime dueDate, params string[] itemStatuses)
            {
                var items = new List<Loanitem>();
                foreach (var status in itemStatuses)
                {
                    items.Add(new Loanitem { Status = status });
                }

                return new Loan
                {
                    DueDate = dueDate,
                    Loanitems = items
                };
            }

            // ---------- Caso 1: I,1,2,F ----------
            [Fact]
            public void Caso1_TodosDevueltos_RetornaCompleted()
            {
                var loan = BuildLoan(new DateTime(2026, 10, 10), ReturnedStatus);

                var result = InvokeDetermineLoanStatus(loan, ReferenceDate);

                Assert.Equal(CompletedStatus, result);
            }

            // ---------- Caso 2: I,1,3,5,6,9,F ----------
            [Fact]
            public void Caso2_ItemActivoYVencimientoFuturo_RetornaActive()
            {
                var loan = BuildLoan(new DateTime(2026, 10, 20), ActiveStatus);

                var result = InvokeDetermineLoanStatus(loan, ReferenceDate);

                Assert.Equal(ActiveStatus, result);
            }

            // ---------- Caso 3: I,1,3,4,5,6,8,F ----------
            [Fact]
            public void Caso3_ItemOverdueYVencimientoPasado_RetornaOverdue()
            {
                var loan = BuildLoan(new DateTime(2026, 10, 10), OverdueStatus);

                var result = InvokeDetermineLoanStatus(loan, ReferenceDate);

                Assert.Equal(OverdueStatus, result);
            }

            // ---------- Caso 4: I,1,3,5,9,F ----------
            [Fact]
            public void Caso4_VencimientoIgualAReferencia_RetornaActive()
            {
                // DueDate.Date == referenceDate.Date => dueDatePassed = false
                var loan = BuildLoan(new DateTime(2026, 10, 15), ActiveStatus);

                var result = InvokeDetermineLoanStatus(loan, ReferenceDate);

                Assert.Equal(ActiveStatus, result);
            }

            // ---------- Caso 5: I,1,3,5,6,9,F ----------
            [Fact]
            public void Caso5_ItemConOtroEstadoYVencimientoPasado_RetornaActive()
            {
                // Sin ítems Active/Overdue => hasPendingItems = false
                var loan = BuildLoan(new DateTime(2026, 10, 10), "Other");

                var result = InvokeDetermineLoanStatus(loan, ReferenceDate);

                Assert.Equal(ActiveStatus, result);
            }

            // ---------- Caso 6: I,1,3,5,6,8,F ----------
            [Fact]
            public void Caso6_ItemActivoYVencimientoPasado_RetornaOverdue()
            {
                var loan = BuildLoan(new DateTime(2026, 10, 10), ActiveStatus);

                var result = InvokeDetermineLoanStatus(loan, ReferenceDate);

                Assert.Equal(OverdueStatus, result);
            }
        }









        public class CountOverdueBusinessDaysTests
        {
            // ---------- Helper ----------

            private static int InvokeCountOverdueBusinessDays(DateTime dueDate, DateTime returnDate)
            {
                var method = typeof(LoanService).GetMethod(
                    "CountOverdueBusinessDays",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(method);

                return (int)method!.Invoke(null, new object[] { dueDate, returnDate })!;
            }

            // ---------- Caso 1: I,1,2,F ----------
            [Fact]
            public void Caso1_DevolucionElMismoDiaDelVencimiento_Retorna0()
            {
                // start = 16/oct, end = 15/oct => end < start
                var dueDate = new DateTime(2026, 10, 15);
                var returnDate = new DateTime(2026, 10, 15);

                var result = InvokeCountOverdueBusinessDays(dueDate, returnDate);

                Assert.Equal(0, result);
            }

            // ---------- Caso 2: I,1,3,4,5,8,4,F ----------
            [Fact]
            public void Caso2_UnSoloDiaSabado_Retorna0()
            {
                // start = end = sábado 17/oct => date.DayOfWeek == Saturday
                var dueDate = new DateTime(2026, 10, 16);
                var returnDate = new DateTime(2026, 10, 17);

                var result = InvokeCountOverdueBusinessDays(dueDate, returnDate);

                Assert.Equal(0, result);
            }

            // ---------- Caso 3: I,1,3,4,5,6,8,4,F ----------
            [Fact]
            public void Caso3_UnSoloDiaDomingo_Retorna0()
            {
                // start = end = domingo 18/oct => date.DayOfWeek == Sunday
                var dueDate = new DateTime(2026, 10, 17);
                var returnDate = new DateTime(2026, 10, 18);

                var result = InvokeCountOverdueBusinessDays(dueDate, returnDate);

                Assert.Equal(0, result);
            }

            // ---------- Caso 4: I,1,3,4,5,6,7,8,4,F ----------
            [Fact]
            public void Caso4_UnSoloDiaLunes_Retorna1()
            {
                // start = end = lunes 19/oct => día hábil, count++
                var dueDate = new DateTime(2026, 10, 18);
                var returnDate = new DateTime(2026, 10, 19);

                var result = InvokeCountOverdueBusinessDays(dueDate, returnDate);

                Assert.Equal(1, result);
            }

            // ---------- Caso 5: recorrido de viernes a lunes ----------
            [Fact]
            public void Caso5_ViernesASabadoDomingoYLunes_Retorna2()
            {
                // Recorre vie (cuenta), sáb (no), dom (no), lun (cuenta)
                var dueDate = new DateTime(2026, 10, 15);
                var returnDate = new DateTime(2026, 10, 19);

                var result = InvokeCountOverdueBusinessDays(dueDate, returnDate);

                Assert.Equal(2, result);
            }
        }















        public class AddBusinessDaysTests
        {
            // ---------- Helper ----------

            private static DateTime InvokeAddBusinessDays(DateTime start, int businessDays)
            {
                var method = typeof(LoanService).GetMethod(
                    "AddBusinessDays",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(method);

                return (DateTime)method!.Invoke(null, new object[] { start, businessDays })!;
            }

            // ---------- Caso 1: I,1,F ----------
            [Fact]
            public void Caso1_CeroDiasHabiles_RetornaLaMismaFecha()
            {
                // added < businessDays es false desde el inicio
                var start = new DateTime(2026, 10, 15); // jueves

                var result = InvokeAddBusinessDays(start, 0);

                Assert.Equal(new DateTime(2026, 10, 15), result);
                Assert.Equal(DayOfWeek.Thursday, result.DayOfWeek);
            }

            // ---------- Caso 2: I,1,2,3,4,5,1,F ----------
            [Fact]
            public void Caso2_DesdeDomingoSumaUnDiaHabil_RetornaLunes()
            {
                // domingo + 1 día = lunes (día hábil, added++)
                var start = new DateTime(2026, 10, 18); // domingo

                var result = InvokeAddBusinessDays(start, 1);

                Assert.Equal(new DateTime(2026, 10, 19), result);
                Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
            }

            // ---------- Caso 3: I,1,2,3,1,2,3,4,1,2,3,4,5,1,F ----------
            [Fact]
            public void Caso3_DesdeViernesSaltaSabadoYDomingo_RetornaLunes()
            {
                // viernes + 1: sábado (se omite), domingo (se omite), lunes (cuenta)
                var start = new DateTime(2026, 10, 16); // viernes

                var result = InvokeAddBusinessDays(start, 1);

                Assert.Equal(new DateTime(2026, 10, 19), result);
                Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
            }

            // ---------- Caso 4: I,1,2,3,4,1,2,3,4,5,1,F ----------
            [Fact]
            public void Caso4_DesdeSabadoSaltaDomingo_RetornaLunes()
            {
                // sábado + 1: domingo (se omite), lunes (cuenta)
                var start = new DateTime(2026, 10, 17); // sábado

                var result = InvokeAddBusinessDays(start, 1);

                Assert.Equal(new DateTime(2026, 10, 19), result);
                Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
            }
        }














        public class UpdateLoanTests
        {
            private const string ActiveStatus = "Active";
            private const string OverdueStatus = "Overdue";
            private const string ReturnedStatus = "Returned";

            private readonly Mock<ILoanRepository> _repo = new();
            private readonly Mock<IConfiguration> _config = new();
            private readonly LoanService _sut;

            public UpdateLoanTests()
            {
                _sut = new LoanService(_repo.Object, _config.Object);
            }

            // ---------- Caso 1: I,1,2,F ----------
            [Fact]
            public async Task Caso1_PrestamoNoExiste_RetornaFalse()
            {
                // Arrange
                var dto = new UpdateLoanDto
                {
                    DueDate = new DateTime(2026, 10, 20),
                    Status = ActiveStatus
                };
                _repo.Setup(r => r.GetLoanWithItemsByIdAsync(999)).ReturnsAsync((Loan?)null);

                // Act
                var result = await _sut.UpdateLoanAsync(999, dto);

                // Assert
                Assert.False(result);
                _repo.Verify(r => r.GetLoanWithItemsByIdAsync(999), Times.Once);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
                _repo.VerifyNoOtherCalls();
            }

            // ---------- Caso 2: I,1,3,4,5,6,F ----------
            [Fact]
            public async Task Caso2_PrestamoExiste_ActualizaSincronizaYGuarda()
            {
                // Arrange
                // Se usa una fecha relativa (en lugar de 2026-10-20 fija) para que el test
                // siga funcionando después de esa fecha: el vencimiento debe estar en el futuro.
                var newDueDate = DateTime.Now.AddDays(5);

                var overdueItem = new Loanitem { CopyId = 1, Status = OverdueStatus };
                var returnedItem = new Loanitem { CopyId = 2, Status = ReturnedStatus };

                var loan = new Loan
                {
                    Id = 1,
                    DueDate = DateTime.Now.AddDays(-10),
                    Status = OverdueStatus,
                    Loanitems = new List<Loanitem> { overdueItem, returnedItem }
                };

                var dto = new UpdateLoanDto
                {
                    DueDate = newDueDate,
                    Status = ActiveStatus
                };

                _repo.Setup(r => r.GetLoanWithItemsByIdAsync(1)).ReturnsAsync(loan);

                var before = DateTime.Now;

                // Act
                var result = await _sut.UpdateLoanAsync(1, dto);

                var after = DateTime.Now;

                // Assert: resultado
                Assert.True(result);

                // Assert: el préstamo se actualiza
                Assert.Equal(newDueDate, loan.DueDate);
                Assert.Equal(ActiveStatus, loan.Status);
                Assert.NotNull(loan.UpdatedAt);
                Assert.InRange(loan.UpdatedAt!.Value, before, after);

                // Assert: sincronización de ítems (Overdue -> Active; Returned no cambia)
                Assert.Equal(ActiveStatus, overdueItem.Status);
                Assert.Equal(ReturnedStatus, returnedItem.Status);

                // Assert: se guardan los cambios
                _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
            }
        }










        public class LoanServiceUpdateLoanStatusesTests
        {
            private const string ActiveStatus = "Active";
            private const string OverdueStatus = "Overdue";

            private static readonly DateTime OldUpdatedAt = new DateTime(2026, 1, 1);

            private readonly Mock<ILoanRepository> _repo = new();
            private readonly Mock<IConfiguration> _config = new();
            private readonly LoanService _sut;

            public LoanServiceUpdateLoanStatusesTests()
            {
                _sut = new LoanService(_repo.Object, _config.Object);
            }

            // ---------- Helpers ----------

            private async Task InvokeUpdateLoanStatusesAsync()
            {
                var method = typeof(LoanService).GetMethod(
                    "UpdateLoanStatusesAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.NotNull(method);

                await (Task)method!.Invoke(_sut, null)!;
            }

            private static Loan BuildLoan(string loanStatus, DateTime dueDate, params Loanitem[] items) =>
                new Loan
                {
                    Status = loanStatus,
                    DueDate = dueDate,
                    UpdatedAt = OldUpdatedAt,
                    Loanitems = new List<Loanitem>(items)
                };

            // ---------- Caso 1: I,1,6,F ----------
            [Fact]
            public async Task Caso1_SinPrestamos_NoGuarda()
            {
                // Arrange
                _repo.Setup(r => r.GetLoansForStatusUpdateAsync())
                     .ReturnsAsync(new List<Loan>());

                // Act
                await InvokeUpdateLoanStatusesAsync();

                // Assert
                _repo.Verify(r => r.GetLoansForStatusUpdateAsync(), Times.Once);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
                _repo.VerifyNoOtherCalls();
            }

            // ---------- Caso 2: I,1,2,3,5,1,6,F ----------
            [Fact]
            public async Task Caso2_EstadoNoCambiaYItemsNoCambian_NoGuarda()
            {
                // Préstamo Active, vencimiento futuro, ítem Active:
                //   DetermineLoanStatus => Active (igual al actual)
                //   SynchronizeItemStatuses => false (no hay ítems Overdue que pasar a Active)
                var item = new Loanitem { Status = ActiveStatus };
                var loan = BuildLoan(ActiveStatus, DateTime.Now.AddDays(5), item);
                _repo.Setup(r => r.GetLoansForStatusUpdateAsync())
                     .ReturnsAsync(new List<Loan> { loan });

                // Act
                await InvokeUpdateLoanStatusesAsync();

                // Assert
                Assert.Equal(ActiveStatus, loan.Status);
                Assert.Equal(OldUpdatedAt, loan.UpdatedAt);
                Assert.Equal(ActiveStatus, item.Status);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
            }

            // ---------- Caso 3: I,1,2,3,4,5,1,6,7,F ----------
            [Fact]
            public async Task Caso3_EstadoCambiaAOverdue_ActualizaYGuarda()
            {
                // Préstamo marcado Active con vencimiento pasado y un ítem ya Overdue:
                //   DetermineLoanStatus => Overdue (distinto al actual)
                //   SynchronizeItemStatuses => false (no hay ítems Active que pasar a Overdue)
                var item = new Loanitem { Status = OverdueStatus };
                var loan = BuildLoan(ActiveStatus, DateTime.Now.AddDays(-5), item);
                _repo.Setup(r => r.GetLoansForStatusUpdateAsync())
                     .ReturnsAsync(new List<Loan> { loan });

                var before = DateTime.Now;

                // Act
                await InvokeUpdateLoanStatusesAsync();

                var after = DateTime.Now;

                // Assert
                Assert.Equal(OverdueStatus, loan.Status);
                Assert.NotNull(loan.UpdatedAt);
                Assert.InRange(loan.UpdatedAt!.Value, before, after);
                Assert.Equal(OverdueStatus, item.Status);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
            }

            // ---------- Caso 4: I,1,2,3,5,1,6,7,F ----------
            [Fact]
            public async Task Caso4_EstadoIgualPeroItemsCambian_Guarda()
            {
                // Préstamo Active, vencimiento futuro, ítem Overdue:
                //   DetermineLoanStatus => Active (igual al actual)
                //   SynchronizeItemStatuses => true (ítem Overdue pasa a Active)
                var item = new Loanitem { Status = OverdueStatus };
                var loan = BuildLoan(ActiveStatus, DateTime.Now.AddDays(5), item);
                _repo.Setup(r => r.GetLoansForStatusUpdateAsync())
                     .ReturnsAsync(new List<Loan> { loan });

                var before = DateTime.Now;

                // Act
                await InvokeUpdateLoanStatusesAsync();

                var after = DateTime.Now;

                // Assert
                Assert.Equal(ActiveStatus, loan.Status);
                Assert.Equal(OldUpdatedAt, loan.UpdatedAt);      // el préstamo no cambia de estado
                Assert.Equal(ActiveStatus, item.Status);          // el ítem sí se sincroniza
                Assert.NotNull(item.UpdatedAt);
                Assert.InRange(item.UpdatedAt!.Value, before, after);
                _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
            }
        }







        public class ValidateLoanRequestTests
        {
            // ---------- Helper ----------

            private static string? InvokeValidateLoanRequest(uint borrowerId, List<uint> bookIds)
            {
                var method = typeof(LoanService).GetMethod(
                    "ValidateLoanRequest",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(method);

                return (string?)method!.Invoke(null, new object[] { borrowerId, bookIds });
            }

            // ---------- Caso 1: I,1,2,F ----------
            [Fact]
            public void Caso1_BorrowerIdCero_RetornaBorrowerIdInvalido()
            {
                var result = InvokeValidateLoanRequest(0, new List<uint> { 101 });

                Assert.Equal("BorrowerId inválido.", result);
            }

            // ---------- Caso 2: I,1,3,4,F ----------
            [Fact]
            public void Caso2_SinLibros_RetornaErrorLibrosNoEspecificados()
            {
                var result = InvokeValidateLoanRequest(10, new List<uint>());

                Assert.Equal("No se han especificado libros para el préstamo.", result);
            }

            // ---------- Caso 3: I,1,3,5,6,F ----------
            [Fact]
            public void Caso3_MasDeDosLibros_RetornaErrorMaximoLibros()
            {
                var result = InvokeValidateLoanRequest(10, new List<uint> { 101, 102, 103 });

                Assert.Equal("No se pueden prestar más de 2 libros a la vez.", result);
            }

            // ---------- Caso 4: I,1,3,5,7,8,9,F ----------
            [Fact]
            public void Caso4_LibrosDuplicados_RetornaErrorDuplicados()
            {
                var result = InvokeValidateLoanRequest(10, new List<uint> { 101, 101 });

                Assert.Equal("No se pueden agregar libros duplicados: 101", result);
            }

            // ---------- Caso 5: I,1,3,5,7,8,10,F ----------
            [Fact]
            public void Caso5_SolicitudValida_RetornaNull()
            {
                var result = InvokeValidateLoanRequest(10, new List<uint> { 101, 102 });

                Assert.Null(result);
            }
        }







        public class GetDailyFineRateTests
        {
            // Valor de la constante privada DefaultDailyFineRate en LoanService
            private const decimal DefaultDailyFineRate = 10.00m;

            // ---------- Helper ----------

            private static decimal InvokeGetDailyFineRate(string? configuredRate)
            {
                var repo = new Mock<ILoanRepository>();
                var config = new Mock<IConfiguration>();
                config.Setup(c => c["Fine:DailyRate"]).Returns(configuredRate);

                var sut = new LoanService(repo.Object, config.Object);

                var method = typeof(LoanService).GetMethod(
                    "GetDailyFineRate",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.NotNull(method);

                return (decimal)method!.Invoke(sut, null)!;
            }

            // ---------- Caso 1: I,1,2,F ----------
            [Fact]
            public void Caso1_ValorConfiguradoValido_RetornaValorParseado()
            {
                var result = InvokeGetDailyFineRate("12.50");

                Assert.Equal(12.50m, result);
            }

            // ---------- Caso 2: I,1,3,F ----------
            [Fact]
            public void Caso2_ValorConfiguradoInvalido_RetornaValorPorDefecto()
            {
                var result = InvokeGetDailyFineRate("invalid");

                Assert.Equal(DefaultDailyFineRate, result);
            }
        }















        public class ReserveCopiesAsyncTests
        {
            // Valores de las constantes privadas del servicio
            private const int AvailableStatus = 1;
            private const int LoanedStatus = 2;

            private readonly Mock<ILoanRepository> _repo = new();
            private readonly Mock<IConfiguration> _config = new();
            private readonly LoanService _sut;

            public ReserveCopiesAsyncTests()
            {
                _sut = new LoanService(_repo.Object, _config.Object);
            }

            // ---------- Helper ----------

            private async Task<(List<Copy> ReservedCopies, List<uint> UnavailableBookIds)>
                InvokeReserveCopiesAsync(IEnumerable<uint> bookIds)
            {
                var method = typeof(LoanService).GetMethod(
                    "ReserveCopiesAsync",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.NotNull(method);

                var task = (Task<(List<Copy> ReservedCopies, List<uint> UnavailableBookIds)>)
                    method!.Invoke(_sut, new object[] { bookIds })!;

                return await task;
            }

            // ---------- Caso 1: I,1,8,F ----------
            [Fact]
            public async Task Caso1_SinLibros_RetornaListasVacias()
            {
                var result = await InvokeReserveCopiesAsync(new List<uint>());

                Assert.Empty(result.ReservedCopies);
                Assert.Empty(result.UnavailableBookIds);
                _repo.Verify(
                    r => r.GetFirstAvailableCopyByBookIdForUpdateAsync(It.IsAny<uint>()),
                    Times.Never);
            }

            // ---------- Caso 2: I,1,2,3,4,5,1,8,F ----------
            [Fact]
            public async Task Caso2_CopiaNoDisponible_AgregaLibroANoDisponibles()
            {
                _repo.Setup(r => r.GetFirstAvailableCopyByBookIdForUpdateAsync(101))
                     .ReturnsAsync((Copy?)null);

                var result = await InvokeReserveCopiesAsync(new List<uint> { 101 });

                Assert.Empty(result.ReservedCopies);
                Assert.Equal(new List<uint> { 101 }, result.UnavailableBookIds);
                _repo.Verify(r => r.GetFirstAvailableCopyByBookIdForUpdateAsync(101), Times.Once);
            }

            // ---------- Caso 3: I,1,2,3,6,7,1,8,F ----------
            [Fact]
            public async Task Caso3_CopiaDisponible_LaReservaComoPrestada()
            {
                var copy = new Copy { Id = 1, StatusId = AvailableStatus };
                _repo.Setup(r => r.GetFirstAvailableCopyByBookIdForUpdateAsync(101))
                     .ReturnsAsync(copy);

                var before = DateTime.Now;

                var result = await InvokeReserveCopiesAsync(new List<uint> { 101 });

                var after = DateTime.Now;

                Assert.Empty(result.UnavailableBookIds);
                Assert.Single(result.ReservedCopies);

                var reserved = result.ReservedCopies.First();
                Assert.Same(copy, reserved);
                Assert.Equal(1u, reserved.Id);
                Assert.Equal(LoanedStatus, (int)reserved.StatusId);
                Assert.NotNull(reserved.UpdatedAt);
                Assert.InRange(reserved.UpdatedAt!.Value, before, after);
            }
        }









        public class SynchronizeItemStatusesTests
        {
            private const string ActiveStatus = "Active";
            private const string OverdueStatus = "Overdue";
            private const string ReturnedStatus = "Returned";
            private const string CompletedStatus = "Completed";

            private static readonly DateTime UpdatedAt = new DateTime(2026, 10, 19, 10, 0, 0);

            // ---------- Helpers ----------

            private static bool InvokeSynchronizeItemStatuses(Loan loan, string loanStatus, DateTime updatedAt)
            {
                var method = typeof(LoanService).GetMethod(
                    "SynchronizeItemStatuses",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(method);

                return (bool)method!.Invoke(null, new object[] { loan, loanStatus, updatedAt })!;
            }

            private static Loan BuildLoan(params Loanitem[] items) =>
                new Loan { Loanitems = new List<Loanitem>(items) };

            // ---------- Caso 1: I,1,3,5,6,7,F ----------
            [Fact]
            public void Caso1_EstadoCompleted_RetornaFalse()
            {
                // "Completed" => targetItemStatus = null
                var loan = BuildLoan();

                var result = InvokeSynchronizeItemStatuses(loan, CompletedStatus, UpdatedAt);

                Assert.False(result);
                Assert.Empty(loan.Loanitems);
            }

            // ---------- Caso 2: I,1,2,6,8,9,11,12,16,F ----------
            [Fact]
            public void Caso2_Overdue_SinItemsActive_RetornaFalse()
            {
                // target = Overdue, source = Active; el ítem Returned no coincide
                var item = new Loanitem { Status = ReturnedStatus };
                var loan = BuildLoan(item);

                var result = InvokeSynchronizeItemStatuses(loan, OverdueStatus, UpdatedAt);

                Assert.False(result);
                Assert.Equal(ReturnedStatus, item.Status);
                Assert.Null(item.UpdatedAt);
            }

            // ---------- Caso 3: I,1,3,4,6,8,10,11,12,16,F ----------
            [Fact]
            public void Caso3_Active_SinItemsOverdue_RetornaFalse()
            {
                // target = Active, source = Overdue; el ítem Returned no coincide
                var item = new Loanitem { Status = ReturnedStatus };
                var loan = BuildLoan(item);

                var result = InvokeSynchronizeItemStatuses(loan, ActiveStatus, UpdatedAt);

                Assert.False(result);
                Assert.Equal(ReturnedStatus, item.Status);
                Assert.Null(item.UpdatedAt);
            }

            // ---------- Caso 4: I,1,2,6,8,9,11,12,13,14,15,12,16,F ----------
            [Fact]
            public void Caso4_Overdue_ItemActivePasaAOverdue_RetornaTrue()
            {
                var item = new Loanitem { Status = ActiveStatus };
                var loan = BuildLoan(item);

                var result = InvokeSynchronizeItemStatuses(loan, OverdueStatus, UpdatedAt);

                Assert.True(result);
                Assert.Equal(OverdueStatus, item.Status);
                Assert.Equal(UpdatedAt, item.UpdatedAt);
            }

            // ---------- Caso 5: I,1,3,4,6,8,10,11,12,13,14,15,12,16,F ----------
            [Fact]
            public void Caso5_Active_ItemOverduePasaAActive_RetornaTrue()
            {
                var item = new Loanitem { Status = OverdueStatus };
                var loan = BuildLoan(item);

                var result = InvokeSynchronizeItemStatuses(loan, ActiveStatus, UpdatedAt);

                Assert.True(result);
                Assert.Equal(ActiveStatus, item.Status);
                Assert.Equal(UpdatedAt, item.UpdatedAt);
            }

            // ---------- Caso 6: I,1,2,6,8,9,11,12,13,14,15,12,13,14,15,12,16,F ----------
            [Fact]
            public void Caso6_Overdue_DosItemsActivePasanAOverdue_RetornaTrue()
            {
                var item1 = new Loanitem { Status = ActiveStatus };
                var item2 = new Loanitem { Status = ActiveStatus };
                var loan = BuildLoan(item1, item2);

                var result = InvokeSynchronizeItemStatuses(loan, OverdueStatus, UpdatedAt);

                Assert.True(result);
                Assert.Equal(OverdueStatus, item1.Status);
                Assert.Equal(OverdueStatus, item2.Status);
                Assert.Equal(UpdatedAt, item1.UpdatedAt);
                Assert.Equal(UpdatedAt, item2.UpdatedAt);
            }
        }







        public class SynchronizeItemsForManualUpdateTests
        {
            private const string ActiveStatus = "Active";
            private const string OverdueStatus = "Overdue";
            private const string ReturnedStatus = "Returned";

            private static readonly DateTime Now = new DateTime(2026, 10, 19, 10, 0, 0);

            // ---------- Helpers ----------

            private static void InvokeSynchronizeItemsForManualUpdate(
                Loan loan, string requestedStatus, DateTime now)
            {
                var method = typeof(LoanService).GetMethod(
                    "SynchronizeItemsForManualUpdate",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(method);

                method!.Invoke(null, new object[] { loan, requestedStatus, now });
            }

            private static Loan BuildLoan(DateTime dueDate, params Loanitem[] items) =>
                new Loan
                {
                    DueDate = dueDate,
                    Loanitems = new List<Loanitem>(items)
                };

            // ---------- Caso 1: I,1,3,4,F ----------
            [Fact]
            public void Caso1_RequestedOverdue_ItemsActivePasanAOverdue()
            {
                var activeItem = new Loanitem { Status = ActiveStatus };
                var overdueItem = new Loanitem { Status = OverdueStatus };
                var returnedItem = new Loanitem { Status = ReturnedStatus };
                var loan = BuildLoan(new DateTime(2026, 10, 20), activeItem, overdueItem, returnedItem);

                InvokeSynchronizeItemsForManualUpdate(loan, OverdueStatus, Now);

                // UpdateItemStatuses(loan, Active, Overdue, now)
                Assert.Equal(OverdueStatus, activeItem.Status);
                Assert.Equal(Now, activeItem.UpdatedAt);

                Assert.Equal(OverdueStatus, overdueItem.Status);
                Assert.Null(overdueItem.UpdatedAt);

                Assert.Equal(ReturnedStatus, returnedItem.Status);
                Assert.Null(returnedItem.UpdatedAt);
            }

            // ---------- Caso 2: I,1,2,3,4,F ----------
            [Fact]
            public void Caso2_RequestedActiveConVencimientoPasado_ItemsActivePasanAOverdue()
            {
                // dueDatePassed = true (18/oct < 19/oct)
                var activeItem = new Loanitem { Status = ActiveStatus };
                var overdueItem = new Loanitem { Status = OverdueStatus };
                var returnedItem = new Loanitem { Status = ReturnedStatus };
                var loan = BuildLoan(new DateTime(2026, 10, 18), activeItem, overdueItem, returnedItem);

                InvokeSynchronizeItemsForManualUpdate(loan, ActiveStatus, Now);

                // UpdateItemStatuses(loan, Active, Overdue, now)
                Assert.Equal(OverdueStatus, activeItem.Status);
                Assert.Equal(Now, activeItem.UpdatedAt);

                Assert.Equal(OverdueStatus, overdueItem.Status);
                Assert.Null(overdueItem.UpdatedAt);

                Assert.Equal(ReturnedStatus, returnedItem.Status);
                Assert.Null(returnedItem.UpdatedAt);
            }

            // ---------- Caso 3: I,1,2,5,6,7,F ----------
            [Fact]
            public void Caso3_RequestedActiveConVencimientoFuturo_ItemsOverduePasanAActive()
            {
                // dueDatePassed = false (20/oct > 19/oct)
                var overdueItem = new Loanitem { Status = OverdueStatus };
                var activeItem = new Loanitem { Status = ActiveStatus };
                var returnedItem = new Loanitem { Status = ReturnedStatus };
                var loan = BuildLoan(new DateTime(2026, 10, 20), overdueItem, activeItem, returnedItem);

                InvokeSynchronizeItemsForManualUpdate(loan, ActiveStatus, Now);

                // UpdateItemStatuses(loan, Overdue, Active, now)
                Assert.Equal(ActiveStatus, overdueItem.Status);
                Assert.Equal(Now, overdueItem.UpdatedAt);

                Assert.Equal(ActiveStatus, activeItem.Status);
                Assert.Null(activeItem.UpdatedAt);

                Assert.Equal(ReturnedStatus, returnedItem.Status);
                Assert.Null(returnedItem.UpdatedAt);
            }

            // ---------- Caso 4: I,1,2,5,F ----------
            [Fact]
            public void Caso4_RequestedCompleted_NoModificaNingunItem()
            {
                // No es Overdue, el vencimiento no pasó y tampoco es Active: no se llama a UpdateItemStatuses
                var activeItem = new Loanitem { Status = ActiveStatus };
                var overdueItem = new Loanitem { Status = OverdueStatus };
                var returnedItem = new Loanitem { Status = ReturnedStatus };
                var loan = BuildLoan(new DateTime(2026, 10, 20), activeItem, overdueItem, returnedItem);

                InvokeSynchronizeItemsForManualUpdate(loan, "Completed", Now);

                Assert.Equal(ActiveStatus, activeItem.Status);
                Assert.Null(activeItem.UpdatedAt);

                Assert.Equal(OverdueStatus, overdueItem.Status);
                Assert.Null(overdueItem.UpdatedAt);

                Assert.Equal(ReturnedStatus, returnedItem.Status);
                Assert.Null(returnedItem.UpdatedAt);
            }
        }



        public class MapToDtoTests
        {
            // ---------- Helper ----------

            private static LoanDto InvokeMapToDto(Loan loan)
            {
                var method = typeof(LoanService).GetMethod(
                    "MapToDto",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(method);

                return (LoanDto)method!.Invoke(null, new object[] { loan })!;
            }

            // ---------- Caso 1: I,1,F ----------
            [Fact]
            public void Caso1_PrestamoSinItems_MapeaTodasLasPropiedades()
            {
                // Arrange
                var loanDate = new DateTime(2026, 10, 15);
                var dueDate = new DateTime(2026, 10, 22);
                var createdAt = new DateTime(2026, 10, 15, 9, 0, 0);
                var updatedAt = new DateTime(2026, 10, 15, 9, 30, 0);

                var loan = new Loan
                {
                    Id = 10,
                    BorrowerId = 5,
                    LibrarianId = 3,
                    Borrower = new User { FirstName = "Ana", LastName = "Lopez" },
                    Librarian = new User { FirstName = "Luis", LastName = "Perez" },
                    LoanDate = loanDate,
                    DueDate = dueDate,
                    Status = "Active",
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt,
                    Loanitems = new List<Loanitem>()
                };

                // Act
                var dto = InvokeMapToDto(loan);

                // Assert
                Assert.Equal(10, dto.Id);
                Assert.Equal(5, dto.BorrowerId);
                Assert.Equal("Ana Lopez", dto.BorrowerName);
                Assert.Equal(3, dto.LibrarianId);
                Assert.Equal("Luis Perez", dto.LibrarianName);
                Assert.Equal(loanDate, dto.LoanDate);
                Assert.Equal(dueDate, dto.DueDate);
                Assert.Equal("Active", dto.Status);
                Assert.Equal("Activo", dto.StatusDisplayName);   // GetLoanStatusDisplayName("Active")
                Assert.Equal(createdAt, dto.CreatedAt);
                Assert.Equal(updatedAt, dto.UpdatedAt);
                Assert.Empty(dto.Items);
            }
        }





        public class MapLoanItemToDtoTests
        {
            // ---------- Helper ----------

            private static LoanItemDto InvokeMapLoanItemToDto(Loanitem loanItem)
            {
                var method = typeof(LoanService).GetMethod(
                    "MapLoanItemToDto",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.NotNull(method);

                return (LoanItemDto)method!.Invoke(null, new object[] { loanItem })!;
            }

            // ---------- Caso 1: I,1,F ----------
            [Fact]
            public void Caso1_ItemActivo_MapeaTodasLasPropiedades()
            {
                // Arrange
                var loanItem = new Loanitem
                {
                    CopyId = 25,
                    Status = "Active",
                    ReturnedAt = null,
                    Copy = new Copy
                    {
                        Id = 25,
                        InternalCode = "COPY-025",
                        BookId = 7,
                        Book = new Book { Title = "Clean Code" }
                    }
                };

                // Act
                var dto = InvokeMapLoanItemToDto(loanItem);

                // Assert
                Assert.Equal(25, dto.CopyId);
                Assert.Equal("COPY-025", dto.InternalCode);
                Assert.Equal(7, dto.BookId);
                Assert.Equal("Clean Code", dto.BookTitle);
                Assert.Null(dto.ReturnedAt);
                Assert.Equal("Active", dto.Status);
                Assert.Equal("Activo", dto.StatusDisplayName);   // GetLoanItemStatusDisplayName("Active")
            }
        }




































    }
}
