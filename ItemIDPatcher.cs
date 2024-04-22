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
using System.Data;
using System.Reflection;
using System.Net.Http;

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
            // TODO: Switch loops that only want to modify one thing to something normal, like an if statement
            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                switch (type.Name)
                {
                    case "Item":
                        foreach (FieldDefinition field in type.Fields)
                        {
                            if (field.Name == "id")
                            {
                                field.FieldType = assembly.MainModule.TypeSystem.Int32;
                            }
                        }
                        break;
                    case "ShopHandler":
                        var shopItemDefintion = assembly.MainModule.Types.Single(t => t.Name == "ShopItem");
                        var shopItemReference = assembly.MainModule.ImportReference(shopItemDefintion);
                        
                        assembly.MainModule.GetType();
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
                                
                                var countMethodReference = assembly.MainModule.ImportReference(
                                    typeof(Enumerable).GetMethods()
                                    .Where(m => m.Name == "Count")
                                    .FirstOrDefault(m => m.GetParameters().Length == 1));

                                var countMethodGenericInstance = new GenericInstanceMethod(countMethodReference);
                                countMethodGenericInstance.GenericArguments.Add(assembly.MainModule.TypeSystem.Int32);
                                countMethodGenericInstance.GenericArguments.Add(shopItemReference);

                                getMethodIL.Emit(OpCodes.Ldarg_0);
                                getMethodIL.Emit(OpCodes.Ldfld, fieldItemsForSale);
                                getMethodIL.Emit(OpCodes.Callvirt, countMethodGenericInstance);
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
                                    var instruction = body.Instructions.First(t => t.OpCode == OpCodes.Newobj && t.Operand.ToString() == "System.Void System.Collections.Generic.Dictionary`2<System.Byte,ShopItem>::.ctor()");

                                    if (instruction == null) break;

                                    var dictionaryTypeDefinition = assembly.MainModule.ImportReference(typeof(Dictionary<,>));
                                    var constructorMethodReference = assembly.MainModule.ImportReference(
                                        typeof(Dictionary<,>).GetConstructors()
                                        .FirstOrDefault(m => m.GetParameters().Length == 0));
                                    var constructorMethodDefinition = constructorMethodReference.Resolve();

                                    constructorMethodReference = new MethodReference(constructorMethodDefinition.Name, constructorMethodDefinition.ReturnType) 
                                        { HasThis = constructorMethodDefinition.HasThis, ExplicitThis = constructorMethodDefinition.ExplicitThis, 
                                        DeclaringType = dictionaryTypeDefinition.MakeGenericInstanceType(assembly.MainModule.TypeSystem.Int32, shopItemReference), 
                                        CallingConvention = constructorMethodDefinition.CallingConvention};

                                    methodILProcessor.Replace(instruction, 
                                        methodILProcessor.Create(OpCodes.Newobj, constructorMethodReference));
                                    break;
                                case "OnAddToCartItemClicked":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                                case "OnChangeCategoryClicked":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                                case "RPCM_RequestShop":
                                    var requestShopBody = method.Body;
                                    var requestShopILProcessor = requestShopBody.GetILProcessor();
                                    
                                    var newArrInstr = requestShopBody.Instructions.First(i => i.OpCode == OpCodes.Newarr);

                                    requestShopBody.Variables[0].VariableType = assembly.MainModule.TypeSystem.Int32;
                                    requestShopILProcessor.Replace(newArrInstr,
                                        requestShopILProcessor.Create(OpCodes.Newarr, assembly.MainModule.TypeSystem.Int32));
                                    // TODO: Change LinQ Select and ToArray
                                    break;
                                case "RPCO_UpdateShop":
                                    method.Parameters[1].ParameterType = assembly.MainModule.TypeSystem.Int32.MakeArrayType();
                                    break;
                                case "ResetCart":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32.MakeArrayType();

                                    var resetCartBody = method.Body;
                                    var resetCartILProcessor = resetCartBody.GetILProcessor();

                                    resetCartBody.Variables[0].VariableType = assembly.MainModule.TypeSystem.Int32.MakeArrayType();
                                    resetCartBody.Variables[2].VariableType = assembly.MainModule.TypeSystem.Int32;

                                    // TODO: Replace call's arguments
                                    var addItemToCartInstanceMethod = new GenericInstanceMethod(assembly.MainModule.ImportReference(
                                        type.GetMethods()
                                         .First(m => m.Name == "RPCA_AddItemToCart")));
                                    addItemToCartInstanceMethod.GenericArguments.Add(assembly.MainModule.TypeSystem.Int32);

                                    var callInstr = resetCartBody.Instructions.First(i => i.OpCode == OpCodes.Call);
                                    resetCartILProcessor.Replace(callInstr, 
                                        resetCartILProcessor.Create(OpCodes.Call, addItemToCartInstanceMethod));
                                    break;
                            }
                        }
                        break;
                    case "ItemInstanceData":
                        foreach (MethodDefinition method in type.Methods)
                        {
                            switch (method.Name)
                            {
                                case "GetEntryIdentifier":
                                    method.ReturnType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                                case "GetEntryType":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32;
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