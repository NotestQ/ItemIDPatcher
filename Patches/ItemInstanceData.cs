using Mono.Cecil;

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
            }
        }
    }
}