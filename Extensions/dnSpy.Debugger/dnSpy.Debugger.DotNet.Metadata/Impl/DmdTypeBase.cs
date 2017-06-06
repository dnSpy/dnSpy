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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdTypeBase : DmdType {
		protected static readonly ReadOnlyCollection<DmdType> emptyReadOnlyCollection = new ReadOnlyCollection<DmdType>(Array.Empty<DmdType>());
		static readonly ReadOnlyCollection<DmdFieldInfo> emptyFieldCollection = new ReadOnlyCollection<DmdFieldInfo>(Array.Empty<DmdFieldInfo>());
		static readonly ReadOnlyCollection<DmdMethodBase> emptyMethodBaseCollection = new ReadOnlyCollection<DmdMethodBase>(Array.Empty<DmdMethodBase>());
		static readonly ReadOnlyCollection<DmdPropertyInfo> emptyPropertyCollection = new ReadOnlyCollection<DmdPropertyInfo>(Array.Empty<DmdPropertyInfo>());
		static readonly ReadOnlyCollection<DmdEventInfo> emptyEventCollection = new ReadOnlyCollection<DmdEventInfo>(Array.Empty<DmdEventInfo>());

		public override DmdMethodBase DeclaringMethod => null;
		public sealed override Guid GUID => throw new NotImplementedException();//TODO:
		public sealed override DmdAssembly Assembly => Module.Assembly;
		public sealed override string FullName => throw new NotImplementedException();//TODO:
		public sealed override string AssemblyQualifiedName => throw new NotImplementedException();//TODO:
		public sealed override bool IsCOMObject => throw new NotImplementedException();//TODO:
		public sealed override bool HasElementType => (object)GetElementType() != null;
		public override DmdGenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException();
		public override bool IsGenericType => false;
		public override bool IsGenericTypeDefinition => false;
		public override int GenericParameterPosition => throw new InvalidOperationException();
		public override ReadOnlyCollection<DmdType> GetReadOnlyGenericArguments() => emptyReadOnlyCollection;
		public override DmdType GetGenericTypeDefinition() => throw new InvalidOperationException();
		public override ReadOnlyCollection<DmdType> GetReadOnlyGenericParameterConstraints() => throw new InvalidOperationException();
		public override DmdMethodSignature GetFunctionPointerMethodSignature() => throw new InvalidOperationException();
		public override DmdType GetElementType() => null;
		public override int GetArrayRank() => throw new ArgumentException();
		public override ReadOnlyCollection<int> GetReadOnlyArraySizes() => throw new ArgumentException();
		public override ReadOnlyCollection<int> GetReadOnlyArrayLowerBounds() => throw new ArgumentException();
		public sealed override DmdType MakePointerType() => throw new NotImplementedException();//TODO:
		public sealed override DmdType MakeByRefType() => throw new NotImplementedException();//TODO:
		public sealed override DmdType MakeArrayType() => throw new NotImplementedException();//TODO:
		public sealed override DmdType MakeArrayType(int rank, IList<int> sizes, IList<int> lowerBounds) => throw new NotImplementedException();//TODO:
		public sealed override DmdType MakeGenericType(IList<DmdType> typeArguments) => throw new NotImplementedException();//TODO:
		public sealed override DmdConstructorInfo GetConstructor(DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types, IList<DmdParameterModifier> modifiers) => throw new NotImplementedException();//TODO:
		public sealed override DmdConstructorInfo[] GetConstructors(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdMethodInfo GetMethod(string name, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types, IList<DmdParameterModifier> modifiers) => throw new NotImplementedException();//TODO:
		public sealed override DmdMethodInfo[] GetMethods(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdFieldInfo GetField(string name, DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdFieldInfo[] GetFields(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdType GetInterface(string name, bool ignoreCase) => throw new NotImplementedException();//TODO:
		public sealed override ReadOnlyCollection<DmdType> GetReadOnlyInterfaces() => throw new NotImplementedException();//TODO:
		public sealed override DmdEventInfo GetEvent(string name, DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdEventInfo[] GetEvents(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdPropertyInfo GetProperty(string name, DmdBindingFlags bindingAttr, DmdType returnType, IList<DmdType> types, IList<DmdParameterModifier> modifiers) => throw new NotImplementedException();//TODO:
		public sealed override DmdPropertyInfo[] GetProperties(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdType[] GetNestedTypes(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdType GetNestedType(string name, DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdMemberInfo[] GetMember(string name, DmdMemberTypes type, DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdMemberInfo[] GetMembers(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdMemberInfo[] GetDefaultMembers() => throw new NotImplementedException();//TODO:
		public sealed override string[] GetEnumNames() => throw new NotImplementedException();//TODO:
		public sealed override IList<DmdCustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();//TODO:
		public sealed override ReadOnlyCollection<DmdCustomModifier> GetCustomModifiers() => throw new NotImplementedException();//TODO:
		public sealed override bool IsDefined(string attributeTypeFullName, bool inherit) => throw new NotImplementedException();//TODO:
		public sealed override bool IsDefined(DmdType attributeType, bool inherit) => throw new NotImplementedException();//TODO:
		public sealed override string ToString() => throw new NotImplementedException();//TODO:

		protected virtual DmdFieldInfo[] CreateDeclaredFields(DmdType reflectedType) => null;
		protected virtual DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType) => null;
		protected virtual DmdPropertyInfo[] CreateDeclaredProperties(DmdType reflectedType) => null;
		protected virtual DmdEventInfo[] CreateDeclaredEvents(DmdType reflectedType) => null;

		public ReadOnlyCollection<DmdFieldInfo> DeclaredFields {
			get {
				var f = ExtraFields;
				if (f.__declaredFields_DONT_USE != null)
					return f.__declaredFields_DONT_USE;
				lock (LockObject) {
					if (f.__declaredFields_DONT_USE != null)
						return f.__declaredFields_DONT_USE;
					var res = CreateDeclaredFields(this);
					f.__declaredFields_DONT_USE = res == null || res.Length == 0 ? emptyFieldCollection : new ReadOnlyCollection<DmdFieldInfo>(res);
					return f.__declaredFields_DONT_USE;
				}
			}
		}

		public ReadOnlyCollection<DmdMethodBase> DeclaredMethods {
			get {
				var f = ExtraFields;
				if (f.__declaredMethods_DONT_USE != null)
					return f.__declaredMethods_DONT_USE;
				lock (LockObject) {
					if (f.__declaredMethods_DONT_USE != null)
						return f.__declaredMethods_DONT_USE;
					var res = CreateDeclaredMethods(this);
					f.__declaredMethods_DONT_USE = res == null || res.Length == 0 ? emptyMethodBaseCollection : new ReadOnlyCollection<DmdMethodBase>(res);
					return f.__declaredMethods_DONT_USE;
				}
			}
		}

		public ReadOnlyCollection<DmdPropertyInfo> DeclaredProperties {
			get {
				var f = ExtraFields;
				if (f.__declaredProperties_DONT_USE != null)
					return f.__declaredProperties_DONT_USE;
				lock (LockObject) {
					if (f.__declaredProperties_DONT_USE != null)
						return f.__declaredProperties_DONT_USE;
					var res = CreateDeclaredProperties(this);
					f.__declaredProperties_DONT_USE = res == null || res.Length == 0 ? emptyPropertyCollection : new ReadOnlyCollection<DmdPropertyInfo>(res);
					return f.__declaredProperties_DONT_USE;
				}
			}
		}

		public ReadOnlyCollection<DmdEventInfo> DeclaredEvents {
			get {
				var f = ExtraFields;
				if (f.__declaredEvents_DONT_USE != null)
					return f.__declaredEvents_DONT_USE;
				lock (LockObject) {
					if (f.__declaredEvents_DONT_USE != null)
						return f.__declaredEvents_DONT_USE;
					var res = CreateDeclaredEvents(this);
					f.__declaredEvents_DONT_USE = res == null || res.Length == 0 ? emptyEventCollection : new ReadOnlyCollection<DmdEventInfo>(res);
					return f.__declaredEvents_DONT_USE;
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
			public ReadOnlyCollection<DmdFieldInfo>  __declaredFields_DONT_USE;
			public ReadOnlyCollection<DmdMethodBase> __declaredMethods_DONT_USE;
			public ReadOnlyCollection<DmdPropertyInfo> __declaredProperties_DONT_USE;
			public ReadOnlyCollection<DmdEventInfo> __declaredEvents_DONT_USE;

			public DmdMemberReader<DmdFieldInfo> __baseFields_DONT_USE;
			public DmdMemberReader<DmdMethodBase> __baseMethods_DONT_USE;
			public DmdMemberReader<DmdPropertyInfo> __baseProperties_DONT_USE;
			public DmdMemberReader<DmdEventInfo> __baseEvents_DONT_USE;
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
					if (currentType == null)
						break;
					// We need a resolved type to get the members. Resolve it now and don't let the TypeRef
					// resolve it later.
					currentType = (DmdTypeBase)currentType.BaseType?.Resolve();
					if (currentType == null)
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
					f.__baseMethods_DONT_USE = new DmdMemberReader<DmdMethodBase>(this, baseType => baseType.CreateDeclaredMethods(this));
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

		IEnumerable<DmdConstructorInfo> GetConstructors(bool inherit) {
			foreach (var methodBase in DeclaredMethods) {
				if (methodBase.MemberType == DmdMemberTypes.Constructor)
					yield return (DmdConstructorInfo)methodBase;
			}

			if (inherit) {
				var reader = BaseMethodsReader;
				int index = 0;
				for (;;) {
					DmdConstructorInfo ctor = null;
					lock (LockObject) {
						for (;;) {
							if (index >= reader.CurrentMembers.Count && !reader.AddMembersFromNextBaseType())
								break;
							var methodBase = reader.CurrentMembers[index++];
							if (methodBase.MemberType != DmdMemberTypes.Constructor)
								continue;
							ctor = (DmdConstructorInfo)methodBase;
							break;
						}
					}
					if (ctor == null)
						break;
					yield return ctor;
				}
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
