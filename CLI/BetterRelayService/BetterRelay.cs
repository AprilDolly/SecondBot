﻿using BetterSecondBot.DiscordSupervisor;
using BetterSecondBotShared.IO;
using BetterSecondBotShared.Json;
using BetterSecondBotShared.logs;
using BetterSecondBotShared.Static;
using BSB.bottypes;
using Newtonsoft.Json;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterSecondBot.BetterRelayService
{
    public class BetterRelay
    {
        Discord_super superV = null;
        CliExitOnLogout controler = null;

        List<relay_config> DiscordRelay = new List<relay_config>();
        List<relay_config> LocalchatRelay = new List<relay_config>();
        List<relay_config> ObjectIMRelay = new List<relay_config>();
        List<relay_config> AvatarIMRelay = new List<relay_config>();
        List<relay_config> GroupChatRelay = new List<relay_config>();

        public BetterRelay(CliExitOnLogout setcontroler, Discord_super setdiscord, bool running_in_docker)
        {
            controler = setcontroler;
            superV = setdiscord;
            if(running_in_docker == true)
            {
                loadRelayConfigFromENV();
            }
            else
            {
                loadRelayConfigFromFile();
            }
            attach_events();
        }

        protected void ApplyRelayConfig(string raw)
        {
            string[] bits = raw.Split(",");
            bool have_source_type = false;
            bool have_source_filter = false;
            bool have_target_type = false;
            bool have_target_filter = false;
            if(bits.Length == 4)
            {
                relay_config relay = new relay_config();
                foreach (string a in bits)
                {
                    string[] subbits = a.Split(":");
                    if(subbits.Length == 2)
                    {
                        if(subbits[0] == "source-type")
                        {
                            have_source_type = true;
                            relay.sourcename = subbits[1];
                        }
                        else if (subbits[0] == "source-filter")
                        {
                            have_source_filter = true;
                            relay.sourcevalue = subbits[1];
                        }
                        else if (subbits[0] == "target-type")
                        {
                            have_target_type = true;
                            relay.targetname = subbits[1];
                        }
                        else if (subbits[0] == "target-config")
                        {
                            have_target_filter = true;
                            relay.targetvalue = subbits[1];
                        }
                        else if(subbits[0] == "encode-as-json")
                        {
                            if(bool.TryParse(subbits[1],out bool use_json_encode) == true)
                            {
                                relay.encode_as_json = use_json_encode;
                            }
                        }
                    }
                }
                List<bool> tests = new List<bool>() { have_source_type, have_source_filter, have_target_type, have_target_filter };
                if(tests.Contains(false) == false)
                {
                    if (relay.sourcename == "localchat") LocalchatRelay.Add(relay);
                    else if (relay.sourcename == "groupchat") GroupChatRelay.Add(relay);
                    else if (relay.sourcename == "avatarim") AvatarIMRelay.Add(relay);
                    else if (relay.sourcename == "objectim") ObjectIMRelay.Add(relay);
                    else if (relay.sourcename == "discord") DiscordRelay.Add(relay);
                }
            }
        }

        protected void loadRelayConfigFromENV()
        {
            int loop = 1;
            bool found = true;
            while (found == true)
            {
                if (helpers.notempty(Environment.GetEnvironmentVariable("CustomRelay_" + loop.ToString())) == true)
                {
                    ApplyRelayConfig(Environment.GetEnvironmentVariable("CustomRelay_" + loop.ToString()));
                }
                else
                {
                    found = false;
                }
                loop++;
            }
        }



        protected void loadRelayConfigFromFile()
        {
            JsonCustomRelays LoadedRelays = new JsonCustomRelays
            {
                CustomRelays = new string[] { "source-type:discord,source-filter:123451231235@12351312321,target-type:localchat,target-config:0" }
            };
            string targetfile = "customrelays.json";
            SimpleIO io = new SimpleIO();
            if (SimpleIO.FileType(targetfile, "json") == false)
            {
                io.WriteJsonRelays(LoadedRelays, targetfile);
                return;
            }
            if (io.Exists(targetfile) == false)
            {
                io.WriteJsonRelays(LoadedRelays, targetfile);
                return;
            }
            string json = io.ReadFile(targetfile);
            if (json.Length > 0)
            {
                try
                {
                    LoadedRelays = JsonConvert.DeserializeObject<JsonCustomRelays>(json);
                    foreach (string loaded in LoadedRelays.CustomRelays)
                    {
                        ApplyRelayConfig(loaded);
                    }
                }
                catch
                {
                    io.makeOld(targetfile);
                    io.WriteJsonRelays(LoadedRelays, targetfile);
                }
                return;
            }
        }

        public void unattach_events()
        {
            controler.Bot.MessageEvent -= SLMessageHandler;
            superV.MessageEvent -= DiscordMessageHandler;
        }

        protected void attach_events()
        {
            controler.Bot.MessageEvent += SLMessageHandler;
            superV.MessageEvent += DiscordMessageHandler;
        }

        protected async void TriggerRelay(string sourcetype, string name,string message, string filtervalue, string discordServerid, string discordMessageid)
        {
            List<relay_config> Dataset = new List<relay_config>();
            if (sourcetype == "localchat") Dataset = LocalchatRelay;
            else if (sourcetype == "groupchat") Dataset = GroupChatRelay;
            else if (sourcetype == "avatarim") Dataset = AvatarIMRelay;
            else if (sourcetype == "objectim") Dataset = ObjectIMRelay;
            else if (sourcetype == "discord") Dataset = DiscordRelay;
            if(message.Contains("[relay]") == true)
            {
                return;
            }
            message = "[relay] " + sourcetype + " # " + name + ": " + message;


            foreach (relay_config cfg in Dataset)
            {
                relay_packet packet = new relay_packet();
                string sendmessage = message;
                if(cfg.encode_as_json == true)
                {
                    packet.source_message = message;
                    packet.source_name = sourcetype;
                    packet.source_user = name;
                    packet.target_name = cfg.targetname;
                    packet.target_value = cfg.targetvalue;
                    packet.unixtime = helpers.UnixTimeNow().ToString();
                    if (sourcetype == "discord")
                    {
                        packet.discord_serverid = discordServerid;
                        packet.discord_messageid = discordMessageid;
                    }
                    sendmessage = JsonConvert.SerializeObject(packet);
                    LogFormater.Info("Created json packet");
                }

                LogFormater.Info("Attempting to check if: "+ cfg.sourcevalue+" == "+filtervalue+"");
                if (((cfg.sourcevalue == "all") && (sourcetype != "discordchat")) || (cfg.sourcevalue == filtervalue))
                {
                    LogFormater.Info("passed source filter");
                    if (cfg.targetname == "discord")
                    {
                        string[] cfga = cfg.targetvalue.Split("@");
                        if (cfga.Length == 2)
                        {
                            if ((cfga[0] + "@" + cfga[1]) != filtervalue)
                            {
                                LogFormater.Info("sending via discord");
                                await controler.Bot.SendMessageToDiscord(cfga[0], cfga[1], sendmessage, false);
                            }
                            else
                            {
                                LogFormater.Info("Blocked - source is the same as target");
                            }
                        }
                    }
                    else if (cfg.targetname == "localchat")
                    {
                        int chan = 0;
                        int.TryParse(cfg.targetvalue, out chan);
                        LogFormater.Info("sending via localchat");
                        controler.Bot.GetClient.Self.Chat(sendmessage, chan, ChatType.Normal);
                    }
                    else if (cfg.targetname == "avatarchat")
                    {
                        if (cfg.targetvalue != filtervalue)
                        {
                            if (UUID.TryParse(cfg.targetvalue, out UUID target) == true)
                            {
                                LogFormater.Info("sending via im");
                                controler.Bot.SendIM(target, sendmessage);
                            }
                            else
                            {
                                LogFormater.Info("Blocked - source is the same as target");
                            }
                        }
                    }
                    else if (cfg.targetname == "groupchat")
                    {
                        LogFormater.Info("processing as groupchat");
                        if (cfg.targetvalue != filtervalue)
                        {
                            LogFormater.Info("sending via groupchat");
                            controler.Bot.GetCommandsInterface.Call("Groupchat", cfg.targetvalue + "~#~" + sendmessage, UUID.Zero, "~#~");
                        }
                        else
                        {
                            LogFormater.Info("Blocked - source is the same as target");
                        }
                    }
                }
                else
                {
                    LogFormater.Info("Did not pass source filter");
                }
            }
        }

        protected void DiscordMessageHandler(object sender, DiscordMessageEvent e)
        {
            TriggerRelay("discord", e.name, e.message, "" + e.server.ToString() + "@" + e.channel.ToString(),e.server.ToString(),e.channel.ToString());
        }

        protected void SLMessageHandler(object sender, MessageEventArgs e)
        {
            if (e.fromme == false)
            {
                if (e.localchat == true)
                {
                    TriggerRelay("localchat", e.sender_name, e.message, e.sender_uuid.ToString(), "", "");
                }
                else if (e.avatar == false)
                {
                    TriggerRelay("objectim", e.sender_name, e.message, e.sender_uuid.ToString(), "", "");
                }
                else if (e.group == true)
                {
                    TriggerRelay("groupchat", e.sender_name, e.message, e.group_uuid.ToString(), "", "");
                }
                else if (e.avatar == true)
                {
                    TriggerRelay("avatarim", e.sender_name, e.message, e.sender_uuid.ToString(), "", "");
                }
            }

        }

    }


    public class relay_config
    {
        public string sourcename = "";
        public string sourcevalue = "";
        public string targetname = "";
        public string targetvalue = "";
        public bool encode_as_json = false;
    }

    public class relay_packet
    {
        public string source_name = "";
        public string source_user = "";
        public string source_message = "";
        public string target_name = "";
        public string target_value = "";
        public string discord_serverid = "notused";
        public string discord_messageid = "notused";
        public string unixtime = "";
    }
}
