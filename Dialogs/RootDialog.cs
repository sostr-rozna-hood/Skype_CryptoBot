using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Skype_CryptoBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            // calculate something for us to return
            int length = (activity.Text ?? string.Empty).Length;


            //Get message, parse it
            String msg = activity.Text;
            String returnmsg = "";
            String[] split = msg.Split(new char[] { ' ' },2);
                
            if(split[1].Equals("eth eur"))
            {
                returnmsg = getPrice("eth","eur","kraken");
                
                await context.PostAsync(returnmsg);
                return;
            }





            // return our reply to the user
            await context.PostAsync($"You sent {activity.Text} which was {length} characters");

            context.Wait(MessageReceivedAsync);
        }

        private String getPrice(String fromCoin, String toCoin, String exchange)
        {
            String returnmsg = "";
            String coins = fromCoin + toCoin;
            if (exchange.Equals("kraken"))
            {
                using (WebClient wc = new WebClient())
                {
                    var json = wc.DownloadString("https://api.kraken.com/0/public/Ticker?pair="+coins);
                    dynamic js = JObject.Parse(json);
                    returnmsg += js.result.c[1];
                }

            }

            return returnmsg;
        }
    }
}