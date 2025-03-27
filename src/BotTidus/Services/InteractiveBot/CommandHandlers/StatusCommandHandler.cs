using BotTidus.ConsoleCommand;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;

namespace BotTidus.Services.InteractiveBot.CommandHandlers
{
    readonly struct StatusCommandHandler(HealthCheckService healthCheckService) : IAsyncConsoleCommandHandler<StatusCommandResult>
    {
        readonly HealthCheckService _healthCheckService = healthCheckService;

        public bool RequiredArgumentsAreFilled => true;

        public async ValueTask<StatusCommandResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var reports = await _healthCheckService.CheckHealthAsync(cancellationToken);
                StringBuilder sb = new("""
                    | Name | Status | Description |
                    | :--- | :----: | :---------- |
                    """);
                sb.AppendLine();

                foreach (var (name, entry) in reports.Entries)
                {
                    var statusBadge = entry.Status switch
                    {
                        HealthStatus.Healthy => ":white_check_mark:",
                        HealthStatus.Degraded => ":warning:",
                        HealthStatus.Unhealthy => ":x:",
                        _ => ""
                    };
                    sb.AppendLine($"| `{name}` | {statusBadge} | {entry.Description} |");
                }

                return new StatusCommandResult()
                {
                    IsSuccessful = true,
                    Message = sb.ToString()
                };
            }
            catch (Exception ex)
            {
                return new StatusCommandResult()
                {
                    IsSuccessful = false,
                    Message = ex.Message,
                    ErrorType = CommandErrorType.InternalError
                };
            }
        }

        public bool TryReadArguments(ConsoleCommandReader reader)
        {
            return reader.EnumeratedAll;
        }
    }

    readonly struct StatusCommandResult : ICommandResult
    {
        public bool IsSuccessful { get; init; }

        public string? Message { get; init; }

        public CommandErrorType ErrorType { get; init; }

        public override string? ToString() => Message;
    }
}
