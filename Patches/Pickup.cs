using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ItemIDPatcher.Patches;

internal class Pickup : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var field in typeDefinition.Fields.Where(field => field.Name == "m_itemID"))
        {
            field.FieldType = typeDefinition.Module.TypeSystem.Int32;
        }

        foreach (var method in typeDefinition.Methods)
        {
            switch (method.Name)
            {
                case "RPC_ConfigurePickup":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "ConfigurePickup":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    var bookEquipMethodBody = method.Body;

                    var instr = bookEquipMethodBody.Instructions.First(i => i.OpCode == OpCodes.Box
                    && i.Previous.OpCode == OpCodes.Ldarg_1);

                    instr.Operand = typeDefinition.Module.TypeSystem.Int32;
                    break;
            }
        }
    }
}