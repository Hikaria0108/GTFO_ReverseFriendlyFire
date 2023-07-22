using HarmonyLib;
using Agents;
using Player;
using Hikaria.ReverseFriendlyFire.Utils;
using SNetwork;

namespace Hikaria.ReverseFriendlyFire.Patches
{
    [HarmonyPatch]
    internal static class Patch_ExplosionDamage
    {
        [HarmonyPatch(typeof(Dam_PlayerDamageBase), nameof(Dam_PlayerDamageBase.ReceiveExplosionDamage))]
        [HarmonyPrefix]
        private static void Dam_PlayerDamageBase__ReceiveExplosionDamage__Prefix(Dam_PlayerDamageBase __instance, ref pExplosionDamageData data)
        {
            if (SNet.IsMaster)
            {
                OnPreExplosionDamage(__instance, ref data);
            }
        }

        [HarmonyPatch(typeof(MineDeployerInstance_Detect_Laser), nameof(MineDeployerInstance_Detect_Laser.Setup))]
        [HarmonyPrefix]
        private static void MineDeployerInstance_Detect_Laser__Setup__Prefix(MineDeployerInstance_Detect_Laser __instance, iMineDeployerInstanceCore core)
        {
            if (SNet.IsMaster)
            {
                int instanceID = core.Cast<MineDeployerInstance>().GetInstanceID();
                __instance.OnTargetDetected += (new Action(delegate ()
                {
                    TripMineDatas[instanceID].triggeredByDetection = true;
#if DEBUG
                    Logs.LogMessage(string.Format("Mine (OnTargetDetected), Mine: {0}", instanceID));
#endif
                }));
            }
        }

        [HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.OnSpawnData))]
        [HarmonyPostfix]
        private static void MineDeployerInstance__OnSpawnData__Postfix(MineDeployerInstance __instance, pItemSpawnData spawnData)
        {
            if (SNet.IsMaster)
            {
                if (spawnData.owner.TryGetPlayer(out SNet_Player player))
                {
                    TripMineData data = new TripMineData();
                    data.owner.Set(player.PlayerAgent.Cast<PlayerAgent>());
                    TripMineDatas.Add(__instance.GetInstanceID(), data);
                    TripMineDamagableOwner.Add(__instance.m_damage.GetInstanceID(), __instance.GetInstanceID());
#if DEBUG
                    Logs.LogMessage(string.Format("Mine (OnSpawnData), Mine: {0}, Owner: {1}", __instance.GetInstanceID(), player.NickName));
#endif
                }
            }
        }

        [HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.OnDestroy))]
        [HarmonyPrefix]
        private static void MineDeployerInstance__OnDestroy__Prefix(MineDeployerInstance __instance)
        {
            if (SNet.IsMaster)
            {
                OnPreMineDestroy(__instance);
            }
        }

        [HarmonyPatch(typeof(GenericDamageComponent), nameof(GenericDamageComponent.BulletDamage))]
        [HarmonyPrefix]
        private static void GenericDamageComponent__BulletDamage__Prefix(GenericDamageComponent __instance)
        {
            if (SNet.IsMaster)
            {
                int instanceID = __instance.GetInstanceID();
                if (TripMineDatas[TripMineDamagableOwner[instanceID]].type == MineType.Explosive)
                {
                    TripMineDatas[TripMineDamagableOwner[instanceID]].triggeredByLocalPlayer = true;
#if DEBUG
                    Logs.LogMessage(string.Format("Mine (OnTriggerByBulletDamage), Mine: {0}", TripMineDamagableOwner[instanceID]));
#endif
                }
            }
        }

        [HarmonyPatch(typeof(GenericDamageComponent), nameof(GenericDamageComponent.ExplosionDamage))]
        [HarmonyPrefix]
        private static void GenericDamageComponent__ExplosionDamage__Prefix(GenericDamageComponent __instance)
        {
            if (SNet.IsMaster)
            {
                int instanceID = __instance.GetInstanceID();
                if (TripMineDatas[TripMineDamagableOwner[instanceID]].type == MineType.Explosive)
                {
                    TripMineDatas[TripMineDamagableOwner[instanceID]].triggeredByExplosion = true;
#if DEBUG
                    Logs.LogMessage(string.Format("Mine (OnTriggerByExplosionDamage), Mine: {0}", TripMineDamagableOwner[instanceID]));
#endif
                }
            }
        }

        [HarmonyPatch(typeof(MineDeployerInstance_Detonate_Explosive), nameof(MineDeployerInstance_Detonate_Explosive.DoExplode))]
        [HarmonyPrefix]
        private static void MineDeployerInstance_Detonate_Explosive__DoExplode__Prefix(MineDeployerInstance_Detonate_Explosive __instance)
        {
            if (SNet.IsMaster)
            {
                int instanceID = __instance.m_core.Cast<MineDeployerInstance>().GetInstanceID();
                MineStack.Push(instanceID);
                if (TripMineDatas[instanceID].triggeredByExplosion && !TripMineDatas[instanceID].triggeredByDetection)
                {
                    TripMineDatas[instanceID].trigger = LastTrigger;
                }
                if (!TripMineDatas[instanceID].triggeredByDetection)
                {
                    if (TripMineDatas[instanceID].triggeredByLocalPlayer)
                    {
                        TripMineDatas[instanceID].trigger.Set(PlayerManager.GetLocalPlayerAgent());
                    }
                    else if (!TripMineDatas[instanceID].triggeredByExplosion)
                    {
                        SNet.Replication.TryGetLastSender(out SNet_Player player);
                        TripMineDatas[instanceID].trigger.Set(player.PlayerAgent.Cast<PlayerAgent>());
                    }
                    else if (!LastTrigger.TryGet(out PlayerAgent lastTrigger) || lastTrigger == null)
                    {
#if DEBUG
                        Logs.LogMessage(string.Format("Mine (OnTriggerExplosiveMine), Mine: {0}, Trigger: Null", instanceID));
#endif
                        return;
                    }
#if DEBUG
                    TripMineDatas[instanceID].trigger.TryGet(out PlayerAgent trigger);
                    Logs.LogMessage(string.Format("Mine (OnTriggerExplosiveMine), Mine: {0}, Trigger: {1}", instanceID, trigger.Owner.NickName));
#endif
                }
            }
        }

        [HarmonyPatch(typeof(MineDeployerInstance_Detonate_Explosive), nameof(MineDeployerInstance_Detonate_Explosive.DoExplode))]
        [HarmonyPostfix]
        private static void MineDeployerInstance_Detonate_Explosive__DoExplode__Postfix(MineDeployerInstance_Detonate_Explosive __instance)
        {
            if (SNet.IsMaster)
            {
                int instanceID = __instance.m_core.Cast<MineDeployerInstance>().GetInstanceID();
                LastTrigger = TripMineDatas[instanceID].trigger;
                MineStack.Pop();
            }
        }

        private static void OnPreExplosionDamage(Dam_PlayerDamageBase __instance, ref pExplosionDamageData data)
        {
            int currentMine = MineStack.Peek();
            float damage = data.damage.Get(__instance.HealthMax);
            TripMineDatas[currentMine].owner.TryGet(out PlayerAgent owner);
            if (owner.Owner.Lookup != __instance.Owner.Owner.Lookup)
            {
                data.damage.Set(damage * EntryPoint.Instance.friendlyFireMulti, __instance.HealthMax);
            }
            if (TripMineDatas[currentMine].triggeredByDetection)
            {
                return;
            }
            if (!TripMineDatas[currentMine].trigger.TryGet(out PlayerAgent trigger) || trigger == null)
            {
#if DEBUG
                Logs.LogMessage(string.Format("Mine (OnExplosionDamage), Mine: {0}, Trigger: Null", currentMine));
#endif
                return;
            }

            damage = Math.Clamp(damage * EntryPoint.Instance.reverseFriendlyFireMulti, 0f, __instance.HealthMax);
            float targetHealth = Math.Clamp(trigger.Damage.Health - damage, 0.01f, trigger.Damage.HealthMax);
            trigger.Damage.SendSetHealth(targetHealth);
#if DEBUG

            Logs.LogMessage(string.Format("Mine (OnExplosionDamage), Mine: {0}, Owner: {1}, Trigger: {2}, Target: {3}", currentMine, owner.Owner.NickName, trigger.Owner.NickName, __instance.Owner.Owner.NickName));
#endif
        }

        private static void OnPreMineDestroy(MineDeployerInstance __instance)
        {
            int instanceID = __instance.GetInstanceID();
#if DEBUG
            Logs.LogMessage(string.Format("Mine (OnDestroy), Mine: {0}", instanceID));
#endif
            if (TripMineDatas.ContainsKey(instanceID))
            {
                TripMineDatas.Remove(instanceID);
            }
            if (TripMineDamagableOwner.ContainsKey(__instance.m_damage.GetInstanceID()))
            {
                TripMineDamagableOwner.Remove(__instance.m_damage.GetInstanceID());
            }
        }

        public class TripMineData
        {
            public pPlayerAgent owner;

            public pPlayerAgent trigger;

            public MineType type;

            public bool triggeredByDetection;

            public bool triggeredByExplosion;

            public bool triggeredByLocalPlayer;
        }

        public enum MineType : byte
        {
            Explosive,
            Glue
        }

        private static readonly Stack<int> MineStack = new();

        private static readonly Dictionary<int, int> TripMineDamagableOwner = new();

        private static readonly Dictionary<int, TripMineData> TripMineDatas = new();

        private static pPlayerAgent LastTrigger = new();
    }
}
    