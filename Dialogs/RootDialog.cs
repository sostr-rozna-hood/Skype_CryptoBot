using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections;
using AdaptiveCards;

namespace Skype_CryptoBot.Dialogs
{
    

    [Serializable]
    public class RootDialog : IDialog<object>
    {
        //Variables
        public String botName = "@CryptoBot";
        public List<String> krakenCurrencyPairs = new List<String>();
        public List<String> krakenCurrencies = new List<String>();
        public List<String> krakenAltCurrencies = new List<String>();
        public List<String> bittrexCurrencies = new List<string>();
        public List<String> bittrexCurrencyPairs = new List<string>();
        public Boolean prazniListi = true;
        public List<String> exchanges = new List<String>(new String[] { "kraken", "poloniex", "bittrex" });
      


        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            StateClient stateClient = activity.GetStateClient();
           

            // calculate something for us to return
            int length = (activity.Text ?? string.Empty).Length;

            /*--------------------------------TEST-*/
            Activity replyToConversation = activity.CreateReply();
            replyToConversation.Recipient = activity.From;
            replyToConversation.Type = "message";
            /*---------------------------------------------*/
            prazniListi = true;
            if (prazniListi)
            {
                checkExchanges();
                prazniListi = false;
            }

            //Get message, parse it 
            String msg = activity.Text;
            String returnmsg = "";
            String[] split = msg.Split(new char[] { ' ' },2);
            if (split[0].Equals(botName))
            {
                msg = split[1];
            }

            //------------------------------------------POSSIBLE MESSAGES------------------------------------------------------
            //Split into words, careful when checking for data!
            split = msg.Split(new char[] { ' ' });
            for(int i = 0; i < split.Length; i++)
            {
                split[i] = split[i].ToLower();
            }
            int forceExchange = -1; // 1 kraken, 2 polo, 3 bittrex
            //Check exchanges
            if (split[0].Equals("ex"))
            {
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                var exchange = userData.GetProperty<String>("exchange");
                if (exchange != null)
                {
                    exchange = exchange.ToLower();
                }
                if (split.Length == 1)
                {
                    returnmsg += "No parameters for keyword ex. \n Usage: \"ex XXX YYY\" for currency pairs or \"ex set\" for setting defaults exchanges. In case of errors, try \"ex reset\".";
                }
                else if (split[1].Equals("reset") && split.Length==2)
                {
                    var data = context.UserData;
                    data.RemoveValue("exchange");
                    returnmsg += "Parameters reset! Please choose your default exchange again by typing \"ex set\".";
                }
                else if(split[1].Equals("set") && split.Length == 2)
                {
                    returnmsg += "Current default: "+exchange+" .What exchange would you to set as the default?";
                    HeroCard heroj = new HeroCard()
                    {
                        Buttons = new List<CardAction>(){
                         new CardAction(){ Title = "Kraken", Type=ActionTypes.ImBack, Value="ex set kraken" },
                         new CardAction(){ Title = "Poloniex", Type=ActionTypes.ImBack, Value="ex set poloniex" },
                         new CardAction(){ Title = "Bittrex", Type=ActionTypes.ImBack, Value="ex set bittrex" }}
                    };
                    Attachment plAttachment = heroj.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                }
                else if (split[1].Equals("set") && split.Length > 2)
                {
                    if (exchanges.Contains(split[2])){
                        // userData.SetProperty<String>("exchange", split[2]);
                       // userData.Data = split[2];
                        var data = context.UserData;
                       data.SetValue("exchange",split[2]);
                    }
                    
                    returnmsg += "Exchange " + split[2] + " set as default.";
                    
                }
                else if (split.Length < 3)
                {
                    returnmsg += "Wrong parameters! Please try again. You can use \"ex XXX YYY\" for currency pairs or \"ex set\" for setting default exchanges.";
                }
                else if (exchange==null)
                {
                    returnmsg += "You haven't selected your default exchange yet. Please do so by clicking your preffered exchange below.";
                    HeroCard heroj = new HeroCard()
                    {
                        Buttons =  new List<CardAction>(){
                         new CardAction(){ Title = "Kraken", Type=ActionTypes.ImBack, Value="ex set kraken" },
                         new CardAction(){ Title = "Poloniex", Type=ActionTypes.ImBack, Value="ex set poloniex" },
                         new CardAction(){ Title = "Bittrex", Type=ActionTypes.ImBack, Value="ex set bittrex" }}
                    };
                    Attachment plAttachment = heroj.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                }
                else if (exchanges.Contains(exchange))
                {
                    
                    returnmsg = getPrice(split[1], split[2], exchange);  
                }
            }

            else if (split[0].Equals("mining"))
            {
                returnmsg = getMiningStats();
            }
            else if (split[0].Equals("arso"))
            {
                replyToConversation.Attachments.Add(new Attachment()
                {
                    ContentUrl = "http://www.arso.gov.si/vreme/napovedi%20in%20podatki/radar.gif",
                    ContentType = "image/gif",
                    Name = "radar.gif"
                });
            }
            else if (split[0].Equals("vreme"))
            {
                String datum,temp,vbes,slikaUrl = "";
                CardImage slika = new CardImage();
                List<CardImage> slikice = new List<CardImage>();
                slikice.Add(slika);
                using (WebClient wc = new WebClient())
                {
                    var json = wc.DownloadString("http://api.openweathermap.org/data/2.5/weather?q=Ljubljana&units=metric&appid=5ce4f42d030fee115dbd958987b37797");
                    dynamic js = JObject.Parse(json);
                    datum = FromUnixTime(Convert.ToInt64(js.dt)).AddHours(2).ToString();
                    temp = js.main.temp;
                    slikaUrl = "http://openweathermap.org/img/w/" + js.weather[0].icon + ".png";
                    vbes = js.weather[0].description;
                }
                slika.Url = slikaUrl;
                HeroCard plCard = new HeroCard()
                {
                    Title =  temp + "°C ,"+vbes,
                    Subtitle = $"Ljubljana, " + datum ,
                    Images = slikice,
                   
                };
                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
            }
            else if (split[0].Equals("napoved"))
            {
                
               // returnmsg += sentGreeting;

            }
            else if (split[0].Equals("help"))
            {
                //AdaptiveCard card = new AdaptiveCard();
                //TEMPORARY. Will replace with AdaptiveCard or similar
                replyToConversation.TextFormat = TextFormatTypes.Plain;
                returnmsg += "Currently supported commands:\n\n";
                returnmsg += "mining - Shows current mining status\n\n";
                returnmsg += "ex XXX YYY - Where XXX and YYY are cryptocurrency pairs (e.g. \"ex ETH EUR\")\n\n";
                returnmsg += "arso - Shows current rain radar image for Slovenia\n\n";
                returnmsg += "vreme - Shows current weather\n";

                returnmsg += "You can also choose from the buttons below:";
                HeroCard heroj = new HeroCard()
                {
                    Buttons = new List<CardAction>(){
                         new CardAction(){ Title = "Mining", Type=ActionTypes.ImBack, Value="mining" },
                         new CardAction(){ Title = "Arso", Type=ActionTypes.ImBack, Value="arso" },
                         new CardAction(){ Title = "Vreme", Type=ActionTypes.ImBack, Value="vreme" }}
                };
                Attachment plAttachment = heroj.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
            }
            else
            {          
                returnmsg += "I'm sorry, but I don't have that keyword yet. Please type \"help\" in order to see available keywords.";   
            }
            replyToConversation.Text = returnmsg;
            
            await context.PostAsync(replyToConversation);
            context.Wait(MessageReceivedAsync);

            /* return our reply to the user
            await context.PostAsync($"You sent {activity.Text} which was {length} characters");

            context.Wait(MessageReceivedAsync);*/
        }

        //Get price from exchange and put it in human readable format
        //TODO:
        //More exchanges, custom trades
        //Various settings, such as volume, previous trades, charts, etc.
        //Error handling
        private String getPrice(String fromCoin, String toCoin, String exchange)
        {
            String returnmsg = "";
            String coins = "";
            String fromR = fromCoin.ToUpper();
            String toR = toCoin.ToUpper();
            //KRAKEN
            if (exchange.Equals("kraken"))
            {
                if ((krakenCurrencies.Contains(fromCoin) || krakenAltCurrencies.Contains(fromCoin)) && (krakenCurrencies.Contains(toCoin) || krakenAltCurrencies.Contains(toCoin)))
                {
                    if (krakenAltCurrencies.Contains(fromCoin))
                    {
                        fromCoin = krakenCurrencies[krakenAltCurrencies.IndexOf(fromCoin)];
                    }
                    if (krakenAltCurrencies.Contains(toCoin))
                    {
                        toCoin = krakenCurrencies[krakenAltCurrencies.IndexOf(toCoin)];
                    }
                }
                coins = fromCoin + toCoin;
                if (!krakenCurrencyPairs.Contains(coins)) {
                    returnmsg += "Unknown currency pair!(" + fromR + "," + toR + ")";
                   
                }
                else
                {
                    using (WebClient wc = new WebClient())
                    {
                        coins = coins.ToUpper();
                        var json = wc.DownloadString("https://api.kraken.com/0/public/Ticker?pair=" + coins);
                        dynamic js = JObject.Parse(json);
                        returnmsg += "1 " + fromR + "=" + js.result[coins].c[0] + " " + toR; //Deserialize an unknown object, remove hardcode!               
                    }
                }        
            }
            //POLONIEX

            //BITTREX
            if (exchange.Equals("bittrex"))
            {
                if (!bittrexCurrencies.Contains(fromCoin))
                {
                    returnmsg += "Wrong coin!" + fromCoin+ "\n";
                    return returnmsg;
                }
                if (!bittrexCurrencies.Contains(toCoin))
                {
                    returnmsg += "Wrong coin!" + toCoin + "\n";
                    return returnmsg;
                }
                coins = toCoin + "-" + fromCoin;
                if (!bittrexCurrencyPairs.Contains(coins))
                {
                    returnmsg += "Unknown currency pair!(" + fromCoin + "," + ")";
                    return returnmsg;
                }
               using (WebClient wc = new WebClient())
                {
                    coins = coins.ToUpper();
                    var json = wc.DownloadString("https://bittrex.com/api/v1.1/public/getticker?market=" + coins);
                    dynamic js = JObject.Parse(json);
                    returnmsg += "1 " + fromCoin.ToUpper() + "=" + js.result.Last + " " + toCoin.ToUpper(); //Deserialize an unknown object, remove hardcode!               
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
                returnmsg += js.reportedHashRate + "  Balance: " +  bal.ToString()+"ETH\n"; //Deserialize an unknown object, remove hardcode!     
            }
            return returnmsg;

        }

        //Taken from StackExchange, Epoch time converter https://stackoverflow.com/questions/2883576/how-do-you-convert-epoch-time-in-c
        public static DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }




        //Fetches first-time data, so we can check for input validity
        private void checkExchanges()
        {
            //Kraken
            //If currency pair list is empty, fill it
            String tmp = "";
                using (WebClient wc = new WebClient())
                {
                    var json = wc.DownloadString("https://api.kraken.com/0/public/AssetPairs?info=margin");
                    dynamic js = JObject.Parse(json);
                    foreach (var x in js.result)
                    {
                         tmp = (String)x.Name;
                        krakenCurrencyPairs.Add(tmp.ToLower());
                    }
                }
            //If currency list is empty, fill it  
                using (WebClient wc = new WebClient())
                {
                    var json = wc.DownloadString("https://api.kraken.com/0/public/Assets");
                    dynamic js = JObject.Parse(json);
                    foreach (var x in js.result)
                    {
                         tmp = (String)x.Name;
                        krakenCurrencies.Add(tmp.ToLower());
                        tmp = (String)x.Value.altname;
                        krakenAltCurrencies.Add(tmp.ToLower());         
                    }
                }
            //Bittrex
            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString("https://bittrex.com/api/v1.1/public/getmarkets");
                dynamic js = JObject.Parse(json);
                foreach (var x in js.result)
                {
                    tmp = (String)x.MarketName;
                    bittrexCurrencyPairs.Add(tmp.ToLower());
                }
            }

            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString("https://bittrex.com/api/v1.1/public/getcurrencies");
                dynamic js = JObject.Parse(json);
                foreach (var x in js.result)
                {
                    tmp = (String)x.Currency;
                    bittrexCurrencies.Add(tmp.ToLower());
                }
            }
        }

        

    }
}