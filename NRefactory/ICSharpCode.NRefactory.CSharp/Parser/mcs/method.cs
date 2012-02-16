//
// method.cs: Method based declarations
//
// Authors: Miguel de Icaza (miguel@gnu.org)
//          Martin Baulig (martin@ximian.com)
//          Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002, 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
// Copyright 2011 Xamarin Inc.
//

using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Linq;

#if NET_2_1
using XmlElement = System.Object;
#else
using System.Xml;
#endif

#if STATIC
using MetaType = IKVM.Reflection.Type;
using SecurityType = System.Collections.Generic.List<IKVM.Reflection.Emit.CustomAttributeBuilder>;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using MetaType = System.Type;
using SecurityType = System.Collections.Generic.Dictionary<System.Security.Permissions.SecurityAction, System.Security.PermissionSet>;
using System.Reflection;
using System.Reflection.Emit;
#endif

using Mono.CompilerServices.SymbolWriter;

namespace Mono.CSharp {

	public abstract class MethodCore : InterfaceMemberBase, IParametersMember
	{
		protected ParametersCompiled parameters;
		protected ToplevelBlock block;
		protected MethodSpec spec;

		public MethodCore (TypeDefinition parent, FullNamedExpression type, Modifiers mod, Modifiers allowed_mod,
			MemberName name, Attributes attrs, ParametersCompiled parameters)
			: base (parent, type, mod, allowed_mod, name, attrs)
		{
			this.parameters = parameters;
		}

		public override Variance ExpectedMemberTypeVariance {
			get {
				return Variance.Covariant;
			}
		}

		//
		//  Returns the System.Type array for the parameters of this method
		//
		public TypeSpec [] ParameterTypes {
			get {
				return parameters.Types;
			}
		}

		public ParametersCompiled ParameterInfo {
			get {
				return parameters;
			}
		}

		AParametersCollection IParametersMember.Parameters {
			get { return parameters; }
		}
		
		public ToplevelBlock Block {
			get {
				return block;
			}

			set {
				block = value;
			}
		}

		public CallingConventions CallingConventions {
			get {
				CallingConventions cc = parameters.CallingConvention;
				if (!IsInterface)
					if ((ModFlags & Modifiers.STATIC) == 0)
						cc |= CallingConventions.HasThis;

				// FIXME: How is `ExplicitThis' used in C#?
			
				return cc;
			}
		}

		protected override bool CheckOverrideAgainstBase (MemberSpec base_member)
		{
			bool res = base.CheckOverrideAgainstBase (base_member);

			//
			// Check that the permissions are not being changed
			//
			if (!CheckAccessModifiers (this, base_member)) {
				Error_CannotChangeAccessModifiers (this, base_member);
				res = false;
			}

			return res;
		}

		protected override bool CheckBase ()
		{
			// Check whether arguments were correct.
			if (!DefineParameters (parameters))
				return false;

			return base.CheckBase ();
		}

		//
		//   Represents header string for documentation comment.
		//
		public override string DocCommentHeader 
		{
			get { return "M:"; }
		}

		public override void Emit ()
		{
			if ((ModFlags & Modifiers.COMPILER_GENERATED) == 0) {
				parameters.CheckConstraints (this);
			}

			base.Emit ();
		}

		public override bool EnableOverloadChecks (MemberCore overload)
		{
			if (overload is MethodCore) {
				caching_flags |= Flags.MethodOverloadsExist;
				return true;
			}

			if (overload is AbstractPropertyEventMethod)
				return true;

			return base.EnableOverloadChecks (overload);
		}

		public override string GetSignatureForDocumentation ()
		{
			string s = base.GetSignatureForDocumentation ();
			if (MemberName.Arity > 0)
				s += "``" + MemberName.Arity.ToString ();

			return s + parameters.GetSignatureForDocumentation ();
		}

		public MethodSpec Spec {
			get { return spec; }
		}

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance ())
				return false;

			if (parameters.HasArglist) {
				Report.Warning (3000, 1, Location, "Methods with variable arguments are not CLS-compliant");
			}

			if (member_type != null && !member_type.IsCLSCompliant ()) {
				Report.Warning (3002, 1, Location, "Return type of `{0}' is not CLS-compliant",
					GetSignatureForError ());
			}

			parameters.VerifyClsCompliance (this);
			return true;
		}
	}

	public interface IGenericMethodDefinition : IMemberDefinition
	{
		TypeParameterSpec[] TypeParameters { get; }
		int TypeParametersCount { get; }

//		MethodInfo MakeGenericMethod (TypeSpec[] targs);
	}

	public sealed class MethodSpec : MemberSpec, IParametersMember
	{
		MethodBase metaInfo, inflatedMetaInfo;
		AParametersCollection parameters;
		TypeSpec returnType;

		TypeSpec[] targs;
		TypeParameterSpec[] constraints;

		public MethodSpec (MemberKind kind, TypeSpec declaringType, IMemberDefinition details, TypeSpec returnType,
			MethodBase info, AParametersCollection parameters, Modifiers modifiers)
			: base (kind, declaringType, details, modifiers)
		{
			this.metaInfo = info;
			this.parameters = parameters;
			this.returnType = returnType;
		}

		#region Properties

		public override int Arity {
			get {
				return IsGeneric ? GenericDefinition.TypeParametersCount : 0;
			}
		}

		public TypeParameterSpec[] Constraints {
			get {
				if (constraints == null && IsGeneric)
					constraints = GenericDefinition.TypeParameters;

				return constraints;
			}
		}

		public bool IsConstructor {
			get {
				return Kind == MemberKind.Constructor;
			}
		}

		public IGenericMethodDefinition GenericDefinition {
			get {
				return (IGenericMethodDefinition) definition;
			}
		}

		public bool IsExtensionMethod {
			get {
				return IsStatic && parameters.HasExtensionMethodType;
			}
		}

		public bool IsSealed {
			get {
				return (Modifiers & Modifiers.SEALED) != 0;
			}
		}

		// When is virtual or abstract
		public bool IsVirtual {
			get {
				return (Modifiers & (Modifiers.VIRTUAL | Modifiers.ABSTRACT | Modifiers.OVERRIDE)) != 0;
			}
		}

		public bool IsReservedMethod {
			get {
				return Kind == MemberKind.Operator || IsAccessor;
			}
		}

		TypeSpec IInterfaceMemberSpec.MemberType {
			get {
				return returnType;
			}
		}

		public AParametersCollection Parameters {
			get { 
				return parameters;
			}
		}

		public TypeSpec ReturnType {
			get {
				return returnType;
			}
		}

		public TypeSpec[] TypeArguments {
			get {
				return targs;
			}
		}

		#endregion

		public MethodSpec GetGenericMethodDefinition ()
		{
			if (!IsGeneric && !DeclaringType.IsGeneric)
				return this;

			return MemberCache.GetMember (declaringType, this);
		}

		public MethodBase GetMetaInfo ()
		{
			//
			// inflatedMetaInfo is extra field needed for cases where we
			// inflate method but another nested type can later inflate
			// again (the cache would be build with inflated metaInfo) and
			// TypeBuilder can work with method definitions only
			//
			if (inflatedMetaInfo == null) {
				if ((state & StateFlags.PendingMetaInflate) != 0) {
					var dt_meta = DeclaringType.GetMetaInfo ();

					if (DeclaringType.IsTypeBuilder) {
						if (IsConstructor)
							inflatedMetaInfo = TypeBuilder.GetConstructor (dt_meta, (ConstructorInfo) metaInfo);
						else
							inflatedMetaInfo = TypeBuilder.GetMethod (dt_meta, (MethodInfo) metaInfo);
					} else {
#if STATIC
						// it should not be reached
						throw new NotImplementedException ();
#else
						inflatedMetaInfo = MethodInfo.GetMethodFromHandle (metaInfo.MethodHandle, dt_meta.TypeHandle);
#endif
					}

					state &= ~StateFlags.PendingMetaInflate;
				} else {
					inflatedMetaInfo = metaInfo;
				}
			}

			if ((state & StateFlags.PendingMakeMethod) != 0) {
				var sre_targs = new MetaType[targs.Length];
				for (int i = 0; i < sre_targs.Length; ++i)
					sre_targs[i] = targs[i].GetMetaInfo ();

				inflatedMetaInfo = ((MethodInfo) inflatedMetaInfo).MakeGenericMethod (sre_targs);
				state &= ~StateFlags.PendingMakeMethod;
			}

			return inflatedMetaInfo;
		}

		public override string GetSignatureForDocumentation ()
		{
			string name;
			switch (Kind) {
			case MemberKind.Constructor:
				name = "#ctor";
				break;
			case MemberKind.Method:
				if (Arity > 0)
					name = Name + "``" + Arity.ToString ();
				else
					name = Name;

				break;
			default:
				name = Name;
				break;
			}

			name = DeclaringType.GetSignatureForDocumentation () + "." + name + parameters.GetSignatureForDocumentation ();
			if (Kind == MemberKind.Operator) {
				var op = Operator.GetType (Name).Value;
				if (op == Operator.OpType.Explicit || op == Operator.OpType.Implicit) {
					name += "~" + ReturnType.GetSignatureForDocumentation ();
				}
			}

			return name;
		}

		public override string GetSignatureForError ()
		{
			string name;
			if (IsConstructor) {
				name = DeclaringType.GetSignatureForError () + "." + DeclaringType.Name;
			} else if (Kind == MemberKind.Operator) {
				var op = Operator.GetType (Name).Value;
				if (op == Operator.OpType.Implicit || op == Operator.OpType.Explicit) {
					name = DeclaringType.GetSignatureForError () + "." + Operator.GetName (op) + " operator " + returnType.GetSignatureForError ();
				} else {
					name = DeclaringType.GetSignatureForError () + ".operator " + Operator.GetName (op);
				}
			} else if (IsAccessor) {
				int split = Name.IndexOf ('_');
				name = Name.Substring (split + 1);
				var postfix = Name.Substring (0, split);
				if (split == 3) {
					var pc = parameters.Count;
					if (pc > 0 && postfix == "get") {
						name = "this" + parameters.GetSignatureForError ("[", "]", pc);
					} else if (pc > 1 && postfix == "set") {
						name = "this" + parameters.GetSignatureForError ("[", "]", pc - 1);
					}
				}

				return DeclaringType.GetSignatureForError () + "." + name + "." + postfix;
			} else {
				name = base.GetSignatureForError ();
				if (targs != null)
					name += "<" + TypeManager.CSharpName (targs) + ">";
				else if (IsGeneric)
					name += "<" + TypeManager.CSharpName (GenericDefinition.TypeParameters) + ">";
			}

			return name + parameters.GetSignatureForError ();
		}

		public override MemberSpec InflateMember (TypeParameterInflator inflator)
		{
			var ms = (MethodSpec) base.InflateMember (inflator);
			ms.inflatedMetaInfo = null;
			ms.returnType = inflator.Inflate (returnType);
			ms.parameters = parameters.Inflate (inflator);
			if (IsGeneric)
				ms.constraints = TypeParameterSpec.InflateConstraints (inflator, Constraints);

			return ms;
		}

		public MethodSpec MakeGenericMethod (IMemberContext context, params TypeSpec[] targs)
		{
			if (targs == null)
				throw new ArgumentNullException ();
// TODO MemberCache
//			if (generic_intances != null && generic_intances.TryGetValue (targs, out ginstance))
//				return ginstance;

			//if (generic_intances == null)
			//    generic_intances = new Dictionary<TypeSpec[], Method> (TypeSpecArrayComparer.Default);

			var inflator = new TypeParameterInflator (context, DeclaringType, GenericDefinition.TypeParameters, targs);

			var inflated = (MethodSpec) MemberwiseClone ();
			inflated.declaringType = inflator.TypeInstance;
			inflated.returnType = inflator.Inflate (returnType);
			inflated.parameters = parameters.Inflate (inflator);
			inflated.targs = targs;
			inflated.constraints = TypeParameterSpec.InflateConstraints (inflator, constraints ?? GenericDefinition.TypeParameters);
			inflated.state |= StateFlags.PendingMakeMethod;

			//			if (inflated.parent == null)
			//				inflated.parent = parent;

			//generic_intances.Add (targs, inflated);
			return inflated;
		}

		public MethodSpec Mutate (TypeParameterMutator mutator)
		{
			var targs = TypeArguments;
			if (targs != null)
				targs = mutator.Mutate (targs);

			var decl = DeclaringType;
			if (DeclaringType.IsGenericOrParentIsGeneric) {
				decl = mutator.Mutate (decl);
			}

			if (targs == TypeArguments && decl == DeclaringType)
				return this;

			var ms = (MethodSpec) MemberwiseClone ();
			if (decl != DeclaringType) {
				ms.inflatedMetaInfo = null;
				ms.declaringType = decl;
				ms.state |= StateFlags.PendingMetaInflate;
			}

			if (targs != null) {
				ms.targs = targs;
				ms.state |= StateFlags.PendingMakeMethod;
			}

			return ms;
		}

		public override List<TypeSpec> ResolveMissingDependencies ()
		{
			var missing = returnType.ResolveMissingDependencies ();
			foreach (var pt in parameters.Types) {
				var m = pt.GetMissingDependencies ();
				if (m == null)
					continue;

				if (missing == null)
					missing = new List<TypeSpec> ();

				missing.AddRange (m);
			}

			return missing;			
		}

		public void SetMetaInfo (MethodInfo info)
		{
			if (this.metaInfo != null)
				throw new InternalErrorException ("MetaInfo reset");

			this.metaInfo = info;
		}
	}

	public abstract class MethodOrOperator : MethodCore, IMethodData
	{
		public MethodBuilder MethodBuilder;
		ReturnParameter return_attributes;
		SecurityType declarative_security;
		protected MethodData MethodData;

		static readonly string[] attribute_targets = new string [] { "method", "return" };

		protected MethodOrOperator (TypeDefinition parent, FullNamedExpression type, Modifiers mod, Modifiers allowed_mod, MemberName name,
				Attributes attrs, ParametersCompiled parameters)
			: base (parent, type, mod, allowed_mod, name, attrs, parameters)
		{
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Target == AttributeTargets.ReturnValue) {
				if (return_attributes == null)
					return_attributes = new ReturnParameter (this, MethodBuilder, Location);

				return_attributes.ApplyAttributeBuilder (a, ctor, cdata, pa);
				return;
			}

			if (a.Type == pa.MethodImpl) {
				is_external_implementation = a.IsInternalCall ();
			}

			if (a.Type == pa.DllImport) {
				const Modifiers extern_static = Modifiers.EXTERN | Modifiers.STATIC;
				if ((ModFlags & extern_static) != extern_static) {
					Report.Error (601, a.Location, "The DllImport attribute must be specified on a method marked `static' and `extern'");
				}
				is_external_implementation = true;
			}

			if (a.IsValidSecurityAttribute ()) {
				a.ExtractSecurityPermissionSet (ctor, ref declarative_security);
				return;
			}

			if (MethodBuilder != null)
				MethodBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
		}

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Method; 
			}
		}

		protected override bool CheckForDuplications ()
		{
			return Parent.MemberCache.CheckExistingMembersOverloads (this, parameters);
		}

		public virtual EmitContext CreateEmitContext (ILGenerator ig)
		{
			return new EmitContext (this, ig, MemberType);
		}

		public override bool Define ()
		{
			if (!base.Define ())
				return false;

			if (!CheckBase ())
				return false;

			MemberKind kind;
			if (this is Operator)
				kind = MemberKind.Operator;
			else if (this is Destructor)
				kind = MemberKind.Destructor;
			else
				kind = MemberKind.Method;

			if (IsPartialDefinition) {
				caching_flags &= ~Flags.Excluded_Undetected;
				caching_flags |= Flags.Excluded;

				// Add to member cache only when a partial method implementation has not been found yet
				if ((caching_flags & Flags.PartialDefinitionExists) == 0) {
//					MethodBase mb = new PartialMethodDefinitionInfo (this);

					spec = new MethodSpec (kind, Parent.Definition, this, ReturnType, null, parameters, ModFlags);
					if (MemberName.Arity > 0) {
						spec.IsGeneric = true;

						// TODO: Have to move DefineMethod after Define (ideally to Emit)
						throw new NotImplementedException ("Generic partial methods");
					}

					Parent.MemberCache.AddMember (spec);
				}

				return true;
			}

			MethodData = new MethodData (
				this, ModFlags, flags, this, MethodBuilder, base_method);

			if (!MethodData.Define (Parent.PartialContainer, GetFullName (MemberName)))
				return false;
					
			MethodBuilder = MethodData.MethodBuilder;

			spec = new MethodSpec (kind, Parent.Definition, this, ReturnType, MethodBuilder, parameters, ModFlags);
			if (MemberName.Arity > 0)
				spec.IsGeneric = true;
			
			Parent.MemberCache.AddMember (this, MethodBuilder.Name, spec);

			return true;
		}

		protected override void DoMemberTypeIndependentChecks ()
		{
			base.DoMemberTypeIndependentChecks ();

			CheckAbstractAndExtern (block != null);

			if ((ModFlags & Modifiers.PARTIAL) != 0) {
				for (int i = 0; i < parameters.Count; ++i) {
					IParameterData p = parameters.FixedParameters [i];
					if (p.ModFlags == Parameter.Modifier.OUT) {
						Report.Error (752, Location, "`{0}': A partial method parameters cannot use `out' modifier",
							GetSignatureForError ());
					}

					if (p.HasDefaultValue && IsPartialImplementation)
						((Parameter) p).Warning_UselessOptionalParameter (Report);
				}
			}
		}

		protected override void DoMemberTypeDependentChecks ()
		{
			base.DoMemberTypeDependentChecks ();

			if (MemberType.IsStatic) {
				Error_StaticReturnType ();
			}
		}

		public override void Emit ()
		{
			if ((ModFlags & Modifiers.COMPILER_GENERATED) != 0 && !Parent.IsCompilerGenerated)
				Module.PredefinedAttributes.CompilerGenerated.EmitAttribute (MethodBuilder);
			if ((ModFlags & Modifiers.DEBUGGER_HIDDEN) != 0)
				Module.PredefinedAttributes.DebuggerHidden.EmitAttribute (MethodBuilder);

			if (ReturnType.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				return_attributes = new ReturnParameter (this, MethodBuilder, Location);
				Module.PredefinedAttributes.Dynamic.EmitAttribute (return_attributes.Builder);
			} else if (ReturnType.HasDynamicElement) {
				return_attributes = new ReturnParameter (this, MethodBuilder, Location);
				Module.PredefinedAttributes.Dynamic.EmitAttribute (return_attributes.Builder, ReturnType, Location);
			}

			if (OptAttributes != null)
				OptAttributes.Emit ();

			if (declarative_security != null) {
				foreach (var de in declarative_security) {
#if STATIC
					MethodBuilder.__AddDeclarativeSecurity (de);
#else
					MethodBuilder.AddDeclarativeSecurity (de.Key, de.Value);
#endif
				}
			}

			if (type_expr != null)
				ConstraintChecker.Check (this, member_type, type_expr.Location);

			base.Emit ();

			if (MethodData != null)
				MethodData.Emit (Parent);

			Block = null;
			MethodData = null;
		}

		protected void Error_ConditionalAttributeIsNotValid ()
		{
			Report.Error (577, Location,
				"Conditional not valid on `{0}' because it is a constructor, destructor, operator or explicit interface implementation",
				GetSignatureForError ());
		}

		public bool IsPartialDefinition {
			get {
				return (ModFlags & Modifiers.PARTIAL) != 0 && Block == null;
			}
		}

		public bool IsPartialImplementation {
			get {
				return (ModFlags & Modifiers.PARTIAL) != 0 && Block != null;
			}
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_targets;
			}
		}

		#region IMethodData Members

		bool IMethodData.IsAccessor {
			get {
				return false;
			}
		}

		public TypeSpec ReturnType {
			get {
				return MemberType;
			}
		}

		public MemberName MethodName {
			get {
				return MemberName;
			}
		}

		/// <summary>
		/// Returns true if method has conditional attribute and the conditions is not defined (method is excluded).
		/// </summary>
		public override string[] ConditionalConditions ()
		{
			if ((caching_flags & (Flags.Excluded_Undetected | Flags.Excluded)) == 0)
				return null;

			if ((ModFlags & Modifiers.PARTIAL) != 0 && (caching_flags & Flags.Excluded) != 0)
				return new string [0];

			caching_flags &= ~Flags.Excluded_Undetected;
			string[] conditions;

			if (base_method == null) {
				if (OptAttributes == null)
					return null;

				Attribute[] attrs = OptAttributes.SearchMulti (Module.PredefinedAttributes.Conditional);
				if (attrs == null)
					return null;

				conditions = new string[attrs.Length];
				for (int i = 0; i < conditions.Length; ++i)
					conditions[i] = attrs[i].GetConditionalAttributeValue ();
			} else {
				conditions = base_method.MemberDefinition.ConditionalConditions();
			}

			if (conditions != null)
				caching_flags |= Flags.Excluded;

			return conditions;
		}

		#endregion

	}

	public class SourceMethod : IMethodDef
	{
		MethodBase method;

		SourceMethod (MethodBase method, ICompileUnit file)
		{
			this.method = method;
			SymbolWriter.OpenMethod (file, this);
		}

		public string Name {
			get { return method.Name; }
		}

		public int Token {
			get {
				MethodToken token;
				var mb = method as MethodBuilder;
				if (mb != null)
					token = mb.GetToken ();
				else
					token = ((ConstructorBuilder) method).GetToken ();
#if STATIC
				if (token.IsPseudoToken)
					return ((ModuleBuilder) method.Module).ResolvePseudoToken (token.Token);
#endif
				return token.Token;
			}
		}

		public void CloseMethod ()
		{
			SymbolWriter.CloseMethod ();
		}

		public static SourceMethod Create (TypeDefinition parent, MethodBase method)
		{
			if (!SymbolWriter.HasSymbolWriter)
				return null;

			var source_file = parent.GetCompilationSourceFile ();
			if (source_file == null)
				return null;

			return new SourceMethod (method, source_file.SymbolUnitEntry);
		}
	}

	public class Method : MethodOrOperator, IGenericMethodDefinition
	{
		Method partialMethodImplementation;

		public Method (TypeDefinition parent, FullNamedExpression return_type, Modifiers mod, MemberName name, ParametersCompiled parameters, Attributes attrs)
			: base (parent, return_type, mod,
				parent.PartialContainer.Kind == MemberKind.Interface ? AllowedModifiersInterface :
				parent.PartialContainer.Kind == MemberKind.Struct ? AllowedModifiersStruct | Modifiers.ASYNC :
				AllowedModifiersClass | Modifiers.ASYNC,
				name, attrs, parameters)
		{
		}

		protected Method (TypeDefinition parent, FullNamedExpression return_type, Modifiers mod, Modifiers amod,
					MemberName name, ParametersCompiled parameters, Attributes attrs)
			: base (parent, return_type, mod, amod, name, attrs, parameters)
		{
		}

		#region Properties

		public override TypeParameters CurrentTypeParameters {
			get {
				return MemberName.TypeParameters;
			}
		}

		public TypeParameterSpec[] TypeParameters {
			get {
				return CurrentTypeParameters.Types;
			}
		}

		public int TypeParametersCount {
			get {
				return CurrentTypeParameters == null ? 0 : CurrentTypeParameters.Count;
			}
		}

#endregion

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public static Method Create (TypeDefinition parent, FullNamedExpression returnType, Modifiers mod,
				   MemberName name, ParametersCompiled parameters, Attributes attrs, bool hasConstraints)
		{
			var m = new Method (parent, returnType, mod, name, parameters, attrs);

			if (hasConstraints && ((mod & Modifiers.OVERRIDE) != 0 || m.IsExplicitImpl)) {
				m.Report.Error (460, m.Location,
					"`{0}': Cannot specify constraints for overrides and explicit interface implementation methods",
					m.GetSignatureForError ());
			}

			if ((mod & Modifiers.PARTIAL) != 0) {
				const Modifiers invalid_partial_mod = Modifiers.AccessibilityMask | Modifiers.ABSTRACT | Modifiers.EXTERN |
					Modifiers.NEW | Modifiers.OVERRIDE | Modifiers.SEALED | Modifiers.VIRTUAL;

				if ((mod & invalid_partial_mod) != 0) {
					m.Report.Error (750, m.Location,
						"A partial method cannot define access modifier or any of abstract, extern, new, override, sealed, or virtual modifiers");
					mod &= ~invalid_partial_mod;
				}

				if ((parent.ModFlags & Modifiers.PARTIAL) == 0) {
					m.Report.Error (751, m.Location, 
						"A partial method must be declared within a partial class or partial struct");
				}
			}

			if ((mod & Modifiers.STATIC) == 0 && parameters.HasExtensionMethodType) {
				m.Report.Error (1105, m.Location, "`{0}': Extension methods must be declared static",
					m.GetSignatureForError ());
			}


			return m;
		}

		public override string GetSignatureForError()
		{
			return base.GetSignatureForError () + parameters.GetSignatureForError ();
		}

		void Error_DuplicateEntryPoint (Method b)
		{
			Report.Error (17, b.Location,
				"Program `{0}' has more than one entry point defined: `{1}'",
				b.Module.Builder.ScopeName, b.GetSignatureForError ());
		}

		bool IsEntryPoint ()
		{
			if (ReturnType.Kind != MemberKind.Void && ReturnType.BuiltinType != BuiltinTypeSpec.Type.Int)
				return false;

			if (parameters.IsEmpty)
				return true;

			if (parameters.Count > 1)
				return false;

			var ac = parameters.Types [0] as ArrayContainer;
			return ac != null && ac.Rank == 1 && ac.Element.BuiltinType == BuiltinTypeSpec.Type.String &&
					(parameters[0].ModFlags & ~Parameter.Modifier.PARAMS) == Parameter.Modifier.NONE;
		}

		public override FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc)
		{
			if (arity == 0) {
				var tp = CurrentTypeParameters;
				if (tp != null) {
					TypeParameter t = tp.Find (name);
					if (t != null)
						return new TypeParameterExpr (t, loc);
				}
			}

			return base.LookupNamespaceOrType (name, arity, mode, loc);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Type == pa.Conditional) {
				if (IsExplicitImpl) {
					Error_ConditionalAttributeIsNotValid ();
					return;
				}

				if ((ModFlags & Modifiers.OVERRIDE) != 0) {
					Report.Error (243, Location, "Conditional not valid on `{0}' because it is an override method", GetSignatureForError ());
					return;
				}

				if (ReturnType.Kind != MemberKind.Void) {
					Report.Error (578, Location, "Conditional not valid on `{0}' because its return type is not void", GetSignatureForError ());
					return;
				}

				if (IsInterface) {
					Report.Error (582, Location, "Conditional not valid on interface members");
					return;
				}

				if (MethodData.implementing != null) {
					Report.SymbolRelatedToPreviousError (MethodData.implementing.DeclaringType);
					Report.Error (629, Location, "Conditional member `{0}' cannot implement interface member `{1}'",
						GetSignatureForError (), TypeManager.CSharpSignature (MethodData.implementing));
					return;
				}

				for (int i = 0; i < parameters.Count; ++i) {
					if (parameters.FixedParameters [i].ModFlags == Parameter.Modifier.OUT) {
						Report.Error (685, Location, "Conditional method `{0}' cannot have an out parameter", GetSignatureForError ());
						return;
					}
				}
			}

			if (a.Type == pa.Extension) {
				a.Error_MisusedExtensionAttribute ();
				return;
			}

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}

		void CreateTypeParameters ()
		{
			var tparams = MemberName.TypeParameters;
			string[] snames = new string[MemberName.Arity];
			var parent_tparams = Parent.TypeParametersAll;

			for (int i = 0; i < snames.Length; i++) {
				string type_argument_name = tparams[i].MemberName.Name;

				if (block == null) {
					int idx = parameters.GetParameterIndexByName (type_argument_name);
					if (idx >= 0) {
						var b = block;
						if (b == null)
							b = new ToplevelBlock (Compiler, Location);

						b.Error_AlreadyDeclaredTypeParameter (type_argument_name, parameters[i].Location);
					}
				} else {
					INamedBlockVariable variable = null;
					block.GetLocalName (type_argument_name, block, ref variable);
					if (variable != null)
						variable.Block.Error_AlreadyDeclaredTypeParameter (type_argument_name, variable.Location);
				}

				if (parent_tparams != null) {
					var tp = parent_tparams.Find (type_argument_name);
					if (tp != null) {
						tparams[i].WarningParentNameConflict (tp);
					}
				}

				snames[i] = type_argument_name;
			}

			GenericTypeParameterBuilder[] gen_params = MethodBuilder.DefineGenericParameters (snames);
			tparams.Define (gen_params, null, 0, Parent);
		}

		protected virtual void DefineTypeParameters ()
		{
			var tparams = CurrentTypeParameters;

			TypeParameterSpec[] base_tparams = null;
			TypeParameterSpec[] base_decl_tparams = TypeParameterSpec.EmptyTypes;
			TypeSpec[] base_targs = TypeSpec.EmptyTypes;
			if (((ModFlags & Modifiers.OVERRIDE) != 0 || IsExplicitImpl)) {
				if (base_method != null) {
					base_tparams = base_method.GenericDefinition.TypeParameters;
				
					if (base_method.DeclaringType.IsGeneric) {
						base_decl_tparams = base_method.DeclaringType.MemberDefinition.TypeParameters;

						var base_type_parent = CurrentType;
						while (base_type_parent.BaseType != base_method.DeclaringType) {
							base_type_parent = base_type_parent.BaseType;
						}

						base_targs = base_type_parent.BaseType.TypeArguments;
					}

					if (base_method.IsGeneric) {
						ObsoleteAttribute oa;
						foreach (var base_tp in base_tparams) {
							oa = base_tp.BaseType.GetAttributeObsolete ();
							if (oa != null) {
								AttributeTester.Report_ObsoleteMessage (oa, base_tp.BaseType.GetSignatureForError (), Location, Report);
							}

							if (base_tp.InterfacesDefined != null) {
								foreach (var iface in base_tp.InterfacesDefined) {
									oa = iface.GetAttributeObsolete ();
									if (oa != null) {
										AttributeTester.Report_ObsoleteMessage (oa, iface.GetSignatureForError (), Location, Report);
									}
								}
							}
						}

						if (base_decl_tparams.Length != 0) {
							base_decl_tparams = base_decl_tparams.Concat (base_tparams).ToArray ();
							base_targs = base_targs.Concat (tparams.Types).ToArray ();
						} else {
							base_decl_tparams = base_tparams;
							base_targs = tparams.Types;
						}
					}
				} else if (MethodData.implementing != null) {
					base_tparams = MethodData.implementing.GenericDefinition.TypeParameters;
					if (MethodData.implementing.DeclaringType.IsGeneric) {
						base_decl_tparams = MethodData.implementing.DeclaringType.MemberDefinition.TypeParameters;
						foreach (var iface in Parent.CurrentType.Interfaces) {
							if (iface == MethodData.implementing.DeclaringType) {
								base_targs = iface.TypeArguments;
								break;
							}
						}
					}
				}
			}

			for (int i = 0; i < tparams.Count; ++i) {
				var tp = tparams[i];

				if (!tp.ResolveConstraints (this))
					continue;

				//
				// Copy base constraints for override/explicit methods
				//
				if (base_tparams != null) {
					var base_tparam = base_tparams[i];
					var local_tparam = tp.Type;
					local_tparam.SpecialConstraint = base_tparam.SpecialConstraint;

					var inflator = new TypeParameterInflator (this, CurrentType, base_decl_tparams, base_targs);
					base_tparam.InflateConstraints (inflator, local_tparam);

					//
					// Check all type argument constraints for possible collision or unification
					// introduced by inflating inherited constraints in this context
					//
					// Conflict example:
					//
					// class A<T> { virtual void Foo<U> () where U : class, T {} }
					// class B : A<int> { override void Foo<U> {} }
					//
					var local_tparam_targs = local_tparam.TypeArguments;
					if (local_tparam_targs != null) {
						for (int ii = 0; ii < local_tparam_targs.Length; ++ii) {
							var ta = local_tparam_targs [ii];
							if (!ta.IsClass && !ta.IsStruct)
								continue;

							TypeSpec[] unique_tparams = null;
							for (int iii = ii + 1; iii < local_tparam_targs.Length; ++iii) {
								//
								// Remove any identical or unified constraint types
								//
								var tparam_checked = local_tparam_targs[iii];
								if (TypeSpecComparer.IsEqual (ta, tparam_checked) || TypeSpec.IsBaseClass (ta, tparam_checked, false)) {
									unique_tparams = new TypeSpec[local_tparam_targs.Length - 1];
									Array.Copy (local_tparam_targs, 0, unique_tparams, 0, iii);
									Array.Copy (local_tparam_targs, iii + 1, unique_tparams, iii, local_tparam_targs.Length - iii - 1);
								} else if (!TypeSpec.IsBaseClass (tparam_checked, ta, false)) {
									Constraints.Error_ConflictingConstraints (this, local_tparam, ta, tparam_checked, Location);
								}
							}

							if (unique_tparams != null) {
								local_tparam_targs = unique_tparams;
								local_tparam.TypeArguments = local_tparam_targs;
								continue;
							}

							Constraints.CheckConflictingInheritedConstraint (local_tparam, ta, this, Location);
						}
					}

					continue;
				}
			}

			if (base_tparams == null && MethodData != null && MethodData.implementing != null) {
				CheckImplementingMethodConstraints (Parent, spec, MethodData.implementing);
			}
		}

		public static bool CheckImplementingMethodConstraints (TypeContainer container, MethodSpec method, MethodSpec baseMethod)
		{
			var tparams = method.Constraints;
			var base_tparams = baseMethod.Constraints;
			for (int i = 0; i < tparams.Length; ++i) {
				if (!tparams[i].HasSameConstraintsImplementation (base_tparams[i])) {
					container.Compiler.Report.SymbolRelatedToPreviousError (method);
					container.Compiler.Report.SymbolRelatedToPreviousError (baseMethod);

					// Using container location because the interface can be implemented
					// by base class
					container.Compiler.Report.Error (425, container.Location,
						"The constraints for type parameter `{0}' of method `{1}' must match the constraints for type parameter `{2}' of interface method `{3}'. Consider using an explicit interface implementation instead",
						tparams[i].GetSignatureForError (), method.GetSignatureForError (),
						base_tparams[i].GetSignatureForError (), baseMethod.GetSignatureForError ());
 
					return false;
				}
			}

			return true;
		}

		//
		// Creates the type
		//
		public override bool Define ()
		{
			if (!base.Define ())
				return false;

			if (member_type.Kind == MemberKind.Void && parameters.IsEmpty && MemberName.Arity == 0 && MemberName.Name == Destructor.MetadataName) {
				Report.Warning (465, 1, Location,
					"Introducing `Finalize' method can interfere with destructor invocation. Did you intend to declare a destructor?");
			}

			if (partialMethodImplementation != null && IsPartialDefinition)
				MethodBuilder = partialMethodImplementation.MethodBuilder;

			if (Compiler.Settings.StdLib && ReturnType.IsSpecialRuntimeType) {
				Error1599 (Location, ReturnType, Report);
				return false;
			}

			if (CurrentTypeParameters == null) {
				if (base_method != null && !IsExplicitImpl) {
					if (parameters.Count == 1 && ParameterTypes[0].BuiltinType == BuiltinTypeSpec.Type.Object && MemberName.Name == "Equals")
						Parent.PartialContainer.Mark_HasEquals ();
					else if (parameters.IsEmpty && MemberName.Name == "GetHashCode")
						Parent.PartialContainer.Mark_HasGetHashCode ();
				}
					
			} else {
				DefineTypeParameters ();
			}

			if (block != null) {
				if (block.IsIterator) {
					//
					// Current method is turned into automatically generated
					// wrapper which creates an instance of iterator
					//
					Iterator.CreateIterator (this, Parent.PartialContainer, ModFlags);
					ModFlags |= Modifiers.DEBUGGER_HIDDEN;
				}

				if ((ModFlags & Modifiers.ASYNC) != 0) {
					if (ReturnType.Kind != MemberKind.Void &&
						ReturnType != Module.PredefinedTypes.Task.TypeSpec &&
						!ReturnType.IsGenericTask) {
						Report.Error (1983, Location, "The return type of an async method must be void, Task, or Task<T>");
					}

					AsyncInitializer.Create (this, block, parameters, Parent.PartialContainer, ReturnType, Location);
				}
			}

			if ((ModFlags & Modifiers.STATIC) == 0)
				return true;

			if (parameters.HasExtensionMethodType) {
				if (Parent.PartialContainer.IsStatic && !Parent.IsGenericOrParentIsGeneric) {
					if (!Parent.IsTopLevel)
						Report.Error (1109, Location, "`{0}': Extension methods cannot be defined in a nested class",
							GetSignatureForError ());

					PredefinedAttribute pa = Module.PredefinedAttributes.Extension;
					if (!pa.IsDefined) {
						Report.Error (1110, Location,
							"`{0}': Extension methods require `System.Runtime.CompilerServices.ExtensionAttribute' type to be available. Are you missing an assembly reference?",
							GetSignatureForError ());
					}

					ModFlags |= Modifiers.METHOD_EXTENSION;
					Parent.PartialContainer.ModFlags |= Modifiers.METHOD_EXTENSION;
					Spec.DeclaringType.SetExtensionMethodContainer ();
					Parent.Module.HasExtensionMethod = true;
				} else {
					Report.Error (1106, Location, "`{0}': Extension methods must be defined in a non-generic static class",
						GetSignatureForError ());
				}
			}

			//
			// This is used to track the Entry Point,
			//
			var settings = Compiler.Settings;
			if (settings.NeedsEntryPoint && MemberName.Name == "Main" && (settings.MainClass == null || settings.MainClass == Parent.TypeBuilder.FullName)) {
				if (IsEntryPoint ()) {
					if (Parent.DeclaringAssembly.EntryPoint == null) {
						if (Parent.IsGenericOrParentIsGeneric || MemberName.IsGeneric) {
							Report.Warning (402, 4, Location, "`{0}': an entry point cannot be generic or in a generic type",
								GetSignatureForError ());
						} else if ((ModFlags & Modifiers.ASYNC) != 0) {
							Report.Error (4009, Location, "`{0}': an entry point cannot be async method",
								GetSignatureForError ());
						} else {
							SetIsUsed ();
							Parent.DeclaringAssembly.EntryPoint = this;
						}
					} else {
						Error_DuplicateEntryPoint (Parent.DeclaringAssembly.EntryPoint);
						Error_DuplicateEntryPoint (this);
					}
				} else {
					Report.Warning (28, 4, Location, "`{0}' has the wrong signature to be an entry point",
						GetSignatureForError ());
				}
			}

			return true;
		}

		//
		// Emits the code
		// 
		public override void Emit ()
		{
			try {
				if (IsPartialDefinition) {
					//
					// Use partial method implementation builder for partial method declaration attributes
					//
					if (partialMethodImplementation != null) {
						MethodBuilder = partialMethodImplementation.MethodBuilder;
					}

					return;
				}
				
				if ((ModFlags & Modifiers.PARTIAL) != 0 && (caching_flags & Flags.PartialDefinitionExists) == 0) {
					Report.Error (759, Location, "A partial method `{0}' implementation is missing a partial method declaration",
						GetSignatureForError ());
				}

				if (CurrentTypeParameters != null) {
					for (int i = 0; i < CurrentTypeParameters.Count; ++i) {
						var tp = CurrentTypeParameters [i];
						tp.CheckGenericConstraints (false);
						tp.Emit ();
					}
				}

				base.Emit ();
				
				if ((ModFlags & Modifiers.METHOD_EXTENSION) != 0)
					Module.PredefinedAttributes.Extension.EmitAttribute (MethodBuilder);
			} catch {
				Console.WriteLine ("Internal compiler error at {0}: exception caught while emitting {1}",
						   Location, MethodBuilder);
				throw;
			}
		}

		public override bool EnableOverloadChecks (MemberCore overload)
		{
			if (overload is Indexer)
				return false;

			return base.EnableOverloadChecks (overload);
		}

		public static void Error1599 (Location loc, TypeSpec t, Report Report)
		{
			Report.Error (1599, loc, "Method or delegate cannot return type `{0}'", TypeManager.CSharpName (t));
		}

		protected override bool ResolveMemberType ()
		{
			if (CurrentTypeParameters != null) {
				MethodBuilder = Parent.TypeBuilder.DefineMethod (GetFullName (MemberName), flags);
				CreateTypeParameters ();
			}

			return base.ResolveMemberType ();
		}

		public void SetPartialDefinition (Method methodDefinition)
		{
			caching_flags |= Flags.PartialDefinitionExists;
			methodDefinition.partialMethodImplementation = this;

			// Ensure we are always using method declaration parameters
			for (int i = 0; i < methodDefinition.parameters.Count; ++i ) {
				parameters [i].Name = methodDefinition.parameters [i].Name;
				parameters [i].DefaultValue = methodDefinition.parameters [i].DefaultValue;
			}

			if (methodDefinition.attributes == null)
				return;

			if (attributes == null) {
				attributes = methodDefinition.attributes;
			} else {
				attributes.Attrs.AddRange (methodDefinition.attributes.Attrs);
			}
		}
	}

	public abstract class ConstructorInitializer : ExpressionStatement
	{
		Arguments argument_list;
		MethodSpec base_ctor;

		public ConstructorInitializer (Arguments argument_list, Location loc)
		{
			this.argument_list = argument_list;
			this.loc = loc;
		}

		public Arguments Arguments {
			get {
				return argument_list;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotSupportedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			eclass = ExprClass.Value;

			// FIXME: Hack
			var caller_builder = (Constructor) ec.MemberContext;

			//
			// Spec mandates that constructor initializer will not have `this' access
			//
			using (ec.Set (ResolveContext.Options.BaseInitializer)) {
				if (argument_list != null) {
					bool dynamic;
					argument_list.Resolve (ec, out dynamic);

					if (dynamic) {
						ec.Report.Error (1975, loc,
							"The constructor call cannot be dynamically dispatched within constructor initializer");

						return null;
					}
				}

				type = ec.CurrentType;
				if (this is ConstructorBaseInitializer) {
					if (ec.CurrentType.BaseType == null)
						return this;

					type = ec.CurrentType.BaseType;
					if (ec.CurrentType.IsStruct) {
						ec.Report.Error (522, loc,
							"`{0}': Struct constructors cannot call base constructors", caller_builder.GetSignatureForError ());
						return this;
					}
				} else {
					//
					// It is legal to have "this" initializers that take no arguments
					// in structs, they are just no-ops.
					//
					// struct D { public D (int a) : this () {}
					//
					if (ec.CurrentType.IsStruct && argument_list == null)
						return this;
				}

				base_ctor = ConstructorLookup (ec, type, ref argument_list, loc);
			}
	
			// TODO MemberCache: Does it work for inflated types ?
			if (base_ctor == caller_builder.Spec){
				ec.Report.Error (516, loc, "Constructor `{0}' cannot call itself",
					caller_builder.GetSignatureForError ());
			}

			return this;
		}

		public override void Emit (EmitContext ec)
		{
			// It can be null for static initializers
			if (base_ctor == null)
				return;
			
			var call = new CallEmitter ();
			call.InstanceExpression = new CompilerGeneratedThis (type, loc); 
			call.EmitPredefined (ec, base_ctor, argument_list);
		}

		public override void EmitStatement (EmitContext ec)
		{
			Emit (ec);
		}
	}

	public class ConstructorBaseInitializer : ConstructorInitializer {
		public ConstructorBaseInitializer (Arguments argument_list, Location l) :
			base (argument_list, l)
		{
		}
	}

	class GeneratedBaseInitializer: ConstructorBaseInitializer {
		public GeneratedBaseInitializer (Location loc):
			base (null, loc)
		{
		}
	}

	public class ConstructorThisInitializer : ConstructorInitializer {
		public ConstructorThisInitializer (Arguments argument_list, Location l) :
			base (argument_list, l)
		{
		}
	}
	
	public class Constructor : MethodCore, IMethodData {
		public ConstructorBuilder ConstructorBuilder;
		public ConstructorInitializer Initializer;
		SecurityType declarative_security;
		bool has_compliant_args;

		// <summary>
		//   Modifiers allowed for a constructor.
		// </summary>
		public const Modifiers AllowedModifiers =
			Modifiers.PUBLIC |
			Modifiers.PROTECTED |
			Modifiers.INTERNAL |
			Modifiers.STATIC |
			Modifiers.UNSAFE |
			Modifiers.EXTERN |		
			Modifiers.PRIVATE;

		static readonly string[] attribute_targets = new string [] { "method" };

		public static readonly string ConstructorName = ".ctor";
		public static readonly string TypeConstructorName = ".cctor";

		public Constructor (TypeDefinition parent, string name, Modifiers mod, Attributes attrs, ParametersCompiled args, Location loc)
			: base (parent, null, mod, AllowedModifiers, new MemberName (name, loc), attrs, args)
		{
		}

		public bool HasCompliantArgs {
			get {
				return has_compliant_args;
			}
		}

		public override AttributeTargets AttributeTargets {
			get {
				return AttributeTargets.Constructor;
			}
		}

		bool IMethodData.IsAccessor {
			get {
				return false;
			}
		}

		//
		// Returns true if this is a default constructor
		//
		public bool IsDefault ()
		{
			if ((ModFlags & Modifiers.STATIC) != 0)
				return parameters.IsEmpty;

			return parameters.IsEmpty &&
					(Initializer is ConstructorBaseInitializer) &&
					(Initializer.Arguments == null);
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.IsValidSecurityAttribute ()) {
				a.ExtractSecurityPermissionSet (ctor, ref declarative_security);
				return;
			}

			if (a.Type == pa.MethodImpl) {
				is_external_implementation = a.IsInternalCall ();
			}

			ConstructorBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
		}

		protected override bool CheckBase ()
		{
			if ((ModFlags & Modifiers.STATIC) != 0) {
				if (!parameters.IsEmpty) {
					Report.Error (132, Location, "`{0}': The static constructor must be parameterless",
						GetSignatureForError ());
					return false;
				}

				if ((caching_flags & Flags.MethodOverloadsExist) != 0)
					Parent.MemberCache.CheckExistingMembersOverloads (this, parameters);

				// the rest can be ignored
				return true;
			}

			// Check whether arguments were correct.
			if (!DefineParameters (parameters))
				return false;

			if ((caching_flags & Flags.MethodOverloadsExist) != 0)
				Parent.MemberCache.CheckExistingMembersOverloads (this, parameters);

			if (Parent.PartialContainer.Kind == MemberKind.Struct && parameters.IsEmpty) {
				Report.Error (568, Location, 
					"Structs cannot contain explicit parameterless constructors");
				return false;
			}

			CheckProtectedModifier ();
			
			return true;
		}
		
		//
		// Creates the ConstructorBuilder
		//
		public override bool Define ()
		{
			if (ConstructorBuilder != null)
				return true;

			if (!CheckAbstractAndExtern (block != null))
				return false;
			
			// Check if arguments were correct.
			if (!CheckBase ())
				return false;

			var ca = ModifiersExtensions.MethodAttr (ModFlags) | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;

			ConstructorBuilder = Parent.TypeBuilder.DefineConstructor (
				ca, CallingConventions,
				parameters.GetMetaInfo ());

			spec = new MethodSpec (MemberKind.Constructor, Parent.Definition, this, Compiler.BuiltinTypes.Void, ConstructorBuilder, parameters, ModFlags);
			
			Parent.MemberCache.AddMember (spec);
			
			// It's here only to report an error
			if (block != null && block.IsIterator) {
				member_type = Compiler.BuiltinTypes.Void;
				Iterator.CreateIterator (this, Parent.PartialContainer, ModFlags);
			}

			return true;
		}

		//
		// Emits the code
		//
		public override void Emit ()
		{
			if (Parent.PartialContainer.IsComImport) {
				if (!IsDefault ()) {
					Report.Error (669, Location, "`{0}': A class with the ComImport attribute cannot have a user-defined constructor",
						Parent.GetSignatureForError ());
				}

				// Set as internal implementation and reset block data
				// to ensure no IL is generated
				ConstructorBuilder.SetImplementationFlags (MethodImplAttributes.InternalCall);
				block = null;
			}

			if ((ModFlags & Modifiers.DEBUGGER_HIDDEN) != 0)
				Module.PredefinedAttributes.DebuggerHidden.EmitAttribute (ConstructorBuilder);

			if (OptAttributes != null)
				OptAttributes.Emit ();

			base.Emit ();
			parameters.ApplyAttributes (this, ConstructorBuilder);


			BlockContext bc = new BlockContext (this, block, Compiler.BuiltinTypes.Void);
			bc.Set (ResolveContext.Options.ConstructorScope);

			//
			// If we use a "this (...)" constructor initializer, then
			// do not emit field initializers, they are initialized in the other constructor
			//
			if (!(Initializer is ConstructorThisInitializer))
				Parent.PartialContainer.ResolveFieldInitializers (bc);

			if (block != null) {
				if (!IsStatic) {
					if (Initializer == null) {
						if (Parent.PartialContainer.Kind == MemberKind.Struct) {
							//
							// If this is a non-static `struct' constructor and doesn't have any
							// initializer, it must initialize all of the struct's fields.
							//
							block.AddThisVariable (bc);
						} else if (Parent.PartialContainer.Kind == MemberKind.Class) {
							Initializer = new GeneratedBaseInitializer (Location);
						}
					}

					if (Initializer != null) {
						//
						// Use location of the constructor to emit sequence point of initializers
						// at beginning of constructor name
						//
						// TODO: Need to extend mdb to support line regions to allow set a breakpoint at
						// initializer
						//
						block.AddScopeStatement (new StatementExpression (Initializer, Location));
					}
				}

				if (block.Resolve (null, bc, this)) {
					EmitContext ec = new EmitContext (this, ConstructorBuilder.GetILGenerator (), bc.ReturnType);
					ec.With (EmitContext.Options.ConstructorScope, true);

					SourceMethod source = SourceMethod.Create (Parent, ConstructorBuilder);

					block.Emit (ec);

					if (source != null)
						source.CloseMethod ();
				}
			}

			if (declarative_security != null) {
				foreach (var de in declarative_security) {
#if STATIC
					ConstructorBuilder.__AddDeclarativeSecurity (de);
#else
					ConstructorBuilder.AddDeclarativeSecurity (de.Key, de.Value);
#endif
				}
			}

			block = null;
		}

		protected override MemberSpec FindBaseMember (out MemberSpec bestCandidate, ref bool overrides)
		{
			// Is never override
			bestCandidate = null;
			return null;
		}

		public override string GetSignatureForDocumentation ()
		{
			return Parent.GetSignatureForDocumentation () + ".#ctor" + parameters.GetSignatureForDocumentation ();
		}

		public override string GetSignatureForError()
		{
			return base.GetSignatureForError () + parameters.GetSignatureForError ();
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_targets;
			}
		}

		protected override bool VerifyClsCompliance ()
		{
			if (!base.VerifyClsCompliance () || !IsExposedFromAssembly ()) {
				return false;
			}

			if (!parameters.IsEmpty && Parent.Definition.IsAttribute) {
				foreach (TypeSpec param in parameters.Types) {
					if (param.IsArray) {
						return true;
					}
				}
			}

			has_compliant_args = true;
			return true;
		}

		#region IMethodData Members

		public MemberName MethodName {
			get {
				return MemberName;
			}
		}

		public TypeSpec ReturnType {
			get {
				return MemberType;
			}
		}

		public EmitContext CreateEmitContext (ILGenerator ig)
		{
			throw new NotImplementedException ();
		}

		public bool IsExcluded()
		{
			return false;
		}

		#endregion
	}

	/// <summary>
	/// Interface for MethodData class. Holds links to parent members to avoid member duplication.
	/// </summary>
	public interface IMethodData : IMemberContext
	{
		CallingConventions CallingConventions { get; }
		Location Location { get; }
		MemberName MethodName { get; }
		TypeSpec ReturnType { get; }
		ParametersCompiled ParameterInfo { get; }
		MethodSpec Spec { get; }
		bool IsAccessor { get; }

		Attributes OptAttributes { get; }
		ToplevelBlock Block { get; set; }

		EmitContext CreateEmitContext (ILGenerator ig);
	}

	//
	// Encapsulates most of the Method's state
	//
	public class MethodData
	{
#if !STATIC
		static FieldInfo methodbuilder_attrs_field;
#endif

		public readonly IMethodData method;

		//
		// Are we implementing an interface ?
		//
		public MethodSpec implementing;

		//
		// Protected data.
		//
		protected InterfaceMemberBase member;
		protected Modifiers modifiers;
		protected MethodAttributes flags;
		protected TypeSpec declaring_type;
		protected MethodSpec parent_method;

		MethodBuilder builder;
		public MethodBuilder MethodBuilder {
			get {
				return builder;
			}
		}

		public TypeSpec DeclaringType {
			get {
				return declaring_type;
			}
		}

		public MethodData (InterfaceMemberBase member,
				   Modifiers modifiers, MethodAttributes flags, IMethodData method)
		{
			this.member = member;
			this.modifiers = modifiers;
			this.flags = flags;

			this.method = method;
		}

		public MethodData (InterfaceMemberBase member,
				   Modifiers modifiers, MethodAttributes flags, 
				   IMethodData method, MethodBuilder builder,
				   MethodSpec parent_method)
			: this (member, modifiers, flags, method)
		{
			this.builder = builder;
			this.parent_method = parent_method;
		}

		public bool Define (TypeDefinition container, string method_full_name)
		{
			PendingImplementation pending = container.PendingImplementations;
			MethodSpec ambig_iface_method;
			bool optional = false;

			if (pending != null) {
				implementing = pending.IsInterfaceMethod (method.MethodName, member.InterfaceType, this, out ambig_iface_method, ref optional);

				if (member.InterfaceType != null) {
					if (implementing == null) {
						if (member is PropertyBase) {
							container.Compiler.Report.Error (550, method.Location,
								"`{0}' is an accessor not found in interface member `{1}{2}'",
									  method.GetSignatureForError (), TypeManager.CSharpName (member.InterfaceType),
									  member.GetSignatureForError ().Substring (member.GetSignatureForError ().LastIndexOf ('.')));

						} else {
							container.Compiler.Report.Error (539, method.Location,
									  "`{0}.{1}' in explicit interface declaration is not a member of interface",
									  TypeManager.CSharpName (member.InterfaceType), member.ShortName);
						}
						return false;
					}
					if (implementing.IsAccessor && !method.IsAccessor) {
						container.Compiler.Report.SymbolRelatedToPreviousError (implementing);
						container.Compiler.Report.Error (683, method.Location,
							"`{0}' explicit method implementation cannot implement `{1}' because it is an accessor",
							member.GetSignatureForError (), TypeManager.CSharpSignature (implementing));
						return false;
					}
				} else {
					if (implementing != null) {
						if (!method.IsAccessor) {
							if (implementing.IsAccessor) {
								container.Compiler.Report.SymbolRelatedToPreviousError (implementing);
								container.Compiler.Report.Error (470, method.Location,
									"Method `{0}' cannot implement interface accessor `{1}'",
									method.GetSignatureForError (), TypeManager.CSharpSignature (implementing));
							}
						} else if (implementing.DeclaringType.IsInterface) {
							if (!implementing.IsAccessor) {
								container.Compiler.Report.SymbolRelatedToPreviousError (implementing);
								container.Compiler.Report.Error (686, method.Location,
									"Accessor `{0}' cannot implement interface member `{1}' for type `{2}'. Use an explicit interface implementation",
									method.GetSignatureForError (), TypeManager.CSharpSignature (implementing), container.GetSignatureForError ());
							} else {
								PropertyBase.PropertyMethod pm = method as PropertyBase.PropertyMethod;
								if (pm != null && pm.HasCustomAccessModifier && (pm.ModFlags & Modifiers.PUBLIC) == 0) {
									container.Compiler.Report.SymbolRelatedToPreviousError (implementing);
									container.Compiler.Report.Error (277, method.Location,
										"Accessor `{0}' must be declared public to implement interface member `{1}'",
										method.GetSignatureForError (), implementing.GetSignatureForError ());
								}
							}
						}
					}
				}
			} else {
				ambig_iface_method = null;
			}

			//
			// For implicit implementations, make sure we are public, for
			// explicit implementations, make sure we are private.
			//
			if (implementing != null){
				if (member.IsExplicitImpl) {
					if (method.ParameterInfo.HasParams && !implementing.Parameters.HasParams) {
						container.Compiler.Report.SymbolRelatedToPreviousError (implementing);
						container.Compiler.Report.Error (466, method.Location,
							"`{0}': the explicit interface implementation cannot introduce the params modifier",
							method.GetSignatureForError ());
					}

					if (ambig_iface_method != null) {
						container.Compiler.Report.SymbolRelatedToPreviousError (ambig_iface_method);
						container.Compiler.Report.SymbolRelatedToPreviousError (implementing);
						container.Compiler.Report.Warning (473, 2, method.Location,
							"Explicit interface implementation `{0}' matches more than one interface member. Consider using a non-explicit implementation instead",
							method.GetSignatureForError ());
					}
				} else {
					//
					// Setting implementin to null inside this block will trigger a more
					// verbose error reporting for missing interface implementations
					//
					if (implementing.DeclaringType.IsInterface) {
						//
						// If this is an interface method implementation,
						// check for public accessibility
						//
						if ((flags & MethodAttributes.MemberAccessMask) != MethodAttributes.Public) {
							implementing = null;
						} else if (optional && (container.Interfaces == null || Array.IndexOf (container.Interfaces, implementing.DeclaringType) < 0)) {
							//
							// We are not implementing interface when base class already implemented it
							//
							implementing = null;
						}
					} else if ((flags & MethodAttributes.MemberAccessMask) == MethodAttributes.Private) {
						// We may never be private.
						implementing = null;

					} else if ((modifiers & Modifiers.OVERRIDE) == 0) {
						//
						// We may be protected if we're overriding something.
						//
						implementing = null;
					}
				}
					
				//
				// Static is not allowed
				//
				if ((modifiers & Modifiers.STATIC) != 0){
					implementing = null;
				}
			}
			
			//
			// If implementing is still valid, set flags
			//
			if (implementing != null){
				//
				// When implementing interface methods, set NewSlot
				// unless, we are overwriting a method.
				//
				if ((modifiers & Modifiers.OVERRIDE) == 0 && implementing.DeclaringType.IsInterface) {
					flags |= MethodAttributes.NewSlot;
				}

				flags |= MethodAttributes.Virtual | MethodAttributes.HideBySig;

				// Set Final unless we're virtual, abstract or already overriding a method.
				if ((modifiers & (Modifiers.VIRTUAL | Modifiers.ABSTRACT | Modifiers.OVERRIDE)) == 0)
					flags |= MethodAttributes.Final;

				//
				// clear the pending implementation flag (requires explicit methods to be defined first)
				//
				pending.ImplementMethod (method.MethodName,
					member.InterfaceType, this, member.IsExplicitImpl, out ambig_iface_method, ref optional);

				//
				// Update indexer accessor name to match implementing abstract accessor
				//
				if (!implementing.DeclaringType.IsInterface && !member.IsExplicitImpl && implementing.IsAccessor)
					method_full_name = implementing.MemberDefinition.Name;
			}

			DefineMethodBuilder (container, method_full_name, method.ParameterInfo);

			if (builder == null)
				return false;

//			if (container.CurrentType != null)
//				declaring_type = container.CurrentType;
//			else
				declaring_type = container.Definition;

			if (implementing != null && member.IsExplicitImpl) {
				container.TypeBuilder.DefineMethodOverride (builder, (MethodInfo) implementing.GetMetaInfo ());
			}

			return true;
		}


		/// <summary>
		/// Create the MethodBuilder for the method 
		/// </summary>
		void DefineMethodBuilder (TypeDefinition container, string method_name, ParametersCompiled param)
		{
			var return_type = method.ReturnType.GetMetaInfo ();
			var p_types = param.GetMetaInfo ();

			if (builder == null) {
				builder = container.TypeBuilder.DefineMethod (
					method_name, flags, method.CallingConventions,
					return_type, p_types);
				return;
			}

			//
			// Generic method has been already defined to resolve method parameters
			// correctly when they use type parameters
			//
			builder.SetParameters (p_types);
			builder.SetReturnType (return_type);
			if (builder.Attributes != flags) {
#if STATIC
				builder.__SetAttributes (flags);
#else
				try {
					if (methodbuilder_attrs_field == null)
						methodbuilder_attrs_field = typeof (MethodBuilder).GetField ("attrs", BindingFlags.NonPublic | BindingFlags.Instance);
					methodbuilder_attrs_field.SetValue (builder, flags);
				} catch {
					container.Compiler.Report.RuntimeMissingSupport (method.Location, "Generic method MethodAttributes");
				}
#endif
			}
		}

		//
		// Emits the code
		// 
		public void Emit (TypeDefinition parent)
		{
			var mc = (IMemberContext) method;

			method.ParameterInfo.ApplyAttributes (mc, MethodBuilder);

			ToplevelBlock block = method.Block;
			if (block != null) {
				BlockContext bc = new BlockContext (mc, block, method.ReturnType);
				if (block.Resolve (null, bc, method)) {
					EmitContext ec = method.CreateEmitContext (MethodBuilder.GetILGenerator ());

					SourceMethod source = SourceMethod.Create (parent, MethodBuilder);

					block.Emit (ec);

					if (source != null)
						source.CloseMethod ();
				}
			}
		}
	}

	public class Destructor : MethodOrOperator
	{
		const Modifiers AllowedModifiers =
			Modifiers.UNSAFE |
			Modifiers.EXTERN;

		static readonly string[] attribute_targets = new string [] { "method" };

		public static readonly string MetadataName = "Finalize";

		public Destructor (TypeDefinition parent, Modifiers mod, ParametersCompiled parameters, Attributes attrs, Location l)
			: base (parent, null, mod, AllowedModifiers, new MemberName (MetadataName, l), attrs, parameters)
		{
			ModFlags &= ~Modifiers.PRIVATE;
			ModFlags |= Modifiers.PROTECTED | Modifiers.OVERRIDE;
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Type == pa.Conditional) {
				Error_ConditionalAttributeIsNotValid ();
				return;
			}

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}
		
		protected override bool CheckBase ()
		{
			// Don't check base, destructors have special syntax
			return true;
		}

		public override void Emit()
		{
			var base_type = Parent.PartialContainer.BaseType;
			if (base_type != null && Block != null) {
				var base_dtor = MemberCache.FindMember (base_type,
					new MemberFilter (MetadataName, 0, MemberKind.Destructor, null, null), BindingRestriction.InstanceOnly) as MethodSpec;

				if (base_dtor == null)
					throw new NotImplementedException ();

				MethodGroupExpr method_expr = MethodGroupExpr.CreatePredefined (base_dtor, base_type, Location);
				method_expr.InstanceExpression = new BaseThis (base_type, Location);

				var try_block = new ExplicitBlock (block, block.StartLocation, block.EndLocation) {
					IsCompilerGenerated = true
				};
				var finaly_block = new ExplicitBlock (block, Location, Location) {
					IsCompilerGenerated = true
				};

				//
				// 0-size arguments to avoid CS0250 error
				// TODO: Should use AddScopeStatement or something else which emits correct
				// debugger scope
				//
				finaly_block.AddStatement (new StatementExpression (new Invocation (method_expr, new Arguments (0)), Location.Null));

				var tf = new TryFinally (try_block, finaly_block, Location);
				block.WrapIntoDestructor (tf, try_block);
			}

			base.Emit ();
		}

		public override string GetSignatureForError ()
		{
			return Parent.GetSignatureForError () + ".~" + Parent.MemberName.Name + "()";
		}

		protected override bool ResolveMemberType ()
		{
			member_type = Compiler.BuiltinTypes.Void;
			return true;
		}

		public override string[] ValidAttributeTargets {
			get {
				return attribute_targets;
			}
		}
	}

	// Ooouh Martin, templates are missing here.
	// When it will be possible move here a lot of child code and template method type.
	public abstract class AbstractPropertyEventMethod : MemberCore, IMethodData {
		protected MethodData method_data;
		protected ToplevelBlock block;
		protected SecurityType declarative_security;

		protected readonly string prefix;

		ReturnParameter return_attributes;

		public AbstractPropertyEventMethod (InterfaceMemberBase member, string prefix, Attributes attrs, Location loc)
			: base (member.Parent, SetupName (prefix, member, loc), attrs)
		{
			this.prefix = prefix;
		}

		static MemberName SetupName (string prefix, InterfaceMemberBase member, Location loc)
		{
			return new MemberName (member.MemberName.Left, prefix + member.ShortName, member.MemberName.ExplicitInterface, loc);
		}

		public void UpdateName (InterfaceMemberBase member)
		{
			SetMemberName (SetupName (prefix, member, Location));
		}

		#region IMethodData Members

		public ToplevelBlock Block {
			get {
				return block;
			}

			set {
				block = value;
			}
		}

		public CallingConventions CallingConventions {
			get {
				return CallingConventions.Standard;
			}
		}

		public EmitContext CreateEmitContext (ILGenerator ig)
		{
			return new EmitContext (this, ig, ReturnType);
		}

		public bool IsAccessor {
			get {
				return true;
			}
		}

		public bool IsExcluded ()
		{
			return false;
		}

		public MemberName MethodName {
			get {
				return MemberName;
			}
		}

		public TypeSpec[] ParameterTypes { 
			get {
				return ParameterInfo.Types;
			}
		}

		public abstract ParametersCompiled ParameterInfo { get ; }
		public abstract TypeSpec ReturnType { get; }

		#endregion

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Type == pa.CLSCompliant || a.Type == pa.Obsolete || a.Type == pa.Conditional) {
				Report.Error (1667, a.Location,
					"Attribute `{0}' is not valid on property or event accessors. It is valid on `{1}' declarations only",
					TypeManager.CSharpName (a.Type), a.GetValidTargets ());
				return;
			}

			if (a.IsValidSecurityAttribute ()) {
				a.ExtractSecurityPermissionSet (ctor, ref declarative_security);
				return;
			}

			if (a.Target == AttributeTargets.Method) {
				method_data.MethodBuilder.SetCustomAttribute ((ConstructorInfo) ctor.GetMetaInfo (), cdata);
				return;
			}

			if (a.Target == AttributeTargets.ReturnValue) {
				if (return_attributes == null)
					return_attributes = new ReturnParameter (this, method_data.MethodBuilder, Location);

				return_attributes.ApplyAttributeBuilder (a, ctor, cdata, pa);
				return;
			}

			ApplyToExtraTarget (a, ctor, cdata, pa);
		}

		protected virtual void ApplyToExtraTarget (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			throw new NotSupportedException ("You forgot to define special attribute target handling");
		}

		// It is not supported for the accessors
		public sealed override bool Define()
		{
			throw new NotSupportedException ();
		}

		public virtual void Emit (TypeDefinition parent)
		{
			method_data.Emit (parent);

			if ((ModFlags & Modifiers.COMPILER_GENERATED) != 0 && !Parent.IsCompilerGenerated)
				Module.PredefinedAttributes.CompilerGenerated.EmitAttribute (method_data.MethodBuilder);
			if (((ModFlags & Modifiers.DEBUGGER_HIDDEN) != 0))
				Module.PredefinedAttributes.DebuggerHidden.EmitAttribute (method_data.MethodBuilder);

			if (ReturnType.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				return_attributes = new ReturnParameter (this, method_data.MethodBuilder, Location);
				Module.PredefinedAttributes.Dynamic.EmitAttribute (return_attributes.Builder);
			} else if (ReturnType.HasDynamicElement) {
				return_attributes = new ReturnParameter (this, method_data.MethodBuilder, Location);
				Module.PredefinedAttributes.Dynamic.EmitAttribute (return_attributes.Builder, ReturnType, Location);
			}

			if (OptAttributes != null)
				OptAttributes.Emit ();

			if (declarative_security != null) {
				foreach (var de in declarative_security) {
#if STATIC
					method_data.MethodBuilder.__AddDeclarativeSecurity (de);
#else
					method_data.MethodBuilder.AddDeclarativeSecurity (de.Key, de.Value);
#endif
				}
			}

			block = null;
		}

		public override bool EnableOverloadChecks (MemberCore overload)
		{
			if (overload is MethodCore) {
				caching_flags |= Flags.MethodOverloadsExist;
				return true;
			}

			// This can only happen with indexers and it will
			// be catched as indexer difference
			if (overload is AbstractPropertyEventMethod)
				return true;

			return false;
		}

		public override string GetSignatureForDocumentation ()
		{
			// should not be called
			throw new NotSupportedException ();
		}

		public override bool IsClsComplianceRequired()
		{
			return false;
		}

		public MethodSpec Spec { get; protected set; }

		//
		//   Represents header string for documentation comment.
		//
		public override string DocCommentHeader {
			get { throw new InvalidOperationException ("Unexpected attempt to get doc comment from " + this.GetType () + "."); }
		}
	}

	public class Operator : MethodOrOperator {

		const Modifiers AllowedModifiers =
			Modifiers.PUBLIC |
			Modifiers.UNSAFE |
			Modifiers.EXTERN |
			Modifiers.STATIC;

		public enum OpType : byte {

			// Unary operators
			LogicalNot,
			OnesComplement,
			Increment,
			Decrement,
			True,
			False,

			// Unary and Binary operators
			Addition,
			Subtraction,

			UnaryPlus,
			UnaryNegation,
			
			// Binary operators
			Multiply,
			Division,
			Modulus,
			BitwiseAnd,
			BitwiseOr,
			ExclusiveOr,
			LeftShift,
			RightShift,
			Equality,
			Inequality,
			GreaterThan,
			LessThan,
			GreaterThanOrEqual,
			LessThanOrEqual,

			// Implicit and Explicit
			Implicit,
			Explicit,

			// Just because of enum
			TOP
		};

		public readonly OpType OperatorType;

		static readonly string [] [] names;

		static Operator ()
		{
			names = new string[(int)OpType.TOP][];
			names [(int) OpType.LogicalNot] = new string [] { "!", "op_LogicalNot" };
			names [(int) OpType.OnesComplement] = new string [] { "~", "op_OnesComplement" };
			names [(int) OpType.Increment] = new string [] { "++", "op_Increment" };
			names [(int) OpType.Decrement] = new string [] { "--", "op_Decrement" };
			names [(int) OpType.True] = new string [] { "true", "op_True" };
			names [(int) OpType.False] = new string [] { "false", "op_False" };
			names [(int) OpType.Addition] = new string [] { "+", "op_Addition" };
			names [(int) OpType.Subtraction] = new string [] { "-", "op_Subtraction" };
			names [(int) OpType.UnaryPlus] = new string [] { "+", "op_UnaryPlus" };
			names [(int) OpType.UnaryNegation] = new string [] { "-", "op_UnaryNegation" };
			names [(int) OpType.Multiply] = new string [] { "*", "op_Multiply" };
			names [(int) OpType.Division] = new string [] { "/", "op_Division" };
			names [(int) OpType.Modulus] = new string [] { "%", "op_Modulus" };
			names [(int) OpType.BitwiseAnd] = new string [] { "&", "op_BitwiseAnd" };
			names [(int) OpType.BitwiseOr] = new string [] { "|", "op_BitwiseOr" };
			names [(int) OpType.ExclusiveOr] = new string [] { "^", "op_ExclusiveOr" };
			names [(int) OpType.LeftShift] = new string [] { "<<", "op_LeftShift" };
			names [(int) OpType.RightShift] = new string [] { ">>", "op_RightShift" };
			names [(int) OpType.Equality] = new string [] { "==", "op_Equality" };
			names [(int) OpType.Inequality] = new string [] { "!=", "op_Inequality" };
			names [(int) OpType.GreaterThan] = new string [] { ">", "op_GreaterThan" };
			names [(int) OpType.LessThan] = new string [] { "<", "op_LessThan" };
			names [(int) OpType.GreaterThanOrEqual] = new string [] { ">=", "op_GreaterThanOrEqual" };
			names [(int) OpType.LessThanOrEqual] = new string [] { "<=", "op_LessThanOrEqual" };
			names [(int) OpType.Implicit] = new string [] { "implicit", "op_Implicit" };
			names [(int) OpType.Explicit] = new string [] { "explicit", "op_Explicit" };
		}

		public Operator (TypeDefinition parent, OpType type, FullNamedExpression ret_type, Modifiers mod_flags, ParametersCompiled parameters,
				 ToplevelBlock block, Attributes attrs, Location loc)
			: base (parent, ret_type, mod_flags, AllowedModifiers, new MemberName (GetMetadataName (type), loc), attrs, parameters)
		{
			OperatorType = type;
			Block = block;
		}

		public override void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		public override void ApplyAttributeBuilder (Attribute a, MethodSpec ctor, byte[] cdata, PredefinedAttributes pa)
		{
			if (a.Type == pa.Conditional) {
				Error_ConditionalAttributeIsNotValid ();
				return;
			}

			base.ApplyAttributeBuilder (a, ctor, cdata, pa);
		}
		
		public override bool Define ()
		{
			const Modifiers RequiredModifiers = Modifiers.PUBLIC | Modifiers.STATIC;
			if ((ModFlags & RequiredModifiers) != RequiredModifiers){
				Report.Error (558, Location, "User-defined operator `{0}' must be declared static and public", GetSignatureForError ());
			}

			if (!base.Define ())
				return false;

			if (block != null && block.IsIterator) {
				//
				// Current method is turned into automatically generated
				// wrapper which creates an instance of iterator
				//
				Iterator.CreateIterator (this, Parent.PartialContainer, ModFlags);
				ModFlags |= Modifiers.DEBUGGER_HIDDEN;
			}

			// imlicit and explicit operator of same types are not allowed
			if (OperatorType == OpType.Explicit)
				Parent.MemberCache.CheckExistingMembersOverloads (this, GetMetadataName (OpType.Implicit), parameters);
			else if (OperatorType == OpType.Implicit)
				Parent.MemberCache.CheckExistingMembersOverloads (this, GetMetadataName (OpType.Explicit), parameters);

			TypeSpec declaring_type = Parent.CurrentType;
			TypeSpec return_type = MemberType;
			TypeSpec first_arg_type = ParameterTypes [0];
			
			TypeSpec first_arg_type_unwrap = first_arg_type;
			if (first_arg_type.IsNullableType)
				first_arg_type_unwrap = Nullable.NullableInfo.GetUnderlyingType (first_arg_type);
			
			TypeSpec return_type_unwrap = return_type;
			if (return_type.IsNullableType)
				return_type_unwrap = Nullable.NullableInfo.GetUnderlyingType (return_type);

			//
			// Rules for conversion operators
			//
			if (OperatorType == OpType.Implicit || OperatorType == OpType.Explicit) {
				if (first_arg_type_unwrap == return_type_unwrap && first_arg_type_unwrap == declaring_type) {
					Report.Error (555, Location,
						"User-defined operator cannot take an object of the enclosing type and convert to an object of the enclosing type");
					return false;
				}

				TypeSpec conv_type;
				if (declaring_type == return_type || declaring_type == return_type_unwrap) {
					conv_type = first_arg_type;
				} else if (declaring_type == first_arg_type || declaring_type == first_arg_type_unwrap) {
					conv_type = return_type;
				} else {
					Report.Error (556, Location,
						"User-defined conversion must convert to or from the enclosing type");
					return false;
				}

				if (conv_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
					Report.Error (1964, Location,
						"User-defined conversion `{0}' cannot convert to or from the dynamic type",
						GetSignatureForError ());

					return false;
				}

				if (conv_type.IsInterface) {
					Report.Error (552, Location, "User-defined conversion `{0}' cannot convert to or from an interface type",
						GetSignatureForError ());
					return false;
				}

				if (conv_type.IsClass) {
					if (TypeSpec.IsBaseClass (declaring_type, conv_type, true)) {
						Report.Error (553, Location, "User-defined conversion `{0}' cannot convert to or from a base class",
							GetSignatureForError ());
						return false;
					}

					if (TypeSpec.IsBaseClass (conv_type, declaring_type, false)) {
						Report.Error (554, Location, "User-defined conversion `{0}' cannot convert to or from a derived class",
							GetSignatureForError ());
						return false;
					}
				}
			} else if (OperatorType == OpType.LeftShift || OperatorType == OpType.RightShift) {
				if (first_arg_type != declaring_type || parameters.Types[1].BuiltinType != BuiltinTypeSpec.Type.Int) {
					Report.Error (564, Location, "Overloaded shift operator must have the type of the first operand be the containing type, and the type of the second operand must be int");
					return false;
				}
			} else if (parameters.Count == 1) {
				// Checks for Unary operators

				if (OperatorType == OpType.Increment || OperatorType == OpType.Decrement) {
					if (return_type != declaring_type && !TypeSpec.IsBaseClass (return_type, declaring_type, false)) {
						Report.Error (448, Location,
							"The return type for ++ or -- operator must be the containing type or derived from the containing type");
						return false;
					}
					if (first_arg_type != declaring_type) {
						Report.Error (
							559, Location, "The parameter type for ++ or -- operator must be the containing type");
						return false;
					}
				}

				if (first_arg_type_unwrap != declaring_type) {
					Report.Error (562, Location,
						"The parameter type of a unary operator must be the containing type");
					return false;
				}

				if (OperatorType == OpType.True || OperatorType == OpType.False) {
					if (return_type.BuiltinType != BuiltinTypeSpec.Type.Bool) {
						Report.Error (
							215, Location,
							"The return type of operator True or False " +
							"must be bool");
						return false;
					}
				}

			} else if (first_arg_type_unwrap != declaring_type) {
				// Checks for Binary operators

				var second_arg_type = ParameterTypes[1];
				if (second_arg_type.IsNullableType)
					second_arg_type = Nullable.NullableInfo.GetUnderlyingType (second_arg_type);

				if (second_arg_type != declaring_type) {
					Report.Error (563, Location,
						"One of the parameters of a binary operator must be the containing type");
					return false;
				}
			}

			return true;
		}

		protected override bool ResolveMemberType ()
		{
			if (!base.ResolveMemberType ())
				return false;

			flags |= MethodAttributes.SpecialName | MethodAttributes.HideBySig;
			return true;
		}

		protected override MemberSpec FindBaseMember (out MemberSpec bestCandidate, ref bool overrides)
		{
			// Operator cannot be override
			bestCandidate = null;
			return null;
		}

		public static string GetName (OpType ot)
		{
			return names [(int) ot] [0];
		}

		public static string GetName (string metadata_name)
		{
			for (int i = 0; i < names.Length; ++i) {
				if (names [i] [1] == metadata_name)
					return names [i] [0];
			}
			return null;
		}

		public static string GetMetadataName (OpType ot)
		{
			return names [(int) ot] [1];
		}

		public static string GetMetadataName (string name)
		{
			for (int i = 0; i < names.Length; ++i) {
				if (names [i] [0] == name)
					return names [i] [1];
			}
			return null;
		}

		public static OpType? GetType (string metadata_name)
		{
			for (int i = 0; i < names.Length; ++i) {
				if (names[i][1] == metadata_name)
					return (OpType) i;
			}

			return null;
		}

		public OpType GetMatchingOperator ()
		{
			switch (OperatorType) {
			case OpType.Equality:
				return OpType.Inequality;
			case OpType.Inequality:
				return OpType.Equality;
			case OpType.True:
				return OpType.False;
			case OpType.False:
				return OpType.True;
			case OpType.GreaterThan:
				return OpType.LessThan;
			case OpType.LessThan:
				return OpType.GreaterThan;
			case OpType.GreaterThanOrEqual:
				return OpType.LessThanOrEqual;
			case OpType.LessThanOrEqual:
				return OpType.GreaterThanOrEqual;
			default:
				return OpType.TOP;
			}
		}

		public override string GetSignatureForDocumentation ()
		{
			string s = base.GetSignatureForDocumentation ();
			if (OperatorType == OpType.Implicit || OperatorType == OpType.Explicit) {
				s = s + "~" + ReturnType.GetSignatureForDocumentation ();
			}

			return s;
		}

		public override string GetSignatureForError ()
		{
			StringBuilder sb = new StringBuilder ();
			if (OperatorType == OpType.Implicit || OperatorType == OpType.Explicit) {
				sb.AppendFormat ("{0}.{1} operator {2}",
					Parent.GetSignatureForError (), GetName (OperatorType),
					member_type == null ? type_expr.GetSignatureForError () : member_type.GetSignatureForError ());
			}
			else {
				sb.AppendFormat ("{0}.operator {1}", Parent.GetSignatureForError (), GetName (OperatorType));
			}

			sb.Append (parameters.GetSignatureForError ());
			return sb.ToString ();
		}
	}
}

