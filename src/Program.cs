using SwitchBotInfluxDbExporter.Options;
using ZLogger;

namespace SwitchBotInfluxDbExporter;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging
            .ClearProviders()
            .AddZLoggerConsole(options =>
            {
                options.IncludeScopes = true;
                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"{0}|{1}|", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
                    formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                });
            })
            .AddZLoggerRollingFile(options =>
            {
                options.IncludeScopes = true;
                options.FilePathSelector = (_, sequenceNumber) =>
                    $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")}/{sequenceNumber:000}.log";

                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"{0}|{1}|", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
                    formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
                });

                options.RollingSizeKB = 1024;
            });

        var configuration = builder.Configuration;

        builder.Services.Configure<SwitchBotOption>(option =>
        {
            option.Token = configuration["SwitchBotOption:Token"] ?? string.Empty;
            option.Secret = configuration["SwitchBotOption:Secret"] ?? string.Empty;
        });

        builder.Services.Configure<List<Target>>(option =>
        {
            var options =
                configuration
                    .GetSection("Target")
                    .GetChildren()
                    .Select(section =>
                        new Target
                        {
                            DeviceId = section["DeviceId"] ?? string.Empty,
                            Measurement = section["Measurement"] ?? string.Empty,
                            Keys = section.GetSection("Keys").Get<List<string>>() ?? new List<string>(),
                            Tags = section.GetSection("Tags").Get<List<Tags>>() ?? new List<Tags>()
                        });
            option.AddRange(options);
        });

        builder.Services.Configure<InfluxDbOption>(option =>
        {
            option.Url = configuration["InfluxDbOption:Url"] ?? string.Empty;
            option.Token = configuration["InfluxDbOption:Token"] ?? string.Empty;
            option.Bucket = configuration["InfluxDbOption:Bucket"] ?? string.Empty;
            option.Org = configuration["InfluxDbOption:Org"] ?? string.Empty;
        });

        builder.Services.AddSingleton<InfluxDbSender>();

        builder.Services.AddHttpClient<SwitchBotClient>();
        builder.Services.AddSingleton<SwitchBotClient>();

        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}
