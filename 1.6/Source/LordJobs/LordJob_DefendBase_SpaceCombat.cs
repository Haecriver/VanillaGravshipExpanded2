using RimWorld;
using Verse;
using Verse.AI.Group;

namespace VanillaGravshipExpanded2;

public class LordJob_DefendBase_SpaceCombat : LordJob
{
    private Faction faction;
    private IntVec3 baseCenter;
    private bool attackWhenPlayerBecameEnemy;
    private int delayBeforeAssault;

    public LordJob_DefendBase_SpaceCombat()
    {
    }

    public LordJob_DefendBase_SpaceCombat(Faction faction, IntVec3 baseCenter, int delayBeforeAssault, bool attackWhenPlayerBecameEnemy = false)
    {
        this.faction = faction;
        this.baseCenter = baseCenter;
        this.attackWhenPlayerBecameEnemy = attackWhenPlayerBecameEnemy;
        this.delayBeforeAssault = delayBeforeAssault;
    }

    public override StateGraph CreateGraph()
    {
        var stateGraph = new StateGraph();
        // Defend base with transition to either defend while friendly, or defend while player present
        var defend_HostileToPlayer_NoPlayerPresent = new LordToil_DefendBase_SpaceCombat(baseCenter);
        // Only transition to assault player if player is present on the map
        var defend_HostileToPlayer_PlayerPresent = new LordToil_DefendBase_SpaceCombat(baseCenter);
        // Defend base while friendly to player, can't transition to assaulting player
        var defend_FriendlyToPlayer = new LordToil_DefendBase_SpaceCombat(baseCenter);
        // Assault player pawns state
        var assaultPlayer = new LordToil_AssaultColony(true);

        // Starting toil
        stateGraph.StartingToil = defend_HostileToPlayer_NoPlayerPresent;
        // Other toils
        stateGraph.AddToil(defend_FriendlyToPlayer);
        assaultPlayer.useAvoidGrid = true;
        stateGraph.AddToil(assaultPlayer);

        // Setup transition from being hostile to friendly
        var hostileToFriendlyBase = new Transition(defend_HostileToPlayer_NoPlayerPresent, defend_FriendlyToPlayer);
        hostileToFriendlyBase.AddSource(assaultPlayer);
        hostileToFriendlyBase.AddSource(defend_HostileToPlayer_PlayerPresent);
        hostileToFriendlyBase.AddTrigger(new Trigger_BecameNonHostileToPlayer());
        stateGraph.AddTransition(hostileToFriendlyBase);

        var hostileToFriendlyPlayerPresent = new Transition(defend_HostileToPlayer_PlayerPresent, defend_FriendlyToPlayer);
        hostileToFriendlyPlayerPresent.AddSource(assaultPlayer);
        hostileToFriendlyPlayerPresent.AddTrigger(new Trigger_BecameNonHostileToPlayer());
        stateGraph.AddTransition(hostileToFriendlyPlayerPresent);

        // Setup transition from being friendly to hostile
        var becomeHostileToPlayer = new Transition(defend_FriendlyToPlayer, attackWhenPlayerBecameEnemy ? assaultPlayer : defend_HostileToPlayer_NoPlayerPresent);
        if (attackWhenPlayerBecameEnemy)
            becomeHostileToPlayer.AddSource(defend_HostileToPlayer_NoPlayerPresent);
        becomeHostileToPlayer.AddTrigger(new Trigger_BecamePlayerEnemy());
        stateGraph.AddTransition(becomeHostileToPlayer);

        // Setup transition to be ready to assault if player arrived at map
        var getReadyAfterPlayerArrival = new Transition(defend_HostileToPlayer_NoPlayerPresent, defend_HostileToPlayer_PlayerPresent);
        getReadyAfterPlayerArrival.AddTrigger(new Trigger_PlayerPresentOnMap(GenDate.TicksPerHour));
        stateGraph.AddTransition(getReadyAfterPlayerArrival);

        // Setup transition to be passive if player left the map
        var getPassiveAfterPlayerDeparture = new Transition(defend_HostileToPlayer_PlayerPresent, defend_HostileToPlayer_NoPlayerPresent);
        getPassiveAfterPlayerDeparture.AddTrigger(new Trigger_PlayerNotPresentOnMap(GenDate.TicksPerHour));
        stateGraph.AddTransition(getPassiveAfterPlayerDeparture);

        // Transition from hostile defend to assaulting the player
        var startAssaultingPlayers = new Transition(defend_HostileToPlayer_PlayerPresent, assaultPlayer);
        startAssaultingPlayers.AddTrigger(new Trigger_FractionPawnsLost(0.2f));
        startAssaultingPlayers.AddTrigger(new Trigger_PawnHarmed(0.4f));
        startAssaultingPlayers.AddTrigger(new Trigger_ChanceOnTickInterval(GenDate.TicksPerHour, 0.03f));
        startAssaultingPlayers.AddTrigger(new Trigger_TicksPassed(delayBeforeAssault));
        startAssaultingPlayers.AddTrigger(new Trigger_UrgentlyHungry());
        startAssaultingPlayers.AddTrigger(new Trigger_ChanceOnPlayerHarmNPCBuilding(0.4f));
        startAssaultingPlayers.AddTrigger(new Trigger_OnClamor(ClamorDefOf.Ability));
        startAssaultingPlayers.AddPostAction(new TransitionAction_WakeAll());
        var taggedString = faction.def.messageDefendersAttacking.Formatted(faction.def.pawnsPlural, faction.Name, Faction.OfPlayer.def.pawnsPlural).CapitalizeFirst();
        startAssaultingPlayers.AddPreAction(new TransitionAction_Message(taggedString, MessageTypeDefOf.ThreatBig));
        stateGraph.AddTransition(startAssaultingPlayers);

        return stateGraph;
    }
}