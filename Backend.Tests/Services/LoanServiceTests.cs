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


























    }
}
