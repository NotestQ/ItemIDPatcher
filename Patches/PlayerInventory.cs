using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ItemIDPatcher.Patches;

internal class PlayerInventory : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var method in typeDefinition.Methods)
        {
            switch (method.Name)
            {
                case "SyncAddToSlot":
                    var syncMethodBody = method.Body;

                    var instr = syncMethodBody.Instructions.First(i => i.OpCode == OpCodes.Box
                                                                       && i.Previous.OpCode == OpCodes.Ldfld
                                                                       && i.Previous.Previous.OpCode == OpCodes.Ldfld);

                    instr.Operand = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "RPC_AddToSlot":
                    method.Parameters[1].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "SerializeInventory":
                    var serializeMethodBody = method.Body;

                    var ldcInst = serializeMethodBody.Instructions.First(i => i.OpCode == OpCodes.Ldc_I4
                                                                              && i.Previous.OpCode == OpCodes.Br_S
                                                                              && i.Next.OpCode == OpCodes.Callvirt);

                    if (ldcInst.Next.Operand is MethodReference methodRef)
                    {
                        var writeInt = methodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "WriteInt");
                        ldcInst.Next.Operand = typeDefinition.Module.ImportReference(writeInt);
                    }

                    ldcInst.Operand = int.MaxValue;


                    break;
                case "UpdateInventory":
                    var updateMethodBody = method.Body;
                    foreach (var variable in updateMethodBody.Variables)
                    {
                        switch (variable.Index)
                        {
                            case 4:
                                variable.VariableType = typeDefinition.Module.TypeSystem.Int32;
                                break;
                        }
                    }
                        
                    var callVirtInstr = updateMethodBody.Instructions.First(i => i.OpCode == OpCodes.Callvirt
                                                                           && i.Previous.OpCode == OpCodes.Ldloc_0
                                                                           && i.Next.OpCode == OpCodes.Stloc_S);
                    var ldci4Instr = updateMethodBody.Instructions.First(i => i.OpCode == OpCodes.Ldc_I4
                                                                              && i.Previous.OpCode == OpCodes.Ldloc_S
                                                                              && i.Next.OpCode == OpCodes.Bge_S);

                    if (callVirtInstr.Operand is MethodReference callVirtMethodRef)
                    {
                        var readInt = callVirtMethodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "ReadInt");
                        callVirtInstr.Operand = typeDefinition.Module.ImportReference(readInt);
                    }

                    ldci4Instr.Operand = int.MaxValue;

                    break;
            }
        }
    }
}