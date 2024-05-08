using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ItemIDPatcher.Patches;

internal class PlayerEmoteContentEvent : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var field in typeDefinition.Fields.Where(field => field.Name == "emoteItemID"))
        {
            field.FieldType = typeDefinition.Module.TypeSystem.Int32;
        }

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
                    break;
                case "Deserialize":
                    var deserializeMethodBody = method.Body;
                    var deserializeCallVirtInstr = deserializeMethodBody.Instructions.First(i => i.OpCode == OpCodes.Callvirt
                        && i.Next.OpCode == OpCodes.Stfld);

                    if (deserializeCallVirtInstr.Operand is MethodReference deserializeCallVirtMethodRef)
                    {
                        var readInt = deserializeCallVirtMethodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "ReadInt");
                        deserializeCallVirtInstr.Operand = typeDefinition.Module.ImportReference(readInt);
                    }
                    break;
            }
        }
        //var constructor = typeDefinition.GetConstructors().First(c => c.Parameters.Count > 0)
        //var constructorBody = constructor.Body;
        
    }
}