using RimWorld;
using Verse;
using Verse.AI;

namespace Emergency_Trasnfusion
{
    // Shared rules for the "Stabilize with hemogen" order, used by both the float menu and the job driver
    // so the two cannot disagree about what is allowed.
    public static class StabilizationUtility
    {
        public const int ApplyDurationTicks = 60;

        public static bool IsAvailable => ModsConfig.BiotechActive && ThingDefOf.HemogenPack != null;

        public static Hediff GetBloodLoss(Pawn patient)
        {
            return patient.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
        }

        // Whether the patient is a valid target at all. Mirrors vanilla drafted tending, but is keyed on
        // BloodLoss instead of tendable injuries. Does not look at the doctor's drafted state.
        public static bool IsValidPatient(Pawn doctor, Pawn patient)
        {
            if (!IsAvailable || patient == null || patient.Dead || !patient.Spawned || patient.Map != doctor.Map)
            {
                return false;
            }
            if (!patient.RaceProps.IsFlesh)
            {
                return false;
            }
            Hediff bloodLoss = GetBloodLoss(patient);
            if (bloodLoss == null || bloodLoss.Severity <= 0f)
            {
                return false;
            }
            if (patient.Downed)
            {
                return true;
            }
            if (patient.HostileTo(doctor.Faction))
            {
                return false;
            }
            if (patient.IsColonist || patient.IsQuestLodger() || patient.IsPrisonerOfColony || patient.IsSlaveOfColony
                || (patient.Faction == Faction.OfPlayer && patient.IsAnimal))
            {
                return true;
            }
            return patient.IsColonySubhuman && patient.mutant.Def.entitledToMedicalCare;
        }

        public static bool InAggroMentalState(Pawn patient)
        {
            return patient.InAggroMentalState && !patient.health.hediffSet.HasHediff(HediffDefOf.Scaria);
        }

        public static bool IsHeldBy(Pawn worker, Thing pack)
        {
            return pack != null && !pack.Destroyed && pack.stackCount > 0
                && (worker.inventory.innerContainer.Contains(pack) || worker.carryTracker.CarriedThing == pack);
        }

        // Finds the pack to use: the worker's own inventory first, then a pack already in hand, then the
        // nearest reachable, reservable, unforbidden pack on the map. Never touches other pawns' inventories.
        public static Thing FindPack(Pawn worker)
        {
            if (!IsAvailable || worker.Map == null)
            {
                return null;
            }
            Thing held = FindHeldPack(worker);
            if (held != null)
            {
                return held;
            }
            return GenClosest.ClosestThing_Global_Reachable(
                worker.Position,
                worker.Map,
                worker.Map.listerThings.ThingsOfDef(ThingDefOf.HemogenPack),
                PathEndMode.ClosestTouch,
                TraverseParms.For(worker, Danger.Deadly),
                9999f,
                t => t.Spawned && !t.Destroyed && t.stackCount > 0 && !t.IsForbidden(worker) && worker.CanReserve(t, 1, 1));
        }

        public static Thing FindHeldPack(Pawn worker)
        {
            ThingOwner<Thing> inventory = worker.inventory.innerContainer;
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].def == ThingDefOf.HemogenPack && inventory[i].stackCount > 0)
                {
                    return inventory[i];
                }
            }
            Thing carried = worker.carryTracker.CarriedThing;
            if (carried != null && carried.def == ThingDefOf.HemogenPack && carried.stackCount > 0)
            {
                return carried;
            }
            return null;
        }

        // The single application: reads blood loss again, lowers it by one pack's worth and consumes exactly one
        // unit. Returns false without side effects if anything has gone stale. Deliberately skips
        // Recipe_BloodTransfusion.ApplyOnPawn (needs a bill and also changes hemogen genes) and never touches
        // injuries, so the wounds keep bleeding.
        public static bool TryApplyPack(Pawn worker, Pawn patient, Thing pack)
        {
            if (!IsValidPatient(worker, patient) || !IsHeldBy(worker, pack) || pack.def != ThingDefOf.HemogenPack)
            {
                return false;
            }
            Hediff bloodLoss = GetBloodLoss(patient);
            if (bloodLoss == null)
            {
                return false;
            }
            bloodLoss.Severity -= Recipe_BloodTransfusion.BloodlossHealedPerPack;
            pack.SplitOff(1).Destroy();
            return true;
        }
    }
}
