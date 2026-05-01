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
            fire_tmp = __instance.GetAttachment(ThingDefOf.Fire) as Fire;
        }

        public static void Pawn_Kill_Postfix() {
            fire_tmp = null;
        }

        public static Thing MyGetAttachment(Thing t, ThingDef def) {
            if (def == ThingDefOf.Fire) {
                Fire fire = fire_tmp;
                fire_tmp = null;
                return fire;
            }
            return t.GetAttachment(def);
        }

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
