using System.Threading.Tasks;

namespace KekBot.Lib {
    /// Note: this will NOT work for slash commands.
    interface INeedsInitialized {

        Task Initialize();

    }
}
