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
                        EntrypointPatcher.Logger.LogDebug("It work");
                        ldcInst.Next.Operand = typeDefinition.Module.ImportReference(writeInt);
                    }

                    ldcInst.Operand = int.MaxValue;


                    break;
            }
        }
    }
}