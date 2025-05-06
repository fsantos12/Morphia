using System.Reflection;
using System.Reflection.Emit;

public class DynamicTypeGenerator
{
    public static Type CreateDynamicType(IEnumerable<string> properties)
    {
        // Define the assembly, module, and the new type
        var assemblyName = new AssemblyName("DynamicAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        // Define a type with public visibility
        var typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public | TypeAttributes.Class);

        // Define properties based on the provided property names
        foreach (var propName in properties)
        {
            var fieldBuilder = typeBuilder.DefineField($"_{propName}", typeof(object), FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(propName, PropertyAttributes.HasDefault, typeof(object), null);

            // Create the get and set methods for the property
            var getMethodBuilder = typeBuilder.DefineMethod($"get_{propName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(object), Type.EmptyTypes);
            var getILGenerator = getMethodBuilder.GetILGenerator();
            getILGenerator.Emit(OpCodes.Ldarg_0);               // Load 'this' (the object)
            getILGenerator.Emit(OpCodes.Ldfld, fieldBuilder);   // Load the value of the field
            getILGenerator.Emit(OpCodes.Ret);                   // Return the value

            var setMethodBuilder = typeBuilder.DefineMethod($"set_{propName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, [typeof(object)]);
            var setILGenerator = setMethodBuilder.GetILGenerator();
            setILGenerator.Emit(OpCodes.Ldarg_0);               // Load 'this' (the object)
            setILGenerator.Emit(OpCodes.Ldarg_1);               // Load the new value to set
            setILGenerator.Emit(OpCodes.Stfld, fieldBuilder);   // Set the field with the value
            setILGenerator.Emit(OpCodes.Ret);                   // Return

            // Assign the get and set methods to the property
            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }

        // Create the type and return it
        return typeBuilder.CreateTypeInfo().AsType();
    }
}
