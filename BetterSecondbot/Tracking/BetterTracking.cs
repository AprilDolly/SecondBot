﻿using BetterSecondBot;
using BetterSecondBot.bottypes;
using BetterSecondBotShared.logs;
using BetterSecondBotShared.Static;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace BetterSecondbot.Tracking
{
    public class BetterTracking
    {
        protected Cli controler = null;

        protected bool output_to_url = false;

        protected bool output_to_channel = false;
        protected int output_channel = 0;


        protected Dictionary<UUID, long> AvatarSeen = new Dictionary<UUID, long>();
        protected bool AfterLogin = false;

        public BetterTracking(Cli setcontroler)
        {
            controler = setcontroler;
            if (controler.getBot() == null)
            {
                LogFormater.Info("Tracker - error no bot");
                return;
            }
            SecondBot bot = controler.getBot();
            if (bot.getMyConfig.Setting_Tracker.ToLowerInvariant() == "false")
            {
                LogFormater.Info("Tracker - Disabled because set to false");
                return;
            }
            if(int.TryParse(controler.getBot().getMyConfig.Setting_Tracker,out output_channel) == true)
            {
                output_to_channel = true;
                attachEvents();
            }
            else if (controler.getBot().getMyConfig.Setting_Tracker.StartsWith("http") == true)
            {
                output_to_url = true;
                attachEvents();
            }
            else
            {
                LogFormater.Info("Tracker - Disabled not a vaild channel or http");
            }
        }
        protected void attachEvents()
        {
            LogFormater.Info("Tracker - Connected to login process event");
            controler.getBot().LoginProgess += LoginProcess;
        }

        protected void LoginProcess(object o, LoginProgressEventArgs e)
        {
            if(e.Status == LoginStatus.ConnectingToSim)
            {
                LogFormater.Info("Tracker - disconnected from login process event");
                controler.getBot().LoginProgess -= LoginProcess;
                if (AfterLogin == false)
                {
                    AfterLogin = true;
                    if (output_to_channel == true)
                    {
                        LogFormater.Info("Tracker - Enabled Chat on channel: " + output_channel.ToString());
                    }
                    if (output_to_url == true)
                    {
                        LogFormater.Info("Tracker - Enabled HTTP: " + controler.getBot().getMyConfig.Setting_Tracker);
                    }
                    controler.getBot().StatusMessageEvent += StatusPing;
                    //controler.getBot().GetClient.Grid.CoarseLocationUpdate += LocationUpdate;
                }
            }
        }

        protected bool hasBot()
        {
            if (controler == null)
            {
                return false;
            }
            if (controler.getBot() == null)
            {
                return false;
            }
            if (controler.getBot().GetClient == null)
            {
                return false;
            }
            if (controler.getBot().GetClient.Network == null)
            {
                return false;
            }
            return controler.getBot().GetClient.Network.Connected;
        }

        protected void StatusPing(object o, StatusMessageEvent e)
        {
            if(hasBot() == true)
            {
                if(controler.getBot().GetClient.Network.CurrentSim != null)
                {
                    Dictionary<UUID, Vector3> entrys = controler.getBot().GetClient.Network.CurrentSim.AvatarPositions.Copy();
                    List<UUID> avs = entrys.Keys.ToList();
                    List<UUID> seenavs = new List<UUID>();
                    foreach (UUID A in avs)
                    {
                        seenavs.Add(A);
                        Thread newThread = new Thread(this.TrackerEventAdd);
                        newThread.Start(A);
                    }
                    List<UUID> TrackedUUID = new List<UUID>();
                    lock (AvatarSeen)
                    {
                        TrackedUUID = AvatarSeen.Keys.ToList();
                    }
                    foreach (UUID A in TrackedUUID)
                    {
                        if(seenavs.Contains(A) == false)
                        {
                            long dif = helpers.UnixTimeNow() - AvatarSeen[A];
                            if (dif > 15)
                            {
                                Thread newThread = new Thread(this.TrackerEventRemove);
                                newThread.Start(A);
                            }
                        }
                    }
                }
            }
        }

        protected void output(UUID av, string mode)
        {
            string output = av.ToString() + "|||" + mode;
            if (output_to_channel == true)
            {
                controler.getBot().GetClient.Self.Chat(output, output_channel, ChatType.Normal);
            }
            if(output_to_url == true)
            {
                Thread newThread = new Thread(this.SendHTTPData);
                newThread.Start(output);
            }
        }

        protected HttpClient HTTPclient = new HttpClient();

        protected void SendHTTPData(object data)
        {
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                { "reply", data.ToString() },
            };
            var content = new FormUrlEncodedContent(values);
            try
            {
                HTTPclient.PostAsync(controler.getBot().getMyConfig.Setting_Tracker, content);
            }
            catch (Exception e)
            {
                LogFormater.Crit("[Tracker] HTTP failed: " + e.Message + "");
            }
        }

        protected void TrackerEventAdd(object av)
        {
            if (UUID.TryParse(av.ToString(), out UUID avuuid) == true)
            {
                if (AvatarSeen.ContainsKey(avuuid) == false)
                {
                    addToSeen(avuuid);
                    string name = controler.getBot().FindAvatarKey2Name(avuuid);
                    int loop = 0;
                    while ((loop < 32) && (name == "lookup"))
                    {
                        Thread.Sleep(500);
                        name = controler.getBot().FindAvatarKey2Name(avuuid);
                        loop++;
                    }
                    output(avuuid, "entry###" + name);
                }
                addToSeen(avuuid);
            }
        }

        protected void addToSeen(UUID av)
        {
            lock (AvatarSeen)
            {
                if (AvatarSeen.ContainsKey(av) == false)
                {
                    AvatarSeen.Add(av, 0);
                }
                AvatarSeen[av] = helpers.UnixTimeNow();
            }   
        }

        protected void removeFromSeen(UUID av)
        {
            lock (AvatarSeen)
            {
                if (AvatarSeen.ContainsKey(av) == true)
                {
                    AvatarSeen.Remove(av);
                }
            }
        }

        protected void TrackerEventRemove(object av)
        {
            if (UUID.TryParse(av.ToString(), out UUID avuuid) == true)
            {
                if (AvatarSeen.ContainsKey(avuuid) == true)
                {
                    string name = controler.getBot().FindAvatarKey2Name(avuuid);
                    int loop = 0;
                    while ((loop < 32) && (name == "lookup"))
                    {
                        Thread.Sleep(500);
                        name = controler.getBot().FindAvatarKey2Name(avuuid);
                        loop++;
                    }
                    if (AvatarSeen.ContainsKey(avuuid) == true)
                    {
                        output(avuuid, "exit###" + name + "~#~" + AvatarSeen[avuuid].ToString());
                        removeFromSeen(avuuid);
                    }
                }
            }
        }



        protected void LocationUpdate(object o, CoarseLocationUpdateEventArgs e)
        {
            if (e.NewEntries.Count() > 0)
            {
                foreach (UUID av in e.NewEntries)
                {
                    Thread newThread = new Thread(this.TrackerEventAdd);
                    newThread.Start(av);
                }
            }
            if (e.RemovedEntries.Count() > 0)
            {
                foreach (UUID av in e.RemovedEntries)
                {
                    TrackerEventRemove(av);
                }
            }

        }
    }
}
