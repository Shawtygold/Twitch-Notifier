using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchNotifier.Helpers
{
    internal class ErrorMessageHelper
    {
        public static void SendConsoleErrorMessage(string errorMessage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now.ToShortTimeString} | {errorMessage}");
            Console.ResetColor();
        }

        public static async Task SendEmbedErrorMessageAsync(DiscordChannel discordChannel, string errorMessage)
        {
            await discordChannel.SendMessageAsync(new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Red,
                Description = errorMessage
            });
        }
    }
}
