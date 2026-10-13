using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs;

public class LoanDto
{
    public int Id { get; set; }

    public int BorrowerId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public int LibrarianId { get; set; }

    public string LibrarianName { get; set; } = string.Empty;

    public DateTime LoanDate { get; set; }

    public DateTime DueDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusDisplayName { get; set; } = string.Empty;

    public List<LoanItemDto> Items { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class LoanItemDto
{
    public int CopyId { get; set; }

    public string InternalCode { get; set; } = string.Empty;

    public int BookId { get; set; }

    public string BookTitle { get; set; } = string.Empty;

    public DateTime? ReturnedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusDisplayName { get; set; } = string.Empty;
}

public class LoanListDto
{
    public int Id { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public string LibrarianName { get; set; } = string.Empty;

    public DateTime LoanDate { get; set; }

    public DateTime DueDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusDisplayName { get; set; } = string.Empty;

    public int ItemsCount { get; set; }

    public int ReturnedItemsCount { get; set; }
}