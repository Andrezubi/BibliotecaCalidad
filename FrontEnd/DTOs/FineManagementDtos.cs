namespace FrontEnd.DTOs;

public sealed class LoanFineSummaryDto
{
    public int LoanId { get; set; }
    public int FineCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public int? LastPaymentId { get; set; }
}

public sealed class FinePaymentInfoDto
{
    public int PaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
}

public sealed class LoanFineItemDto
{
    public int FineId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int OverdueDays { get; set; }
    public decimal DailyRate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<FinePaymentInfoDto> Payments { get; set; } = new();
}

public sealed class LoanFineDetailDto
{
    public int LoanId { get; set; }
    public decimal TotalRegistered { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalEstimated { get; set; }
    public List<LoanFineItemDto> Fines { get; set; } = new();
}

public sealed class FinePaymentReceiptDto
{
    public int PaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public decimal Amount { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string LibrarianName { get; set; } = string.Empty;
    public int OverdueDays { get; set; }
    public decimal DailyRate { get; set; }
}
