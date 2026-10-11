using System;
using EFT;
using EFT.Ballistics;
using HarmonyLib;
using UnityEngine;

namespace AlternateHud
{
    internal struct HitEvent
    {
        public float Damage;
        public EBodyPart Part;
        public Vector3 Point;
        public bool Killed;
    }

    internal struct DamageTakenEvent
    {
        public float Damage;
        public EBodyPart Part;
        public Vector3 SourceDirection;
    }

    internal struct KillEvent
    {
        public string Name, Weapon, Tag;
        public EBodyPart Part;
        public float Distance;
        public int Level;
        public bool Headshot;
    }

    internal static class HudEvents
    {
        public static event Action<HitEvent> HitDealt;
        public static event Action<DamageTakenEvent> DamageTaken;
        public static event Action<KillEvent> Kill;

        public static void Raise(HitEvent e) => HitDealt?.Invoke(e);
        public static void Raise(DamageTakenEvent e) => DamageTaken?.Invoke(e);
        public static void Raise(KillEvent e) => Kill?.Invoke(e);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ApplyDamageInfo))]
    internal static class ApplyDamageInfoPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance, out bool __state)
        {
            __state = __instance.HealthController != null && __instance.HealthController.IsAlive;
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance, DamageInfo damageInfo, EBodyPart bodyPartType, bool __state)
        {
            if (!__state) return;

            try
            {
                var main = HudRoot.MainPlayer;
                if (main == null) return;

                var attacker = damageInfo.Player?.iPlayer;

                if (__instance == main)
                {
                    HudEvents.Raise(new DamageTakenEvent
                    {
                        Damage = damageInfo.Damage,
                        Part = bodyPartType,
                        SourceDirection = attacker != null && attacker.ProfileId != main.ProfileId
                            ? attacker.Position - main.Position
                            : -damageInfo.Direction
                    });
                }
                else if (attacker != null && attacker.ProfileId == main.ProfileId)
                {
                    HudEvents.Raise(new HitEvent
                    {
                        Damage = damageInfo.Damage,
                        Part = bodyPartType,
                        Point = damageInfo.HitPoint,
                        Killed = !__instance.HealthController.IsAlive
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[AlternateHud] ApplyDamageInfo: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnBeenKilledByAggressor))]
    internal static class KilledPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance, IPlayer aggressor, DamageInfo damageInfo, EBodyPart bodyPart)
        {
            try
            {
                var main = HudRoot.MainPlayer;
                if (main == null || aggressor == null || aggressor.ProfileId != main.ProfileId ||
                    __instance == main)
                    return;

                var info = __instance.Profile.Info;
                var role = info.Settings.Role.ToString();
                string tag;
                if (role.StartsWith("boss", StringComparison.OrdinalIgnoreCase)) tag = "BOSS";
                else if (!__instance.IsAI) tag = "PMC";
                else if (info.Side == EPlayerSide.Savage) tag = "SCAV";
                else tag = "PMC BOT";

                HudEvents.Raise(new KillEvent
                {
                    Name = HudLocalization.Localized(__instance.Profile.Nickname),
                    Weapon = HudLocalization.Localized(damageInfo.Weapon?.ShortName ?? ""),
                    Part = bodyPart,
                    Distance = Vector3.Distance(aggressor.Position, __instance.Position),
                    Level = info.Level,
                    Headshot = bodyPart == EBodyPart.Head,
                    Tag = tag
                });
            }
            catch (Exception e)
            {
                Debug.LogError("[AlternateHud] Kill: " + e);
            }
        }
    }
}

