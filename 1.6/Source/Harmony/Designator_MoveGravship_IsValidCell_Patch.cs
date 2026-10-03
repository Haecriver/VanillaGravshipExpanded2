using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaGravshipExpanded2
{
    [HarmonyPatch(typeof(Designator_MoveGravship), nameof(Designator_MoveGravship.IsValidCell))]
    public static class Designator_MoveGravship_IsValidCell_Patch
    {
        public static void Postfix(IntVec3 cell, Map map, ref AcceptanceReport __result)
        {
            if (IsCellJammed(cell, map))
            {
                __result = new AcceptanceReport("VGE_DestinationJammed".Translate());
                return;
            }
        }

        public static bool IsCellJammed(IntVec3 cell, Map map)
        {
            if (cell.IsValid && map != null)
            {
                for (var i = 0; i < CompProperties_EnemyJammer.EnemySignalJammerDefs.Count; i++)
                {
                    var things = map.listerThings.ThingsOfDef(CompProperties_EnemyJammer.EnemySignalJammerDefs[i]);
                    foreach (var t in things)
                    {
                        if (t is not ThingWithComps thing)
                            continue;

                        var jammer = thing.GetComp<CompEnemyJammer>();
                        // Range check
                        if (thing.Position.DistanceToSquared(cell) > jammer.Props.jammingRange * jammer.Props.jammingRange)
                            continue;

                        // Check if power comp not null and power is off
                        if (thing.GetComp<CompPowerTrader>()?.PowerOn == false)
                            continue;

                        // Check if stunnable comp not null and thing is stunned
                        if (thing.GetComp<CompStunnable>()?.StunHandler.Stunned == true)
                            continue;

                        return true;
                    }
                }
            }
            return false;
        }
    }
}
