using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SwitchBotInfluxDbExporter.Options;
using ZLogger;

namespace SwitchBotInfluxDbExporter;

/// <summary>
/// Worker
/// </summary>
/// <param name="logger"></param>
/// <param name="targetListOption"></param>
/// <param name="client"></param>
/// <param name="influxDbSender"></param>
public class Worker(
    ILogger<Worker> logger,
    IOptions<List<Target>> targetListOption,
    SwitchBotClient client,
    InfluxDbSender influxDbSender) : BackgroundService
{
    private readonly List<Target> _targetList = targetListOption.Value;

    private const string SuccessStatus = "100";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastExecutionTimes = new Dictionary<string, DateTimeOffset>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var dt = DateTimeOffset.Now;

            foreach (var target in _targetList)
            {
                // 初回実行時は全件実行、以降はApiIntervalで指定した間隔で処理を行う
                if (lastExecutionTimes.TryGetValue(target.DeviceId, out var lastExecutionTime) &&
                    !((dt - lastExecutionTime).TotalSeconds >= target.ApiInterval)) continue;

                try
                {
                    var (isSuccess, json, err) = await client.GetDeviceStatusAsync(target.DeviceId, stoppingToken);

                    if (!isSuccess)
                    {
                        logger.ZLogError(err, $"Failed to get device status for {target.DeviceId}");
                        continue;
                    }

                    var jsonDocument = JsonDocument.Parse(json);

                    var status = jsonDocument.RootElement.GetProperty("statusCode").GetDouble().ToString(CultureInfo.InvariantCulture);

                    if (status is not SuccessStatus)
                    {
                        logger.ZLogWarning($"API status code is {status}");
                        continue;
                    }

                    var bodyElement = jsonDocument.RootElement.GetProperty("body");

                    foreach (var key in target.Keys)
                    {
                        if (bodyElement.TryGetProperty(key, out var valueElement))
                        {
                            switch (valueElement.ValueKind)
                            {
                                case JsonValueKind.String:
                                    await influxDbSender.SendTelemetryAsync(
                                        measurement: target.Measurement,
                                        field: key,
                                        value: valueElement.GetString(),
                                        tags: target.Tags,
                                        dt: dt,
                                        ct: stoppingToken);
                                    break;

                                case JsonValueKind.Number:
                                    await influxDbSender.SendTelemetryAsync(
                                        measurement: target.Measurement,
                                        field: key,
                                        value: valueElement.GetDouble(),
                                        tags: target.Tags,
                                        dt: dt,
                                        ct: stoppingToken);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    lastExecutionTimes[target.DeviceId] = dt;
                }
                catch (Exception ex)
                {
                    logger.ZLogError(ex, $"An error occurred while processing device {target.DeviceId}");
                }
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
