using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ItemIDPatcher.Patches;

internal class EmoteItem : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var method in typeDefinition.Methods.Where(m => m.Name == "Update"))
        {
            var updateMethodBody = method.Body;

            var updateCallVirtInstr = updateMethodBody.Instructions.First(i => i.OpCode == OpCodes.Callvirt 
            && i.Next.OpCode == OpCodes.Ldstr
            && i.Next.Next.OpCode == OpCodes.Call);

            if (updateCallVirtInstr.Operand is MethodReference callVirtMethodRef)
            {
                var writeInt = callVirtMethodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "WriteInt");
                updateCallVirtInstr.Operand = typeDefinition.Module.ImportReference(writeInt);
            }


        }
    }
}