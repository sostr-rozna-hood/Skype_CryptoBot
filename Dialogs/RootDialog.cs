﻿using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections;

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
        public Boolean prazniListi = true;

      


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
            split = msg.Split(new char[] { ' ' }, 2);
            for(int i = 0; i < split.Length; i++)
            {
                split[i] = split[i].ToLower();
            }
           //Check exchanges
            if(split.Length>1 && (krakenCurrencies.Contains(split[0]) || krakenAltCurrencies.Contains(split[0]))  && (krakenCurrencies.Contains(split[1]) || krakenAltCurrencies.Contains(split[1])))
            {
                if (krakenAltCurrencies.Contains(split[0])){
                    split[0] = krakenCurrencies[krakenAltCurrencies.IndexOf(split[0])];
                }
                if (krakenAltCurrencies.Contains(split[1]))
                {
                    split[1] = krakenCurrencies[krakenAltCurrencies.IndexOf(split[1])];
                }

                returnmsg = getPrice(split[0],split[1],"kraken");
            }
 
            else if (msg.Equals("mining"))
            {
                returnmsg = getMiningStats();
            }
            else if (msg.Equals("arso"))
            {
                replyToConversation.Attachments.Add(new Attachment()
                {
                    ContentUrl = "http://www.arso.gov.si/vreme/napovedi%20in%20podatki/radar.gif",
                    ContentType = "image/gif",
                    Name = "radar.gif"
                });
                returnmsg = "http://www.arso.gov.si/vreme/napovedi%20in%20podatki/radar.gif";
            }
            else if (msg.Equals("arso gif"))
            {
                AnimationCard card = new AnimationCard();
                card.Title = "Arso animated radar";
                List<MediaUrl> tmpList = new List<MediaUrl>();
                tmpList.Add(new MediaUrl("http://www.arso.gov.si/vreme/napovedi%20in%20podatki/radar_anim.gif"));
                card.Media = tmpList;
                card.Autostart = true;
                card.Autoloop = true;
                card.Image = new ThumbnailUrl("http://www.arso.gov.si/vreme/napovedi%20in%20podatki/radar_anim.gif");
                returnmsg = "http://www.arso.gov.si/vreme/napovedi%20in%20podatki/radar_anim.gif";
                replyToConversation.Attachments.Add(card.ToAttachment());
            }
            else if (msg.Equals("help"))
            {
                //AdaptiveCard card = new AdaptiveCard();
                //TEMPORARY. Will replace with AdaptiveCard or similar
                
            }
            else
            {
                
                returnmsg += "I'm sorry, but I don't have that keyword yet.";
                
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
        //More exchanges, list of coin asset pairs (Dictionary?), custom trades
        //Various settings, such as volume, previous trades, charts, etc.
        //Error handling
        private String getPrice(String fromCoin, String toCoin, String exchange)
        {
            String returnmsg = "";
            String coins = fromCoin + toCoin;

            //KRAKEN
            if (exchange.Equals("kraken"))
            {
                if (!krakenCurrencyPairs.Contains(coins)) {
                    returnmsg += "Unknown currency pair!";
                    return returnmsg;
                }
                using (WebClient wc = new WebClient())
                {
                    coins = coins.ToUpper();
                    var json = wc.DownloadString("https://api.kraken.com/0/public/Ticker?pair="+coins);
                    dynamic js = JObject.Parse(json);
                    returnmsg += js.result[coins].c[0]; //Deserialize an unknown object, remove hardcode!
                   
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
                returnmsg += js.workers.rig.reportedHashRate + "  Balance: " +  bal.ToString()+"ETH\n"; //Deserialize an unknown object, remove hardcode!
               
            }
            return returnmsg;

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
        }

        

    }
}