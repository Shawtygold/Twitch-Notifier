using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchNotifier.Config
{
    internal class JSONReader
    {
        public string Token { get; set; }

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
            }
        }
    }

    internal class JSONStructure
    {
        public string Token { get; set; }
    }
}
