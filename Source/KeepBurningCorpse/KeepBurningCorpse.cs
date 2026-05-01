using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KeepBurningCorpse
{
    public class KeepBurningCorpse : Mod
    {
        public KeepBurningCorpse(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("tixiv.KeepBurningCorpse");

            harmony.Patch(AccessTools.Method(typeof(Pawn), "Kill"),
                  prefix: new HarmonyMethod(typeof(KeepBurningCorpse), nameof(KeepBurningCorpse.Pawn_Kill_Prefix)),
                  transpiler: new HarmonyMethod(typeof(KeepBurningCorpse), nameof(KeepBurningCorpse.Pawn_Kill_Transpiler)),
                  postfix: new HarmonyMethod(typeof(KeepBurningCorpse), nameof(KeepBurningCorpse.Pawn_Kill_Postfix)));

            harmony.PatchAll();
        }

        public static Fire fire_tmp = null;

        public static void Pawn_Kill_Prefix(Pawn __instance /* , DamageInfo? dinfo, Hediff exactCulprit */)
        {
            // We get the 'Fire' object that might be attached to a pawn here to a temporary, before it is
            // removed by 'Pawn.MakeCorpse(assignedGrave, currentBed)', which is called later in 'Pawn::Kill()'.
            fire_tmp = __instance.GetAttachment(ThingDefOf.Fire) as Fire;
        }

        public static void Pawn_Kill_Postfix() {
            // Null the temporary so the resources for the fire can get freed in case it wasn't used in 'Pawn::Kill()'.
            fire_tmp = null;
        }

        // This function gets called instead of the original 'AttachmentUtility.GetAttachment()' in 'Pawn::Kill()'.
        // By returning the correct 'Fire' object here, which has actually been removed from thhe attachments at this
        // point in 'Pawn::Kill()', we restore the original bugged game functionality which will attach the fire to
        // the generated corpse.
        public static Thing MyGetAttachment(Thing t, ThingDef def) {
            if (def == ThingDefOf.Fire) {
                Fire fire = fire_tmp;
                fire_tmp = null;
                return fire;
            }
            return t.GetAttachment(def);
        }


        // Transpiler to patch the call to 'AttachmentUtility.GetAttachment()' in 'Pawn.Kill()' to go to our own function
        // 'KeepBurningCorpse.MyGetAttachment()' above instead.
        public static IEnumerable<CodeInstruction> Pawn_Kill_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int phase = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(AccessTools.Method(typeof(AttachmentUtility), nameof(AttachmentUtility.GetAttachment))))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeepBurningCorpse), nameof(KeepBurningCorpse.MyGetAttachment)));
                    phase++;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (phase == 0)
            {
                Log.Warning("Transpiler failed to patch Pawn.Kill() correctly.");
            }
            else if (phase != 1)
            {
                Log.Warning("Transpiler may not have patched Pawn.Kill() correctly.");
            }
        }
    }
}
