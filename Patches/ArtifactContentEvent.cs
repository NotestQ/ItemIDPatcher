using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ItemIDPatcher.Patches;

internal class ArtifactContentEvent : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var method in typeDefinition.Methods)
        {
            switch (method.Name)
            {
                case "Serialize":
                    var serializeMethodBody = method.Body;

                    var serializeCallVirtInstr = serializeMethodBody.Instructions.Last().Previous;

                    if (serializeCallVirtInstr.Operand is MethodReference callVirtMethodRef)
                    {
                        var writeInt = callVirtMethodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "WriteInt");
                        serializeCallVirtInstr.Operand = typeDefinition.Module.ImportReference(writeInt);
                    }

                    //serializeCallVirtInstr.Operand = int.MaxValue;
                    break;
                case "Deserialize":
                    var deserializeMethodBody = method.Body;

                    var deserializeCallVirtInstr = deserializeMethodBody.Instructions.Last(i => i.OpCode == OpCodes.Callvirt);

                    if (deserializeCallVirtInstr.Operand is MethodReference deserializeCallVirtMethodRef)
                    {
                        var readInt = deserializeCallVirtMethodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "ReadInt");
                        deserializeCallVirtInstr.Operand = typeDefinition.Module.ImportReference(readInt);
                    }

                    //deserializeCallVirtInstr.Operand = int.MaxValue;
                    break;
            }
        }
    }
}