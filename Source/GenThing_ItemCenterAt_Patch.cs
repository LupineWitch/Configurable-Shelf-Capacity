using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.Code;

namespace LupineWitch.ConfigurableShelfCapacity
{
    [HarmonyPatch(typeof(GenThing))]
    [HarmonyPatch("ItemCenterAt")]
    internal static class GenThing_ItemCenterAt_Patch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase original)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, il);
            var methodCallToBePatchedIn = AccessTools.Method(typeof(GenThing_ItemCenterAt_Patch),nameof(GenThing_ItemCenterAt_Patch.ShouldItemsMakeTowerBasedOnCellContents));
            var thingList = instructions.FirstOrDefault(ci => ci.opcode == OpCodes.Stloc_S && ci.operand is LocalBuilder builder && builder.LocalType == typeof(List<Thing>)).operand;
            var flag2 = instructions.FirstOrDefault(ci => ci.opcode == OpCodes.Stloc_S && ci.operand is LocalBuilder builder && builder.LocalType == typeof(bool)).operand;
            
            CodeInstruction getThingsListOnEvaluationStack = new CodeInstruction(OpCodes.Ldloc_S, thingList);
            CodeInstruction callShouldTowerMethod = new CodeInstruction(OpCodes.Callvirt, methodCallToBePatchedIn);
            CodeInstruction popMethodResultToFlag2 = new CodeInstruction(OpCodes.Stloc_S, flag2);

            //Insert new instructions:
            codeMatcher.MatchStartForward(
                //Match section in IL Code 
                new CodeMatch(OpCodes.Ldloc_2),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Stloc_2))
                .Advance(5) //Escape if brackets TODO: Try to improve code match to use single advance
                .Insert(getThingsListOnEvaluationStack, callShouldTowerMethod,popMethodResultToFlag2);

            return codeMatcher.InstructionEnumeration();
        }

        internal static bool ShouldItemsMakeTowerBasedOnCellContents(List<Thing> thingList)
        {
            int itemsCount = thingList.Where(thing => thing.def.category == ThingCategory.Item).Count();
            return ConfigurableShelfCapacitySettings.SplitVisualStackCount >= itemsCount;
        }
    }
}
