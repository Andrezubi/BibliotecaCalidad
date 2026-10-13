namespace Backend.Domain.Common;

public class LoanedBook
{
    public uint CopyId { get; set; }

    public uint BookId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string InternalCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}