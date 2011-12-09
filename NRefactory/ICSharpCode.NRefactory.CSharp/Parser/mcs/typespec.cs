//
// typespec.cs: Type specification
//
// Authors: Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2010 Novell, Inc
// Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
//

using System;
using System.Collections.Generic;
using System.Text;

#if STATIC
using MetaType = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using MetaType = System.Type;
using System.Reflection;
#endif

namespace Mono.CSharp
{
	public class TypeSpec : MemberSpec
	{
		protected MetaType info;
		protected MemberCache cache;
		protected IList<TypeSpec> ifaces;
		TypeSpec base_type;

		Dictionary<TypeSpec[], InflatedTypeSpec> inflated_instances;

		public static readonly TypeSpec[] EmptyTypes = new TypeSpec[0];

#if !STATIC
		// Reflection Emit hacking
		static readonly Type TypeBuilder;
		static readonly Type GenericTypeBuilder;

		static TypeSpec ()
		{
			var assembly = typeof (object).Assembly;
			TypeBuilder = assembly.GetType ("System.Reflection.Emit.TypeBuilder");
			GenericTypeBuilder = assembly.GetType ("System.Reflection.MonoGenericClass");
			if (GenericTypeBuilder == null)
				GenericTypeBuilder = assembly.GetType ("System.Reflection.Emit.TypeBuilderInstantiation");
		}
#endif

		public TypeSpec (MemberKind kind, TypeSpec declaringType, ITypeDefinition definition, MetaType info, Modifiers modifiers)
			: base (kind, declaringType, definition, modifiers)
		{
			this.declaringType = declaringType;
			this.info = info;

			if (definition != null && definition.TypeParametersCount > 0)
				state |= StateFlags.IsGeneric;
		}

		#region Properties

		public override int Arity {
			get {
				return MemberDefinition.TypeParametersCount;
			}
		}

		public virtual TypeSpec BaseType {
			get {
				return base_type;
			}
			set {
				base_type = value;
			}
		}

		public virtual BuiltinTypeSpec.Type BuiltinType {
			get {
				return BuiltinTypeSpec.Type.None;
			}
		}

		public bool HasDynamicElement {
			get {
				return (state & StateFlags.HasDynamicElement) != 0;
			}
		}

		public virtual IList<TypeSpec> Interfaces {
			get {
				return ifaces;
			}
			set {
				ifaces = value;
			}
		}

		public bool IsArray {
			get {
				return Kind == MemberKind.ArrayType;
			}
		}

		public bool IsAttribute {
			get {
				if (!IsClass)
					return false;

				var type = this;
				do {
					if (type.BuiltinType == BuiltinTypeSpec.Type.Attribute)
						return true;

					if (type.IsGeneric)
						return false;
					
					type = type.base_type;
				} while (type != null);

				return false;
			}
		}

		public bool IsInterface {
			get {
				return Kind == MemberKind.Interface;
			}
		}

		public bool IsClass {
			get {
				return Kind == MemberKind.Class;
			}
		}

		public bool IsConstantCompatible {
			get {
				if ((Kind & (MemberKind.Enum | MemberKind.Class | MemberKind.Interface | MemberKind.Delegate | MemberKind.ArrayType)) != 0)
					return true;

				switch (BuiltinType) {
				case BuiltinTypeSpec.Type.Int:
				case BuiltinTypeSpec.Type.UInt:
				case BuiltinTypeSpec.Type.Long:
				case BuiltinTypeSpec.Type.ULong:
				case BuiltinTypeSpec.Type.Float:
				case BuiltinTypeSpec.Type.Double:
				case BuiltinTypeSpec.Type.Char:
				case BuiltinTypeSpec.Type.Short:
				case BuiltinTypeSpec.Type.Decimal:
				case BuiltinTypeSpec.Type.Bool:
				case BuiltinTypeSpec.Type.SByte:
				case BuiltinTypeSpec.Type.Byte:
				case BuiltinTypeSpec.Type.UShort:
				case BuiltinTypeSpec.Type.Dynamic:
					return true;
				}

				return false;
			}
		}

		public bool IsDelegate {
			get {
				return Kind == MemberKind.Delegate;
			}
		}

		//
		// Returns true for instances of Expression<T>
		//
		public virtual bool IsExpressionTreeType {
			get {
				return false;
			}
			set {
				state = value ? state | StateFlags.InflatedExpressionType : state & ~StateFlags.InflatedExpressionType;
			}
		}

		public bool IsEnum {
			get {
				return Kind == MemberKind.Enum;
			}
		}

		//
		// Returns true for instances of IList<T>, IEnumerable<T>, ICollection<T>
		//
		public virtual bool IsGenericIterateInterface {
			get {
				return false;
			}
			set {
				state = value ? state | StateFlags.GenericIterateInterface : state & ~StateFlags.GenericIterateInterface;
			}
		}

		//
		// Returns true for instances of System.Threading.Tasks.Task<T>
		//
		public virtual bool IsGenericTask {
			get {
				return false;
			}
			set {
				state = value ? state | StateFlags.GenericTask : state & ~StateFlags.GenericTask;
			}
		}

		// TODO: Should probably do
		// IsGenericType -- recursive
		// HasTypeParameter -- non-recursive
		public bool IsGenericOrParentIsGeneric {
			get {
				var ts = this;
				do {
					if (ts.IsGeneric)
						return true;
					ts = ts.declaringType;
				} while (ts != null);

				return false;
			}
		}

		public bool IsGenericParameter {
			get {
				return Kind == MemberKind.TypeParameter;
			}
		}

		//
		// Returns true for instances of Nullable<T>
		//
		public virtual bool IsNullableType {
			get {
				return false;
			}
			set {
				state = value ? state | StateFlags.InflatedNullableType : state & ~StateFlags.InflatedNullableType;
			}
		}

		public bool IsNested {
			get { return declaringType != null && Kind != MemberKind.TypeParameter; }
		}

		public bool IsPointer {
			get {
				return Kind == MemberKind.PointerType;
			}
		}

		public bool IsSealed {
			get { return (Modifiers & Modifiers.SEALED) != 0; }
		}

		public bool IsSpecialRuntimeType {
			get {
				return (state & StateFlags.SpecialRuntimeType) != 0;
			}
			set {
				state = value ? state | StateFlags.SpecialRuntimeType : state & ~StateFlags.SpecialRuntimeType;
			}
		}

		public bool IsStruct {
			get { 
				return Kind == MemberKind.Struct;
			}
		}

		public bool IsTypeBuilder {
			get {
#if STATIC
				return true;
#else
				var meta = GetMetaInfo().GetType ();
				return meta == TypeBuilder || meta == GenericTypeBuilder;
#endif
			}
		}

		//
		// Whether a type is unmanaged. This is used by the unsafe code
		//
		public bool IsUnmanaged {
			get {
				if (IsPointer)
					return ((ElementTypeSpec) this).Element.IsUnmanaged;

				var ds = MemberDefinition as DeclSpace;
				if (ds != null)
					return ds.IsUnmanagedType ();

				if (Kind == MemberKind.Void)
					return true;

				if (IsNested && DeclaringType.IsGenericOrParentIsGeneric)
					return false;

				return IsValueType (this);
			}
		}

		public MemberCache MemberCache {
			get {
				if (cache == null || (state & StateFlags.PendingMemberCacheMembers) != 0)
					InitializeMemberCache (false);

				return cache;
			}
			set {
				if (cache != null)
					throw new InternalErrorException ("Membercache reset");

				cache = value;
			}
		}

		public MemberCache MemberCacheTypes {
			get {
				if (cache == null)
					InitializeMemberCache (true);

				return cache;
			}
		}	

		public new ITypeDefinition MemberDefinition {
			get {
				return (ITypeDefinition) definition;
			}
		}

		// TODO: Wouldn't be better to rely on cast to InflatedTypeSpec and
		// remove the property, YES IT WOULD !!!
		public virtual TypeSpec[] TypeArguments {
			get { return TypeSpec.EmptyTypes; }
		}

		#endregion

		public bool AddInterface (TypeSpec iface)
		{
			if ((state & StateFlags.InterfacesExpanded) != 0)
				throw new InternalErrorException ("Modifying expanded interface list");

			if (ifaces == null) {
				ifaces = new List<TypeSpec> () { iface };
				return true;
			}

			if (!ifaces.Contains (iface)) {
				ifaces.Add (iface);
				return true;
			}

			return false;
		}

		//
		// Special version used during type definition
		//
		public bool AddInterfaceDefined (TypeSpec iface)
		{
			if (!AddInterface (iface))
				return false;

			//
			// We can get into a situation where a type is inflated before
			// its interfaces are resoved. Consider this situation
			//
			// class A<T> : X<A<int>>, IFoo {}
			//
			// When resolving base class of X`1 we inflate context type A`1
			// All this happens before we even hit IFoo resolve. Without
			// additional expansion any inside usage of A<T> would miss IFoo
			// interface because it comes from early inflated TypeSpec
			//
			if (inflated_instances != null) {
				foreach (var inflated in inflated_instances) {
					inflated.Value.AddInterface (iface);
				}
			}

			return true;
		}

		//
		// Returns all type arguments, usefull for nested types
		//
		public static TypeSpec[] GetAllTypeArguments (TypeSpec type)
		{
			IList<TypeSpec> targs = TypeSpec.EmptyTypes;

			do {
				if (type.Arity > 0) {
					if (targs.Count == 0) {
						targs = type.TypeArguments;
					} else {
						var list = targs as List<TypeSpec> ?? new List<TypeSpec> (targs);
						list.AddRange (type.TypeArguments);
						targs = list;
					}
				}

				type = type.declaringType;
			} while (type != null);

			return targs as TypeSpec[] ?? ((List<TypeSpec>) targs).ToArray ();
		}

		public AttributeUsageAttribute GetAttributeUsage (PredefinedAttribute pa)
		{
			if (Kind != MemberKind.Class)
				throw new InternalErrorException ();

			if (!pa.IsDefined)
				return Attribute.DefaultUsageAttribute;

			AttributeUsageAttribute aua = null;
			var type = this;
			while (type != null) {
				aua = type.MemberDefinition.GetAttributeUsage (pa);
				if (aua != null)
					break;

				type = type.BaseType;
			}

			return aua;
		}

		public virtual MetaType GetMetaInfo ()
		{
			return info;
		}

		public virtual TypeSpec GetDefinition ()
		{
			return this;
		}

		public override string GetSignatureForDocumentation ()
		{
			StringBuilder sb = new StringBuilder ();
			if (IsNested) {
				sb.Append (DeclaringType.GetSignatureForDocumentation ());
			} else {
				sb.Append (MemberDefinition.Namespace);
			}

			if (sb.Length != 0)
				sb.Append (".");

			sb.Append (Name);
			if (Arity > 0) {
				if (this is InflatedTypeSpec) {
				    sb.Append ("{");
				    for (int i = 0; i < Arity; ++i) {
				        if (i > 0)
				            sb.Append (",");

				        sb.Append (TypeArguments[i].GetSignatureForDocumentation ());
				    }
				    sb.Append ("}");
				} else {
					sb.Append ("`");
					sb.Append (Arity.ToString ());
				}
			}

			return sb.ToString ();
		}

		public string GetExplicitNameSignatureForDocumentation ()
		{
			StringBuilder sb = new StringBuilder ();
			if (IsNested) {
				sb.Append (DeclaringType.GetExplicitNameSignatureForDocumentation ());
			} else if (MemberDefinition.Namespace != null) {
				sb.Append (MemberDefinition.Namespace.Replace ('.', '#'));
			}

			if (sb.Length != 0)
				sb.Append ("#");

			sb.Append (Name);
			if (Arity > 0) {
				sb.Append ("{");
				for (int i = 0; i < Arity; ++i) {
					if (i > 0)
						sb.Append (",");

					sb.Append (TypeArguments[i].GetExplicitNameSignatureForDocumentation ());
				}
				sb.Append ("}");
			}

			return sb.ToString ();
		}

		public override string GetSignatureForError ()
		{
			string s;

			if (IsNested) {
				s = DeclaringType.GetSignatureForError ();
			} else if (MemberDefinition is AnonymousTypeClass) {
				return ((AnonymousTypeClass) MemberDefinition).GetSignatureForError ();
			} else {
				s = MemberDefinition.Namespace;
			}

			if (!string.IsNullOrEmpty (s))
				s += ".";

			return s + Name + GetTypeNameSignature ();
		}

		public string GetSignatureForErrorIncludingAssemblyName ()
		{
			return string.Format ("{0} [{1}]", GetSignatureForError (), MemberDefinition.DeclaringAssembly.FullName);
		}

		protected virtual string GetTypeNameSignature ()
		{
			if (!IsGeneric)
				return null;

			return "<" + TypeManager.CSharpName (MemberDefinition.TypeParameters) + ">";
		}

		public bool ImplementsInterface (TypeSpec iface, bool variantly)
		{
			var t = this;
			do {
				var ifaces = t.Interfaces;
				if (ifaces != null) {
					for (int i = 0; i < ifaces.Count; ++i) {
						if (TypeSpecComparer.IsEqual (ifaces[i], iface))
							return true;

						if (variantly && TypeSpecComparer.Variant.IsEqual (ifaces[i], iface))
							return true;
					}
				}

				// TODO: Why is it needed when we do it during import
				t = t.BaseType;
			} while (t != null);

			return false;
		}

		protected virtual void InitializeMemberCache (bool onlyTypes)
		{
			try {
				MemberDefinition.LoadMembers (this, onlyTypes, ref cache);
			} catch (Exception e) {
				throw new InternalErrorException (e, "Unexpected error when loading type `{0}'", GetSignatureForError ());
			}

			if (onlyTypes)
				state |= StateFlags.PendingMemberCacheMembers;
			else
				state &= ~StateFlags.PendingMemberCacheMembers;
		}

		//
		// Is @baseClass base implementation of @type. With enabled @dynamicIsEqual the slower
		// comparison is used to hide differences between `object' and `dynamic' for generic
		// types. Should not be used for comparisons where G<object> != G<dynamic>
		//
		public static bool IsBaseClass (TypeSpec type, TypeSpec baseClass, bool dynamicIsObject)
		{
			if (dynamicIsObject && baseClass.IsGeneric) {
				//
				// Returns true for a hierarchies like this when passing baseClass of A<dynamic>
				//
				// class B : A<object> {}
				//
				type = type.BaseType;
				while (type != null) {
					if (TypeSpecComparer.IsEqual (type, baseClass))
						return true;

					type = type.BaseType;
				}

				return false;
			}

			while (type != null) {
				type = type.BaseType;
				if (type == baseClass)
					return true;
			}

			return false;
		}

		public static bool IsReferenceType (TypeSpec t)
		{
			switch (t.Kind) {
			case MemberKind.TypeParameter:
				return ((TypeParameterSpec) t).IsReferenceType;
			case MemberKind.Struct:
			case MemberKind.Enum:
			case MemberKind.Void:
				return false;
			case MemberKind.InternalCompilerType:
				//
				// Null is considered to be a reference type
				//			
				return t == InternalType.NullLiteral || t.BuiltinType == BuiltinTypeSpec.Type.Dynamic;
			default:
				return true;
			}
		}

		public static bool IsValueType (TypeSpec t)
		{
			switch (t.Kind) {
			case MemberKind.TypeParameter:
				return ((TypeParameterSpec) t).IsValueType;
			case MemberKind.Struct:
			case MemberKind.Enum:
				return true;
			default:
				return false;
			}
		}

		public override MemberSpec InflateMember (TypeParameterInflator inflator)
		{
			var targs = IsGeneric ? MemberDefinition.TypeParameters : TypeSpec.EmptyTypes;

			//
			// When inflating nested type from inside the type instance will be same
			// because type parameters are same for all nested types
			//
			if (DeclaringType == inflator.TypeInstance) {
				return MakeGenericType (inflator.Context, targs);
			}

			return new InflatedTypeSpec (inflator.Context, this, inflator.TypeInstance, targs);
		}

		public InflatedTypeSpec MakeGenericType (IModuleContext context, TypeSpec[] targs)
		{
			if (targs.Length == 0 && !IsNested)
				throw new ArgumentException ("Empty type arguments for type " + GetSignatureForError ());

			InflatedTypeSpec instance;

			if (inflated_instances == null) {
				inflated_instances = new Dictionary<TypeSpec[], InflatedTypeSpec> (TypeSpecComparer.Default);

				if (IsNested) {
					instance = this as InflatedTypeSpec;
					if (instance != null) {
						//
						// Nested types could be inflated on already inflated instances
						// Caching this type ensured we are using same instance for
						// inside/outside inflation using local type parameters
						//
						inflated_instances.Add (TypeArguments, instance);
					}
				}
			}

			if (!inflated_instances.TryGetValue (targs, out instance)) {
				if (GetDefinition () != this && !IsNested)
					throw new InternalErrorException ("`{0}' must be type definition or nested non-inflated type to MakeGenericType",
						GetSignatureForError ());

				instance = new InflatedTypeSpec (context, this, declaringType, targs);
				inflated_instances.Add (targs, instance);
			}

			return instance;
		}

		public virtual TypeSpec Mutate (TypeParameterMutator mutator)
		{
			return this;
		}

		public override List<TypeSpec> ResolveMissingDependencies ()
		{
			List<TypeSpec> missing = null;

			if (Kind == MemberKind.MissingType) {
				missing = new List<TypeSpec> ();
				missing.Add (this);
				return missing;
			}

			foreach (var targ in TypeArguments) {
				if (targ.Kind == MemberKind.MissingType) {
					if (missing == null)
						missing = new List<TypeSpec> ();

					missing.Add (targ);
				}
			}

			if (Interfaces != null) {
				foreach (var iface in Interfaces) {
					if (iface.Kind == MemberKind.MissingType) {
						if (missing == null)
							missing = new List<TypeSpec> ();

						missing.Add (iface);
					}
				}
			}

			if (missing != null || BaseType == null)
				return missing;

			return BaseType.ResolveMissingDependencies ();
		}

		public void SetMetaInfo (MetaType info)
		{
			if (this.info != null)
				throw new InternalErrorException ("MetaInfo reset");

			this.info = info;
		}

		public void SetExtensionMethodContainer ()
		{
			modifiers |= Modifiers.METHOD_EXTENSION;
		}
	}

	public sealed class BuiltinTypeSpec : TypeSpec
	{
		public enum Type
		{
			None = 0,

			// Ordered carefully for fast compares
			FirstPrimitive = 1,
			Bool = 1,
			Byte = 2,
			SByte = 3,
			Char = 4,
			Short = 5,
			UShort = 6,
			Int = 7,
			UInt = 8,
			Long = 9,
			ULong = 10,
			Float = 11,
			Double = 12,
			LastPrimitive = 12,
			Decimal = 13,

			IntPtr = 14,
			UIntPtr = 15,

			Object = 16,
			Dynamic = 17,
			String = 18,
			Type = 19,

			ValueType = 20,
			Enum = 21,
			Delegate = 22,
			MulticastDelegate = 23,
			Array = 24,

			IEnumerator,
			IEnumerable,
			IDisposable,
			Exception,
			Attribute,
			Other,
		}

		readonly Type type;
		readonly string ns;
		readonly string name;

		public BuiltinTypeSpec (MemberKind kind, string ns, string name, Type builtinKind)
			: base (kind, null, null, null, Modifiers.PUBLIC)
		{
			this.type = builtinKind;
			this.ns = ns;
			this.name = name;
		}

		public BuiltinTypeSpec (string name, Type builtinKind)
			: this (MemberKind.InternalCompilerType, "", name, builtinKind)
		{
			// Make all internal types CLS-compliant, non-obsolete, compact
			state = (state & ~(StateFlags.CLSCompliant_Undetected | StateFlags.Obsolete_Undetected | StateFlags.MissingDependency_Undetected)) | StateFlags.CLSCompliant;
		}

		#region Properties

		public override int Arity {
			get {
				return 0;
			}
		}

		public override BuiltinTypeSpec.Type BuiltinType {
			get {
				return type;
			}
		}

		public string FullName {
			get {
				return ns + '.' + name;
			}
		}

		public override string Name {
			get {
				return name;
			}
		}

		public string Namespace {
			get {
				return ns;
			}
		}

		#endregion

		public static bool IsPrimitiveType (TypeSpec type)
		{
			return type.BuiltinType >= Type.FirstPrimitive && type.BuiltinType <= Type.LastPrimitive;
		}

		public static bool IsPrimitiveTypeOrDecimal (TypeSpec type)
		{
			return type.BuiltinType >= Type.FirstPrimitive && type.BuiltinType <= Type.Decimal;
		}

		public override string GetSignatureForError ()
		{
			switch (Name) {
			case "Int32": return "int";
			case "Int64": return "long";
			case "String": return "string";
			case "Boolean": return "bool";
			case "Void": return "void";
			case "Object": return "object";
			case "UInt32": return "uint";
			case "Int16": return "short";
			case "UInt16": return "ushort";
			case "UInt64": return "ulong";
			case "Single": return "float";
			case "Double": return "double";
			case "Decimal": return "decimal";
			case "Char": return "char";
			case "Byte": return "byte";
			case "SByte": return "sbyte";
			}

			if (ns.Length == 0)
				return name;

			return FullName;
		}

		//
		// Returns the size of type if known, otherwise, 0
		//
		public static int GetSize (TypeSpec type)
		{
			switch (type.BuiltinType) {
			case Type.Int:
			case Type.UInt:
			case Type.Float:
				return 4;
			case Type.Long:
			case Type.ULong:
			case Type.Double:
				return 8;
			case Type.Byte:
			case Type.SByte:
			case Type.Bool:
				return 1;
			case Type.Short:
			case Type.Char:
			case Type.UShort:
				return 2;
			case Type.Decimal:
				return 16;
			default:
				return 0;
			}
		}

		public void SetDefinition (ITypeDefinition td, MetaType type, Modifiers mod)
		{
			this.definition = td;
			this.info = type;
			this.modifiers |= (mod & ~Modifiers.AccessibilityMask);
		}

		public void SetDefinition (TypeSpec ts)
		{
			this.definition = ts.MemberDefinition;
			this.info = ts.GetMetaInfo ();
			this.BaseType = ts.BaseType;
			this.Interfaces = ts.Interfaces;
			this.modifiers = ts.Modifiers;
		}
	}

	static class TypeSpecComparer
	{
		//
		// Does strict reference comparion only
		//
		public static readonly DefaultImpl Default = new DefaultImpl ();

		public class DefaultImpl : IEqualityComparer<TypeSpec[]>
		{
			#region IEqualityComparer<TypeSpec[]> Members

			bool IEqualityComparer<TypeSpec[]>.Equals (TypeSpec[] x, TypeSpec[] y)
			{
				if (x == y)
					return true;

				if (x.Length != y.Length)
					return false;

				for (int i = 0; i < x.Length; ++i)
					if (x[i] != y[i])
						return false;

				return true;
			}

			int IEqualityComparer<TypeSpec[]>.GetHashCode (TypeSpec[] obj)
			{
				int hash = 0;
				for (int i = 0; i < obj.Length; ++i)
					hash = (hash << 5) - hash + obj[i].GetHashCode ();

				return hash;
			}

			#endregion
		}

		//
		// When comparing type signature of overrides or overloads
		// this version tolerates different MVARs at same position
		//
		public static class Override
		{
			public static bool IsEqual (TypeSpec a, TypeSpec b)
			{
				if (a == b)
					return true;

				//
				// Consider the following example:
				//
				//     public abstract class A
				//     {
				//        public abstract T Foo<T>();
				//     }
				//
				//     public class B : A
				//     {
				//        public override U Foo<T>() { return default (U); }
				//     }
				//
				// Here, `T' and `U' are method type parameters from different methods
				// (A.Foo and B.Foo), so both `==' and Equals() will fail.
				//
				// However, since we're determining whether B.Foo() overrides A.Foo(),
				// we need to do a signature based comparision and consider them equal.
				//

				var tp_a = a as TypeParameterSpec;
				if (tp_a != null) {
					var tp_b = b as TypeParameterSpec;
					return tp_b != null && tp_a.IsMethodOwned == tp_b.IsMethodOwned && tp_a.DeclaredPosition == tp_b.DeclaredPosition;
				}

				var ac_a = a as ArrayContainer;
				if (ac_a != null) {
					var ac_b = b as ArrayContainer;
					return ac_b != null && ac_a.Rank == ac_b.Rank && IsEqual (ac_a.Element, ac_b.Element);
				}

				if (a.BuiltinType == BuiltinTypeSpec.Type.Dynamic || b.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
					return b.BuiltinType == BuiltinTypeSpec.Type.Object || a.BuiltinType == BuiltinTypeSpec.Type.Object;

				if (a.MemberDefinition != b.MemberDefinition)
					return false;

				do {
					for (int i = 0; i < a.TypeArguments.Length; ++i) {
						if (!IsEqual (a.TypeArguments[i], b.TypeArguments[i]))
							return false;
					}

					a = a.DeclaringType;
					b = b.DeclaringType;
				} while (a != null);

				return true;
			}

			//
			// Compares unordered arrays
			//
			public static bool IsSame (TypeSpec[] a, TypeSpec[] b)
			{
				if (a == b)
					return true;

				if (a == null || b == null || a.Length != b.Length)
					return false;

				for (int ai = 0; ai < a.Length; ++ai) {
					bool found = false;
					for (int bi = 0; bi < b.Length; ++bi) {
						if (IsEqual (a[ai], b[bi])) {
							found = true;
							break;
						}
					}

					if (!found)
						return false;
				}

				return true;
			}

			public static bool IsEqual (AParametersCollection a, AParametersCollection b)
			{
				if (a == b)
					return true;

				if (a.Count != b.Count)
					return false;

				for (int i = 0; i < a.Count; ++i) {
					if (!IsEqual (a.Types[i], b.Types[i]))
						return false;

					const Parameter.Modifier ref_out = Parameter.Modifier.REF | Parameter.Modifier.OUT;
					if ((a.FixedParameters[i].ModFlags & ref_out) != (b.FixedParameters[i].ModFlags & ref_out))
						return false;
				}

				return true;
			}
		}

		//
		// Type variance equality comparison
		//
		public static class Variant
		{
			public static bool IsEqual (TypeSpec type1, TypeSpec type2)
			{
				if (!type1.IsGeneric || !type2.IsGeneric)
					return false;

				var target_type_def = type2.MemberDefinition;
				if (type1.MemberDefinition != target_type_def)
					return false;

				var t1_targs = type1.TypeArguments;
				var t2_targs = type2.TypeArguments;
				var targs_definition = target_type_def.TypeParameters;

				if (!type1.IsInterface && !type1.IsDelegate) {
					return false;
				}

				for (int i = 0; i < targs_definition.Length; ++i) {
					if (TypeSpecComparer.IsEqual (t1_targs[i], t2_targs[i]))
						continue;

					Variance v = targs_definition[i].Variance;
					if (v == Variance.None) {
						return false;
					}

					if (v == Variance.Covariant) {
						if (!Convert.ImplicitReferenceConversionExists (t1_targs[i], t2_targs[i]))
							return false;
					} else if (!Convert.ImplicitReferenceConversionExists (t2_targs[i], t1_targs[i])) {
						return false;
					}
				}

				return true;
			}
		}

		//
		// Checks whether two generic instances may become equal for some
		// particular instantiation (26.3.1).
		//
		public static class Unify
		{
			//
			// Either @a or @b must be generic type
			//
			public static bool IsEqual (TypeSpec a, TypeSpec b)
			{
				if (a.MemberDefinition != b.MemberDefinition) {
					var base_ifaces = a.Interfaces;
					if (base_ifaces != null) {
						foreach (var base_iface in base_ifaces) {
							if (base_iface.Arity > 0 && IsEqual (base_iface, b))
								return true;
						}
					}

					return false;
				}

				var ta = a.TypeArguments;
				var tb = b.TypeArguments;
				for (int i = 0; i < ta.Length; i++) {
					if (!MayBecomeEqualGenericTypes (ta[i], tb[i]))
						return false;
				}

				return true;
			}

			static bool ContainsTypeParameter (TypeSpec tparam, TypeSpec type)
			{
				TypeSpec[] targs = type.TypeArguments;
				for (int i = 0; i < targs.Length; i++) {
					if (tparam == targs[i])
						return true;

					if (ContainsTypeParameter (tparam, targs[i]))
						return true;
				}

				return false;
			}

			/// <summary>
			///   Check whether `a' and `b' may become equal generic types.
			///   The algorithm to do that is a little bit complicated.
			/// </summary>
			static bool MayBecomeEqualGenericTypes (TypeSpec a, TypeSpec b)
			{
				if (a.IsGenericParameter) {
					//
					// If a is an array of a's type, they may never
					// become equal.
					//
					if (b.IsArray)
						return false;

					//
					// If b is a generic parameter or an actual type,
					// they may become equal:
					//
					//    class X<T,U> : I<T>, I<U>
					//    class X<T> : I<T>, I<float>
					// 
					if (b.IsGenericParameter)
						return a != b && a.DeclaringType == b.DeclaringType;

					//
					// We're now comparing a type parameter with a
					// generic instance.  They may become equal unless
					// the type parameter appears anywhere in the
					// generic instance:
					//
					//    class X<T,U> : I<T>, I<X<U>>
					//        -> error because you could instanciate it as
					//           X<X<int>,int>
					//
					//    class X<T> : I<T>, I<X<T>> -> ok
					//

					return !ContainsTypeParameter (a, b);
				}

				if (b.IsGenericParameter)
					return MayBecomeEqualGenericTypes (b, a);

				//
				// At this point, neither a nor b are a type parameter.
				//
				// If one of them is a generic instance, compare them (if the
				// other one is not a generic instance, they can never
				// become equal).
				//
				if (TypeManager.IsGenericType (a) || TypeManager.IsGenericType (b))
					return IsEqual (a, b);

				//
				// If both of them are arrays.
				//
				var a_ac = a as ArrayContainer;
				if (a_ac != null) {
					var b_ac = b as ArrayContainer;
					if (b_ac == null || a_ac.Rank != b_ac.Rank)
						return false;

					return MayBecomeEqualGenericTypes (a_ac.Element, b_ac.Element);
				}

				//
				// Ok, two ordinary types.
				//
				return false;
			}
		}

		public static bool Equals (TypeSpec[] x, TypeSpec[] y)
		{
			if (x == y)
				return true;

			if (x.Length != y.Length)
				return false;

			for (int i = 0; i < x.Length; ++i)
				if (!IsEqual (x[i], y[i]))
					return false;

			return true;
		}

		//
		// Identity type conversion
		//
		// Default reference comparison, it has to be used when comparing
		// two possible dynamic/internal types
		//
		public static bool IsEqual (TypeSpec a, TypeSpec b)
		{
			if (a == b) {
				// This also rejects dynamic == dynamic
				return a.Kind != MemberKind.InternalCompilerType || a.BuiltinType == BuiltinTypeSpec.Type.Dynamic;
			}

			if (a == null || b == null)
				return false;

			if (a.IsArray) {
				var a_a = (ArrayContainer) a;
				var b_a = b as ArrayContainer;
				if (b_a == null)
					return false;

				return a_a.Rank == b_a.Rank && IsEqual (a_a.Element, b_a.Element);
			}

			if (!a.IsGeneric || !b.IsGeneric) {
				//
				// object and dynamic are considered equivalent there is an identity conversion
				// between object and dynamic, and between constructed types that are the same
				// when replacing all occurences of dynamic with object.
				//
				if (a.BuiltinType == BuiltinTypeSpec.Type.Dynamic || b.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
					return b.BuiltinType == BuiltinTypeSpec.Type.Object || a.BuiltinType == BuiltinTypeSpec.Type.Object;

				return false;
			}

			if (a.MemberDefinition != b.MemberDefinition)
				return false;

			do {
				if (!Equals (a.TypeArguments, b.TypeArguments))
					return false;

				a = a.DeclaringType;
				b = b.DeclaringType;
			} while (a != null);

			return true;
		}
	}

	public interface ITypeDefinition : IMemberDefinition
	{
		IAssemblyDefinition DeclaringAssembly { get; }
		string Namespace { get; }
		int TypeParametersCount { get; }
		TypeParameterSpec[] TypeParameters { get; }

		TypeSpec GetAttributeCoClass ();
		string GetAttributeDefaultMember ();
		AttributeUsageAttribute GetAttributeUsage (PredefinedAttribute pa);
		bool IsInternalAsPublic (IAssemblyDefinition assembly);
		void LoadMembers (TypeSpec declaringType, bool onlyTypes, ref MemberCache cache);
	}

	class InternalType : TypeSpec, ITypeDefinition
	{
		public static readonly InternalType AnonymousMethod = new InternalType ("anonymous method");
		public static readonly InternalType Arglist = new InternalType ("__arglist");
		public static readonly InternalType MethodGroup = new InternalType ("method group");
		public static readonly InternalType NullLiteral = new InternalType ("null");
		public static readonly InternalType FakeInternalType = new InternalType ("<fake$type>");
		public static readonly InternalType Namespace = new InternalType ("<namespace>");
		public static readonly InternalType ErrorType = new InternalType ("<error>");

		readonly string name;

		InternalType (string name, MemberCache cache)
			: this (name)
		{
			this.cache = cache;
		}

		InternalType (string name)
			: base (MemberKind.InternalCompilerType, null, null, null, Modifiers.PUBLIC)
		{
			this.name = name;
			this.definition = this;
			cache = MemberCache.Empty;

			// Make all internal types CLS-compliant, non-obsolete
			state = (state & ~(StateFlags.CLSCompliant_Undetected | StateFlags.Obsolete_Undetected)) | StateFlags.CLSCompliant;
		}

		#region Properties

		public override int Arity {
			get {
				return 0;
			}
		}

		IAssemblyDefinition ITypeDefinition.DeclaringAssembly {
			get {
				throw new NotImplementedException ();
			}
		}

		bool IMemberDefinition.IsImported {
			get {
				return false;
			}
		}

		public override string Name {
			get {
				return name;
			}
		}

		string ITypeDefinition.Namespace {
			get {
				return null;
			}
		}

		int ITypeDefinition.TypeParametersCount {
			get {
				return 0;
			}
		}

		TypeParameterSpec[] ITypeDefinition.TypeParameters {
			get {
				return null;
			}
		}

		#endregion

		public override string GetSignatureForError ()
		{
			return name;
		}

		#region ITypeDefinition Members

		TypeSpec ITypeDefinition.GetAttributeCoClass ()
		{
			return null;
		}

		string ITypeDefinition.GetAttributeDefaultMember ()
		{
			return null;
		}

		AttributeUsageAttribute ITypeDefinition.GetAttributeUsage (PredefinedAttribute pa)
		{
			return null;
		}

		bool ITypeDefinition.IsInternalAsPublic (IAssemblyDefinition assembly)
		{
			throw new NotImplementedException ();
		}

		void ITypeDefinition.LoadMembers (TypeSpec declaringType, bool onlyTypes, ref MemberCache cache)
		{
			throw new NotImplementedException ();
		}

		string[] IMemberDefinition.ConditionalConditions ()
		{
			return null;
		}

		ObsoleteAttribute IMemberDefinition.GetAttributeObsolete ()
		{
			return null;
		}

		bool? IMemberDefinition.CLSAttributeValue {
			get {
				return null;
			}
		}

		void IMemberDefinition.SetIsAssigned ()
		{
		}

		void IMemberDefinition.SetIsUsed ()
		{
		}

		#endregion
	}

	public abstract class ElementTypeSpec : TypeSpec, ITypeDefinition
	{
		protected ElementTypeSpec (MemberKind kind, TypeSpec element, MetaType info)
			: base (kind, element.DeclaringType, null, info, element.Modifiers)
		{
			this.Element = element;

			// Some flags can be copied directly from the element
			const StateFlags shared_flags = StateFlags.CLSCompliant | StateFlags.CLSCompliant_Undetected
				| StateFlags.Obsolete | StateFlags.Obsolete_Undetected | StateFlags.HasDynamicElement;
			state &= ~shared_flags;
			state |= (element.state & shared_flags);

			if (element.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
				state |= StateFlags.HasDynamicElement;

			// Has to use its own type definition instead of just element definition to
			// correctly identify itself for cases like x.MemberDefininition == predefined.MemberDefinition
			this.definition = this;

			cache = MemberCache.Empty;
		}

		#region Properties

		public TypeSpec Element { get; private set; }

		public override string Name {
			get {
				throw new NotSupportedException ();
			}
		}

		#endregion

		public override ObsoleteAttribute GetAttributeObsolete ()
		{
			return Element.GetAttributeObsolete ();
		}

		protected virtual string GetPostfixSignature ()
		{
			return null;
		}

		public override string GetSignatureForDocumentation ()
		{
			return Element.GetSignatureForDocumentation () + GetPostfixSignature ();
		}

		public override string GetSignatureForError ()
		{
			return Element.GetSignatureForError () + GetPostfixSignature ();
		}

		public override TypeSpec Mutate (TypeParameterMutator mutator)
		{
			var me = Element.Mutate (mutator);
			if (me == Element)
				return this;

			var mutated = (ElementTypeSpec) MemberwiseClone ();
			mutated.Element = me;
			mutated.info = null;
			return mutated;
		}

		#region ITypeDefinition Members

		IAssemblyDefinition ITypeDefinition.DeclaringAssembly {
			get {
				return Element.MemberDefinition.DeclaringAssembly;
			}
		}

		bool ITypeDefinition.IsInternalAsPublic (IAssemblyDefinition assembly)
		{
			return Element.MemberDefinition.IsInternalAsPublic (assembly);
		}

		public string Namespace {
			get { throw new NotImplementedException (); }
		}

		public int TypeParametersCount {
			get {
				return 0;
			}
		}

		public TypeParameterSpec[] TypeParameters {
			get {
				throw new NotSupportedException ();
			}
		}

		public TypeSpec GetAttributeCoClass ()
		{
			return Element.MemberDefinition.GetAttributeCoClass ();
		}

		public string GetAttributeDefaultMember ()
		{
			return Element.MemberDefinition.GetAttributeDefaultMember ();
		}

		public void LoadMembers (TypeSpec declaringType, bool onlyTypes, ref MemberCache cache)
		{
			Element.MemberDefinition.LoadMembers (declaringType, onlyTypes, ref cache);
		}

		public bool IsImported {
			get {
				return Element.MemberDefinition.IsImported;
			}
		}

		public string[] ConditionalConditions ()
		{
			return Element.MemberDefinition.ConditionalConditions ();
		}

		bool? IMemberDefinition.CLSAttributeValue {
			get {
				return Element.MemberDefinition.CLSAttributeValue;
			}
		}

		public void SetIsAssigned ()
		{
			Element.MemberDefinition.SetIsAssigned ();
		}

		public void SetIsUsed ()
		{
			Element.MemberDefinition.SetIsUsed ();
		}

		#endregion
	}

	public class ArrayContainer : ElementTypeSpec
	{
		public struct TypeRankPair : IEquatable<TypeRankPair>
		{
			TypeSpec ts;
			int rank;

			public TypeRankPair (TypeSpec ts, int rank)
			{
				this.ts = ts;
				this.rank = rank;
			}

			public override int GetHashCode ()
			{
				return ts.GetHashCode () ^ rank.GetHashCode ();
			}

			#region IEquatable<Tuple<T1,T2>> Members

			public bool Equals (TypeRankPair other)
			{
				return other.ts == ts && other.rank == rank;
			}

			#endregion
		}

		readonly int rank;
		readonly ModuleContainer module;

		private ArrayContainer (ModuleContainer module, TypeSpec element, int rank)
			: base (MemberKind.ArrayType, element, null)
		{
			this.module = module;
			this.rank = rank;
		}

		public int Rank {
			get {
				return rank;
			}
		}

		public MethodInfo GetConstructor ()
		{
			var mb = module.Builder;

			var arg_types = new MetaType[rank];
			for (int i = 0; i < rank; i++)
				arg_types[i] = module.Compiler.BuiltinTypes.Int.GetMetaInfo ();

			var ctor = mb.GetArrayMethod (
				GetMetaInfo (), Constructor.ConstructorName,
				CallingConventions.HasThis,
				null, arg_types);

			return ctor;
		}

		public MethodInfo GetAddressMethod ()
		{
			var mb = module.Builder;

			var arg_types = new MetaType[rank];
			for (int i = 0; i < rank; i++)
				arg_types[i] = module.Compiler.BuiltinTypes.Int.GetMetaInfo ();

			var address = mb.GetArrayMethod (
				GetMetaInfo (), "Address",
				CallingConventions.HasThis | CallingConventions.Standard,
				ReferenceContainer.MakeType (module, Element).GetMetaInfo (), arg_types);

			return address;
		}

		public MethodInfo GetGetMethod ()
		{
			var mb = module.Builder;

			var arg_types = new MetaType[rank];
			for (int i = 0; i < rank; i++)
				arg_types[i] = module.Compiler.BuiltinTypes.Int.GetMetaInfo ();

			var get = mb.GetArrayMethod (
				GetMetaInfo (), "Get",
				CallingConventions.HasThis | CallingConventions.Standard,
				Element.GetMetaInfo (), arg_types);

			return get;
		}

		public MethodInfo GetSetMethod ()
		{
			var mb = module.Builder;

			var arg_types = new MetaType[rank + 1];
			for (int i = 0; i < rank; i++)
				arg_types[i] = module.Compiler.BuiltinTypes.Int.GetMetaInfo ();

			arg_types[rank] = Element.GetMetaInfo ();

			var set = mb.GetArrayMethod (
				GetMetaInfo (), "Set",
				CallingConventions.HasThis | CallingConventions.Standard,
				module.Compiler.BuiltinTypes.Void.GetMetaInfo (), arg_types);

			return set;
		}

		public override MetaType GetMetaInfo ()
		{
			if (info == null) {
				if (rank == 1)
					info = Element.GetMetaInfo ().MakeArrayType ();
				else
					info = Element.GetMetaInfo ().MakeArrayType (rank);
			}

			return info;
		}

		protected override string GetPostfixSignature()
		{
			return GetPostfixSignature (rank);
		}

		public static string GetPostfixSignature (int rank)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("[");
			for (int i = 1; i < rank; i++) {
				sb.Append (",");
			}
			sb.Append ("]");

			return sb.ToString ();
		}

		public override string GetSignatureForDocumentation ()
		{
			StringBuilder sb = new StringBuilder ();
			GetElementSignatureForDocumentation (sb);
			return sb.ToString ();
		}

		void GetElementSignatureForDocumentation (StringBuilder sb)
		{
			var ac = Element as ArrayContainer;
			if (ac == null)
				sb.Append (Element.GetSignatureForDocumentation ());
			else
				ac.GetElementSignatureForDocumentation (sb);

			sb.Append ("[");
			for (int i = 1; i < rank; i++) {
				if (i == 1)
					sb.Append ("0:");

				sb.Append (",0:");
			}
			sb.Append ("]");
		}

		public static ArrayContainer MakeType (ModuleContainer module, TypeSpec element)
		{
			return MakeType (module, element, 1);
		}

		public static ArrayContainer MakeType (ModuleContainer module, TypeSpec element, int rank)
		{
			ArrayContainer ac;
			var key = new TypeRankPair (element, rank);
			if (!module.ArrayTypesCache.TryGetValue (key, out ac)) {
				ac = new ArrayContainer (module, element, rank) {
					BaseType = module.Compiler.BuiltinTypes.Array
				};

				module.ArrayTypesCache.Add (key, ac);
			}

			return ac;
		}
	}

	class ReferenceContainer : ElementTypeSpec
	{
		private ReferenceContainer (TypeSpec element)
			: base (MemberKind.Class, element, null)	// TODO: Kind.Class is most likely wrong
		{
		}

		public override MetaType GetMetaInfo ()
		{
			if (info == null) {
				info = Element.GetMetaInfo ().MakeByRefType ();
			}

			return info;
		}

		public static ReferenceContainer MakeType (ModuleContainer module, TypeSpec element)
		{
			ReferenceContainer pc;
			if (!module.ReferenceTypesCache.TryGetValue (element, out pc)) {
				pc = new ReferenceContainer (element);
				module.ReferenceTypesCache.Add (element, pc);
			}

			return pc;
		}
	}

	class PointerContainer : ElementTypeSpec
	{
		private PointerContainer (TypeSpec element)
			: base (MemberKind.PointerType, element, null)
		{
			// It's never CLS-Compliant
			state &= ~StateFlags.CLSCompliant_Undetected;
		}

		public override MetaType GetMetaInfo ()
		{
			if (info == null) {
				info = Element.GetMetaInfo ().MakePointerType ();
			}

			return info;
		}

		protected override string GetPostfixSignature()
		{
 			return "*";
		}

		public static PointerContainer MakeType (ModuleContainer module, TypeSpec element)
		{
			PointerContainer pc;
			if (!module.PointerTypesCache.TryGetValue (element, out pc)) {
				pc = new PointerContainer (element);
				module.PointerTypesCache.Add (element, pc);
			}

			return pc;
		}
	}
}
