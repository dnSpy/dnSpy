//
// class.cs: Class and Struct handlers
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Martin Baulig (martin@ximian.com)
//          Marek Safar (marek.safar@seznam.cz)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002, 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Linq;

#if NET_2_1
using XmlElement = System.Object;
#endif

#if STATIC
using SecurityType = System.Collections.Generic.List<IKVM.Reflection.Emit.CustomAttributeBuilder>;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using SecurityType = System.Collections.Generic.Dictionary<System.Security.Permissions.SecurityAction, System.Security.PermissionSet>;
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{

	public interface ITypesContainer
	{
		Location Location { get; }
		MemberName MemberName { get; }
	}

	/// <summary>
	///   This is the base class for structs and classes.  
	/// </summary>
	public abstract class TypeContainer : DeclSpace, ITypeDefinition, ITypesContainer
	{
		//
		// Different context is needed when resolving type container base
		// types. Type names come from the parent scope but type parameter
		// names from the container scope.
		//
		public struct BaseContext : IMemberContext
		{
			TypeContainer tc;

			public BaseContext (TypeContainer tc)
			{
				this.tc = tc;
			}

			#region IMemberContext Members

			public CompilerContext Compiler {
				get { return tc.Compiler; }
			}

			public TypeSpec CurrentType {
				get { return tc.Parent.CurrentType; }
			}

			public TypeParameter[] CurrentTypeParameters {
				get { return tc.PartialContainer.CurrentTypeParameters; }
			}

			public MemberCore CurrentMemberDefinition {
				get { return tc; }
			}

			public bool IsObsolete {
				get { return tc.IsObsolete; }
			}

			public bool IsUnsafe {
				get { return tc.IsUnsafe; }
			}

			public bool IsStatic {
				get { return tc.IsStatic; }
			}

			public ModuleContainer Module {
				get { return tc.Module; }
			}

			public string GetSignatureForError ()
			{
				return tc.GetSignatureForError ();
			}

			public IList<MethodSpec> LookupExtensionMethod (TypeSpec extensionType, string name, int arity, ref NamespaceContainer scope)
			{
				return null;
			}

			public FullNamedExpression LookupNamespaceAlias (string name)
			{
				return tc.Parent.LookupNamespaceAlias (name);
			}

			public FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc)
			{
				if (arity == 0) {
					TypeParameter[] tp = CurrentTypeParameters;
					if (tp != null) {
						TypeParameter t = TypeParameter.FindTypeParameter (tp, name);
						if (t != null)
							return new TypeParameterExpr (t, loc);
					}
				}

				return tc.Parent.LookupNamespaceOrType (name, arity, mode, loc);
			}

			#endregion
		}

		[Flags]
		enum CachedMethods
		{
			Equals				= 1,
			GetHashCode			= 1 << 1,
			HasStaticFieldInitializer	= 1 << 2
		}


		// Whether this is a struct, class or interface
		public readonly MemberKind Kind;

		// Holds a list of classes and structures
		protected List<TypeContainer> types;

		List<MemberCore> ordered_explicit_member_list;
		List<MemberCore> ordered_member_list;

		// Holds the list of properties
		List<MemberCore> properties;

		// Holds the list of constructors
		protected List<Constructor> instance_constructors;

		// Holds the list of fields
		protected List<FieldBase> fields;

		// Holds a list of fields that have initializers
		protected List<FieldInitializer> initialized_fields;

		// Holds a list of static fields that have initializers
		protected List<FieldInitializer> initialized_static_fields;

		// Holds the list of constants
		protected List<MemberCore> constants;

		// Holds the methods.
		List<MemberCore> methods;

		// Holds the events
		protected List<MemberCore> events;

		// Holds the indexers
		List<MemberCore> indexers;

		// Holds the operators
		List<MemberCore> operators;

		// Holds the compiler generated classes
		protected List<CompilerGeneratedClass> compiler_generated;

		Dictionary<MethodSpec, Method> hoisted_base_call_proxies;

		Dictionary<string, FullNamedExpression> Cache = new Dictionary<string, FullNamedExpression> ();

		//
		// Pointers to the default constructor and the default static constructor
		//
		protected Constructor default_constructor;
		protected Constructor default_static_constructor;

		//
		// Points to the first non-static field added to the container.
		//
		// This is an arbitrary choice.  We are interested in looking at _some_ non-static field,
		// and the first one's as good as any.
		//
		FieldBase first_nonstatic_field;

		//
		// This one is computed after we can distinguish interfaces
		// from classes from the arraylist `type_bases' 
		//
		protected TypeSpec base_type;
		protected FullNamedExpression base_type_expr;	// TODO: It's temporary variable
		protected TypeSpec[] iface_exprs;

		protected List<FullNamedExpression> type_bases;

		bool members_defined;
		bool members_defined_ok;
		bool type_defined;

		TypeContainer InTransit;

		GenericTypeParameterBuilder[] all_tp_builders;

		public const string DefaultIndexerName = "Item";

		private bool seen_normal_indexers = false;
		private string indexer_name = DefaultIndexerName;
		protected bool requires_delayed_unmanagedtype_check;
		bool error;

		private CachedMethods cached_method;

		protected TypeSpec spec;
		TypeSpec current_type;

		List<TypeContainer> partial_parts;

		public int DynamicSitesCounter;

		/// <remarks>
		///  The pending methods that need to be implemented
		//   (interfaces or abstract methods)
		/// </remarks>
		PendingImplementation pending;

		public TypeContainer (NamespaceContainer ns, DeclSpace parent, MemberName name,
				      Attributes attrs, MemberKind kind)
			: base (ns, parent, name, attrs)
		{
			if (parent != null && parent.NamespaceEntry != ns)
				throw new InternalErrorException ("A nested type should be in the same NamespaceEntry as its enclosing class");

			this.Kind = kind;
			this.PartialContainer = this;
		}

		List<MemberCore> orderedAllMembers = new List<MemberCore> ();
		public List<MemberCore> OrderedAllMembers {
			get {
				return this.orderedAllMembers; 
			}
		}

		#region Properties

		public override TypeSpec CurrentType {
			get {
				if (current_type == null) {
					if (IsGeneric) {
						//
						// Switch to inflated version as it's used by all expressions
						//
						var targs = CurrentTypeParameters == null ? TypeSpec.EmptyTypes : CurrentTypeParameters.Select (l => l.Type).ToArray ();
						current_type = spec.MakeGenericType (this, targs);
					} else {
						current_type = spec;
					}
				}

				return current_type;
			}
		}

		public override TypeParameter[] CurrentTypeParameters {
			get {
				return PartialContainer.type_params;
			}
		}

		int CurrentTypeParametersStartIndex {
			get {
				int total = all_tp_builders.Length;
				if (CurrentTypeParameters != null) {
					return total - CurrentTypeParameters.Length;
				}
				return total;
			}
		}

		public virtual AssemblyDefinition DeclaringAssembly {
			get {
				return Module.DeclaringAssembly;
			}
		}

		IAssemblyDefinition ITypeDefinition.DeclaringAssembly {
			get {
				return Module.DeclaringAssembly;
			}
		}

		public TypeSpec Definition {
			get {
				return spec;
			}
		}

		public bool HasMembersDefined {
			get {
				return members_defined;
			}
		}
		
		public List<FullNamedExpression> TypeBaseExpressions {
			get {
				return type_bases;
			}
		}

		#endregion

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}
		
		public bool AddMember (MemberCore symbol)
		{
			return AddToContainer (symbol, symbol.MemberName.Basename);
		}

		public bool AddMember (MemberCore symbol, string name)
		{
			return AddToContainer (symbol, name);
		}

		protected virtual bool AddMemberType (TypeContainer ds)
		{
			return AddToContainer (ds, ds.Basename);
		}

		protected virtual void RemoveMemberType (TypeContainer ds)
		{
			RemoveFromContainer (ds.Basename);
		}

		public void AddConstant (Const constant)
		{
			orderedAllMembers.Add (constant);
			if (!AddMember (constant))
				return;
			
			if (constants == null)
				constants = new List<MemberCore> ();
			constants.Add (constant);
		}

		public TypeContainer AddTypeContainer (TypeContainer tc)
		{
			orderedAllMembers.Add (tc);
			if (!AddMemberType (tc))
				return tc;

			if (types == null)
				types = new List<TypeContainer> ();
			
			types.Add (tc);
			return tc;
		}

		public virtual TypeContainer AddPartial (TypeContainer next_part)
		{
			return AddPartial (next_part, next_part.Basename);
		}

		protected TypeContainer AddPartial (TypeContainer next_part, string name)
		{
			next_part.ModFlags |= Modifiers.PARTIAL;
			TypeContainer tc = GetDefinition (name) as TypeContainer;
			if (tc == null)
				return AddTypeContainer (next_part);

			if ((tc.ModFlags & Modifiers.PARTIAL) == 0) {
				Report.SymbolRelatedToPreviousError (next_part);
				Error_MissingPartialModifier (tc);
			}

			if (tc.Kind != next_part.Kind) {
				Report.SymbolRelatedToPreviousError (tc);
				Report.Error (261, next_part.Location,
					"Partial declarations of `{0}' must be all classes, all structs or all interfaces",
					next_part.GetSignatureForError ());
			}

			if ((tc.ModFlags & Modifiers.AccessibilityMask) != (next_part.ModFlags & Modifiers.AccessibilityMask) &&
				((tc.ModFlags & Modifiers.DEFAULT_ACCESS_MODIFER) == 0 &&
				 (next_part.ModFlags & Modifiers.DEFAULT_ACCESS_MODIFER) == 0)) {
				Report.SymbolRelatedToPreviousError (tc);
				Report.Error (262, next_part.Location,
					"Partial declarations of `{0}' have conflicting accessibility modifiers",
					next_part.GetSignatureForError ());
			}

			if (tc.partial_parts == null)
				tc.partial_parts = new List<TypeContainer> (1);

			if ((next_part.ModFlags & Modifiers.DEFAULT_ACCESS_MODIFER) != 0) {
				tc.ModFlags |= next_part.ModFlags & ~(Modifiers.DEFAULT_ACCESS_MODIFER | Modifiers.AccessibilityMask);
			} else if ((tc.ModFlags & Modifiers.DEFAULT_ACCESS_MODIFER) != 0) {
				tc.ModFlags &= ~(Modifiers.DEFAULT_ACCESS_MODIFER | Modifiers.AccessibilityMask);
				tc.ModFlags |= next_part.ModFlags;
			} else {
				tc.ModFlags |= next_part.ModFlags;
			}

			tc.spec.Modifiers = tc.ModFlags;

			if (next_part.attributes != null) {
				if (tc.attributes == null)
					tc.attributes = next_part.attributes;
				else
					tc.attributes.AddAttributes (next_part.attributes.Attrs);
			}

			next_part.PartialContainer = tc;
			tc.partial_parts.Add (next_part);
			return tc;
		}

		public virtual void RemoveTypeContainer (TypeContainer next_part)
		{
			if (types != null)
				types.Remove (next_part);

			Cache.Remove (next_part.Basename);
			RemoveMemberType (next_part);
		}
		
		public void AddDelegate (Delegate d)
		{
			AddTypeContainer (d);
		}

		private void AddMemberToList (MemberCore mc, List<MemberCore> alist, bool isexplicit)
		{
			if (ordered_explicit_member_list == null)  {
				ordered_explicit_member_list = new List<MemberCore> ();
				ordered_member_list = new List<MemberCore> ();
			}

			if (isexplicit) {
				if (Kind == MemberKind.Interface) {
					Report.Error (541, mc.Location,
						"`{0}': explicit interface declaration can only be declared in a class or struct",
						mc.GetSignatureForError ());
				}

				ordered_explicit_member_list.Add (mc);
				alist.Insert (0, mc);
			} else {
				ordered_member_list.Add (mc);
				alist.Add (mc);
			}

		}
		
		public void AddMethod (MethodOrOperator method)
		{
			orderedAllMembers.Add (method);
			if (!AddToContainer (method, method.MemberName.Basename))
				return;
			
			if (methods == null)
				methods = new List<MemberCore> ();

			if (method.MemberName.Left != null) 
				AddMemberToList (method, methods, true);
			else 
				AddMemberToList (method, methods, false);
		}

		public void AddConstructor (Constructor c)
		{
			orderedAllMembers.Add (c);
			bool is_static = (c.ModFlags & Modifiers.STATIC) != 0;
			if (!AddToContainer (c, is_static ? Constructor.ConstructorName : Constructor.TypeConstructorName))
				return;
			
			if (is_static && c.ParameterInfo.IsEmpty){
				if (default_static_constructor != null) {
				    Report.SymbolRelatedToPreviousError (default_static_constructor);
					Report.Error (111, c.Location,
						"A member `{0}' is already defined. Rename this member or use different parameter types",
						c.GetSignatureForError ());
				    return;
				}

				default_static_constructor = c;
			} else {
				if (c.ParameterInfo.IsEmpty)
					default_constructor = c;
				if (instance_constructors == null)
					instance_constructors = new List<Constructor> ();
				
				instance_constructors.Add (c);
			}
		}

		public bool AddField (FieldBase field)
		{
			orderedAllMembers.Add (field);
			if (!AddMember (field))
				return false;
			if (fields == null)
				fields = new List<FieldBase> ();

			fields.Add (field);

			if ((field.ModFlags & Modifiers.STATIC) != 0)
				return true;

			if (first_nonstatic_field == null) {
				first_nonstatic_field = field;
				return true;
			}

			if (Kind == MemberKind.Struct && first_nonstatic_field.Parent != field.Parent) {
				Report.SymbolRelatedToPreviousError (first_nonstatic_field.Parent);
				Report.Warning (282, 3, field.Location,
					"struct instance field `{0}' found in different declaration from instance field `{1}'",
					field.GetSignatureForError (), first_nonstatic_field.GetSignatureForError ());
			}
			return true;
		}

		public void AddProperty (Property prop)
		{
			orderedAllMembers.Add (prop);
			if (!AddMember (prop))
				return;
			if (properties == null)
				properties = new List<MemberCore> ();

			if (prop.MemberName.Left != null)
				AddMemberToList (prop, properties, true);
			else 
				AddMemberToList (prop, properties, false);
		}

		public void AddEvent (Event e)
		{
			orderedAllMembers.Add (e);
			if (!AddMember (e))
				return;
			if (events == null)
				events = new List<MemberCore> ();

			events.Add (e);
		}

		/// <summary>
		/// Indexer has special handling in constrast to other AddXXX because the name can be driven by IndexerNameAttribute
		/// </summary>
		public void AddIndexer (Indexer i)
		{
			orderedAllMembers.Add (i);
			if (indexers == null)
				indexers = new List<MemberCore> ();
			if (i.IsExplicitImpl)
				AddMemberToList (i, indexers, true);
			else 
				AddMemberToList (i, indexers, false);
		}

		public void AddOperator (Operator op)
		{
			orderedAllMembers.Add (op);
			if (!AddMember (op))
				return;
			if (operators == null)
				operators = new List<MemberCore> ();

			operators.Add (op);
		}

		public void AddCompilerGeneratedClass (CompilerGeneratedClass c)
		{
			if (compiler_generated == null)
				compiler_generated = new List<CompilerGeneratedClass> ();

			compiler_generated.Add (c);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Type == pa.DefaultMember) {
				if (Indexers != null) {
					Report.Error (646, a.Location, "Cannot specify the `DefaultMember' attribute on type containing an indexer");
					return;
				}
			}

			if (a.Type == pa.Required) {
				Report.Error (1608, a.Location, "The RequiredAttribute attribute is not permitted on C# types");
				return;
			}

			TypeBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
		} 

		public override AttributeTargets AttributeTargets {
			get {
				throw new NotSupportedException ();
			}
		}

		public IList<TypeContainer> Types {
			get {
				return types;
			}
		}

		public IList<MemberCore> Methods {
			get {
				return methods;
			}
		}

		public IList<MemberCore> Constants {
			get {
				return constants;
			}
		}

		public TypeSpec BaseType {
			get {
				return spec.BaseType;
			}
		}

		public IList<FieldBase> Fields {
			get {
				return fields;
			}
		}

		public IList<Constructor> InstanceConstructors {
			get {
				return instance_constructors;
			}
		}

		public IList<MemberCore> Properties {
			get {
				return properties;
			}
		}

		public IList<MemberCore> Events {
			get {
				return events;
			}
		}
		
		public IList<MemberCore> Indexers {
			get {
				return indexers;
			}
		}

		public IList<MemberCore> Operators {
			get {
				return operators;
			}
		}

		protected override TypeAttributes TypeAttr {
			get {
				return ModifiersExtensions.TypeAttr (ModFlags, IsTopLevel);
			}
		}

		public int TypeParametersCount {
			get {
				return MemberName.Arity;
			}
		}

		TypeParameterSpec[] ITypeDefinition.TypeParameters {
			get {
				// TODO MemberCache: this is going to hurt
				return PartialContainer.type_params.Select (l => l.Type).ToArray ();
			}
		}

		public string GetAttributeDefaultMember ()
		{
			return indexers == null ? DefaultIndexerName : indexer_name;
		}

		public bool IsComImport {
			get {
				if (OptAttributes == null)
					return false;

				return OptAttributes.Contains (Module.PredefinedAttributes.ComImport);
			}
		}

		string ITypeDefinition.Namespace {
			get {
				return NamespaceEntry.NS.MemberName.GetSignatureForError ();
			}
		}

		public virtual void RegisterFieldForInitialization (MemberCore field, FieldInitializer expression)
		{
			if ((field.ModFlags & Modifiers.STATIC) != 0){
				if (initialized_static_fields == null) {
					PartialContainer.HasStaticFieldInitializer = true;
					initialized_static_fields = new List<FieldInitializer> (4);
				}

				initialized_static_fields.Add (expression);
			} else {
				if (initialized_fields == null)
					initialized_fields = new List<FieldInitializer> (4);

				initialized_fields.Add (expression);
			}
		}

		public void ResolveFieldInitializers (BlockContext ec)
		{
			if (partial_parts != null) {
				foreach (TypeContainer part in partial_parts) {
					part.DoResolveFieldInitializers (ec);
				}
			}
			DoResolveFieldInitializers (ec);
		}

		void DoResolveFieldInitializers (BlockContext ec)
		{
			if (ec.IsStatic) {
				if (initialized_static_fields == null)
					return;

				bool has_complex_initializer = !ec.Module.Compiler.Settings.Optimize;
				int i;
				ExpressionStatement [] init = new ExpressionStatement [initialized_static_fields.Count];
				for (i = 0; i < initialized_static_fields.Count; ++i) {
					FieldInitializer fi = initialized_static_fields [i];
					ExpressionStatement s = fi.ResolveStatement (ec);
					if (s == null) {
						s = EmptyExpressionStatement.Instance;
					} else if (fi.IsComplexInitializer) {
						has_complex_initializer |= true;
					}

					init [i] = s;
				}

				for (i = 0; i < initialized_static_fields.Count; ++i) {
					FieldInitializer fi = initialized_static_fields [i];
					//
					// Need special check to not optimize code like this
					// static int a = b = 5;
					// static int b = 0;
					//
					if (!has_complex_initializer && fi.IsDefaultInitializer)
						continue;

					ec.CurrentBlock.AddScopeStatement (new StatementExpression (init [i]));
				}

				return;
			}

			if (initialized_fields == null)
				return;

			for (int i = 0; i < initialized_fields.Count; ++i) {
				FieldInitializer fi = initialized_fields [i];
				ExpressionStatement s = fi.ResolveStatement (ec);
				if (s == null)
					continue;

				//
				// Field is re-initialized to its default value => removed
				//
				if (fi.IsDefaultInitializer && ec.Module.Compiler.Settings.Optimize)
					continue;

				ec.CurrentBlock.AddScopeStatement (new StatementExpression (s));
			}
		}

		public override string DocComment {
			get {
				return comment;
			}
			set {
				if (value == null)
					return;

				comment += value;
			}
		}

		public PendingImplementation PendingImplementations {
			get { return pending; }
		}

		internal override void GenerateDocComment (DocumentationBuilder builder)
		{
			base.GenerateDocComment (builder);

			if (DefaultStaticConstructor != null)
				DefaultStaticConstructor.GenerateDocComment (builder);

			if (InstanceConstructors != null)
				foreach (Constructor c in InstanceConstructors)
					c.GenerateDocComment (builder);

			if (Types != null)
				foreach (TypeContainer tc in Types)
					tc.GenerateDocComment (builder);

			if (Constants != null)
				foreach (Const c in Constants)
					c.GenerateDocComment (builder);

			if (Fields != null)
				foreach (FieldBase f in Fields)
					f.GenerateDocComment (builder);

			if (Events != null)
				foreach (Event e in Events)
					e.GenerateDocComment (builder);

			if (Indexers != null)
				foreach (Indexer ix in Indexers)
					ix.GenerateDocComment (builder);

			if (Properties != null)
				foreach (Property p in Properties)
					p.GenerateDocComment (builder);

			if (Methods != null)
				foreach (MethodOrOperator m in Methods)
					m.GenerateDocComment (builder);

			if (Operators != null)
				foreach (Operator o in Operators)
					o.GenerateDocComment (builder);
		}

		public TypeSpec GetAttributeCoClass ()
		{
			if (OptAttributes == null)
				return null;

			Attribute a = OptAttributes.Search (Module.PredefinedAttributes.CoClass);
			if (a == null)
				return null;

			return a.GetCoClassAttributeValue ();
		}

		public AttributeUsageAttribute GetAttributeUsage (PredefinedAttribute pa)
		{
			Attribute a = null;
			if (OptAttributes != null) {
				a = OptAttributes.Search (pa);
			}

			if (a == null)
				return null;

			return a.GetAttributeUsageAttribute ();
		}

		public virtual void AddBasesForPart (DeclSpace part, List<FullNamedExpression> bases)
		{
			// FIXME: get rid of partial_parts and store lists of bases of each part here
			// assumed, not verified: 'part' is in 'partial_parts' 
			((TypeContainer) part).type_bases = bases;
		}

		/// <summary>
		///   This function computes the Base class and also the
		///   list of interfaces that the class or struct @c implements.
		///   
		///   The return value is an array (might be null) of
		///   interfaces implemented (as Types).
		///   
		///   The @base_class argument is set to the base object or null
		///   if this is `System.Object'. 
		/// </summary>
		protected virtual TypeSpec[] ResolveBaseTypes (out FullNamedExpression base_class)
		{
			base_class = null;
			if (type_bases == null)
				return null;

			int count = type_bases.Count;
			TypeSpec[] ifaces = null;
			var base_context = new BaseContext (this);
			for (int i = 0, j = 0; i < count; i++){
				FullNamedExpression fne = type_bases [i];

				var fne_resolved = fne.ResolveAsType (base_context);
				if (fne_resolved == null)
					continue;

				if (i == 0 && Kind == MemberKind.Class && !fne_resolved.IsInterface) {
					if (fne_resolved.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
						Report.Error (1965, Location, "Class `{0}' cannot derive from the dynamic type",
							GetSignatureForError ());

						continue;
					}
					
					base_type = fne_resolved;
					base_class = fne;
					continue;
				}

				if (ifaces == null)
					ifaces = new TypeSpec [count - i];

				if (fne_resolved.IsInterface) {
					for (int ii = 0; ii < j; ++ii) {
						if (fne_resolved == ifaces [ii]) {
							Report.Error (528, Location, "`{0}' is already listed in interface list",
								fne_resolved.GetSignatureForError ());
							break;
						}
					}

					if (Kind == MemberKind.Interface && !IsAccessibleAs (fne_resolved)) {
						Report.Error (61, fne.Location,
							"Inconsistent accessibility: base interface `{0}' is less accessible than interface `{1}'",
							fne_resolved.GetSignatureForError (), GetSignatureForError ());
					}
				} else {
					Report.SymbolRelatedToPreviousError (fne_resolved);
					if (Kind != MemberKind.Class) {
						Report.Error (527, fne.Location, "Type `{0}' in interface list is not an interface", fne_resolved.GetSignatureForError ());
					} else if (base_class != null)
						Report.Error (1721, fne.Location, "`{0}': Classes cannot have multiple base classes (`{1}' and `{2}')",
							GetSignatureForError (), base_class.GetSignatureForError (), fne_resolved.GetSignatureForError ());
					else {
						Report.Error (1722, fne.Location, "`{0}': Base class `{1}' must be specified as first",
							GetSignatureForError (), fne_resolved.GetSignatureForError ());
					}
				}

				ifaces [j++] = fne_resolved;
			}

			return ifaces;
		}

		TypeSpec[] GetNormalPartialBases ()
		{
			var ifaces = new List<TypeSpec> (0);
			if (iface_exprs != null)
				ifaces.AddRange (iface_exprs);

			foreach (TypeContainer part in partial_parts) {
				FullNamedExpression new_base_class;
				var new_ifaces = part.ResolveBaseTypes (out new_base_class);
				if (new_base_class != null) {
					if (base_type_expr != null && part.base_type != base_type) {
						Report.SymbolRelatedToPreviousError (new_base_class.Location, "");
						Report.Error (263, part.Location,
							"Partial declarations of `{0}' must not specify different base classes",
							part.GetSignatureForError ());
					} else {
						base_type_expr = new_base_class;
						base_type = part.base_type;
					}
				}

				if (new_ifaces == null)
					continue;

				foreach (var iface in new_ifaces) {
					if (ifaces.Contains (iface))
						continue;

					ifaces.Add (iface);
				}
			}

			if (ifaces.Count == 0)
				return null;

			return ifaces.ToArray ();
		}

		//
		// Checks that some operators come in pairs:
		//  == and !=
		// > and <
		// >= and <=
		// true and false
		//
		// They are matched based on the return type and the argument types
		//
		void CheckPairedOperators ()
		{
			bool has_equality_or_inequality = false;
			var operators = this.operators.ToArray ();
			bool[] has_pair = new bool[operators.Length];

			for (int i = 0; i < operators.Length; ++i) {
				if (operators[i] == null)
					continue;

				Operator o_a = (Operator) operators[i];
				Operator.OpType o_type = o_a.OperatorType;
				if (o_type == Operator.OpType.Equality || o_type == Operator.OpType.Inequality)
					has_equality_or_inequality = true;

				Operator.OpType matching_type = o_a.GetMatchingOperator ();
				if (matching_type == Operator.OpType.TOP) {
					operators[i] = null;
					continue;
				}

				for (int ii = 0; ii < operators.Length; ++ii) {
					Operator o_b = (Operator) operators[ii];
					if (o_b == null || o_b.OperatorType != matching_type)
						continue;

					if (!TypeSpecComparer.IsEqual (o_a.ReturnType, o_b.ReturnType))
						continue;

					if (!TypeSpecComparer.Equals (o_a.ParameterTypes, o_b.ParameterTypes))
						continue;

					operators[i] = null;

					//
					// Used to ignore duplicate user conversions
					//
					has_pair[ii] = true;
				}
			}

			for (int i = 0; i < operators.Length; ++i) {
				if (operators[i] == null || has_pair[i])
					continue;

				Operator o = (Operator) operators [i];
				Report.Error (216, o.Location,
					"The operator `{0}' requires a matching operator `{1}' to also be defined",
					o.GetSignatureForError (), Operator.GetName (o.GetMatchingOperator ()));
			}

			if (has_equality_or_inequality) {
				if (Methods == null || !HasEquals)
					Report.Warning (660, 2, Location, "`{0}' defines operator == or operator != but does not override Object.Equals(object o)",
						GetSignatureForError ());

				if (Methods == null || !HasGetHashCode)
					Report.Warning (661, 2, Location, "`{0}' defines operator == or operator != but does not override Object.GetHashCode()",
						GetSignatureForError ());
			}
		}
	
		bool CreateTypeBuilder ()
		{
			//
			// Sets .size to 1 for structs with no instance fields
			//
			int type_size = Kind == MemberKind.Struct && first_nonstatic_field == null ? 1 : 0;

			if (IsTopLevel) {
				TypeBuilder = Module.CreateBuilder (Name, TypeAttr, type_size);
			} else {
				TypeBuilder = Parent.TypeBuilder.DefineNestedType (Basename, TypeAttr, null, type_size);
			}

			if (DeclaringAssembly.Importer != null)
				DeclaringAssembly.Importer.AddCompiledType (TypeBuilder, spec);

			spec.SetMetaInfo (TypeBuilder);
			spec.MemberCache = new MemberCache (this);
			spec.DeclaringType = Parent.CurrentType;

			if (!IsTopLevel)
				Parent.MemberCache.AddMember (spec);

			if (IsGeneric) {
				string[] param_names = new string[TypeParameters.Length];
				for (int i = 0; i < TypeParameters.Length; i++)
					param_names [i] = TypeParameters[i].Name;

				all_tp_builders = TypeBuilder.DefineGenericParameters (param_names);

				int offset = CurrentTypeParametersStartIndex;
				for (int i = offset; i < all_tp_builders.Length; i++) {
					CurrentTypeParameters [i - offset].Define (all_tp_builders [i], spec);
				}
			}

			return true;
		}

		//
		// Creates a proxy base method call inside this container for hoisted base member calls
		//
		public MethodSpec CreateHoistedBaseCallProxy (ResolveContext rc, MethodSpec method)
		{
			Method proxy_method;

			//
			// One proxy per base method is enough
			//
			if (hoisted_base_call_proxies == null) {
				hoisted_base_call_proxies = new Dictionary<MethodSpec, Method> ();
				proxy_method = null;
			} else {
				hoisted_base_call_proxies.TryGetValue (method, out proxy_method);
			}

			if (proxy_method == null) {
				string name = CompilerGeneratedClass.MakeName (method.Name, null, "BaseCallProxy", hoisted_base_call_proxies.Count);
				var base_parameters = new Parameter[method.Parameters.Count];
				for (int i = 0; i < base_parameters.Length; ++i) {
					var base_param = method.Parameters.FixedParameters[i];
					base_parameters[i] = new Parameter (new TypeExpression (method.Parameters.Types[i], Location),
						base_param.Name, base_param.ModFlags, null, Location);
					base_parameters[i].Resolve (this, i);
				}

				var cloned_params = ParametersCompiled.CreateFullyResolved (base_parameters, method.Parameters.Types);
				if (method.Parameters.HasArglist) {
					cloned_params.FixedParameters[0] = new Parameter (null, "__arglist", Parameter.Modifier.NONE, null, Location);
					cloned_params.Types[0] = Module.PredefinedTypes.RuntimeArgumentHandle.Resolve ();
				}

				GenericMethod generic_method;
				MemberName member_name;
				TypeArguments targs = null;
				if (method.IsGeneric) {
					//
					// Copy all base generic method type parameters info
					//
					var hoisted_tparams = method.GenericDefinition.TypeParameters;
					var type_params = new TypeParameter[hoisted_tparams.Length];
					targs = new TypeArguments ();
					targs.Arguments = new TypeSpec[type_params.Length];
					for (int i = 0; i < type_params.Length; ++i) {
						var tp = hoisted_tparams[i];
						targs.Add (new TypeParameterName (tp.Name, null, Location));
						targs.Arguments[i] = tp;
						type_params[i] = new TypeParameter (tp, this, null, new MemberName (tp.Name), null);
					}

					member_name = new MemberName (name, targs, Location);
					generic_method = new GenericMethod (NamespaceEntry, this, member_name, type_params,
						new TypeExpression (method.ReturnType, Location), cloned_params);
				} else {
					member_name = new MemberName (name);
					generic_method = null;
				}

				// Compiler generated proxy
				proxy_method = new Method (this, generic_method, new TypeExpression (method.ReturnType, Location),
					Modifiers.PRIVATE | Modifiers.COMPILER_GENERATED | Modifiers.DEBUGGER_HIDDEN,
					member_name, cloned_params, null);

				var block = new ToplevelBlock (Compiler, proxy_method.ParameterInfo, Location);

				var mg = MethodGroupExpr.CreatePredefined (method, method.DeclaringType, Location);
				mg.InstanceExpression = new BaseThis (method.DeclaringType, Location);
				if (targs != null)
					mg.SetTypeArguments (rc, targs);

				// Get all the method parameters and pass them as arguments
				var real_base_call = new Invocation (mg, block.GetAllParametersArguments ());
				Statement statement;
				if (method.ReturnType.Kind == MemberKind.Void)
					statement = new StatementExpression (real_base_call);
				else
					statement = new Return (real_base_call, Location);

				block.AddStatement (statement);
				proxy_method.Block = block;

				methods.Add (proxy_method);
				proxy_method.Define ();

				hoisted_base_call_proxies.Add (method, proxy_method);
			}

			return proxy_method.Spec;
		}

		bool DefineBaseTypes ()
		{
			iface_exprs = ResolveBaseTypes (out base_type_expr);
			if (partial_parts != null) {
				iface_exprs = GetNormalPartialBases ();
			}

			var cycle = CheckRecursiveDefinition (this);
			if (cycle != null) {
				Report.SymbolRelatedToPreviousError (cycle);
				if (this is Interface) {
					Report.Error (529, Location,
						"Inherited interface `{0}' causes a cycle in the interface hierarchy of `{1}'",
					    GetSignatureForError (), cycle.GetSignatureForError ());

					iface_exprs = null;
				} else {
					Report.Error (146, Location,
						"Circular base class dependency involving `{0}' and `{1}'",
						GetSignatureForError (), cycle.GetSignatureForError ());

					base_type = null;
				}
			}

			if (iface_exprs != null) {
				foreach (var iface_type in iface_exprs) {
					// Prevents a crash, the interface might not have been resolved: 442144
					if (iface_type == null)
						continue;
					
					if (!spec.AddInterface (iface_type))
						continue;

					TypeBuilder.AddInterfaceImplementation (iface_type.GetMetaInfo ());

					// Ensure the base is always setup
					var compiled_iface = iface_type.MemberDefinition as Interface;
					if (compiled_iface != null) {
						// TODO: Need DefineBaseType only
						compiled_iface.DefineType ();
					}

					if (iface_type.Interfaces != null) {
						var base_ifaces = new List<TypeSpec> (iface_type.Interfaces);
						for (int i = 0; i < base_ifaces.Count; ++i) {
							var ii_iface_type = base_ifaces[i];
							if (spec.AddInterface (ii_iface_type)) {
								TypeBuilder.AddInterfaceImplementation (ii_iface_type.GetMetaInfo ());

								if (ii_iface_type.Interfaces != null)
									base_ifaces.AddRange (ii_iface_type.Interfaces);
							}
						}
					}
				}
			}

			if (Kind == MemberKind.Interface) {
				spec.BaseType = Compiler.BuiltinTypes.Object;
				return true;
			}

			if (base_type != null) {
				spec.BaseType = base_type;

				// Set base type after type creation
				TypeBuilder.SetParent (base_type.GetMetaInfo ());
			} else {
				TypeBuilder.SetParent (null);
			}

			return true;
		}

		public virtual void DefineConstants ()
		{
			if (constants != null) {
				foreach (Const c in constants) {
					c.DefineValue ();
				}
			}

			if (instance_constructors != null) {
				foreach (MethodCore m in instance_constructors) {
					var p = m.ParameterInfo;
					if (!p.IsEmpty) {
						p.ResolveDefaultValues (m);
					}
				}
			}

			if (methods != null) {
				foreach (MethodCore m in methods) {
					var p = m.ParameterInfo;
					if (!p.IsEmpty) {
						p.ResolveDefaultValues (m);
					}
				}
			}

			if (indexers != null) {
				foreach (Indexer i in indexers) {
					i.ParameterInfo.ResolveDefaultValues (i);
				}
			}

			if (types != null) {
				foreach (var t in types)
					t.DefineConstants ();
			}
		}

		//
		// Defines the type in the appropriate ModuleBuilder or TypeBuilder.
		//
		public bool CreateType ()
		{
			if (TypeBuilder != null)
				return !error;

			if (error)
				return false;

			if (!CreateTypeBuilder ()) {
				error = true;
				return false;
			}

			if (partial_parts != null) {
				foreach (TypeContainer part in partial_parts) {
					part.spec = spec;
					part.current_type = current_type;
					part.TypeBuilder = TypeBuilder;
				}
			}

			if (Types != null) {
				foreach (TypeContainer tc in Types) {
					tc.CreateType ();
				}
			}

			return true;
		}

		public override void DefineType ()
		{
			if (error)
				return;
			if (type_defined)
				return;

			type_defined = true;

			// TODO: Driver resolves only first level of namespace, do the rest here for now
			if (IsTopLevel && (ModFlags & Modifiers.COMPILER_GENERATED) == 0) {
				NamespaceEntry.Resolve ();
			}

			if (!DefineBaseTypes ()) {
				error = true;
				return;
			}

			if (!DefineNestedTypes ()) {
				error = true;
				return;
			}
		}

		public override void SetParameterInfo (List<Constraints> constraints_list)
		{
			base.SetParameterInfo (constraints_list);

			if (PartialContainer.CurrentTypeParameters == null || PartialContainer == this)
				return;

			TypeParameter[] tc_names = PartialContainer.CurrentTypeParameters;
			for (int i = 0; i < tc_names.Length; ++i) {
				if (tc_names [i].Name != type_params [i].Name) {
					Report.SymbolRelatedToPreviousError (PartialContainer.Location, "");
					Report.Error (264, Location, "Partial declarations of `{0}' must have the same type parameter names in the same order",
						GetSignatureForError ());
					break;
				}

				if (tc_names [i].Variance != type_params [i].Variance) {
					Report.SymbolRelatedToPreviousError (PartialContainer.Location, "");
					Report.Error (1067, Location, "Partial declarations of `{0}' must have the same type parameter variance modifiers",
						GetSignatureForError ());
					break;
				}
			}
		}

		//
		// Replaces normal spec with predefined one when compiling corlib
		// and this type container defines predefined type
		//
		public void SetPredefinedSpec (BuiltinTypeSpec spec)
		{
			// When compiling build-in types we start with two
			// version of same type. One is of BuiltinTypeSpec and
			// second one is ordinary TypeSpec. The unification
			// happens at later stage when we know which type
			// really matches the builtin type signature. However
			// that means TypeSpec create during CreateType of this
			// type has to be replaced with builtin one
			// 
			spec.SetMetaInfo (TypeBuilder);
			spec.MemberCache = this.spec.MemberCache;
			spec.DeclaringType = this.spec.DeclaringType;

			this.spec = spec;
			current_type = null;
		}

		void UpdateTypeParameterConstraints (TypeContainer part)
		{
			TypeParameter[] current_params = type_params;
			for (int i = 0; i < current_params.Length; i++) {
				if (current_params [i].AddPartialConstraints (part, part.type_params [i]))
					continue;

				Report.SymbolRelatedToPreviousError (Location, "");
				Report.Error (265, part.Location,
					"Partial declarations of `{0}' have inconsistent constraints for type parameter `{1}'",
					GetSignatureForError (), current_params [i].GetSignatureForError ());
			}
		}

		public bool ResolveTypeParameters ()
		{
			if (!DoResolveTypeParameters ())
				return false;

			if (types != null) {
				foreach (var type in types)
					if (!type.ResolveTypeParameters ())
						return false;
			}

			if (compiler_generated != null) {
				foreach (CompilerGeneratedClass c in compiler_generated)
					if (!c.ResolveTypeParameters ())
						return false;
			}

			return true;
		}

		protected virtual bool DoResolveTypeParameters ()
		{
			if (CurrentTypeParameters == null)
				return true;

			if (PartialContainer != this)
				throw new InternalErrorException ();

			var base_context = new BaseContext (this);
			foreach (TypeParameter type_param in CurrentTypeParameters) {
				if (!type_param.ResolveConstraints (base_context)) {
					error = true;
					return false;
				}
			}

			if (partial_parts != null) {
				foreach (TypeContainer part in partial_parts)
					UpdateTypeParameterConstraints (part);
			}

			return true;
		}

		protected virtual bool DefineNestedTypes ()
		{
			if (Types != null) {
				foreach (TypeContainer tc in Types)
					tc.DefineType ();
			}

			return true;
		}

		TypeSpec CheckRecursiveDefinition (TypeContainer tc)
		{
			if (InTransit != null)
				return spec;

			InTransit = tc;

			if (base_type != null) {
				var ptc = base_type.MemberDefinition as TypeContainer;
				if (ptc != null && ptc.CheckRecursiveDefinition (this) != null)
					return base_type;
			}

			if (iface_exprs != null) {
				foreach (var iface in iface_exprs) {
					// the interface might not have been resolved, prevents a crash, see #442144
					if (iface == null)
						continue;
					var ptc = iface.MemberDefinition as Interface;
					if (ptc != null && ptc.CheckRecursiveDefinition (this) != null)
						return iface;
				}
			}

			if (!IsTopLevel && Parent.PartialContainer.CheckRecursiveDefinition (this) != null)
				return spec;

			InTransit = null;
			return null;
		}

		/// <summary>
		///   Populates our TypeBuilder with fields and methods
		/// </summary>
		public sealed override bool Define ()
		{
			if (members_defined)
				return members_defined_ok;

			members_defined_ok = DoDefineMembers ();
			members_defined = true;

			if (types != null) {
				foreach (var nested in types)
					nested.Define ();
			}

			return members_defined_ok;
		}

		protected virtual bool DoDefineMembers ()
		{
			if (iface_exprs != null) {
				foreach (var iface_type in iface_exprs) {
					if (iface_type == null)
						continue;

					// Ensure the base is always setup
					var compiled_iface = iface_type.MemberDefinition as Interface;
					if (compiled_iface != null)
						compiled_iface.Define ();

					if (Kind == MemberKind.Interface)
						MemberCache.AddInterface (iface_type);

					ObsoleteAttribute oa = iface_type.GetAttributeObsolete ();
					if (oa != null && !IsObsolete)
						AttributeTester.Report_ObsoleteMessage (oa, iface_type.GetSignatureForError (), Location, Report);

					if (iface_type.Arity > 0) {
						// TODO: passing `this' is wrong, should be base type iface instead
						TypeManager.CheckTypeVariance (iface_type, Variance.Covariant, this);

						if (((InflatedTypeSpec) iface_type).HasDynamicArgument () && !IsCompilerGenerated) {
							Report.Error (1966, Location,
								"`{0}': cannot implement a dynamic interface `{1}'",
								GetSignatureForError (), iface_type.GetSignatureForError ());
							return false;
						}

						if (spec.Interfaces != null) {
							foreach (var prev_iface in iface_exprs) {
								if (prev_iface == iface_type)
									break;

								if (!TypeSpecComparer.Unify.IsEqual (iface_type, prev_iface))
									continue;

								Report.Error (695, Location,
									"`{0}' cannot implement both `{1}' and `{2}' because they may unify for some type parameter substitutions",
									GetSignatureForError (), prev_iface.GetSignatureForError (), iface_type.GetSignatureForError ());
							}
						}
					}
				}
			}

			if (base_type != null) {
				//
				// Run checks skipped during DefineType (e.g FullNamedExpression::ResolveAsType)
				//
				if (base_type_expr != null) {
					ObsoleteAttribute obsolete_attr = base_type.GetAttributeObsolete ();
					if (obsolete_attr != null && !IsObsolete)
						AttributeTester.Report_ObsoleteMessage (obsolete_attr, base_type.GetSignatureForError (), base_type_expr.Location, Report);

					if (IsGeneric && base_type.IsAttribute) {
						Report.Error (698, base_type_expr.Location,
							"A generic type cannot derive from `{0}' because it is an attribute class",
							base_type.GetSignatureForError ());
					}
				}

				if (base_type.Interfaces != null) {
					foreach (var iface in base_type.Interfaces)
						spec.AddInterface (iface);
				}

				var baseContainer = base_type.MemberDefinition as ClassOrStruct;
				if (baseContainer != null) {
					baseContainer.Define ();

					//
					// It can trigger define of this type (for generic types only)
					//
					if (HasMembersDefined)
						return true;
				}
			}

			DefineContainerMembers (constants);
			DefineContainerMembers (fields);

			if (Kind == MemberKind.Struct || Kind == MemberKind.Class) {
				pending = PendingImplementation.GetPendingImplementations (this);

				if (requires_delayed_unmanagedtype_check) {
					requires_delayed_unmanagedtype_check = false;
					foreach (FieldBase f in fields) {
						if (f.MemberType != null && f.MemberType.IsPointer)
							TypeManager.VerifyUnmanaged (Module, f.MemberType, f.Location);
					}
				}
			}
		
			//
			// Constructors are not in the defined_names array
			//
			DefineContainerMembers (instance_constructors);
		
			DefineContainerMembers (events);
			DefineContainerMembers (ordered_explicit_member_list);
			DefineContainerMembers (ordered_member_list);

			if (operators != null) {
				DefineContainerMembers (operators);
				CheckPairedOperators ();
			}

			ComputeIndexerName();
			CheckEqualsAndGetHashCode();

			if (Kind == MemberKind.Interface && iface_exprs != null) {
				MemberCache.RemoveHiddenMembers (spec);
			}

			return true;
		}

		protected virtual void DefineContainerMembers (System.Collections.IList mcal) // IList<MemberCore>
		{
			if (mcal != null) {
				for (int i = 0; i < mcal.Count; ++i) {
					MemberCore mc = (MemberCore) mcal[i];
					try {
						mc.Define ();
					} catch (Exception e) {
						throw new InternalErrorException (mc, e);
					}
				}
			}
		}
		
		protected virtual void ComputeIndexerName ()
		{
			if (indexers == null)
				return;

			string class_indexer_name = null;

			//
			// If there's both an explicit and an implicit interface implementation, the
			// explicit one actually implements the interface while the other one is just
			// a normal indexer.  See bug #37714.
			//

			// Invariant maintained by AddIndexer(): All explicit interface indexers precede normal indexers
			foreach (Indexer i in indexers) {
				if (i.InterfaceType != null) {
					if (seen_normal_indexers)
						throw new Exception ("Internal Error: 'Indexers' array not sorted properly.");
					continue;
				}

				seen_normal_indexers = true;

				if (class_indexer_name == null) {
					class_indexer_name = i.ShortName;
					continue;
				}

				if (i.ShortName != class_indexer_name)
					Report.Error (668, i.Location, "Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type");
			}

			if (class_indexer_name != null)
				indexer_name = class_indexer_name;
		}

		void EmitIndexerName ()
		{
			if (!seen_normal_indexers)
				return;

			var ctor = Module.PredefinedMembers.DefaultMemberAttributeCtor.Get ();
			if (ctor == null)
				return;

			var encoder = new AttributeEncoder ();
			encoder.Encode (GetAttributeDefaultMember ());
			encoder.EncodeEmptyNamedArguments ();

			TypeBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), encoder.ToArray ());
		}

		protected virtual void CheckEqualsAndGetHashCode ()
		{
			if (methods == null)
				return;

			if (HasEquals && !HasGetHashCode) {
				Report.Warning (659, 3, this.Location, "`{0}' overrides Object.Equals(object) but does not override Object.GetHashCode()", this.GetSignatureForError ());
			}
		}

		// Indicated whether container has StructLayout attribute set Explicit
		public bool HasExplicitLayout {
			get { return (caching_flags & Flags.HasExplicitLayout) != 0; }
			set { caching_flags |= Flags.HasExplicitLayout; }
		}

		public bool HasStructLayout {
			get { return (caching_flags & Flags.HasStructLayout) != 0; }
			set { caching_flags |= Flags.HasStructLayout; }
		}

		public MemberCache MemberCache {
			get {
				return spec.MemberCache;
			}
		}

		void CheckMemberUsage (List<MemberCore> al, string member_type)
		{
			if (al == null)
				return;

			foreach (MemberCore mc in al) {
				if ((mc.ModFlags & Modifiers.AccessibilityMask) != Modifiers.PRIVATE)
					continue;

				if ((mc.ModFlags & Modifiers.PARTIAL) != 0)
					continue;

				if (!mc.IsUsed && (mc.caching_flags & Flags.Excluded) == 0) {
					Report.Warning (169, 3, mc.Location, "The private {0} `{1}' is never used", member_type, mc.GetSignatureForError ());
				}
			}
		}

		public virtual void VerifyMembers ()
		{
			//
			// Check for internal or private fields that were never assigned
			//
			if (Report.WarningLevel >= 3) {
				if (Compiler.Settings.EnhancedWarnings) {
					CheckMemberUsage (properties, "property");
					CheckMemberUsage (methods, "method");
					CheckMemberUsage (constants, "constant");
				}

				if (fields != null){
					bool is_type_exposed = Kind == MemberKind.Struct || IsExposedFromAssembly ();
					foreach (FieldBase f in fields) {
						if ((f.ModFlags & Modifiers.AccessibilityMask) != Modifiers.PRIVATE) {
							if (is_type_exposed)
								continue;

							f.SetIsUsed ();
						}				
						
						if (!f.IsUsed){
							if ((f.caching_flags & Flags.IsAssigned) == 0)
								Report.Warning (169, 3, f.Location, "The private field `{0}' is never used", f.GetSignatureForError ());
							else {
								Report.Warning (414, 3, f.Location, "The private field `{0}' is assigned but its value is never used",
									f.GetSignatureForError ());
							}
							continue;
						}
						
						if ((f.caching_flags & Flags.IsAssigned) != 0)
							continue;

						//
						// Only report 649 on level 4
						//
						if (Report.WarningLevel < 4)
							continue;

						//
						// Don't be pendatic over serializable attributes
						//
						if (f.OptAttributes != null || PartialContainer.HasStructLayout)
							continue;
						
						Constant c = New.Constantify (f.MemberType, f.Location);
						string value;
						if (c != null) {
							value = c.GetValueAsLiteral ();
						} else if (TypeSpec.IsReferenceType (f.MemberType)) {
							value = "null";
						} else {
							// Ignore this warning for struct value fields (they are always initialized)
							if (f.MemberType.IsStruct)
								continue;

							value = null;
						}

						if (value != null)
							value = " `" + value + "'";

						Report.Warning (649, 4, f.Location, "Field `{0}' is never assigned to, and will always have its default value{1}",
							f.GetSignatureForError (), value);
					}
				}
			}
		}

		public override void Emit ()
		{
			if (!IsTopLevel) {
				MemberSpec candidate;
				bool overrides = false;
				var conflict_symbol = MemberCache.FindBaseMember (this, out candidate, ref overrides);
				if (conflict_symbol == null && candidate == null) {
					if ((ModFlags & Modifiers.NEW) != 0)
						Report.Warning (109, 4, Location, "The member `{0}' does not hide an inherited member. The new keyword is not required",
							GetSignatureForError ());
				} else {
					if ((ModFlags & Modifiers.NEW) == 0) {
						if (candidate == null)
							candidate = conflict_symbol;

						Report.SymbolRelatedToPreviousError (candidate);
						Report.Warning (108, 2, Location, "`{0}' hides inherited member `{1}'. Use the new keyword if hiding was intended",
							GetSignatureForError (), candidate.GetSignatureForError ());
					}
				}
			}

			// Run constraints check on all possible generic types
			if ((ModFlags & Modifiers.COMPILER_GENERATED) == 0) {
				if (base_type != null && base_type_expr != null) {
					ConstraintChecker.Check (this, base_type, base_type_expr.Location);
				}

				if (iface_exprs != null) {
					foreach (var iface_type in iface_exprs) {
						if (iface_type == null)
							continue;

						ConstraintChecker.Check (this, iface_type, Location);	// TODO: Location is wrong
					}
				}
			}

			if (all_tp_builders != null) {
				int current_starts_index = CurrentTypeParametersStartIndex;
				for (int i = 0; i < all_tp_builders.Length; i++) {
					if (i < current_starts_index) {
						TypeParameters[i].EmitConstraints (all_tp_builders [i]);
					} else {
						var tp = CurrentTypeParameters [i - current_starts_index];
						tp.CheckGenericConstraints (!IsObsolete);
						tp.Emit ();
					}
				}
			}

			if ((ModFlags & Modifiers.COMPILER_GENERATED) != 0 && !Parent.IsCompilerGenerated)
				Module.PredefinedAttributes.CompilerGenerated.EmitAttribute (TypeBuilder);

#if STATIC
			if ((TypeBuilder.Attributes & TypeAttributes.StringFormatMask) == 0 && Module.HasDefaultCharSet)
				TypeBuilder.__SetAttributes (TypeBuilder.Attributes | Module.DefaultCharSetType);
#endif

			base.Emit ();
		}

		// TODO: move to ClassOrStruct
		void EmitConstructors ()
		{
			if (instance_constructors == null)
				return;

			if (spec.IsAttribute && IsExposedFromAssembly () && Compiler.Settings.VerifyClsCompliance && IsClsComplianceRequired ()) {
				bool has_compliant_args = false;

				foreach (Constructor c in instance_constructors) {
					try {
						c.Emit ();
					}
					catch (Exception e) {
						throw new InternalErrorException (c, e);
					}

					if (has_compliant_args)
						continue;

					has_compliant_args = c.HasCompliantArgs;
				}
				if (!has_compliant_args)
					Report.Warning (3015, 1, Location, "`{0}' has no accessible constructors which use only CLS-compliant types", GetSignatureForError ());
			} else {
				foreach (Constructor c in instance_constructors) {
					try {
						c.Emit ();
					}
					catch (Exception e) {
						throw new InternalErrorException (c, e);
					}
				}
			}
		}

		/// <summary>
		///   Emits the code, this step is performed after all
		///   the types, enumerations, constructors
		/// </summary>
		public virtual void EmitType ()
		{
			if ((caching_flags & Flags.CloseTypeCreated) != 0)
				return;

			if (OptAttributes != null)
				OptAttributes.Emit ();

			Emit ();

			EmitConstructors ();

			if (constants != null)
				foreach (Const con in constants)
					con.Emit ();

			if (default_static_constructor != null)
				default_static_constructor.Emit ();
			
			if (operators != null)
				foreach (Operator o in operators)
					o.Emit ();

			if (properties != null)
				foreach (Property p in properties)
					p.Emit ();

			if (indexers != null) {
				foreach (Indexer indx in indexers)
					indx.Emit ();
				EmitIndexerName ();
			}

			if (events != null){
				foreach (Event e in Events)
					e.Emit ();
			}

			if (methods != null) {
				for (int i = 0; i < methods.Count; ++i)
					((MethodOrOperator) methods [i]).Emit ();
			}
			
			if (fields != null)
				foreach (FieldBase f in fields)
					f.Emit ();

			if (types != null) {
				foreach (TypeContainer t in types)
					t.EmitType ();
			}

			if (pending != null)
				pending.VerifyPendingMethods ();

			if (Report.Errors > 0)
				return;

			if (compiler_generated != null) {
				for (int i = 0; i < compiler_generated.Count; ++i)
					compiler_generated [i].EmitType ();
			}
		}

		public virtual void CloseType ()
		{
			if ((caching_flags & Flags.CloseTypeCreated) != 0)
				return;

			// Close base type container first to avoid TypeLoadException
			if (spec.BaseType != null) {
				var btype = spec.BaseType.MemberDefinition as TypeContainer;
				if (btype != null) {
					btype.CloseType ();

					if ((caching_flags & Flags.CloseTypeCreated) != 0)
						return;
				}
			}

			try {
				caching_flags |= Flags.CloseTypeCreated;
				TypeBuilder.CreateType ();
			} catch (TypeLoadException){
				//
				// This is fine, the code still created the type
				//
//				Report.Warning (-20, "Exception while creating class: " + TypeBuilder.Name);
//				Console.WriteLine (e.Message);
			} catch (Exception e) {
				throw new InternalErrorException (this, e);
			}
			
			if (Types != null){
				foreach (TypeContainer tc in Types)
					tc.CloseType ();
			}

			if (compiler_generated != null)
				foreach (CompilerGeneratedClass c in compiler_generated)
					c.CloseType ();
			
			types = null;
			initialized_fields = null;
			initialized_static_fields = null;
			constants = null;
			ordered_explicit_member_list = null;
			ordered_member_list = null;
			methods = null;
			events = null;
			indexers = null;
			operators = null;
			compiler_generated = null;
			default_constructor = null;
			default_static_constructor = null;
			type_bases = null;
			OptAttributes = null;
		}

		//
		// Performs the validation on a Method's modifiers (properties have
		// the same properties).
		//
		// TODO: Why is it not done at parse stage, move to Modifiers::Check
		//
		public bool MethodModifiersValid (MemberCore mc)
		{
			const Modifiers vao = (Modifiers.VIRTUAL | Modifiers.ABSTRACT | Modifiers.OVERRIDE);
			const Modifiers nv = (Modifiers.NEW | Modifiers.VIRTUAL);
			bool ok = true;
			var flags = mc.ModFlags;
			
			//
			// At most one of static, virtual or override
			//
			if ((flags & Modifiers.STATIC) != 0){
				if ((flags & vao) != 0){
					Report.Error (112, mc.Location, "A static member `{0}' cannot be marked as override, virtual or abstract",
						mc.GetSignatureForError ());
					ok = false;
				}
			}

			if ((flags & Modifiers.OVERRIDE) != 0 && (flags & nv) != 0){
				Report.Error (113, mc.Location, "A member `{0}' marked as override cannot be marked as new or virtual",
					mc.GetSignatureForError ());
				ok = false;
			}

			//
			// If the declaration includes the abstract modifier, then the
			// declaration does not include static, virtual or extern
			//
			if ((flags & Modifiers.ABSTRACT) != 0){
				if ((flags & Modifiers.EXTERN) != 0){
					Report.Error (
						180, mc.Location, "`{0}' cannot be both extern and abstract", mc.GetSignatureForError ());
					ok = false;
				}

				if ((flags & Modifiers.SEALED) != 0) {
					Report.Error (502, mc.Location, "`{0}' cannot be both abstract and sealed", mc.GetSignatureForError ());
					ok = false;
				}

				if ((flags & Modifiers.VIRTUAL) != 0){
					Report.Error (503, mc.Location, "The abstract method `{0}' cannot be marked virtual", mc.GetSignatureForError ());
					ok = false;
				}

				if ((ModFlags & Modifiers.ABSTRACT) == 0){
					Report.SymbolRelatedToPreviousError (this);
					Report.Error (513, mc.Location, "`{0}' is abstract but it is declared in the non-abstract class `{1}'",
						mc.GetSignatureForError (), GetSignatureForError ());
					ok = false;
				}
			}

			if ((flags & Modifiers.PRIVATE) != 0){
				if ((flags & vao) != 0){
					Report.Error (621, mc.Location, "`{0}': virtual or abstract members cannot be private", mc.GetSignatureForError ());
					ok = false;
				}
			}

			if ((flags & Modifiers.SEALED) != 0){
				if ((flags & Modifiers.OVERRIDE) == 0){
					Report.Error (238, mc.Location, "`{0}' cannot be sealed because it is not an override", mc.GetSignatureForError ());
					ok = false;
				}
			}

			return ok;
		}

		public Constructor DefaultStaticConstructor {
			get { return default_static_constructor; }
		}

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ())
				return false;

			// Check this name against other containers
			NamespaceEntry.NS.VerifyClsCompliance ();

			// Check all container names for user classes
			if (Kind != MemberKind.Delegate)
				MemberCache.VerifyClsCompliance (Definition, Report);

			if (BaseType != null && !BaseType.IsCLSCompliant ()) {
				Report.Warning (3009, 1, Location, "`{0}': base type `{1}' is not CLS-compliant",
					GetSignatureForError (), BaseType.GetSignatureForError ());
			}
			return true;
		}

		/// <summary>
		///   Performs checks for an explicit interface implementation.  First it
		///   checks whether the `interface_type' is a base inteface implementation.
		///   Then it checks whether `name' exists in the interface type.
		/// </summary>
		public bool VerifyImplements (InterfaceMemberBase mb)
		{
			var ifaces = spec.Interfaces;
			if (ifaces != null) {
				foreach (TypeSpec t in ifaces){
					if (t == mb.InterfaceType)
						return true;
				}
			}
			
			Report.SymbolRelatedToPreviousError (mb.InterfaceType);
			Report.Error (540, mb.Location, "`{0}': containing type does not implement interface `{1}'",
				mb.GetSignatureForError (), TypeManager.CSharpName (mb.InterfaceType));
			return false;
		}

		//
		// Used for visiblity checks to tests whether this definition shares
		// base type baseType, it does member-definition search
		//
		public bool IsBaseTypeDefinition (TypeSpec baseType)
		{
			// RootContext check
			if (TypeBuilder == null)
				return false;

			var type = spec;
			do {
				if (type.MemberDefinition == baseType.MemberDefinition)
					return true;

				type = type.BaseType;
			} while (type != null);

			return false;
		}

		bool ITypeDefinition.IsInternalAsPublic (IAssemblyDefinition assembly)
		{
			return Module.DeclaringAssembly == assembly;
		}

		public void LoadMembers (TypeSpec declaringType, bool onlyTypes, ref MemberCache cache)
		{
			throw new NotSupportedException ("Not supported for compiled definition " + GetSignatureForError ());
		}

		//
		// Public function used to locate types.
		//
		// Set 'ignore_cs0104' to true if you want to ignore cs0104 errors.
		//
		// Returns: Type or null if they type can not be found.
		//
		public override FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc)
		{
			FullNamedExpression e;
			if (arity == 0 && Cache.TryGetValue (name, out e) && mode != LookupMode.IgnoreAccessibility)
				return e;

			e = null;

			if (arity == 0) {
				TypeParameter[] tp = CurrentTypeParameters;
				if (tp != null) {
					TypeParameter tparam = TypeParameter.FindTypeParameter (tp, name);
					if (tparam != null)
						e = new TypeParameterExpr (tparam, Location.Null);
				}
			}

			if (e == null) {
				TypeSpec t = LookupNestedTypeInHierarchy (name, arity);

				if (t != null && (t.IsAccessible (this) || mode == LookupMode.IgnoreAccessibility))
					e = new TypeExpression (t, Location.Null);
				else if (Parent != null) {
					e = Parent.LookupNamespaceOrType (name, arity, mode, loc);
				} else {
					int errors = Report.Errors;

					e = NamespaceEntry.LookupNamespaceOrType (name, arity, mode, loc);

					if (errors != Report.Errors)
						return e;
				}
			}

			// TODO MemberCache: How to cache arity stuff ?
			if (arity == 0 && mode == LookupMode.Normal)
				Cache[name] = e;

			return e;
		}

		TypeSpec LookupNestedTypeInHierarchy (string name, int arity)
		{
			// TODO: GenericMethod only
			if (PartialContainer == null)
				return null;

			// Has any nested type
			// Does not work, because base type can have
			//if (PartialContainer.Types == null)
			//	return null;

			var container = PartialContainer.CurrentType;

			// Is not Root container
			if (container == null)
				return null;

			return MemberCache.FindNestedType (container, name, arity);
		}

		public void Mark_HasEquals ()
		{
			cached_method |= CachedMethods.Equals;
		}

		public void Mark_HasGetHashCode ()
		{
			cached_method |= CachedMethods.GetHashCode;
		}

		/// <summary>
		/// Method container contains Equals method
		/// </summary>
		public bool HasEquals {
			get {
				return (cached_method & CachedMethods.Equals) != 0;
			}
		}
 
		/// <summary>
		/// Method container contains GetHashCode method
		/// </summary>
		public bool HasGetHashCode {
			get {
				return (cached_method & CachedMethods.GetHashCode) != 0;
			}
		}

		public bool HasStaticFieldInitializer {
			get {
				return (cached_method & CachedMethods.HasStaticFieldInitializer) != 0;
			}
			set {
				if (value)
					cached_method |= CachedMethods.HasStaticFieldInitializer;
				else
					cached_method &= ~CachedMethods.HasStaticFieldInitializer;
			}
		}

		public override string DocCommentHeader {
			get { return "T:"; }
		}
	}

	public abstract class ClassOrStruct : TypeContainer
	{
		SecurityType declarative_security;

		public ClassOrStruct (NamespaceContainer ns, DeclSpace parent,
				      MemberName name, Attributes attrs, MemberKind kind)
			: base (ns, parent, name, attrs, kind)
		{
		}

		protected override bool AddToContainer (MemberCore symbol, string name)
		{
			if (!(symbol is Constructor) && symbol.MemberName.Name == MemberName.Name) {
				if (symbol is TypeParameter) {
					Report.Error (694, symbol.Location,
						"Type parameter `{0}' has same name as containing type, or method",
						symbol.GetSignatureForError ());
					return false;
				}
			
				InterfaceMemberBase imb = symbol as InterfaceMemberBase;
				if (imb == null || !imb.IsExplicitImpl) {
					Report.SymbolRelatedToPreviousError (this);
					Report.Error (542, symbol.Location, "`{0}': member names cannot be the same as their enclosing type",
						symbol.GetSignatureForError ());
					return false;
				}
			}

			return base.AddToContainer (symbol, name);
		}

		public override void VerifyMembers ()
		{
			base.VerifyMembers ();

			if ((events != null) && Report.WarningLevel >= 3) {
				foreach (Event e in events){
					// Note: The event can be assigned from same class only, so we can report
					// this warning for all accessibility modes
					if ((e.caching_flags & Flags.IsUsed) == 0)
						Report.Warning (67, 3, e.Location, "The event `{0}' is never used", e.GetSignatureForError ());
				}
			}

			if (types != null) {
				foreach (var t in types)
					t.VerifyMembers ();
			}
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.IsValidSecurityAttribute ()) {
				a.ExtractSecurityPermissionSet (ctor, ref declarative_security);
				return;
			}

			if (a.Type == pa.StructLayout) {
				PartialContainer.HasStructLayout = true;
				if (a.IsExplicitLayoutKind ())
					PartialContainer.HasExplicitLayout = true;
			}

			if (a.Type == pa.Dynamic) {
				a.Error_MisusedDynamicAttribute ();
				return;
			}

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}

		/// <summary>
		/// Defines the default constructors 
		/// </summary>
		protected void DefineDefaultConstructor (bool is_static)
		{
			// The default instance constructor is public
			// If the class is abstract, the default constructor is protected
			// The default static constructor is private

			Modifiers mods;
			if (is_static) {
				mods = Modifiers.STATIC | Modifiers.PRIVATE;
			} else {
				mods = ((ModFlags & Modifiers.ABSTRACT) != 0) ? Modifiers.PROTECTED : Modifiers.PUBLIC;
			}

			Constructor c = new Constructor (this, MemberName.Name, mods,
				null, ParametersCompiled.EmptyReadOnlyParameters,
				new GeneratedBaseInitializer (Location),
				Location);
			
			AddConstructor (c);
			c.Block = new ToplevelBlock (Compiler, ParametersCompiled.EmptyReadOnlyParameters, Location);
		}

		protected override bool DoDefineMembers ()
		{
			CheckProtectedModifier ();

			base.DoDefineMembers ();

			if (default_static_constructor != null)
				default_static_constructor.Define ();

			return true;
		}

		public override void Emit ()
		{
			if (default_static_constructor == null && PartialContainer.HasStaticFieldInitializer) {
				DefineDefaultConstructor (true);
				default_static_constructor.Define ();
			}

			base.Emit ();

			if (declarative_security != null) {
				foreach (var de in declarative_security) {
#if STATIC
					TypeBuilder.__AddDeclarativeSecurity (de);
#else
					TypeBuilder.AddDeclarativeSecurity (de.Key, de.Value);
#endif
				}
			}
		}

		public override IList<MethodSpec> LookupExtensionMethod (TypeSpec extensionType, string name, int arity, ref NamespaceContainer scope)
		{
			DeclSpace top_level = Parent;
			if (top_level != null) {
				var candidates = NamespaceEntry.NS.LookupExtensionMethod (this, extensionType, name, arity);
				if (candidates != null) {
					scope = NamespaceEntry;
					return candidates;
				}
			}

			return NamespaceEntry.LookupExtensionMethod (extensionType, name, arity, ref scope);
		}

		protected override TypeAttributes TypeAttr {
			get {
				if (default_static_constructor == null)
					return base.TypeAttr | TypeAttributes.BeforeFieldInit;

				return base.TypeAttr;
			}
		}
	}


	// TODO: should be sealed
	public class Class : ClassOrStruct {
		const Modifiers AllowedModifiers =
			Modifiers.NEW |
			Modifiers.PUBLIC |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL |
			Modifiers.PRIVATE |
			Modifiers.ABSTRACT |
			Modifiers.SEALED |
			Modifiers.STATIC |
			Modifiers.UNSAFE;

		public const TypeAttributes StaticClassAttribute = TypeAttributes.Abstract | TypeAttributes.Sealed;

		public Class (NamespaceContainer ns, DeclSpace parent, MemberName name, Modifiers mod,
			      Attributes attrs)
			: base (ns, parent, name, attrs, MemberKind.Class)
		{
			var accmods = (Parent == null || Parent.Parent == null) ? Modifiers.INTERNAL : Modifiers.PRIVATE;
			this.ModFlags = ModifiersExtensions.Check (AllowedModifiers, mod, accmods, Location, Report);
			spec = new TypeSpec (Kind, null, this, null, ModFlags);
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void AddBasesForPart (DeclSpace part, List<FullNamedExpression> bases)
		{
			if (part.Name == "System.Object")
				Report.Error (537, part.Location,
					"The class System.Object cannot have a base class or implement an interface.");
			base.AddBasesForPart (part, bases);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Type == pa.AttributeUsage) {
				if (!BaseType.IsAttribute && spec.BuiltinType != BuiltinTypeSpec.Type.Attribute) {
					Report.Error (641, a.Location, "Attribute `{0}' is only valid on classes derived from System.Attribute", a.GetSignatureForError ());
				}
			}

			if (a.Type == pa.Conditional && !BaseType.IsAttribute) {
				Report.Error (1689, a.Location, "Attribute `System.Diagnostics.ConditionalAttribute' is only valid on methods or attribute classes");
				return;
			}

			if (a.Type == pa.ComImport && !attributes.Contains (pa.Guid)) {
				a.Error_MissingGuidAttribute ();
				return;
			}

			if (a.Type == pa.Extension) {
				a.Error_MisusedExtensionAttribute ();
				return;
			}

			if (a.Type.IsConditionallyExcluded (Compiler, Location))
				return;

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Class;
			}
		}

		protected override void DefineContainerMembers (System.Collections.IList list)
		{
			if (list == null)
				return;

			if (!IsStatic) {
				base.DefineContainerMembers (list);
				return;
			}

			foreach (MemberCore m in list) {
				if (m is Operator) {
					Report.Error (715, m.Location, "`{0}': Static classes cannot contain user-defined operators", m.GetSignatureForError ());
					continue;
				}

				if (m is Destructor) {
					Report.Error (711, m.Location, "`{0}': Static classes cannot contain destructor", GetSignatureForError ());
					continue;
				}

				if (m is Indexer) {
					Report.Error (720, m.Location, "`{0}': cannot declare indexers in a static class", m.GetSignatureForError ());
					continue;
				}

				if ((m.ModFlags & Modifiers.STATIC) != 0 || m is Enum || m is Delegate)
					continue;

				if (m is Constructor) {
					Report.Error (710, m.Location, "`{0}': Static classes cannot have instance constructors", GetSignatureForError ());
					continue;
				}

				Method method = m as Method;
				if (method != null && method.ParameterInfo.HasExtensionMethodType) {
					Report.Error (1105, m.Location, "`{0}': Extension methods must be declared static", m.GetSignatureForError ());
					continue;
				}

				Report.Error (708, m.Location, "`{0}': cannot declare instance members in a static class", m.GetSignatureForError ());
			}

			base.DefineContainerMembers (list);
		}

		protected override bool DoDefineMembers ()
		{
			if ((ModFlags & Modifiers.ABSTRACT) == Modifiers.ABSTRACT && (ModFlags & (Modifiers.SEALED | Modifiers.STATIC)) != 0) {
				Report.Error (418, Location, "`{0}': an abstract class cannot be sealed or static", GetSignatureForError ());
			}

			if ((ModFlags & (Modifiers.SEALED | Modifiers.STATIC)) == (Modifiers.SEALED | Modifiers.STATIC)) {
				Report.Error (441, Location, "`{0}': a class cannot be both static and sealed", GetSignatureForError ());
			}

			if (InstanceConstructors == null && !IsStatic)
				DefineDefaultConstructor (false);

			return base.DoDefineMembers ();
		}

		public override void Emit ()
		{
			base.Emit ();

			if ((ModFlags & Modifiers.METHOD_EXTENSION) != 0)
				Module.PredefinedAttributes.Extension.EmitAttribute (TypeBuilder);

			if (base_type != null && base_type.HasDynamicElement) {
				Module.PredefinedAttributes.Dynamic.EmitAttribute (TypeBuilder, base_type, Location);
			}
		}

		protected override TypeSpec[] ResolveBaseTypes (out FullNamedExpression base_class)
		{
			var ifaces = base.ResolveBaseTypes (out base_class);

			if (base_class == null) {
				if (spec.BuiltinType != BuiltinTypeSpec.Type.Object)
					base_type = Compiler.BuiltinTypes.Object;
			} else {
				if (base_type.IsGenericParameter){
					Report.Error (689, base_class.Location, "`{0}': Cannot derive from type parameter `{1}'",
						GetSignatureForError (), base_type.GetSignatureForError ());
				} else if (base_type.IsStatic) {
					Report.SymbolRelatedToPreviousError (base_type);
					Report.Error (709, Location, "`{0}': Cannot derive from static class `{1}'",
						GetSignatureForError (), base_type.GetSignatureForError ());
				} else if (base_type.IsSealed) {
					Report.SymbolRelatedToPreviousError (base_type);
					Report.Error (509, Location, "`{0}': cannot derive from sealed type `{1}'",
						GetSignatureForError (), base_type.GetSignatureForError ());
				} else if (PartialContainer.IsStatic && base_type.BuiltinType != BuiltinTypeSpec.Type.Object) {
					Report.Error (713, Location, "Static class `{0}' cannot derive from type `{1}'. Static classes must derive from object",
						GetSignatureForError (), base_type.GetSignatureForError ());
				}

				switch (base_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Enum:
				case BuiltinTypeSpec.Type.ValueType:
				case BuiltinTypeSpec.Type.MulticastDelegate:
				case BuiltinTypeSpec.Type.Delegate:
				case BuiltinTypeSpec.Type.Array:
					if (!(spec is BuiltinTypeSpec)) {
						Report.Error (644, Location, "`{0}' cannot derive from special class `{1}'",
							GetSignatureForError (), base_type.GetSignatureForError ());

						base_type = Compiler.BuiltinTypes.Object;
					}
					break;
				}

				if (!IsAccessibleAs (base_type)) {
					Report.SymbolRelatedToPreviousError (base_type);
					Report.Error (60, Location, "Inconsistent accessibility: base class `{0}' is less accessible than class `{1}'",
						base_type.GetSignatureForError (), GetSignatureForError ());
				}
			}

			if (PartialContainer.IsStatic && ifaces != null) {
				foreach (var t in ifaces)
					Report.SymbolRelatedToPreviousError (t);
				Report.Error (714, Location, "Static class `{0}' cannot implement interfaces", GetSignatureForError ());
			}

			return ifaces;
		}

		/// Search for at least one defined condition in ConditionalAttribute of attribute class
		/// Valid only for attribute classes.
		public override string[] ConditionalConditions ()
		{
			if ((caching_flags & (Flags.Excluded_Undetected | Flags.Excluded)) == 0)
				return null;

			caching_flags &= ~Flags.Excluded_Undetected;

			if (OptAttributes == null)
				return null;

			Attribute[] attrs = OptAttributes.SearchMulti (Module.PredefinedAttributes.Conditional);
			if (attrs == null)
				return null;

			string[] conditions = new string[attrs.Length];
			for (int i = 0; i < conditions.Length; ++i)
				conditions[i] = attrs[i].GetConditionalAttributeValue ();

			caching_flags |= Flags.Excluded;
			return conditions;
		}

		//
		// FIXME: How do we deal with the user specifying a different
		// layout?
		//
		protected override TypeAttributes TypeAttr {
			get {
				TypeAttributes ta = base.TypeAttr | TypeAttributes.AutoLayout | TypeAttributes.Class;
				if (IsStatic)
					ta |= StaticClassAttribute;
				return ta;
			}
		}
	}

	public sealed class Struct : ClassOrStruct {

		bool is_unmanaged, has_unmanaged_check_done;
		bool InTransit;

		// <summary>
		//   Modifiers allowed in a struct declaration
		// </summary>
		const Modifiers AllowedModifiers =
			Modifiers.NEW       |
			Modifiers.PUBLIC    |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL  |
			Modifiers.UNSAFE    |
			Modifiers.PRIVATE;

		public Struct (NamespaceContainer ns, DeclSpace parent, MemberName name,
			       Modifiers mod, Attributes attrs)
			: base (ns, parent, name, attrs, MemberKind.Struct)
		{
			var accmods = parent.Parent == null ? Modifiers.INTERNAL : Modifiers.PRIVATE;			
			this.ModFlags = ModifiersExtensions.Check (AllowedModifiers, mod, accmods, Location, Report) | Modifiers.SEALED ;
			spec = new TypeSpec (Kind, null, this, null, ModFlags);
		}

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Struct;
			}
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			base.ApplyAttributeBuilder (a, ctor, cdata, pa);

			//
			// When struct constains fixed fixed and struct layout has explicitly
			// set CharSet, its value has to be propagated to compiler generated
			// fixed types
			//
			if (a.Type == pa.StructLayout && Fields != null) {
				var value = a.GetNamedValue ("CharSet");
				if (value == null)
					return;

				for (int i = 0; i < Fields.Count; ++i) {
					FixedField ff = Fields [i] as FixedField;
					if (ff == null)
						continue;

					ff.CharSet = (CharSet) System.Enum.Parse (typeof (CharSet), value.GetValue ().ToString ());
				}
			}
		}

		bool CheckStructCycles (Struct s)
		{
			if (s.Fields == null)
				return true;

			if (s.InTransit)
				return false;

			s.InTransit = true;
			foreach (FieldBase field in s.Fields) {
				TypeSpec ftype = field.Spec.MemberType;
				if (!ftype.IsStruct)
					continue;

				if (ftype is BuiltinTypeSpec)
					continue;

				foreach (var targ in ftype.TypeArguments) {
					if (!CheckFieldTypeCycle (targ)) {
						Report.Error (523, field.Location,
							"Struct member `{0}' of type `{1}' causes a cycle in the struct layout",
							field.GetSignatureForError (), ftype.GetSignatureForError ());
						break;
					}
				}

				if ((field.IsStatic && (!ftype.IsGeneric || ftype == CurrentType)))
					continue;

				if (!CheckFieldTypeCycle (ftype)) {
					Report.Error (523, field.Location,
						"Struct member `{0}' of type `{1}' causes a cycle in the struct layout",
						field.GetSignatureForError (), ftype.GetSignatureForError ());
					break;
				}
			}

			s.InTransit = false;
			return true;
		}

		bool CheckFieldTypeCycle (TypeSpec ts)
		{
			var fts = ts.MemberDefinition as Struct;
			if (fts == null)
				return true;

			return CheckStructCycles (fts);
		}

		public override void Emit ()
		{
			CheckStructCycles (this);

			base.Emit ();
		}

		public override bool IsUnmanagedType ()
		{
			if (fields == null)
				return true;

			if (has_unmanaged_check_done)
				return is_unmanaged;

			if (requires_delayed_unmanagedtype_check)
				return true;

			requires_delayed_unmanagedtype_check = true;

			foreach (FieldBase f in fields) {
				if (f.IsStatic)
					continue;

				// It can happen when recursive unmanaged types are defined
				// struct S { S* s; }
				TypeSpec mt = f.MemberType;
				if (mt == null) {
					return true;
				}

				while (mt.IsPointer)
					mt = TypeManager.GetElementType (mt);

				if (mt.IsGenericOrParentIsGeneric || mt.IsGenericParameter) {
					has_unmanaged_check_done = true;
					return false;
				}

				if (TypeManager.IsUnmanagedType (mt))
					continue;

				has_unmanaged_check_done = true;
				return false;
			}

			has_unmanaged_check_done = true;
			is_unmanaged = true;
			return true;
		}

		protected override TypeSpec[] ResolveBaseTypes (out FullNamedExpression base_class)
		{
			var ifaces = base.ResolveBaseTypes (out base_class);
			base_type = Compiler.BuiltinTypes.ValueType;
			return ifaces;
		}

		protected override TypeAttributes TypeAttr {
			get {
				const
				TypeAttributes DefaultTypeAttributes =
					TypeAttributes.SequentialLayout | 
					TypeAttributes.Sealed ; 

				return base.TypeAttr | DefaultTypeAttributes;
			}
		}

		public override void RegisterFieldForInitialization (MemberCore field, FieldInitializer expression)
		{
			if ((field.ModFlags & Modifiers.STATIC) == 0) {
				Report.Error (573, field.Location, "`{0}': Structs cannot have instance field initializers",
					field.GetSignatureForError ());
				return;
			}
			base.RegisterFieldForInitialization (field, expression);
		}

	}

	/// <summary>
	///   Interfaces
	/// </summary>
	public sealed class Interface : TypeContainer {

		/// <summary>
		///   Modifiers allowed in a class declaration
		/// </summary>
		const Modifiers AllowedModifiers =
			Modifiers.NEW       |
			Modifiers.PUBLIC    |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL  |
		 	Modifiers.UNSAFE    |
			Modifiers.PRIVATE;

		public Interface (NamespaceContainer ns, DeclSpace parent, MemberName name, Modifiers mod,
				  Attributes attrs)
			: base (ns, parent, name, attrs, MemberKind.Interface)
		{
			var accmods = parent.Parent == null ? Modifiers.INTERNAL : Modifiers.PRIVATE;

			this.ModFlags = ModifiersExtensions.Check (AllowedModifiers, mod, accmods, name.Location, Report);
			spec = new TypeSpec (Kind, null, this, null, ModFlags);
		}

		#region Properties

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Interface;
			}
		}

		protected override TypeAttributes TypeAttr {
			get {
				const TypeAttributes DefaultTypeAttributes =
					TypeAttributes.AutoLayout |
					TypeAttributes.Abstract |
					TypeAttributes.Interface;

				return base.TypeAttr | DefaultTypeAttributes;
			}
		}

		#endregion

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Type == pa.ComImport && !attributes.Contains (pa.Guid)) {
				a.Error_MissingGuidAttribute ();
				return;
			}

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ())
				return false;

			if (iface_exprs != null) {
				foreach (var iface in iface_exprs) {
					if (iface.IsCLSCompliant ())
						continue;

					Report.SymbolRelatedToPreviousError (iface);
					Report.Warning (3027, 1, Location, "`{0}' is not CLS-compliant because base interface `{1}' is not CLS-compliant",
						GetSignatureForError (), TypeManager.CSharpName (iface));
				}
			}

			return true;
		}
	}

	public abstract class InterfaceMemberBase : MemberBase
	{
		//
		// Common modifiers allowed in a class declaration
		//
		protected const Modifiers AllowedModifiersClass =
			Modifiers.NEW |
			Modifiers.PUBLIC |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL |
			Modifiers.PRIVATE |
			Modifiers.STATIC |
			Modifiers.VIRTUAL |
			Modifiers.SEALED |
			Modifiers.OVERRIDE |
			Modifiers.ABSTRACT |
			Modifiers.UNSAFE |
			Modifiers.EXTERN;

		//
		// Common modifiers allowed in a struct declaration
		//
		protected const Modifiers AllowedModifiersStruct =
			Modifiers.NEW |
			Modifiers.PUBLIC |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL |
			Modifiers.PRIVATE |
			Modifiers.STATIC |
			Modifiers.OVERRIDE |
			Modifiers.UNSAFE |
			Modifiers.EXTERN;

		//
		// Common modifiers allowed in a interface declaration
		//
		protected const Modifiers AllowedModifiersInterface =
			Modifiers.NEW |
			Modifiers.UNSAFE;

		//
		// Whether this is an interface member.
		//
		public bool IsInterface;

		//
		// If true, this is an explicit interface implementation
		//
		public bool IsExplicitImpl;

		protected bool is_external_implementation;

		//
		// The interface type we are explicitly implementing
		//
		public TypeSpec InterfaceType;

		//
		// The method we're overriding if this is an override method.
		//
		protected MethodSpec base_method;

		readonly Modifiers explicit_mod_flags;
		public MethodAttributes flags;

		public InterfaceMemberBase (DeclSpace parent, GenericMethod generic,
				   FullNamedExpression type, Modifiers mod, Modifiers allowed_mod,
				   MemberName name, Attributes attrs)
			: base (parent, generic, type, mod, allowed_mod, Modifiers.PRIVATE,
				name, attrs)
		{
			IsInterface = parent.PartialContainer.Kind == MemberKind.Interface;
			IsExplicitImpl = (MemberName.Left != null);
			explicit_mod_flags = mod;
		}

		public abstract Variance ExpectedMemberTypeVariance { get; }
		
		protected override bool CheckBase ()
		{
			if (!base.CheckBase ())
				return false;

			if ((caching_flags & Flags.MethodOverloadsExist) != 0)
				CheckForDuplications ();
			
			if (IsExplicitImpl)
				return true;

			// For System.Object only
			if (Parent.BaseType == null)
				return true;

			MemberSpec candidate;
			bool overrides = false;
			var base_member = FindBaseMember (out candidate, ref overrides);

			if ((ModFlags & Modifiers.OVERRIDE) != 0) {
				if (base_member == null) {
					if (candidate == null) {
						if (this is Method && ((Method)this).ParameterInfo.IsEmpty && MemberName.Name == Destructor.MetadataName && MemberName.Arity == 0) {
							Report.Error (249, Location, "Do not override `{0}'. Use destructor syntax instead",
								"object.Finalize()");
						} else {
							Report.Error (115, Location, "`{0}' is marked as an override but no suitable {1} found to override",
								GetSignatureForError (), SimpleName.GetMemberType (this));
						}
					} else {
						Report.SymbolRelatedToPreviousError (candidate);
						if (this is Event)
							Report.Error (72, Location, "`{0}': cannot override because `{1}' is not an event",
								GetSignatureForError (), TypeManager.GetFullNameSignature (candidate));
						else if (this is PropertyBase)
							Report.Error (544, Location, "`{0}': cannot override because `{1}' is not a property",
								GetSignatureForError (), TypeManager.GetFullNameSignature (candidate));
						else
							Report.Error (505, Location, "`{0}': cannot override because `{1}' is not a method",
								GetSignatureForError (), TypeManager.GetFullNameSignature (candidate));
					}

					return false;
				}

				//
				// Handles ambiguous overrides
				//
				if (candidate != null) {
					Report.SymbolRelatedToPreviousError (candidate);
					Report.SymbolRelatedToPreviousError (base_member);

					// Get member definition for error reporting
					var m1 = MemberCache.GetMember (base_member.DeclaringType.GetDefinition (), base_member);
					var m2 = MemberCache.GetMember (candidate.DeclaringType.GetDefinition (), candidate);

					Report.Error (462, Location,
						"`{0}' cannot override inherited members `{1}' and `{2}' because they have the same signature when used in type `{3}'",
						GetSignatureForError (), m1.GetSignatureForError (), m2.GetSignatureForError (), Parent.GetSignatureForError ());
				}

				if (!CheckOverrideAgainstBase (base_member))
					return false;

				ObsoleteAttribute oa = base_member.GetAttributeObsolete ();
				if (oa != null) {
					if (OptAttributes == null || !OptAttributes.Contains (Module.PredefinedAttributes.Obsolete)) {
						Report.SymbolRelatedToPreviousError (base_member);
						Report.Warning (672, 1, Location, "Member `{0}' overrides obsolete member `{1}'. Add the Obsolete attribute to `{0}'",
							GetSignatureForError (), TypeManager.GetFullNameSignature (base_member));
					}
				} else {
					if (OptAttributes != null && OptAttributes.Contains (Module.PredefinedAttributes.Obsolete)) {
						Report.SymbolRelatedToPreviousError (base_member);
						Report.Warning (809, 1, Location, "Obsolete member `{0}' overrides non-obsolete member `{1}'",
							GetSignatureForError (), TypeManager.GetFullNameSignature (base_member));
					}
				}

				base_method = base_member as MethodSpec;
				return true;
			}

			if (base_member == null && candidate != null && (!(candidate is IParametersMember) || !(this is IParametersMember)))
				base_member = candidate;

			if (base_member == null) {
				if ((ModFlags & Modifiers.NEW) != 0) {
					if (base_member == null) {
						Report.Warning (109, 4, Location, "The member `{0}' does not hide an inherited member. The new keyword is not required",
							GetSignatureForError ());
					}
				}
			} else {
				if ((ModFlags & Modifiers.NEW) == 0) {
					ModFlags |= Modifiers.NEW;
					if (!IsCompilerGenerated) {
						Report.SymbolRelatedToPreviousError (base_member);
						if (!IsInterface && (base_member.Modifiers & (Modifiers.ABSTRACT | Modifiers.VIRTUAL | Modifiers.OVERRIDE)) != 0) {
							Report.Warning (114, 2, Location, "`{0}' hides inherited member `{1}'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword",
								GetSignatureForError (), base_member.GetSignatureForError ());
						} else {
							Report.Warning (108, 2, Location, "`{0}' hides inherited member `{1}'. Use the new keyword if hiding was intended",
								GetSignatureForError (), base_member.GetSignatureForError ());
						}
					}
				}

				if (!IsInterface && base_member.IsAbstract && !overrides) {
					Report.SymbolRelatedToPreviousError (base_member);
					Report.Error (533, Location, "`{0}' hides inherited abstract member `{1}'",
						GetSignatureForError (), base_member.GetSignatureForError ());
				}
			}

			return true;
		}

		protected virtual bool CheckForDuplications ()
		{
			return Parent.MemberCache.CheckExistingMembersOverloads (this, ParametersCompiled.EmptyReadOnlyParameters);
		}

		//
		// Performs various checks on the MethodInfo `mb' regarding the modifier flags
		// that have been defined.
		//
		protected virtual bool CheckOverrideAgainstBase (MemberSpec base_member)
		{
			bool ok = true;

			if ((base_member.Modifiers & (Modifiers.ABSTRACT | Modifiers.VIRTUAL | Modifiers.OVERRIDE)) == 0) {
				Report.SymbolRelatedToPreviousError (base_member);
				Report.Error (506, Location,
					"`{0}': cannot override inherited member `{1}' because it is not marked virtual, abstract or override",
					 GetSignatureForError (), TypeManager.CSharpSignature (base_member));
				ok = false;
			}

			// Now we check that the overriden method is not final	
			if ((base_member.Modifiers & Modifiers.SEALED) != 0) {
				Report.SymbolRelatedToPreviousError (base_member);
				Report.Error (239, Location, "`{0}': cannot override inherited member `{1}' because it is sealed",
							  GetSignatureForError (), TypeManager.CSharpSignature (base_member));
				ok = false;
			}

			var base_member_type = ((IInterfaceMemberSpec) base_member).MemberType;
			if (!TypeSpecComparer.Override.IsEqual (MemberType, base_member_type)) {
				Report.SymbolRelatedToPreviousError (base_member);
				if (this is PropertyBasedMember) {
					Report.Error (1715, Location, "`{0}': type must be `{1}' to match overridden member `{2}'",
						GetSignatureForError (), TypeManager.CSharpName (base_member_type), TypeManager.CSharpSignature (base_member));
				} else {
					Report.Error (508, Location, "`{0}': return type must be `{1}' to match overridden member `{2}'",
						GetSignatureForError (), TypeManager.CSharpName (base_member_type), TypeManager.CSharpSignature (base_member));
				}
				ok = false;
			}

			return ok;
		}

		protected static bool CheckAccessModifiers (MemberCore this_member, MemberSpec base_member)
		{
			var thisp = this_member.ModFlags & Modifiers.AccessibilityMask;
			var base_classp = base_member.Modifiers & Modifiers.AccessibilityMask;

			if ((base_classp & (Modifiers.PROTECTED | Modifiers.INTERNAL)) == (Modifiers.PROTECTED | Modifiers.INTERNAL)) {
				//
				// It must be at least "protected"
				//
				if ((thisp & Modifiers.PROTECTED) == 0) {
					return false;
				}

				//
				// when overriding protected internal, the method can be declared
				// protected internal only within the same assembly or assembly
				// which has InternalsVisibleTo
				//
				if ((thisp & Modifiers.INTERNAL) != 0) {
					return base_member.DeclaringType.MemberDefinition.IsInternalAsPublic (this_member.Module.DeclaringAssembly);
				}

				//
				// protected overriding protected internal inside same assembly
				// requires internal modifier as well
				//
				if (base_member.DeclaringType.MemberDefinition.IsInternalAsPublic (this_member.Module.DeclaringAssembly)) {
					return false;
				}

				return true;
			}

			return thisp == base_classp;
		}

		public override bool Define ()
		{
			if (IsInterface) {
				ModFlags = Modifiers.PUBLIC | Modifiers.ABSTRACT |
					Modifiers.VIRTUAL | (ModFlags & (Modifiers.UNSAFE | Modifiers.NEW));

				flags = MethodAttributes.Public |
					MethodAttributes.Abstract |
					MethodAttributes.HideBySig |
					MethodAttributes.NewSlot |
					MethodAttributes.Virtual;
			} else {
				Parent.PartialContainer.MethodModifiersValid (this);

				flags = ModifiersExtensions.MethodAttr (ModFlags);
			}

			if (IsExplicitImpl) {
				InterfaceType = MemberName.Left.GetTypeExpression ().ResolveAsType (Parent);
				if (InterfaceType == null)
					return false;

				if ((ModFlags & Modifiers.PARTIAL) != 0) {
					Report.Error (754, Location, "A partial method `{0}' cannot explicitly implement an interface",
						GetSignatureForError ());
				}

				if (!InterfaceType.IsInterface) {
					Report.SymbolRelatedToPreviousError (InterfaceType);
					Report.Error (538, Location, "The type `{0}' in explicit interface declaration is not an interface",
						TypeManager.CSharpName (InterfaceType));
				} else {
					Parent.PartialContainer.VerifyImplements (this);
				}

				ModifiersExtensions.Check (Modifiers.AllowedExplicitImplFlags, explicit_mod_flags, 0, Location, Report);
			}

			return base.Define ();
		}

		protected bool DefineParameters (ParametersCompiled parameters)
		{
			if (!parameters.Resolve (this))
				return false;

			bool error = false;
			for (int i = 0; i < parameters.Count; ++i) {
				Parameter p = parameters [i];

				if (p.HasDefaultValue && (IsExplicitImpl || this is Operator || (this is Indexer && parameters.Count == 1)))
					p.Warning_UselessOptionalParameter (Report);

				if (p.CheckAccessibility (this))
					continue;

				TypeSpec t = parameters.Types [i];
				Report.SymbolRelatedToPreviousError (t);
				if (this is Indexer)
					Report.Error (55, Location,
						      "Inconsistent accessibility: parameter type `{0}' is less accessible than indexer `{1}'",
						      TypeManager.CSharpName (t), GetSignatureForError ());
				else if (this is Operator)
					Report.Error (57, Location,
						      "Inconsistent accessibility: parameter type `{0}' is less accessible than operator `{1}'",
						      TypeManager.CSharpName (t), GetSignatureForError ());
				else
					Report.Error (51, Location,
						"Inconsistent accessibility: parameter type `{0}' is less accessible than method `{1}'",
						TypeManager.CSharpName (t), GetSignatureForError ());
				error = true;
			}
			return !error;
		}

		protected override void DoMemberTypeDependentChecks ()
		{
			base.DoMemberTypeDependentChecks ();

			TypeManager.CheckTypeVariance (MemberType, ExpectedMemberTypeVariance, this);
		}

		public override void Emit()
		{
			// for extern static method must be specified either DllImport attribute or MethodImplAttribute.
			// We are more strict than csc and report this as an error because SRE does not allow emit that
			if ((ModFlags & Modifiers.EXTERN) != 0 && !is_external_implementation) {
				if (this is Constructor) {
					Report.Warning (824, 1, Location,
						"Constructor `{0}' is marked `external' but has no external implementation specified", GetSignatureForError ());
				} else {
					Report.Warning (626, 1, Location,
						"`{0}' is marked as an external but has no DllImport attribute. Consider adding a DllImport attribute to specify the external implementation",
						GetSignatureForError ());
				}
			}

			base.Emit ();
		}

		public override bool EnableOverloadChecks (MemberCore overload)
		{
			//
			// Two members can differ in their explicit interface
			// type parameter only
			//
			InterfaceMemberBase imb = overload as InterfaceMemberBase;
			if (imb != null && imb.IsExplicitImpl) {
				if (IsExplicitImpl) {
					caching_flags |= Flags.MethodOverloadsExist;
				}
				return true;
			}

			return IsExplicitImpl;
		}

		protected void Error_CannotChangeAccessModifiers (MemberCore member, MemberSpec base_member)
		{
			var base_modifiers = base_member.Modifiers;

			// Remove internal modifier from types which are not internally accessible
			if ((base_modifiers & Modifiers.AccessibilityMask) == (Modifiers.PROTECTED | Modifiers.INTERNAL) &&
				!base_member.DeclaringType.MemberDefinition.IsInternalAsPublic (member.Module.DeclaringAssembly))
				base_modifiers = Modifiers.PROTECTED;

			Report.SymbolRelatedToPreviousError (base_member);
			Report.Error (507, member.Location,
				"`{0}': cannot change access modifiers when overriding `{1}' inherited member `{2}'",
				member.GetSignatureForError (),
				ModifiersExtensions.AccessibilityName (base_modifiers),
				base_member.GetSignatureForError ());
		}

		protected void Error_StaticReturnType ()
		{
			Report.Error (722, Location,
				"`{0}': static types cannot be used as return types",
				MemberType.GetSignatureForError ());
		}

		/// <summary>
		/// Gets base method and its return type
		/// </summary>
		protected virtual MemberSpec FindBaseMember (out MemberSpec bestCandidate, ref bool overrides)
		{
			return MemberCache.FindBaseMember (this, out bestCandidate, ref overrides);
		}

		//
		// The "short" name of this property / indexer / event.  This is the
		// name without the explicit interface.
		//
		public string ShortName {
			get { return MemberName.Name; }
			set { SetMemberName (new MemberName (MemberName.Left, value, Location)); }
		}
		
		//
		// Returns full metadata method name
		//
		public string GetFullName (MemberName name)
		{
			return GetFullName (name.Name);
		}

		public string GetFullName (string name)
		{
			if (!IsExplicitImpl)
				return name;

			//
			// When dealing with explicit members a full interface type
			// name is added to member name to avoid possible name conflicts
			//
			// We use CSharpName which gets us full name with benefit of
			// replacing predefined names which saves some space and name
			// is still unique
			//
			return TypeManager.CSharpName (InterfaceType) + "." + name;
		}

		public override string GetSignatureForDocumentation ()
		{
			if (IsExplicitImpl)
				return Parent.GetSignatureForDocumentation () + "." + InterfaceType.GetExplicitNameSignatureForDocumentation () + "#" + ShortName;

			return Parent.GetSignatureForDocumentation () + "." + ShortName;
		}

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ()) {
				return false;
			}

			if (GenericMethod != null)
				GenericMethod.VerifyClsCompliance ();

			return true;
		}

		public override bool IsUsed 
		{
			get { return IsExplicitImpl || base.IsUsed; }
		}

	}

	public abstract class MemberBase : MemberCore
	{
		protected FullNamedExpression type_expr;
		protected TypeSpec member_type;

		public readonly DeclSpace ds;
		public readonly GenericMethod GenericMethod;
		
		public FullNamedExpression TypeName {
			get {
				return type_expr;
			}
		}
		
		protected MemberBase (DeclSpace parent, GenericMethod generic,
				      FullNamedExpression type, Modifiers mod, Modifiers allowed_mod, Modifiers def_mod,
				      MemberName name, Attributes attrs)
			: base (parent, name, attrs)
		{
			this.ds = generic != null ? generic : (DeclSpace) parent;
			this.type_expr = type;
			ModFlags = ModifiersExtensions.Check (allowed_mod, mod, def_mod, Location, Report);
			GenericMethod = generic;
			if (GenericMethod != null)
				GenericMethod.ModFlags = ModFlags;
		}

		#region Properties

		public TypeSpec MemberType {
			get {
				return member_type;
			}
		}

		public FullNamedExpression TypeExpression {
			get {
				return type_expr;
			}
		}

		#endregion

		//
		// Main member define entry
		//
		public override bool Define ()
		{
			DoMemberTypeIndependentChecks ();

			//
			// Returns false only when type resolution failed
			//
			if (!ResolveMemberType ())
				return false;

			DoMemberTypeDependentChecks ();
			return true;
		}

		//
		// Any type_name independent checks
		//
		protected virtual void DoMemberTypeIndependentChecks ()
		{
			if ((Parent.ModFlags & Modifiers.SEALED) != 0 &&
				(ModFlags & (Modifiers.VIRTUAL | Modifiers.ABSTRACT)) != 0) {
				Report.Error (549, Location, "New virtual member `{0}' is declared in a sealed class `{1}'",
					GetSignatureForError (), Parent.GetSignatureForError ());
			}
		}

		//
		// Any type_name dependent checks
		//
		protected virtual void DoMemberTypeDependentChecks ()
		{
			// verify accessibility
			if (!IsAccessibleAs (MemberType)) {
				Report.SymbolRelatedToPreviousError (MemberType);
				if (this is Property)
					Report.Error (53, Location,
						      "Inconsistent accessibility: property type `" +
						      TypeManager.CSharpName (MemberType) + "' is less " +
						      "accessible than property `" + GetSignatureForError () + "'");
				else if (this is Indexer)
					Report.Error (54, Location,
						      "Inconsistent accessibility: indexer return type `" +
						      TypeManager.CSharpName (MemberType) + "' is less " +
						      "accessible than indexer `" + GetSignatureForError () + "'");
				else if (this is MethodCore) {
					if (this is Operator)
						Report.Error (56, Location,
							      "Inconsistent accessibility: return type `" +
							      TypeManager.CSharpName (MemberType) + "' is less " +
							      "accessible than operator `" + GetSignatureForError () + "'");
					else
						Report.Error (50, Location,
							      "Inconsistent accessibility: return type `" +
							      TypeManager.CSharpName (MemberType) + "' is less " +
							      "accessible than method `" + GetSignatureForError () + "'");
				} else {
					Report.Error (52, Location,
						      "Inconsistent accessibility: field type `" +
						      TypeManager.CSharpName (MemberType) + "' is less " +
						      "accessible than field `" + GetSignatureForError () + "'");
				}
			}
		}

		protected bool IsTypePermitted ()
		{
			if (MemberType.IsSpecialRuntimeType) {
				Report.Error (610, Location, "Field or property cannot be of type `{0}'", TypeManager.CSharpName (MemberType));
				return false;
			}
			return true;
		}

		protected virtual bool CheckBase ()
		{
			CheckProtectedModifier ();

			return true;
		}

		public override string GetSignatureForDocumentation ()
		{
			return Parent.Name + "." + Name;
		}

		protected virtual bool ResolveMemberType ()
		{
			if (member_type != null)
				throw new InternalErrorException ("Multi-resolve");

			member_type = type_expr.ResolveAsType (this);
			return member_type != null;
		}
	}
}

