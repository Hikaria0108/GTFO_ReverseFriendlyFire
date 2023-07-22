using HarmonyLib;
using Player;
using SNetwork;

namespace Hikaria.ReverseFriendlyFire.Patches
{
    [HarmonyPatch]
    internal static class Patch_BulletDamage
    {
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveBulletDamage))]
        [HarmonyPrefix]
        private static void Dam_PlayerDamageBase__ReceiveBulletDamage__Prefix(ref pBulletDamageData data)
        {
            if (SNet.IsMaster)
            {
                OnPreBulletDamage(ref data);
            }
        }

        private static void OnPreBulletDamage(ref pBulletDamageData data)
        {
            data.source.pRep.TryGetID(out IReplicator replicator);
            PlayerAgent sourcePlayer = replicator.OwningPlayer.PlayerAgent.Cast<PlayerAgent>();
            float damage = data.damage.Get(sourcePlayer.Damage.HealthMax);
            data.damage.Set(damage * EntryPoint.Instance.friendlyFireMulti, sourcePlayer.Damage.HealthMax);
            damage = Math.Clamp(damage * EntryPoint.Instance.reverseFriendlyFireMulti, 0f, sourcePlayer.Damage.HealthMax);
            float targetHealth = Math.Clamp(sourcePlayer.Damage.Health - damage, 0.01f, sourcePlayer.Damage.HealthMax);
            sourcePlayer.Damage.SendSetHealth(targetHealth);
        }
    }
}
