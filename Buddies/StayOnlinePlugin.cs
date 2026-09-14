using System;
using System.Reflection;
using System.Threading;

using AOSharp.Clientless;
using AOSharp.Clientless.Logging;

namespace CityBuddies
{
    public sealed class StayOnlinePlugin : ClientlessPluginEntry
    {
        // AOSharp.Clientless 1.0.16 opens StaticDynelData.bin with
        // FileShare.None. Use the same machine-local mutex as CityDwellers so
        // every AppDomain and process warms its private cache one at a time.
        private const string StaticDynelDataMutexName =
            @"Local\CityDwellers.StaticDynelData.1.0.16";
        private static readonly TimeSpan StaticDynelDataMutexTimeout =
            TimeSpan.FromSeconds(30);

        private bool _reportedInPlay;

        public override void Init(string pluginDir)
        {
            PreloadStaticDynelData();
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

        private static void PreloadStaticDynelData()
        {
            bool mutexHeld = false;

            using (var mutex = new Mutex(false, StaticDynelDataMutexName))
            {
                try
                {
                    try
                    {
                        mutexHeld = mutex.WaitOne(StaticDynelDataMutexTimeout);
                    }
                    catch (AbandonedMutexException)
                    {
                        // The previous owner exited while holding the mutex;
                        // this caller owns it now and can safely continue.
                        mutexHeld = true;
                    }

                    if (!mutexHeld)
                    {
                        throw new TimeoutException(
                            "Timed out waiting to preload AOSharp static-dynel data.");
                    }

                    Type dataType = typeof(DynelManager).Assembly.GetType(
                        "AOSharp.Clientless.StaticDynelData",
                        true);
                    PropertyInfo staticDynels = dataType.GetProperty(
                        "StaticDynels",
                        BindingFlags.Static | BindingFlags.NonPublic);

                    if (staticDynels == null)
                    {
                        throw new MissingMemberException(
                            dataType.FullName,
                            "StaticDynels");
                    }

                    staticDynels.GetValue(null, null);
                    Logger.Information(
                        $"Static-dynel data ready for {Client.CharacterName}.");
                }
                catch (TargetInvocationException ex)
                {
                    throw new InvalidOperationException(
                        "AOSharp static-dynel data preload failed.",
                        ex.InnerException ?? ex);
                }
                finally
                {
                    if (mutexHeld)
                        mutex.ReleaseMutex();
                }
            }
        }
    }
}
