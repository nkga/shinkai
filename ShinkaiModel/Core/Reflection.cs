using System;
using System.Linq;
using System.Reflection;

namespace ShinkaiModel.Core {
	public static class Reflection {
		// Public calls are useful for reflected, inaccessible objects.
		public static object ReflectionCall<T>(this T o, string methodName, bool isPublic = false, bool isStatic = false, params object[] args) {
			Type t = isStatic ? typeof(T) : o.GetType();
			MethodInfo methodInfo = GetMethod(t, methodName, isPublic, isStatic);
			return methodInfo.Invoke(o, args);
		}

		public static object ReflectionCall<T>(this T o, string methodName, Type[] types, bool isPublic = false, bool isStatic = false, params object[] args) {
			Type t = isStatic ? typeof(T) : o.GetType();
			MethodInfo methodInfo = GetMethod(t, methodName, isPublic, isStatic, types);
			return methodInfo.Invoke(o, args);
		}

		public static object ReflectionGet<T>(this T o, string fieldName, bool isPublic = false, bool isStatic = false) {
			Type t = isStatic ? typeof(T) : o.GetType();
			FieldInfo fieldInfo = GetField(t, fieldName, isPublic, isStatic);
			return fieldInfo.GetValue(o);
		}

		public static object ReflectionGet(this object o, FieldInfo fieldInfo) {
			return fieldInfo.GetValue(o);
		}

		public static object ReflectionGet<T, T2>(this T2 o, string fieldName, bool isPublic = false, bool isStatic = false) where T2 : T {
			Type t = typeof(T);
			FieldInfo fieldInfo = GetField(t, fieldName, isPublic, isStatic);
			return fieldInfo.GetValue(o);
		}

		public static void ReflectionSet<T>(this T o, string fieldName, object value, bool isPublic = false, bool isStatic = false) {
			Type t = isStatic ? typeof(T) : o.GetType();
			FieldInfo fieldInfo = GetField(t, fieldName, isPublic, isStatic);
			fieldInfo.SetValue(o, value);
		}

		public static void ReflectionSet(this object o, FieldInfo fieldInfo, object value) {
			fieldInfo.SetValue(o, value);
		}

		public static void ReflectionSet<T, T2>(this T2 o, string fieldName, object value, bool isPublic = false, bool isStatic = false) where T2 : T {
			Type t = typeof(T);
			FieldInfo fieldInfo = GetField(t, fieldName, isPublic, isStatic);
			fieldInfo.SetValue(o, value);
		}

		public static MethodInfo GetMethod<T>(string methodName, bool isPublic = false, bool isStatic = false, params Type[] types) {
			return GetMethod(typeof(T), methodName, isPublic, isStatic, types);
		}

		private static MethodInfo GetMethod(this Type t, string methodName, bool isPublic = false, bool isStatic = false, params Type[] types) {
			MethodInfo methodInfo;
			BindingFlags bindingFlags = GetBindingFlagsFromMethodQualifiers(isPublic, isStatic);
			if (types != null && types.Length > 0) {
				methodInfo = t.GetMethod(methodName, bindingFlags, null, types, null);
			} else {
				methodInfo = t.GetMethod(methodName, bindingFlags);
			}
			return methodInfo;
		}

		public static FieldInfo GetField<T>(string fieldName, bool isPublic = false, bool isStatic = false) {
			return GetField(typeof(T), fieldName, isPublic, isStatic);
		}

		private static FieldInfo GetField(this Type t, string fieldName, bool isPublic = false, bool isStatic = false) {
			BindingFlags bindingFlags = GetBindingFlagsFromMethodQualifiers(isPublic, isStatic);
			FieldInfo fieldInfo = t.GetField(fieldName, bindingFlags);
			return fieldInfo;
		}

		private static BindingFlags GetBindingFlagsFromMethodQualifiers(bool isPublic, bool isStatic) {
			BindingFlags bindingFlags = isPublic ? BindingFlags.Public : BindingFlags.NonPublic;
			bindingFlags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;

			return bindingFlags;
		}
	}
}
