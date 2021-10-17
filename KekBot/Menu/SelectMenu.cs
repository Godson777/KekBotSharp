using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using ImageMagick;
using RethinkDb.Driver.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KekBot.Menu {
    class SelectMenu : Menu {
        public DiscordColor Color { private get; set; }
        public string Text { private get; set; }
        public string Description { private get; set; }
        public string Placeholder { protected get; set; } = "Select an option.";
        public int MinChoices { private get; set; } = 1;
        public int MaxChoices { private get; set; } = 1;
        public Action<DiscordMessage, string[]> Action { private get; set; }
        public Action<DiscordMessage> CancelAction { private get; set; } = async x => await x.ModifyAsync(new DiscordMessageBuilder().WithContent(x.Content));

        private DiscordSelectComponent SelectMen;
        private DiscordButtonComponent CancelBtn = new DiscordButtonComponent(ButtonStyle.Danger, "cancel", "Cancel");
        private List<DiscordSelectComponentOption> Choices = new List<DiscordSelectComponentOption>();

        public SelectMenu(InteractivityExtension interactivity) : base(interactivity) {
        }
        
        public override async Task Display(DiscordChannel channel) {
            DisplayChecks();
            SelectMen = new DiscordSelectComponent("select_menu", Placeholder, Choices, false, MinChoices, Math.Min(MaxChoices, Choices.Count));
            await Engage(await channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent(Text)
                .AddComponents(SelectMen)
                .AddComponents(CancelBtn)));
        }

        public override async Task Display(DiscordMessage message) {
            DisplayChecks();
            SelectMen = new DiscordSelectComponent("select_menu", Placeholder, Choices, false, MinChoices, Math.Min(MaxChoices, Choices.Count));
            await Engage(await message.ModifyAsync(new DiscordMessageBuilder()
                .WithContent(Text)
                .AddComponents(new DiscordComponent[] {
                    SelectMen, CancelBtn
                })));
        }

        public void AddChoice(string id, string label, string description = null) {
            Choices.Add(new DiscordSelectComponentOption(label.Length > 25 ? label.Substring(0,22) + "..." : label, id, description.Length > 50 ? description.Substring(0,47) + "..." : description));
        }

        private protected override void DisplayChecks() {
            if (Users.Count == 0 && Roles.Count == 0) throw new MenuFailedException();
            if (Choices.Count == 0) throw new MenuFailedException("Must have more than one choice.");
            if (Choices.Count > 25) throw new MenuFailedException("Must have no more than 25 choices.");
            if (MaxChoices > 25) throw new MenuFailedException("Max Choices must not be over 25.");
            if (Action == null) throw new MenuFailedException("Action can not be null.");
            if (Text == null && Description == null) throw new MenuFailedException("Text and description must be provided.");
        }

        private async Task Engage(DiscordMessage message) {
            var result = await Interactivity.WaitForEventArgsAsync<ComponentInteractionCreateEventArgs>((args) => {
                if (args.Message.Id == message.Id) {
                    if (args.Id == SelectMen.CustomId || args.Id == CancelBtn.CustomId) {
                        return true;
                    }
                    return false;
                }
                return false;
            }, Timeout);

            if (result.TimedOut) {
                CancelAction?.Invoke(message);
                return;
            }

            //Throw an error as an emphemeral message if the user attempting to interact isn't "valid".
            if (!IsValidUser(result.Result.Interaction.User, result.Result.Interaction.Guild)) {
                await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Only the person who ran the command can interact with this menu.").AsEphemeral(true));
                await Engage(result.Result.Message);
                return;
            }

            await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
            if (result.Result.Id == CancelBtn.CustomId) CancelAction.Invoke(result.Result.Message); 
            else Action?.Invoke(result.Result.Message, result.Result.Values);
        }
    }
}
