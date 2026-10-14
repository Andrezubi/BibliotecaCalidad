using System.Collections.Generic;

namespace Backend.Application.DTOs
{
    public class LoanCreateRequest
    {
        public uint BorrowerId { get; set; }
        public List<uint> BookIds { get; set; } = new();
        public uint? LibrarianId { get; set; }
        public int LoanDays { get; set; } = 14;
    }
}
