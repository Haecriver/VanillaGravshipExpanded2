using System.Collections.Generic;
using Verse;

namespace VanillaGravshipExpanded2;

public class CompProperties_EnemyJammer : CompProperties
{
    public static readonly List<ThingDef> EnemySignalJammerDefs = [];

    public float jammingRange = 55.9f;

    public CompProperties_EnemyJammer() => compClass = typeof(CompEnemyJammer);

    public override void PostLoadSpecial(ThingDef parent)
    {
        base.PostLoadSpecial(parent);

        EnemySignalJammerDefs.Add(parent);
    }
}