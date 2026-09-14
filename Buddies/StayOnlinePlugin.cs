using AOSharp.Clientless;
using AOSharp.Clientless.Logging;

namespace CityBuddies
{
    public sealed class StayOnlinePlugin : ClientlessPluginEntry
    {
        private bool _reportedInPlay;

        public override void Init(string pluginDir)
        {
            Client.Config.AutoReconnect = true;
            Client.OnUpdate += OnUpdate;
            Logger.Information(
                $"CityBuddies login started for {Client.CharacterName}; AutoReconnect=True.");
        }

        public override void Teardown()
        {
            Client.OnUpdate -= OnUpdate;
        }

        private void OnUpdate(object sender, double deltaTime)
        {
            if (_reportedInPlay || !Client.InPlay)
                return;

            _reportedInPlay = true;
            Logger.Information($"City buddy ready: {Client.CharacterName} is in play.");
        }
    }
}

