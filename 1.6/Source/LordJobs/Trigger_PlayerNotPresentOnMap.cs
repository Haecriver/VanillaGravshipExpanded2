using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VanillaGravshipExpanded2;

public class Trigger_PlayerNotPresentOnMap(int checkInterval) : Trigger
{
    public override bool ActivateOn(Lord lord, TriggerSignal signal) => signal.type == TriggerSignalType.Tick && Find.TickManager.TicksGame % checkInterval == 0 && lord.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Count <= 0;
}