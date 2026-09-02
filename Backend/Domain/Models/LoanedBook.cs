namespace Backend.Domain.Models
{
    public class LoanedBook
    {
        public int CopyId { get; set; }

        public int BookId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string InternalCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}