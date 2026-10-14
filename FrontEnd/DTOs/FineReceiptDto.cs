using System;

namespace FrontEnd.DTOs;

public class FineReceiptDto
{
    public bool HasFine { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ReceiptNumber { get; set; }
    public decimal Amount { get; set; }
    public int OverdueDays { get; set; }
    public string? BorrowerName { get; set; }
    public string? BookTitle { get; set; }
    public DateTime? PaidAt { get; set; }
}