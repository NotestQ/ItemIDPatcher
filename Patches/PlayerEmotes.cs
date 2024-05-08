using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ItemIDPatcher.Patches;

internal class PlayerEmotes : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var method in typeDefinition.Methods)
        {
            switch (method.Name)
            {
                
                case "RPC_PlayEmote":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "RPC_DoBookEquipEffect":
                    method.Parameters[1].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "DoBookEquipEffect":
                    method.Parameters[1].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    var bookEquipMethodBody = method.Body;

                    var instr = bookEquipMethodBody.Instructions.First(i => i.OpCode == OpCodes.Ldarg_2).Next;
                    instr.Operand = typeDefinition.Module.TypeSystem.Int32;

                    break;
                case "PlayEmote":
                    var playEmoteMethodBody = method.Body;

                    var playEmoteBoxInstr = playEmoteMethodBody.Instructions.First(i => i.OpCode == OpCodes.Box);
                    playEmoteBoxInstr.Operand = typeDefinition.Module.TypeSystem.Int32;

                    break;
            }

        }
    }
}