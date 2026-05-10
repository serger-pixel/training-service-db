using FluentAssertions.Equivalency;
using System.Diagnostics.Metrics;

namespace API_sprot_training_program.Metrics
{
    public class DataBaseRequestTime
    {
        private readonly Gauge<double> _gauge;
        public DataBaseRequestTime(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("data_base_request_time", "1.0.0");
            _gauge = meter.CreateGauge<double>(
                name: "request-time",
                unit: "seconds",
                description: "The number of seconds to access database");
        }

        public void add_value(double value)
        {
            _gauge.Record(value);
        }
    }
}
