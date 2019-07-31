using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Dominium.Store.Dapper
{
	internal class ClassBuilder
	{
		private readonly AssemblyName _assemblyName;

		public ClassBuilder(string assemblyName)
			=> _assemblyName = new AssemblyName(assemblyName);

		public object CreateObject(string[] propertyNames, object[] values)
		{
			if (propertyNames.Length != values.Length)
				throw new Exception("The number of property names should match their corresponding types number");

			var dynamicClass = CreateClass();
			CreateConstructor(dynamicClass);
			for (var ind = 0; ind < propertyNames.Length; ind++)
				CreateProperty(dynamicClass, propertyNames[ind], values[ind].GetType());
			var type = dynamicClass.CreateTypeInfo();

			var instance = Activator.CreateInstance(type);

			for (var ind = 0; ind < propertyNames.Length; ind++)
				type.GetProperty(propertyNames[ind])?.SetValue(instance, values[ind]);

			return instance;
		}

		private TypeBuilder CreateClass()
		{
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run);
			var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
			var typeBuilder = moduleBuilder.DefineType(this._assemblyName.FullName, 
				TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass |
				TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null);
			return typeBuilder;
		}

		private void CreateConstructor(TypeBuilder typeBuilder)
			=> typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

		private void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
		{
			var fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

			var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
			var getPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName, 
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
			var getIl = getPropMthdBldr.GetILGenerator();

			getIl.Emit(OpCodes.Ldarg_0);
			getIl.Emit(OpCodes.Ldfld, fieldBuilder);
			getIl.Emit(OpCodes.Ret);

			var setPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
				null, new[] {propertyType});

			var setIl = setPropMthdBldr.GetILGenerator();
			var modifyProperty = setIl.DefineLabel();
			var exitSet = setIl.DefineLabel();

			setIl.MarkLabel(modifyProperty);
			setIl.Emit(OpCodes.Ldarg_0);
			setIl.Emit(OpCodes.Ldarg_1);
			setIl.Emit(OpCodes.Stfld, fieldBuilder);

			setIl.Emit(OpCodes.Nop);
			setIl.MarkLabel(exitSet);
			setIl.Emit(OpCodes.Ret);

			propertyBuilder.SetGetMethod(getPropMthdBldr);
			propertyBuilder.SetSetMethod(setPropMthdBldr);
		}
	}
}

// Modified Version of freely available code here:
// https://www.c-sharpcorner.com/UploadFile/87b416/dynamically-create-a-class-at-runtime/