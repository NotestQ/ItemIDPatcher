using System.Linq;
using Mono.Cecil;

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
            if (method is { Name: "RPC_ConfigurePickup" or "ConfigurePickup" })
                method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
        }
    }
}