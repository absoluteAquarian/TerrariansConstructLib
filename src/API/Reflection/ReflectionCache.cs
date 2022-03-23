using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TerrariansConstructLib.API.Reflection {
	public static class ReflectionCache {
		public const BindingFlags UniversalFlags =
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

		private static readonly Dictionary<CacheType, Dictionary<string, object>> cache = new() {
			[CacheType.Field] = new Dictionary<string, object>(),
			[CacheType.Method] = new Dictionary<string, object>(),
			[CacheType.Property] = new Dictionary<string, object>(),
			[CacheType.Constructor] = new Dictionary<string, object>(),
			[CacheType.Type] = new Dictionary<string, object>()
		};

		public static Type? GetCachedType(this Assembly assembly, string name) {
			return RetrieveFromCache(CacheType.Type, name, () => assembly.GetType(name));
		}

		public static FieldInfo? GetCachedField(this Type type, string name) {
			return RetrieveFromCache(CacheType.Field, GetFieldNameForCache(type, name), () => type.GetField(name, UniversalFlags));
		}

		public static PropertyInfo? GetCachedProperty(this Type type, string name) {
			return RetrieveFromCache(CacheType.Property, GetPropertyNameForCache(type, name), () => type.GetProperty(name, UniversalFlags));
		}

		public static ConstructorInfo? GetCachedConstructor(this Type type, params Type[] types) {
			return RetrieveFromCache(CacheType.Constructor, GetConstructorNameForCache(type, types), () => type.GetConstructor(UniversalFlags, null, types, null));
		}

		public static MethodInfo? GetCachedMethod(this Type type, string name) {
			return RetrieveFromCache(CacheType.Method, GetMethodNameForCache(type, name), () => type.GetMethod(name, UniversalFlags));
		}

		public static object? InvokeUnderlyingMethod(this FieldInfo field, string method, object fieldInstance, params object[] parameters) {
			return field.FieldType.GetCachedMethod(method)?.Invoke(field.GetValue(fieldInstance), parameters);
		}

		public static string GetFieldNameForCache(Type type, string fieldName) {
			string assemblyName = type.Assembly.GetName().Name!;
			string typeName = type.Name;
			return $"{assemblyName}.{typeName}.{fieldName}";
		}

		public static string GetPropertyNameForCache(Type type, string property) {
			string assemblyName = type.Assembly.GetName().Name!;
			string typeName = type.Name;
			return $"{assemblyName}.{typeName}.{property}";
		}

		public static string GetConstructorNameForCache(Type type, params Type[] types) {
			string assemblyName = type.Assembly.GetName().Name!;
			string typeName = type.Name;
			List<string> typeNames = types.Select(cType => cType.Name).ToList();
			return $"{assemblyName}.{typeName}::{{{string.Join(",", typeNames)}}}";
		}

		public static string GetMethodNameForCache(Type type, string method) {
			string assemblyName = type.Assembly.GetName().Name!;
			string typeName = type.Name;
			return $"{assemblyName}.{typeName}::{method}";
		}

		private static T RetrieveFromCache<T>(CacheType refType, string key, Func<T> fallback) {
			if (cache[refType].ContainsKey(key))
				return (T)cache[refType][key];

			T value = fallback();
			cache[refType].Add(key, value!);
			return value;
		}

		public static F? GetFieldValue<T, F>(this T obj, string name) {
			return (F?)typeof(T).GetCachedField(name)?.GetValue(obj);
		}

		public static void SetFieldValue<T, F>(this T obj, string name, F value) {
			typeof(T).GetCachedField(name)?.SetValue(obj, value);
		}

		public static F? GetPropertyValue<T, F>(this T obj, string name) {
			return (F?)typeof(T).GetCachedProperty(name)?.GetValue(obj);
		}

		public static void SetPropertyValue<T, F>(this T obj, string name, F value) {
			typeof(T).GetCachedProperty(name)?.SetValue(obj, value);
		}

		public static FieldInfo? GetCachedField<T>(string name) {
			return typeof(T).GetCachedField(name);
		}

		public static PropertyInfo? GetCachedProperty<T>(string name) {
			return typeof(T).GetCachedProperty(name);
		}

		public static void SetToNewInstance(this FieldInfo field, object? instance = null, object? value = null) {
			field.SetValue(instance, value ?? Activator.CreateInstance(field.FieldType));
		}

		public static void SetToNewInstance(this PropertyInfo property, object? propertyInstance = null,
			object? value = null) {
			property.SetValue(propertyInstance, value ?? Activator.CreateInstance(property.PropertyType));
		}

		public static T? GetValue<T>(this FieldInfo field, object instance) {
			return (T?)field.GetValue(instance);
		}

		public static T? GetValue<T>(this PropertyInfo property, object instance) {
			return (T?)property.GetValue(instance);
		}

		private enum CacheType {
			Field,
			Method,
			Property,
			Constructor,
			Type
		}
	}
}
