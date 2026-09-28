using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using RimWorld;
using VanillaGravshipExpanded;
using Verse;
using Verse.AI;

namespace VanillaGravshipExpanded2
{
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class JobGiver_AIFightEnemy_TryGiveJob_Patch
    {
        private static readonly List<ThingDef> EnemyTerminals = [];

        static JobGiver_AIFightEnemy_TryGiveJob_Patch()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                for (var i = 0; i < DefDatabase<ThingDef>.AllDefsListForReading.Count; i++)
                {
                    var def = DefDatabase<ThingDef>.AllDefsListForReading[i];
                    if (def.HasComp<CompEnemyTerminal>() && def.HasComp<CompMannable>())
                        EnemyTerminals.Add(def);
                }

                // Trim the list
                EnemyTerminals.Capacity = EnemyTerminals.Count;
            });
        }

        public static void Postfix(JobGiver_AIFightEnemy __instance, Pawn pawn, ref Job __result)
        {
            if (__result != null || pawn.Faction == null || pawn.Faction.IsPlayer) return;

            if (pawn.skills != null && !pawn.skills.GetSkill(SkillDefOf.Intellectual).TotallyDisabled)
            {
                var distanceSquared = float.MaxValue;
                Thing terminal = null;

                for (var i = 0; i < EnemyTerminals.Count; i++)
                {
                    var thing = pawn.Map.listerThings.ThingsOfDef(EnemyTerminals[i])
                        .OrderBy(x => pawn.Position.DistanceToSquared(x.Position))
                        .FirstOrDefault(x => x.Faction == pawn.Faction && pawn.Position.DistanceToSquared(x.Position) < 30f * 30f && pawn.CanReserve(x) && x.TryGetComp<CompMannable>() is CompMannable m && !m.MannedNow && pawn.CanReach(x, PathEndMode.InteractionCell, Danger.Deadly));

                    if (thing != null)
                    {
                        var dist = pawn.Position.DistanceToSquared(thing.Position);
                        if (dist < distanceSquared)
                        {
                            terminal = thing;
                            distanceSquared = dist;
                        }
                    }
                }

                if (terminal != null)
                {
                    __result = JobMaker.MakeJob(InternalDefOf.VGE_OperateEnemyTerminal, terminal);
                    return;
                }
            }

            if (pawn.skills != null && !pawn.skills.GetSkill(SkillDefOf.Construction).TotallyDisabled)
            {
                var building = pawn.Map.listerBuildingsRepairable.HashSetFor(pawn.Faction)
                    .OrderBy(x => pawn.Position.DistanceToSquared(x.Position))
                    .FirstOrDefault(x => x.Faction == pawn.Faction && pawn.Position.DistanceToSquared(x.Position) < 30f * 30f && x.def.useHitPoints && x.HitPoints < x.MaxHitPoints && pawn.CanReserve(x) && IsOnEnemyShipTerrain(x) && pawn.CanReach(x, PathEndMode.Touch, Danger.Deadly));
                if (building != null)
                {
                    __result = JobMaker.MakeJob(JobDefOf.Repair, building);
                }
            }
        }

        private static bool IsOnEnemyShipTerrain(Thing building)
        {
            var terrainGrid = building.Map.terrainGrid;
            foreach (var cell in building.OccupiedRect())
            {
                if (terrainGrid.TerrainAt(cell) is TerrainDef terrain && terrain.HasModExtension<EnemyShipTerrainExtension>()) return true;
                if (terrainGrid.FoundationAt(cell) is TerrainDef foundation && foundation.HasModExtension<EnemyShipTerrainExtension>()) return true;
            }

            return false;
        }
    }
}