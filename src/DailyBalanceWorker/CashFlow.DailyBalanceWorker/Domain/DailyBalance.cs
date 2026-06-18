namespace CashFlow.DailyBalanceWorker.Domain;

public class DailyBalance
{
    public DateOnly Date { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal ConsolidatedBalance => TotalCredits - TotalDebits;
    public DateTime UpdatedAt { get; set; }
}
