//
// roottypes.cs: keeps a tree representation of the generated code
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Marek Safar  (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2003-2008 Novell, Inc.
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.CompilerServices.SymbolWriter;

#if STATIC
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	//
	// Module (top-level type) container
	//
	public sealed class ModuleContainer : TypeContainer
	{
#if STATIC
		//
		// Compiler generated container for static data
		//
		sealed class StaticDataContainer : CompilerGeneratedClass
		{
			readonly Dictionary<int, Struct> size_types;
			new int fields;

			public StaticDataContainer (ModuleContainer module)
				: base (module, new MemberName ("<PrivateImplementationDetails>" + module.builder.ModuleVersionId.ToString ("B"), Location.Null), Modifiers.STATIC)
			{
				size_types = new Dictionary<int, Struct> ();
			}

			public override void CloseType ()
			{
				base.CloseType ();

				foreach (var entry in size_types) {
					entry.Value.CloseType ();
				}
			}

			public FieldSpec DefineInitializedData (byte[] data, Location loc)
			{
				Struct size_type;
				if (!size_types.TryGetValue (data.Length, out size_type)) {
					//
					// Build common type for this data length. We cannot use
					// DefineInitializedData because it creates public type,
					// and its name is not unique among modules
					//
					size_type = new Struct (null, this, new MemberName ("$ArrayType=" + data.Length, Location), Modifiers.PRIVATE | Modifiers.COMPILER_GENERATED, null);
					size_type.CreateType ();
					size_type.DefineType ();

					size_types.Add (data.Length, size_type);
					var ctor = Module.PredefinedMembers.StructLayoutAttributeCtor.Resolve (Location);
					if (ctor != null) {
						var argsEncoded = new AttributeEncoder ();
						argsEncoded.Encode ((short) LayoutKind.Explicit);

						var field_size = Module.PredefinedMembers.StructLayoutSize.Resolve (Location);
						var pack = Module.PredefinedMembers.StructLayoutPack.Resolve (Location);
						if (field_size != null && pack != null) {
							argsEncoded.EncodeNamedArguments (
								new[] { field_size, pack },
								new[] { new IntConstant (Compiler.BuiltinTypes, (int) data.Length, Location), new IntConstant (Compiler.BuiltinTypes, 1, Location) }
							);

							size_type.TypeBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), argsEncoded.ToArray ());
						}
					}
				}

				var name = "$field-" + fields.ToString ("X");
				++fields;
				const Modifiers fmod = Modifiers.STATIC | Modifiers.INTERNAL;
				var fbuilder = TypeBuilder.DefineField (name, size_type.CurrentType.GetMetaInfo (), ModifiersExtensions.FieldAttr (fmod) | FieldAttributes.HasFieldRVA);
				fbuilder.__SetDataAndRVA (data);

				return new FieldSpec (CurrentType, null, size_type.CurrentType, fbuilder, fmod);
			}
		}

		StaticDataContainer static_data;

		//
		// Makes const data field inside internal type container
		//
		public FieldSpec MakeStaticData (byte[] data, Location loc)
		{
			if (static_data == null) {
				static_data = new StaticDataContainer (this);
				static_data.CreateType ();
				static_data.DefineType ();

				AddCompilerGeneratedClass (static_data);
			}

			return static_data.DefineInitializedData (data, loc);
		}
#endif

		public CharSet? DefaultCharSet;
		public TypeAttributes DefaultCharSetType = TypeAttributes.AnsiClass;

		readonly Dictionary<int, List<AnonymousTypeClass>> anonymous_types;
		readonly Dictionary<ArrayContainer.TypeRankPair, ArrayContainer> array_types;
		readonly Dictionary<TypeSpec, PointerContainer> pointer_types;
		readonly Dictionary<TypeSpec, ReferenceContainer> reference_types;
		readonly Dictionary<TypeSpec, MethodSpec> attrs_cache;

		// Used for unique namespaces/types during parsing
		Dictionary<MemberName, ITypesContainer> defined_type_containers;

		AssemblyDefinition assembly;
		readonly CompilerContext context;
		readonly RootNamespace global_ns;
		readonly Dictionary<string, RootNamespace> alias_ns;

		ModuleBuilder builder;

		bool has_extenstion_method;

		PredefinedAttributes predefined_attributes;
		PredefinedTypes predefined_types;
		PredefinedMembers predefined_members;

		static readonly string[] attribute_targets = new string[] { "assembly", "module" };

		public ModuleContainer (CompilerContext context)
			: base (null, null, MemberName.Null, null, 0)
		{
			this.context = context;

			caching_flags &= ~(Flags.Obsolete_Undetected | Flags.Excluded_Undetected);

			types = new List<TypeContainer> ();
			anonymous_types = new Dictionary<int, List<AnonymousTypeClass>> ();
			global_ns = new GlobalRootNamespace ();
			alias_ns = new Dictionary<string, RootNamespace> ();
			array_types = new Dictionary<ArrayContainer.TypeRankPair, ArrayContainer> ();
			pointer_types = new Dictionary<TypeSpec, PointerContainer> ();
			reference_types = new Dictionary<TypeSpec, ReferenceContainer> ();
			attrs_cache = new Dictionary<TypeSpec, MethodSpec> ();

			defined_type_containers = new Dictionary<MemberName, ITypesContainer> ();
		}

		#region Properties

		internal Dictionary<ArrayContainer.TypeRankPair, ArrayContainer> ArrayTypesCache {
			get {
				return array_types;
			}
		}

		//
		// Cache for parameter-less attributes
		//
		internal Dictionary<TypeSpec, MethodSpec> AttributeConstructorCache {
			get {
				return attrs_cache;
			}
		}

 		public override AttributeTargets AttributeTargets {
 			get {
 				return AttributeTargets.Assembly;
 			}
		}

		public ModuleBuilder Builder {
			get {
				return builder;
			}
		}

		public override CompilerContext Compiler {
			get {
				return context;
			}
		}

		public override AssemblyDefinition DeclaringAssembly {
			get {
				return assembly;
			}
		}

		internal DocumentationBuilder DocumentationBuilder {
			get; set;
		}

		public Evaluator Evaluator {
			get; set;
		}

		public bool HasDefaultCharSet {
			get {
				return DefaultCharSet.HasValue;
			}
		}

		public bool HasExtensionMethod {
			get {
				return has_extenstion_method;
			}
			set {
				has_extenstion_method = value;
			}
		}

		public bool HasTypesFullyDefined {
			get; set;
		}

		//
		// Returns module global:: namespace
		//
		public RootNamespace GlobalRootNamespace {
		    get {
		        return global_ns;
		    }
		}

		public override ModuleContainer Module {
			get {
				return this;
			}
		}

		internal Dictionary<TypeSpec, PointerContainer> PointerTypesCache {
			get {
				return pointer_types;
			}
		}

		internal PredefinedAttributes PredefinedAttributes {
			get {
				return predefined_attributes;
			}
		}

		internal PredefinedMembers PredefinedMembers {
			get {
				return predefined_members;
			}
		}

		internal PredefinedTypes PredefinedTypes {
			get {
				return predefined_types;
			}
		}

		internal Dictionary<TypeSpec, ReferenceContainer> ReferenceTypesCache {
			get {
				return reference_types;
			}
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_targets;
			}
		}

		#endregion

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public void AddAnonymousType (AnonymousTypeClass type)
		{
			List<AnonymousTypeClass> existing;
			if (!anonymous_types.TryGetValue (type.Parameters.Count, out existing))
			if (existing == null) {
				existing = new List<AnonymousTypeClass> ();
				anonymous_types.Add (type.Parameters.Count, existing);
			}

			existing.Add (type);
		}

		public void AddAttribute (Attribute attr, IMemberContext context)
		{
			attr.AttachTo (this, context);

			if (attributes == null) {
				attributes = new Attributes (attr);
				return;
			}

			attributes.AddAttribute (attr);
		}

		public override TypeContainer AddPartial (TypeContainer nextPart)
		{
			return AddPartial (nextPart, nextPart.Name);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Target == AttributeTargets.Assembly) {
				assembly.ApplyAttributeBuilder (a, ctor, cdata, pa);
				return;
			}

			if (a.Type == pa.DefaultCharset) {
				switch (a.GetCharSetValue ()) {
				case CharSet.Ansi:
				case CharSet.None:
					break;
				case CharSet.Auto:
					DefaultCharSet = CharSet.Auto;
					DefaultCharSetType = TypeAttributes.AutoClass;
					break;
				case CharSet.Unicode:
					DefaultCharSet = CharSet.Unicode;
					DefaultCharSetType = TypeAttributes.UnicodeClass;
					break;
				default:
					Report.Error (1724, a.Location, "Value specified for the argument to `{0}' is not valid",
						a.GetSignatureForError ());
					break;
				}
			} else if (a.Type == pa.CLSCompliant) {
				Attribute cls = DeclaringAssembly.CLSCompliantAttribute;
				if (cls == null) {
					Report.Warning (3012, 1, a.Location,
						"You must specify the CLSCompliant attribute on the assembly, not the module, to enable CLS compliance checking");
				} else if (DeclaringAssembly.IsCLSCompliant != a.GetBoolean ()) {
					Report.SymbolRelatedToPreviousError (cls.Location, cls.GetSignatureForError ());
					Report.Warning (3017, 1, a.Location,
						"You cannot specify the CLSCompliant attribute on a module that differs from the CLSCompliant attribute on the assembly");
					return;
				}
			}

			builder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
		}

		public override void CloseType ()
		{
			foreach (TypeContainer tc in types) {
				tc.CloseType ();
			}

			if (compiler_generated != null)
				foreach (CompilerGeneratedClass c in compiler_generated)
					c.CloseType ();
		}

		public TypeBuilder CreateBuilder (string name, TypeAttributes attr, int typeSize)
		{
			return builder.DefineType (name, attr, null, typeSize);
		}

		//
		// Creates alias global namespace
		//
		public RootNamespace CreateRootNamespace (string alias)
		{
			if (alias == global_ns.Alias) {
				NamespaceContainer.Error_GlobalNamespaceRedefined (Location.Null, Report);
				return global_ns;
			}

			RootNamespace rn;
			if (!alias_ns.TryGetValue (alias, out rn)) {
				rn = new RootNamespace (alias);
				alias_ns.Add (alias, rn);
			}

			return rn;
		}

		public void Create (AssemblyDefinition assembly, ModuleBuilder moduleBuilder)
		{
			this.assembly = assembly;
			builder = moduleBuilder;
		}

		public new void CreateType ()
		{
			// Release cache used by parser only
			if (Evaluator == null)
				defined_type_containers = null;
			else
				defined_type_containers.Clear ();

			foreach (TypeContainer tc in types)
				tc.CreateType ();
		}

		public new void Define ()
		{
			foreach (TypeContainer tc in types)
				tc.DefineType ();

			foreach (TypeContainer tc in types)
				tc.ResolveTypeParameters ();

			foreach (TypeContainer tc in types) {
				try {
					tc.Define ();
				} catch (Exception e) {
					throw new InternalErrorException (tc, e);
				}
			}

			HasTypesFullyDefined = true;
		}

		public override void Emit ()
		{
			if (OptAttributes != null)
				OptAttributes.Emit ();

			if (Compiler.Settings.Unsafe) {
				var pa = PredefinedAttributes.UnverifiableCode;
				if (pa.IsDefined)
					pa.EmitAttribute (builder);
			}

			foreach (var tc in types)
				tc.DefineConstants ();

			foreach (TypeContainer tc in types)
				tc.EmitType ();

			if (Compiler.Report.Errors > 0)
				return;

			foreach (TypeContainer tc in types)
				tc.VerifyMembers ();

			if (compiler_generated != null)
				foreach (var c in compiler_generated)
					c.EmitType ();
		}

		internal override void GenerateDocComment (DocumentationBuilder builder)
		{
			foreach (var tc in types)
				tc.GenerateDocComment (builder);
		}

		public AnonymousTypeClass GetAnonymousType (IList<AnonymousTypeParameter> parameters)
		{
			List<AnonymousTypeClass> candidates;
			if (!anonymous_types.TryGetValue (parameters.Count, out candidates))
				return null;

			int i;
			foreach (AnonymousTypeClass at in candidates) {
				for (i = 0; i < parameters.Count; ++i) {
					if (!parameters [i].Equals (at.Parameters [i]))
						break;
				}

				if (i == parameters.Count)
					return at;
			}

			return null;
		}

		public RootNamespace GetRootNamespace (string name)
		{
			RootNamespace rn;
			alias_ns.TryGetValue (name, out rn);
			return rn;
		}

		public override string GetSignatureForError ()
		{
			return "<module>";
		}

		public void InitializePredefinedTypes ()
		{
			predefined_attributes = new PredefinedAttributes (this);
			predefined_types = new PredefinedTypes (this);
			predefined_members = new PredefinedMembers (this);
		}

		public override bool IsClsComplianceRequired ()
		{
			return DeclaringAssembly.IsCLSCompliant;
		}

		protected override bool AddMemberType (TypeContainer tc)
		{
			if (AddTypesContainer (tc)) {
				if ((tc.ModFlags & Modifiers.PARTIAL) != 0)
					defined_names.Add (tc.Name, tc);

				tc.NamespaceEntry.NS.AddType (this, tc.Definition);
				return true;
			}

			return false;
		}

		public bool AddTypesContainer (ITypesContainer container)
		{
			var mn = container.MemberName;
			ITypesContainer found;
			if (!defined_type_containers.TryGetValue (mn, out found)) {
				defined_type_containers.Add (mn, container);
				return true;
			}

			if (container is NamespaceContainer && found is NamespaceContainer)
				return true;

			var container_tc = container as TypeContainer;
			var found_tc = found as TypeContainer;
			if (container_tc != null && found_tc != null && container_tc.Kind == found_tc.Kind) {
				if ((found_tc.ModFlags & container_tc.ModFlags & Modifiers.PARTIAL) != 0) {
					return false;
				}

				if (((found_tc.ModFlags | container_tc.ModFlags) & Modifiers.PARTIAL) != 0) {
					Report.SymbolRelatedToPreviousError (found_tc);
					Error_MissingPartialModifier (container_tc);
					return false;
				}
			}

			string ns = mn.Left != null ? mn.Left.GetSignatureForError () : Module.GlobalRootNamespace.GetSignatureForError ();
			mn = new MemberName (mn.Name, mn.TypeArguments, mn.Location);

			Report.SymbolRelatedToPreviousError (found.Location, "");
			Report.Error (101, container.Location,
				"The namespace `{0}' already contains a definition for `{1}'",
				ns, mn.GetSignatureForError ());
			return false;
		}

		protected override void RemoveMemberType (TypeContainer ds)
		{
			defined_type_containers.Remove (ds.MemberName);
			ds.NamespaceEntry.NS.RemoveDeclSpace (ds.Basename);
			base.RemoveMemberType (ds);
		}

		public Attribute ResolveAssemblyAttribute (PredefinedAttribute a_type)
		{
			Attribute a = OptAttributes.Search ("assembly", a_type);
			if (a != null) {
				a.Resolve ();
			}
			return a;
		}

		public void SetDeclaringAssembly (AssemblyDefinition assembly)
		{
			// TODO: This setter is quite ugly but I have not found a way around it yet
			this.assembly = assembly;
		}
	}

	sealed class RootDeclSpace : TypeContainer {
		public RootDeclSpace (ModuleContainer module, NamespaceContainer ns)
			: base (ns, null, MemberName.Null, null, 0)
		{
			PartialContainer = module;
		}

		public override AttributeTargets AttributeTargets {
			get { throw new InternalErrorException ("should not be called"); }
		}

		public override CompilerContext Compiler {
			get {
				return PartialContainer.Compiler;
			}
		}

		public override string DocCommentHeader {
			get { throw new InternalErrorException ("should not be called"); }
		}

		public override void DefineType ()
		{
			throw new InternalErrorException ("should not be called");
		}

		public override ModuleContainer Module {
			get {
				return PartialContainer.Module;
			}
		}

		public override void Accept (StructuralVisitor visitor)
		{
			throw new InternalErrorException ("should not be called");
		}

		public override bool IsClsComplianceRequired ()
		{
			return PartialContainer.IsClsComplianceRequired ();
		}

		public override IList<MethodSpec> LookupExtensionMethod (TypeSpec extensionType, string name, int arity, ref NamespaceContainer scope)
		{
			return null;
		}

		public override FullNamedExpression LookupNamespaceAlias (string name)
		{
			return NamespaceEntry.LookupNamespaceAlias (name);
		}
	}
}
