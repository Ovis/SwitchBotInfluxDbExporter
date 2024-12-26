namespace SwitchBotInfluxDbExporter.Options
{
    public class InfluxDbOption
    {
        /// <summary>
        /// Token
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Bucket
        /// </summary>
        public string Bucket { get; set; } = string.Empty;

        /// <summary>
        /// Organization
        /// </summary>
        public string Org { get; set; } = string.Empty;
    }
}
