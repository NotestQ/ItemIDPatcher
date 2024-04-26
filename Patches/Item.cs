using System.Linq;
using Mono.Cecil;

namespace ItemIDPatcher.Patches;

internal class Item : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var field in typeDefinition.Fields.Where(field => field.Name == "id"))
        {
            field.FieldType = typeDefinition.Module.TypeSystem.Int32;
        }
    }
}