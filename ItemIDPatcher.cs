using BepInEx.Preloader;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using BepInEx;
using Cecilifier.Runtime;
using System.Reflection;
using System.Net;
using System.Data;

namespace ItemIDPatcher
{
    class EntrypointPatcher
    {
        // List of assemblies to patch
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };
        // Patches the assemblies
        public static void Patch(AssemblyDefinition assembly)
        {
            // Patcher code here
            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                switch (type.Name)
                {
                    case "Item":
                        foreach (FieldDefinition field in type.Fields)
                        {
                            if (field.Name == "id")
                            {
                                //Console.WriteLine(field.FieldType);
                                //field.FieldType = assembly.MainModule.TypeSystem.Int32;
                                //Console.WriteLine(field.FieldType);
                            }
                        }
                        break;
                    case "ShopHandler":
                        var shopItemDefintion = assembly.MainModule.Types.Single(t => t.Name == "ShopItem");
                        var shopItemReference = assembly.MainModule.ImportReference(shopItemDefintion);
                        Type shopItem = Type.GetType(shopItemReference.FullName + ", " + shopItemReference.Module.Assembly.FullName); // Getting the ShopItem struct as a Type like this currently makes the game blue

                        foreach (FieldDefinition field in type.Fields)
                        {
                            if (field.Name == "m_ItemsForSaleDictionary")
                            {
                                field.FieldType = assembly.MainModule.ImportReference(typeof(System.Collections.Generic.Dictionary<,>)).MakeGenericInstanceType(assembly.MainModule.TypeSystem.Int32, shopItemDefintion);
                            }
                        }

                        foreach (PropertyDefinition property in type.Properties)
                        {
                            Console.WriteLine($"{property.Name} {property.FullName}");
                            if (property.Name == "NumberOfItemsInShop")
                            {
                                var getMethodIL = property.GetMethod.Body.GetILProcessor();
                                property.GetMethod.Body.Instructions.Clear();

                                var fieldItemsForSale = type.Fields.Single(f => f.Name == "m_ItemsForSaleDictionary");

                                var countMethod = typeof(Enumerable).GetMethods()
                                    .Where(m => m.Name == "Count")
                                    .FirstOrDefault(m => m.GetParameters().Length == 1);

                                var genericMethod = assembly.MainModule.ImportReference(countMethod.MakeGenericMethod(new Type[] { typeof(System.Collections.Generic.KeyValuePair<,>).MakeGenericType(new Type[] { typeof(System.Int32), Type.GetType(shopItemReference.FullName + ", " + shopItemReference.Module.Assembly.FullName) }) }));

                                getMethodIL.Emit(OpCodes.Ldarg_0);
                                getMethodIL.Emit(OpCodes.Ldfld, fieldItemsForSale);
                                getMethodIL.Emit(OpCodes.Callvirt, genericMethod);
                                getMethodIL.Emit(OpCodes.Ret);
                            }
                        }
                        
                        foreach (MethodDefinition method in type.Methods)
                        {
                            switch (method.Name)
                            {
                                case "InitShop":
                                    var body = method.Body;
                                    var methodILProcessor = body.GetILProcessor();
                                    
                                    foreach (Instruction instruction in body.Instructions.Where(t => t.OpCode == OpCodes.Newobj))
                                    {
                                        Console.WriteLine(instruction.Operand.ToString());
                                        if (instruction.Operand.ToString() == "System.Void System.Collections.Generic.Dictionary`2<System.Byte,ShopItem>::.ctor()") {
                                            Console.WriteLine("Operand found");
                                            var obj = assembly.MainModule.ImportReference(TypeHelpers.ResolveMethod(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(new Type[] { typeof(System.Int32), shopItem }), ".ctor", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public));
                                            // I used Cecilifier because I don't want to make the object myself :bluecookieemoji:
                                            methodILProcessor.Replace(instruction, 
                                                methodILProcessor.Create(OpCodes.Newobj, obj));
                                            break;
                                        }

                                    };
                                    break;
                            }
                        }
                        break;
                }
            }

            if (!Directory.Exists(Paths.CachePath)) Directory.CreateDirectory(Paths.CachePath);

            var outputPath = Path.Combine(Paths.CachePath, $"ItemIDPatcher." + assembly.Name.Name + ".dll");
            assembly.Write(outputPath);
        }
    }
}