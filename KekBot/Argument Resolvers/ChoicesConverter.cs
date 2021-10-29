using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using KekBot.Arguments;

namespace KekBot.ArgumentResolvers {
    class ChoicesConverter : IArgumentConverter<ChoicesList> {

        public Task<Optional<ChoicesList>> ConvertAsync(string value, CommandContext ctx) =>
            Task.FromResult(Optional.FromValue(
                ChoicesList.Parse(value)
            ));
        
    }
}
