//
// membercache.cs: A container for all member lookups
//
// Author: Miguel de Icaza (miguel@gnu.org)
//         Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2010 Novell, Inc
// Copyright 2011 Xamarin Inc
//
//

using System;
using System.Collections.Generic;

namespace Mono.CSharp {

	[Flags]
	public enum MemberKind
	{
		Constructor = 1,
		Event = 1 << 1,
		Field = 1 << 2,
		Method = 1 << 3,
		Property = 1 << 4,
		Indexer = 1 << 5,
		Operator = 1 << 6,
		Destructor	= 1 << 7,

		Class		= 1 << 11,
		Struct		= 1 << 12,
		Delegate	= 1 << 13,
		Enum		= 1 << 14,
		Interface	= 1 << 15,
		TypeParameter = 1 << 16,

		ArrayType = 1 << 19,
		PointerType = 1 << 20,
		InternalCompilerType = 1 << 21,
		MissingType = 1 << 22,
		Void = 1 << 23,
		Namespace = 1 << 24,

		NestedMask = Class | Struct | Delegate | Enum | Interface,
		GenericMask = Method | Class | Struct | Delegate | Interface,
		MaskType = Constructor | Event | Field | Method | Property | Indexer | Operator | Destructor | NestedMask
	}

	[Flags]
	public enum BindingRestriction
	{
		None = 0,

		// Inspect only queried type members
		DeclaredOnly = 1 << 1,

		// Exclude static
		InstanceOnly = 1 << 2,

		NoAccessors = 1 << 3,

		// Member has to be override
		OverrideOnly = 1 << 4
	}

	public struct MemberFilter : IEquatable<MemberSpec>
	{
		public readonly string Name;
		public readonly MemberKind Kind;
		public readonly AParametersCollection Parameters;
		public readonly TypeSpec MemberType;
		public readonly int Arity; // -1 to ignore the check

		public MemberFilter (MethodSpec m)
		{
			Name = m.Name;
			Kind = MemberKind.Method;
			Parameters = m.Parameters;
			MemberType = m.ReturnType;
			Arity = m.Arity;
		}

		public MemberFilter (string name, int arity, MemberKind kind, AParametersCollection param, TypeSpec type)
		{
			Name = name;
			Kind = kind;
			Parameters = param;
			MemberType = type;
			this.Arity = arity;
		}

		public static MemberFilter Constructor (AParametersCollection param)
		{
			return new MemberFilter (Mono.CSharp.Constructor.ConstructorName, 0, MemberKind.Constructor, param, null);
		}

		public static MemberFilter Property (string name, TypeSpec type)
		{
			return new MemberFilter (name, 0, MemberKind.Property, null, type);
		}

		public static MemberFilter Field (string name, TypeSpec type)
		{
			return new MemberFilter (name, 0, MemberKind.Field, null, type);
		}

		public static MemberFilter Method (string name, int arity, AParametersCollection param, TypeSpec type)
		{
			return new MemberFilter (name, arity, MemberKind.Method, param, type);
		}

		#region IEquatable<MemberSpec> Members

		public bool Equals (MemberSpec other)
		{
			// Is the member of the correct type ?
			// TODO: Isn't this redundant ?
			if ((other.Kind & Kind & MemberKind.MaskType) == 0)
				return false;

			// Check arity when not disabled
			if (Arity >= 0 && Arity != other.Arity)
				return false;

			if (Parameters != null) {
				if (other is IParametersMember) {
					var other_param = ((IParametersMember) other).Parameters;
					if (!TypeSpecComparer.Override.IsEqual (Parameters, other_param))
						return false;
				} else {
					return false;
				}
			}

			if (MemberType != null) {
				if (other is IInterfaceMemberSpec) {
					var other_type = ((IInterfaceMemberSpec) other).MemberType;
					if (!TypeSpecComparer.Override.IsEqual (other_type, MemberType))
						return false;
				} else {
					return false;
				}
			}

			return true;
		}

		#endregion
	}

	//
	// The MemberCache is the main members container used by compiler. It contains
	// all members imported or defined during compilation using on demand filling
	// process. Inflated containers are also using MemberCache to make inflated
	// members look like normal definition.
	//
	// All of the methods are performance and memory sensitive as the MemberCache
	// is the underlying engine of all member based operations.
	//
	public class MemberCache
	{
		[Flags]
		enum StateFlags
		{
			HasConversionOperator = 1 << 1,
			HasUserOperator = 1 << 2
		}

		readonly Dictionary<string, IList<MemberSpec>> member_hash;
		Dictionary<string, MemberSpec[]> locase_members;
		IList<MethodSpec> missing_abstract;
		StateFlags state;	// TODO: Move to TypeSpec or ITypeDefinition

		public static readonly string IndexerNameAlias = "<this>";

		public static readonly MemberCache Empty = new MemberCache (0);

		public MemberCache ()
			: this (16)
		{
		}

		public MemberCache (int capacity)
		{
			member_hash = new Dictionary<string, IList<MemberSpec>> (capacity);
		}

		public MemberCache (MemberCache cache)
			: this (cache.member_hash.Count)
		{
			this.state = cache.state;
		}

		//
		// Creates a new MemberCache for the given `container'.
		//
		public MemberCache (TypeContainer container)
			: this ()				// TODO: Optimize the size
		{
		}

		//
		// For cases where we need to union cache members
		//
		public void AddBaseType (TypeSpec baseType)
		{
			var cache = baseType.MemberCache;

			IList<MemberSpec> list;
			foreach (var entry in cache.member_hash) {
				if (!member_hash.TryGetValue (entry.Key, out list)) {
					if (entry.Value.Count == 1) {
						list = entry.Value;
					} else {
						list = new List<MemberSpec> (entry.Value);
					}

					member_hash.Add (entry.Key, list);
					continue;
				}

				foreach (var ce in entry.Value) {
					if (list.Contains (ce))
						continue;

					if (list is MemberSpec[]) {
						list = new List<MemberSpec> () { list [0] };
						member_hash[entry.Key] = list;
					}

					list.Add (ce);
				}
			}
		}

		//
		// Member-cache does not contain base members but it does
		// contain all base interface members, so the Lookup code
		// can use simple inheritance rules.
		//
		public void AddInterface (TypeSpec iface)
		{
			var cache = iface.MemberCache;

			IList<MemberSpec> list;
			foreach (var entry in cache.member_hash) {
				if (!member_hash.TryGetValue (entry.Key, out list)) {
					if (entry.Value.Count == 1) {
						list = entry.Value;
					} else {
						list = new List<MemberSpec> (entry.Value);
					}

					member_hash.Add (entry.Key, list);
					continue;
				}

				foreach (var ce in entry.Value) {
					if (list.Contains (ce))
						continue;

					if (AddInterfaceMember (ce, ref list))
						member_hash[entry.Key] = list;
				}
			}

			// Add also all base interfaces
			if (iface.Interfaces != null) {
				foreach (var base_iface in iface.Interfaces)
					AddInterface (base_iface);
			}
		}

		public void AddMember (InterfaceMemberBase imb, string exlicitName, MemberSpec ms)
		{
			// Explicit names cannot be looked-up but can be used for
			// collision checking (no name mangling needed)
			if (imb.IsExplicitImpl)
				AddMember (exlicitName, ms, false);
			else
				AddMember (ms);
		}

		//
		// Add non-explicit member to member cache
		//
		public void AddMember (MemberSpec ms)
		{
			AddMember (GetLookupName (ms), ms, false);
		}

		void AddMember (string name, MemberSpec member, bool removeHiddenMembers)
		{
			if (member.Kind == MemberKind.Operator) {
				var dt = member.DeclaringType;

				//
				// Some core types have user operators but they cannot be used like normal
				// user operators as they are predefined and therefore having different
				// rules (e.g. binary operators) by not setting the flag we hide them for
				// user conversions
				//
				if (!BuiltinTypeSpec.IsPrimitiveType (dt)) {
					switch (dt.BuiltinType) {
					case BuiltinTypeSpec.Type.String:
					case BuiltinTypeSpec.Type.Delegate:
					case BuiltinTypeSpec.Type.MulticastDelegate:
						break;
					default:
						if (name == Operator.GetMetadataName (Operator.OpType.Implicit) || name == Operator.GetMetadataName (Operator.OpType.Explicit)) {
							state |= StateFlags.HasConversionOperator;
						} else {
							state |= StateFlags.HasUserOperator;
						}

						break;
					}
				}
			}

			IList<MemberSpec> list;
			if (!member_hash.TryGetValue (name, out list)) {
				member_hash.Add (name, new MemberSpec[] { member });
				return;
			}

			if (removeHiddenMembers && member.DeclaringType.IsInterface) {
				if (AddInterfaceMember (member, ref list))
					member_hash[name] = list;
			} else {
				if (list.Count == 1) {
					list = new List<MemberSpec> () { list[0] };
					member_hash[name] = list;
				}

				list.Add (member);
			}
		}

		public void AddMemberImported (MemberSpec ms)
		{
			AddMember (GetLookupName (ms), ms, true);
		}

		//
		// Ignores any base interface member which can be hidden
		// by this interface
		//
		static bool AddInterfaceMember (MemberSpec member, ref IList<MemberSpec> existing)
		{
			var member_param = member is IParametersMember ? ((IParametersMember) member).Parameters : ParametersCompiled.EmptyReadOnlyParameters;

			//
			// interface IA : IB { int Prop { set; } }
			// interface IB { bool Prop { get; } }
			//
			// IB.Prop is never accessible from IA interface
			//
			for (int i = 0; i < existing.Count; ++i) {
				var entry = existing[i];

				if (entry.Arity != member.Arity)
					continue;

				if (entry is IParametersMember) {
					var entry_param = ((IParametersMember) entry).Parameters;
					if (!TypeSpecComparer.Override.IsEqual (entry_param, member_param))
						continue;
				}

				if (member.DeclaringType.ImplementsInterface (entry.DeclaringType, false)) {
					if (existing.Count == 1) {
						existing = new MemberSpec[] { member };
						return true;
					}

					existing.RemoveAt (i--);
					continue;
				}

				if ((entry.DeclaringType == member.DeclaringType && entry.IsAccessor == member.IsAccessor) ||
					entry.DeclaringType.ImplementsInterface (member.DeclaringType, false))
					return false;
			}

			if (existing.Count == 1) {
				existing = new List<MemberSpec> () { existing[0], member };
				return true;
			}

			existing.Add (member);
			return false;
		}

		public static MemberSpec FindMember (TypeSpec container, MemberFilter filter, BindingRestriction restrictions)
		{
			do {
				IList<MemberSpec> applicable;
				if (container.MemberCache.member_hash.TryGetValue (filter.Name, out applicable)) {
					// Start from the end because interface members are in reverse order
					for (int i = applicable.Count - 1; i >= 0; i--) {
						var entry = applicable [i];

						if ((restrictions & BindingRestriction.InstanceOnly) != 0 && entry.IsStatic)
							continue;

						if ((restrictions & BindingRestriction.NoAccessors) != 0 && entry.IsAccessor)
							continue;

						if ((restrictions & BindingRestriction.OverrideOnly) != 0 && (entry.Modifiers & Modifiers.OVERRIDE) == 0)
							continue;

						if (!filter.Equals (entry))
							continue;

						if ((restrictions & BindingRestriction.DeclaredOnly) != 0 && container.IsInterface && entry.DeclaringType != container)
							continue;

						return entry;
					}
				}

				if ((restrictions & BindingRestriction.DeclaredOnly) != 0)
					break;

				container = container.BaseType;
			} while (container != null);

			return null;
		}

		//
		// A special method to work with member lookup only. It returns a list of all members named @name
		// starting from @container. It's very performance sensitive
		//
		// declaredOnlyClass cannot be used interfaces. Manual filtering is required because names are
		// compacted
		//
		public static IList<MemberSpec> FindMembers (TypeSpec container, string name, bool declaredOnlyClass)
		{
			IList<MemberSpec> applicable;

			do {
				if (container.MemberCache.member_hash.TryGetValue (name, out applicable) || declaredOnlyClass)
					return applicable;

				container = container.BaseType;
			} while (container != null);

			return null;
		}

		//
		// Finds the nested type in container
		//
		public static TypeSpec FindNestedType (TypeSpec container, string name, int arity)
		{
			IList<MemberSpec> applicable;
			TypeSpec best_match = null;
			do {
				// TODO: Don't know how to handle this yet
				// When resolving base type of nested type, parent type must have
				// base type resolved to scan full hierarchy correctly
				// Similarly MemberCacheTypes will inflate BaseType and Interfaces
				// based on type definition
				var tc = container.MemberDefinition as TypeContainer;
				if (tc != null)
					tc.DefineContainer ();

				if (container.MemberCacheTypes.member_hash.TryGetValue (name, out applicable)) {
					for (int i = applicable.Count - 1; i >= 0; i--) {
						var entry = applicable[i];
						if ((entry.Kind & MemberKind.NestedMask) == 0)
							continue;

						var ts = (TypeSpec) entry;
						if (arity == ts.Arity)
							return ts;

						if (arity < 0) {
							if (best_match == null) {
								best_match = ts;
							} else if (System.Math.Abs (ts.Arity + arity) < System.Math.Abs (ts.Arity + arity)) {
								best_match = ts;
							}
						}
					}
				}

				container = container.BaseType;
			} while (container != null);

			return best_match;
		}

		//
		// Looks for extension methods with defined name and extension type
		//
		public List<MethodSpec> FindExtensionMethods (IMemberContext invocationContext, TypeSpec extensionType, string name, int arity)
		{
			IList<MemberSpec> entries;
			if (!member_hash.TryGetValue (name, out entries))
				return null;

			List<MethodSpec> candidates = null;
			foreach (var entry in entries) {
				if (entry.Kind != MemberKind.Method || (arity > 0 && entry.Arity != arity))
					continue;

				var ms = (MethodSpec) entry;
				if (!ms.IsExtensionMethod)
					continue;

				if (!ms.IsAccessible (invocationContext))
					continue;

				//
				// Extension methods cannot be nested hence checking parent is enough
				//
				if ((ms.DeclaringType.Modifiers & Modifiers.INTERNAL) != 0 && !ms.DeclaringType.MemberDefinition.IsInternalAsPublic (invocationContext.Module.DeclaringAssembly))
					continue;

				if (candidates == null)
					candidates = new List<MethodSpec> ();
				candidates.Add (ms);
			}

			return candidates;
		}

		//
		// Returns base members of @member member if no exact match is found @bestCandidate returns
		// the best match
		//
		public static MemberSpec FindBaseMember (MemberCore member, out MemberSpec bestCandidate, ref bool overrides)
		{
			bestCandidate = null;
			var container = member.Parent.PartialContainer.Definition;
			if (!container.IsInterface) {
				container = container.BaseType;

				// It can happen for a user definition of System.Object
				if (container == null)
					return null;
			}

			string name = GetLookupName (member);
			var member_param = member is IParametersMember ? ((IParametersMember) member).Parameters : null;

			var mkind = GetMemberCoreKind (member);
			bool member_with_accessors = mkind == MemberKind.Indexer || mkind == MemberKind.Property;

			IList<MemberSpec> applicable;
			MemberSpec ambig_candidate = null;

			do {
				if (container.MemberCache.member_hash.TryGetValue (name, out applicable)) {
					for (int i = 0; i < applicable.Count; ++i) {
						var entry = applicable [i];

						if ((entry.Modifiers & Modifiers.PRIVATE) != 0)
							continue;

						if ((entry.Modifiers & Modifiers.AccessibilityMask) == Modifiers.INTERNAL &&
							!entry.DeclaringType.MemberDefinition.IsInternalAsPublic (member.Module.DeclaringAssembly))
							continue;

						//
						// Isn't the member of same kind ?
						//
						if ((entry.Kind & ~MemberKind.Destructor & mkind & MemberKind.MaskType) == 0) {
							// Destructors are ignored as they cannot be overridden by user
							if ((entry.Kind & MemberKind.Destructor) != 0)
								continue;

							// A method with different arity does not hide base member
							if (mkind != MemberKind.Method && member.MemberName.Arity != entry.Arity)
								continue;

							bestCandidate = entry;
							return null;
						}

						//
						// Same kind of different arity is valid
						//
						if (member.MemberName.Arity != entry.Arity) {
							continue;
						}

						if ((entry.Kind & mkind & (MemberKind.Method | MemberKind.Indexer)) != 0) {
							if (entry.IsAccessor != member is AbstractPropertyEventMethod)
								continue;

							var pm = entry as IParametersMember;
							if (!TypeSpecComparer.Override.IsEqual (pm.Parameters, member_param))
								continue;
						}

						//
						// Skip override for member with accessors. It may not fully implement the base member
						// but keep flag we found an implementation in case the base member is abstract
						//
						if (member_with_accessors && ((entry.Modifiers & (Modifiers.OVERRIDE | Modifiers.SEALED)) == Modifiers.OVERRIDE)) {
							//
							// Set candidate to override implementation to flag we found an implementation
							//
							overrides = true;
							continue;
						}

						//
						// For members with parameters we can encounter an ambiguous candidates (they match exactly)
						// because generic type parameters could be inflated into same types
						//
						if (ambig_candidate == null && (entry.Kind & mkind & (MemberKind.Method | MemberKind.Indexer)) != 0) {
							bestCandidate = null;
							ambig_candidate = entry;
							continue;
						}

						bestCandidate = ambig_candidate;
						return entry;
					}
				}

				if (container.IsInterface || ambig_candidate != null)
					break;

				container = container.BaseType;
			} while (container != null);

			return ambig_candidate;
		}

		//
		// Returns inflated version of MemberSpec, it works similarly to
		// SRE TypeBuilder.GetMethod
		//
		public static T GetMember<T> (TypeSpec container, T spec) where T : MemberSpec
		{
			IList<MemberSpec> applicable;
			if (container.MemberCache.member_hash.TryGetValue (GetLookupName (spec), out applicable)) {
				for (int i = applicable.Count - 1; i >= 0; i--) {
					var entry = applicable[i];
					if (entry.MemberDefinition == spec.MemberDefinition)
						return (T) entry;
				}
			}

			throw new InternalErrorException ("Missing member `{0}' on inflated type `{1}'",
				spec.GetSignatureForError (), container.GetSignatureForError ());
		}

		static MemberKind GetMemberCoreKind (MemberCore member)
		{
			if (member is FieldBase)
				return MemberKind.Field;
			if (member is Indexer)
				return MemberKind.Indexer;
			if (member is Class)
				return MemberKind.Class;
			if (member is Struct)
				return MemberKind.Struct;
			if (member is Destructor)
				return MemberKind.Destructor;
			if (member is Method)
				return MemberKind.Method;
			if (member is Property)
				return MemberKind.Property;
			if (member is EventField)
				return MemberKind.Event;
			if (member is Interface)
				return MemberKind.Interface;
			if (member is EventProperty)
				return MemberKind.Event;
			if (member is Delegate)
				return MemberKind.Delegate;
			if (member is Enum)
				return MemberKind.Enum;

			throw new NotImplementedException (member.GetType ().ToString ());
		}

		public static List<FieldSpec> GetAllFieldsForDefiniteAssignment (TypeSpec container)
		{
			List<FieldSpec> fields = null;
			foreach (var entry in container.MemberCache.member_hash) {
				foreach (var name_entry in entry.Value) {
					if (name_entry.Kind != MemberKind.Field)
						continue;

					if ((name_entry.Modifiers & Modifiers.STATIC) != 0)
						continue;

					//
					// Fixed size buffers are not subject to definite assignment checking
					//
					if (name_entry is FixedFieldSpec || name_entry is ConstSpec)
						continue;

					var fs = (FieldSpec) name_entry;

					//
					// LAMESPEC: Very bizzare hack, definitive assignment is not done
					// for imported non-public reference fields except array. No idea what the
					// actual csc rule is
					//
					if (!fs.IsPublic && container.MemberDefinition.IsImported && (!fs.MemberType.IsArray && TypeSpec.IsReferenceType (fs.MemberType)))
						continue;

					if (fields == null)
						fields = new List<FieldSpec> ();

					fields.Add (fs);
					break;
				}
			}

			return fields ?? new List<FieldSpec> (0);
		}

		public static IList<MemberSpec> GetCompletitionMembers (IMemberContext ctx, TypeSpec container, string name)
		{
			var matches = new List<MemberSpec> ();
			foreach (var entry in container.MemberCache.member_hash) {
				foreach (var name_entry in entry.Value) {
					if (name_entry.IsAccessor)
						continue;

					if ((name_entry.Kind & (MemberKind.Constructor | MemberKind.Destructor | MemberKind.Operator)) != 0)
						continue;

					if (!name_entry.IsAccessible (ctx))
						continue;

					if (name == null || name_entry.Name.StartsWith (name)) {
						matches.Add (name_entry);
					}
				}
			}

			return matches;
		}

		//
		// Returns members of @iface only, base members are ignored
		//
		public static List<MethodSpec> GetInterfaceMethods (TypeSpec iface)
		{
			//
			// MemberCache flatten interfaces, therefore in cases like this one
			// 
			// interface IA : IB {}
			// interface IB { void Foo () }
			//
			// we would return Foo inside IA which is not expected in this case
			//
			var methods = new List<MethodSpec> ();
			foreach (var entry in iface.MemberCache.member_hash.Values) {
				foreach (var name_entry in entry) {
					if (iface == name_entry.DeclaringType) {
						if (name_entry.Kind == MemberKind.Method) {
							methods.Add ((MethodSpec) name_entry);
						}
					}
				}
			}

			return methods;
		}

		//
		// Returns all not implememted abstract members inside abstract type
		// NOTE: Returned list is shared and must not be modified
		//
		public static IList<MethodSpec> GetNotImplementedAbstractMethods (TypeSpec type)
		{
			if (type.MemberCache.missing_abstract != null)
				return type.MemberCache.missing_abstract;
				
			var abstract_methods = new List<MethodSpec> ();
			List<TypeSpec> hierarchy = null;

			//
			// Stage 1: top-to-bottom scan for abstract members
			//
			var abstract_type = type;
			while (true) {
				foreach (var entry in abstract_type.MemberCache.member_hash) {
					foreach (var name_entry in entry.Value) {
						if ((name_entry.Modifiers & (Modifiers.ABSTRACT | Modifiers.OVERRIDE)) != Modifiers.ABSTRACT)
							continue;

						if (name_entry.Kind != MemberKind.Method)
							continue;

						abstract_methods.Add ((MethodSpec) name_entry);
					}
				}

				var base_type = abstract_type.BaseType;
				if (!base_type.IsAbstract)
					break;

				if (hierarchy == null)
					hierarchy = new List<TypeSpec> ();

				hierarchy.Add (abstract_type);
				abstract_type = base_type;
			}

			int not_implemented_count = abstract_methods.Count;
			if (not_implemented_count == 0 || hierarchy == null) {
				type.MemberCache.missing_abstract = abstract_methods;
				return type.MemberCache.missing_abstract;
			}

			//
			// Stage 2: Remove already implemented methods
			//
			foreach (var type_up in hierarchy) {
				var members = type_up.MemberCache.member_hash;
				if (members.Count == 0)
					continue;

				for (int i = 0; i < abstract_methods.Count; ++i) {
					var candidate = abstract_methods [i];
					if (candidate == null)
						continue;

					IList<MemberSpec> applicable;
					if (!members.TryGetValue (candidate.Name, out applicable))
						continue;

					var filter = new MemberFilter (candidate);
					foreach (var item in applicable) {
						if ((item.Modifiers & (Modifiers.OVERRIDE | Modifiers.VIRTUAL)) == 0)
							continue;

						//
						// Abstract override does not override anything
						//
						if ((item.Modifiers & Modifiers.ABSTRACT) != 0)
							continue;

						if (filter.Equals (item)) {
							--not_implemented_count;
							abstract_methods [i] = null;
							break;
						}
					}
				}
			}

			if (not_implemented_count == abstract_methods.Count) {
				type.MemberCache.missing_abstract = abstract_methods;
				return type.MemberCache.missing_abstract;
			}

			var not_implemented = new MethodSpec[not_implemented_count];
			int counter = 0;
			foreach (var m in abstract_methods) {
				if (m == null)
					continue;

				not_implemented[counter++] = m;
			}

			type.MemberCache.missing_abstract = not_implemented;
			return type.MemberCache.missing_abstract;
		}

		static string GetLookupName (MemberSpec ms)
		{
			if (ms.Kind == MemberKind.Indexer)
				return IndexerNameAlias;

			if (ms.Kind == MemberKind.Constructor) {
				if (ms.IsStatic)
					return Constructor.TypeConstructorName;

				return Constructor.ConstructorName;
			}

			return ms.Name;
		}

		static string GetLookupName (MemberCore mc)
		{
			if (mc is Indexer)
				return IndexerNameAlias;

			if (mc is Constructor)
				return mc.IsStatic ? Constructor.TypeConstructorName : Constructor.ConstructorName;

			return mc.MemberName.Name;
		}

		//
		// Returns all operators declared on container and its base types (until declaredOnly is used)
		//
		public static IList<MemberSpec> GetUserOperator (TypeSpec container, Operator.OpType op, bool declaredOnly)
		{
			IList<MemberSpec> found = null;

			IList<MemberSpec> applicable;
			do {
				var mc = container.MemberCache;

				if (((op == Operator.OpType.Implicit || op == Operator.OpType.Explicit) && (mc.state & StateFlags.HasConversionOperator) != 0) ||
					 (mc.state & StateFlags.HasUserOperator) != 0) {

					if (mc.member_hash.TryGetValue (Operator.GetMetadataName (op), out applicable)) {
						int i;
						for (i = 0; i < applicable.Count; ++i) {
							if (applicable[i].Kind != MemberKind.Operator) {
								break;
							}
						}

						//
						// Handles very rare case where a method with same name as operator (op_xxxx) exists
						// and we have to resize the applicable list
						//
						if (i != applicable.Count) {
							for (i = 0; i < applicable.Count; ++i) {
								if (applicable[i].Kind != MemberKind.Operator) {
									continue;
								}

								if (found == null) {
									found = new List<MemberSpec> ();
									found.Add (applicable[i]);
								} else {
									var prev = found as List<MemberSpec>;
									if (prev == null) {
										prev = new List<MemberSpec> (found.Count + 1);
										prev.AddRange (found);
									}

									prev.Add (applicable[i]);
								}
							}
						} else {
							if (found == null) {
								found = applicable;
							} else {
								var merged = found as List<MemberSpec>;
								if (merged == null) {
									merged = new List<MemberSpec> (found.Count + applicable.Count);
									merged.AddRange (found);
									found = merged;
								}

								merged.AddRange (applicable);
							}
						}
					}
				}

				// BaseType call can be expensive
				if (declaredOnly)
					break;

				container = container.BaseType;
			} while (container != null);

			return found;
		}

		//
		// Inflates all member cache nested types
		//
		public void InflateTypes (MemberCache inflated_cache, TypeParameterInflator inflator)
		{
			foreach (var item in member_hash) {
				IList<MemberSpec> inflated_members = null;
				for (int i = 0; i < item.Value.Count; ++i ) {
					var member = item.Value[i];

					// FIXME: When inflating members refering nested types before they are inflated
					if (member == null)
						continue;

					if ((member.Kind & MemberKind.NestedMask) != 0 &&
						(member.Modifiers & Modifiers.COMPILER_GENERATED) == 0) {
						if (inflated_members == null) {
							inflated_members = new MemberSpec[item.Value.Count];
							inflated_cache.member_hash.Add (item.Key, inflated_members);
						}

						inflated_members [i] = member.InflateMember (inflator);
					}
				}
			}
		}

		//
		// Inflates all open type members, requires InflateTypes to be called before
		//
		public void InflateMembers (MemberCache cacheToInflate, TypeSpec inflatedType, TypeParameterInflator inflator)
		{
			var inflated_member_hash = cacheToInflate.member_hash;
			Dictionary<MemberSpec, MethodSpec> accessor_relation = null;
			List<MemberSpec> accessor_members = null;

			// Copy member specific flags when all members were added
			cacheToInflate.state = state;

			foreach (var item in member_hash) {
				var members = item.Value;
				IList<MemberSpec> inflated_members = null;
				for (int i = 0; i < members.Count; ++i ) {
					var member = members[i];

					//
					// All nested types have been inflated earlier except for
					// compiler types which are created later and could miss InflateTypes
					//
					if ((member.Kind & MemberKind.NestedMask) != 0 &&
						(member.Modifiers & Modifiers.COMPILER_GENERATED) == 0) {
						if (inflated_members == null)
							inflated_members = inflated_member_hash[item.Key];

						continue;
					}

					//
					// Clone the container first
					//
					if (inflated_members == null) {
						inflated_members = new MemberSpec [item.Value.Count];
						inflated_member_hash.Add (item.Key, inflated_members);
					}

					var local_inflator = inflator;

					if (member.DeclaringType != inflatedType) {
						//
						// Don't inflate top-level non-generic interface members
						// merged into generic interface
						//
						if (!member.DeclaringType.IsGeneric && !member.DeclaringType.IsNested) {
							inflated_members [i] = member;
							continue;
						}

						//
						// Needed when inflating flatten interfaces. It inflates
						// container type only, type parameters are already done
						//
						// Handles cases like:
						//
						// interface I<T> {}
						// interface I<U, V> : I<U> {}
						// 
						// class C: I<int, bool> {}
						//
						var inflated_parent = inflator.Inflate (member.DeclaringType);
						if (inflated_parent != inflator.TypeInstance)
							local_inflator = new TypeParameterInflator (inflator, inflated_parent);
					}

					//
					// Inflate every member, its parent is now different
					//
					var inflated = member.InflateMember (local_inflator);
					inflated_members [i] = inflated;

					if (member is PropertySpec || member is EventSpec) {
						if (accessor_members == null)
							accessor_members = new List<MemberSpec> ();

						accessor_members.Add (inflated);
						continue;
					}

					if (member.IsAccessor) {
						if (accessor_relation == null)
							accessor_relation = new Dictionary<MemberSpec, MethodSpec> ();
						accessor_relation.Add (member, (MethodSpec) inflated);
					}
				}
			}

			if (accessor_members != null) {
				foreach (var member in accessor_members) {
					var prop = member as PropertySpec;
					if (prop != null) {
						if (prop.Get != null)
							prop.Get = accessor_relation[prop.Get];
						if (prop.Set != null)
							prop.Set = accessor_relation[prop.Set];

						continue;
					}

					var ev = (EventSpec) member;
					ev.AccessorAdd = accessor_relation[ev.AccessorAdd];
					ev.AccessorRemove = accessor_relation[ev.AccessorRemove];
				}
			}
		}

		//
		// Removes hidden base members of an interface. For compiled interfaces we cannot
		// do name filtering during Add (as we do for import) because we need all base
		// names to be valid during type definition.
		// Add replaces hidden base member with current one which means any name collision
		// (CS0108) of non-first name would be unnoticed because the name was replaced
		// with the one from compiled type
		//
		public void RemoveHiddenMembers (TypeSpec container)
		{
			foreach (var entry in member_hash) {
				var values = entry.Value;

				int container_members_start_at = 0;
				while (values[container_members_start_at].DeclaringType != container && ++container_members_start_at < entry.Value.Count);

				if (container_members_start_at == 0 || container_members_start_at == values.Count)
					continue;

				for (int i = 0; i < container_members_start_at; ++i) {
					var member = values[i];

					if (!container.ImplementsInterface (member.DeclaringType, false))
						continue;

					var member_param = member is IParametersMember ? ((IParametersMember) member).Parameters : ParametersCompiled.EmptyReadOnlyParameters;

					for (int ii = container_members_start_at; ii < values.Count; ++ii) {
						var container_entry = values[ii];

						if (container_entry.Arity != member.Arity)
							continue;

						if (container_entry is IParametersMember) {
							if (!TypeSpecComparer.Override.IsEqual (((IParametersMember) container_entry).Parameters, member_param))
								continue;
						}

						values.RemoveAt (i);
						--container_members_start_at;
						--ii;
						--i;
					}
				}
			}
		}

		//
		// Checks all appropriate container members for CLS compliance
		//
		public void VerifyClsCompliance (TypeSpec container, Report report)
		{
			if (locase_members != null)
				return;

			if (container.BaseType == null) {
				locase_members = new Dictionary<string, MemberSpec[]> (member_hash.Count); // StringComparer.OrdinalIgnoreCase);
			} else {
				var btype = container.BaseType.GetDefinition ();
				btype.MemberCache.VerifyClsCompliance (btype, report);
				locase_members = new Dictionary<string, MemberSpec[]> (btype.MemberCache.locase_members); //, StringComparer.OrdinalIgnoreCase);
			}

			var is_imported_type = container.MemberDefinition.IsImported;
			foreach (var entry in container.MemberCache.member_hash) {
				for (int i = 0; i < entry.Value.Count; ++i ) {
					var name_entry = entry.Value[i];
					if ((name_entry.Modifiers & (Modifiers.PUBLIC | Modifiers.PROTECTED)) == 0)
						continue;

					if ((name_entry.Modifiers & (Modifiers.OVERRIDE | Modifiers.COMPILER_GENERATED)) != 0)
						continue;

					if ((name_entry.Kind & MemberKind.MaskType) == 0)
						continue;

					if (name_entry.MemberDefinition.CLSAttributeValue == false)
					    continue;

					IParametersMember p_a = null;
					if (!is_imported_type) {
						p_a = name_entry as IParametersMember;
						if (p_a != null && !name_entry.IsAccessor) {
							var p_a_pd = p_a.Parameters;
							//
							// Check differing overloads in @container
							//
							for (int ii = i + 1; ii < entry.Value.Count; ++ii) {
								var checked_entry = entry.Value[ii];
								IParametersMember p_b = checked_entry as IParametersMember;
								if (p_b == null)
									continue;

								if (p_a_pd.Count != p_b.Parameters.Count)
									continue;

								if (checked_entry.IsAccessor)
									continue;

								var res = ParametersCompiled.IsSameClsSignature (p_a.Parameters, p_b.Parameters);
								if (res != 0) {
									ReportOverloadedMethodClsDifference (name_entry, checked_entry, res, report);
								}
							}
						}
					}

					if (i > 0 || name_entry.Kind == MemberKind.Constructor || name_entry.Kind == MemberKind.Indexer)
						continue;

					var name_entry_locase = name_entry.Name.ToLowerInvariant ();

					MemberSpec[] found;
					if (!locase_members.TryGetValue (name_entry_locase, out found)) {
						found = new MemberSpec[] { name_entry };
						locase_members.Add (name_entry_locase, found);
					} else {
						bool same_names_only = true;
						foreach (var f in found) {
							if (f.Name == name_entry.Name) {
								if (p_a != null) {
									IParametersMember p_b = f as IParametersMember;
									if (p_b == null)
										continue;

									if (p_a.Parameters.Count != p_b.Parameters.Count)
										continue;

									if (f.IsAccessor)
										continue;

									var res = ParametersCompiled.IsSameClsSignature (p_a.Parameters, p_b.Parameters);
									if (res != 0) {
										ReportOverloadedMethodClsDifference (f, name_entry, res, report);
									}
								}

								continue;
							}

							same_names_only = false;
							if (!is_imported_type) {
								var last = GetLaterDefinedMember (f, name_entry);
								if (last == f.MemberDefinition) {
									report.SymbolRelatedToPreviousError (name_entry);
								} else {
									report.SymbolRelatedToPreviousError (f);
								}

								report.Warning (3005, 1, last.Location,
									"Identifier `{0}' differing only in case is not CLS-compliant", last.GetSignatureForError ());
							}
						}

						if (!same_names_only) {
							Array.Resize (ref found, found.Length + 1);
							found[found.Length - 1] = name_entry;
							locase_members[name_entry_locase] = found;
						}
					}
				}
			}
		}

		//
		// Local report helper to issue correctly ordered members stored in hashtable
		//
		static MemberCore GetLaterDefinedMember (MemberSpec a, MemberSpec b)
		{
			var mc_a = a.MemberDefinition as MemberCore;
			var mc_b = b.MemberDefinition as MemberCore;
			if (mc_a == null)
				return mc_b;

			if (mc_b == null)
				return mc_a;

			if (a.DeclaringType.MemberDefinition != b.DeclaringType.MemberDefinition)
				return mc_b;

			if (mc_a.Location.File != mc_a.Location.File)
				return mc_b;

			return mc_b.Location.Row > mc_a.Location.Row ? mc_b : mc_a;
		}

		static void ReportOverloadedMethodClsDifference (MemberSpec a, MemberSpec b, int res, Report report)
		{
			var last = GetLaterDefinedMember (a, b);
			if (last == a.MemberDefinition) {
				report.SymbolRelatedToPreviousError (b);
			} else {
				report.SymbolRelatedToPreviousError (a);
			}

			if ((res & 1) != 0) {
				report.Warning (3006, 1, last.Location,
						"Overloaded method `{0}' differing only in ref or out, or in array rank, is not CLS-compliant",
						last.GetSignatureForError ());
			}

			if ((res & 2) != 0) {
				report.Warning (3007, 1, last.Location,
					"Overloaded method `{0}' differing only by unnamed array types is not CLS-compliant",
					last.GetSignatureForError ());
			}
		}

		public bool CheckExistingMembersOverloads (MemberCore member, AParametersCollection parameters)
		{
			var name = GetLookupName (member);
			var imb = member as InterfaceMemberBase;
			if (imb != null && imb.IsExplicitImpl) {
				name = imb.GetFullName (name);
			}

			return CheckExistingMembersOverloads (member, name, parameters);
		}

		public bool CheckExistingMembersOverloads (MemberCore member, string name, AParametersCollection parameters)
		{
			IList<MemberSpec> entries;
			if (!member_hash.TryGetValue (name, out entries))
				return false;

			var Report = member.Compiler.Report;

			int method_param_count = parameters.Count;
			for (int i = entries.Count - 1; i >= 0; --i) {
				var ce = entries[i];
				var pm = ce as IParametersMember;
				var pd = pm == null ? ParametersCompiled.EmptyReadOnlyParameters : pm.Parameters;
				if (pd.Count != method_param_count)
					continue;

				if (ce.Arity != member.MemberName.Arity)
					continue;

				// Ignore merged interface members
				if (member.Parent.PartialContainer != ce.DeclaringType.MemberDefinition)
					continue;

				var p_types = pd.Types;
				if (method_param_count > 0) {
					int ii = method_param_count - 1;
					TypeSpec type_a, type_b;
					do {
						type_a = parameters.Types [ii];
						type_b = p_types [ii];

						var a_byref = (pd.FixedParameters[ii].ModFlags & Parameter.Modifier.RefOutMask) != 0;
						var b_byref = (parameters.FixedParameters[ii].ModFlags & Parameter.Modifier.RefOutMask) != 0;

						if (a_byref != b_byref)
							break;

					} while (TypeSpecComparer.Override.IsEqual (type_a, type_b) && ii-- != 0);

					if (ii >= 0)
						continue;

					//
					// Operators can differ in return type only
					//
					if (member is Operator && ce.Kind == MemberKind.Operator && ((MethodSpec) ce).ReturnType != ((Operator) member).ReturnType)
						continue;

					//
					// Report difference in parameter modifiers only
					//
					if (pd != null && member is MethodCore) {
						ii = method_param_count;
						while (ii-- != 0 &&
							(parameters.FixedParameters[ii].ModFlags & Parameter.Modifier.ModifierMask) ==
							(pd.FixedParameters[ii].ModFlags & Parameter.Modifier.ModifierMask) &&
							parameters.ExtensionMethodType == pd.ExtensionMethodType) ;

						if (ii >= 0) {
							var mc = ce as MethodSpec;
							member.Compiler.Report.SymbolRelatedToPreviousError (ce);
							if ((member.ModFlags & Modifiers.PARTIAL) != 0 && (mc.Modifiers & Modifiers.PARTIAL) != 0) {
								if (parameters.HasParams || pd.HasParams) {
									Report.Error (758, member.Location,
										"A partial method declaration and partial method implementation cannot differ on use of `params' modifier");
								} else {
									Report.Error (755, member.Location,
										"A partial method declaration and partial method implementation must be both an extension method or neither");
								}
							} else if (member is Constructor) {
								Report.Error (851, member.Location,
									"Overloaded contructor `{0}' cannot differ on use of parameter modifiers only",
									member.GetSignatureForError ());
							} else {
								Report.Error (663, member.Location,
									"Overloaded method `{0}' cannot differ on use of parameter modifiers only",
									member.GetSignatureForError ());
							}
							return false;
						}
					}
				}

				if ((ce.Kind & MemberKind.Method) != 0) {
					Method method_a = member as Method;
					Method method_b = ce.MemberDefinition as Method;
					if (method_a != null && method_b != null && (method_a.ModFlags & method_b.ModFlags & Modifiers.PARTIAL) != 0) {
						const Modifiers partial_modifiers = Modifiers.STATIC | Modifiers.UNSAFE;
						if (method_a.IsPartialDefinition == method_b.IsPartialImplementation) {
							if ((method_a.ModFlags & partial_modifiers) == (method_b.ModFlags & partial_modifiers) ||
								method_a.Parent.IsUnsafe && method_b.Parent.IsUnsafe) {
								if (method_a.IsPartialImplementation) {
									method_a.SetPartialDefinition (method_b);
									if (entries.Count == 1)
										member_hash.Remove (name);
									else
										entries.RemoveAt (i);
								} else {
									method_b.SetPartialDefinition (method_a);
									method_a.caching_flags |= MemberCore.Flags.PartialDefinitionExists;
								}
								continue;
							}

							if (method_a.IsStatic != method_b.IsStatic) {
								Report.SymbolRelatedToPreviousError (ce);
								Report.Error (763, member.Location,
									"A partial method declaration and partial method implementation must be both `static' or neither");
							}

							Report.SymbolRelatedToPreviousError (ce);
							Report.Error (764, member.Location,
								"A partial method declaration and partial method implementation must be both `unsafe' or neither");
							return false;
						}

						Report.SymbolRelatedToPreviousError (ce);
						if (method_a.IsPartialDefinition) {
							Report.Error (756, member.Location, "A partial method `{0}' declaration is already defined",
								member.GetSignatureForError ());
						}

						Report.Error (757, member.Location, "A partial method `{0}' implementation is already defined",
							member.GetSignatureForError ());
						return false;
					}

					Report.SymbolRelatedToPreviousError (ce);

					bool is_reserved_a = member is AbstractPropertyEventMethod || member is Operator;
					bool is_reserved_b = ((MethodSpec) ce).IsReservedMethod;

					if (is_reserved_a || is_reserved_b) {
						Report.Error (82, member.Location, "A member `{0}' is already reserved",
							is_reserved_a ?
							ce.GetSignatureForError () :
							member.GetSignatureForError ());
						return false;
					}
				} else {
					Report.SymbolRelatedToPreviousError (ce);
				}

				if (member is Operator && ce.Kind == MemberKind.Operator) {
					Report.Error (557, member.Location, "Duplicate user-defined conversion in type `{0}'",
						member.Parent.GetSignatureForError ());
					return false;
				}

				Report.Error (111, member.Location,
					"A member `{0}' is already defined. Rename this member or use different parameter types",
					member.GetSignatureForError ());
				return false;
			}

			return true;
		}
	}
}
