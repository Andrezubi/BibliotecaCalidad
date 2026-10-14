namespace Backend.Application.DTOs;

public class UpdateLoanDto
{
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
}