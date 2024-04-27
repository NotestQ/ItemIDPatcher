using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using System.Reflection;
using BepInEx.Logging;
using JetBrains.Annotations;

namespace ItemIDPatcher;

internal abstract class IDPatch {
}
    
internal static class EntrypointPatcher
{
    // List of assemblies to patch
    [UsedImplicitly]
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };
    internal static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("ItemIDPatcher");
    private static Dictionary<string, Actionable> IDPatches { get; } = new ();

    private delegate void Actionable(TypeDefinition memberDefinition);
        
    [UsedImplicitly]
    public static void Initialize()
    {
        foreach (var definedType in Assembly.GetExecutingAssembly().DefinedTypes.Where(type => typeof(IDPatch).IsAssignableFrom(type) && type.FullName != typeof(IDPatch).FullName))
        {
                
            var PatchMethod = definedType.DeclaredMethods.First(method => method.Name == "Patch");
            IDPatches.Add(definedType.Name, (Actionable)PatchMethod.CreateDelegate(typeof(Actionable)));
        }
            
        Logger.LogDebug($"Registered {IDPatches.Count} patch{(IDPatches.Count == 1 ? "" : "es")}!");
    }
    [UsedImplicitly]
    public static void Patch(AssemblyDefinition assembly)
    {
        foreach (var type in assembly.MainModule.Types)
        {
            if (IDPatches.TryGetValue(type.Name, out var actionable))
                actionable(type);

            // TODO: Review ShopHandler, Pickup, PickupHandler, PlayerInventory, ItemInstanceData, ArtifactContentEvent, EmoteItem, PlayerEmotes, PlayerEmoteContentEvent, Player patches
        }

        if (!Directory.Exists(Paths.CachePath)) Directory.CreateDirectory(Paths.CachePath);

        var outputPath = Path.Combine(Paths.CachePath, $"ItemIDPatcher.{assembly.Name.Name}.dll");
        assembly.Write(outputPath);
    }
}