namespace AutoDownload.Application.Contracts;

public sealed record OperatorResponse(Guid Id, string Name, string Type, string Icon);

public sealed record AccountCreateRequest(
    Guid OperadoraId,
    string LoginPortal,
    string SenhaPortal,
    string UnidadeConsumidora);

public sealed record AccountUpdateRequest(
    Guid OperadoraId,
    string LoginPortal,
    string? SenhaPortal,
    string UnidadeConsumidora);

public sealed record AccountScheduleRequest(
    bool Enabled,
    int? DayOfMonth,
    bool LastDayOfMonth,
    TimeOnly Time);

public sealed record AccountPortalPasswordResponse(string SenhaPortal);

public sealed record AccountResponse(
    Guid Id,
    Guid OperadoraId,
    string Operadora,
    string Tipo,
    string Icon,
    string LoginPortal,
    string UnidadeConsumidora,
    string Status,
    DateTimeOffset? UltimaExecucao,
    DateTimeOffset? ProximaExecucao,
    bool AgendamentoAtivo,
    int? DiaAgendamento,
    bool UltimoDiaDoMes,
    TimeOnly HorarioAgendamento);

public sealed record BillResponse(
    Guid Id,
    Guid ContaId,
    string Operadora,
    string Tipo,
    string Icon,
    string Referencia,
    DateOnly Vencimento,
    decimal Valor,
    string Arquivo,
    DateTimeOffset BaixadoEm,
    string Status);

public sealed record BillFileResponse(Guid Id, string FileName, string StoragePath);

public sealed record HistoryResponse(
    Guid Id,
    Guid ContaId,
    string Operadora,
    string Tipo,
    DateTimeOffset DataExecucao,
    string Status,
    string Mensagem,
    string? Arquivo,
    string Duracao);

public sealed record NotificationResponse(
    Guid Id,
    string Texto,
    DateTimeOffset Data,
    bool Lida,
    string Type);

public sealed record DashboardResponse(
    int TotalContas,
    int LimiteContas,
    int TotalBoletos,
    int ExecucoesSucesso,
    int ExecucoesFalha,
    DateTimeOffset? ProximaExecucao,
    int NotificacoesNaoLidas,
    IReadOnlyList<BillResponse> BoletosRecentes,
    IReadOnlyList<HistoryResponse> HistoricoRecente);
