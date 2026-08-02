using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocumentIngestion.Application;

public sealed class RepositoryOperationLogger
{
    private readonly ILogger<RepositoryOperationLogger> _logger;

    public RepositoryOperationLogger(ILogger<RepositoryOperationLogger>? logger = null)
    {
        _logger = logger ?? NullLogger<RepositoryOperationLogger>.Instance;
    }

    public void LogRepositoryError(string operation, Exception exception)
    {
        _logger.LogError(exception, "Repository operation failed: {Operation}", operation);
    }
}
