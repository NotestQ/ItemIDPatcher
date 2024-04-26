using System.Linq;
using Mono.Cecil;

namespace ItemIDPatcher.Patches;

internal class ShopItem : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        foreach (var property in typeDefinition.Properties.Where(property => property.Name == "ItemID"))
        {
            property.PropertyType = typeDefinition.Module.TypeSystem.Int32;
            property.GetMethod.ReturnType = typeDefinition.Module.TypeSystem.Int32;
        }
    }
}