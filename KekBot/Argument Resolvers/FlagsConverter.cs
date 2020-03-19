using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using KekBot.Arguments;
using KekBot.Utils;

namespace KekBot.ArgumentResolvers {
    class FlagsConverter : IArgumentConverter<FlagArgs> {
        public Task<Optional<FlagArgs>> ConvertAsync(string value, CommandContext ctx) => Task.FromResult(
            FlagArgs.ParseString(value).ToOptional()
        );
    }
}
