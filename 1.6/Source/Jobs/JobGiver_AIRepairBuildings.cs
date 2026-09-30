using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaGravshipExpanded2;

public class JobGiver_AIRepairBuildings : ThinkNode_JobGiver
{
    public float maxDist = 30f;

    public override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.skills == null || pawn.skills.GetSkill(SkillDefOf.Construction).TotallyDisabled)
            return null;

        var building = pawn.Map.listerBuildingsRepairable.HashSetFor(pawn.Faction)
            // Order by closest first (use squared distance since it works here, and is faster since we skip sqrt call)
            .OrderBy(x => pawn.Position.DistanceToSquared(x.Position))
            // Grab the first valid building matching our conditions
            .FirstOrDefault(x => x.Faction == pawn.Faction && pawn.Position.DistanceToSquared(x.Position) < maxDist * maxDist && x.def.useHitPoints && x.HitPoints < x.MaxHitPoints && pawn.CanReserve(x) && pawn.CanReach(x, PathEndMode.Touch, Danger.Deadly));

        if (building != null)
            return JobMaker.MakeJob(JobDefOf.Repair, building);
        return null;
    }

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        var jobGiver = (JobGiver_AIRepairBuildings)base.DeepCopy(resolve);
        jobGiver.maxDist = maxDist;
        return jobGiver;
    }
}