// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

using Debugger.Interop.CorDebug;
using Debugger.Interop.MetaData;
using ICSharpCode.NRefactory.Ast;
using Mono.Cecil.Signatures;

namespace Debugger.MetaData
{
	/// <summary>
	/// Represents a type in a debugee. That is, a class, array, value type or a primitive type.
	/// This class mimics the <see cref="System.Type"/> class.
	/// </summary>
	/// <remarks>
	/// If two types are identical, the references to DebugType will also be identical 
	/// Type will be loaded once per each appdomain.
	/// </remarks>
	[Debugger.Tests.IgnoreOnException]
	public class DebugType: System.Type, IDebugMemberInfo
	{
		public const BindingFlags BindingFlagsAll = BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
		public const BindingFlags BindingFlagsAllDeclared = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
		public const BindingFlags BindingFlagsAllInScope = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
		
		Module module;
		ICorDebugType corType;
		CorElementType corElementType;
		Type primitiveType;
		TypeDefProps classProps;
		string ns;
		string name;
		string fullName;
		string fullNameWithoutGenericArguments;
		DebugType declaringType;
		DebugType elementType;
		List<DebugType> genericArguments = new List<DebugType>();
		List<DebugType> interfaces = new List<DebugType>();
		
		// Members of the type; empty if not applicable
		Dictionary<string, List<MemberInfo>> membersByName = new Dictionary<string, List<MemberInfo>>();
		Dictionary<int, MemberInfo> membersByToken = new Dictionary<int, MemberInfo>();
		
		internal ICorDebugType CorType {
			get { return corType; }
		}
		
		/// <inheritdoc/>
		public override Type DeclaringType {
			get { return declaringType; }
		}
		
		[Debugger.Tests.Ignore]
		public IEnumerable<DebugType> GetSelfAndDeclaringTypes()
		{
			DebugType type = this;
			while(type != null) {
				yield return type;
				type = (DebugType)type.DeclaringType;
			}
		}
		
		/// <summary> The AppDomain in which this type is loaded </summary>
		public AppDomain AppDomain {
			get { return module.AppDomain; }
		}
		
		/// <summary> The Process in which this type is loaded </summary>
		public Process Process {
			get { return module.Process; }
		}
		
		/// <summary> The Module in which this type is loaded </summary>
		public Debugger.Module DebugModule {
			get { return module; }
		}
		
		/// <inheritdoc/>
		public override int MetadataToken {
			get { return (int)classProps.Token; }
		}
		
		/// <inheritdoc/>
		public override System.Reflection.Module Module {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override string Name {
			get { return name; }
		}
		
		/// <inheritdoc/>
		public override Type ReflectedType {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override object[] GetCustomAttributes(bool inherit)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		public override object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		public override bool IsDefined(Type attributeType, bool inherit)
		{
			return IsDefined(this, inherit, attributeType);
		}
		
		public static bool IsDefined(IDebugMemberInfo member, bool inherit, params Type[] attributeTypes)
		{
			if (inherit)
				throw new NotSupportedException("inherit");
			
			MetaDataImport metaData = member.DebugModule.MetaData;
			uint token = (uint)member.MetadataToken;
			foreach(CustomAttributeProps ca in metaData.EnumCustomAttributeProps(token, 0)) {
				CorTokenType tkType = (CorTokenType)(ca.Type & 0xFF000000);
				string attributeName;
				if (tkType == CorTokenType.MemberRef) {
					MemberRefProps constructorMethod = metaData.GetMemberRefProps(ca.Type);
					attributeName = metaData.GetTypeRefProps(constructorMethod.DeclaringType).Name;
				} else if (tkType == CorTokenType.MethodDef) {
					MethodProps constructorMethod = metaData.GetMethodProps(ca.Type);
					attributeName = metaData.GetTypeDefProps(constructorMethod.ClassToken).Name;
				} else {
					throw new DebuggerException("Not expected: " + tkType);
				}
				foreach(Type attributeType in attributeTypes) {
					if (attributeName == attributeType.FullName)
						return true;
				}
			}
			return false;
		}
		
		/// <inheritdoc/>
		public override Assembly Assembly {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override string AssemblyQualifiedName {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override Type BaseType {
			get {
				// corType.Base *sometimes* does not work for object and can cause "Value does not fall within the expected range." exception
				if (this.FullName == typeof(object).FullName) {
					return null;
				}
				// corType.Base does not work for arrays
				if (this.IsArray) {
					return DebugType.CreateFromType(this.AppDomain.Mscorlib, typeof(Array));
				}
				// corType.Base does not work for primitive types
//				if (this.IsPrimitive) {
//					return DebugType.CreateFromType(this.AppDomain, typeof(ValueType));
//				}
				if (this.IsPointer || corElementType == CorElementType.VOID) {
					return null;
				}
				ICorDebugType baseType = corType.GetBase();
				if (baseType != null) {
					return CreateFromCorType(this.AppDomain, baseType);
				} else {
					return null;
				}
			}
		}
		
		//		public virtual bool ContainsGenericParameters { get; }
		//		public virtual MethodBase DeclaringMethod { get; }
		
		/// <inheritdoc/>
		public override string FullName {
			get { return fullName; }
		}
		
		[Debugger.Tests.Ignore]
		public string FullNameWithoutGenericArguments {
			get { return fullNameWithoutGenericArguments; }
		}
		
		/// <inheritdoc/>
		public override Guid GUID {
			get { throw new NotSupportedException(); }
		}
		
		//		public virtual GenericParameterAttributes GenericParameterAttributes { get; }
		//		public virtual int GenericParameterPosition { get; }
		//		public virtual bool IsGenericParameter { get; }
		//		public virtual bool IsGenericTypeDefinition { get; }
		
		/// <inheritdoc/>
		public override bool IsGenericType {
			get {
				return this.GetGenericArguments().Length > 0;
			}
		}
		
		/// <inheritdoc/>
		public override string Namespace {
			get { return ns; }
		}
		
		//		public virtual StructLayoutAttribute StructLayoutAttribute { get; }
		
		/// <inheritdoc/>
		public override RuntimeTypeHandle TypeHandle {
			get { throw new NotSupportedException(); }
		}
		
		/// <inheritdoc/>
		public override Type UnderlyingSystemType {
			get { return this; }
		}
		
		/// <inheritdoc/>
		public override int GetArrayRank()
		{
			if (!IsArray) throw new ArgumentException("Type is not array");
			
			return (int)corType.GetRank();
		}
		
		/// <inheritdoc/>
		protected override TypeAttributes GetAttributeFlagsImpl()
		{
			return (TypeAttributes)classProps.Flags;
		}
		
		/// <inheritdoc/>
		protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			if (bindingAttr == BindingFlags.Default)
				bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			MethodInfo ctor = GetMethodImpl(".ctor", bindingAttr, binder, callConvention, types, modifiers);
			if (ctor == null)
				return null;
			return new DebugConstructorInfo((DebugMethodInfo)ctor);
		}
		
		/// <inheritdoc/>
		public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}
		
		//		public virtual MemberInfo[] GetDefaultMembers();
		
		/// <inheritdoc/>
		public override Type GetElementType()
		{
			return elementType;
		}
		
		const BindingFlags SupportedFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy;
		
		/// <summary> Return member with the given token</summary>
		public MemberInfo GetMember(uint token)
		{
			return membersByToken[(int)token];
		}
		
		/// <summary> Return member with the given token</summary>
		public bool TryGetMember(uint token, out MemberInfo memberInfo)
		{
			return membersByToken.TryGetValue((int)token, out memberInfo);
		}
		
		public T GetMember<T>(string name, BindingFlags bindingFlags, Predicate<T> filter) where T:MemberInfo
		{
			T[] res = GetMembers<T>(name, bindingFlags, filter);
			if (res.Length > 0) {
				return res[0];
			} else {
				return null;
			}
		}
		
		/// <remarks>
		/// Note that at the moment the function will return two methods for interface implementations:
		/// the acutual implementation and the method in the interface
		/// </remarks>
		public T[] GetMembers<T>(string name, BindingFlags bindingFlags, Predicate<T> filter) where T:MemberInfo
		{
			BindingFlags unsupported = bindingFlags & ~SupportedFlags;
			if (unsupported != 0)
				throw new NotSupportedException("BindingFlags: " + unsupported);
			
			if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == 0)
				throw new ArgumentException("Public or NonPublic flag must be included", "bindingFlags");
			
			if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == 0)
				throw new ArgumentException("Instance or Static flag must be included", "bindingFlags");
			
			// Filter by name
			IEnumerable<List<MemberInfo>> searchScope;
			if (name != null) {
				if (membersByName.ContainsKey(name)) {
					searchScope = new List<MemberInfo>[] { membersByName[name] };
				} else {
					searchScope = new List<MemberInfo>[] { };
				}
			} else {
				searchScope = membersByName.Values;
			}
			
			List<T> results = new List<T>();
			foreach(List<MemberInfo> memberInfos in searchScope) {
				foreach(MemberInfo memberInfo in memberInfos) {
					// Filter by type
					if (!(memberInfo is T)) continue; // Reject item
					
					// Filter by access
					if (((IDebugMemberInfo)memberInfo).IsPublic) {
						if ((bindingFlags & BindingFlags.Public) == 0) continue; // Reject item
					} else {
						if ((bindingFlags & BindingFlags.NonPublic) == 0) continue; // Reject item
					}
					
					// Filter by static / instance
					if (((IDebugMemberInfo)memberInfo).IsStatic) {
						if ((bindingFlags & BindingFlags.Static) == 0) continue; // Reject item
					} else {
						if ((bindingFlags & BindingFlags.Instance) == 0) continue; // Reject item
					}
					
					// Filter using predicate
					if (filter != null && !filter((T)memberInfo)) continue; // Reject item
					
					results.Add((T)memberInfo);
				}
			}
			
			if ((bindingFlags & BindingFlags.DeclaredOnly) == 0) {
				// Query supertype
				if (this.BaseType != null) {
					if ((bindingFlags & BindingFlags.FlattenHierarchy) == 0) {
						// Do not include static types
						bindingFlags = bindingFlags & ~BindingFlags.Static;
					}
					// Any flags left?
					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) != 0) {
						T[] superResults = ((DebugType)this.BaseType).GetMembers<T>(name, bindingFlags, filter);
						results.AddRange(superResults);
					}
				}
				// Query interfaces - needed to get inherited methods of an interface
				if (this.IsInterface) {
					foreach (DebugType inter in this.GetInterfaces()) {
						// GetInterfaces will return all interfaces - no need to recurse
						bindingFlags |= BindingFlags.DeclaredOnly;
						T[] interResults = inter.GetMembers<T>(name, bindingFlags, filter);
						results.AddRange(interResults);
					}
				}
			}
			
			return results.ToArray();
		}
		
		MemberInfo SelectOverload(MemberInfo[] candidates, Type[] argumentTypes)
		{
			if (candidates.Length == 0)
				return null;
			if (candidates.Length == 1) {
				if (argumentTypes == null)
					return candidates[0];
				ParameterInfo[] pars = ((IOverloadable)candidates[0]).GetParameters();
				if (pars.Length != argumentTypes.Length)
					throw new GetValueException("Incorrect parameter count");
				for(int i = 0; i < pars.Length; i++) {
					ParameterInfo par = pars[i];
					if (!((DebugType)argumentTypes[i]).CanImplicitelyConvertTo(par.ParameterType))
						throw new GetValueException("Incorrect parameter type for '{0}'. Excpeted {1}, seen {2}", par.Name, par.ParameterType.FullName, argumentTypes[i]);
				}
				return candidates[0];
			}
			
			List<MemberInfo> applicable = new List<MemberInfo>();
			foreach(MemberInfo candidate in candidates) {
				bool isExactMatch;
				if (IsApplicable(((IOverloadable)candidate).GetParameters(), argumentTypes, out isExactMatch))
					applicable.Add(candidate);
				if (isExactMatch)
					return candidate;
			}
			if (applicable.Count == 0) {
				throw new GetValueException("No applicable overload found");
			} else if (applicable.Count == 1) {
				return applicable[0];
			} else {
				// Remove base class definitions
				IntPtr sig = ((IOverloadable)applicable[0]).GetSignarture();
				for(int i = 1; i < applicable.Count;) {
					if (sig == ((IOverloadable)applicable[i]).GetSignarture()) {
						applicable.RemoveAt(i);
					} else {
						i++;
					}
				}
				if (applicable.Count == 1)
					return applicable[0];
				StringBuilder overloads = new StringBuilder();
				foreach(MemberInfo app in applicable) {
					overloads.Append(Environment.NewLine);
					overloads.Append("  ");
					overloads.Append(app.ToString());
				}
				throw new GetValueException("More then one applicable overload found:" + overloads.ToString());
			}
		}
		
		bool IsApplicable(ParameterInfo[] parameters, Type[] argumentTypes, out bool isExactMatch)
		{
			isExactMatch = false;
			if (argumentTypes == null)
				return true;
			if (argumentTypes.Length != parameters.Length)
				return false;
			isExactMatch = true;
			for(int i = 0; i < parameters.Length; i++) {
				if (argumentTypes[i] != parameters[i].ParameterType) {
					isExactMatch = false;
					if (!((DebugType)argumentTypes[i]).CanImplicitelyConvertTo(parameters[i].ParameterType))
						return false;
				}
			}
			return true;
		}
		
		static string Byte    = typeof(byte).FullName;
		static string Short   = typeof(short).FullName;
		static string Int     = typeof(int).FullName;
		static string Long    = typeof(long).FullName;
		static string SByte   = typeof(sbyte).FullName;
		static string UShort  = typeof(ushort).FullName;
		static string UInt    = typeof(uint).FullName;
		static string ULong   = typeof(ulong).FullName;
		static string Float   = typeof(float).FullName;
		static string Double  = typeof(double).FullName;
		static string Char    = typeof(char).FullName;
		static string Decimal = typeof(decimal).FullName;
		
		public bool CanImplicitelyConvertTo(Type toType)
		{
			if (this == toType)
				return true;
			if (this.IsPrimitive && toType.IsPrimitive) {
				string f = this.FullName;
				string t = toType.FullName;
				if (f == t)
					return true;
				if (f == SByte && (t == Short || t == Int || t == Long || t == Float || t == Double || t == Decimal))
					return true;
				if (f == Byte && (t == Short || t == UShort || t == Int || t == UInt || t == Long || t == ULong || t == Float || t == Double || t == Decimal))
					return true;
				if (f == Short && (t == Int || t == Long || t == Float || t == Double || t == Decimal))
					return true;
				if (f == UShort && (t == Int || t == UInt || t == Long || t == ULong || t == Float || t == Double || t == Decimal))
					return true;
				if (f == Int && (t == Long || t == Float || t == Double || t == Decimal))
					return true;
				if (f == UInt && (t == Long || t == ULong || t == Float || t == Double || t == Decimal))
					return true;
				if ((f == Long || f == ULong) && (t == Float || t == Double || t == Decimal))
					return true;
				if (f == Char && (t == UShort || t == Int || t == UInt || t == Long || t == ULong || t == Float || t == Double || t == Decimal))
					return true;
				if (f == Float && t == Double)
					return true;
				return false;
			} else {
				return toType.IsAssignableFrom(this);
			}
		}
		
		/// <inheritdoc/>
		public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}
		
		//		public virtual EventInfo[] GetEvents();
		
		/// <inheritdoc/>
		public override EventInfo[] GetEvents(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		public override FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			return GetMember<FieldInfo>(name, bindingAttr, null);
		}
		
		/// <inheritdoc/>
		public override FieldInfo[] GetFields(BindingFlags bindingAttr)
		{
			return GetMembers<FieldInfo>(null, bindingAttr, null);
		}
		
		/// <inheritdoc/>
		public override Type[] GetGenericArguments()
		{
			return genericArguments.ToArray();
		}
		
		internal ICorDebugType[] GenericArgumentsAsCorDebugType {
			get {
				List<ICorDebugType> types = new List<ICorDebugType>();
				foreach(DebugType arg in GetGenericArguments()) {
					types.Add(arg.CorType);
				}
				return types.ToArray();
			}
		}
		
		//		public virtual Type[] GetGenericParameterConstraints();
		//		public virtual Type GetGenericTypeDefinition();
		
		/// <inheritdoc/>
		public override Type GetInterface(string name, bool ignoreCase)
		{
			foreach(DebugType inter in this.GetInterfaces()) {
				if (string.Equals(inter.FullName, name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
					return inter;
				if (string.Equals(inter.FullNameWithoutGenericArguments, name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
					return inter;
			}
			if (BaseType != null) {
				return BaseType.GetInterface(fullName);
			} else {
				return null;
			}
		}
		
		//		public virtual InterfaceMapping GetInterfaceMap(Type interfaceType);
		
		/// <inheritdoc/>
		/// <returns> All interfaces implemented by the type </returns>
		public override Type[] GetInterfaces()
		{
			return this.interfaces.ToArray();
		}
		
		/// <inheritdoc/>
		public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
		{
			return GetMembers<MemberInfo>(name, bindingAttr, delegate(MemberInfo info) { return (info.MemberType & type) != 0; });
		}
		
		/// <inheritdoc/>
		public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
		{
			return GetMembers<MemberInfo>(null, bindingAttr, null);
		}
		
		/// <summary> Return method with the given token</summary>
		public MethodInfo GetMethod(uint token)
		{
			return (MethodInfo)membersByToken[(int)token];
		}
		
		/// <summary> Return method overload with given parameter names </summary>
		/// <returns> Null if not found </returns>
		public MethodInfo GetMethod(string name, string[] paramNames)
		{
			foreach(DebugMethodInfo candidate in GetMembers<DebugMethodInfo>(name, BindingFlagsAll, null)) {
				if (candidate.ParameterCount == paramNames.Length) {
					bool match = true;
					for(int i = 0; i < paramNames.Length; i++) {
						if (paramNames[i] != candidate.GetParameters()[i].Name)
							match = false;
					}
					if (match)
						return candidate;
				}
			}
			return null;
		}
		
		/// <inheritdoc/>
		protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] paramTypes, ParameterModifier[] modifiers)
		{
			if (binder != null)
				throw new NotSupportedException("binder");
			if (callConvention != CallingConventions.Any)
				throw new NotSupportedException("callConvention");
			if (modifiers != null)
				throw new NotSupportedException("modifiers");
			
			MethodInfo[] candidates = GetMethods(name, bindingAttr);
			return (MethodInfo)SelectOverload(candidates, paramTypes);
		}
		
		public MethodInfo[] GetMethods(string name, BindingFlags bindingAttr)
		{
			return GetMembers<MethodInfo>(name, bindingAttr, null);
		}
		
		/// <inheritdoc/>
		public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
		{
			return GetMembers<MethodInfo>(null, bindingAttr, null);
		}
		
		/// <inheritdoc/>
		public override Type GetNestedType(string name, BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		public override Type[] GetNestedTypes(BindingFlags bindingAttr)
		{
			throw new NotSupportedException();
		}
		
		public MemberInfo[] GetFieldsAndNonIndexedProperties(BindingFlags bindingAttr)
		{
			return GetMembers<MemberInfo>(null, bindingAttr, IsFieldOrNonIndexedProperty);
		}
		
		public static bool IsFieldOrNonIndexedProperty(MemberInfo info)
		{
			if (info is FieldInfo)
				return true;
			if (info is PropertyInfo) {
				return ((PropertyInfo)info).GetGetMethod(true) != null &&
				       ((PropertyInfo)info).GetGetMethod(true).GetParameters().Length == 0;
			}
			return false;
		}
		
		public PropertyInfo[] GetProperties(string name, BindingFlags bindingAttr)
		{
			return GetMembers<PropertyInfo>(name, bindingAttr, null);
		}
		
		/// <inheritdoc/>
		public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
		{
			return GetMembers<PropertyInfo>(null, bindingAttr, null);
		}
		
		/// <inheritdoc/>
		protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] paramTypes, ParameterModifier[] modifiers)
		{
			if (binder != null)
				throw new NotSupportedException("binder");
			if (returnType != null)
				throw new NotSupportedException("returnType");
			if (modifiers != null)
				throw new NotSupportedException("modifiers");
			
			PropertyInfo[] candidates = GetProperties(name, bindingAttr);
			return (PropertyInfo)SelectOverload(candidates, paramTypes);
		}
		
		/// <inheritdoc/>
		protected override bool HasElementTypeImpl()
		{
			return elementType != null;
		}
		
		/// <inheritdoc/>
		public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		protected override bool IsArrayImpl()
		{
			return corElementType == CorElementType.ARRAY ||
			       corElementType == CorElementType.SZARRAY;
		}
		
		/// <inheritdoc/>
		protected override bool IsByRefImpl()
		{
			return corElementType == CorElementType.BYREF;
		}
		
		/// <inheritdoc/>
		protected override bool IsPointerImpl()
		{
			return corElementType == CorElementType.PTR;
		}
		
//		public bool IsClass {
//			get {
//				return !this.IsInterface && !this.IsSubclassOf(valueType);
//			}
//		}
//		
//		public bool IsInterface {
//			get {
//				return ((this.GetAttributeFlagsImpl() & TypeAttributes.Interface) != 0);
//			}
//		}
		
		/// <inheritdoc/>
		protected override bool IsValueTypeImpl()
		{
			// ValueType and Enum are exceptions and are threated as classes
			return this.FullName != typeof(ValueType).FullName &&
			       this.FullName != typeof(Enum).FullName &&
			       this.IsSubclassOf(DebugType.CreateFromType(this.AppDomain.Mscorlib, typeof(ValueType)));
		}
		
		/// <inheritdoc/>
		public override bool IsSubclassOf(Type superType)
		{
			if (!(superType is DebugType)) {
				superType = CreateFromType(this.AppDomain, superType);
			}
			return base.IsSubclassOf(superType);
		}
		
		/// <inheritdoc/>
		protected override bool IsCOMObjectImpl()
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		public override bool IsInstanceOfType(object o)
		{
			if (o == null) return false;
			if (!(o is Value)) return false;
			return this.IsAssignableFrom(((Value)o).Type);
		}
		
		/// <inheritdoc/>
		public override bool IsAssignableFrom(Type c)
		{
			if (this == c) return true;
			if (this.IsInterface) {
				foreach(Type intf in c.GetInterfaces()) {
					if (this == intf)
						return true;
				}
				return false;
			} else {
				return c.IsSubclassOf(this);
			}
		}
		
		//		protected virtual bool IsContextfulImpl();
		//		protected virtual bool IsMarshalByRefImpl();
		
		/// <summary> Returns simple managed type coresponding to the primitive type. </summary>
		[Debugger.Tests.Ignore]
		public System.Type PrimitiveType {
			get { return primitiveType; }
		}
		
		/// <inheritdoc/>
		protected override bool IsPrimitiveImpl()
		{
			return this.PrimitiveType != null;
		}
		
		/// <summary> Gets a value indicating whether the type is an integer type </summary>
		public bool IsInteger {
			get {
				switch (this.FullName) {
					case "System.SByte":
					case "System.Byte":
					case "System.Int16":
					case "System.UInt16":
					case "System.Int32":
					case "System.UInt32":
					case "System.Int64":
					case "System.UInt64": return true;
					default: return false;
				}
			}
		}
		
		public bool IsCompilerGenerated {
			get {
				return IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);
			}
		}
		
		public bool IsDisplayClass {
			get {
				return this.Name.StartsWith("<>") && this.Name.Contains("__DisplayClass");
			}
		}
		
		public bool IsYieldEnumerator {
			get {
				if (this.IsCompilerGenerated) {
					return GetInterface(typeof(System.Collections.IEnumerator).FullName) != null;
				}
				return false;
			}
		}
		
		bool IDebugMemberInfo.IsAssembly {
			get { return false; }
		}
		
		bool IDebugMemberInfo.IsFamily {
			get { return false; }
		}
		
		bool IDebugMemberInfo.IsPrivate {
			get { return this.IsNotPublic; }
		}
		
		bool IDebugMemberInfo.IsStatic {
			get { return false; }
		}
		
		public static DebugType CreateFromTypeDefOrRef(Module module, bool? valueType, uint token, DebugType[] genericArguments)
		{
			CorTokenType tkType = (CorTokenType)(token & 0xFF000000);
			if (tkType == CorTokenType.TypeDef) {
				ICorDebugClass corClass = module.CorModule.GetClassFromToken(token);
				return CreateFromCorClass(module.AppDomain, valueType, corClass, genericArguments);
			} else if (tkType == CorTokenType.TypeRef) {
				TypeRefProps refProps = module.MetaData.GetTypeRefProps(token);
				string fullName = refProps.Name;
				CorTokenType scopeType = (CorTokenType)(refProps.ResolutionScope & 0xFF000000);
				DebugType enclosingType = null;
				if (scopeType == CorTokenType.TypeDef || scopeType == CorTokenType.TypeRef) {
					// Resolve the enclosing TypeRef in this scope
					enclosingType = CreateFromTypeDefOrRef(module, null, refProps.ResolutionScope, genericArguments);
				}
				return CreateFromName(module.AppDomain, fullName, enclosingType, genericArguments);
			} else {
				throw new DebuggerException("TypeDef or TypeRef expected.  Seen " + tkType);
			}
		}
		
		public static DebugType CreateFromType(Module module, System.Type type)
		{
			if (type is DebugType)
				throw new DebuggerException("You already have DebugType, no need to create it.");
			if (type.GetGenericArguments().Length > 0)
				throw new DebuggerException("Generic arguments not allowed in this overload");
			
			if (module.LoadedDebugTypes.ContainsKey(type.FullName))
				return module.LoadedDebugTypes[type.FullName];
			
			DebugType declaringType = null;
			if (type.DeclaringType != null)
				declaringType = CreateFromType(module, type.DeclaringType);
			
			return CreateFromName(module, type.FullName, declaringType);
		}
		
		public static DebugType CreateFromType(AppDomain appDomain, System.Type type, params DebugType[] genericArgumentsOverride)
		{
			if (type is DebugType)
				throw new DebuggerException("You already have DebugType, no need to create it.");
			
			// Get generic arguments for the type if they are not explicitely defined
			if (genericArgumentsOverride == null) {
				List<DebugType> genArgs = new List<DebugType>();
				foreach(System.Type arg in type.GetGenericArguments()) {
					genArgs.Add(CreateFromType(appDomain, arg, null /* implicit */));
				}
				genericArgumentsOverride = genArgs.ToArray();
			}
			
			string name;
			DebugType declaringType;
			if (type.DeclaringType != null) {
				name = type.Name;
				declaringType = CreateFromType(appDomain, type.DeclaringType, genericArgumentsOverride);
			} else {
				name = string.IsNullOrEmpty(type.Namespace) ? type.Name : type.Namespace + "." + type.Name;
				declaringType = null;
			}
			
			return CreateFromName(appDomain, name, declaringType, genericArgumentsOverride);
		}
		
		public static DebugType CreateFromName(AppDomain appDomain, string name, DebugType declaringType, params DebugType[] genericArguments)
		{
			DebugType type = CreateFromNameOrNull(appDomain, name, declaringType, genericArguments);
			if (type == null)
				throw new DebuggerException("Type not found: " + name + (declaringType != null ? " (declaring type = " + declaringType.FullName + ")" : string.Empty));
			return type;
		}
		
		public static DebugType CreateFromNameOrNull(AppDomain appDomain, string name, DebugType declaringType, params DebugType[] genericArguments)
		{
			if (declaringType != null)
				return CreateFromNameOrNull(declaringType.DebugModule, name, declaringType, genericArguments);
			foreach(Module module in appDomain.Process.Modules) {
				if (module.AppDomain != appDomain) continue;
				DebugType result = CreateFromNameOrNull(module, name, declaringType, genericArguments);
				if (result != null)
					return result;
			}
			return null;
		}
		
		public static DebugType CreateFromName(Module module, string name, DebugType declaringType, params DebugType[] genericArguments)
		{
			DebugType type = CreateFromNameOrNull(module, name, declaringType, genericArguments);
			if (type == null)
				throw new DebuggerException("Type not found: " + name + (declaringType != null ? " (declaring type = " + declaringType.FullName + ")" : string.Empty));
			return type;
		}
		
		public static DebugType CreateFromNameOrNull(Module module, string name, DebugType declaringType, params DebugType[] genericArguments)
		{
			if (declaringType != null && declaringType.DebugModule != module)
				throw new DebuggerException("Declaring type must be in the same module");
			
			uint token;
			try {
				token = module.MetaData.FindTypeDefPropsByName(name, declaringType == null ? 0 : (uint)declaringType.MetadataToken).Token;
			} catch {
				return null;
			}
			return CreateFromTypeDefOrRef(module, null, token, genericArguments);
		}
		
		public static DebugType CreateFromTypeSpec(Module module, uint token, DebugType declaringType)
		{
			CorTokenType tokenType = (CorTokenType)(token & 0xFF000000);
			if (tokenType != CorTokenType.TypeSpec) {
				throw new DebuggerException("TypeSpec expected.  Seen " + tokenType);
			}
			
			byte[] typeSpecBlob = module.MetaData.GetTypeSpecFromToken(token).GetData();
			return CreateFromSignature(module, typeSpecBlob, declaringType);
		}
		
		public static DebugType CreateFromSignature(Module module, byte[] signature, DebugType declaringType)
		{
			SignatureReader sigReader = new SignatureReader(signature);
			int start;
			SigType sigType = sigReader.ReadType(signature, 0, out start);
			return CreateFromSignature(module, sigType, declaringType);
		}
		
		internal static DebugType CreateFromSignature(Module module, SigType sigType, DebugType declaringType)
		{
			System.Type sysType = CorElementTypeToManagedType((CorElementType)(uint)sigType.ElementType);
			if (sysType != null)
				return CreateFromType(module.AppDomain.Mscorlib, sysType);
			
			if (sigType is CLASS) {
				return CreateFromTypeDefOrRef(module, false, ((CLASS)sigType).Type.ToUInt(), null);
			}
			
			if (sigType is VALUETYPE) {
				return CreateFromTypeDefOrRef(module, true, ((VALUETYPE)sigType).Type.ToUInt(), null);
			}
			
			// Numbered generic reference
			if (sigType is VAR) {
				if (declaringType == null) throw new DebuggerException("declaringType is needed");
				return (DebugType)declaringType.GetGenericArguments()[((VAR)sigType).Index];
			}
			
			// Numbered generic reference
			if (sigType is MVAR) {
				return module.AppDomain.ObjectType;
			}
			
			if (sigType is GENERICINST) {
				GENERICINST genInst = (GENERICINST)sigType;
				
				List<DebugType> genArgs = new List<DebugType>(genInst.Signature.Arity);
				foreach(GenericArg genArgSig in genInst.Signature.Types) {
					genArgs.Add(CreateFromSignature(module, genArgSig.Type, declaringType));
				}
				
				return CreateFromTypeDefOrRef(module, genInst.ValueType, genInst.Type.ToUInt(), genArgs.ToArray());
			}
			
			if (sigType is ARRAY) {
				ARRAY arraySig = (ARRAY)sigType;
				DebugType elementType = CreateFromSignature(module, arraySig.Type, declaringType);
				return (DebugType)elementType.MakeArrayType(arraySig.Shape.Rank);
			}
			
			if (sigType is SZARRAY) {
				SZARRAY arraySig = (SZARRAY)sigType;
				DebugType elementType = CreateFromSignature(module, arraySig.Type, declaringType);
				return (DebugType)elementType.MakeArrayType();
			}
			
			if (sigType is PTR) {
				PTR ptrSig = (PTR)sigType;
				DebugType elementType;
				if (ptrSig.Void) {
					elementType = DebugType.CreateFromType(module.AppDomain.Mscorlib, typeof(void));
				} else {
					elementType = CreateFromSignature(module, ptrSig.PtrType, declaringType);
				}
				return (DebugType)elementType.MakePointerType();
			}
			
			if (sigType is FNPTR) {
				// TODO: FNPTR
			}
			
			throw new NotImplementedException(sigType.ElementType.ToString());
		}
		
		// public virtual Type MakeGenericType(params Type[] typeArguments);
		
		/// <inheritdoc/>
		public override Type MakeArrayType(int rank)
		{
			ICorDebugType res = this.AppDomain.CorAppDomain2.GetArrayOrPointerType((uint)CorElementType.ARRAY, (uint)rank, this.CorType);
			return CreateFromCorType(this.AppDomain, res);
		}
		
		/// <inheritdoc/>
		public override Type MakeArrayType()
		{
			ICorDebugType res = this.AppDomain.CorAppDomain2.GetArrayOrPointerType((uint)CorElementType.SZARRAY, 1, this.CorType);
			return CreateFromCorType(this.AppDomain, res);
		}
		
		/// <inheritdoc/>
		public override Type MakePointerType()
		{
			ICorDebugType res = this.AppDomain.CorAppDomain2.GetArrayOrPointerType((uint)CorElementType.PTR, 0, this.CorType);
			return CreateFromCorType(this.AppDomain, res);
		}
		
		/// <inheritdoc/>
		public override Type MakeByRefType()
		{
			ICorDebugType res = this.AppDomain.CorAppDomain2.GetArrayOrPointerType((uint)CorElementType.BYREF, 0, this.CorType);
			return CreateFromCorType(this.AppDomain, res);
		}
		
		public static DebugType CreateFromCorClass(AppDomain appDomain, bool? valueType, ICorDebugClass corClass, DebugType[] genericArguments)
		{
			MetaDataImport metaData = appDomain.Process.Modules[corClass.GetModule()].MetaData;
			
			if (valueType == null) {
				uint superClassToken = metaData.GetTypeDefProps(corClass.GetToken()).SuperClassToken;
				CorTokenType tkType = (CorTokenType)(superClassToken & 0xFF000000);
				if (tkType == CorTokenType.TypeDef) {
					valueType = metaData.GetTypeDefProps(superClassToken).Name == typeof(ValueType).FullName;
				}
				if (tkType == CorTokenType.TypeRef) {
					valueType = metaData.GetTypeRefProps(superClassToken).Name == typeof(ValueType).FullName;
				}
				if (tkType == CorTokenType.TypeSpec) {
					valueType = false; // TODO: Resolve properly
				}
			}
			
			genericArguments = genericArguments ?? new DebugType[] {};
			if (genericArguments.Length < metaData.EnumGenericParams(corClass.GetToken()).Length) {
				throw new DebuggerException("Not enough generic arguments");
			}
			Array.Resize(ref genericArguments, metaData.EnumGenericParams(corClass.GetToken()).Length);
			
			List<ICorDebugType> corGenArgs = new List<ICorDebugType>(genericArguments.Length);
			foreach(DebugType genAgr in genericArguments) {
				corGenArgs.Add(genAgr.CorType);
			}
			
			ICorDebugType corType = ((ICorDebugClass2)corClass).GetParameterizedType((uint)(valueType.Value ? CorElementType.VALUETYPE : CorElementType.CLASS), corGenArgs.ToArray());
			
			return CreateFromCorType(appDomain, corType);
		}
		
		/// <summary> Obtains instance of DebugType. Same types will return identical instance. </summary>
		public static DebugType CreateFromCorType(AppDomain appDomain, ICorDebugType corType)
		{
			if (appDomain.DebugTypeCache.ContainsKey(corType))
				return appDomain.DebugTypeCache[corType];
			
			// Convert short-form to class-form
			Type primitiveType = CorElementTypeToManagedType((CorElementType)(corType.GetTheType()));
			if (primitiveType != null) {
				DebugType type = CreateFromType(appDomain.Mscorlib, primitiveType);
				// Use cache next time
				appDomain.DebugTypeCache[corType] = type;
				return type;
			} else {
				DebugType type = new DebugType(appDomain, corType);
				// Ensure name-identity
				if (type.DebugModule.LoadedDebugTypes.ContainsKey(type.FullName)) {
					type = type.DebugModule.LoadedDebugTypes[type.FullName];
					// corDebug cache needs to be fixed to this type - we do not want the semi-loaded type there
					appDomain.DebugTypeCache[corType] = type;
				} else {
					type.LoadMembers();
					type.DebugModule.LoadedDebugTypes[type.FullName] = type;
				}
				return type;
			}
		}
		
		DebugType(AppDomain appDomain, ICorDebugType corType)
		{
			if (corType == null)
				throw new ArgumentNullException("corType");
			
			this.corType = corType;
			this.corElementType = (CorElementType)corType.GetTheType();
			
			// Loading might access the type again
			appDomain.DebugTypeCache[corType] = this;
			
			if (corElementType == CorElementType.ARRAY ||
			    corElementType == CorElementType.SZARRAY ||
			    corElementType == CorElementType.PTR ||
			    corElementType == CorElementType.BYREF)
			{
				// CorDebugClass for arrays "is not loaded" and can not be used
				this.elementType = CreateFromCorType(appDomain, corType.GetFirstTypeParameter());
				this.module = appDomain.Mscorlib;
				this.classProps = new TypeDefProps();
				// Get names
				string suffix = string.Empty;
				if (corElementType == CorElementType.SZARRAY) suffix = "[]";
				if (corElementType == CorElementType.ARRAY)   suffix = "[" + new String(',', GetArrayRank() - 1) + "]";
				if (corElementType == CorElementType.PTR)     suffix = "*";
				if (corElementType == CorElementType.BYREF)   suffix = "&";
				this.ns = this.GetElementType().Namespace;
				this.name = this.GetElementType().Name + suffix;
				this.fullNameWithoutGenericArguments = ((DebugType)this.GetElementType()).FullNameWithoutGenericArguments + suffix;
				this.fullName = this.GetElementType().FullName + suffix;
			}
			
			if (corElementType == CorElementType.CLASS ||
			    corElementType == CorElementType.VALUETYPE)
			{
				// Get generic arguments
				foreach(ICorDebugType t in corType.EnumerateTypeParameters().GetEnumerator()) {
					genericArguments.Add(DebugType.CreateFromCorType(appDomain, t));
				}
				// Get class props
				this.module = appDomain.Process.Modules[corType.GetClass().GetModule()];
				this.classProps = module.MetaData.GetTypeDefProps(corType.GetClass().GetToken());
				if (this.DebugModule.AppDomain != appDomain)
					throw new DebuggerException("The specified AppDomain was inccorect");
				// Get the enclosing class
				if (!this.IsPublic && !this.IsNotPublic) {
					uint enclosingTk = module.MetaData.GetNestedClassProps((uint)this.MetadataToken).EnclosingClass;
					this.declaringType = DebugType.CreateFromTypeDefOrRef(this.DebugModule, null, enclosingTk, genericArguments.ToArray());
				}
				// Get names (it depends on the previous steps)
				int index = classProps.Name.LastIndexOf('.');
				if (index == -1) {
					this.ns = string.Empty;
					this.name = classProps.Name;
				} else {
					this.ns = classProps.Name.Substring(0, index);
					this.name = classProps.Name.Substring(index + 1);
				}
				LoadFullName();
				this.primitiveType = GetPrimitiveType(this.FullName);
			}
			
			if (module == null)
				throw new DebuggerException("Unexpected: " + corElementType);
		}
		
		internal static Type CorElementTypeToManagedType(CorElementType corElementType)
		{
			switch(corElementType) {
				case CorElementType.BOOLEAN: return typeof(System.Boolean);
				case CorElementType.CHAR:    return typeof(System.Char);
				case CorElementType.I1:      return typeof(System.SByte);
				case CorElementType.U1:      return typeof(System.Byte);
				case CorElementType.I2:      return typeof(System.Int16);
				case CorElementType.U2:      return typeof(System.UInt16);
				case CorElementType.I4:      return typeof(System.Int32);
				case CorElementType.U4:      return typeof(System.UInt32);
				case CorElementType.I8:      return typeof(System.Int64);
				case CorElementType.U8:      return typeof(System.UInt64);
				case CorElementType.R4:      return typeof(System.Single);
				case CorElementType.R8:      return typeof(System.Double);
				case CorElementType.I:       return typeof(System.IntPtr);
				case CorElementType.U:       return typeof(System.UIntPtr);
				case CorElementType.STRING:  return typeof(System.String);
				case CorElementType.OBJECT:  return typeof(System.Object);
				case CorElementType.VOID:    return typeof(void);
				default: return null;
			}
		}
		
		static Type GetPrimitiveType(string fullname)
		{
			switch (fullname) {
				case "System.Boolean": return typeof(System.Boolean);
				case "System.Char":    return typeof(System.Char);
				case "System.SByte":   return typeof(System.SByte);
				case "System.Byte":    return typeof(System.Byte);
				case "System.Int16":   return typeof(System.Int16);
				case "System.UInt16":  return typeof(System.UInt16);
				case "System.Int32":   return typeof(System.Int32);
				case "System.UInt32":  return typeof(System.UInt32);
				case "System.Int64":   return typeof(System.Int64);
				case "System.UInt64":  return typeof(System.UInt64);
				case "System.Single":  return typeof(System.Single);
				case "System.Double":  return typeof(System.Double);
				// String is not primitive type
				default: return null;
			}
		}
		
		void LoadFullName()
		{
			StringBuilder sb = new StringBuilder();
			
			if (declaringType != null) {
				sb.Append(declaringType.FullNameWithoutGenericArguments);
				sb.Append('+');
			}
			
			// '`' might be missing in nested generic classes
			sb.Append(classProps.Name);
			
			this.fullNameWithoutGenericArguments = sb.ToString();
			
			if (this.GetGenericArguments().Length > 0) {
				sb.Append("[");
				bool first = true;
				foreach(DebugType arg in this.GetGenericArguments()) {
					if (!first)
						sb.Append(",");
					first = false;
					sb.Append(arg.FullName);
				}
				sb.Append("]");
			}
			
			this.fullName = sb.ToString();
		}
		
		void LoadMembers()
		{
			System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
			
			if (corElementType == CorElementType.ARRAY || corElementType == CorElementType.SZARRAY) {
				// Arrays are special and normal loading does not work for them
				DebugType iList = DebugType.CreateFromName(this.AppDomain.Mscorlib, typeof(IList<>).FullName, null, new DebugType[] { (DebugType)this.GetElementType() });
				this.interfaces.Add(iList);
				this.interfaces.AddRange(iList.interfaces);
			} else {
				// Load interfaces
				foreach(InterfaceImplProps implProps in module.MetaData.EnumInterfaceImplProps((uint)this.MetadataToken)) {
					CorTokenType tkType = (CorTokenType)(implProps.Interface & 0xFF000000);
					if (tkType == CorTokenType.TypeDef || tkType == CorTokenType.TypeRef) {
						// TODO: Fix properly
						try {
							this.interfaces.Add(DebugType.CreateFromTypeDefOrRef(module, false, implProps.Interface, null));
						} catch (DebuggerException) {
						}
					} else if (tkType == CorTokenType.TypeSpec) {
						this.interfaces.Add(DebugType.CreateFromTypeSpec(module, implProps.Interface, this));
					} else {
						throw new DebuggerException("Uknown token type for interface: " + tkType);
					}
				}
				
				// Load fields
				foreach(FieldProps field in module.MetaData.EnumFieldProps((uint)this.MetadataToken)) {
					DebugFieldInfo fieldInfo = new DebugFieldInfo(this, field);
					AddMember(fieldInfo);
				};
				
				// Load methods
				foreach(MethodProps m in module.MetaData.EnumMethodProps((uint)this.MetadataToken)) {
					AddMember(new DebugMethodInfo(this, m));
				}
				
				// Load properties
				foreach(PropertyProps prop in module.MetaData.EnumPropertyProps((uint)this.MetadataToken)) {
					DebugPropertyInfo propInfo = new DebugPropertyInfo(
						this,
						prop.Name,
						prop.GetterMethod != 0x06000000 ? GetMethod(prop.GetterMethod) : null,
						prop.SetterMethod != 0x06000000 ? GetMethod(prop.SetterMethod) : null
					);
					if (propInfo.GetGetMethod() != null)
						((DebugMethodInfo)propInfo.GetGetMethod()).IsPropertyAccessor = true;
					if (propInfo.GetSetMethod() != null)
						((DebugMethodInfo)propInfo.GetSetMethod()).IsPropertyAccessor = true;
					AddMember(propInfo);
				}
			}
			
			if (this.Process.Options.Verbose)
				this.Process.TraceMessage("Loaded {0} ({1} ms)", this.FullName, stopwatch.ElapsedMilliseconds);
			
			// Load base type
			Type baseType = this.BaseType;
			
			// Add base type's inerfaces
			if (baseType != null) {
				foreach (DebugType debugType in baseType.GetInterfaces()) {
					if (!this.interfaces.Contains(debugType)) {
						this.interfaces.Add(debugType);
					}
				}
			}
		}
		
		void AddMember(MemberInfo member)
		{
			if (!membersByName.ContainsKey(member.Name))
				membersByName.Add(member.Name, new List<MemberInfo>(1));
			membersByName[member.Name].Add(member);
			membersByToken[member.MetadataToken] = member;
		}
		
		public override bool Equals(object o)
		{
			DebugType other = o as DebugType;
			if (other == null)
				return false;
			return this.MetadataToken == other.MetadataToken && // Performance optimization
			       this.DebugModule == other.DebugModule &&
			       this.FullName == other.FullName;
		}
		
		public override int GetHashCode()
		{
			return this.FullName.GetHashCode();
		}
		
		public static bool operator == (DebugType a, DebugType b)
		{
			if ((object)a == (object)b)
				return true;
			if (((object)a == null) || ((object)b == null))
				return false;
			return a.Equals(b);
		}
		
		public static bool operator != (DebugType a, DebugType b)
		{
			return !(a == b);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return this.FullName;
		}
		
		DebugType IDebugMemberInfo.MemberType {
			get { return null; }
		}
	}
}
