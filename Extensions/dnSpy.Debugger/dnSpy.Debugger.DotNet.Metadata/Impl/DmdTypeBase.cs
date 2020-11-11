/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdTypeBase : DmdType {
		sealed private protected override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();

		readonly ReadOnlyCollection<DmdCustomModifier> customModifiers;
		public sealed override ReadOnlyCollection<DmdCustomModifier> GetCustomModifiers() => customModifiers;
		protected DmdTypeBase(IList<DmdCustomModifier>? customModifiers) =>
			this.customModifiers = ReadOnlyCollectionHelpers.Create(customModifiers);
		protected IList<DmdCustomModifier>? VerifyCustomModifiers(IList<DmdCustomModifier>? customModifiers) {
			if (customModifiers is not null) {
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
		public abstract DmdTypeBase? FullResolve();

		public sealed override DmdType? Resolve(bool throwOnError) {
			var res = ResolveNoThrowCore();
			if (res is null && throwOnError)
				throw new TypeResolveException(this);
			return res;
		}
		protected abstract DmdType? ResolveNoThrowCore();

		public override DmdMethodBase? DeclaringMethod => throw new InvalidOperationException();
		public sealed override DmdAssembly Assembly => Module.Assembly;
		public sealed override bool HasElementType => GetElementType() is not null;
		public override DmdGenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException();
		public override bool IsGenericType => false;
		public override bool IsGenericTypeDefinition => false;
		public override int GenericParameterPosition => throw new InvalidOperationException();
		public override DmdType GetGenericTypeDefinition() => throw new InvalidOperationException();
		public override ReadOnlyCollection<DmdType> GetGenericParameterConstraints() => throw new InvalidOperationException();
		public override DmdMethodSignature GetFunctionPointerMethodSignature() => throw new InvalidOperationException();
		public override DmdType? GetElementType() => null;
		public override int GetArrayRank() => throw new ArgumentException();
		public override ReadOnlyCollection<int> GetArraySizes() => throw new ArgumentException();
		public override ReadOnlyCollection<int> GetArrayLowerBounds() => throw new ArgumentException();
		public sealed override DmdType MakePointerType() => AppDomain.MakePointerType(this, null);
		public sealed override DmdType MakeByRefType() => AppDomain.MakeByRefType(this, null);
		public sealed override DmdType MakeArrayType() => AppDomain.MakeArrayType(this, null);
		public sealed override DmdType MakeArrayType(int rank, IList<int> sizes, IList<int> lowerBounds) => AppDomain.MakeArrayType(this, rank, sizes, lowerBounds, null);
		public sealed override DmdType MakeGenericType(IList<DmdType> typeArguments) => AppDomain.MakeGenericType(this, typeArguments, null);

		public sealed override ReadOnlyCollection<DmdType> GetGenericArguments() => SkipElementTypes()?.GetGenericArgumentsCore() ?? ReadOnlyCollectionHelpers.Empty<DmdType>();
		protected virtual ReadOnlyCollection<DmdType> GetGenericArgumentsCore() => ReadOnlyCollectionHelpers.Empty<DmdType>();

		public sealed override string? Namespace {
			get {
				DmdType type = SkipElementTypes();
				while (type.DeclaringType is DmdType declType)
					type = declType;
				var ns = type.MetadataNamespace;
				return ns is null || ns.Length == 0 ? null : ns;
			}
		}

		protected DmdTypeBase SkipElementTypes() {
			var type = this;
			while (type.GetElementType() is DmdTypeBase elementType)
				type = elementType;
			return type;
		}

		public sealed override DmdConstructorInfo? GetConstructor(DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types) {
			if (types is null)
				throw new ArgumentNullException(nameof(types));
			foreach (var ctor in GetDeclaredConstructors()) {
				if (DmdMemberInfoComparer.IsMatch(ctor, bindingAttr, callConvention, types))
					return ctor;
			}
			return null;
		}

		public sealed override DmdConstructorInfo[] GetConstructors(DmdBindingFlags bindingAttr) {
			List<DmdConstructorInfo>? ctors = null;
			foreach (var ctor in GetDeclaredConstructors()) {
				if (DmdMemberInfoComparer.IsMatch(ctor, bindingAttr)) {
					if (ctors is null)
						ctors = new List<DmdConstructorInfo>();
					ctors.Add(ctor);
				}
			}
			return ctors?.ToArray() ?? Array.Empty<DmdConstructorInfo>();
		}

		public sealed override DmdMethodInfo? GetMethod(string name, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType>? types) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			DmdMethodInfo? foundMethod = null;
			int counter = 0;
			foreach (var method in GetMethods(ToGetMemberOptions(bindingAttr))) {
				if (!DmdMemberInfoComparer.IsMatch(method, name, bindingAttr))
					continue;
				if (!DmdMemberInfoComparer.IsMatch(method, bindingAttr, callConvention))
					continue;
				if (types is not null && DmdMemberInfoComparer.IsMatch(method, types))
					return method;
				foundMethod = method;
				counter++;
			}
			if (foundMethod is not null && types is null) {
				if (counter == 1)
					return foundMethod;
				throw new System.Reflection.AmbiguousMatchException();
			}
			return null;
		}

		public sealed override DmdMethodInfo[] GetMethods(DmdBindingFlags bindingAttr) {
			List<DmdMethodInfo>? methods = null;
			foreach (var method in GetMethods(ToGetMemberOptions(bindingAttr))) {
				if (DmdMemberInfoComparer.IsMatch(method, bindingAttr)) {
					if (methods is null)
						methods = new List<DmdMethodInfo>();
					methods.Add(method);
				}
			}
			return methods?.ToArray() ?? Array.Empty<DmdMethodInfo>();
		}

		public sealed override DmdFieldInfo? GetField(string name, DmdBindingFlags bindingAttr) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			foreach (var field in GetFields(ToGetMemberOptions(bindingAttr))) {
				if (DmdMemberInfoComparer.IsMatch(field, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(field, bindingAttr))
					return field;
			}
			return null;
		}

		public sealed override DmdFieldInfo[] GetFields(DmdBindingFlags bindingAttr) {
			List<DmdFieldInfo>? fields = null;
			foreach (var field in GetFields(ToGetMemberOptions(bindingAttr))) {
				if (DmdMemberInfoComparer.IsMatch(field, bindingAttr)) {
					if (fields is null)
						fields = new List<DmdFieldInfo>();
					fields.Add(field);
				}
			}
			return fields?.ToArray() ?? Array.Empty<DmdFieldInfo>();
		}

		public sealed override DmdEventInfo? GetEvent(string name, DmdBindingFlags bindingAttr) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			foreach (var @event in GetEvents(ToGetMemberOptions(bindingAttr))) {
				if (DmdMemberInfoComparer.IsMatch(@event, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(@event, bindingAttr))
					return @event;
			}
			return null;
		}

		public sealed override DmdEventInfo[] GetEvents(DmdBindingFlags bindingAttr) {
			List<DmdEventInfo>? events = null;
			foreach (var @event in GetEvents(ToGetMemberOptions(bindingAttr))) {
				if (DmdMemberInfoComparer.IsMatch(@event, bindingAttr)) {
					if (events is null)
						events = new List<DmdEventInfo>();
					events.Add(@event);
				}
			}
			return events?.ToArray() ?? Array.Empty<DmdEventInfo>();
		}

		public sealed override DmdPropertyInfo? GetProperty(string name, DmdBindingFlags bindingAttr, DmdType? returnType, IList<DmdType>? types) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			DmdPropertyInfo? foundProperty = null;
			int counter = 0;
			foreach (var property in GetProperties(ToGetMemberOptions(bindingAttr))) {
				if (!DmdMemberInfoComparer.IsMatch(property, name, bindingAttr))
					continue;
				if (!DmdMemberInfoComparer.IsMatch(property, bindingAttr))
					continue;
				if (returnType is not null && !DmdMemberInfoComparer.IsMatch(property, returnType))
					continue;
				if (types is not null && !DmdMemberInfoComparer.IsMatch(property, types))
					continue;
				if (returnType is not null && types is not null)
					return property;
				foundProperty = property;
				counter++;
			}
			if (foundProperty is not null && types is null) {
				if (counter == 1)
					return foundProperty;
				if (returnType is null)
					throw new System.Reflection.AmbiguousMatchException();
			}
			return null;
		}

		public sealed override DmdPropertyInfo[] GetProperties(DmdBindingFlags bindingAttr) {
			List<DmdPropertyInfo>? properties = null;
			foreach (var property in GetProperties(ToGetMemberOptions(bindingAttr))) {
				if (DmdMemberInfoComparer.IsMatch(property, bindingAttr)) {
					if (properties is null)
						properties = new List<DmdPropertyInfo>();
					properties.Add(property);
				}
			}
			return properties?.ToArray() ?? Array.Empty<DmdPropertyInfo>();
		}

		public sealed override DmdMemberInfo[] GetMember(string name, DmdMemberTypes type, DmdBindingFlags bindingAttr) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			DmdConstructorInfo[]? ctors = null;
			if ((type & DmdMemberTypes.Constructor) != 0) {
				ctors = GetConstructors(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Constructor)
					return ctors;
			}

			DmdMethodInfo[]? methods = null;
			if ((type & DmdMemberTypes.Method) != 0) {
				methods = GetMethods(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Method)
					return methods;
			}

			DmdFieldInfo[]? fields = null;
			if ((type & DmdMemberTypes.Field) != 0) {
				fields = GetFields(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Field)
					return fields;
			}

			DmdPropertyInfo[]? properties = null;
			if ((type & DmdMemberTypes.Property) != 0) {
				properties = GetProperties(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Property)
					return properties;
			}

			DmdEventInfo[]? events = null;
			if ((type & DmdMemberTypes.Event) != 0) {
				events = GetEvents(bindingAttr).Where(a => DmdMemberInfoComparer.IsMatch(a, name, bindingAttr) && DmdMemberInfoComparer.IsMatch(a, bindingAttr)).ToArray();
				if (type == DmdMemberTypes.Event)
					return events;
			}

			DmdType[]? types = null;
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

		static void Copy(DmdMemberInfo[]? src, DmdMemberInfo[] dst, ref int index) {
			if (src is null)
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
			var nestedTypes = NestedTypes;
			if (nestedTypes.Count == 0)
				return Array.Empty<DmdType>();
			List<DmdType>? list = ObjectPools.AllocListOfType();
			foreach (var type in nestedTypes) {
				if (DmdMemberInfoComparer.IsMatch(type, bindingAttr))
					list.Add(type);
			}
			return ObjectPools.FreeAndToArray(ref list);
		}

		public sealed override DmdType? GetNestedType(string fullName, DmdBindingFlags bindingAttr) {
			if (fullName is null)
				throw new ArgumentNullException(nameof(fullName));
			var nestedTypes = NestedTypes;
			if (nestedTypes.Count == 0)
				return null;
			DmdTypeUtilities.SplitFullName(fullName, out var @namespace, out var name);
			foreach (var type in nestedTypes) {
				if (DmdMemberInfoComparer.IsMatch(type, bindingAttr) && DmdMemberInfoComparer.IsMatch(type, @namespace, name, bindingAttr))
					return type;
			}
			return null;
		}

		public sealed override DmdType? GetInterface(string fullName, bool ignoreCase) {
			if (fullName is null)
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
			if (f.__implementedInterfaces_DONT_USE is not null)
				return f.__implementedInterfaces_DONT_USE;

			var implIfaces = CreateInterfaces(this);
			Interlocked.CompareExchange(ref f.__implementedInterfaces_DONT_USE, ReadOnlyCollectionHelpers.Create(implIfaces), null);
			return f.__implementedInterfaces_DONT_USE!;
		}
		public abstract DmdType[]? ReadDeclaredInterfaces();

		static DmdType[] CreateInterfaces(DmdTypeBase type) {
			List<DmdType>? list = ObjectPools.AllocListOfType();
			HashSet<DmdType>? hash = ObjectPools.AllocHashSetOfType();
			Stack<IEnumerator<DmdType>>? stack = ObjectPools.AllocStackOfIEnumeratorOfType();

			IEnumerator<DmdType>? tmpEnum = null;
			try {
				for (DmdTypeBase? t = type; ;) {
					if (!hash.Add(t))
						break;
					stack.Push(tmpEnum = t.DeclaredInterfaces.GetEnumerator());
					tmpEnum = null;
					t = (DmdTypeBase?)t.BaseType;
					if (t is null)
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
			var res = ObjectPools.FreeAndToArray(ref list);
			ObjectPools.Free(ref hash);
			ObjectPools.Free(ref stack);
			return res;
		}

		public sealed override DmdMemberInfo[] GetDefaultMembers() {
			var name = GetDefaultMemberName(this);
			if (name is null)
				return Array.Empty<DmdMemberInfo>();
			return GetMember(name);
		}

		static string? GetDefaultMemberName(DmdType type) {
			DmdType? currentType = type;
			var defaultMemberAttribute = currentType.AppDomain.GetWellKnownType(DmdWellKnownType.System_Reflection_DefaultMemberAttribute, isOptional: true);
			if (defaultMemberAttribute is null)
				return null;
			for (int i = 0; i < 100; i++) {
				foreach (var ca in currentType.GetCustomAttributesData()) {
					if (ca.AttributeType == defaultMemberAttribute)
						return ca.ConstructorArguments.Count == 1 ? ca.ConstructorArguments[0].Value as string : null;
				}
				currentType = currentType.BaseType;
				if (currentType is null)
					break;
			}
			return null;
		}

		public sealed override string[] GetEnumNames() {
			if (!IsEnum)
				throw new ArgumentException();
			// This isn't the same order as reflection but it's not important. The CLR sorts it
			// by value (ulong comparison), see coreclr: ReflectionEnum::GetEnumValuesAndNames
			return DeclaredFields.Where(a => a.IsStatic).Select(a => a.Name).ToArray();
		}

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() {
			var f = ExtraFields;
			if (f.__customAttributes_DONT_USE is null)
				InitializeCustomAttributes(f);
			return f.__customAttributes_DONT_USE!;
		}

		void InitializeCustomAttributes(ExtraFieldsImpl f) {
			if (f.__customAttributes_DONT_USE is not null)
				return;
			var info = CreateCustomAttributes();
			var newSAs = ReadOnlyCollectionHelpers.Create(info.sas);
			var newCAs = CustomAttributesHelper.AddPseudoCustomAttributes(this, info.cas, newSAs);
			lock (LockObject) {
				if (f.__customAttributes_DONT_USE is null) {
					f.__securityAttributes_DONT_USE = newSAs;
					f.__customAttributes_DONT_USE = newCAs;
				}
			}
		}
		public virtual (DmdCustomAttributeData[]? cas, DmdCustomAttributeData[]? sas) CreateCustomAttributes() => (null, null);

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetSecurityAttributesData() {
			var f = ExtraFields;
			if (f.__customAttributes_DONT_USE is null)
				InitializeCustomAttributes(f);
			return f.__securityAttributes_DONT_USE!;
		}

		public virtual DmdFieldInfo[]? CreateDeclaredFields(DmdType reflectedType) => null;
		public virtual DmdMethodBase[]? CreateDeclaredMethods(DmdType reflectedType) => null;
		public virtual DmdPropertyInfo[]? CreateDeclaredProperties(DmdType reflectedType) => null;
		public virtual DmdEventInfo[]? CreateDeclaredEvents(DmdType reflectedType) => null;

		public sealed override IEnumerable<DmdFieldInfo> Fields => GetFields(GetMemberOptions.Inherit | GetMemberOptions.Inaccessible);
		public sealed override IEnumerable<DmdMethodBase> Methods => GetMethodsAndConstructors(GetMemberOptions.Inherit | GetMemberOptions.Inaccessible);
		public sealed override IEnumerable<DmdPropertyInfo> Properties => GetProperties(GetMemberOptions.Inherit | GetMemberOptions.Inaccessible);
		public sealed override IEnumerable<DmdEventInfo> Events => GetEvents(GetMemberOptions.Inherit | GetMemberOptions.Inaccessible);

		public sealed override ReadOnlyCollection<DmdFieldInfo> DeclaredFields {
			get {
				var f = ExtraFields;
				if (f.__declaredFields_DONT_USE is not null)
					return f.__declaredFields_DONT_USE;
				var res = CreateDeclaredFields(this);
				Interlocked.CompareExchange(ref f.__declaredFields_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
				return f.__declaredFields_DONT_USE!;
			}
		}

		public sealed override ReadOnlyCollection<DmdMethodBase> DeclaredMethods {
			get {
				var f = ExtraFields;
				if (f.__declaredMethods_DONT_USE is not null)
					return f.__declaredMethods_DONT_USE;
				var res = CreateDeclaredMethods(this);
				Interlocked.CompareExchange(ref f.__declaredMethods_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
				return f.__declaredMethods_DONT_USE!;
			}
		}

		public sealed override ReadOnlyCollection<DmdPropertyInfo> DeclaredProperties {
			get {
				var f = ExtraFields;
				if (f.__declaredProperties_DONT_USE is not null)
					return f.__declaredProperties_DONT_USE;
				var res = CreateDeclaredProperties(this);
				Interlocked.CompareExchange(ref f.__declaredProperties_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
				return f.__declaredProperties_DONT_USE!;
			}
		}

		public sealed override ReadOnlyCollection<DmdEventInfo> DeclaredEvents {
			get {
				var f = ExtraFields;
				if (f.__declaredEvents_DONT_USE is not null)
					return f.__declaredEvents_DONT_USE;
				var res = CreateDeclaredEvents(this);
				Interlocked.CompareExchange(ref f.__declaredEvents_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
				return f.__declaredEvents_DONT_USE!;
			}
		}

		protected virtual DmdType[]? CreateNestedTypes() => null;
		protected ReadOnlyCollection<DmdType> NestedTypesCore {
			get {
				// We loop here because the field could be cleared if it's a dynamic type
				for (;;) {
					var f = ExtraFields;
					var nestedTypes = f.__nestedTypes_DONT_USE;
					if (nestedTypes is not null)
						return nestedTypes;
					var res = CreateNestedTypes();
					Interlocked.CompareExchange(ref f.__nestedTypes_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
				}
			}
		}

		/// <summary>
		/// Invalidates all cached nested types.
		/// 
		/// Used by dynamic modules when the debugger sends a LoadClass event.
		/// </summary>
		internal void DynamicType_InvalidateCachedNestedTypes() {
			Debug.Assert(Module.IsDynamic);
			var f = __extraFields_DONT_USE;
			if (f is null)
				return;
			f.__nestedTypes_DONT_USE = null;
		}

		/// <summary>
		/// Invalidates all cached collections of types and members.
		/// 
		/// Used by dynamic modules when the debugger sends a LoadClass event.
		/// </summary>
		internal void DynamicType_InvalidateCachedMembers() {
			Debug.Assert(Module.IsDynamic);
			__extraFields_DONT_USE = null;
		}

		internal ReadOnlyCollection<DmdType> DeclaredInterfaces {
			get {
				var f = ExtraFields;
				if (f.__declaredInterfaces_DONT_USE is not null)
					return f.__declaredInterfaces_DONT_USE;
				var res = ReadDeclaredInterfaces();
				Interlocked.CompareExchange(ref f.__declaredInterfaces_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
				return f.__declaredInterfaces_DONT_USE!;
			}
		}

		ExtraFieldsImpl ExtraFields {
			get {
				// We loop here because the field could be cleared if it's a dynamic type
				for (;;) {
					var f = __extraFields_DONT_USE;
					if (f is not null)
						return f;
					Interlocked.CompareExchange(ref __extraFields_DONT_USE, new ExtraFieldsImpl(), null);
				}
			}
		}
		volatile ExtraFieldsImpl? __extraFields_DONT_USE;

		// Most of the fields aren't used so we alloc them when needed
		sealed class ExtraFieldsImpl {
			public volatile ReadOnlyCollection<DmdType>? __implementedInterfaces_DONT_USE;
			public volatile ReadOnlyCollection<DmdType>? __declaredInterfaces_DONT_USE;

			public volatile ReadOnlyCollection<DmdType>? __nestedTypes_DONT_USE;

			public volatile ReadOnlyCollection<DmdFieldInfo>? __declaredFields_DONT_USE;
			public volatile ReadOnlyCollection<DmdMethodBase>? __declaredMethods_DONT_USE;
			public volatile ReadOnlyCollection<DmdPropertyInfo>? __declaredProperties_DONT_USE;
			public volatile ReadOnlyCollection<DmdEventInfo>? __declaredEvents_DONT_USE;

			public volatile DmdFieldReader? __baseFields_DONT_USE;
			public volatile DmdMethodReader? __baseMethods_DONT_USE;
			public volatile DmdPropertyReader? __baseProperties_DONT_USE;
			public volatile DmdEventReader? __baseEvents_DONT_USE;

			public volatile ReadOnlyCollection<DmdCustomAttributeData>? __customAttributes_DONT_USE;
			public volatile ReadOnlyCollection<DmdCustomAttributeData>? __securityAttributes_DONT_USE;
		}

		static bool IsVtblGap(DmdMethodBase method) => method.IsRTSpecialName && method.Name.StartsWith("_VtblGap");
		internal void InitializeParentDefinitions() => BaseMethodsReader.InitializeAll();

		abstract class DmdMemberReader<T> where T : DmdMemberInfo {
			const int MAX_BASE_TYPES = 100;
			readonly object lockObj;
			readonly DmdTypeBase ownerType;
			IList<MemberInfo<T>> members;
			DmdTypeBase? currentType;
			int baseTypeCounter;

			protected DmdMemberReader(DmdTypeBase ownerType) {
				lockObj = new object();
				members = new List<MemberInfo<T>>();
				this.ownerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
				currentType = ownerType;
				baseTypeCounter = 0;
			}

			protected abstract T[]? CreatedDeclaredMembers(DmdTypeBase ownerType, DmdTypeBase baseType);
			protected abstract MemberInfoKind GetMemberInfoKind(T member);
			protected abstract void OnCompleted();

			public MemberInfo<T> GetNext(ref int index) {
				MemberInfo<T> member;
				lock (lockObj) {
					if (index >= members.Count && !AddMembersFromNextBaseType())
						return default;
					member = members[index++];
				}
				return member;
			}

			protected void InitializeAllCore() {
				lock (lockObj) {
					int index = members.Count;
					while (GetNext(ref index).Member is not null)
						index = members.Count;
				}
			}

			bool AddMembersFromNextBaseType() {
				for (;;) {
					// Could be bad MD with an infinite loop
					if (baseTypeCounter >= MAX_BASE_TYPES)
						break;
					if (currentType is null)
						break;
					// We need a resolved type to get the members. Resolve it now and don't let the TypeRef
					// resolve it later.
					currentType = (DmdTypeBase?)currentType.BaseType?.Resolve();
					if (currentType is null)
						break;
					baseTypeCounter++;

					int membersCount = members.Count;
					var createdMembers = CreatedDeclaredMembers(ownerType, currentType) ?? Array.Empty<T>();
					foreach (var member in createdMembers)
						members.Add(new MemberInfo<T>(member, GetMemberInfoKind(member)));
					if (membersCount != members.Count)
						return true;
				}
				if (members is List<MemberInfo<T>> list) {
					members = list.ToArray();
					OnCompleted();
				}
				return false;
			}
		}

		[Flags]
		enum MemberInfoKind : byte {
			/// <summary>
			/// Normal visible member
			/// </summary>
			Visible,

			/// <summary>
			/// Overridden by a derived class
			/// </summary>
			Overridden,

			/// <summary>
			/// Method is private in base class and thus not accessible by the current type, so it's hidden
			/// </summary>
			Inaccessible,

			/// <summary>
			/// Constructor in base class
			/// </summary>
			BaseConstructor,

			/// <summary>
			/// VtblGap method
			/// </summary>
			VtblGap,
		}

		readonly struct MemberInfo<TMember> {
			public readonly TMember Member;
			public readonly MemberInfoKind Kind;
			public MemberInfo(TMember member, MemberInfoKind kind) {
				Member = member;
				Kind = kind;
			}
		}

		sealed class DmdFieldReader : DmdMemberReader<DmdFieldInfo> {
			public DmdFieldReader(DmdTypeBase owner) : base(owner) { }
			protected override DmdFieldInfo[]? CreatedDeclaredMembers(DmdTypeBase ownerType, DmdTypeBase baseType) => baseType.CreateDeclaredFields(ownerType);
			protected override MemberInfoKind GetMemberInfoKind(DmdFieldInfo field) => MemberInfoKind.Visible;
			protected override void OnCompleted() { }
		}

		sealed class DmdMethodReader : DmdMemberReader<DmdMethodBase> {
			sealed class EqualityComparer : IEqualityComparer<Key> {
				public static readonly EqualityComparer Instance = new EqualityComparer();
				EqualityComparer() { }
				public bool Equals([AllowNull] Key x, [AllowNull] Key y) => x.Equals(y);
				public int GetHashCode([DisallowNull] Key obj) => obj.GetHashCode();
			}
			readonly struct Key : IEquatable<Key> {
				public string Name { get; }
				public DmdMethodSignature Signature { get; }
				public Key(string name, DmdMethodSignature signature) {
					Name = name;
					Signature = signature;
				}
				public bool Equals(Key other) => Name == other.Name && DmdMemberInfoEqualityComparer.DefaultMember.Equals(Signature, other.Signature);
				public override bool Equals(object? obj) => obj is Key other && Equals(other);
				public override int GetHashCode() => (Name?.GetHashCode() ?? 0) ^ DmdMemberInfoEqualityComparer.DefaultMember.GetHashCode(Signature);
				public override string ToString() => Name + ": " + Signature?.ToString();
			}

			bool HasInitializedAllMethods => overriddenHash is null;

			Dictionary<Key, DmdMethodDef> overriddenHash;
			public DmdMethodReader(DmdTypeBase owner) : base(owner) {
				overriddenHash = new Dictionary<Key, DmdMethodDef>(EqualityComparer.Instance);
				foreach (var method in owner.DeclaredMethods) {
					if (IsVtblGap(method)) {
						// Nothing
					}
					else if ((method.Attributes & (DmdMethodAttributes.Virtual | DmdMethodAttributes.Abstract)) != 0 && !method.IsNewSlot) {
						var key = new Key(method.Name, method.GetMethodSignature());
						overriddenHash[key] = (DmdMethodDef)method;
					}
				}
			}

			public void InitializeAll() {
				if (HasInitializedAllMethods)
					return;
				InitializeAllCore();
			}

			protected override DmdMethodBase[]? CreatedDeclaredMembers(DmdTypeBase ownerType, DmdTypeBase baseType) => baseType.CreateDeclaredMethods(ownerType);
			protected override MemberInfoKind GetMemberInfoKind(DmdMethodBase method) {
				if (method is DmdConstructorInfo)
					return MemberInfoKind.BaseConstructor;
				if (IsVtblGap(method))
					return MemberInfoKind.VtblGap;
				if ((method.Attributes & (DmdMethodAttributes.Virtual | DmdMethodAttributes.Abstract)) != 0) {
					var key = new Key(method.Name, method.GetMethodSignature());
					if (overriddenHash.TryGetValue(key, out var derivedTypeMethod)) {
						var methodDef = (DmdMethodDef)method;
						if (method.IsNewSlot)
							overriddenHash.Remove(key);
						else
							overriddenHash[key] = methodDef;
						derivedTypeMethod.SetParentDefinition(methodDef);
						return MemberInfoKind.Overridden;
					}
					else {
						if (method.IsNewSlot)
							Debug.Assert(!overriddenHash.ContainsKey(key));
						else
							overriddenHash[key] = (DmdMethodDef)method;
						return MemberInfoKind.Visible;
					}
				}
				else
					return MemberInfoKind.Visible;
			}

			protected override void OnCompleted() => overriddenHash = null!;
		}

		sealed class DmdPropertyReader : DmdMemberReader<DmdPropertyInfo> {
			sealed class EqualityComparer : IEqualityComparer<Key> {
				public static readonly EqualityComparer Instance = new EqualityComparer();
				EqualityComparer() { }
				public bool Equals([AllowNull] Key x, [AllowNull] Key y) => x.Equals(y);
				public int GetHashCode([DisallowNull] Key obj) => obj.GetHashCode();
			}
			readonly struct Key : IEquatable<Key> {
				public string Name { get; }
				public DmdMethodSignature Signature { get; }
				public Key(string name, DmdMethodSignature signature) {
					Name = name;
					Signature = signature;
				}
				public bool Equals(Key other) => Name == other.Name && DmdMemberInfoEqualityComparer.DefaultMember.Equals(Signature, other.Signature);
				public override bool Equals(object? obj) => obj is Key other && Equals(other);
				public override int GetHashCode() => (Name?.GetHashCode() ?? 0) ^ DmdMemberInfoEqualityComparer.DefaultMember.GetHashCode(Signature);
				public override string ToString() => Name + ": " + Signature?.ToString();
			}

			HashSet<Key> overriddenHash;
			public DmdPropertyReader(DmdTypeBase owner) : base(owner) {
				overriddenHash = new HashSet<Key>(EqualityComparer.Instance);
				foreach (var property in owner.DeclaredProperties) {
					var key = new Key(property.Name, property.GetMethodSignature());
					overriddenHash.Add(key);
				}
			}

			protected override DmdPropertyInfo[]? CreatedDeclaredMembers(DmdTypeBase ownerType, DmdTypeBase baseType) => baseType.CreateDeclaredProperties(ownerType);
			protected override MemberInfoKind GetMemberInfoKind(DmdPropertyInfo property) {
				if (!IsAccessible(property))
					return MemberInfoKind.Inaccessible;
				var key = new Key(property.Name, property.GetMethodSignature());
				bool hide = !overriddenHash.Add(key);
				return hide ? MemberInfoKind.Overridden : MemberInfoKind.Visible;
			}

			void UpdateIsPrivate(DmdMethodInfo method, ref bool? isPrivate) {
				if (method is null)
					return;
				if (method.IsPrivate && isPrivate is null)
					isPrivate = true;
				else
					isPrivate &= method.IsPrivate;
			}

			bool IsAccessible(DmdPropertyInfo property) {
				bool? isPrivate = null;
				foreach (var method in property.GetAccessors(DmdGetAccessorOptions.All))
					UpdateIsPrivate(method, ref isPrivate);

				bool isDeclaredProperty = (object?)property.DeclaringType == property.ReflectedType;
				return isDeclaredProperty || isPrivate != true;
			}

			protected override void OnCompleted() => overriddenHash = null!;
		}

		sealed class DmdEventReader : DmdMemberReader<DmdEventInfo> {
			HashSet<string> overriddenHash;
			public DmdEventReader(DmdTypeBase owner) : base(owner) {
				overriddenHash = new HashSet<string>(StringComparer.Ordinal);
				foreach (var @event in owner.DeclaredEvents)
					overriddenHash.Add(@event.Name);
			}

			protected override DmdEventInfo[]? CreatedDeclaredMembers(DmdTypeBase ownerType, DmdTypeBase baseType) => baseType.CreateDeclaredEvents(ownerType);
			protected override MemberInfoKind GetMemberInfoKind(DmdEventInfo @event) {
				if (!IsAccessible(@event))
					return MemberInfoKind.Inaccessible;
				bool hide = !overriddenHash.Add(@event.Name);
				return hide ? MemberInfoKind.Overridden : MemberInfoKind.Visible;
			}

			void UpdateIsPrivate(DmdMethodInfo? method, ref bool? isPrivate) {
				if (method is null)
					return;
				if (method.IsPrivate && isPrivate is null)
					isPrivate = true;
				else
					isPrivate &= method.IsPrivate;
			}

			bool IsAccessible(DmdEventInfo @event) {
				var addMethod = @event.GetAddMethod(DmdGetAccessorOptions.All);
				var removeMethod = @event.GetRemoveMethod(DmdGetAccessorOptions.All);
				var raiseMethod = @event.GetRaiseMethod(DmdGetAccessorOptions.All);
				var otherMethods = @event.GetOtherMethods(DmdGetAccessorOptions.All);

				bool? isPrivate = null;
				UpdateIsPrivate(addMethod, ref isPrivate);
				UpdateIsPrivate(removeMethod, ref isPrivate);
				UpdateIsPrivate(raiseMethod, ref isPrivate);
				foreach (var otherMethod in otherMethods)
					UpdateIsPrivate(otherMethod, ref isPrivate);

				bool isDeclaredEvent = (object?)@event.DeclaringType == @event.ReflectedType;
				return isDeclaredEvent || isPrivate != true;
			}

			protected override void OnCompleted() => overriddenHash = null!;
		}

		DmdFieldReader BaseFieldsReader {
			get {
				var f = ExtraFields;
				if (f.__baseFields_DONT_USE is not null)
					return f.__baseFields_DONT_USE;
				Interlocked.CompareExchange(ref f.__baseFields_DONT_USE, new DmdFieldReader(this), null);
				return f.__baseFields_DONT_USE!;
			}
		}

		DmdMethodReader BaseMethodsReader {
			get {
				var f = ExtraFields;
				if (f.__baseMethods_DONT_USE is not null)
					return f.__baseMethods_DONT_USE;
				Interlocked.CompareExchange(ref f.__baseMethods_DONT_USE, new DmdMethodReader(this), null);
				return f.__baseMethods_DONT_USE!;
			}
		}

		DmdPropertyReader BasePropertiesReader {
			get {
				var f = ExtraFields;
				if (f.__baseProperties_DONT_USE is not null)
					return f.__baseProperties_DONT_USE;
				Interlocked.CompareExchange(ref f.__baseProperties_DONT_USE, new DmdPropertyReader(this), null);
				return f.__baseProperties_DONT_USE!;
			}
		}

		DmdEventReader BaseEventsReader {
			get {
				var f = ExtraFields;
				if (f.__baseEvents_DONT_USE is not null)
					return f.__baseEvents_DONT_USE;
				Interlocked.CompareExchange(ref f.__baseEvents_DONT_USE, new DmdEventReader(this), null);
				return f.__baseEvents_DONT_USE!;
			}
		}

		[Flags]
		enum GetMemberOptions {
			None			= 0,
			Inherit			= 0x00000001,
			Inaccessible	= 0x00000002,
			All				= 0x00000004,
		}

		static GetMemberOptions ToGetMemberOptions(DmdBindingFlags bindingAttr) {
			var res = GetMemberOptions.None;
			if ((bindingAttr & DmdBindingFlags.DeclaredOnly) == 0)
				res |= GetMemberOptions.Inherit;
			if ((bindingAttr & DmdBindingFlags.Inaccessible) != 0)
				res |= GetMemberOptions.Inaccessible;
			return res;
		}

		static bool IsMatch(MemberInfoKind kind, GetMemberOptions options) {
			if ((options & GetMemberOptions.All) != 0)
				return true;
			if (kind == MemberInfoKind.Visible)
				return true;
			if (kind == MemberInfoKind.Inaccessible && (options & GetMemberOptions.Inaccessible) != 0)
				return true;
			return false;
		}

		IEnumerable<DmdFieldInfo> GetFields(GetMemberOptions options) {
			foreach (var field in DeclaredFields)
				yield return field;

			if ((options & GetMemberOptions.Inherit) != 0) {
				var reader = BaseFieldsReader;
				int index = 0;
				for (;;) {
					var info = reader.GetNext(ref index);
					if (info.Member is null)
						break;
					if (IsMatch(info.Kind, options))
						yield return info.Member;
				}
			}
		}

		IEnumerable<DmdConstructorInfo> GetDeclaredConstructors() {
			foreach (var methodBase in DeclaredMethods) {
				if (methodBase.MemberType == DmdMemberTypes.Constructor)
					yield return (DmdConstructorInfo)methodBase;
			}
		}

		IEnumerable<DmdMethodBase> GetMethodsAndConstructors(GetMemberOptions options) {
			foreach (var method in DeclaredMethods) {
				// Don't filter out VtblGap methods
				yield return method;
			}

			if ((options & GetMemberOptions.Inherit) != 0) {
				var reader = BaseMethodsReader;
				int index = 0;
				for (;;) {
					var info = reader.GetNext(ref index);
					if (info.Member is null)
						break;
					if (IsMatch(info.Kind, options))
						yield return info.Member;
				}
			}
		}

		IEnumerable<DmdMethodInfo> GetMethods(GetMemberOptions options) {
			foreach (var methodBase in DeclaredMethods) {
				if (IsVtblGap(methodBase))
					continue;
				if (methodBase.MemberType == DmdMemberTypes.Method)
					yield return (DmdMethodInfo)methodBase;
			}

			if ((options & GetMemberOptions.Inherit) != 0) {
				var reader = BaseMethodsReader;
				int index = 0;
				for (;;) {
					var info = reader.GetNext(ref index);
					if (info.Member is null)
						break;
					if (IsMatch(info.Kind, options)) {
						if (info.Member is DmdMethodInfo method)
							yield return method;
					}
				}
			}
		}

		IEnumerable<DmdPropertyInfo> GetProperties(GetMemberOptions options) {
			foreach (var property in DeclaredProperties)
				yield return property;

			if ((options & GetMemberOptions.Inherit) != 0) {
				var reader = BasePropertiesReader;
				int index = 0;
				for (;;) {
					var info = reader.GetNext(ref index);
					if (info.Member is null)
						break;
					if (IsMatch(info.Kind, options))
						yield return info.Member;
				}
			}
		}

		IEnumerable<DmdEventInfo> GetEvents(GetMemberOptions options) {
			foreach (var @event in DeclaredEvents)
				yield return @event;

			if ((options & GetMemberOptions.Inherit) != 0) {
				var reader = BaseEventsReader;
				int index = 0;
				for (;;) {
					var info = reader.GetNext(ref index);
					if (info.Member is null)
						break;
					if (IsMatch(info.Kind, options))
						yield return info.Member;
				}
			}
		}

		public sealed override DmdMethodBase? GetMethod(DmdModule module, int metadataToken, bool throwOnError) {
			foreach (var method in GetMethodsAndConstructors(GetMemberOptions.Inherit | GetMemberOptions.All)) {
				if (method.Module == module && method.MetadataToken == metadataToken)
					return method;
			}

			if (throwOnError)
				throw new MethodNotFoundException("0x" + MetadataToken.ToString("X8") + "(" + module.ToString() + ")");
			return null;
		}

		public sealed override DmdFieldInfo? GetField(DmdModule module, int metadataToken, bool throwOnError) {
			foreach (var field in GetFields(GetMemberOptions.Inherit | GetMemberOptions.All)) {
				if (field.Module == module && field.MetadataToken == metadataToken)
					return field;
			}

			if (throwOnError)
				throw new FieldNotFoundException("0x" + MetadataToken.ToString("X8") + "(" + module.ToString() + ")");
			return null;
		}

		public sealed override DmdPropertyInfo? GetProperty(DmdModule module, int metadataToken, bool throwOnError) {
			foreach (var property in GetProperties(GetMemberOptions.Inherit | GetMemberOptions.All)) {
				if (property.Module == module && property.MetadataToken == metadataToken)
					return property;
			}

			if (throwOnError)
				throw new PropertyNotFoundException("0x" + MetadataToken.ToString("X8") + "(" + module.ToString() + ")");
			return null;
		}

		public sealed override DmdEventInfo? GetEvent(DmdModule module, int metadataToken, bool throwOnError) {
			foreach (var @event in GetEvents(GetMemberOptions.Inherit | GetMemberOptions.All)) {
				if (@event.Module == module && @event.MetadataToken == metadataToken)
					return @event;
			}

			if (throwOnError)
				throw new EventNotFoundException("0x" + MetadataToken.ToString("X8") + "(" + module.ToString() + ")");
			return null;
		}

		public sealed override DmdMethodBase? GetMethod(string name, DmdSignatureCallingConvention flags, int genericParameterCount, DmdType? returnType, IList<DmdType> parameterTypes, bool throwOnError) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if (parameterTypes is null)
				throw new ArgumentNullException(nameof(parameterTypes));
			foreach (var method in GetMethodsAndConstructors(GetMemberOptions.Inherit | GetMemberOptions.Inaccessible)) {
				if (method.Name != name)
					continue;
				var sig = method.GetMethodSignature();
				if (sig.GetVarArgsParameterTypes().Count != 0)
					continue;
				var sigParamTypes = sig.GetParameterTypes();
				if (sigParamTypes.Count != parameterTypes.Count)
					continue;
				if (sig.Flags != flags)
					continue;
				bool ok = true;
				for (int i = 0; i < sigParamTypes.Count; i++) {
					if (!DmdMemberInfoEqualityComparer.DefaultMember.Equals(sigParamTypes[i], parameterTypes[i])) {
						ok = false;
						break;
					}
				}
				if (!ok)
					continue;
				if (sig.GenericParameterCount != genericParameterCount)
					continue;
				if (returnType is not null) {
					if (!DmdMemberInfoEqualityComparer.DefaultMember.Equals(returnType, sig.ReturnType))
						continue;
				}

				return method;
			}

			if (throwOnError)
				throw new MethodNotFoundException(name);
			return null;
		}

		public sealed override DmdFieldInfo? GetField(string name, DmdType fieldType, bool throwOnError) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if (fieldType is null)
				throw new ArgumentNullException(nameof(fieldType));
			foreach (var field in GetFields(GetMemberOptions.Inherit | GetMemberOptions.Inaccessible)) {
				if (field.Name != name)
					continue;
				if (!DmdMemberInfoEqualityComparer.DefaultMember.Equals(field.FieldType, fieldType))
					continue;

				return field;
			}

			if (throwOnError)
				throw new FieldNotFoundException(name);
			return null;
		}

		public sealed override DmdPropertyInfo? GetProperty(string name, DmdSignatureCallingConvention flags, int genericParameterCount, DmdType? returnType, IList<DmdType> parameterTypes, bool throwOnError) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if (parameterTypes is null)
				throw new ArgumentNullException(nameof(parameterTypes));
			foreach (var property in GetProperties(GetMemberOptions.Inherit | GetMemberOptions.Inaccessible)) {
				if (property.Name != name)
					continue;
				var sig = property.GetMethodSignature();
				if (sig.GetVarArgsParameterTypes().Count != 0)
					continue;
				var sigParamTypes = sig.GetParameterTypes();
				if (sigParamTypes.Count != parameterTypes.Count)
					continue;
				if (sig.Flags != flags)
					continue;
				bool ok = true;
				for (int i = 0; i < sigParamTypes.Count; i++) {
					if (!DmdMemberInfoEqualityComparer.DefaultMember.Equals(sigParamTypes[i], parameterTypes[i])) {
						ok = false;
						break;
					}
				}
				if (!ok)
					continue;
				if (sig.GenericParameterCount != genericParameterCount)
					continue;
				if (returnType is not null) {
					if (!DmdMemberInfoEqualityComparer.DefaultMember.Equals(returnType, sig.ReturnType))
						continue;
				}

				return property;
			}

			if (throwOnError)
				throw new PropertyNotFoundException(name);
			return null;
		}

		public sealed override DmdEventInfo? GetEvent(string name, DmdType eventHandlerType, bool throwOnError) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));
			if (eventHandlerType is null)
				throw new ArgumentNullException(nameof(eventHandlerType));
			foreach (var @event in GetEvents(GetMemberOptions.Inherit | GetMemberOptions.Inaccessible)) {
				if (@event.Name != name)
					continue;
				if (!DmdMemberInfoEqualityComparer.DefaultMember.Equals(@event.EventHandlerType, eventHandlerType))
					continue;

				return @event;
			}

			if (throwOnError)
				throw new EventNotFoundException(name);
			return null;
		}
	}
}
