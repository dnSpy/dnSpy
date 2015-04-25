//
// module.cs: keeps a tree representation of the generated code
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Marek Safar  (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2003-2008 Novell, Inc.
// Copyright 2011 Xamarin Inc
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.CompilerServices.SymbolWriter;
using System.Linq;

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
		sealed class StaticDataContainer : CompilerGeneratedContainer
		{
			readonly Dictionary<int, Struct> size_types;
			int fields;

			public StaticDataContainer (ModuleContainer module)
				: base (module, new MemberName ("<PrivateImplementationDetails>" + module.builder.ModuleVersionId.ToString ("B"), Location.Null),
					Modifiers.STATIC | Modifiers.INTERNAL)
			{
				size_types = new Dictionary<int, Struct> ();
			}

			public override void CloseContainer ()
			{
				base.CloseContainer ();

				foreach (var entry in size_types) {
					entry.Value.CloseContainer ();
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
					size_type = new Struct (this, new MemberName ("$ArrayType=" + data.Length, loc), Modifiers.PRIVATE | Modifiers.COMPILER_GENERATED, null);
					size_type.CreateContainer ();
					size_type.DefineContainer ();

					size_types.Add (data.Length, size_type);

					// It has to work even if StructLayoutAttribute does not exist
					size_type.TypeBuilder.__SetLayout (1, data.Length);
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
				static_data.CreateContainer ();
				static_data.DefineContainer ();

				AddCompilerGeneratedClass (static_data);
			}

			return static_data.DefineInitializedData (data, loc);
		}
#endif

		public sealed class PatternMatchingHelper : CompilerGeneratedContainer
		{
			public PatternMatchingHelper (ModuleContainer module)
				: base (module, new MemberName ("<PatternMatchingHelper>", Location.Null),
					Modifiers.STATIC | Modifiers.INTERNAL | Modifiers.DEBUGGER_HIDDEN)
			{
			}

			public Method NumberMatcher { get; private set; }

			protected override bool DoDefineMembers ()
			{
				if (!base.DoDefineMembers ())
					return false;

				NumberMatcher = GenerateNumberMatcher ();
				return true;
			}

			Method GenerateNumberMatcher ()
			{
				var loc = Location;
				var parameters = ParametersCompiled.CreateFullyResolved (
					new [] {
						new Parameter (new TypeExpression (Compiler.BuiltinTypes.Object, loc), "obj", 0, null, loc),
						new Parameter (new TypeExpression (Compiler.BuiltinTypes.Object, loc), "value", 0, null, loc),
						new Parameter (new TypeExpression (Compiler.BuiltinTypes.Bool, loc), "enumType", 0, null, loc),
					},
					new [] {
						Compiler.BuiltinTypes.Object,
						Compiler.BuiltinTypes.Object,
						Compiler.BuiltinTypes.Bool
					});

				var m = new Method (this, new TypeExpression (Compiler.BuiltinTypes.Bool, loc),
					Modifiers.PUBLIC | Modifiers.STATIC | Modifiers.DEBUGGER_HIDDEN, new MemberName ("NumberMatcher", loc),
					parameters, null);

				parameters [0].Resolve (m, 0);
				parameters [1].Resolve (m, 1);
				parameters [2].Resolve (m, 2);

				ToplevelBlock top_block = new ToplevelBlock (Compiler, parameters, loc);
				m.Block = top_block;

				//
				// if (enumType)
				//		return Equals (obj, value);
				//
				var equals_args = new Arguments (2);
				equals_args.Add (new Argument (top_block.GetParameterReference (0, loc)));
				equals_args.Add (new Argument (top_block.GetParameterReference (1, loc)));

				var if_type = new If (
					              top_block.GetParameterReference (2, loc),
					              new Return (new Invocation (new SimpleName ("Equals", loc), equals_args), loc),
					              loc);

				top_block.AddStatement (if_type);

				//
				// if (obj is Enum || obj == null)
				//		return false;
				//

				var if_enum = new If (
					              new Binary (Binary.Operator.LogicalOr,
						              new Is (top_block.GetParameterReference (0, loc), new TypeExpression (Compiler.BuiltinTypes.Enum, loc), loc),
						              new Binary (Binary.Operator.Equality, top_block.GetParameterReference (0, loc), new NullLiteral (loc))),
					              new Return (new BoolLiteral (Compiler.BuiltinTypes, false, loc), loc),
					              loc);

				top_block.AddStatement (if_enum);


				var system_convert = new MemberAccess (new QualifiedAliasMember ("global", "System", loc), "Convert", loc);

				//
				// var converted = System.Convert.ChangeType (obj, System.Convert.GetTypeCode (value));
				//
				var lv_converted = LocalVariable.CreateCompilerGenerated (Compiler.BuiltinTypes.Object, top_block, loc);

				var arguments_gettypecode = new Arguments (1);
				arguments_gettypecode.Add (new Argument (top_block.GetParameterReference (1, loc)));

				var gettypecode = new Invocation (new MemberAccess (system_convert, "GetTypeCode", loc), arguments_gettypecode);

				var arguments_changetype = new Arguments (1);
				arguments_changetype.Add (new Argument (top_block.GetParameterReference (0, loc)));
				arguments_changetype.Add (new Argument (gettypecode));

				var changetype = new Invocation (new MemberAccess (system_convert, "ChangeType", loc), arguments_changetype);

				top_block.AddStatement (new StatementExpression (new SimpleAssign (new LocalVariableReference (lv_converted, loc), changetype, loc)));


				//
				// return converted.Equals (value)
				//
				var equals_arguments = new Arguments (1);
				equals_arguments.Add (new Argument (top_block.GetParameterReference (1, loc)));
				var equals_invocation = new Invocation (new MemberAccess (new LocalVariableReference (lv_converted, loc), "Equals"), equals_arguments);
				top_block.AddStatement (new Return (equals_invocation, loc));

				m.Define ();
				m.PrepareEmit ();
				AddMember (m);

				return m;
			}
		}

		PatternMatchingHelper pmh;

		public PatternMatchingHelper CreatePatterMatchingHelper ()
		{
			if (pmh == null) {
				pmh = new PatternMatchingHelper (this);

				pmh.CreateContainer ();
				pmh.DefineContainer ();
				pmh.Define ();
				AddCompilerGeneratedClass (pmh);
			}

			return pmh;
		}

		public CharSet? DefaultCharSet;
		public TypeAttributes DefaultCharSetType = TypeAttributes.AnsiClass;

		readonly Dictionary<int, List<AnonymousTypeClass>> anonymous_types;
		readonly Dictionary<ArrayContainer.TypeRankPair, ArrayContainer> array_types;
		readonly Dictionary<TypeSpec, PointerContainer> pointer_types;
		readonly Dictionary<TypeSpec, ReferenceContainer> reference_types;
		readonly Dictionary<TypeSpec, MethodSpec> attrs_cache;
		readonly Dictionary<TypeSpec, AwaiterDefinition> awaiters;

		AssemblyDefinition assembly;
		readonly CompilerContext context;
		readonly RootNamespace global_ns;
		readonly Dictionary<string, RootNamespace> alias_ns;

		ModuleBuilder builder;

		bool has_extenstion_method;

		PredefinedAttributes predefined_attributes;
		PredefinedTypes predefined_types;
		PredefinedMembers predefined_members;

		public Binary.PredefinedOperator[] OperatorsBinaryEqualityLifted;
		public Binary.PredefinedOperator[] OperatorsBinaryLifted;

		static readonly string[] attribute_targets = new string[] { "assembly", "module" };

		public ModuleContainer (CompilerContext context)
			: base (null, MemberName.Null, null, 0)
		{
			this.context = context;

			caching_flags &= ~(Flags.Obsolete_Undetected | Flags.Excluded_Undetected);

			containers = new List<TypeContainer> ();
			anonymous_types = new Dictionary<int, List<AnonymousTypeClass>> ();
			global_ns = new GlobalRootNamespace ();
			alias_ns = new Dictionary<string, RootNamespace> ();
			array_types = new Dictionary<ArrayContainer.TypeRankPair, ArrayContainer> ();
			pointer_types = new Dictionary<TypeSpec, PointerContainer> ();
			reference_types = new Dictionary<TypeSpec, ReferenceContainer> ();
			attrs_cache = new Dictionary<TypeSpec, MethodSpec> ();
			awaiters = new Dictionary<TypeSpec, AwaiterDefinition> ();
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

		public int CounterAnonymousTypes { get; set; }

		public AssemblyDefinition DeclaringAssembly {
			get {
				return assembly;
			}
		}

		internal DocumentationBuilder DocumentationBuilder {
			get; set;
		}

		public override string DocCommentHeader {
			get {
				throw new NotSupportedException ();
			}
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

		public override void AddTypeContainer (TypeContainer tc)
		{
			AddTypeContainerMember (tc);
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

		public override void CloseContainer ()
		{
			if (anonymous_types != null) {
				foreach (var atypes in anonymous_types)
					foreach (var at in atypes.Value)
						at.CloseContainer ();
			}

			base.CloseContainer ();
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
				RootNamespace.Error_GlobalNamespaceRedefined (Report, Location.Null);
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

		public override bool Define ()
		{
			DefineContainer ();

			ExpandBaseInterfaces ();

			base.Define ();

			HasTypesFullyDefined = true;

			return true;
		}

		public override bool DefineContainer ()
		{
			DefineNamespace ();

			return base.DefineContainer ();
		}

		public void EnableRedefinition ()
		{
			is_defined = false;
		}

		public override void EmitContainer ()
		{
			if (OptAttributes != null)
				OptAttributes.Emit ();

			if (Compiler.Settings.Unsafe && !assembly.IsSatelliteAssembly) {
				var pa = PredefinedAttributes.UnverifiableCode;
				if (pa.IsDefined)
					pa.EmitAttribute (builder);
			}

			foreach (var tc in containers) {
				tc.PrepareEmit ();
			}

			base.EmitContainer ();

			if (Compiler.Report.Errors == 0 && !Compiler.Settings.WriteMetadataOnly)
				VerifyMembers ();

			if (anonymous_types != null) {
				foreach (var atypes in anonymous_types)
					foreach (var at in atypes.Value)
						at.EmitContainer ();
			}
		}

		internal override void GenerateDocComment (DocumentationBuilder builder)
		{
			foreach (var tc in containers)
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

		//
		// Return container with awaiter definition. It never returns null
		// but all container member can be null for easier error reporting
		//
		public AwaiterDefinition GetAwaiter (TypeSpec type)
		{
			AwaiterDefinition awaiter;
			if (awaiters.TryGetValue (type, out awaiter))
				return awaiter;

			awaiter = new AwaiterDefinition ();

			//
			// Predefined: bool IsCompleted { get; } 
			//
			awaiter.IsCompleted = MemberCache.FindMember (type, MemberFilter.Property ("IsCompleted", Compiler.BuiltinTypes.Bool),
				BindingRestriction.InstanceOnly) as PropertySpec;

			//
			// Predefined: GetResult ()
			//
			// The method return type is also result type of await expression
			//
			awaiter.GetResult = MemberCache.FindMember (type, MemberFilter.Method ("GetResult", 0,
				ParametersCompiled.EmptyReadOnlyParameters, null),
				BindingRestriction.InstanceOnly) as MethodSpec;

			//
			// Predefined: INotifyCompletion.OnCompleted (System.Action)
			//
			var nc = PredefinedTypes.INotifyCompletion;
			awaiter.INotifyCompletion = !nc.Define () || type.ImplementsInterface (nc.TypeSpec, false);

			awaiters.Add (type, awaiter);
			return awaiter;
		}

		public override void GetCompletionStartingWith (string prefix, List<string> results)
		{
			var names = Evaluator.GetVarNames ();
			results.AddRange (names.Where (l => l.StartsWith (prefix)));
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

		public Binary.PredefinedOperator[] GetPredefinedEnumAritmeticOperators (TypeSpec enumType, bool nullable)
		{
			TypeSpec underlying;
			Binary.Operator mask = 0;

			if (nullable) {
				underlying = Nullable.NullableInfo.GetEnumUnderlyingType (this, enumType);
				mask = Binary.Operator.NullableMask;
			} else {
				underlying = EnumSpec.GetUnderlyingType (enumType);
			}

			var operators = new[] {
				new Binary.PredefinedOperator (enumType, underlying,
					mask | Binary.Operator.AdditionMask | Binary.Operator.SubtractionMask | Binary.Operator.DecomposedMask, enumType),
				new Binary.PredefinedOperator (underlying, enumType,
					mask | Binary.Operator.AdditionMask | Binary.Operator.SubtractionMask | Binary.Operator.DecomposedMask, enumType),
				new Binary.PredefinedOperator (enumType, mask | Binary.Operator.SubtractionMask, underlying)
			};

			return operators;
		}

		public void InitializePredefinedTypes ()
		{
			predefined_attributes = new PredefinedAttributes (this);
			predefined_types = new PredefinedTypes (this);
			predefined_members = new PredefinedMembers (this);

			OperatorsBinaryEqualityLifted = Binary.CreateEqualityLiftedOperatorsTable (this);
			OperatorsBinaryLifted = Binary.CreateStandardLiftedOperatorsTable (this);
		}

		public override bool IsClsComplianceRequired ()
		{
			return DeclaringAssembly.IsCLSCompliant;
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
}
