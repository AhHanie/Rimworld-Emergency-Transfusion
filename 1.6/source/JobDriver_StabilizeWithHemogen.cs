using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Emergency_Trasnfusion
{
    // Job ET_StabilizeWithHemogen. Targets: A = patient, B = hemogen pack. job.count = 1.
    public class JobDriver_StabilizeWithHemogen : JobDriver
    {
        private const TargetIndex PatientIndex = TargetIndex.A;
        private const TargetIndex PackIndex = TargetIndex.B;

        private Pawn Patient => job.GetTarget(PatientIndex).Pawn;

        private Thing Pack => job.GetTarget(PackIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            Thing pack = Pack;
            // A pack the worker already holds needs no map reservation.
            if (pack != null && pack.Spawned && !pawn.Reserve(pack, job, 1, 1, null, errorOnFailed))
            {
                return false;
            }
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(PatientIndex);
            this.FailOn(() => !StabilizationUtility.IsValidPatient(pawn, Patient));
            this.FailOnAggroMentalState(PatientIndex);

            bool selfTarget = Patient == pawn;
            Toil wait = selfTarget
                ? Toils_General.Wait(StabilizationUtility.ApplyDurationTicks)
                : Toils_General.WaitWith(PatientIndex, StabilizationUtility.ApplyDurationTicks, useProgressBar: false,
                    maintainPosture: true, maintainSleep: false, PatientIndex, PathEndMode.ClosestTouch);
            Toil gotoPatient = selfTarget ? wait : Toils_Goto.GotoThing(PatientIndex, PathEndMode.ClosestTouch);

            // Collect the pack first unless the worker already holds it.
            yield return Toils_Jump.JumpIf(gotoPatient, () => StabilizationUtility.IsHeldBy(pawn, Pack));
            yield return Toils_Goto.GotoThing(PackIndex, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(PackIndex);
            yield return TakePackIntoInventory();

            if (!selfTarget)
            {
                yield return gotoPatient;
                wait.AddFinishAction(delegate
                {
                    Pawn patient = Patient;
                    if (patient != null && patient != pawn && patient.CurJob != null
                        && (patient.CurJob.def == JobDefOf.Wait || patient.CurJob.def == JobDefOf.Wait_MaintainPosture))
                    {
                        patient.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                });
                wait.tickIntervalAction = delegate
                {
                    pawn.rotationTracker.FaceTarget(Patient);
                };
                wait.handlingFacing = true;
            }
            wait.WithProgressBarToilDelay(PatientIndex).PlaySustainerOrSound(SoundDefOf.Interact_Tend);
            wait.FailOn(() => !StabilizationUtility.IsHeldBy(pawn, Pack));
            wait.FailOn(() => pawn != Patient && !pawn.CanReachImmediate(Patient.SpawnedParentOrMe, PathEndMode.ClosestTouch));
            yield return wait;

            yield return ApplyPack();
        }

        // Moves exactly one pack from the ground stack into the worker's inventory and points B at the result.
        private Toil TakePackIntoInventory()
        {
            Toil toil = ToilMaker.MakeToil("TakePackIntoInventory");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Thing pack = actor.CurJob.GetTarget(PackIndex).Thing;
                if (pack == null || pack.Destroyed || !pack.Spawned || pack.stackCount < 1
                    || MassUtility.WillBeOverEncumberedAfterPickingUp(actor, pack, 1))
                {
                    actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                    return;
                }
                actor.Map.reservationManager.Release(pack, actor, actor.CurJob);
                Thing unit = pack.SplitOff(1);
                if (!actor.inventory.innerContainer.TryAdd(unit))
                {
                    if (!unit.Destroyed && unit.stackCount > 0)
                    {
                        GenPlace.TryPlaceThing(unit, actor.Position, actor.Map, ThingPlaceMode.Near);
                    }
                    actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                    return;
                }
                // The unit may have merged into an existing inventory stack, so look the pack up again.
                Thing held = StabilizationUtility.FindHeldPack(actor);
                if (held == null)
                {
                    actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                    return;
                }
                actor.CurJob.SetTarget(PackIndex, held);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        // Final instant toil: revalidates, lowers BloodLoss and consumes one pack. Ending without further
        // toils completes the job; there is no loop to a second pack.
        private Toil ApplyPack()
        {
            Toil toil = ToilMaker.MakeToil("ApplyHemogenPack");
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                if (!StabilizationUtility.TryApplyPack(actor, Patient, Pack))
                {
                    actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }
    }
}
