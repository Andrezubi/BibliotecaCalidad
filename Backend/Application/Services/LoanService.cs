using Backend.Application.Interfaces;
using Backend.Domain.Interfaces;
using Backend.Domain.Models;

namespace Backend.Application.Services;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;

    public LoanService(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync()
    {
        return await _loanRepository.GetLoanedBooksAsync();
    }

    public async Task<bool> ReturnLoanAsync(int loanId)
    {
        return await _loanRepository.ReturnLoanAsync(loanId);
    }
    public async Task<bool> LoanBookAsync(int bookId)
    {
        return await _loanRepository.LoanBookAsync(bookId);
    }
}