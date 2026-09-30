using System.Collections.Generic;
using System.Linq;
using RimWorld;
using VanillaGravshipExpanded;
using Verse;
using Verse.AI;

namespace VanillaGravshipExpanded2;

public class JobGiver_AIManTerminal : ThinkNode_JobGiver
{
    private static readonly List<ThingDef> EnemyTerminals = [];

    public float maxDist = 30f;

    static JobGiver_AIManTerminal()
    {
        // Check in ExecuteWhenFinished in case DefDatabase isn't initialized yet
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            // Set up a list of all the defs that can be used as enemy terminals
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

    public override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.skills == null || pawn.skills.GetSkill(SkillDefOf.Intellectual).TotallyDisabled)
            return null;

        // Initial state - max distance (we always take lower values)
        var distanceSquared = float.MaxValue;
        Thing terminal = null;

        // Go through all the defs
        for (var i = 0; i < EnemyTerminals.Count; i++)
        {
            // Grab a list of things
            var thing = pawn.Map.listerThings.ThingsOfDef(EnemyTerminals[i])
                // Order by closest first (use squared distance since it works here, and is faster since we skip sqrt call)
                .OrderBy(x => pawn.Position.DistanceToSquared(x.Position))
                // Grab the first valid building matching our conditions
                .FirstOrDefault(x => x.Faction == pawn.Faction && pawn.Position.DistanceToSquared(x.Position) < maxDist * maxDist && pawn.CanReserve(x) && x.TryGetComp<CompMannable>() is CompMannable m && !m.MannedNow && pawn.CanReach(x, PathEndMode.InteractionCell, Danger.Deadly));

            if (thing != null)
            {
                // Check if the newly found stations is closer than the previous one
                var dist = pawn.Position.DistanceToSquared(thing.Position);
                if (dist < distanceSquared)
                {
                    terminal = thing;
                    distanceSquared = dist;
                }
            }
        }

        if (terminal != null)
            return JobMaker.MakeJob(InternalDefOf.VGE_OperateEnemyTerminal, terminal);
        return null;
    }

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var jobGiver = (JobGiver_AIManTerminal)base.DeepCopy(resolve);
        jobGiver.maxDist = maxDist;
        return jobGiver;
    }
}