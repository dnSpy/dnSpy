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
	public class ModuleContainer : TypeContainer
	{
		//
		// Compiler generated container for static data
		//
		sealed class StaticDataContainer : CompilerGeneratedClass
		{
			Dictionary<int, Struct> size_types;
			new int fields;
#if !STATIC
			static MethodInfo set_data;
#endif

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

					var pa = Module.PredefinedAttributes.StructLayout;
					if (pa.Constructor != null || pa.ResolveConstructor (Location, TypeManager.short_type)) {
						var argsEncoded = new AttributeEncoder ();
						argsEncoded.Encode ((short) LayoutKind.Explicit);

						var field_size = pa.GetField ("Size", TypeManager.int32_type, Location);
						var pack = pa.GetField ("Pack", TypeManager.int32_type, Location);
						if (field_size != null) {
							argsEncoded.EncodeNamedArguments (
								new[] { field_size, pack },
								new[] { new IntConstant ((int) data.Length, Location), new IntConstant (1, Location) }
							);
						}

						pa.EmitAttribute (size_type.TypeBuilder, argsEncoded);
					}
				}

				var name = "$field-" + fields.ToString ("X");
				++fields;
				const Modifiers fmod = Modifiers.STATIC | Modifiers.INTERNAL;
				var fbuilder = TypeBuilder.DefineField (name, size_type.CurrentType.GetMetaInfo (), ModifiersExtensions.FieldAttr (fmod) | FieldAttributes.HasFieldRVA);
#if STATIC
				fbuilder.__SetDataAndRVA (data);
#else
				if (set_data == null)
					set_data = typeof (FieldBuilder).GetMethod ("SetRVAData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

				try {
					set_data.Invoke (fbuilder, new object[] { data });
				} catch {
					Report.RuntimeMissingSupport (loc, "SetRVAData");
				}
#endif

				return new FieldSpec (CurrentType, null, size_type.CurrentType, fbuilder, fmod);
			}
		}

		public CharSet? DefaultCharSet;
		public TypeAttributes DefaultCharSetType = TypeAttributes.AnsiClass;

		Dictionary<int, List<AnonymousTypeClass>> anonymous_types;
		StaticDataContainer static_data;

		AssemblyDefinition assembly;
		readonly CompilerContext context;
		readonly RootNamespace global_ns;
		Dictionary<string, RootNamespace> alias_ns;

		ModuleBuilder builder;

		bool has_extenstion_method;

		PredefinedAttributes predefined_attributes;
		PredefinedTypes predefined_types;

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
		}

		#region Properties

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

		internal PredefinedAttributes PredefinedAttributes {
			get {
				return predefined_attributes;
			}
		}

		internal PredefinedTypes PredefinedTypes {
			get {
				return predefined_types;
			}
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_targets;
			}
		}

		#endregion

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

		public void AddAttributes (List<Attribute> attrs)
		{
			AddAttributes (attrs, this);
		}

		public void AddAttributes (List<Attribute> attrs, IMemberContext context)
		{
			foreach (Attribute a in attrs)
				a.AttachTo (this, context);

			if (attributes == null) {
				attributes = new Attributes (attrs);
				return;
			}
			attributes.AddAttributes (attrs);
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
				NamespaceEntry.Error_GlobalNamespaceRedefined (Location.Null, Report);
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
			foreach (TypeContainer tc in types)
				tc.CreateType ();
		}

		public new void Define ()
		{
			// FIXME: Temporary hack for repl to reset
			static_data = null;

			InitializePredefinedTypes ();

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
		}

		public override void Emit ()
		{
			if (OptAttributes != null)
				OptAttributes.Emit ();

			if (RootContext.Unsafe) {
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
		}

		public override bool IsClsComplianceRequired ()
		{
			return DeclaringAssembly.IsCLSCompliant;
		}

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

		protected override bool AddMemberType (TypeContainer ds)
		{
			if (!AddToContainer (ds, ds.Name))
				return false;
			ds.NamespaceEntry.NS.AddType (ds.Definition);
			return true;
		}

		protected override void RemoveMemberType (DeclSpace ds)
		{
			ds.NamespaceEntry.NS.RemoveDeclSpace (ds.Basename);
			base.RemoveMemberType (ds);
		}
		
		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
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

	class RootDeclSpace : TypeContainer {
		public RootDeclSpace (NamespaceEntry ns)
			: base (ns, null, MemberName.Null, null, 0)
		{
			PartialContainer = RootContext.ToplevelTypes;
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

		public override bool IsClsComplianceRequired ()
		{
			return PartialContainer.IsClsComplianceRequired ();
		}

		public override FullNamedExpression LookupNamespaceAlias (string name)
		{
			return NamespaceEntry.LookupNamespaceAlias (name);
		}
	}
}
