using System;

namespace FrontEnd.DTOs;

public class FineReceiptDto
{
    public bool HasFine { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ReceiptNumber { get; set; }
    public int? CopyId { get; set; }
    public decimal Amount { get; set; }
    public int OverdueDays { get; set; }
    public string? BorrowerName { get; set; }
    public string? BookTitle { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? FineStatus { get; set; }
    public decimal? DailyRate { get; set; }
    public DateTime? DueDate { get; set; }
    public int? LoanId { get; set; }
}
