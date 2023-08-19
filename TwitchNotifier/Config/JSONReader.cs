using Newtonsoft.Json;

namespace TwitchNotifier.Config
{
    internal class JSONReader
    {
        public string Token { get; set; }
        public string TwitchClientId { get; set; }
        public string TwitchAccessToken { get; set; }

        internal async Task ReadJSONAsync()
        {
            using (StreamReader streamReader = new StreamReader("config.json"))
            {
                string json = await streamReader.ReadToEndAsync();
                JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);

                if(data == null)
                {
                    return;
                }

                Token = data.Token;
                TwitchAccessToken = data.TwitchAccessToken;
                TwitchClientId = data.TwitchClientId;
            }
        }
    }

    internal class JSONStructure
    {
        public string Token { get; set; }
        public string TwitchClientId { get; set; }
        public string TwitchAccessToken { get; set; }
    }
}
