using System;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace TerrariansConstructLib.API.Reflection {
	public static partial class ReflectionHelper<T> {
		private static Func<T, T> clone;

		public static T InvokeCloneMethod(T arg) {
			CreateCloneMethod();

			return clone(arg);
		}

		public static void CreateCloneMethod() {
			if (clone is not null)
				return; //Already defined

			Type type = typeof(T);

			if (type.IsAbstract || type.IsByRefLike || type.IsGenericTypeDefinition || type.IsNested)
				throw new InvalidOperationException($"Operation was not valid for type \"{type.FullName}\"");

			DynamicMethod method = new("shallowClone", type, new[] {type}, type.Module, true);
			ILGenerator il = method.GetILGenerator();

			// if(arg is null)
			//     return null;
			Label valueIsNullCheck = il.DefineLabel();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Brtrue, valueIsNullCheck);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ret);
			il.MarkLabel(valueIsNullCheck);

			// return (T)(arg as object).MemberwiseClone();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Box, type);
			il.EmitCall(OpCodes.Call, typeof(object).GetCachedMethod("MemberwiseClone")!, null);
			if (type.IsValueType)
				il.Emit(OpCodes.Unbox_Any, type);
			else
				il.Emit(OpCodes.Castclass, type);
			il.Emit(OpCodes.Ret);

			clone = (Func<T, T>)method.CreateDelegate(typeof(Func<T, T>));

			CoreLibMod.UnloadReflection += () => clone = null;
		}
	}
}
