﻿using BetterSecondBot.bottypes;
using BetterSecondBot.Static;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Newtonsoft.Json;
using static OpenMetaverse.ParcelManager;
using BetterSecondBotShared.Static;
using BetterSecondBotShared.logs;
using System.Threading;

namespace BetterSecondBot.HttpService
{
    public partial class HTTP_Parcel : WebApiControllerWithTokens
    {
        public HTTP_Parcel(SecondBot mainbot, TokenStorage setuptokens) : base(mainbot, setuptokens) { }

        [About("Sets the current parcel for sale Also marks the parcel for sale")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ReturnHintsFailure("Invaild amount")]
        [ArgHints("amount", "URLARG", "The amount to sell the parcel for from 1 to 9999999")]
        [ArgHints("avatar", "URLARG", "Avatar uuid or Firstname Lastname or \"none\" who we are locking the sale to")]
        [Route(HttpVerbs.Get, "/SetParcelSale/{amount}/{avatar}/{token}")]
        public object SetParcelSale(string amount,string avatar,string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "SetParcelSale");
            if(tests.Key == false)
            {
                return Failure(tests.Value, "SetParcelSale", new [] { amount, avatar });
            }
            if (int.TryParse(amount,out int amountvalue) == false)
            {
                return Failure("Invaild amount", "SetParcelSale", new [] { amount, avatar });
            }

            int minAmount = 1;
            avataruuid = UUID.Zero;
            if (avatar != "none")
            {
                ProcessAvatar(avatar);
                if(avataruuid != UUID.Zero)
                {
                    minAmount = 0;
                }
            }
            if ((amountvalue < minAmount) || (amountvalue > 9999999))
            {
                return Failure("Invaild amount", "SetParcelSale", new [] { amount, avatar });
            }
            targetparcel.SalePrice = amountvalue;
            targetparcel.AuthBuyerID = avataruuid;
            parcel_static.ParcelSetFlag(ParcelFlags.ForSale, targetparcel, true);
            parcel_static.ParcelSetFlag(ParcelFlags.ForSaleObjects, targetparcel, false);
            targetparcel.Update(bot.GetClient.Network.CurrentSim, false);
            return Failure("ok", "SetParcelSale", new [] { amount, avatar });
        }

        [About("Gets the parcel Dwell (Traffic) value and returns it via the reply target")]
        [ReturnHints("traffic value")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [Route(HttpVerbs.Get, "/GetParcelTraffic/{token}")]
        public object GetParcelTraffic(string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "GetParcelTraffic");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "GetParcelTraffic");
            }
            return BasicReply(targetparcel.Dwell.ToString(), "GetParcelTraffic");
        }

        [About("Changes the parcel landing mode to point and sets the landing point")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ReturnHintsFailure("Invaild amount")]
        [ArgHints("x", "URLARG", "X point for landing")]
        [ArgHints("y", "URLARG", "Y point for landing")]
        [ArgHints("z", "URLARG", "Z point for landing")]
        [Route(HttpVerbs.Get, "/SetParcelLandingZone/{x}/{y}/{z}/{token}")]
        public object SetParcelLandingZone(string x, string y, string z, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "SetParcelLandingZone");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "SetParcelLandingZone", new [] { x, y, z });
            }
            if(int.TryParse(x,out int X) == false)
            {
                return Failure("Unable to process landing point x value", "SetParcelLandingZone", new [] { x, y, z });
            }
            if (int.TryParse(y, out int Y) == false)
            {
                return Failure("Unable to process landing point y value", "SetParcelLandingZone", new [] { x, y, z });
            }
            if (int.TryParse(z, out int Z) == false)
            {
                return Failure("Unable to process landing point z value", "SetParcelLandingZone", new [] { x, y, z });
            }
            targetparcel.Landing = LandingType.LandingPoint;
            targetparcel.UserLocation = new Vector3(X, Y, Z);
            targetparcel.Update(bot.GetClient.Network.CurrentSim, false);
            return BasicReply("ok", "SetParcelLandingZone", new [] { x, y, z });
        }


        [About("Updates the current parcels name")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ReturnHintsFailure("Parcel name is empty")]
        [ArgHints("name", "URLARG", "The new name of the parcel")]
        [Route(HttpVerbs.Get, "/SetParcelName/{name}/{token}")]
        public object SetParcelName(string name, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "SetParcelName");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "SetParcelName", new [] { name });
            }
            if (helpers.notempty(name) == false)
            {
                return Failure("Parcel name is empty", "SetParcelName", new [] { name });
            }
            targetparcel.Name = name;
            targetparcel.Update(bot.GetClient.Network.CurrentSim, false);
            return BasicReply("ok", "SetParcelName", new [] { name });
        }

        [About("Updates the current parcels description")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ArgHints("desc", "Text", "The new desc of the parcel")]
        [Route(HttpVerbs.Post, "/SetParcelDesc/{token}")]
        public object SetParcelDesc([FormField] string desc, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "SetParcelDesc");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "SetParcelDesc", new [] { desc });
            }
            targetparcel.Desc = desc;
            targetparcel.Update(bot.GetClient.Network.CurrentSim, false);
            return BasicReply("ok", "SetParcelDesc", new [] { desc });
        }

        [About("Fetchs the current parcels desc")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [Route(HttpVerbs.Get, "/GetParcelDesc/{token}")]
        public object GetParcelDesc(string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "GetParcelDesc");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "GetParcelDesc");
            }
            return BasicReply(targetparcel.Desc, "GetParcelDesc");
        }

        [About("gets the flags for the parcel")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [Route(HttpVerbs.Get, "/GetParcelFlags/{token}")]
        public object GetParcelFlags(string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "GetParcelFlags");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "GetParcelFlags");
            }
            Dictionary<string, ParcelFlags> flags = parcel_static.get_flags_list();
            Dictionary<string, string> collection = new Dictionary<string, string>();
            foreach (ParcelFlags cfg in flags.Values)
            {
                collection.Add(cfg.ToString(), targetparcel.Flags.HasFlag(cfg).ToString());
            }
            SuccessNoReturn("GetParcelFlags");
            return collection;
        }

        [About("Ejects an avatar")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Invaild avatar")]
        [ArgHints("avatar", "URLARG", "uuid of the avatar or Firstname Lastname")]
        [Route(HttpVerbs.Get, "/ParcelEject/{avatar}/{token}")]
        public object ParcelEject(string avatar,string token)
        {
            if (tokens.Allow(token, "parcel", "ParcelEject", handleGetClientIP()) == false)
            {
                return Failure("Token not accepted", "ParcelEject", new [] { avatar });
            }
            ProcessAvatar(avatar);
            if(avataruuid == UUID.Zero)
            {
                return Failure("Invaild avatar", "ParcelEject", new [] { avatar });
            }
            bot.GetClient.Parcels.EjectUser(avataruuid, false);
            return BasicReply("ok", "ParcelEject", new [] { avatar });
        }

        [About("Abandons the parcel the bot is currently on, returning it to Linden's or Estate owner")]
        [ReturnHints("ok")]
        [Route(HttpVerbs.Get, "/AbandonLand/{token}")]
        public object AbandonLand(string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "AbandonLand");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "AbandonLand");
            }
            bot.GetClient.Parcels.ReleaseParcel(bot.GetClient.Network.CurrentSim, targetparcel.LocalID);
            return BasicReply("ok", "AbandonLand");
        }


        [About("Bans an avatar from a parcel")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ReturnHintsFailure("Invaild avatar")]
        [ReturnHintsFailure("Avatar is in the blacklist")]
        [ArgHints("avatar", "URLARG", "uuid of the avatar or Firstname Lastname")]
        [Route(HttpVerbs.Get, "/ParcelBan/{avatar}/{token}")]
        public object ParcelBan(string avatar, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "ParcelBan");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "ParcelBan", new [] { avatar });
            }
            ProcessAvatar(avatar);
            if (avataruuid == UUID.Zero)
            {
                return Failure("Invaild avatar", "ParcelBan", new [] { avatar });
            }
            bool alreadyBanned = false;
            foreach (ParcelAccessEntry E in targetparcel.AccessBlackList)
            {
                if (E.AgentID == avataruuid)
                {
                    alreadyBanned = true;
                    break;
                }
            }
            if (alreadyBanned == true)
            {
                return BasicReply("Avatar is in the blacklist", "ParcelBan", new [] { avatar });
            }
            ParcelAccessEntry entry = new ParcelAccessEntry();
            entry.AgentID = avataruuid;
            entry.Flags = AccessList.Ban;
            entry.Time = new System.DateTime(3030, 03, 03);
            targetparcel.AccessBlackList.Add(entry);
            targetparcel.Update(bot.GetClient.Network.CurrentSim, false);
            return BasicReply("ok", "ParcelBan", new [] { avatar });
        }

        [About("Unbans an avatar from a parcel")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ReturnHintsFailure("Invaild avatar")]
        [ReturnHintsFailure("Avatar is already unbanned")]
        [ArgHints("avatar", "URLARG", "uuid of the avatar or Firstname Lastname")]
        [Route(HttpVerbs.Get, "/ParcelUnBan/{avatar}/{token}")]
        public object ParcelUnBan(string avatar, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "ParcelUnBan");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "ParcelUnBan", new [] { avatar });
            }
            ProcessAvatar(avatar);
            if (avataruuid == UUID.Zero)
            {
                return Failure("Invaild avatar", "ParcelUnBan", new [] { avatar });
            }
            bool alreadyBanned = false;
            ParcelAccessEntry removeentry = new ParcelAccessEntry();
            foreach (ParcelAccessEntry E in targetparcel.AccessBlackList)
            {
                if (E.AgentID == avataruuid)
                {
                    alreadyBanned = true;
                    removeentry = E;
                    break;
                }
            }
            if (alreadyBanned == false)
            {
                return BasicReply("Avatar is already unbanned", "ParcelUnBan", new [] { avatar });
            }
            targetparcel.AccessBlackList.Remove(removeentry);
            targetparcel.Update(bot.GetClient.Network.CurrentSim, false);
            return BasicReply("ok", "ParcelUnBan", new [] { avatar });
        }


        [About("Updates the current parcels name")]
        [ReturnHints("true|false")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ArgHints("musicurl", "URLARG", "The new name of the parcel")]
        [Route(HttpVerbs.Get, "/SetParcelMusic/{musicurl}/{token}")]
        public object SetParcelMusic(string musicurl, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "SetParcelMusic");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "SetParcelMusic", new [] { musicurl });
            }
            bool status = parcel_static.set_parcel_music(bot, targetparcel, musicurl);
            return BasicReply(status.ToString(), "SetParcelMusic", new [] { musicurl });
        }

        [About("Updates the current parcels name")]
        [ReturnHints("true|false")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ReturnHintsFailure("Incorrect perms to control parcel")]
        [ReturnHintsFailure("No accepted flags")]
        [ReturnHintsFailure("Unable to set flag ...")]
        [ReturnHintsFailure("Flag: ? is unknown")]
        [ReturnHintsFailure("Flag: ? missing \"=\"")]
        [ArgHints("escapedflagdata", "Text", "repeatable flag data split by ::: formated Flag=True|False")]
        [Route(HttpVerbs.Post, "/SetParcelFlag/{escapedflagdata}/{token}")]
        public object SetParcelFlag([FormField] string escapedflagdata, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "SetParcelFlag");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "SetParcelFlag", new [] { escapedflagdata });
            }
            List<string> acceptablewords = new List<string>();
            Dictionary<string, ParcelFlags> flags = parcel_static.get_flags_list();
            acceptablewords.AddRange(new[] { "True", "False" });

            Dictionary<string, bool> setflags = new Dictionary<string, bool>();
            string[] args = escapedflagdata.Split(":::");
            foreach (string a in args)
            {
                string[] parts = a.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    LogFormater.Warn("Flag: " + a + " missing \"=\"");
                    continue;
                }
                if (flags.ContainsKey(parts[0]) == false)
                {
                    LogFormater.Warn("Flag: " + parts[0] + " is unknown");
                    continue;
                }
                if (acceptablewords.Contains(parts[1]) == false)
                {
                    LogFormater.Crit("Unable to set flag " + parts[0] + " to : " + parts[1] + "");
                }
                setflags.Add(parts[0], Convert.ToBoolean(parts[1]));
            }
            if (setflags.Count == 0)
            {
                return Failure("No accepted flags", "SetParcelFlag", new [] { escapedflagdata });
            }
            if (parcel_static.has_parcel_perm(targetparcel, bot) == false)
            {
                return Failure("Incorrect perms to control parcel", "SetParcelFlag", new [] { escapedflagdata });
            }
            foreach (KeyValuePair<string, bool> cfg in setflags)
            {
                if (flags.ContainsKey(cfg.Key) == true)
                {
                    parcel_static.ParcelSetFlag(flags[cfg.Key], targetparcel, cfg.Value);
                }
            }
            targetparcel.Update(bot.GetClient.Network.CurrentSim, false);
            return BasicReply("Applying perms", "SetParcelFlag", new [] { escapedflagdata });
        }

        [About("Returns all objects from the current parcel for the selected avatar")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ReturnHintsFailure("Invaild avatar UUID")]
        [ArgHints("avatar", "URLARG", "avatar uuid or Firstname Lastname")]
        [Route(HttpVerbs.Get, "/ParcelReturnTargeted/{avatar}/{token}")]
        public object ParcelReturnTargeted(string avatar, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "ParcelReturnTargeted");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "ParcelReturnTargeted", new [] { avatar });
            }
            ProcessAvatar(avatar);
            if (avataruuid == UUID.Zero)
            {
                return Failure("Invaild avatar UUID", "ParcelReturnTargeted", new [] { avatar });
            }
            bot.GetClient.Parcels.ReturnObjects(bot.GetClient.Network.CurrentSim, targetparcel.LocalID, ObjectReturnType.None, new List<UUID>() { avataruuid });
            return BasicReply("ok", "ParcelReturnTargeted", new [] { avatar });
        }


        [About("transfers the current parcel ownership to the assigned group")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ReturnHintsFailure("Invaild group uuid")]
        [ReturnHintsFailure("Not in group")]
        [Route(HttpVerbs.Get, "/ParcelDeedToGroup/{group}/{token}")]
        public object ParcelDeedToGroup(string group, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "ParcelDeedToGroup");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "ParcelDeedToGroup", new [] { group });
            }
            if (UUID.TryParse(group, out UUID groupuuid) == false)
            {
                return Failure("Invaild group uuid", "ParcelDeedToGroup", new [] { group });
            }
            if (bot.MyGroups.ContainsKey(groupuuid) == false)
            {
                return Failure("Not in group", "ParcelDeedToGroup", new [] { group });
            }
            targetparcel.GroupID = groupuuid;
            targetparcel.Update(bot.GetClient.Network.CurrentSim, false);
            Thread.Sleep(500);
            bot.GetClient.Parcels.DeedToGroup(bot.GetClient.Network.CurrentSim, targetparcel.LocalID, groupuuid);
            return BasicReply("ok", "ParcelDeedToGroup", new [] { group });
        }

        [About("Attempts to buy the parcel the bot is standing on, the amount must match the sale price for the land!")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ReturnHintsFailure("Parcel not for sale")]
        [ReturnHintsFailure("Parcel not for sale")]
        [ReturnHintsFailure("Parcel sale locked to other avatars")]
        [ReturnHintsFailure("Parcel sale price and amount do not match")]
        [ReturnHintsFailure("Invaild amount")]
        [ArgHints("amount", "URLARG", "amount to pay for the parcel (min 1)")]
        [Route(HttpVerbs.Get, "/ParcelBuy/{amount}/{token}")]
        public object ParcelBuy(string amount, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "ParcelBuy");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "ParcelBuy", new [] { amount });
            }
            if ((targetparcel.AuthBuyerID != UUID.Zero) && (targetparcel.AuthBuyerID != bot.GetClient.Self.AgentID))
            {
                return Failure("Parcel sale locked to other avatars", "ParcelBuy", new[] { amount });
            }
            int minAmount = 1;
            if (targetparcel.AuthBuyerID == bot.GetClient.Self.AgentID)
            {
                minAmount = 0;
            }
            if (int.TryParse(amount, out int amountvalue) == false)
            {
                return Failure("Invaild amount", "ParcelBuy", new [] { amount });
            }
            if (amountvalue < minAmount)
            {
                return Failure("Invaild amount", "ParcelBuy", new [] { amount });
            };
            if (targetparcel.Flags.HasFlag(ParcelFlags.ForSale) == false)
            {
                return Failure("Parcel not for sale", "ParcelBuy", new [] { amount });
            }
            if (targetparcel.SalePrice != amountvalue)
            {
                return Failure("Parcel sale price and amount do not match", "ParcelBuy", new [] { amount });
            }
            bot.GetClient.Parcels.Buy(bot.GetClient.Network.CurrentSim, targetparcel.LocalID, false, UUID.Zero, false, targetparcel.Area, amountvalue);
            return BasicReply("ok", "ParcelBuy", new [] { amount });
        }

        [About("Freezes an avatar")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Invaild avatar UUID")]
        [ReturnHintsFailure("Invaild state")]
        [ArgHints("avatar", "URLARG", "avatar uuid or Firstname Lastname")]
        [ArgHints("state", "URLARG", "setting state to false will unfreeze or true to freeze")]
        [Route(HttpVerbs.Get, "/ParcelFreeze/{avatar}/{state}/{token}")]
        public object ParcelFreeze(string avatar, string state, string token)
        {
            if (tokens.Allow(token, "parcel", "ParcelFreeze", handleGetClientIP()) == false)
            {
                return Failure("Token not accepted", "ParcelFreeze", new [] { avatar, state });
            }
            if(bool.TryParse(state, out bool freezestate) == false)
            {
                return Failure("Invaild state", "ParcelFreeze", new [] { avatar, state });
            }
            ProcessAvatar(avatar);
            if (avataruuid == UUID.Zero)
            {
                return Failure("Invaild avatar UUID", "ParcelFreeze", new [] { avatar, state });
            }
            bot.GetClient.Parcels.FreezeUser(avataruuid, freezestate);
            return BasicReply("ok", "ParcelFreeze", new [] { avatar, state });
        }


        [About("Fetchs the parcel ban list of the parcel the bot is currently on<br/>If the name returned is lookup the bot is currently requesting the avatar name")]
        [ReturnHintsFailure("json object: GetParcelBanlistObject")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [Route(HttpVerbs.Get, "/GetParcelBanlist/{token}")]
        public object GetParcelBanlist(string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "GetParcelBanlist");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "GetParcelBanlist");
            }
            GetParcelBanlistObject reply = new GetParcelBanlistObject();
            int delays = 0;
            bool haslookup = true;
            while ((haslookup == true) && (delays < 3))
            {
                haslookup = false;
                foreach (ParcelAccessEntry e in targetparcel.AccessBlackList)
                {
                    if (e.AgentID != UUID.Zero)
                    {
                        if (bot.FindAvatarKey2Name(e.AgentID) == "lookup")
                        {
                            haslookup = true;
                        }
                    }
                }
                if(haslookup == true)
                {
                    Thread.Sleep(1000);
                    delays++;
                }
            }
            foreach (ParcelAccessEntry e in targetparcel.AccessBlackList)
            {
                string name = bot.FindAvatarKey2Name(e.AgentID);
                reply.entrys.Add(e.AgentID, name);
            }
            reply.reportedEntrys = targetparcel.AccessBlackList.Count;
            reply.delay = delays * 1000;
            reply.parcelName = targetparcel.Name;
            reply.regionName = bot.GetClient.Network.CurrentSim.Name;
            return BasicReply(JsonConvert.SerializeObject(reply), "GetParcelBanlist");
        }

        [About("Returns a rezzed object")]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Invaild object uuid")]
        [ReturnHintsFailure("Unable to find object")]
        [ArgHints("objectuuid", "URLARG", "object UUID to unrez")]
        [Route(HttpVerbs.Get, "/UnRezObject/{objectuuid}/{token}")]
        public object UnRezObject(string objectuuid,string token)
        {
            if (tokens.Allow(token, "parcel", "UnRezObject", handleGetClientIP()) == false)
            {
                return Failure("Token not accepted", "UnRezObject", new [] { objectuuid });
            }
            if (UUID.TryParse(objectuuid, out UUID targetobject) == false)
            {
                return Failure("Invaild object uuid", "UnRezObject", new [] { objectuuid });
            }
            bool found = false;
            Dictionary<uint, Primitive> objects_copy = bot.GetClient.Network.CurrentSim.ObjectsPrimitives.Copy();
            foreach (KeyValuePair<uint, Primitive> Obj in objects_copy)
            {
                if (Obj.Value.ID == targetobject)
                {
                    bot.GetClient.Inventory.RequestDeRezToInventory(Obj.Key);
                    found = true;
                    break;
                }
            }
            if(found == false)
            {
                return Failure("Unable to find object", "UnRezObject", new [] { objectuuid });
            }
            return BasicReply("ok", "UnRezObject", new [] { objectuuid });
        }

        [About("Updates the current parcels media settings \n" +
            "MediaAutoScale=Bool (True|False)\n" +
            "MediaLoop=Bool (True|False)\n" +
            "MediaID=UUID (Texture)\n" +
            "MediaURL=String\n" +
            "MediaDesc=String\n" +
            "MediaHeight=Int (256 to 1024)\n" +
            "MediaWidth=Int (256 to 1024)\n" +
            "MediaType=String [\"IMG-PNG\",\"IMG-JPG\",\"VID-MP4\",\"VID-AVI\" or \"Custom-MIME_TYPE_CODE\"]"
            )]
        [ReturnHints("ok")]
        [ReturnHintsFailure("Error not in a sim")]
        [ReturnHintsFailure("Parcel data not ready")]
        [ArgHints("escapedflagdata", "Text", "repeatable flag data split by ::: formated Flag=True|False")]
        [Route(HttpVerbs.Get, "/ParcelSetMedia/{token}")]
        public object ParcelSetMedia([FormField] string escapedflagdata, string token)
        {
            KeyValuePair<bool, string> tests = SetupCurrentParcel(token, "parcel", "ParcelSetMedia");
            if (tests.Key == false)
            {
                return Failure(tests.Value, "ParcelSetMedia", new [] { escapedflagdata });
            }
            string[] args = escapedflagdata.Split(":::");
            foreach (string A in args)
            {
                string[] bits = A.Split('=');
                if (bits.Length == 2)
                {
                    if (bits[0] == "MediaType")
                    {
                        UpdateParcel_MediaType(targetparcel, bits[1]);
                    }
                    else if ((bits[0] == "MediaWidth") || (bits[0] == "MediaHeight"))
                    {
                        if (int.TryParse(bits[1], out int size) == true)
                        {
                            if ((size >= 256) || (size <= 1024))
                            {
                                if (bits[0] == "MediaHeight")
                                {
                                    targetparcel.Media.MediaHeight = size;
                                }
                                else if (bits[0] == "MediaWidth")
                                {
                                    targetparcel.Media.MediaWidth = size;
                                }
                            }
                        }
                    }
                    else if (bits[0] == "MediaID")
                    {
                        if (UUID.TryParse(bits[1], out UUID texture) == true)
                        {
                            targetparcel.Media.MediaID = texture;
                        }
                    }
                    else if (bits[0] == "MediaURL")
                    {
                        targetparcel.Media.MediaURL = bits[1];
                    }
                    else if (bits[0] == "MediaDesc")
                    {
                        targetparcel.Media.MediaDesc = bits[1];
                    }
                    else if (bits[0] == "MediaAutoScale")
                    {
                        if (bool.TryParse(bits[1], out bool output) == true)
                        {
                            targetparcel.Media.MediaAutoScale = output;
                        }
                    }
                    else if (bits[0] == "MediaLoop")
                    {
                        if (bool.TryParse(bits[1], out bool output) == true)
                        {
                            targetparcel.Media.MediaLoop = output;
                        }
                    }
                }
            }
            targetparcel.Update(bot.GetClient.Network.CurrentSim, false);
            return BasicReply("ok", "ParcelSetMedia", new [] { escapedflagdata });
        }

        protected bool UpdateParcel_MediaType(Parcel targetparcel, string value)
        {
            if (value == "IMG-PNG")
            {
                targetparcel.Media.MediaType = "image/png";
            }
            else if (value == "IMG-JPG")
            {
                targetparcel.Media.MediaType = "image/jpeg";
            }
            else if (value == "VID-MP4")
            {
                targetparcel.Media.MediaType = "video/mp4";
            }
            else if (value == "VID-AVI")
            {
                targetparcel.Media.MediaType = "video/x-msvideo";
            }
            else if (value.StartsWith("Custom-") == true)
            {
                string mime = value;
                mime = mime.Replace("Custom-", "");
                targetparcel.Media.MediaType = mime;
            }
            else
            {
                return false;
            }
            return true;
        }
    }

    public class GetParcelBanlistObject
    {
        public Dictionary<UUID, String> entrys = new Dictionary<UUID, string>();
        public string parcelName = "";
        public string regionName = "";
        public int delay = 0;
        public int reportedEntrys = 0;
    }
}
