using RimWorld;
using Verse;
using Verse.AI;

namespace Emergency_Trasnfusion
{
    // Right-click order for a drafted pawn. Manual only: no work giver, think-tree entry or bill is involved.
    // Discovered automatically: every concrete FloatMenuOptionProvider subclass is instantiated at play-data load.
    public class FloatMenuOptionProvider_StabilizeWithHemogen : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;

        protected override bool Undrafted => false;

        protected override bool Multiselect => false;

        protected override bool RequiresManipulation => true;

        protected override bool MechanoidCanDo => false;

        protected override bool CanSelfTarget => true;

        protected override bool AppliesInt(FloatMenuContext context)
        {
            return StabilizationUtility.IsAvailable && context.FirstSelectedPawn.IsPlayerControlled;
        }

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            Pawn doctor = context.FirstSelectedPawn;
            if (!StabilizationUtility.IsValidPatient(doctor, clickedPawn))
            {
                return null;
            }
            if (StabilizationUtility.InAggroMentalState(clickedPawn))
            {
                return Disabled(clickedPawn, "PawnIsInAggroMentalState".Translate(clickedPawn).CapitalizeFirst());
            }
            if (doctor.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
            {
                return Disabled(clickedPawn, "CannotPrioritizeWorkTypeDisabled".Translate(WorkTypeDefOf.Doctor.gerundLabel));
            }
            if (clickedPawn != doctor && !doctor.CanReach(clickedPawn, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return Disabled(clickedPawn, "NoPath".Translate().CapitalizeFirst());
            }
            if (StabilizationUtility.FindPack(doctor) == null)
            {
                return Disabled(clickedPawn, "ET_NoHemogenPack".Translate().CapitalizeFirst());
            }
            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption("ET_StabilizeWithHemogen".Translate(clickedPawn), () => TakeOrder(doctor, clickedPawn)),
                doctor, clickedPawn);
        }

        private static FloatMenuOption Disabled(Pawn patient, TaggedString reason)
        {
            return new FloatMenuOption("ET_CannotStabilizeWithHemogen".Translate(patient) + ": " + reason, null);
        }

        // Everything is checked again here: the map and the patient's health may have changed while the menu was
        // open, and the pack found at menu time may have been used by someone else.
        private static void TakeOrder(Pawn doctor, Pawn patient)
        {
            if (!StabilizationUtility.IsValidPatient(doctor, patient))
            {
                return;
            }
            Thing pack = StabilizationUtility.FindPack(doctor);
            if (pack == null)
            {
                Messages.Message("ET_CannotStabilizeWithHemogen".Translate(patient) + ": " + "ET_NoHemogenPack".Translate().CapitalizeFirst(),
                    patient, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            Job job = JobMaker.MakeJob(StabilizeJobDefOf.ET_StabilizeWithHemogen, patient, pack);
            job.count = 1;
            doctor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}
