using System.Linq;
using Mono.Cecil;

namespace ItemIDPatcher.Patches;

internal class ShopInteractibleItem : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var field in typeDefinition.Fields.Where(field => field.Name == "<ItemID>k__BackingField"))
            field.FieldType = typeDefinition.Module.TypeSystem.Int32;
            
        foreach (var property in typeDefinition.Properties.Where(property => property.Name == "ItemID"))
            property.PropertyType = typeDefinition.Module.TypeSystem.Int32;
    }
}