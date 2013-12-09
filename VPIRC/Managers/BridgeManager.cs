﻿using System;
using VP;
using Meebey.SmartIrc4net;

namespace VPIRC
{
    class BridgeManager
    {
        const string tag = "Bridge";

        public void Setup()
        {
            VPIRC.VP.Enter   += u => { onEnterLeave(u, Direction.Entering); };
            VPIRC.VP.Leave   += u => { onEnterLeave(u, Direction.Leaving); };
            VPIRC.VP.Message += onVPMessage;
            VPIRC.VP.Console += onVPConsole;

            VPIRC.IRC.Enter       += u => { onEnterLeave(u, Direction.Entering); };
            VPIRC.IRC.Leave       += u => { onEnterLeave(u, Direction.Leaving); };
            VPIRC.IRC.Message     += onIRCMessage;
            VPIRC.IRC.PrivMessage += onIRCPrivateMessage;
        } 

        public void Takedown() { }

        void onVPMessage(VPUser source, string message)
        {
            SendType sendType;
            var bot = VPIRC.IRC.GetBot(source);

            if (bot == null || bot.State != ConnState.Connected)
                return;

            if ( message.StartsWith("/me ") )
            {
                sendType = SendType.Action;
                message  = message.Substring(4);
            }
            else
                sendType = SendType.Message;

            bot.Client.SendMessage(sendType, VPIRC.IRC.Channel, message);
        }

        void onVPConsole(ConsoleMessage message)
        {
            if ( string.IsNullOrWhiteSpace(message.Name) || message.Name.IEquals(VPIRC.VP.Root.Name) )
                return;

            var outMsg = "{0}: {1}".LFormat(message.Name, message.Message);
            var bot    = VPIRC.IRC.Root;
            var prefix = (message.Effect & ChatEffect.Bold) == ChatEffect.Bold
                ? "\u0002"
                : "";

            if (bot == null || bot.State != ConnState.Connected)
                return;

            bot.Client.SendMessage(SendType.Message, VPIRC.IRC.Channel, outMsg);
        }
        
        void onIRCMessage(IRCUser source, string message, bool action)
        {
            var bot    = VPIRC.VP.GetBot(source);
            var prefix = action ? "/me " : "";

            if (bot == null || bot.State != ConnState.Connected)
                return;

            if (message.Length <= 250)
                bot.Bot.Say("{0}{1}", prefix, message);
            else while (message.Length > 0)
            {
                var len   = Math.Min(message.Length, 250);
                var chunk = message.Substring(0, len);
                message   = message.Substring(len);

                bot.Bot.Say("{0}{1}", prefix, chunk);
            }
        }

        void onIRCPrivateMessage(IRCUser source, VPUser target, string message, bool action)
        {
            var prefix    = action ? "/me " : "";
            var sourceBot = VPIRC.VP.GetBot(source).VPName;
            var bot       = VPIRC.VP.Root.Bot;

            foreach (var session in target.Sessions)
                if (message.Length <= 250)
                    bot.ConsoleMessage(session, ChatEffect.Italic, Colors.Private, sourceBot, "{0}{1}", prefix, message);
                else while (message.Length > 0)
                {
                    var len   = Math.Min(message.Length, 250);
                    var chunk = message.Substring(0, len);
                    message   = message.Substring(len);

                    bot.ConsoleMessage(session, ChatEffect.Italic, Colors.Private, sourceBot, "{0}{1}", prefix, message);
                }
        }

        void onEnterLeave(User user, Direction dir)
        {
            if (user is IRCUser)
            {
                if (dir == Direction.Entering)
                    VPIRC.VP.Add(user as IRCUser);
                else
                    VPIRC.VP.Remove(user as IRCUser);
            }
            else
            {
                if (dir == Direction.Entering)
                    VPIRC.IRC.Add(user as VPUser);
                else
                    VPIRC.IRC.Remove(user as VPUser);
            }
        }
    }

    public enum Direction
    {
        Entering,
        Leaving
    }
}
