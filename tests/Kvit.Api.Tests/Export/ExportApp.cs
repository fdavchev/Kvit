using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Postgres;

namespace Kvit.Api.Tests.Export
{
    public class ExportApp : ExpensesApp
    {
        private readonly Lazy<Task<ExportScenario>> _scenario;
        private readonly Lazy<Task<ExportBody>> _exporterBody;

        public ExportApp(PostgresFixture postgres)
            : base(postgres)
        {
            _scenario = new Lazy<Task<ExportScenario>>(() => ExportScenarioSteps.CreateAsync(this));
            _exporterBody = new Lazy<Task<ExportBody>>(ReadExporterBodyAsync);
        }

        public Task<ExportScenario> ScenarioAsync()
        {
            return _scenario.Value;
        }

        public Task<ExportBody> ExporterBodyAsync()
        {
            return _exporterBody.Value;
        }

        private async Task<ExportBody> ReadExporterBodyAsync()
        {
            ExportScenario scenario = await ScenarioAsync();

            return await ExportRequests.GetBodyAsync(scenario.Exporter.Client);
        }
    }
}
