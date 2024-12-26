using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SwitchBotInfluxDbExporter.Extensions;
using SwitchBotInfluxDbExporter.Options;
using ZLogger;

namespace SwitchBotInfluxDbExporter;

public class SwitchBotClient(
    ILogger<SwitchBotClient> logger,
    IOptions<SwitchBotOption> option,
    IHttpClientFactory httpClientFactory)
{
    private readonly HttpClient _client = httpClientFactory.CreateClient();

    private readonly SwitchBotOption _option = option.Value;

    private const string BaseUri = "https://api.switch-bot.com/v1.1";



    /// <summary>
    /// デバイス状態取得
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async ValueTask<(bool IsSuccess, string Json, Exception? Error)> GetDeviceStatusAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{BaseUri}/devices/{deviceId}/status");
        AddAuthHeaders(requestMessage);

        try
        {
            var response = await _client.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            return (true, (await response.Content.ReadAsStringAsync(cancellationToken)).JsonFormatting(), null);
        }
        catch (Exception e)
        {
            logger.ZLogError(e, $"Error occurred while getting device status.");
            return (false, string.Empty, e);
        }
    }



    /// <summary>
    /// 認証ヘッダー生成
    /// </summary>
    /// <param name="requestMessage"></param>
    private void AddAuthHeaders(HttpRequestMessage requestMessage)
    {
        var unixTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var nonce = Guid.NewGuid().ToString();
        var data = _option.Token + unixTime + nonce;
        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_option.Secret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));

        requestMessage.Headers.TryAddWithoutValidation(@"Authorization", _option.Token);
        requestMessage.Headers.TryAddWithoutValidation(@"sign", signature);
        requestMessage.Headers.TryAddWithoutValidation(@"nonce", nonce);
        requestMessage.Headers.TryAddWithoutValidation(@"t", unixTime.ToString());
    }
}
