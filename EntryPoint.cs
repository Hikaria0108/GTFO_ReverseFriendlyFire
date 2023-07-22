using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Hikaria.ReverseFriendlyFire.Utils;

namespace Hikaria.ReverseFriendlyFire
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class EntryPoint : BasePlugin
    {
        public override void Load()
        {
            Instance = this;

            ReverseFriendlyFireMulti = configFile.Bind("通用设置", "反伤倍率", 1f, "设置反伤倍率, 范围 0 - 2");
            FriendlyFireMulti = configFile.Bind("通用设置", "友伤倍率", 0f, "设置友伤倍率, 范围 0 - 1");

            reverseFriendlyFireMulti = Math.Clamp(ReverseFriendlyFireMulti.Value, 0f, 2f);
            friendlyFireMulti = Math.Clamp(FriendlyFireMulti.Value, 0f, 1f);

            m_Harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            m_Harmony.PatchAll();

            Logs.LogMessage("OK");
        }

        public static EntryPoint Instance { get; private set; }

        private Harmony m_Harmony;

        private static ConfigEntry<float> ReverseFriendlyFireMulti { get; set; }

        private static ConfigEntry<float> FriendlyFireMulti { get; set; }

        private static ConfigFile configFile = new(string.Concat(Paths.ConfigPath, "\\Hikaria\\ReverseFriendlyFire\\Hikaria.ReverseFriendlyFire.cfg"), true);

        public float reverseFriendlyFireMulti = 1f;

        public float friendlyFireMulti = 0f;
    }
}
