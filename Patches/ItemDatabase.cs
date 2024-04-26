using Mono.Cecil;

namespace ItemIDPatcher.Patches;

internal class ItemDatabase : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var method in typeDefinition.Methods)
        {
            if (method is { Name: "TryGetItemFromID" })
                method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
        }
    }
}