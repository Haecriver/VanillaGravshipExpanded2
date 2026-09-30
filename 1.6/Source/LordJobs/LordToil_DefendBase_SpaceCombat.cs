using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VanillaGravshipExpanded2;

public class LordToil_DefendBase_SpaceCombat(IntVec3 baseCenter) : LordToil
{
    public IntVec3 baseCenter = baseCenter;

    public override IntVec3 FlagLoc => baseCenter;

    public override void UpdateAllDuties()
    {
        for (var i = 0; i < lord.ownedPawns.Count; i++)
            lord.ownedPawns[i].mindState.duty = new PawnDuty(InternalDefOf.VGE_DefendBase_SpaceCombat, baseCenter);
    }
}