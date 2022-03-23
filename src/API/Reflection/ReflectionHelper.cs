using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace TerrariansConstructLib.API.Reflection {
	//Copied from my game
	// -- absoluteAquarian
	/// <summary>
	/// Reflection helper for accessing <typeparamref name="T"/> fields with an unspecified type
	/// </summary>
	/// <typeparam name="T">The declaring type</typeparam>
	public static partial class ReflectionHelper<T> {
		private static readonly Dictionary<string, Func<T, object>> getterFuncs = new();
		private static readonly Dictionary<string, Func<object>> getterStaticFuncs = new();
		private static readonly Dictionary<string, Action<T, object>> setterFuncs = new();
		private static readonly Dictionary<string, Action<object>> setterStaticFuncs = new();

		/// <summary>
		/// Creates the getter delegate for the field, <paramref name="field"/>, in <typeparamref name="T"/> if necessary, then invokes it
		/// </summary>
		/// <param name="field">The name of the field</param>
		/// <param name="instance">The instance.  If the field is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <returns>The boxed value of the field</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="InvalidOperationException"/>
		public static object InvokeGetterFunction(string field, T instance) {
			string name = "get_" + field;

			Func<object>? staticFunc = null;
			if (!getterFuncs.TryGetValue(name, out Func<T, object>? func) && !getterStaticFuncs.TryGetValue(name, out staticFunc)) {
				CreateGetAccessor(field, out _);

				_ = getterFuncs.TryGetValue(name, out func) || getterStaticFuncs.TryGetValue(name, out staticFunc);
			}

			if (func is not null) {
				if (instance is null)
					throw new Exception("Cannot invoke instance methods from a null value");

				return func(instance);
			}

			if (staticFunc is not null) {
				if (instance is not null)
					throw new Exception("Cannot invoke static methods from an instance");

				return staticFunc();
			}

			throw new InvalidOperationException("Unable to create getter function");
		}

		/// <summary>
		/// Creates the setter delegate for the field, <paramref name="field"/>, in <typeparamref name="T"/> if necessary, then invokes it
		/// </summary>
		/// <param name="field">The name of the field</param>
		/// <param name="instance">The instance.  If the field is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <param name="obj">The boxed value to set the field to</param>
		/// <exception cref="Exception"/>
		public static void InvokeSetterFunction(string field, T instance, object obj) {
			string name = "set_" + field;

			Action<object>? staticFunc = null;
			if (!setterFuncs.TryGetValue(name, out Action<T, object>? func) && !setterStaticFuncs.TryGetValue(name, out staticFunc)) {
				CreateSetAccessor(field, out _);

				_ = setterFuncs.TryGetValue(name, out func) || setterStaticFuncs.TryGetValue(name, out staticFunc);
			}

			if (func is not null) {
				if (instance is null)
					throw new Exception("Cannot invoke instance methods from a null value");

				func(instance, obj);
			} else if (staticFunc is not null) {
				if (instance is not null)
					throw new Exception("Cannot invoke static methods from an instance");

				staticFunc(obj);
			} else
				throw new Exception("Unable to create setter function");
		}

		/// <summary>
		/// Creates the getter delegate for the field, <paramref name="field"/>, in <typeparamref name="T"/>
		/// </summary>
		/// <param name="field">The name of the field</param>
		/// <param name="name">The key used to access the getter delegate</param>
		/// <exception cref="Exception"/>
		public static void CreateGetAccessor(string field, out string name) {
			name = "get_" + field;
			if (getterFuncs.ContainsKey(name) || getterStaticFuncs.ContainsKey(name))
				return; //Already defined

			FieldInfo? fieldInfo = typeof(T).GetField(field, ReflectionHelper.AllFlags);
			if (fieldInfo is null)
				throw new Exception($"Field \"{typeof(T).FullName}::{field}\" could not be found");

			DynamicMethod method;
			if (!fieldInfo.IsStatic)
				method = new DynamicMethod(name, typeof(object), new[] {typeof(T)}, typeof(T).Module, true);
			else
				method = new DynamicMethod(name, typeof(object), null, typeof(T).Module, true);

			ILGenerator il = method.GetILGenerator();

			if (!fieldInfo.IsStatic) {
				//arg0.field
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, fieldInfo);
			} else {
				//type.field
				il.Emit(OpCodes.Ldsfld, fieldInfo);
			}

			if (fieldInfo.FieldType.IsValueType)
				il.Emit(OpCodes.Box, fieldInfo.FieldType);

			il.Emit(OpCodes.Ret);

			if (!fieldInfo.IsStatic)
				getterFuncs[name] = method.CreateDelegate<Func<T, object>>();
			else
				getterStaticFuncs[name] = method.CreateDelegate<Func<object>>();

			if (getterFuncs.Count + getterStaticFuncs.Count == 1) {
				CoreLibMod.UnloadReflection += () => {
					getterFuncs.Clear();
					getterStaticFuncs.Clear();
				};
			}
		}

		/// <summary>
		/// Creates the setter delegate for the field, <paramref name="field"/>, in <typeparamref name="T"/>
		/// </summary>
		/// <param name="field">The name of the field</param>
		/// <param name="name">The key used to access the setter delegate</param>
		/// <exception cref="Exception"/>
		public static void CreateSetAccessor(string field, out string name) {
			name = "set_" + field;
			if (setterFuncs.ContainsKey(name) || setterStaticFuncs.ContainsKey(name))
				return; //Already defined

			FieldInfo? fieldInfo = typeof(T).GetField(field, ReflectionHelper.AllFlags);
			if (fieldInfo is null)
				throw new Exception($"Field \"{typeof(T).FullName}::{field}\" could not be found");

			DynamicMethod method;
			if (!fieldInfo.IsStatic)
				method = new DynamicMethod(name, null, new[] {typeof(T), typeof(object)}, typeof(T).Module, true);
			else
				method = new DynamicMethod(name, null, new[] {typeof(object)}, typeof(T).Module, true);

			ILGenerator il = method.GetILGenerator();

			if (!fieldInfo.IsStatic) {
				//arg0.field = arg1 as fieldType;
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
			} else {
				//type.field = arg0 as fieldType;
				il.Emit(OpCodes.Ldarg_0);
			}

			if (fieldInfo.FieldType.IsValueType)
				il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
			else
				il.Emit(OpCodes.Castclass, fieldInfo.FieldType);

			if (!fieldInfo.IsStatic)
				il.Emit(OpCodes.Stfld, fieldInfo);
			else
				il.Emit(OpCodes.Stsfld, fieldInfo);

			il.Emit(OpCodes.Ret);

			if (!fieldInfo.IsStatic)
				setterFuncs[name] = method.CreateDelegate<Action<T, object>>();
			else
				setterStaticFuncs[name] = method.CreateDelegate<Action<object>>();

			if (setterFuncs.Count + setterStaticFuncs.Count == 1) {
				CoreLibMod.UnloadReflection += () => {
					setterFuncs.Clear();
					setterStaticFuncs.Clear();
				};
			}
		}

		/// <summary>
		/// A wrapper class for easily using delegates created by <seealso cref="ReflectionHelper{T}"/>
		/// </summary>
		public class ValueMutationMethodPackage {
			/// <summary>
			/// The name of the field being accessed
			/// </summary>
			public readonly string field;

			/// <summary>
			/// Constructs a getter and setter delegate for the field <paramref name="field"/> in <typeparamref name="T"/>
			/// </summary>
			/// <param name="field">The name of the field</param>
			public ValueMutationMethodPackage(string field) {
				this.field = field;
				CreateGetAccessor(field, out _);
				CreateSetAccessor(field, out _);
			}

			/// <summary>
			/// Calls the generated getter function for the package's field
			/// </summary>
			/// <param name="instance">The instance.  If the field is <see langword="static" />, pass <see langword="null" /> for this parameter.</param>
			/// <returns>The boxed value for the field</returns>
			public object GetValue(T instance)
				=> InvokeGetterFunction(field, instance);

			/// <summary>
			/// Calls the generated setter function for the package's field
			/// </summary>
			/// <param name="instance">The instance.  If the field is <see langword="static" />, pass <see langword="null" /> for this parameter.</param>
			/// <param name="value">The value to set the field to</param>
			public void SetValue(T instance, object value)
				=> InvokeSetterFunction(field, instance, value);
		}
	}

	public static class ReflectionHelper {
		public const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
	}
}
