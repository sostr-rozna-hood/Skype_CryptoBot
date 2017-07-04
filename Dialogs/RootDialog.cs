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
        public String botName = "@CryptoBot";
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
            if (split[0].Equals(botName))
            {
                msg = split[1];
            }
         
            //------------------------------------------POSSIBLE MESSAGES------------------------------------------------------
                

           
            if(msg.Equals("eth eur"))
            {
                returnmsg = getPrice("eth","eur","kraken");
            }
 
            else if (msg.Equals("mining"))
            {
                returnmsg = getMiningStats();

            }

            else
            {
                await context.PostAsync($"I'm sorry, but I don't have that keyword yet.");
                context.Wait(MessageReceivedAsync);

            }

            await context.PostAsync(returnmsg);
            context.Wait(MessageReceivedAsync);

            /* return our reply to the user
            await context.PostAsync($"You sent {activity.Text} which was {length} characters");

            context.Wait(MessageReceivedAsync);*/
        }

        //Get price from exchange and put it in human readable format
        //TODO:
        //More exchanges, list of coin asset pairs (Dictionary?), custom trades
        //Various settings, such as volume, previous trades, charts, etc.
        //Error handling
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
                    returnmsg += js.result.XETHZEUR.c[0]; //Deserialize an unknown object, remove hardcode!
                }

            }
            return returnmsg;
        }

        //Get mining data and put it in human readable format
        //TODO:
        //Group access control, custom mining pools, etc.
        //More mining stats
        //Error handling
        private String getMiningStats()
        {
            String returnmsg = "";
            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString("https://ethermine.org/api/miner_new/D710fFE6f2f363505f026b80a6434C3c60B4a3D4");
                dynamic js = JObject.Parse(json);
                double bal = float.Parse((String)js.unpaid);
                bal *= Math.Pow(10,-18);
                returnmsg += js.workers.rig.reportedHashRate + "  Balance: " +  bal.ToString()+"ETH"; //Deserialize an unknown object, remove hardcode!
            }
            return returnmsg;

        }
    }
}