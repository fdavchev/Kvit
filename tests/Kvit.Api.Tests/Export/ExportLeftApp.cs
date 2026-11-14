using Kvit.Api.Tests.Expenses;
using Kvit.Api.Tests.Postgres;

namespace Kvit.Api.Tests.Export
{
    public class ExportLeftApp : ExpensesApp
    {
        private readonly Lazy<Task<ExportLeftOutcome>> _callerLeft;
        private readonly Lazy<Task<ExportLeftOutcome>> _callerRemoved;
        private readonly Lazy<Task<ExportLeftOutcome>> _callerCameBack;
        private readonly Lazy<Task<ExportLeftOutcome>> _callerNeverLeft;

        public ExportLeftApp(PostgresFixture postgres)
            : base(postgres)
        {
            _callerLeft = new Lazy<Task<ExportLeftOutcome>>(() => ExportLeftSteps.CallerLeftAsync(this));
            _callerRemoved = new Lazy<Task<ExportLeftOutcome>>(() => ExportLeftSteps.CallerRemovedAsync(this));
            _callerCameBack = new Lazy<Task<ExportLeftOutcome>>(() => ExportLeftSteps.CallerLeftAndCameBackAsync(this));
            _callerNeverLeft = new Lazy<Task<ExportLeftOutcome>>(() => ExportLeftSteps.CallerNeverLeftAsync(this));
        }

        public Task<ExportLeftOutcome> CallerLeftAsync()
        {
            return _callerLeft.Value;
        }

        public Task<ExportLeftOutcome> CallerRemovedAsync()
        {
            return _callerRemoved.Value;
        }

        public Task<ExportLeftOutcome> CallerCameBackAsync()
        {
            return _callerCameBack.Value;
        }

        public Task<ExportLeftOutcome> CallerNeverLeftAsync()
        {
            return _callerNeverLeft.Value;
        }
    }
}
