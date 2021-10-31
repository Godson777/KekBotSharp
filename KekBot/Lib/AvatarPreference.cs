using DSharpPlus.SlashCommands;

namespace KekBot.Lib
{
    public enum AvatarPreference
    {
        [ChoiceName("prefer server avatar")]
        Guild = 0, // default
        [ChoiceName("use global avatar")]
        Global,
    }
}