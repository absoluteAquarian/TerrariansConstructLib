using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace TerrariansConstructLib.API.Reflection {
	/// <summary>
	/// Reflection helper for <typeparamref name="TType"/> methods with a <see langword="void"/> return signature and no parameters
	/// </summary>
	/// <typeparam name="TType">The declaring type</typeparam>
	public static class ReflectionHelperVoid<TType> {
		private delegate void MethodDelegate(TType instance);
		private delegate void StaticMethodDelegate();

		private static readonly Dictionary<string, MethodDelegate> methodFuncs = new();
		private static readonly Dictionary<string, StaticMethodDelegate> staticMethodFuncs = new();

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/> if necessary, then invokes it
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <param name="instance">The instance.  If the method is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <exception cref="Exception"/>
		/// <exception cref="InvalidOperationException"/>
		public static void InvokeMethod(string method, TType instance) {
			StaticMethodDelegate staticFunc = null;
			if (!methodFuncs.TryGetValue(method, out MethodDelegate func) && !staticMethodFuncs.TryGetValue(method, out staticFunc)) {
				CreateMethod(method);

				_ = methodFuncs.TryGetValue(method, out func) || staticMethodFuncs.TryGetValue(method, out staticFunc);
			}

			if (func is not null) {
				if (instance is null)
					throw new Exception("Cannot invoke instance methods from a null value");

				func(instance);

				return;
			}

			if (staticFunc is not null) {
				if (instance is not null)
					throw new Exception("Cannot invoke static methods from an instance");

				staticFunc();

				return;
			}

			throw new InvalidOperationException("Unable to create getter function");
		}

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/>
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <exception cref="ArgumentException"/>
		public static void CreateMethod(string method) {
			MethodInfo methodInfo = typeof(TType).GetMethod(method, ReflectionHelper.AllFlags, Type.EmptyTypes);

			if (methodFuncs.ContainsKey(method) || staticMethodFuncs.ContainsKey(method))
				return;  //Already defined

			if (methodInfo is null)
				throw new ArgumentException($"Method \"{typeof(TType).FullName}::{method}()\" does not exist");

			DynamicMethod dMethod;
			if (!methodInfo.IsStatic)
				dMethod = new DynamicMethod(method, null, new[] { typeof(TType) }, typeof(TType).Module, true);
			else
				dMethod = new DynamicMethod(method, null, Type.EmptyTypes, typeof(TType).Module, true);

			ILGenerator il = dMethod.GetILGenerator();

			if (!dMethod.IsStatic)
				il.Emit(OpCodes.Ldarg_0);

			il.Emit(OpCodes.Call, methodInfo);

			il.Emit(OpCodes.Ret);

			if (!methodInfo.IsStatic)
				methodFuncs[method] = dMethod.CreateDelegate<MethodDelegate>();
			else
				staticMethodFuncs[method] = dMethod.CreateDelegate<StaticMethodDelegate>();
		}
	}

	/// <summary>
	/// Reflection helper for <typeparamref name="TType"/> methods with a <typeparamref name="TReturn"/> return signature and no parameters
	/// </summary>
	/// <typeparam name="TType">The declaring type</typeparam>
	/// <typeparam name="TReturn">The return type of the method</typeparam>
	public static class ReflectionHelperReturn<TType, TReturn> {
		private delegate TReturn MethodDelegate(TType instance);
		private delegate TReturn StaticMethodDelegate();

		private static readonly Dictionary<string, MethodDelegate> methodFuncs = new();
		private static readonly Dictionary<string, StaticMethodDelegate> staticMethodFuncs = new();

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/> if necessary, then invokes it
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <param name="instance">The instance.  If the method is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <returns>The return value for the method</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="InvalidOperationException"/>
		public static TReturn InvokeMethod(string method, TType instance) {
			StaticMethodDelegate staticFunc = null;
			if (!methodFuncs.TryGetValue(method, out MethodDelegate func) && !staticMethodFuncs.TryGetValue(method, out staticFunc)) {
				CreateMethod(method);

				_ = methodFuncs.TryGetValue(method, out func) || staticMethodFuncs.TryGetValue(method, out staticFunc);
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
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/>
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <exception cref="ArgumentException"/>
		public static void CreateMethod(string method) {
			MethodInfo methodInfo = typeof(TType).GetMethod(method, ReflectionHelper.AllFlags, Type.EmptyTypes);

			if (methodFuncs.ContainsKey(method) || staticMethodFuncs.ContainsKey(method))
				return;  //Already defined

			if (methodInfo is null)
				throw new ArgumentException($"Method \"{typeof(TType).FullName}::{method}()\" does not exist");

			DynamicMethod dMethod;
			if (!methodInfo.IsStatic)
				dMethod = new DynamicMethod(method, typeof(TReturn), new[] { typeof(TType) }, typeof(TType).Module, true);
			else
				dMethod = new DynamicMethod(method, typeof(TReturn), Type.EmptyTypes, typeof(TType).Module, true);

			ILGenerator il = dMethod.GetILGenerator();

			if (!dMethod.IsStatic)
				il.Emit(OpCodes.Ldarg_0);

			il.Emit(OpCodes.Call, methodInfo);

			il.Emit(OpCodes.Ret);

			if (!methodInfo.IsStatic)
				methodFuncs[method] = dMethod.CreateDelegate<MethodDelegate>();
			else
				staticMethodFuncs[method] = dMethod.CreateDelegate<StaticMethodDelegate>();
		}
	}

	/// <summary>
	/// Reflection helper for <typeparamref name="TType"/> methods with a <see langword="void"/> return signature and a <typeparamref name="TArg"/> argument
	/// </summary>
	/// <typeparam name="TType">The declaring type</typeparam>
	/// <typeparam name="TArg">The type of the first argument</typeparam>
	public static class ReflectionHelperVoid<TType, TArg> {
		private delegate void MethodDelegate(TType instance, TArg arg);
		private delegate void StaticMethodDelegate(TArg arg);

		private static readonly Dictionary<string, MethodDelegate> methodFuncs = new();
		private static readonly Dictionary<string, StaticMethodDelegate> staticMethodFuncs = new();

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/> if necessary, then invokes it
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <param name="instance">The instance.  If the method is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <param name="arg">The first argument</param>
		/// <exception cref="Exception"/>
		/// <exception cref="InvalidOperationException"/>
		public static void InvokeMethod(string method, TType instance, TArg arg) {
			StaticMethodDelegate staticFunc = null;
			if (!methodFuncs.TryGetValue(method, out MethodDelegate func) && !staticMethodFuncs.TryGetValue(method, out staticFunc)) {
				CreateMethod(method);

				_ = methodFuncs.TryGetValue(method, out func) || staticMethodFuncs.TryGetValue(method, out staticFunc);
			}

			if (func is not null) {
				if (instance is null)
					throw new Exception("Cannot invoke instance methods from a null value");

				func(instance, arg);

				return;
			}

			if (staticFunc is not null) {
				if (instance is not null)
					throw new Exception("Cannot invoke static methods from an instance");

				staticFunc(arg);

				return;
			}

			throw new InvalidOperationException("Unable to create getter function");
		}

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/>
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <exception cref="ArgumentException"/>
		public static void CreateMethod(string method) {
			MethodInfo methodInfo = typeof(TType).GetMethod(method, ReflectionHelper.AllFlags, new Type[]{ typeof(TArg) });

			if (methodFuncs.ContainsKey(method) || staticMethodFuncs.ContainsKey(method))
				return;  //Already defined

			if (methodInfo is null)
				throw new ArgumentException($"Method \"{typeof(TType).FullName}::{method}({typeof(TArg).FullName})\" does not exist");

			DynamicMethod dMethod;
			if (!methodInfo.IsStatic)
				dMethod = new DynamicMethod(method, null, new[] { typeof(TType), typeof(TArg) }, typeof(TType).Module, true);
			else
				dMethod = new DynamicMethod(method, null, new[] { typeof(TArg) }, typeof(TType).Module, true);

			ILGenerator il = dMethod.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);

			if (!dMethod.IsStatic)
				il.Emit(OpCodes.Ldarg_1);

			il.Emit(OpCodes.Call, methodInfo);

			il.Emit(OpCodes.Ret);

			if (!methodInfo.IsStatic)
				methodFuncs[method] = dMethod.CreateDelegate<MethodDelegate>();
			else
				staticMethodFuncs[method] = dMethod.CreateDelegate<StaticMethodDelegate>();
		}
	}

	/// <summary>
	/// Reflection helper for <typeparamref name="TType"/> methods with a <typeparamref name="TReturn"/> return signature and a <typeparamref name="TArg"/> argument
	/// </summary>
	/// <typeparam name="TType">The declaring type</typeparam>
	/// <typeparam name="TArg">The type of the first argument</typeparam>
	/// <typeparam name="TReturn">The return type of the method</typeparam>
	public static class ReflectionHelper<TType, TArg, TReturn> {
		private delegate TReturn MethodDelegate(TType instance, TArg arg);
		private delegate TReturn StaticMethodDelegate(TArg arg);

		private static readonly Dictionary<string, MethodDelegate> methodFuncs = new();
		private static readonly Dictionary<string, StaticMethodDelegate> staticMethodFuncs = new();

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/> if necessary, then invokes it
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <param name="instance">The instance.  If the method is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <param name="arg">The first argument</param>
		/// <returns>The return value for the method</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="InvalidOperationException"/>
		public static TReturn InvokeMethod(string method, TType instance, TArg arg) {
			StaticMethodDelegate staticFunc = null;
			if (!methodFuncs.TryGetValue(method, out MethodDelegate func) && !staticMethodFuncs.TryGetValue(method, out staticFunc)) {
				CreateMethod(method);

				_ = methodFuncs.TryGetValue(method, out func) || staticMethodFuncs.TryGetValue(method, out staticFunc);
			}

			if (func is not null) {
				if (instance is null)
					throw new Exception("Cannot invoke instance methods from a null value");

				return func(instance, arg);
			}

			if (staticFunc is not null) {
				if (instance is not null)
					throw new Exception("Cannot invoke static methods from an instance");

				return staticFunc(arg);
			}

			throw new InvalidOperationException("Unable to create getter function");
		}

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/>
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <exception cref="ArgumentException"/>
		public static void CreateMethod(string method) {
			MethodInfo methodInfo = typeof(TType).GetMethod(method, ReflectionHelper.AllFlags, new Type[]{ typeof(TArg) });

			if (methodFuncs.ContainsKey(method) || staticMethodFuncs.ContainsKey(method))
				return;  //Already defined

			if (methodInfo is null)
				throw new ArgumentException($"Method \"{typeof(TType).FullName}::{method}({typeof(TArg).FullName})\" does not exist");

			DynamicMethod dMethod;
			if (!methodInfo.IsStatic)
				dMethod = new DynamicMethod(method, typeof(TReturn), new[] { typeof(TType), typeof(TArg) }, typeof(TType).Module, true);
			else
				dMethod = new DynamicMethod(method, typeof(TReturn), new[] { typeof(TArg) }, typeof(TType).Module, true);

			ILGenerator il = dMethod.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);

			if (!dMethod.IsStatic)
				il.Emit(OpCodes.Ldarg_1);

			il.Emit(OpCodes.Call, methodInfo);

			il.Emit(OpCodes.Ret);

			if (!methodInfo.IsStatic)
				methodFuncs[method] = dMethod.CreateDelegate<MethodDelegate>();
			else
				staticMethodFuncs[method] = dMethod.CreateDelegate<StaticMethodDelegate>();
		}
	}

	/// <summary>
	/// Reflection helper for <typeparamref name="TType"/> methods with a <see langword="void"/> return signature and (<typeparamref name="TArg"/>, <typeparamref name="TArg2"/>) arguments
	/// </summary>
	/// <typeparam name="TType">The declaring type</typeparam>
	/// <typeparam name="TArg">The type of the first argument</typeparam>
	/// <typeparam name="TArg2">The type of the second argument</typeparam>
	public static class ReflectionHelperVoid<TType, TArg, TArg2> {
		private delegate void MethodDelegate(TType instance, TArg arg, TArg2 arg2);
		private delegate void StaticMethodDelegate(TArg arg, TArg2 arg2);

		private static readonly Dictionary<string, MethodDelegate> methodFuncs = new();
		private static readonly Dictionary<string, StaticMethodDelegate> staticMethodFuncs = new();

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/> if necessary, then invokes it
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <param name="instance">The instance.  If the method is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <param name="arg">The first argument</param>
		/// <param name="arg2">The second argument</param>
		/// <exception cref="Exception"/>
		/// <exception cref="InvalidOperationException"/>
		public static void InvokeMethod(string method, TType instance, TArg arg, TArg2 arg2) {
			StaticMethodDelegate staticFunc = null;
			if (!methodFuncs.TryGetValue(method, out MethodDelegate func) && !staticMethodFuncs.TryGetValue(method, out staticFunc)) {
				CreateMethod(method);

				_ = methodFuncs.TryGetValue(method, out func) || staticMethodFuncs.TryGetValue(method, out staticFunc);
			}

			if (func is not null) {
				if (instance is null)
					throw new Exception("Cannot invoke instance methods from a null value");

				func(instance, arg, arg2);

				return;
			}

			if (staticFunc is not null) {
				if (instance is not null)
					throw new Exception("Cannot invoke static methods from an instance");

				staticFunc(arg, arg2);

				return;
			}

			throw new InvalidOperationException("Unable to create getter function");
		}

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/>
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <exception cref="ArgumentException"/>
		public static void CreateMethod(string method) {
			MethodInfo methodInfo = typeof(TType).GetMethod(method, ReflectionHelper.AllFlags, new Type[]{ typeof(TArg), typeof(TArg2) });

			if (methodFuncs.ContainsKey(method) || staticMethodFuncs.ContainsKey(method))
				return;  //Already defined

			if (methodInfo is null)
				throw new ArgumentException($"Method \"{typeof(TType).FullName}::{method}({typeof(TArg).FullName})\" does not exist");

			DynamicMethod dMethod;
			if (!methodInfo.IsStatic)
				dMethod = new DynamicMethod(method, null, new[] { typeof(TType), typeof(TArg), typeof(TArg2) }, typeof(TType).Module, true);
			else
				dMethod = new DynamicMethod(method, null, new[] { typeof(TArg), typeof(TArg2) }, typeof(TType).Module, true);

			ILGenerator il = dMethod.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);

			if (!dMethod.IsStatic)
				il.Emit(OpCodes.Ldarg_2);

			il.Emit(OpCodes.Call, methodInfo);

			il.Emit(OpCodes.Ret);

			if (!methodInfo.IsStatic)
				methodFuncs[method] = dMethod.CreateDelegate<MethodDelegate>();
			else
				staticMethodFuncs[method] = dMethod.CreateDelegate<StaticMethodDelegate>();
		}
	}

	/// <summary>
	/// Reflection helper for <typeparamref name="TType"/> methods with a <typeparamref name="TReturn"/> return signature and (<typeparamref name="TArg"/>, <typeparamref name="TArg2"/>) arguments
	/// </summary>
	/// <typeparam name="TType">The declaring type</typeparam>
	/// <typeparam name="TArg">The type of the first argument</typeparam>
	/// <typeparam name="TArg2">The type of the second argument</typeparam>
	/// <typeparam name="TReturn">The return type of the method</typeparam>
	public static class ReflectionHelper<TType, TArg, TArg2, TReturn> {
		private delegate TReturn MethodDelegate(TType instance, TArg arg, TArg2 arg2);
		private delegate TReturn StaticMethodDelegate(TArg arg, TArg2 arg2);

		private static readonly Dictionary<string, MethodDelegate> methodFuncs = new();
		private static readonly Dictionary<string, StaticMethodDelegate> staticMethodFuncs = new();

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/> if necessary, then invokes it
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <param name="instance">The instance.  If the method is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <param name="arg">The first argument</param>
		/// <param name="arg2">The second argument</param>
		/// <returns>The return value for the method</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="InvalidOperationException"/>
		public static TReturn InvokeMethod(string method, TType instance, TArg arg, TArg2 arg2) {
			StaticMethodDelegate staticFunc = null;
			if (!methodFuncs.TryGetValue(method, out MethodDelegate func) && !staticMethodFuncs.TryGetValue(method, out staticFunc)) {
				CreateMethod(method);

				_ = methodFuncs.TryGetValue(method, out func) || staticMethodFuncs.TryGetValue(method, out staticFunc);
			}

			if (func is not null) {
				if (instance is null)
					throw new Exception("Cannot invoke instance methods from a null value");

				return func(instance, arg, arg2);
			}

			if (staticFunc is not null) {
				if (instance is not null)
					throw new Exception("Cannot invoke static methods from an instance");

				return staticFunc(arg, arg2);
			}

			throw new InvalidOperationException("Unable to create getter function");
		}

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/>
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <exception cref="ArgumentException"/>
		public static void CreateMethod(string method) {
			MethodInfo methodInfo = typeof(TType).GetMethod(method, ReflectionHelper.AllFlags, new Type[]{ typeof(TArg), typeof(TArg2) });

			if (methodFuncs.ContainsKey(method) || staticMethodFuncs.ContainsKey(method))
				return;  //Already defined

			if (methodInfo is null)
				throw new ArgumentException($"Method \"{typeof(TType).FullName}::{method}({typeof(TArg).FullName})\" does not exist");

			DynamicMethod dMethod;
			if (!methodInfo.IsStatic)
				dMethod = new DynamicMethod(method, typeof(TReturn), new[] { typeof(TType), typeof(TArg), typeof(TArg2) }, typeof(TType).Module, true);
			else
				dMethod = new DynamicMethod(method, typeof(TReturn), new[] { typeof(TArg), typeof(TArg2) }, typeof(TType).Module, true);

			ILGenerator il = dMethod.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);

			if (!dMethod.IsStatic)
				il.Emit(OpCodes.Ldarg_2);

			il.Emit(OpCodes.Call, methodInfo);

			il.Emit(OpCodes.Ret);

			if (!methodInfo.IsStatic)
				methodFuncs[method] = dMethod.CreateDelegate<MethodDelegate>();
			else
				staticMethodFuncs[method] = dMethod.CreateDelegate<StaticMethodDelegate>();
		}
	}

	/// <summary>
	/// Reflection helper for <typeparamref name="TType"/> methods with a <see langword="void"/> return signature and (<typeparamref name="TArg"/>, <typeparamref name="TArg2"/>, <typeparamref name="TArg3"/>) arguments
	/// </summary>
	/// <typeparam name="TType">The declaring type</typeparam>
	/// <typeparam name="TArg">The type of the first argument</typeparam>
	/// <typeparam name="TArg2">The type of the second argument</typeparam>
	/// <typeparam name="TArg3">The type of the third argument</typeparam>
	public static class ReflectionHelperVoid<TType, TArg, TArg2, TArg3> {
		private delegate void MethodDelegate(TType instance, TArg arg, TArg2 arg2, TArg3 arg3);
		private delegate void StaticMethodDelegate(TArg arg, TArg2 arg2, TArg3 arg3);

		private static readonly Dictionary<string, MethodDelegate> methodFuncs = new();
		private static readonly Dictionary<string, StaticMethodDelegate> staticMethodFuncs = new();

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/> if necessary, then invokes it
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <param name="instance">The instance.  If the method is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <param name="arg">The first argument</param>
		/// <param name="arg2">The second argument</param>
		/// <param name="arg3">The third argument</param>
		/// <exception cref="Exception"/>
		/// <exception cref="InvalidOperationException"/>
		public static void InvokeMethod(string method, TType instance, TArg arg, TArg2 arg2, TArg3 arg3) {
			StaticMethodDelegate staticFunc = null;
			if (!methodFuncs.TryGetValue(method, out MethodDelegate func) && !staticMethodFuncs.TryGetValue(method, out staticFunc)) {
				CreateMethod(method);

				_ = methodFuncs.TryGetValue(method, out func) || staticMethodFuncs.TryGetValue(method, out staticFunc);
			}

			if (func is not null) {
				if (instance is null)
					throw new Exception("Cannot invoke instance methods from a null value");

				func(instance, arg, arg2, arg3);

				return;
			}

			if (staticFunc is not null) {
				if (instance is not null)
					throw new Exception("Cannot invoke static methods from an instance");

				staticFunc(arg, arg2, arg3);

				return;
			}

			throw new InvalidOperationException("Unable to create getter function");
		}

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/>
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <exception cref="ArgumentException"/>
		public static void CreateMethod(string method) {
			MethodInfo methodInfo = typeof(TType).GetMethod(method, ReflectionHelper.AllFlags, new Type[]{ typeof(TArg), typeof(TArg2), typeof(TArg3) });

			if (methodFuncs.ContainsKey(method) || staticMethodFuncs.ContainsKey(method))
				return;  //Already defined

			if (methodInfo is null)
				throw new ArgumentException($"Method \"{typeof(TType).FullName}::{method}({typeof(TArg).FullName})\" does not exist");

			DynamicMethod dMethod;
			if (!methodInfo.IsStatic)
				dMethod = new DynamicMethod(method, null, new[] { typeof(TType), typeof(TArg), typeof(TArg2), typeof(TArg3) }, typeof(TType).Module, true);
			else
				dMethod = new DynamicMethod(method, null, new[] { typeof(TArg), typeof(TArg2), typeof(TArg3) }, typeof(TType).Module, true);

			ILGenerator il = dMethod.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);

			if (!dMethod.IsStatic)
				il.Emit(OpCodes.Ldarg_3);

			il.Emit(OpCodes.Call, methodInfo);

			il.Emit(OpCodes.Ret);

			if (!methodInfo.IsStatic)
				methodFuncs[method] = dMethod.CreateDelegate<MethodDelegate>();
			else
				staticMethodFuncs[method] = dMethod.CreateDelegate<StaticMethodDelegate>();
		}
	}

	/// <summary>
	/// Reflection helper for <typeparamref name="TType"/> methods with a <typeparamref name="TReturn"/> return signature and (<typeparamref name="TArg"/>, <typeparamref name="TArg2"/>, <typeparamref name="TArg3"/>) arguments
	/// </summary>
	/// <typeparam name="TType">The declaring type</typeparam>
	/// <typeparam name="TArg">The type of the first argument</typeparam>
	/// <typeparam name="TArg2">The type of the second argument</typeparam>
	/// <typeparam name="TArg3">The type of the third argument</typeparam>
	/// <typeparam name="TReturn">The return type of the method</typeparam>
	public static class ReflectionHelper<TType, TArg, TArg2, TArg3, TReturn> {
		private delegate TReturn MethodDelegate(TType instance, TArg arg, TArg2 arg2, TArg3 arg3);
		private delegate TReturn StaticMethodDelegate(TArg arg, TArg2 arg2, TArg3 arg3);

		private static readonly Dictionary<string, MethodDelegate> methodFuncs = new();
		private static readonly Dictionary<string, StaticMethodDelegate> staticMethodFuncs = new();

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/> if necessary, then invokes it
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <param name="instance">The instance.  If the method is <see langword="static"/>, pass <see langword="null"/> for this parameter.</param>
		/// <param name="arg">The first argument</param>
		/// <param name="arg2">The second argument</param>
		/// <param name="arg3">The third argument</param>
		/// <returns>The return value for the method</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="InvalidOperationException"/>
		public static TReturn InvokeMethod(string method, TType instance, TArg arg, TArg2 arg2, TArg3 arg3) {
			StaticMethodDelegate staticFunc = null;
			if (!methodFuncs.TryGetValue(method, out MethodDelegate func) && !staticMethodFuncs.TryGetValue(method, out staticFunc)) {
				CreateMethod(method);

				_ = methodFuncs.TryGetValue(method, out func) || staticMethodFuncs.TryGetValue(method, out staticFunc);
			}

			if (func is not null) {
				if (instance is null)
					throw new Exception("Cannot invoke instance methods from a null value");

				return func(instance, arg, arg2, arg3);
			}

			if (staticFunc is not null) {
				if (instance is not null)
					throw new Exception("Cannot invoke static methods from an instance");

				return staticFunc(arg, arg2, arg3);
			}

			throw new InvalidOperationException("Unable to create getter function");
		}

		/// <summary>
		/// Creates a delegate for the method, <paramref name="method"/>, in <typeparamref name="TType"/>
		/// </summary>
		/// <param name="method">The name of the method</param>
		/// <exception cref="ArgumentException"/>
		public static void CreateMethod(string method) {
			MethodInfo methodInfo = typeof(TType).GetMethod(method, ReflectionHelper.AllFlags, new Type[]{ typeof(TArg), typeof(TArg2) });

			if (methodFuncs.ContainsKey(method) || staticMethodFuncs.ContainsKey(method))
				return;  //Already defined

			if (methodInfo is null)
				throw new ArgumentException($"Method \"{typeof(TType).FullName}::{method}({typeof(TArg).FullName})\" does not exist");

			DynamicMethod dMethod;
			if (!methodInfo.IsStatic)
				dMethod = new DynamicMethod(method, typeof(TReturn), new[] { typeof(TType), typeof(TArg), typeof(TArg2), typeof(TArg3) }, typeof(TType).Module, true);
			else
				dMethod = new DynamicMethod(method, typeof(TReturn), new[] { typeof(TArg), typeof(TArg2), typeof(TArg3) }, typeof(TType).Module, true);

			ILGenerator il = dMethod.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);

			if (!dMethod.IsStatic)
				il.Emit(OpCodes.Ldarg_3);

			il.Emit(OpCodes.Call, methodInfo);

			il.Emit(OpCodes.Ret);

			if (!methodInfo.IsStatic)
				methodFuncs[method] = dMethod.CreateDelegate<MethodDelegate>();
			else
				staticMethodFuncs[method] = dMethod.CreateDelegate<StaticMethodDelegate>();
		}
	}
}
