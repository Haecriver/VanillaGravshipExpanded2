using System.Collections.Generic;
using System.Linq;
using RimWorld;
using VanillaGravshipExpanded;
using Verse;
using Verse.AI;

namespace VanillaGravshipExpanded2;

public class JobGiver_AIFightFires : ThinkNode_JobGiver
{
    private static readonly Dictionary<ThingDef, JobDef> FireDefs = new();

    public float maxDist = 30f;

    static JobGiver_AIFightFires()
    {
        // Defs not initialized yet, need to handle it later.
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            FireDefs[ThingDefOf.Fire] = JobDefOf.BeatFire;
            FireDefs[VGEDefOf.VGE_Astrofire] = VGEDefOf.VGE_BeatAstrofire;
        });
    }

    public override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.WorkTagIsDisabled(WorkTags.Firefighting))
            return null;

        Thing closestFire = null;
        JobDef closestFireJobDef = null;
        var closestFireDistSquared = float.MaxValue;

        foreach (var (fireDef, jobDef) in FireDefs)
        {
            var fire = pawn.Map.listerThings.ThingsOfDef(fireDef)
                // Order by closest first (use squared distance since it works here, and is faster since we skip sqrt call)
                .OrderBy(x => pawn.Position.DistanceToSquared(x.Position))
                // Grab the first valid building matching our conditions
                .FirstOrDefault(x => ((AttachableThing)x).parent is not Pawn p || (p.Downed && p.Faction == pawn.Faction) && pawn.CanReserve(x) && pawn.CanReach(x, PathEndMode.Touch, Danger.Deadly));

            if (fire != null)
            {
                var distSquared = pawn.Position.DistanceToSquared(fire.Position);
                if (distSquared < closestFireDistSquared && distSquared <= maxDist * maxDist)
                {
                    closestFire = fire;
                    closestFireJobDef = jobDef;
                    closestFireDistSquared = distSquared;
                }
            }
        }

        if (closestFire == null || closestFireJobDef == null)
            return null;
        return JobMaker.MakeJob(closestFireJobDef, closestFire);
    }

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var node = (JobGiver_AIFightFires)base.DeepCopy(resolve);
        node.maxDist = maxDist;
        return node;
    }
}