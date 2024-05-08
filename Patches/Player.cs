using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ItemIDPatcher.Patches;

internal class Player : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var method in typeDefinition.Methods)
        {
            switch (method.Name)
            {
                case "RequestCreatePickup":
                    if (method.Parameters.Count == 6)
                    {
                        method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                        var requestCreatePickupMethodBody = method.Body;
                        requestCreatePickupMethodBody.Instructions.First(i => i.OpCode == OpCodes.Box).Operand = typeDefinition.Module.TypeSystem.Int32;
                        break;
                    } 
                    
                    var overloadRequestCreatePickupMethodBody = method.Body;
                    overloadRequestCreatePickupMethodBody.Instructions.First(i => i.OpCode == OpCodes.Box).Operand = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "RPC_RequestCreatePickup":
                case "RPC_RequestCreatePickupVel":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    break;
            }
        }
    }
}