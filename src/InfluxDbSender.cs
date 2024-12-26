using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using SwitchBotInfluxDbExporter.Options;
using ZLogger;

namespace SwitchBotInfluxDbExporter;

public sealed class InfluxDbSender(
    ILogger<InfluxDbSender> logger,
    IOptions<InfluxDbOption> options) : IDisposable
{
    private readonly InfluxDbOption _option = options.Value;

    private readonly InfluxDBClient _client = new InfluxDBClient(options.Value.Url, options.Value.Token).EnableGzip();

    public async ValueTask SendTelemetryAsync<T>(
        string measurement,
        string field,
        T value,
        List<Tags> tags,
        DateTimeOffset dt,
        CancellationToken ct)
    {
        try
        {
            var writeApi = _client.GetWriteApiAsync();

            var point = PointData.Measurement(measurement)
                .Field(field, value)
                .Timestamp(dt, WritePrecision.Ns);

            point = tags.Aggregate(point, (current, tag) => current.Tag(tag.Key, tag.Value));

            await writeApi.WritePointAsync(point, _option.Bucket, _option.Org, ct);
        }
        catch (Exception e)
        {
            logger.ZLogError(e, $"Failed send InfluxDB.");
            throw;
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
