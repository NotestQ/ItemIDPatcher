using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ItemIDPatcher.Patches;

internal class ShopHandler : IDPatch {
    internal static void Patch(TypeDefinition typeDefinition)
    {
        var shopItemDefinition = typeDefinition.Module.Types.Single(t => t.Name == "ShopItem");
        var shopItemReference = typeDefinition.Module.ImportReference(shopItemDefinition);
            
        foreach (var field in typeDefinition.Fields.Where(field => field.Name == "m_ItemsForSaleDictionary"))
        {
            field.FieldType = typeDefinition.Module.ImportReference(typeof(Dictionary<,>))
                .MakeGenericInstanceType(typeDefinition.Module.TypeSystem.Int32, shopItemDefinition);
        }

        foreach (var property in typeDefinition.Properties)
        {
            EntrypointPatcher.Logger.LogDebug($"{property.Name} {property.FullName}");
            if (property.Name != "NumberOfItemsInShop") continue;
            
            var getMethodIL = property.GetMethod.Body.GetILProcessor();
            property.GetMethod.Body.Instructions.Clear();

            var fieldItemsForSale = typeDefinition.Fields.Single(f => f.Name == "m_ItemsForSaleDictionary");
                    
            var countMethodReference = typeDefinition.Module.ImportReference(
                typeof(Enumerable).GetMethods()
                    .Where(m => m.Name == "Count")
                    .FirstOrDefault(m => m.GetParameters().Length == 1));

            var countMethodGenericInstance = new GenericInstanceMethod(countMethodReference);
            countMethodGenericInstance.GenericArguments.Add(typeDefinition.Module.TypeSystem.Int32);
            countMethodGenericInstance.GenericArguments.Add(shopItemReference);

            getMethodIL.Emit(OpCodes.Ldarg_0);
            getMethodIL.Emit(OpCodes.Ldfld, fieldItemsForSale);
            getMethodIL.Emit(OpCodes.Callvirt, countMethodGenericInstance);
            getMethodIL.Emit(OpCodes.Ret);
        }

        foreach (var nestedType in typeDefinition.NestedTypes.Where(nestedType => nestedType.Name == "<>c"))
        {
            foreach (var field in nestedType.Fields)
            {
                EntrypointPatcher.Logger.LogDebug(field.Name);
                field.FieldType = field.Name switch {
                    "<>9__31_0" or "<>9__41_0" => typeDefinition.Module.ImportReference(typeof(Func<,>)).MakeGenericInstanceType(typeDefinition.Module.TypeSystem.Int32, shopItemDefinition),
                    _ => field.FieldType
                };
            }

            foreach (var method in nestedType.Methods)
            {
                method.ReturnType = method.Name switch {
                    "<RPCM_RequestShop>b__31_0" or "<BuyItem>b__41_0" => typeDefinition.Module.TypeSystem.Int32,
                    _ => method.ReturnType
                };
            }
        }

        foreach (var method in typeDefinition.Methods)
        {
            switch (method.Name)
            {
                case "InitShop":
                    var body = method.Body;
                    var methodILProcessor = body.GetILProcessor();
                    var newObjInstruction = body.Instructions.First(t => t.OpCode == OpCodes.Newobj && t.Operand.ToString() == "System.Void System.Collections.Generic.Dictionary`2<System.Byte,ShopItem>::.ctor()");

                    if (newObjInstruction == null) break;

                    var dictionaryTypeDefinition = typeDefinition.Module.ImportReference(typeof(Dictionary<,>));
                    var constructorMethodReference = typeDefinition.Module.ImportReference(
                        typeof(Dictionary<,>).GetConstructors()
                            .FirstOrDefault(m => m.GetParameters().Length == 0));
                    var constructorMethodDefinition = constructorMethodReference.Resolve();

                    constructorMethodReference = new MethodReference(constructorMethodDefinition.Name, constructorMethodDefinition.ReturnType) 
                    { 
                        HasThis = constructorMethodDefinition.HasThis, ExplicitThis = constructorMethodDefinition.ExplicitThis, 
                        DeclaringType = dictionaryTypeDefinition.MakeGenericInstanceType(typeDefinition.Module.TypeSystem.Int32, shopItemReference), 
                        CallingConvention = constructorMethodDefinition.CallingConvention
                    };

                    methodILProcessor.Replace(newObjInstruction, 
                        methodILProcessor.Create(OpCodes.Newobj, constructorMethodReference));


                    var callVirtInstruction = body.Instructions.FirstOrDefault(t => t.OpCode == OpCodes.Callvirt );
                        
                    if (callVirtInstruction == null) break;
                        
                    var dictionaryType = typeDefinition.Module.ImportReference(typeof(Dictionary<,>));
                    var dictionaryTypeDef = dictionaryType.Resolve().MakeGenericInstanceType(typeDefinition.Module.TypeSystem.Int32, shopItemReference);

                    if (callVirtInstruction.Operand is MethodReference methodRef)
                    {
                        EntrypointPatcher.Logger.LogDebug("Modifying the methodRef! (Notest dum dum)");
                        methodRef.DeclaringType = dictionaryTypeDef;
                        callVirtInstruction.Operand = typeDefinition.Module.ImportReference(methodRef);
                    }
                    Console.WriteLine(callVirtInstruction.Operand);
                    break;
                case "OnAddToCartItemClicked":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "RPCM_RequestShop":
                    var requestShopBody = method.Body;
                    var requestShopILProcessor = requestShopBody.GetILProcessor();
                        
                    var newArrInstr = requestShopBody.Instructions.First(i => i.OpCode == OpCodes.Newarr);

                    requestShopBody.Variables[0].VariableType = typeDefinition.Module.TypeSystem.Int32;
                    requestShopILProcessor.Replace(newArrInstr,
                        requestShopILProcessor.Create(OpCodes.Newarr, typeDefinition.Module.TypeSystem.Int32));

                    var requestShopSelectCallInstruction = requestShopBody.Instructions.First(i => i.OpCode == OpCodes.Stsfld
                                                                                                   && i.Next.OpCode == OpCodes.Call
                                                                                                   && i.Next.Next.OpCode == OpCodes.Call).Next;
                    var requestShopToArrayCallInstruction = requestShopSelectCallInstruction.Next;

                    {
                    var selectMethodReference = typeDefinition.Module.ImportReference(
                        typeof(Enumerable).GetMethods()
                            .Where(m => m.Name == "Select")
                            .FirstOrDefault(m => m.GetParameters().Length == 2));

                    var selectMethodGenericInstance = new GenericInstanceMethod(selectMethodReference);
                    selectMethodGenericInstance.GenericArguments.Add(shopItemReference);
                    selectMethodGenericInstance.GenericArguments.Add(typeDefinition.Module.TypeSystem.Int32);

                    requestShopILProcessor.Replace(requestShopSelectCallInstruction,
                        requestShopILProcessor.Create(OpCodes.Call, selectMethodGenericInstance));
                    }

                    if (requestShopToArrayCallInstruction != null)
                    {
                        var toArrayMethodReference = typeDefinition.Module.ImportReference(
                            typeof(Enumerable).GetMethods()
                                .FirstOrDefault(m => m.Name == "ToArray"));
                        var toArrayMethodGenericInstance = new GenericInstanceMethod(toArrayMethodReference);
                        toArrayMethodGenericInstance.GenericArguments.Add(typeDefinition.Module.TypeSystem.Int32);

                        requestShopILProcessor.Replace(requestShopToArrayCallInstruction,
                            requestShopILProcessor.Create(OpCodes.Call, toArrayMethodGenericInstance));
                    }
                    break;
                case "RPCO_UpdateShop":
                    method.Parameters[1].ParameterType = typeDefinition.Module.TypeSystem.Int32.MakeArrayType();
                    break;
                case "ResetCart":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32.MakeArrayType();

                    var resetCartBody = method.Body;
                    resetCartBody.Variables[0].VariableType = typeDefinition.Module.TypeSystem.Int32.MakeArrayType();
                    resetCartBody.Variables[2].VariableType = typeDefinition.Module.TypeSystem.Int32;
                        
                    break;
                case "RPCA_AddItemToCart":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;

                    /*var addToCartBody = method.Body;
                    var addToCartILProcessor = addToCartBody.GetILProcessor();

                    foreach (Instruction instr in addToCartBody.Instructions)
                    {
                        Console.WriteLine(instr.OpCode);
                        //addToCartILProcessor.Replace(instr,
                         //   addToCartILProcessor.Create(OpCodes.Box, typeDefinition.Module.TypeSystem.Int32));
                    }*/
                    break;
                case "RPCM_RequestShopAction":
                    method.Parameters[1].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "TryGetShopItem":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32;
                    break;
                case "BuyItem":
                    if (method.Parameters.Count == 1) // Overload where the only parameter is ShoppingCart cart
                    {
                        var buyItemBody = method.Body;
                        var buyItemILProcessor = buyItemBody.GetILProcessor();

                        buyItemBody.Variables[1].VariableType = typeDefinition.Module.TypeSystem.Int32.MakeArrayType();
                        //Patch line 38 and 39 call
                       
                        var selectCallInstruction = buyItemBody.Instructions.First(i =>
                            i.OpCode == OpCodes.Call
                            && i.Previous.OpCode == OpCodes.Stsfld
                            && i.Next.OpCode == OpCodes.Call);

                        var toArrayCallInstruction = selectCallInstruction.Next;

                        {
                            var selectMethodReference = typeDefinition.Module.ImportReference(
                                typeof(Enumerable).GetMethods()
                                    .Where(m => m.Name == "Select")
                                    .FirstOrDefault(m => m.GetParameters().Length == 2));

                            var selectMethodGenericInstance = new GenericInstanceMethod(selectMethodReference);
                            selectMethodGenericInstance.GenericArguments.Add(shopItemReference);
                            selectMethodGenericInstance.GenericArguments.Add(typeDefinition.Module.TypeSystem.Int32);

                            buyItemILProcessor.Replace(selectCallInstruction, 
                                buyItemILProcessor.Create(OpCodes.Call, selectMethodGenericInstance));
                        }

                        if (toArrayCallInstruction != null)
                        {
                            var toArrayMethodReference = typeDefinition.Module.ImportReference(
                                typeof(Enumerable).GetMethods()
                                    .FirstOrDefault(m => m.Name == "ToArray"));
                            var toArrayMethodGenericInstance = new GenericInstanceMethod(toArrayMethodReference);
                            toArrayMethodGenericInstance.GenericArguments.Add(typeDefinition.Module.TypeSystem.Int32);
                            
                            buyItemILProcessor.Replace(toArrayCallInstruction,
                                buyItemILProcessor.Create(OpCodes.Call, toArrayMethodGenericInstance));
                        }

                        // Change line 35 newobj

                        var funcTypeDefinition = typeDefinition.Module.ImportReference(typeof(Func<,>));
                        
                        var newObjFuncInstruction = buyItemBody.Instructions.First(t => t.OpCode == OpCodes.Newobj 
                            && t.Previous.OpCode == OpCodes.Ldftn
                            && t.Next.OpCode == OpCodes.Dup);

                        if (newObjFuncInstruction == null) break;

                        if (newObjFuncInstruction.Operand is MethodReference funcMethodRef)
                        {
                            funcMethodRef.DeclaringType = funcTypeDefinition.MakeGenericInstanceType(shopItemReference,
                                typeDefinition.Module.TypeSystem.Int32);
                        }

                        break;
                    }

                    method.Parameters[1].ParameterType = typeDefinition.Module.TypeSystem.Int32.MakeArrayType();
                    break;
                case "RPCA_SpawnDrone":
                    method.Parameters[0].ParameterType = typeDefinition.Module.TypeSystem.Int32.MakeArrayType();
                    break;
            }
        }
    }
}