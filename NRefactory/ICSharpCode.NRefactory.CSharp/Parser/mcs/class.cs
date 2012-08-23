//
// class.cs: Class and Struct handlers
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Martin Baulig (martin@ximian.com)
//          Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002, 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2011 Novell, Inc
// Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Mono.CompilerServices.SymbolWriter;

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
	//
	// General types container, used as a base class for all constructs which can hold types
	//
	public abstract class TypeContainer : MemberCore
	{
		public readonly MemberKind Kind;
		public readonly string Basename;

		protected List<TypeContainer> containers;

		TypeDefinition main_container;

		protected Dictionary<string, MemberCore> defined_names;

		protected bool is_defined;

		public TypeContainer (TypeContainer parent, MemberName name, Attributes attrs, MemberKind kind)
			: base (parent, name, attrs)
		{
			this.Kind = kind;
			if (name != null)
				this.Basename = name.Basename;

			defined_names = new Dictionary<string, MemberCore> ();
		}

		public override TypeSpec CurrentType {
			get {
				return null;
			}
		}

		public Dictionary<string, MemberCore> DefinedNames {
			get {
				return defined_names;
			}
		}

		public TypeDefinition PartialContainer {
			get {
				return main_container;
			}
			protected set {
				main_container = value;
			}
		}

		public IList<TypeContainer> Containers {
			get {
				return containers;
			}
		}

		//
		// Any unattached attributes during parsing get added here. User
		// by FULL_AST mode
		//
		public Attributes UnattachedAttributes {
			get; set;
		}

		public virtual void AddCompilerGeneratedClass (CompilerGeneratedContainer c)
		{
			containers.Add (c);
		}

		public virtual void AddPartial (TypeDefinition next_part)
		{
			MemberCore mc;
			(PartialContainer ?? this).defined_names.TryGetValue (next_part.Basename, out mc);

			AddPartial (next_part, mc as TypeDefinition);
		}

		protected void AddPartial (TypeDefinition next_part, TypeDefinition existing)
		{
			next_part.ModFlags |= Modifiers.PARTIAL;

			if (existing == null) {
				AddTypeContainer (next_part);
				return;
			}

			if ((existing.ModFlags & Modifiers.PARTIAL) == 0) {
				if (existing.Kind != next_part.Kind) {
					AddTypeContainer (next_part);
				} else {
					Report.SymbolRelatedToPreviousError (next_part);
					Error_MissingPartialModifier (existing);
				}

				return;
			}

			if (existing.Kind != next_part.Kind) {
				Report.SymbolRelatedToPreviousError (existing);
				Report.Error (261, next_part.Location,
					"Partial declarations of `{0}' must be all classes, all structs or all interfaces",
					next_part.GetSignatureForError ());
			}

			if ((existing.ModFlags & Modifiers.AccessibilityMask) != (next_part.ModFlags & Modifiers.AccessibilityMask) &&
				((existing.ModFlags & Modifiers.DEFAULT_ACCESS_MODIFER) == 0 &&
				 (next_part.ModFlags & Modifiers.DEFAULT_ACCESS_MODIFER) == 0)) {
					 Report.SymbolRelatedToPreviousError (existing);
				Report.Error (262, next_part.Location,
					"Partial declarations of `{0}' have conflicting accessibility modifiers",
					next_part.GetSignatureForError ());
			}

			var tc_names = existing.CurrentTypeParameters;
			if (tc_names != null) {
				for (int i = 0; i < tc_names.Count; ++i) {
					var tp = next_part.MemberName.TypeParameters[i];
					if (tc_names[i].MemberName.Name != tp.MemberName.Name) {
						Report.SymbolRelatedToPreviousError (existing.Location, "");
						Report.Error (264, next_part.Location, "Partial declarations of `{0}' must have the same type parameter names in the same order",
							next_part.GetSignatureForError ());
						break;
					}

					if (tc_names[i].Variance != tp.Variance) {
						Report.SymbolRelatedToPreviousError (existing.Location, "");
						Report.Error (1067, next_part.Location, "Partial declarations of `{0}' must have the same type parameter variance modifiers",
							next_part.GetSignatureForError ());
						break;
					}
				}
			}

			if ((next_part.ModFlags & Modifiers.DEFAULT_ACCESS_MODIFER) != 0) {
				existing.ModFlags |= next_part.ModFlags & ~(Modifiers.DEFAULT_ACCESS_MODIFER | Modifiers.AccessibilityMask);
			} else if ((existing.ModFlags & Modifiers.DEFAULT_ACCESS_MODIFER) != 0) {
				existing.ModFlags &= ~(Modifiers.DEFAULT_ACCESS_MODIFER | Modifiers.AccessibilityMask);
				existing.ModFlags |= next_part.ModFlags;
			} else {
				existing.ModFlags |= next_part.ModFlags;
			}

			existing.Definition.Modifiers = existing.ModFlags;

			if (next_part.attributes != null) {
				if (existing.attributes == null)
					existing.attributes = next_part.attributes;
				else
					existing.attributes.AddAttributes (next_part.attributes.Attrs);
			}

			next_part.PartialContainer = existing;

			if (containers == null)
				containers = new List<TypeContainer> ();

			containers.Add (next_part);
		}

		public virtual void AddTypeContainer (TypeContainer tc)
		{
			containers.Add (tc);

			var tparams = tc.MemberName.TypeParameters;
			if (tparams != null && tc.PartialContainer != null) {
				var td = (TypeDefinition) tc;
				for (int i = 0; i < tparams.Count; ++i) {
					var tp = tparams[i];
					if (tp.MemberName == null)
						continue;

					td.AddNameToContainer (tp, tp.Name);
				}
			}
		}

		public virtual void CloseContainer ()
		{
			if (containers != null) {
				foreach (TypeContainer tc in containers) {
					tc.CloseContainer ();
				}
			}
		}

		public virtual void CreateMetadataName (StringBuilder sb)
		{
			if (Parent != null && Parent.MemberName != null)
				Parent.CreateMetadataName (sb);

			MemberName.CreateMetadataName (sb);
		}

		public virtual bool CreateContainer ()
		{
			if (containers != null) {
				foreach (TypeContainer tc in containers) {
					tc.CreateContainer ();
				}
			}

			return true;
		}

		public override bool Define ()
		{
			if (containers != null) {
				foreach (TypeContainer tc in containers) {
					tc.Define ();
				}
			}

			// Release cache used by parser only
			if (Module.Evaluator == null) {
				defined_names = null;
			} else {
				defined_names.Clear ();
			}

			return true;
		}

		public virtual void PrepareEmit ()
		{
			if (containers != null) {
				foreach (var t in containers) {
					try {
						t.PrepareEmit ();
					} catch (Exception e) {
						if (MemberName == MemberName.Null)
							throw;

						throw new InternalErrorException (t, e);
					}
				}
			}
		}

		public virtual bool DefineContainer ()
		{
			if (is_defined)
				return true;

			is_defined = true;

			DoDefineContainer ();

			if (containers != null) {
				foreach (TypeContainer tc in containers) {
					try {
						tc.DefineContainer ();
					} catch (Exception e) {
						if (MemberName == MemberName.Null)
							throw;

						throw new InternalErrorException (tc, e);
					}
				}
			}

			return true;
		}

		protected virtual void DefineNamespace ()
		{
			if (containers != null) {
				foreach (var tc in containers) {
					try {
						tc.DefineNamespace ();
					} catch (Exception e) {
						throw new InternalErrorException (tc, e);
					}
				}
			}
		}

		protected virtual void DoDefineContainer ()
		{
		}

		public virtual void EmitContainer ()
		{
			if (containers != null) {
				for (int i = 0; i < containers.Count; ++i)
					containers[i].EmitContainer ();
			}
		}

		protected void Error_MissingPartialModifier (MemberCore type)
		{
			Report.Error (260, type.Location,
				"Missing partial modifier on declaration of type `{0}'. Another partial declaration of this type exists",
				type.GetSignatureForError ());
		}

		public override string GetSignatureForDocumentation ()
		{
			if (Parent != null && Parent.MemberName != null)
				return Parent.GetSignatureForDocumentation () + "." + MemberName.GetSignatureForDocumentation ();

			return MemberName.GetSignatureForDocumentation ();
		}

		public override string GetSignatureForError ()
		{
			if (Parent != null && Parent.MemberName != null) 
				return Parent.GetSignatureForError () + "." + MemberName.GetSignatureForError ();

			return MemberName.GetSignatureForError ();
		}

		public string GetSignatureForMetadata ()
		{
#if STATIC
			var name = TypeNameParser.Escape (MemberName.Basename);

			if (Parent is TypeDefinition) {
				return Parent.GetSignatureForMetadata () + "+" + name;
			}

			if (Parent != null && Parent.MemberName != null)
				return Parent.GetSignatureForMetadata () + "." + name;

			return name;
#else
			throw new NotImplementedException ();
#endif
		}

		public virtual void RemoveContainer (TypeContainer cont)
		{
			if (containers != null)
				containers.Remove (cont);

			var tc = Parent == Module ? Module : this;
			tc.defined_names.Remove (cont.Basename);
		}

		public virtual void VerifyMembers ()
		{
			if (containers != null) {
				foreach (TypeContainer tc in containers)
					tc.VerifyMembers ();
			}
		}

		public override void WriteDebugSymbol (MonoSymbolFile file)
		{
			if (containers != null) {
				foreach (TypeContainer tc in containers) {
					tc.WriteDebugSymbol (file);
				}
			}
		}
	}

	public abstract class TypeDefinition : TypeContainer, ITypeDefinition
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

			public TypeParameters CurrentTypeParameters {
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

			public ExtensionMethodCandidates LookupExtensionMethod (TypeSpec extensionType, string name, int arity)
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
					var tp = CurrentTypeParameters;
					if (tp != null) {
						TypeParameter t = tp.Find (name);
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

		readonly List<MemberCore> members;

		// Holds a list of fields that have initializers
		protected List<FieldInitializer> initialized_fields;

		// Holds a list of static fields that have initializers
		protected List<FieldInitializer> initialized_static_fields;

		Dictionary<MethodSpec, Method> hoisted_base_call_proxies;

		Dictionary<string, FullNamedExpression> Cache = new Dictionary<string, FullNamedExpression> ();

		//
		// Points to the first non-static field added to the container.
		//
		// This is an arbitrary choice.  We are interested in looking at _some_ non-static field,
		// and the first one's as good as any.
		//
		protected FieldBase first_nonstatic_field;

		//
		// This one is computed after we can distinguish interfaces
		// from classes from the arraylist `type_bases' 
		//
		protected TypeSpec base_type;
		FullNamedExpression base_type_expr;	// TODO: It's temporary variable
		protected TypeSpec[] iface_exprs;

		protected List<FullNamedExpression> type_bases;

		TypeDefinition InTransit;

		public TypeBuilder TypeBuilder;
		GenericTypeParameterBuilder[] all_tp_builders;
		//
		// All recursive type parameters put together sharing same
		// TypeParameter instances
		//
		TypeParameters all_type_parameters;

		public const string DefaultIndexerName = "Item";

		bool has_normal_indexers;
		string indexer_name;
		protected bool requires_delayed_unmanagedtype_check;
		bool error;
		bool members_defined;
		bool members_defined_ok;
		protected bool has_static_constructor;

		private CachedMethods cached_method;

		protected TypeSpec spec;
		TypeSpec current_type;

		public int DynamicSitesCounter;
		public int AnonymousMethodsCounter;

		static readonly string[] attribute_targets = new string[] { "type" };

		/// <remarks>
		///  The pending methods that need to be implemented
		//   (interfaces or abstract methods)
		/// </remarks>
		PendingImplementation pending;

		public TypeDefinition (TypeContainer parent, MemberName name, Attributes attrs, MemberKind kind)
			: base (parent, name, attrs, kind)
		{
			PartialContainer = this;
			members = new List<MemberCore> ();
		}

		#region Properties

		public List<FullNamedExpression> BaseTypeExpressions {
			get {
				return type_bases;
			}
		}

		public override TypeSpec CurrentType {
			get {
				if (current_type == null) {
					if (IsGenericOrParentIsGeneric) {
						//
						// Switch to inflated version as it's used by all expressions
						//
						var targs = CurrentTypeParameters == null ? TypeSpec.EmptyTypes : CurrentTypeParameters.Types;
						current_type = spec.MakeGenericType (this, targs);
					} else {
						current_type = spec;
					}
				}

				return current_type;
			}
		}

		public override TypeParameters CurrentTypeParameters {
			get {
				return PartialContainer.MemberName.TypeParameters;
			}
		}

		int CurrentTypeParametersStartIndex {
			get {
				int total = all_tp_builders.Length;
				if (CurrentTypeParameters != null) {
					return total - CurrentTypeParameters.Count;
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

		public bool HasInstanceConstructor {
			get {
				return (caching_flags & Flags.HasInstanceConstructor) != 0;
			}
			set {
				caching_flags |= Flags.HasInstanceConstructor;
			}
		}

		// Indicated whether container has StructLayout attribute set Explicit
		public bool HasExplicitLayout {
			get { return (caching_flags & Flags.HasExplicitLayout) != 0; }
			set { caching_flags |= Flags.HasExplicitLayout; }
		}

		public bool HasOperators {
			get {
				return (caching_flags & Flags.HasUserOperators) != 0;
			}
			set {
				caching_flags |= Flags.HasUserOperators;
			}
		}

		public bool HasStructLayout {
			get { return (caching_flags & Flags.HasStructLayout) != 0; }
			set { caching_flags |= Flags.HasStructLayout; }
		}

		public TypeSpec[] Interfaces {
			get {
				return iface_exprs;
			}
		}

		public bool IsGenericOrParentIsGeneric {
			get {
				return all_type_parameters != null;
			}
		}

		public bool IsTopLevel {
			get {
				return !(Parent is TypeDefinition);
			}
		}

		public bool IsPartial {
			get {
				return (ModFlags & Modifiers.PARTIAL) != 0;
			}
		}

		//
		// Returns true for secondary partial containers
		//
		bool IsPartialPart {
			get {
				return PartialContainer != this;
			}
		}

		public MemberCache MemberCache {
			get {
				return spec.MemberCache;
			}
		}

		public List<MemberCore> Members {
			get {
				return members;
			}
		}

		string ITypeDefinition.Namespace {
			get {
				var p = Parent;
				while (p.Kind != MemberKind.Namespace)
					p = p.Parent;

				return p.MemberName == null ? null : p.GetSignatureForError ();
			}
		}

		public TypeParameters TypeParametersAll {
			get {
				return all_type_parameters;
			}
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_targets;
			}
		}

#if FULL_AST
		public bool HasOptionalSemicolon {
			get;
			private set;
		}
		Location optionalSemicolon;
		public Location OptionalSemicolon {
			get {
				return optionalSemicolon;
			}
			set {
				optionalSemicolon = value;
				HasOptionalSemicolon = true;
			}
		}
#endif

		#endregion

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public void AddMember (MemberCore symbol)
		{
			if (symbol.MemberName.ExplicitInterface != null) {
				if (!(Kind == MemberKind.Class || Kind == MemberKind.Struct)) {
					Report.Error (541, symbol.Location,
						"`{0}': explicit interface declaration can only be declared in a class or struct",
						symbol.GetSignatureForError ());
				}
			}

			AddNameToContainer (symbol, symbol.MemberName.Basename);
			members.Add (symbol);
		}

		public override void AddTypeContainer (TypeContainer tc)
		{
			AddNameToContainer (tc, tc.Basename);

			if (containers == null)
				containers = new List<TypeContainer> ();

			members.Add (tc);
			base.AddTypeContainer (tc);
		}

		public override void AddCompilerGeneratedClass (CompilerGeneratedContainer c)
		{
			members.Add (c);

			if (containers == null)
				containers = new List<TypeContainer> ();

			base.AddCompilerGeneratedClass (c);
		}

		//
		// Adds the member to defined_names table. It tests for duplications and enclosing name conflicts
		//
		public virtual void AddNameToContainer (MemberCore symbol, string name)
		{
			if (((ModFlags | symbol.ModFlags) & Modifiers.COMPILER_GENERATED) != 0)
				return;

			MemberCore mc;
			if (!PartialContainer.defined_names.TryGetValue (name, out mc)) {
				PartialContainer.defined_names.Add (name, symbol);
				return;
			}

			if (symbol.EnableOverloadChecks (mc))
				return;

			InterfaceMemberBase im = mc as InterfaceMemberBase;
			if (im != null && im.IsExplicitImpl)
				return;

			Report.SymbolRelatedToPreviousError (mc);
			if ((mc.ModFlags & Modifiers.PARTIAL) != 0 && (symbol is ClassOrStruct || symbol is Interface)) {
				Error_MissingPartialModifier (symbol);
				return;
			}

			if (symbol is TypeParameter) {
				Report.Error (692, symbol.Location,
					"Duplicate type parameter `{0}'", symbol.GetSignatureForError ());
			} else {
				Report.Error (102, symbol.Location,
					"The type `{0}' already contains a definition for `{1}'",
					GetSignatureForError (), name);
			}

			return;
		}

		public void AddConstructor (Constructor c)
		{
			AddConstructor (c, false);
		}

		public void AddConstructor (Constructor c, bool isDefault)
		{
			bool is_static = (c.ModFlags & Modifiers.STATIC) != 0;
			if (!isDefault)
				AddNameToContainer (c, is_static ? Constructor.TypeConstructorName : Constructor.ConstructorName);

			if (is_static && c.ParameterInfo.IsEmpty) {
				PartialContainer.has_static_constructor = true;
			} else {
				PartialContainer.HasInstanceConstructor = true;
			}

			members.Add (c);
		}

		public bool AddField (FieldBase field)
		{
			AddMember (field);

			if ((field.ModFlags & Modifiers.STATIC) != 0)
				return true;

			var first_field = PartialContainer.first_nonstatic_field;
			if (first_field == null) {
				PartialContainer.first_nonstatic_field = field;
				return true;
			}

			if (Kind == MemberKind.Struct && first_field.Parent != field.Parent) {
				Report.SymbolRelatedToPreviousError (first_field.Parent);
				Report.Warning (282, 3, field.Location,
					"struct instance field `{0}' found in different declaration from instance field `{1}'",
					field.GetSignatureForError (), first_field.GetSignatureForError ());
			}
			return true;
		}

		/// <summary>
		/// Indexer has special handling in constrast to other AddXXX because the name can be driven by IndexerNameAttribute
		/// </summary>
		public void AddIndexer (Indexer i)
		{
			members.Add (i);
		}

		public void AddOperator (Operator op)
		{
			PartialContainer.HasOperators = true;
			AddMember (op);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (has_normal_indexers && a.Type == pa.DefaultMember) {
				Report.Error (646, a.Location, "Cannot specify the `DefaultMember' attribute on type containing an indexer");
				return;
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

		public TypeSpec BaseType {
			get {
				return spec.BaseType;
			}
		}

		protected virtual TypeAttributes TypeAttr {
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
				return PartialContainer.CurrentTypeParameters.Types;
			}
		}

		public string GetAttributeDefaultMember ()
		{
			return indexer_name ?? DefaultIndexerName;
		}

		public bool IsComImport {
			get {
				if (OptAttributes == null)
					return false;

				return OptAttributes.Contains (Module.PredefinedAttributes.ComImport);
			}
		}

		public virtual void RegisterFieldForInitialization (MemberCore field, FieldInitializer expression)
		{
			if (IsPartialPart)
				PartialContainer.RegisterFieldForInitialization (field, expression);

			if ((field.ModFlags & Modifiers.STATIC) != 0){
				if (initialized_static_fields == null) {
					HasStaticFieldInitializer = true;
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
			Debug.Assert (!IsPartialPart);

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
					} else if (!fi.IsSideEffectFree) {
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

			foreach (var member in members)
				member.GenerateDocComment (builder);
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

		public virtual CompilationSourceFile GetCompilationSourceFile ()
		{
			TypeContainer ns = Parent;
			while (true) {
				var sf = ns as CompilationSourceFile;
				if (sf != null)
					return sf;

				ns = ns.Parent;
			}
		}

		public virtual void AddBasesForPart (List<FullNamedExpression> bases)
		{
			type_bases = bases;
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
			List<Operator.OpType> found_matched = new List<Operator.OpType> ();

			for (int i = 0; i < members.Count; ++i) {
				var o_a = members[i] as Operator;
				if (o_a == null)
					continue;

				var o_type = o_a.OperatorType;
				if (o_type == Operator.OpType.Equality || o_type == Operator.OpType.Inequality)
					has_equality_or_inequality = true;

				if (found_matched.Contains (o_type))
					continue;

				var matching_type = o_a.GetMatchingOperator ();
				if (matching_type == Operator.OpType.TOP) {
					continue;
				}

				bool pair_found = false;
				for (int ii = i + 1; ii < members.Count; ++ii) {
					var o_b = members[ii] as Operator;
					if (o_b == null || o_b.OperatorType != matching_type)
						continue;

					if (!TypeSpecComparer.IsEqual (o_a.ReturnType, o_b.ReturnType))
						continue;

					if (!TypeSpecComparer.Equals (o_a.ParameterTypes, o_b.ParameterTypes))
						continue;

					found_matched.Add (matching_type);
					pair_found = true;
					break;
				}

				if (!pair_found) {
					Report.Error (216, o_a.Location,
						"The operator `{0}' requires a matching operator `{1}' to also be defined",
						o_a.GetSignatureForError (), Operator.GetName (matching_type));
				}
			}

			if (has_equality_or_inequality) {
				if (!HasEquals)
					Report.Warning (660, 2, Location, "`{0}' defines operator == or operator != but does not override Object.Equals(object o)",
						GetSignatureForError ());

				if (!HasGetHashCode)
					Report.Warning (661, 2, Location, "`{0}' defines operator == or operator != but does not override Object.GetHashCode()",
						GetSignatureForError ());
			}
		}

		public override void CreateMetadataName (StringBuilder sb)
		{
			if (Parent.MemberName != null) {
				Parent.CreateMetadataName (sb);

				if (sb.Length != 0) {
					sb.Append (".");
				}
			}

			sb.Append (MemberName.Basename);
		}
	
		bool CreateTypeBuilder ()
		{
			//
			// Sets .size to 1 for structs with no instance fields
			//
			int type_size = Kind == MemberKind.Struct && first_nonstatic_field == null ? 1 : 0;

			var parent_def = Parent as TypeDefinition;
			if (parent_def == null) {
				var sb = new StringBuilder ();
				CreateMetadataName (sb);
				TypeBuilder = Module.CreateBuilder (sb.ToString (), TypeAttr, type_size);
			} else {
				TypeBuilder = parent_def.TypeBuilder.DefineNestedType (Basename, TypeAttr, null, type_size);
			}

			if (DeclaringAssembly.Importer != null)
				DeclaringAssembly.Importer.AddCompiledType (TypeBuilder, spec);

			spec.SetMetaInfo (TypeBuilder);
			spec.MemberCache = new MemberCache (this);

			TypeParameters parentAllTypeParameters = null;
			if (parent_def != null) {
				spec.DeclaringType = Parent.CurrentType;
				parent_def.MemberCache.AddMember (spec);
				parentAllTypeParameters = parent_def.all_type_parameters;
			}

			if (MemberName.TypeParameters != null || parentAllTypeParameters != null) {
				var tparam_names = CreateTypeParameters (parentAllTypeParameters);

				all_tp_builders = TypeBuilder.DefineGenericParameters (tparam_names);

				if (CurrentTypeParameters != null)
					CurrentTypeParameters.Define (all_tp_builders, spec, CurrentTypeParametersStartIndex, this);
			}

			return true;
		}

		string[] CreateTypeParameters (TypeParameters parentAllTypeParameters)
		{
			string[] names;
			int parent_offset = 0;
			if (parentAllTypeParameters != null) {
				if (CurrentTypeParameters == null) {
					all_type_parameters = parentAllTypeParameters;
					return parentAllTypeParameters.GetAllNames ();
				}

				names = new string[parentAllTypeParameters.Count + CurrentTypeParameters.Count];
				all_type_parameters = new TypeParameters (names.Length);
				all_type_parameters.Add (parentAllTypeParameters);

				parent_offset = all_type_parameters.Count;
				for (int i = 0; i < parent_offset; ++i)
					names[i] = all_type_parameters[i].MemberName.Name;

			} else {
				names = new string[CurrentTypeParameters.Count];
			}

			for (int i = 0; i < CurrentTypeParameters.Count; ++i) {
				if (all_type_parameters != null)
					all_type_parameters.Add (MemberName.TypeParameters[i]);

				var name = CurrentTypeParameters[i].MemberName.Name;
				names[parent_offset + i] = name;
				for (int ii = 0; ii < parent_offset + i; ++ii) {
					if (names[ii] != name)
						continue;

					var tp = CurrentTypeParameters[i];
					var conflict = all_type_parameters[ii];

					tp.WarningParentNameConflict (conflict);
				}
			}

			if (all_type_parameters == null)
				all_type_parameters = CurrentTypeParameters;

			return names;
		}


		public SourceMethodBuilder CreateMethodSymbolEntry ()
		{
			if (Module.DeclaringAssembly.SymbolWriter == null)
				return null;

			var source_file = GetCompilationSourceFile ();
			if (source_file == null)
				return null;

			return new SourceMethodBuilder (source_file.SymbolUnitEntry);
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
				string name = CompilerGeneratedContainer.MakeName (method.Name, null, "BaseCallProxy", hoisted_base_call_proxies.Count);
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

				MemberName member_name;
				TypeArguments targs = null;
				if (method.IsGeneric) {
					//
					// Copy all base generic method type parameters info
					//
					var hoisted_tparams = method.GenericDefinition.TypeParameters;
					var tparams = new TypeParameters ();

					targs = new TypeArguments ();
					targs.Arguments = new TypeSpec[hoisted_tparams.Length];
					for (int i = 0; i < hoisted_tparams.Length; ++i) {
						var tp = hoisted_tparams[i];
						tparams.Add (new TypeParameter (tp, null, new MemberName (tp.Name, Location), null));

						targs.Add (new SimpleName (tp.Name, Location));
						targs.Arguments[i] = tp;
					}

					member_name = new MemberName (name, tparams, Location);
				} else {
					member_name = new MemberName (name);
				}

				// Compiler generated proxy
				proxy_method = new Method (this, new TypeExpression (method.ReturnType, Location),
					Modifiers.PRIVATE | Modifiers.COMPILER_GENERATED | Modifiers.DEBUGGER_HIDDEN,
					member_name, cloned_params, null);

				var block = new ToplevelBlock (Compiler, proxy_method.ParameterInfo, Location) {
					IsCompilerGenerated = true
				};

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

				members.Add (proxy_method);
				proxy_method.Define ();

				hoisted_base_call_proxies.Add (method, proxy_method);
			}

			return proxy_method.Spec;
		}

		protected bool DefineBaseTypes ()
		{
			iface_exprs = ResolveBaseTypes (out base_type_expr);
			bool set_base_type;

			if (IsPartialPart) {
				set_base_type = false;

				if (base_type_expr != null) {
					if (PartialContainer.base_type_expr != null && PartialContainer.base_type != base_type) {
						Report.SymbolRelatedToPreviousError (base_type_expr.Location, "");
						Report.Error (263, Location,
							"Partial declarations of `{0}' must not specify different base classes",
							GetSignatureForError ());
					} else {
						PartialContainer.base_type_expr = base_type_expr;
						PartialContainer.base_type = base_type;
						set_base_type = true;
					}
				}

				if (iface_exprs != null) {
					if (PartialContainer.iface_exprs == null)
						PartialContainer.iface_exprs = iface_exprs;
					else {
						var ifaces = new List<TypeSpec> (PartialContainer.iface_exprs);
						foreach (var iface_partial in iface_exprs) {
							if (ifaces.Contains (iface_partial))
								continue;

							ifaces.Add (iface_partial);
						}

						PartialContainer.iface_exprs = ifaces.ToArray ();
					}
				}

				PartialContainer.members.AddRange (members);
				if (containers != null) {
					if (PartialContainer.containers == null)
						PartialContainer.containers = new List<TypeContainer> ();

					PartialContainer.containers.AddRange (containers);
				}

				members_defined = members_defined_ok = true;
				caching_flags |= Flags.CloseTypeCreated;
			} else {
				set_base_type = true;
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
					
					if (!spec.AddInterfaceDefined (iface_type))
						continue;

					TypeBuilder.AddInterfaceImplementation (iface_type.GetMetaInfo ());

					// Ensure the base is always setup
					var compiled_iface = iface_type.MemberDefinition as Interface;
					if (compiled_iface != null) {
						// TODO: Need DefineBaseType only
						compiled_iface.DefineContainer ();
					}

					if (iface_type.Interfaces != null) {
						var base_ifaces = new List<TypeSpec> (iface_type.Interfaces);
						for (int i = 0; i < base_ifaces.Count; ++i) {
							var ii_iface_type = base_ifaces[i];
							if (spec.AddInterfaceDefined (ii_iface_type)) {
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

			if (set_base_type) {
				if (base_type != null) {
					spec.BaseType = base_type;

					// Set base type after type creation
					TypeBuilder.SetParent (base_type.GetMetaInfo ());
				} else {
					TypeBuilder.SetParent (null);
				}
			}

			return true;
		}

		public override void PrepareEmit ()
		{
			if ((caching_flags & Flags.CloseTypeCreated) != 0)
				return;

			foreach (var member in members) {
				var pm = member as IParametersMember;
				if (pm != null) {

					var p = pm.Parameters;
					if (p.IsEmpty)
						continue;

					((ParametersCompiled) p).ResolveDefaultValues (member);
				}

				var c = member as Const;
				if (c != null)
					c.DefineValue ();
			}

			base.PrepareEmit ();
		}

		//
		// Defines the type in the appropriate ModuleBuilder or TypeBuilder.
		//
		public override bool CreateContainer ()
		{
			if (TypeBuilder != null)
				return !error;

			if (error)
				return false;

			if (IsPartialPart) {
				spec = PartialContainer.spec;
				TypeBuilder = PartialContainer.TypeBuilder;
				all_tp_builders = PartialContainer.all_tp_builders;
				all_type_parameters = PartialContainer.all_type_parameters;
			} else {
				if (!CreateTypeBuilder ()) {
					error = true;
					return false;
				}
			}

			return base.CreateContainer ();
		}

		protected override void DoDefineContainer ()
		{
			DefineBaseTypes ();

			DoResolveTypeParameters ();
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

		void UpdateTypeParameterConstraints (TypeDefinition part)
		{
			for (int i = 0; i < CurrentTypeParameters.Count; i++) {
				if (CurrentTypeParameters[i].AddPartialConstraints (part, part.MemberName.TypeParameters[i]))
					continue;

				Report.SymbolRelatedToPreviousError (Location, "");
				Report.Error (265, part.Location,
					"Partial declarations of `{0}' have inconsistent constraints for type parameter `{1}'",
					GetSignatureForError (), CurrentTypeParameters[i].GetSignatureForError ());
			}
		}

		public override void RemoveContainer (TypeContainer cont)
		{
			base.RemoveContainer (cont);
			Members.Remove (cont);
			Cache.Remove (cont.Basename);
		}

		protected virtual bool DoResolveTypeParameters ()
		{
			var tparams = CurrentTypeParameters;
			if (tparams == null)
				return true;

			var base_context = new BaseContext (this);
			for (int i = 0; i < tparams.Count; ++i) {
				var tp = tparams[i];

				if (!tp.ResolveConstraints (base_context)) {
					error = true;
					return false;
				}
			}

			if (IsPartialPart) {
				PartialContainer.UpdateTypeParameterConstraints (this);
			}

			return true;
		}

		TypeSpec CheckRecursiveDefinition (TypeDefinition tc)
		{
			if (InTransit != null)
				return spec;

			InTransit = tc;

			if (base_type != null) {
				var ptc = base_type.MemberDefinition as TypeDefinition;
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

			base.Define ();

			return members_defined_ok;
		}

		protected virtual bool DoDefineMembers ()
		{
			Debug.Assert (!IsPartialPart);

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
					}

					if (iface_type.IsGenericOrParentIsGeneric) {
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

					if (IsGenericOrParentIsGeneric && base_type.IsAttribute) {
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

			if (Kind == MemberKind.Struct || Kind == MemberKind.Class) {
				pending = PendingImplementation.GetPendingImplementations (this);
			}

			var count = members.Count;		
			for (int i = 0; i < count; ++i) {
				var mc = members[i] as InterfaceMemberBase;
				if (mc == null || !mc.IsExplicitImpl)
					continue;

				try {
					mc.Define ();
				} catch (Exception e) {
					throw new InternalErrorException (mc, e);
				}
			}

			for (int i = 0; i < count; ++i) {
				var mc = members[i] as InterfaceMemberBase;
				if (mc != null && mc.IsExplicitImpl)
					continue;

				if (members[i] is TypeContainer)
					continue;

				try {
					members[i].Define ();
				} catch (Exception e) {
					throw new InternalErrorException (members[i], e);
				}
			}

			if (HasOperators) {
				CheckPairedOperators ();
			}

			if (requires_delayed_unmanagedtype_check) {
				requires_delayed_unmanagedtype_check = false;
				foreach (var member in members) {
					var f = member as Field;
					if (f != null && f.MemberType != null && f.MemberType.IsPointer)
						TypeManager.VerifyUnmanaged (Module, f.MemberType, f.Location);
				}
			}

			ComputeIndexerName();

			if (HasEquals && !HasGetHashCode) {
				Report.Warning (659, 3, Location,
					"`{0}' overrides Object.Equals(object) but does not override Object.GetHashCode()", GetSignatureForError ());
			}

			if (Kind == MemberKind.Interface && iface_exprs != null) {
				MemberCache.RemoveHiddenMembers (spec);
			}

			return true;
		}

		void ComputeIndexerName ()
		{
			var indexers = MemberCache.FindMembers (spec, MemberCache.IndexerNameAlias, true);
			if (indexers == null)
				return;

			string class_indexer_name = null;

			//
			// Check normal indexers for consistent name, explicit interface implementation
			// indexers are ignored
			//
			foreach (var indexer in indexers) {
				//
				// FindMembers can return unfiltered full hierarchy names
				//
				if (indexer.DeclaringType != spec)
					continue;

				has_normal_indexers = true;

				if (class_indexer_name == null) {
					indexer_name = class_indexer_name = indexer.Name;
					continue;
				}

				if (indexer.Name != class_indexer_name)
					Report.Error (668, ((Indexer)indexer.MemberDefinition).Location,
						"Two indexers have different names; the IndexerName attribute must be used with the same name on every indexer within a type");
			}
		}

		void EmitIndexerName ()
		{
			if (!has_normal_indexers)
				return;

			var ctor = Module.PredefinedMembers.DefaultMemberAttributeCtor.Get ();
			if (ctor == null)
				return;

			var encoder = new AttributeEncoder ();
			encoder.Encode (GetAttributeDefaultMember ());
			encoder.EncodeEmptyNamedArguments ();

			TypeBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), encoder.ToArray ());
		}

		public override void VerifyMembers ()
		{
			//
			// Check for internal or private fields that were never assigned
			//
			if (!IsCompilerGenerated && Compiler.Settings.WarningLevel >= 3 && this == PartialContainer) {
				bool is_type_exposed = Kind == MemberKind.Struct || IsExposedFromAssembly ();
				foreach (var member in members) {
					if (member is Event) {
						//
						// An event can be assigned from same class only, so we can report
						// this warning for all accessibility modes
						//
						if (!member.IsUsed)
							Report.Warning (67, 3, member.Location, "The event `{0}' is never used", member.GetSignatureForError ());

						continue;
					}

					if ((member.ModFlags & Modifiers.AccessibilityMask) != Modifiers.PRIVATE) {
						if (is_type_exposed)
							continue;

						member.SetIsUsed ();
					}

					var f = member as Field;
					if (f == null)
						continue;

					if (!member.IsUsed) {
						if ((member.caching_flags & Flags.IsAssigned) == 0) {
							Report.Warning (169, 3, member.Location, "The private field `{0}' is never used", member.GetSignatureForError ());
						} else {
							Report.Warning (414, 3, member.Location, "The private field `{0}' is assigned but its value is never used",
								member.GetSignatureForError ());
						}
						continue;
					}

					if ((f.caching_flags & Flags.IsAssigned) != 0)
						continue;

					//
					// Only report 649 on level 4
					//
					if (Compiler.Settings.WarningLevel < 4)
						continue;

					//
					// Don't be pedantic when type requires specific layout
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
						value = null;
					}

					if (value != null)
						value = " `" + value + "'";

					Report.Warning (649, 4, f.Location, "Field `{0}' is never assigned to, and will always have its default value{1}",
						f.GetSignatureForError (), value);
				}
			}

			base.VerifyMembers ();
		}

		public override void Emit ()
		{
			if (OptAttributes != null)
				OptAttributes.Emit ();

			if (!IsCompilerGenerated) {
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
						all_type_parameters[i].EmitConstraints (all_tp_builders [i]);
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

			for (int i = 0; i < members.Count; i++)
				members[i].Emit ();

			EmitIndexerName ();
			CheckAttributeClsCompliance ();

			if (pending != null)
				pending.VerifyPendingMethods ();
		}


		void CheckAttributeClsCompliance ()
		{
			if (!spec.IsAttribute || !IsExposedFromAssembly () || !Compiler.Settings.VerifyClsCompliance || !IsClsComplianceRequired ())
				return;

			foreach (var m in members) {
				var c = m as Constructor;
				if (c == null)
					continue;

				if (c.HasCompliantArgs)
					return;
			}

			Report.Warning (3015, 1, Location, "`{0}' has no accessible constructors which use only CLS-compliant types", GetSignatureForError ());
		}

		public sealed override void EmitContainer ()
		{
			if ((caching_flags & Flags.CloseTypeCreated) != 0)
				return;

			Emit ();
		}

		public override void CloseContainer ()
		{
			if ((caching_flags & Flags.CloseTypeCreated) != 0)
				return;

			// Close base type container first to avoid TypeLoadException
			if (spec.BaseType != null) {
				var btype = spec.BaseType.MemberDefinition as TypeContainer;
				if (btype != null) {
					btype.CloseContainer ();

					if ((caching_flags & Flags.CloseTypeCreated) != 0)
						return;
				}
			}

			try {
				caching_flags |= Flags.CloseTypeCreated;
				TypeBuilder.CreateType ();
			} catch (TypeLoadException) {
				//
				// This is fine, the code still created the type
				//
			} catch (Exception e) {
				throw new InternalErrorException (this, e);
			}

			base.CloseContainer ();
			
			containers = null;
			initialized_fields = null;
			initialized_static_fields = null;
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

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ())
				return false;

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

		public override bool IsClsComplianceRequired ()
		{
			if (IsPartialPart)
				return PartialContainer.IsClsComplianceRequired ();

			return base.IsClsComplianceRequired ();
		}

		bool ITypeDefinition.IsInternalAsPublic (IAssemblyDefinition assembly)
		{
			return Module.DeclaringAssembly == assembly;
		}

		public virtual bool IsUnmanagedType ()
		{
			return false;
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
				var tp = CurrentTypeParameters;
				if (tp != null) {
					TypeParameter tparam = tp.Find (name);
					if (tparam != null)
						e = new TypeParameterExpr (tparam, Location.Null);
				}
			}

			if (e == null) {
				TypeSpec t = LookupNestedTypeInHierarchy (name, arity);

				if (t != null && (t.IsAccessible (this) || mode == LookupMode.IgnoreAccessibility))
					e = new TypeExpression (t, Location.Null);
				else {
					e = Parent.LookupNamespaceOrType (name, arity, mode, loc);
				}
			}

			// TODO MemberCache: How to cache arity stuff ?
			if (arity == 0 && mode == LookupMode.Normal)
				Cache[name] = e;

			return e;
		}

		TypeSpec LookupNestedTypeInHierarchy (string name, int arity)
		{
			// Has any nested type
			// Does not work, because base type can have
			//if (PartialContainer.Types == null)
			//	return null;

			var container = PartialContainer.CurrentType;
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

		public override void WriteDebugSymbol (MonoSymbolFile file)
		{
			if (IsPartialPart)
				return;

			foreach (var m in members) {
				m.WriteDebugSymbol (file);
			}
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

	public abstract class ClassOrStruct : TypeDefinition
	{
		public const TypeAttributes StaticClassAttribute = TypeAttributes.Abstract | TypeAttributes.Sealed;

		SecurityType declarative_security;

		public ClassOrStruct (TypeContainer parent, MemberName name, Attributes attrs, MemberKind kind)
			: base (parent, name, attrs, kind)
		{
		}

		protected override TypeAttributes TypeAttr {
			get {
				TypeAttributes ta = base.TypeAttr;
				if (!has_static_constructor)
					ta |= TypeAttributes.BeforeFieldInit;

				if (Kind == MemberKind.Class) {
					ta |= TypeAttributes.AutoLayout | TypeAttributes.Class;
					if (IsStatic)
						ta |= StaticClassAttribute;
				} else {
					ta |= TypeAttributes.SequentialLayout;
				}

				return ta;
			}
		}

		public override void AddNameToContainer (MemberCore symbol, string name)
		{
			if (!(symbol is Constructor) && symbol.MemberName.Name == MemberName.Name) {
				if (symbol is TypeParameter) {
					Report.Error (694, symbol.Location,
						"Type parameter `{0}' has same name as containing type, or method",
						symbol.GetSignatureForError ());
					return;
				}
			
				InterfaceMemberBase imb = symbol as InterfaceMemberBase;
				if (imb == null || !imb.IsExplicitImpl) {
					Report.SymbolRelatedToPreviousError (this);
					Report.Error (542, symbol.Location, "`{0}': member names cannot be the same as their enclosing type",
						symbol.GetSignatureForError ());
					return;
				}
			}

			base.AddNameToContainer (symbol, name);
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
		protected Constructor DefineDefaultConstructor (bool is_static)
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

			var c = new Constructor (this, MemberName.Name, mods, null, ParametersCompiled.EmptyReadOnlyParameters, Location);
			c.Initializer = new GeneratedBaseInitializer (Location);
			
			AddConstructor (c, true);
			c.Block = new ToplevelBlock (Compiler, ParametersCompiled.EmptyReadOnlyParameters, Location) {
				IsCompilerGenerated = true
			};

			return c;
		}

		protected override bool DoDefineMembers ()
		{
			CheckProtectedModifier ();

			base.DoDefineMembers ();

			return true;
		}

		public override void Emit ()
		{
			if (!has_static_constructor && HasStaticFieldInitializer) {
				var c = DefineDefaultConstructor (true);
				c.Define ();
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
	}


	public sealed class Class : ClassOrStruct
	{
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

		public Class (TypeContainer parent, MemberName name, Modifiers mod, Attributes attrs)
			: base (parent, name, attrs, MemberKind.Class)
		{
			var accmods = IsTopLevel ? Modifiers.INTERNAL : Modifiers.PRIVATE;
			this.ModFlags = ModifiersExtensions.Check (AllowedModifiers, mod, accmods, Location, Report);
			spec = new TypeSpec (Kind, null, this, null, ModFlags);
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void AddBasesForPart (List<FullNamedExpression> bases)
		{
			var pmn = MemberName;
			if (pmn.Name == "Object" && !pmn.IsGeneric && Parent.MemberName.Name == "System" && Parent.MemberName.Left == null)
				Report.Error (537, Location,
					"The class System.Object cannot have a base class or implement an interface.");

			base.AddBasesForPart (bases);
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

			if (a.Type.IsConditionallyExcluded (this, Location))
				return;

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Class;
			}
		}

		protected override bool DoDefineMembers ()
		{
			if ((ModFlags & Modifiers.ABSTRACT) == Modifiers.ABSTRACT && (ModFlags & (Modifiers.SEALED | Modifiers.STATIC)) != 0) {
				Report.Error (418, Location, "`{0}': an abstract class cannot be sealed or static", GetSignatureForError ());
			}

			if ((ModFlags & (Modifiers.SEALED | Modifiers.STATIC)) == (Modifiers.SEALED | Modifiers.STATIC)) {
				Report.Error (441, Location, "`{0}': a class cannot be both static and sealed", GetSignatureForError ());
			}

			if (IsStatic) {
				foreach (var m in Members) {
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

					if ((m.ModFlags & Modifiers.STATIC) != 0 || m is TypeContainer)
						continue;

					if (m is Constructor) {
						Report.Error (710, m.Location, "`{0}': Static classes cannot have instance constructors", GetSignatureForError ());
						continue;
					}

					Report.Error (708, m.Location, "`{0}': cannot declare instance members in a static class", m.GetSignatureForError ());
				}
			} else {
				if (!PartialContainer.HasInstanceConstructor)
					DefineDefaultConstructor (false);
			}

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
	}

	public sealed class Struct : ClassOrStruct
	{
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

		public Struct (TypeContainer parent, MemberName name, Modifiers mod, Attributes attrs)
			: base (parent, name, attrs, MemberKind.Struct)
		{
			var accmods = IsTopLevel ? Modifiers.INTERNAL : Modifiers.PRIVATE;			
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
			if (a.Type == pa.StructLayout) {
				var value = a.GetNamedValue ("CharSet");
				if (value == null)
					return;

				for (int i = 0; i < Members.Count; ++i) {
					FixedField ff = Members [i] as FixedField;
					if (ff == null)
						continue;

					ff.CharSet = (CharSet) System.Enum.Parse (typeof (CharSet), value.GetValue ().ToString ());
				}
			}
		}

		bool CheckStructCycles ()
		{
			if (InTransit)
				return false;

			InTransit = true;
			foreach (var member in Members) {
				var field = member as Field;
				if (field == null)
					continue;

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

				//
				// Static fields of exactly same type are allowed
				//
				if (field.IsStatic && ftype == CurrentType)
					continue;

				if (!CheckFieldTypeCycle (ftype)) {
					Report.Error (523, field.Location,
						"Struct member `{0}' of type `{1}' causes a cycle in the struct layout",
						field.GetSignatureForError (), ftype.GetSignatureForError ());
					break;
				}
			}

			InTransit = false;
			return true;
		}

		static bool CheckFieldTypeCycle (TypeSpec ts)
		{
			var fts = ts.MemberDefinition as Struct;
			if (fts == null)
				return true;

			return fts.CheckStructCycles ();
		}

		public override void Emit ()
		{
			CheckStructCycles ();

			base.Emit ();
		}

		public override bool IsUnmanagedType ()
		{
			if (has_unmanaged_check_done)
				return is_unmanaged;

			if (requires_delayed_unmanagedtype_check)
				return true;

			var parent_def = Parent.PartialContainer;
			if (parent_def != null && parent_def.IsGenericOrParentIsGeneric) {
				has_unmanaged_check_done = true;
				return false;
			}

			if (first_nonstatic_field != null) {
				requires_delayed_unmanagedtype_check = true;

				foreach (var member in Members) {
					var f = member as Field;
					if (f == null)
						continue;

					if (f.IsStatic)
						continue;

					// It can happen when recursive unmanaged types are defined
					// struct S { S* s; }
					TypeSpec mt = f.MemberType;
					if (mt == null) {
						return true;
					}

					if (mt.IsUnmanaged)
						continue;

					has_unmanaged_check_done = true;
					return false;
				}

				has_unmanaged_check_done = true;
			}

			is_unmanaged = true;
			return true;
		}

		protected override TypeSpec[] ResolveBaseTypes (out FullNamedExpression base_class)
		{
			var ifaces = base.ResolveBaseTypes (out base_class);
			base_type = Compiler.BuiltinTypes.ValueType;
			return ifaces;
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
	public sealed class Interface : TypeDefinition {

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

		public Interface (TypeContainer parent, MemberName name, Modifiers mod, Attributes attrs)
			: base (parent, name, attrs, MemberKind.Interface)
		{
			var accmods = IsTopLevel ? Modifiers.INTERNAL : Modifiers.PRIVATE;

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
		public readonly bool IsExplicitImpl;

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

		public InterfaceMemberBase (TypeDefinition parent, FullNamedExpression type, Modifiers mod, Modifiers allowed_mod, MemberName name, Attributes attrs)
			: base (parent, type, mod, allowed_mod, Modifiers.PRIVATE, name, attrs)
		{
			IsInterface = parent.Kind == MemberKind.Interface;
			IsExplicitImpl = (MemberName.ExplicitInterface != null);
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
							GetSignatureForError (), base_member.GetSignatureForError ());
					}
				} else {
					if (OptAttributes != null && OptAttributes.Contains (Module.PredefinedAttributes.Obsolete)) {
						Report.SymbolRelatedToPreviousError (base_member);
						Report.Warning (809, 1, Location, "Obsolete member `{0}' overrides non-obsolete member `{1}'",
							GetSignatureForError (), base_member.GetSignatureForError ());
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
				InterfaceType = MemberName.ExplicitInterface.ResolveAsType (Parent);
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

		public override bool IsUsed 
		{
			get { return IsExplicitImpl || base.IsUsed; }
		}

	}

	public abstract class MemberBase : MemberCore
	{
		protected FullNamedExpression type_expr;
		protected TypeSpec member_type;
		public new TypeDefinition Parent;

		protected MemberBase (TypeDefinition parent, FullNamedExpression type, Modifiers mod, Modifiers allowed_mod, Modifiers def_mod, MemberName name, Attributes attrs)
			: base (parent, name, attrs)
		{
			this.Parent = parent;
			this.type_expr = type;
			ModFlags = ModifiersExtensions.Check (allowed_mod, mod, def_mod, Location, Report);
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

		protected void IsTypePermitted ()
		{
			if (MemberType.IsSpecialRuntimeType) {
				if (Parent is StateMachine) {
					Report.Error (4012, Location,
						"Parameters or local variables of type `{0}' cannot be declared in async methods or iterators",
						MemberType.GetSignatureForError ());
				} else if (Parent is HoistedStoreyClass) {
					Report.Error (4013, Location,
						"Local variables of type `{0}' cannot be used inside anonymous methods, lambda expressions or query expressions",
						MemberType.GetSignatureForError ());
				} else {
					Report.Error (610, Location, 
						"Field or property cannot be of type `{0}'", MemberType.GetSignatureForError ());
				}
			}
		}

		protected virtual bool CheckBase ()
		{
			CheckProtectedModifier ();

			return true;
		}

		public override string GetSignatureForDocumentation ()
		{
			return Parent.GetSignatureForDocumentation () + "." + MemberName.Basename;
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

