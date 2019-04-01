using SysSancBot.Enums;

namespace SysSancBot.Common
{
    public static class Util
    {
        public static ChannelRole GetChannelTypeFromString(string s)
        {
            switch (s)
            {
                case "safe":
                    return ChannelRole.Safe;
                case "unsafe":
                    return ChannelRole.Unsafe;
                case "admin":
                    return ChannelRole.Admin;
                default:
                    return ChannelRole.Unmonitored;
            }
        }

        public static TriggerAction GetTriggerActionFromString(string s)
        {
            switch (s)
            {
                case "inform":
                    return TriggerAction.Inform;
                default:
                    return TriggerAction.Censor;
            }
        }
    }
}