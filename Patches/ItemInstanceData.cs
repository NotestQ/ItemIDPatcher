using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Linq;
using BepInEx.Logging;

namespace ItemIDPatcher.Patches;

internal class ItemInstanceData : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var method in typeDefinition.Methods)
        {
            switch (method.Name)
            {
                case "GetEntryIdentifier":
                    method.ReturnType = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "GetEntryType":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "Serialize": // Patches Entry serialization
                    if (method.Parameters.Count < 2) break;

                    var serializeMethodBody = method.Body;

                    var serializeCallVirtInstr = serializeMethodBody.Instructions.First(i => i.OpCode == OpCodes.Callvirt
                    && i.Next.OpCode == OpCodes.Ldarg_0
                    && i.Next.Next.OpCode == OpCodes.Ldfld);

                    var secondSerializeCallVirtInstr = serializeMethodBody.Instructions.First(i => i.OpCode == OpCodes.Callvirt
                        && i.Previous.OpCode == OpCodes.Call
                        && i.Previous.Previous.OpCode == OpCodes.Callvirt);

                    if (serializeCallVirtInstr.Operand is MethodReference callVirtMethodRef)
                    {
                        serializeMethodBody.GetILProcessor().Remove(serializeCallVirtInstr.Previous);
                        var writeInt = callVirtMethodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "WriteInt");
                        serializeCallVirtInstr.Operand = typeDefinition.Module.ImportReference(writeInt);
                    }

                    if (secondSerializeCallVirtInstr.Operand is MethodReference secondCallVirtMethodRef)
                    {
                        var writeInt = secondCallVirtMethodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "WriteInt");
                        secondSerializeCallVirtInstr.Operand = typeDefinition.Module.ImportReference(writeInt);
                    }
                    break;
                case "Deserialize":
                    if (method.Parameters[0].ParameterType.ToString() == "System.Byte[]") break;

                    var deserializeMethodBody = method.Body;

                    var deserializeCallVirtInstr = deserializeMethodBody.Instructions.First(i => i.OpCode == OpCodes.Callvirt
                        && i.Next.OpCode == OpCodes.Stloc_0);

                    var secondDeserializeCallVirtInstr = deserializeMethodBody.Instructions.First(i => i.OpCode == OpCodes.Callvirt
                        && i.Next.OpCode == OpCodes.Call);

                    if (deserializeCallVirtInstr.Operand is MethodReference deserializeCallVirtMethodRef)
                    {
                        var readInt = deserializeCallVirtMethodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "ReadInt");
                        deserializeCallVirtInstr.Operand = typeDefinition.Module.ImportReference(readInt);
                    }

                    if (secondDeserializeCallVirtInstr.Operand is MethodReference secondDeserializeCallVirtMethodRef)
                    {
                        var readInt = secondDeserializeCallVirtMethodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "ReadInt");
                        secondDeserializeCallVirtInstr.Operand = typeDefinition.Module.ImportReference(readInt);
                    }

                    break;
            }
        }
    }
}