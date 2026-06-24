using System.Diagnostics.Metrics;

namespace CashFlow.Observability;

public static class CashFlowMeters
{
    public const string MeterName = "CashFlow.Platform";

    public static readonly Meter Instance = new(MeterName, "1.0.0");

    public static readonly Counter<long> LaunchRegistrations =
        Instance.CreateCounter<long>("cashflow_launch_registrations_total", description: "Lançamentos registrados");

    public static readonly Counter<long> LaunchValidationErrors =
        Instance.CreateCounter<long>("cashflow_launch_validation_errors_total", description: "Erros de validação de lançamento");

    public static readonly Counter<long> BalanceQueries =
        Instance.CreateCounter<long>("cashflow_daily_balance_queries_total", description: "Consultas de saldo consolidado");

    public static readonly Counter<long> WorkerConsolidations =
        Instance.CreateCounter<long>("cashflow_worker_consolidations_total", description: "Eventos consolidados pelo worker");

    public static readonly Counter<long> WorkerConsolidationErrors =
        Instance.CreateCounter<long>("cashflow_worker_consolidation_errors_total", description: "Falhas de consolidação no worker");
}
