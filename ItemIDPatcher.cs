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
using BepInEx.Logging;

namespace ItemIDPatcher
{
    class EntrypointPatcher
    {
        // List of assemblies to patch
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };
        public static ManualLogSource Logger { get; } = BepInEx.Logging.Logger.CreateLogSource("NotestDum");
        // Patches the assemblies
        public static void Patch(AssemblyDefinition assembly)
        {
            // Patcher code here
            // TODO: Switch loops that only want to modify one thing to something normal, like an if statement
            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                switch (type.Name)
                {
                    case "ShopInteractibleItem": // [sic]

                        foreach (FieldDefinition field in type.Fields)
                        {
                            if (field.Name == "<ItemID>k__BackingField")
                            {
                                field.FieldType = assembly.MainModule.TypeSystem.Int32;
                            }
                        }
                        foreach (PropertyDefinition property in type.Properties)
                        {
                            if (property.Name == "ItemID")
                            {
                                property.PropertyType = assembly.MainModule.TypeSystem.Int32;
                            }
                        }
                        break;
                    case "ShopItem":
                        foreach (PropertyDefinition property in type.Properties)
                        {
                            if (property.Name == "ItemID")
                            {
                                property.PropertyType = assembly.MainModule.TypeSystem.Int32;
                                property.GetMethod.ReturnType = assembly.MainModule.TypeSystem.Int32;
                            }
                        }
                        break;
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
                            Logger.LogDebug($"{property.Name} {property.FullName}");
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

                        foreach (var nestedType in type.NestedTypes) // Commented out for the time being because I hope the issue goes away by itself (*It won't*)
                        {
                            if (nestedType.Name == "<>c")
                            {
                                foreach (FieldDefinition field in nestedType.Fields)
                                {
                                    Logger.LogDebug(field.Name);
                                    switch (field.Name) 
                                    {
                                        case "<>9__31_0":
                                        case "<>9__41_0":
                                            field.FieldType = assembly.MainModule.ImportReference(typeof(System.Func<,>))
                                                .MakeGenericInstanceType(assembly.MainModule.TypeSystem.Int32, shopItemDefintion);
                                            break;
                                    }
                                }

                                foreach (MethodDefinition method in nestedType.Methods)
                                {
                                    switch (method.Name)
                                    {
                                        case "<RPCM_RequestShop>b__31_0": 
                                        case "<BuyItem>b__41_0":
                                            method.ReturnType = assembly.MainModule.TypeSystem.Int32;
                                            break;
                                    }
                                }
                            }
                        }

                        foreach (MethodDefinition method in type.Methods)
                        {
                            switch (method.Name)
                            {
                                case "InitShop":
                                    var body = method.Body;
                                    var methodILProcessor = body.GetILProcessor();
                                    var newObjInstruction = body.Instructions.First(t => t.OpCode == OpCodes.Newobj && t.Operand.ToString() == "System.Void System.Collections.Generic.Dictionary`2<System.Byte,ShopItem>::.ctor()");

                                    if (newObjInstruction == null) break;

                                    var dictionaryTypeDefinition = assembly.MainModule.ImportReference(typeof(Dictionary<,>));
                                    var constructorMethodReference = assembly.MainModule.ImportReference(
                                        typeof(Dictionary<,>).GetConstructors()
                                        .FirstOrDefault(m => m.GetParameters().Length == 0));
                                    var constructorMethodDefinition = constructorMethodReference.Resolve();

                                    constructorMethodReference = new MethodReference(constructorMethodDefinition.Name, constructorMethodDefinition.ReturnType) 
                                    { 
                                        HasThis = constructorMethodDefinition.HasThis, ExplicitThis = constructorMethodDefinition.ExplicitThis, 
                                        DeclaringType = dictionaryTypeDefinition.MakeGenericInstanceType(assembly.MainModule.TypeSystem.Int32, shopItemReference), 
                                        CallingConvention = constructorMethodDefinition.CallingConvention
                                    };

                                    methodILProcessor.Replace(newObjInstruction, 
                                        methodILProcessor.Create(OpCodes.Newobj, constructorMethodReference));

                                    //callVirt patch

                                    var callVirtInstruction = body.Instructions.First(t => t.OpCode == OpCodes.Callvirt );
                                    Console.WriteLine(callVirtInstruction.Operand);
                                    
                                    if (callVirtInstruction == null) break;
                                    
                                    var dictionaryType = assembly.MainModule.ImportReference(typeof(Dictionary<,>));
                                    var dictionaryTypeDef = dictionaryType.Resolve().MakeGenericInstanceType(assembly.MainModule.TypeSystem.Int32, shopItemReference);

                                    if (callVirtInstruction.Operand is MethodReference methodRef)
                                    {
                                        Logger.LogDebug("Modifying the methodRef! (Notest dum dum)");
                                        methodRef.DeclaringType = dictionaryTypeDef;
                                        callVirtInstruction.Operand = assembly.MainModule.ImportReference(methodRef);
                                    }
                                    Console.WriteLine(callVirtInstruction.Operand);
                                    break;
                                case "OnAddToCartItemClicked":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                                case "RPCM_RequestShop":
                                    var requestShopBody = method.Body;
                                    var requestShopILProcessor = requestShopBody.GetILProcessor();
                                    
                                    var newArrInstr = requestShopBody.Instructions.First(i => i.OpCode == OpCodes.Newarr);

                                    requestShopBody.Variables[0].VariableType = assembly.MainModule.TypeSystem.Int32;
                                    requestShopILProcessor.Replace(newArrInstr,
                                        requestShopILProcessor.Create(OpCodes.Newarr, assembly.MainModule.TypeSystem.Int32));

                                    var requestShopSelectCallInstruction = requestShopBody.Instructions.FirstOrDefault(i => i.OpCode != null
                                    && i.OpCode == OpCodes.Stsfld
                                    && i.Next.OpCode == OpCodes.Call
                                    && i.Next.Next.OpCode == OpCodes.Call).Next;
                                    var requestShopToArrayCallInstruction = requestShopSelectCallInstruction.Next;

                                    if (requestShopSelectCallInstruction != null)
                                    {
                                        var selectMethodReference = assembly.MainModule.ImportReference(
                                            typeof(Enumerable).GetMethods()
                                            .Where(m => m.Name == "Select")
                                            .FirstOrDefault(m => m.GetParameters().Length == 2));

                                        var selectMethodGenericInstance = new GenericInstanceMethod(selectMethodReference);
                                        selectMethodGenericInstance.GenericArguments.Add(shopItemReference);
                                        selectMethodGenericInstance.GenericArguments.Add(assembly.MainModule.TypeSystem.Int32);

                                        requestShopILProcessor.Replace(requestShopSelectCallInstruction,
                                            requestShopILProcessor.Create(OpCodes.Call, selectMethodGenericInstance));
                                    }

                                    if (requestShopToArrayCallInstruction != null)
                                    {
                                        var toArrayMethodReference = assembly.MainModule.ImportReference(
                                            typeof(Enumerable).GetMethods()
                                            .FirstOrDefault(m => m.Name == "ToArray"));
                                        var toArrayMethodGenericInstance = new GenericInstanceMethod(toArrayMethodReference);
                                        toArrayMethodGenericInstance.GenericArguments.Add(assembly.MainModule.TypeSystem.Int32);

                                        requestShopILProcessor.Replace(requestShopToArrayCallInstruction,
                                                requestShopILProcessor.Create(OpCodes.Call, toArrayMethodGenericInstance));
                                    }
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
                                    
                                    break;
                                case "RPCA_AddItemToCart":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32;

                                    /*var addToCartBody = method.Body;
                                    var addToCartILProcessor = addToCartBody.GetILProcessor();

                                    foreach (Instruction instr in addToCartBody.Instructions)
                                    {
                                        Console.WriteLine(instr.OpCode);
                                        //addToCartILProcessor.Replace(instr,
                                         //   addToCartILProcessor.Create(OpCodes.Box, assembly.MainModule.TypeSystem.Int32));
                                    }*/
                                    break;
                                case "RPCM_RequestShopAction":
                                    method.Parameters[1].ParameterType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                                case "TryGetShopItem":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                                case "BuyItem":
                                    if (method.Parameters.Count == 1) // Overload where the only parameter is ShoppingCart cart
                                    {
                                        var buyItemBody = method.Body;
                                        var buyItemILProcessor = buyItemBody.GetILProcessor();

                                        buyItemBody.Variables[1].VariableType = assembly.MainModule.TypeSystem.Int32.MakeArrayType(); // stloc 1
                                        //Patch line 38 and 39 call
                                        /*string selectOperand = "System.Collections.Generic.IEnumerable`1 < !!1 > System.Linq.Enumerable::Select<ShopItem, System.Byte>(System.Collections.Generic.IEnumerable`1 < !!0 >, System.Func`2 < !!0, !!1 >)";
                                        string toArrayOperand = "!!0[] System.Linq.Enumerable::ToArray<System.Byte>(System.Collections.Generic.IEnumerable`1 < !!0 >)";*/
                                        var selectCallInstruction = buyItemBody.Instructions.FirstOrDefault(i => i.OpCode != null
                                        //&& i.Operand != null 
                                        && i.OpCode == OpCodes.Call
                                        //&& i.Operand.ToString() == selectOperand);
                                        && i.Previous.OpCode == OpCodes.Stsfld
                                        && i.Next.OpCode == OpCodes.Call);
                                        /*var toArrayCallInstruction = buyItemBody.Instructions.FirstOrDefault(i => i.OpCode != null 
                                        && i.Operand != null 
                                        && i.OpCode == OpCodes.Call 
                                        && i.Operand.ToString() == toArrayOperand);*/
                                        var toArrayCallInstruction = selectCallInstruction.Next;

                                        if (selectCallInstruction != null)
                                        {
                                            var selectMethodReference = assembly.MainModule.ImportReference(
                                                typeof(Enumerable).GetMethods()
                                                .Where(m => m.Name == "Select")
                                                .FirstOrDefault(m => m.GetParameters().Length == 2));

                                            var selectMethodGenericInstance = new GenericInstanceMethod(selectMethodReference);
                                            selectMethodGenericInstance.GenericArguments.Add(shopItemReference);
                                            selectMethodGenericInstance.GenericArguments.Add(assembly.MainModule.TypeSystem.Int32);

                                            buyItemILProcessor.Replace(selectCallInstruction, 
                                                buyItemILProcessor.Create(OpCodes.Call, selectMethodGenericInstance));
                                        }

                                        if (toArrayCallInstruction != null)
                                        {
                                            var toArrayMethodReference = assembly.MainModule.ImportReference(
                                                typeof(Enumerable).GetMethods()
                                                .FirstOrDefault(m => m.Name == "ToArray"));
                                            var toArrayMethodGenericInstance = new GenericInstanceMethod(toArrayMethodReference);
                                            toArrayMethodGenericInstance.GenericArguments.Add(assembly.MainModule.TypeSystem.Int32);
                                            //var newToArrayCall = assembly.MainModule.ImportReference(TypeHelpers.ResolveGenericMethodInstance(typeof(System.Linq.Enumerable).AssemblyQualifiedName, "ToArray", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public, new ParamData[] { }, new[] { typeof(System.Int32).AssemblyQualifiedName }));
                                            buyItemILProcessor.Replace(toArrayCallInstruction,
                                                    buyItemILProcessor.Create(OpCodes.Call, toArrayMethodGenericInstance));
                                        }
                                        // TODO?: Change line 35 newobj
                                        break;
                                    }

                                    method.Parameters[1].ParameterType = assembly.MainModule.TypeSystem.Int32.MakeArrayType();
                                    break;
                                case "RPCA_SpawnDrone":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32.MakeArrayType();
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
                    case "ItemDatabase":
                        foreach (MethodDefinition method in type.Methods)
                        {
                            switch (method.Name)
                            {
                                case "TryGetItemFromID":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                            }
                        }
                        break;
                    case "Pickup":
                        foreach (FieldDefinition field in type.Fields)
                        {
                            if (field.Name == "m_itemID")
                            {
                                field.FieldType = assembly.MainModule.TypeSystem.Int32;
                            }
                        }

                        foreach (MethodDefinition method in type.Methods)
                        {
                            switch (method.Name)
                            {
                                case "RPC_ConfigurePickup":
                                case "ConfigurePickup":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                            }
                        }
                        break;
                    case "PickupHandler":
                        foreach (MethodDefinition method in type.Methods)
                        {
                            switch (method.Name)
                            {
                                case "CreatePickup":
                                    method.Parameters[0].ParameterType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                            }
                        }
                        break;
                    case "PlayerInventory":
                        foreach (MethodDefinition method in type.Methods)
                        {
                            switch (method.Name)
                            {
                                case "RPC_AddToSlot":
                                    method.Parameters[1].ParameterType = assembly.MainModule.TypeSystem.Int32;
                                    break;
                                case "SerializeInventory":
                                    var serializeMethodBody = method.Body;
                                    var serializeMethodIL = serializeMethodBody.GetILProcessor();

                                    var ldcInst = serializeMethodBody.Instructions.First(i => i.OpCode == OpCodes.Ldc_I4
                                    && i.Previous.OpCode == OpCodes.Br_S
                                    && i.Next.OpCode == OpCodes.Callvirt);

                                    if (ldcInst.Next.Operand is MethodReference methodRef)
                                    {
                                        var writeInt = methodRef.DeclaringType.Resolve().Methods.First(m => m.Name == "WriteInt");
                                        Logger.LogDebug("It work");
                                        ldcInst.Next.Operand = assembly.MainModule.ImportReference(writeInt);
                                    }


                                    break;
                            }
                        }
                        break;
                        // TODO: Review Pickup, PickupHandler patches
                        // TODO: PlayerInventory, ItemInstanceData entry serializer, Every serializer and deserializer that uses IDs *shudders* (only if we want to touch base game item IDs), Player RPC_RequestCreatePickupVel | RequestCreatePickup, PlayerEmoteContentEvent?, PlayerEmotes?,
                }
            }

            if (!Directory.Exists(Paths.CachePath)) Directory.CreateDirectory(Paths.CachePath);

            var outputPath = Path.Combine(Paths.CachePath, $"ItemIDPatcher." + assembly.Name.Name + ".dll");
            assembly.Write(outputPath);
        }
    }
}
