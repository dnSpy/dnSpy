/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdTypeBase : DmdType {
		internal sealed override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();

		readonly ReadOnlyCollection<DmdCustomModifier> customModifiers;
		public sealed override ReadOnlyCollection<DmdCustomModifier> GetCustomModifiers() => customModifiers;
		protected DmdTypeBase(IList<DmdCustomModifier> customModifiers) =>
			this.customModifiers = ReadOnlyCollectionHelpers.Create(customModifiers);
		protected IList<DmdCustomModifier> VerifyCustomModifiers(IList<DmdCustomModifier> customModifiers) {
			if (customModifiers != null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != AppDomain)
						throw new ArgumentException();
				}
			}
			return customModifiers;
		}

		/// <summary>
		/// true if there are no metadata references. This instance and any other <see cref="DmdType"/> that it
		/// references directly or indirectly (element type, generic arguments) are all resolved types
		/// (TypeDefs, and not TypeRefs).
		/// 
		/// Even if this property is true, it could still have metadata references:
		/// optional/required modifiers, base type, types in custom attributes, etc could
		/// contain one or more TypeRefs.
		/// </summary>
		public abstract bool IsFullyResolved { get; }

		/// <summary>
		/// Resolves a type whose <see cref="IsFullyResolved"/> property is true or returns null if the resolve failed
		/// </summary>
		/// <returns></returns>
		public abstract DmdTypeBase FullResolve();

		public sealed override DmdType Resolve(bool throwOnError) {
			var res = ResolveNoThrowCore();
			if ((object)res == null && throwOnError)
				throw new TypeResolveException(this);
			return res;
		}
		protected abstract DmdType ResolveNoThrowCore();

		public override DmdMethodBase DeclaringMethod => throw new InvalidOperationException();
		public sealed override Guid GUID => throw new NotImplementedException();//TODO:
		public sealed override DmdAssembly Assembly => Module.Assembly;
		public sealed override bool IsCOMObject => throw new NotImplementedException();//TODO:
		public sealed override bool HasElementType => (object)GetElementType() != null;
		public override DmdGenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException();
		public override bool IsGenericType => false;
		public override bool IsGenericTypeDefinition => false;
		public override int GenericParameterPosition => throw new InvalidOperationException();
		public override ReadOnlyCollection<DmdType> GetGenericArguments() => ReadOnlyCollectionHelpers.Empty<DmdType>();
		public override DmdType GetGenericTypeDefinition() => throw new InvalidOperationException();
		public override ReadOnlyCollection<DmdType> GetGenericParameterConstraints() => throw new InvalidOperationException();
		public override DmdMethodSignature GetFunctionPointerMethodSignature() => throw new InvalidOperationException();
		public override DmdType GetElementType() => null;
		public override int GetArrayRank() => throw new ArgumentException();
		public override ReadOnlyCollection<int> GetArraySizes() => throw new ArgumentException();
		public override ReadOnlyCollection<int> GetArrayLowerBounds() => throw new ArgumentException();
		public sealed override DmdType MakePointerType() => AppDomain.MakePointerType(this, null);
		public sealed override DmdType MakeByRefType() => AppDomain.MakeByRefType(this, null);
		public sealed override DmdType MakeArrayType() => AppDomain.MakeArrayType(this, null);
		public sealed override DmdType MakeArrayType(int rank, IList<int> sizes, IList<int> lowerBounds) => AppDomain.MakeArrayType(this, rank, sizes, lowerBounds, null);
		public sealed override DmdType MakeGenericType(IList<DmdType> typeArguments) => AppDomain.MakeGenericType(this, typeArguments, null);

		protected DmdType SkipElementTypes() {
			DmdType type = this;
			while (type.HasElementType)
				type = type.GetElementType();
			return type;
		}

		public sealed override DmdConstructorInfo GetConstructor(DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types) {
			foreach (var ctor in GetDeclaredConstructors()) {
				if (DmdMemberInfoComparer.IsMatch(ctor, bindingAttr, callConvention, types))
					return ctor;
			}
			return null;
		}

		public sealed override DmdConstructorInfo[] GetConstructors(DmdBindingFlags bindingAttr) {
			List<DmdConstructorInfo> ctors = null;
			foreach (var ctor in GetDeclaredConstructors()) {
				if (DmdMemberInfoComparer.IsMatch(ctor, bindingAttr)) {
					if (ctors == null)
						ctors = new List<DmdConstructorInfo>();
					ctors.Add(ctor);
				}
			}
			return ctors?.ToArray() ?? Array.Empty<DmdConstructorInfo>();
		}

		public sealed override DmdMethodInfo GetMethod(string name, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			foreach (var method in GetMethods(inherit: (bindingAttr & DmdBindingFlags.DeclaredOnly) == 0)) {
				if (DmdMemberInfoComparer.IsMatch(method, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(method, bindingAttr, callConvention, types))
					return method;
			}
			return null;
		}

		public sealed override DmdMethodInfo[] GetMethods(DmdBindingFlags bindingAttr) {
			List<DmdMethodInfo> methods = null;
			foreach (var method in GetMethods(inherit: (bindingAttr & DmdBindingFlags.DeclaredOnly) == 0)) {
				if (DmdMemberInfoComparer.IsMatch(method, bindingAttr)) {
					if (methods == null)
						methods = new List<DmdMethodInfo>();
					methods.Add(method);
				}
			}
			return methods?.ToArray() ?? Array.Empty<DmdMethodInfo>();
		}

		public sealed override DmdFieldInfo GetField(string name, DmdBindingFlags bindingAttr) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			foreach (var field in GetFields(inherit: (bindingAttr & DmdBindingFlags.DeclaredOnly) == 0)) {
				if (DmdMemberInfoComparer.IsMatch(field, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(field, bindingAttr))
					return field;
			}
			return null;
		}

		public sealed override DmdFieldInfo[] GetFields(DmdBindingFlags bindingAttr) {
			List<DmdFieldInfo> fields = null;
			foreach (var field in GetFields(inherit: (bindingAttr & DmdBindingFlags.DeclaredOnly) == 0)) {
				if (DmdMemberInfoComparer.IsMatch(field, bindingAttr)) {
					if (fields == null)
						fields = new List<DmdFieldInfo>();
					fields.Add(field);
				}
			}
			return fields?.ToArray() ?? Array.Empty<DmdFieldInfo>();
		}

		public sealed override DmdEventInfo GetEvent(string name, DmdBindingFlags bindingAttr) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			foreach (var @event in GetEvents(inherit: (bindingAttr & DmdBindingFlags.DeclaredOnly) == 0)) {
				if (DmdMemberInfoComparer.IsMatch(@event, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(@event, bindingAttr))
					return @event;
			}
			return null;
		}

		public sealed override DmdEventInfo[] GetEvents(DmdBindingFlags bindingAttr) {
			List<DmdEventInfo> events = null;
			foreach (var @event in GetEvents(inherit: (bindingAttr & DmdBindingFlags.DeclaredOnly) == 0)) {
				if (DmdMemberInfoComparer.IsMatch(@event, bindingAttr)) {
					if (events == null)
						events = new List<DmdEventInfo>();
					events.Add(@event);
				}
			}
			return events?.ToArray() ?? Array.Empty<DmdEventInfo>();
		}

		public sealed override DmdPropertyInfo GetProperty(string name, DmdBindingFlags bindingAttr, DmdType returnType, IList<DmdType> types) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			foreach (var property in GetProperties(inherit: (bindingAttr & DmdBindingFlags.DeclaredOnly) == 0)) {
				if (DmdMemberInfoComparer.IsMatch(property, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(property, bindingAttr, returnType, types))
					return property;
			}
			return null;
		}

		public sealed override DmdPropertyInfo[] GetProperties(DmdBindingFlags bindingAttr) {
			List<DmdPropertyInfo> properties = null;
			foreach (var property in GetProperties(inherit: (bindingAttr & DmdBindingFlags.DeclaredOnly) == 0)) {
				if (DmdMemberInfoComparer.IsMatch(property, bindingAttr)) {
					if (properties == null)
						properties = new List<DmdPropertyInfo>();
					properties.Add(property);
				}
			}
			return properties?.ToArray() ?? Array.Empty<DmdPropertyInfo>();
		}

		public sealed override DmdMemberInfo[] GetMember(string name, DmdMemberTypes type, DmdBindingFlags bindingAttr) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			DmdConstructorInfo[] ctors = null;
			if ((type & DmdMemberTypes.Constructor) != 0) {
				ctors = GetConstructors(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Constructor)
					return ctors;
			}

			DmdMethodInfo[] methods = null;
			if ((type & DmdMemberTypes.Method) != 0) {
				methods = GetMethods(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Method)
					return methods;
			}

			DmdFieldInfo[] fields = null;
			if ((type & DmdMemberTypes.Field) != 0) {
				fields = GetFields(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Field)
					return fields;
			}

			DmdPropertyInfo[] properties = null;
			if ((type & DmdMemberTypes.Property) != 0) {
				properties = GetProperties(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Property)
					return properties;
			}

			DmdEventInfo[] events = null;
			if ((type & DmdMemberTypes.Event) != 0) {
				events = GetEvents(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Event)
					return events;
			}

			DmdType[] types = null;
			if ((type & (DmdMemberTypes.TypeInfo | DmdMemberTypes.NestedType)) != 0) {
				types = GetNestedTypes(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				// Matches Reflection behavior
				if (type == DmdMemberTypes.TypeInfo || type == DmdMemberTypes.NestedType)
					return types;
			}

			int len = (ctors?.Length ?? 0) + (methods?.Length ?? 0) + (fields?.Length ?? 0) + (properties?.Length ?? 0) + (events?.Length ?? 0) + (types?.Length ?? 0);
			var res = type == (DmdMemberTypes.Method | DmdMemberTypes.Constructor) ? new DmdMethodBase[len] : new DmdMemberInfo[len];
			int index = 0;
			Copy(ctors, res, ref index);
			Copy(methods, res, ref index);
			Copy(fields, res, ref index);
			Copy(properties, res, ref index);
			Copy(events, res, ref index);
			Copy(types, res, ref index);
			if (index != res.Length)
				throw new InvalidOperationException();
			return res;
		}

		static void Copy(DmdMemberInfo[] src, DmdMemberInfo[] dst, ref int index) {
			if (src == null)
				return;
			Array.Copy(src, 0, dst, index, src.Length);
			index += src.Length;
		}

		public sealed override DmdMemberInfo[] GetMembers(DmdBindingFlags bindingAttr) {
			var list = new List<DmdMemberInfo>();
			list.AddRange(GetMethods(bindingAttr));
			list.AddRange(GetConstructors(bindingAttr));
			list.AddRange(GetProperties(bindingAttr));
			list.AddRange(GetEvents(bindingAttr));
			list.AddRange(GetFields(bindingAttr));
			list.AddRange(GetNestedTypes(bindingAttr));
			return list.ToArray();
		}

		public sealed override DmdType[] GetNestedTypes(DmdBindingFlags bindingAttr) {
			var nestedTypes = GetAllNestedTypes();
			if (nestedTypes.Count == 0)
				return Array.Empty<DmdType>();
			var list = new List<DmdType>(nestedTypes.Count);
			foreach (var type in nestedTypes) {
				if (DmdMemberInfoComparer.IsMatch(type, bindingAttr))
					list.Add(type);
			}
			return list.ToArray();
		}

		public sealed override DmdType GetNestedType(string fullName, DmdBindingFlags bindingAttr) {
			if (fullName == null)
				throw new ArgumentNullException(nameof(fullName));
			var nestedTypes = GetAllNestedTypes();
			if (nestedTypes.Count == 0)
				return null;
			DmdTypeUtilities.SplitFullName(fullName, out var @namespace, out var name);
			foreach (var type in nestedTypes) {
				if (DmdMemberInfoComparer.IsMatch(type, bindingAttr) && DmdMemberInfoComparer.IsMatch(type, @namespace, name, bindingAttr))
					return type;
			}
			return null;
		}

		public sealed override ReadOnlyCollection<DmdType> GetAllNestedTypes() => NestedTypes;
		protected virtual DmdType[] CreateNestedTypes() => null;

		public sealed override DmdType GetInterface(string fullName, bool ignoreCase) {
			if (fullName == null)
				throw new ArgumentNullException(nameof(fullName));
			var ifaces = GetInterfaces();
			if (ifaces.Count == 0)
				return null;
			DmdTypeUtilities.SplitFullName(fullName, out var @namespace, out var name);
			var bindingAttr = ignoreCase ? DmdBindingFlags.IgnoreCase : DmdBindingFlags.Default;
			foreach (var type in ifaces) {
				if (DmdMemberInfoComparer.IsMatch(type, @namespace, name, bindingAttr))
					return type;
			}
			return null;
		}

		public sealed override ReadOnlyCollection<DmdType> GetInterfaces() {
			var f = ExtraFields;
			if (f.__implementedInterfaces_DONT_USE != null)
				return f.__implementedInterfaces_DONT_USE;

			lock (LockObject) {
				if (f.__implementedInterfaces_DONT_USE != null)
					return f.__implementedInterfaces_DONT_USE;

				var implIfaces = CreateInterfaces(this);
				f.__implementedInterfaces_DONT_USE = ReadOnlyCollectionHelpers.Create(implIfaces);
				return f.__implementedInterfaces_DONT_USE;
			}
		}
		protected abstract IList<DmdType> ReadDeclaredInterfaces();

		static DmdType[] CreateInterfaces(DmdTypeBase type) {
			//TODO: Pool these?
			var list = new List<DmdType>();
			var hash = new HashSet<DmdType>(DmdMemberInfoEqualityComparer.Default);
			var stack = new Stack<IEnumerator<DmdType>>();

			IEnumerator<DmdType> tmpEnum = null;
			try {
				for (var t = type; ;) {
					if (!hash.Add(t))
						break;
					stack.Push(tmpEnum = t.DeclaredInterfaces.GetEnumerator());
					tmpEnum = null;
					t = (DmdTypeBase)t.BaseType;
					if ((object)t == null)
						break;
				}

				while (stack.Count > 0) {
					var enumerator = tmpEnum = stack.Pop();
					for (;;) {
						if (!enumerator.MoveNext()) {
							tmpEnum = null;
							enumerator.Dispose();
							break;
						}

						var iface = (DmdTypeBase)enumerator.Current;
						if (!hash.Add(iface))
							continue;
						list.Add(iface);
						stack.Push(enumerator);
						tmpEnum = null;
						enumerator = tmpEnum = iface.DeclaredInterfaces.GetEnumerator();
					}
				}
			}
			finally {
				tmpEnum?.Dispose();
				while (stack.Count > 0)
					stack.Pop().Dispose();
			}

			return list.ToArray();
		}

		public sealed override DmdMemberInfo[] GetDefaultMembers() {
			var name = GetDefaultMemberName(this);
			if (name == null)
				return Array.Empty<DmdMemberInfo>();
			return GetMember(name);
		}

		static string GetDefaultMemberName(DmdType type) {
			var defaultMemberAttribute = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Reflection_DefaultMemberAttribute, isOptional: true);
			if ((object)defaultMemberAttribute == null)
				return null;
			for (int i = 0; i < 100; i++) {
				foreach (var ca in type.GetCustomAttributesData()) {
					if (ca.AttributeType == defaultMemberAttribute)
						return ca.ConstructorArguments.Count == 1 ? ca.ConstructorArguments[0].Value as string : null;
				}
				type = type.BaseType;
				if ((object)type == null)
					break;
			}
			return null;
		}

		public sealed override string[] GetEnumNames() => throw new NotImplementedException();//TODO:

		public sealed override IList<DmdCustomAttributeData> GetCustomAttributesData() {
			var f = ExtraFields;
			if (f.__customAttributes_DONT_USE != null)
				return f.__customAttributes_DONT_USE;
			lock (LockObject) {
				if (f.__customAttributes_DONT_USE != null)
					return f.__customAttributes_DONT_USE;
				var res = CreateCustomAttributes();
				f.__customAttributes_DONT_USE = CustomAttributesHelper.AddPseudoCustomAttributes(this, res);
				return f.__customAttributes_DONT_USE;
			}
		}
		protected virtual DmdCustomAttributeData[] CreateCustomAttributes() => null;

		protected virtual DmdFieldInfo[] CreateDeclaredFields(DmdType reflectedType) => null;
		protected virtual DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType, bool includeConstructors) => null;
		protected virtual DmdPropertyInfo[] CreateDeclaredProperties(DmdType reflectedType) => null;
		protected virtual DmdEventInfo[] CreateDeclaredEvents(DmdType reflectedType) => null;

		internal ReadOnlyCollection<DmdFieldInfo> DeclaredFields {
			get {
				var f = ExtraFields;
				if (f.__declaredFields_DONT_USE != null)
					return f.__declaredFields_DONT_USE;
				lock (LockObject) {
					if (f.__declaredFields_DONT_USE != null)
						return f.__declaredFields_DONT_USE;
					var res = CreateDeclaredFields(this);
					f.__declaredFields_DONT_USE = ReadOnlyCollectionHelpers.Create(res);
					return f.__declaredFields_DONT_USE;
				}
			}
		}

		internal ReadOnlyCollection<DmdMethodBase> DeclaredMethods {
			get {
				var f = ExtraFields;
				if (f.__declaredMethods_DONT_USE != null)
					return f.__declaredMethods_DONT_USE;
				lock (LockObject) {
					if (f.__declaredMethods_DONT_USE != null)
						return f.__declaredMethods_DONT_USE;
					var res = CreateDeclaredMethods(this, includeConstructors: true);
					f.__declaredMethods_DONT_USE = ReadOnlyCollectionHelpers.Create(res);
					return f.__declaredMethods_DONT_USE;
				}
			}
		}

		internal ReadOnlyCollection<DmdPropertyInfo> DeclaredProperties {
			get {
				var f = ExtraFields;
				if (f.__declaredProperties_DONT_USE != null)
					return f.__declaredProperties_DONT_USE;
				lock (LockObject) {
					if (f.__declaredProperties_DONT_USE != null)
						return f.__declaredProperties_DONT_USE;
					var res = CreateDeclaredProperties(this);
					f.__declaredProperties_DONT_USE = ReadOnlyCollectionHelpers.Create(res);
					return f.__declaredProperties_DONT_USE;
				}
			}
		}

		internal ReadOnlyCollection<DmdEventInfo> DeclaredEvents {
			get {
				var f = ExtraFields;
				if (f.__declaredEvents_DONT_USE != null)
					return f.__declaredEvents_DONT_USE;
				lock (LockObject) {
					if (f.__declaredEvents_DONT_USE != null)
						return f.__declaredEvents_DONT_USE;
					var res = CreateDeclaredEvents(this);
					f.__declaredEvents_DONT_USE = ReadOnlyCollectionHelpers.Create(res);
					return f.__declaredEvents_DONT_USE;
				}
			}
		}

		internal ReadOnlyCollection<DmdType> NestedTypes {
			get {
				var f = ExtraFields;
				if (f.__nestedTypes_DONT_USE != null)
					return f.__nestedTypes_DONT_USE;
				lock (LockObject) {
					if (f.__nestedTypes_DONT_USE != null)
						return f.__nestedTypes_DONT_USE;
					var res = CreateNestedTypes();
					f.__nestedTypes_DONT_USE = ReadOnlyCollectionHelpers.Create(res);
					return f.__nestedTypes_DONT_USE;
				}
			}
		}

		internal ReadOnlyCollection<DmdType> DeclaredInterfaces {
			get {
				var f = ExtraFields;
				if (f.__declaredInterfaces_DONT_USE != null)
					return f.__declaredInterfaces_DONT_USE;
				lock (LockObject) {
					if (f.__declaredInterfaces_DONT_USE != null)
						return f.__declaredInterfaces_DONT_USE;
					var res = ReadDeclaredInterfaces();
					f.__declaredInterfaces_DONT_USE = ReadOnlyCollectionHelpers.Create(res);
					return f.__declaredInterfaces_DONT_USE;
				}
			}
		}

		ExtraFieldsImpl ExtraFields {
			get {
				if (__extraFields_DONT_USE != null)
					return __extraFields_DONT_USE;
				lock (LockObject) {
					if (__extraFields_DONT_USE != null)
						return __extraFields_DONT_USE;
					__extraFields_DONT_USE = new ExtraFieldsImpl();
					return __extraFields_DONT_USE;
				}
			}
		}
		ExtraFieldsImpl __extraFields_DONT_USE;

		// Most of the fields aren't used so we alloc them when needed
		sealed class ExtraFieldsImpl {
			public ReadOnlyCollection<DmdType> __implementedInterfaces_DONT_USE;
			public ReadOnlyCollection<DmdType> __declaredInterfaces_DONT_USE;

			public ReadOnlyCollection<DmdType> __nestedTypes_DONT_USE;

			public ReadOnlyCollection<DmdFieldInfo>  __declaredFields_DONT_USE;
			public ReadOnlyCollection<DmdMethodBase> __declaredMethods_DONT_USE;
			public ReadOnlyCollection<DmdPropertyInfo> __declaredProperties_DONT_USE;
			public ReadOnlyCollection<DmdEventInfo> __declaredEvents_DONT_USE;

			public DmdMemberReader<DmdFieldInfo> __baseFields_DONT_USE;
			public DmdMemberReader<DmdMethodBase> __baseMethods_DONT_USE;
			public DmdMemberReader<DmdPropertyInfo> __baseProperties_DONT_USE;
			public DmdMemberReader<DmdEventInfo> __baseEvents_DONT_USE;

			public ReadOnlyCollection<DmdCustomAttributeData> __customAttributes_DONT_USE;
		}

		sealed class DmdMemberReader<T> where T : DmdMemberInfo {
			const int MAX_BASE_TYPES = 100;
			readonly Func<DmdTypeBase, T[]> createDeclaredMembers;
			IList<T> members;
			DmdTypeBase currentType;
			int baseTypeCounter;

			public IList<T> CurrentMembers => members;

			public DmdMemberReader(DmdTypeBase owner, Func<DmdTypeBase, T[]> createDeclaredMembers) {
				this.createDeclaredMembers = createDeclaredMembers ?? throw new ArgumentNullException(nameof(createDeclaredMembers));
				members = new List<T>();
				currentType = owner ?? throw new ArgumentNullException(nameof(owner));
				baseTypeCounter = 0;
			}

			public bool AddMembersFromNextBaseType() {
				for (;;) {
					// Could be bad MD with an infinite loop
					if (baseTypeCounter >= MAX_BASE_TYPES)
						break;
					if ((object)currentType == null)
						break;
					// We need a resolved type to get the members. Resolve it now and don't let the TypeRef
					// resolve it later.
					currentType = (DmdTypeBase)currentType.BaseType?.Resolve();
					if ((object)currentType == null)
						break;
					baseTypeCounter++;

					var newMembers = createDeclaredMembers(currentType);
					if (newMembers == null || newMembers.Length == 0)
						continue;

					foreach (var member in newMembers)
						members.Add(member);
					return true;
				}
				if (members is List<T> list)
					members = list.ToArray();
				return false;
			}
		}

		DmdMemberReader<DmdFieldInfo> BaseFieldsReader {
			get {
				var f = ExtraFields;
				if (f.__baseFields_DONT_USE != null)
					return f.__baseFields_DONT_USE;
				lock (LockObject) {
					if (f.__baseFields_DONT_USE != null)
						return f.__baseFields_DONT_USE;
					f.__baseFields_DONT_USE = new DmdMemberReader<DmdFieldInfo>(this, baseType => baseType.CreateDeclaredFields(this));
					return f.__baseFields_DONT_USE;
				}
			}
		}

		DmdMemberReader<DmdMethodBase> BaseMethodsReader {
			get {
				var f = ExtraFields;
				if (f.__baseMethods_DONT_USE != null)
					return f.__baseMethods_DONT_USE;
				lock (LockObject) {
					if (f.__baseMethods_DONT_USE != null)
						return f.__baseMethods_DONT_USE;
					f.__baseMethods_DONT_USE = new DmdMemberReader<DmdMethodBase>(this, baseType => baseType.CreateDeclaredMethods(this, includeConstructors: false));
					return f.__baseMethods_DONT_USE;
				}
			}
		}

		DmdMemberReader<DmdPropertyInfo> BasePropertiesReader {
			get {
				var f = ExtraFields;
				if (f.__baseProperties_DONT_USE != null)
					return f.__baseProperties_DONT_USE;
				lock (LockObject) {
					if (f.__baseProperties_DONT_USE != null)
						return f.__baseProperties_DONT_USE;
					f.__baseProperties_DONT_USE = new DmdMemberReader<DmdPropertyInfo>(this, baseType => baseType.CreateDeclaredProperties(this));
					return f.__baseProperties_DONT_USE;
				}
			}
		}

		DmdMemberReader<DmdEventInfo> BaseEventsReader {
			get {
				var f = ExtraFields;
				if (f.__baseEvents_DONT_USE != null)
					return f.__baseEvents_DONT_USE;
				lock (LockObject) {
					if (f.__baseEvents_DONT_USE != null)
						return f.__baseEvents_DONT_USE;
					f.__baseEvents_DONT_USE = new DmdMemberReader<DmdEventInfo>(this, baseType => baseType.CreateDeclaredEvents(this));
					return f.__baseEvents_DONT_USE;
				}
			}
		}

		IEnumerable<DmdFieldInfo> GetFields(bool inherit) {
			foreach (var field in DeclaredFields)
				yield return field;

			if (inherit) {
				var reader = BaseFieldsReader;
				int index = 0;
				for (;;) {
					DmdFieldInfo field;
					lock (LockObject) {
						if (index >= reader.CurrentMembers.Count && !reader.AddMembersFromNextBaseType())
							break;
						field = reader.CurrentMembers[index++];
					}
					yield return field;
				}
			}
		}

		IEnumerable<DmdConstructorInfo> GetDeclaredConstructors() {
			foreach (var methodBase in DeclaredMethods) {
				if (methodBase.MemberType == DmdMemberTypes.Constructor)
					yield return (DmdConstructorInfo)methodBase;
			}
		}

		IEnumerable<DmdMethodInfo> GetMethods(bool inherit) {
			foreach (var methodBase in DeclaredMethods) {
				if (methodBase.MemberType == DmdMemberTypes.Method)
					yield return (DmdMethodInfo)methodBase;
			}

			if (inherit) {
				var reader = BaseMethodsReader;
				int index = 0;
				for (;;) {
					DmdMethodInfo method = null;
					lock (LockObject) {
						for (;;) {
							if (index >= reader.CurrentMembers.Count && !reader.AddMembersFromNextBaseType())
								break;
							var methodBase = reader.CurrentMembers[index++];
							if (methodBase.MemberType != DmdMemberTypes.Method)
								continue;
							method = (DmdMethodInfo)methodBase;
							break;
						}
					}
					if (method == null)
						break;
					yield return method;
				}
			}
		}

		IEnumerable<DmdPropertyInfo> GetProperties(bool inherit) {
			foreach (var property in DeclaredProperties)
				yield return property;

			if (inherit) {
				var reader = BasePropertiesReader;
				int index = 0;
				for (;;) {
					DmdPropertyInfo property;
					lock (LockObject) {
						if (index >= reader.CurrentMembers.Count && !reader.AddMembersFromNextBaseType())
							break;
						property = reader.CurrentMembers[index++];
					}
					yield return property;
				}
			}
		}

		IEnumerable<DmdEventInfo> GetEvents(bool inherit) {
			foreach (var @event in DeclaredEvents)
				yield return @event;

			if (inherit) {
				var reader = BaseEventsReader;
				int index = 0;
				for (;;) {
					DmdEventInfo @event;
					lock (LockObject) {
						if (index >= reader.CurrentMembers.Count && !reader.AddMembersFromNextBaseType())
							break;
						@event = reader.CurrentMembers[index++];
					}
					yield return @event;
				}
			}
		}
	}
}
