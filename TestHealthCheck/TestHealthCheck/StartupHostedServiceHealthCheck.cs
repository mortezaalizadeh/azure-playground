namespace TestHealthCheck
{
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using System.Threading;
    using System.Threading.Tasks;

    public class StartupHostedServiceHealthCheck : IHealthCheck
    {
        private volatile bool _startupTaskCompleted = false;

        public string Name => "slow_dependency_check";

        public bool StartupTaskCompleted { get => _startupTaskCompleted; set => _startupTaskCompleted = value; }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) => StartupTaskCompleted
                ? Task.FromResult(
                    HealthCheckResult.Healthy("The startup task is finished."))
                : Task.FromResult(
                HealthCheckResult.Unhealthy("The startup task is still running."));
    }
}
