//
// decl.cs: Declaration base class for structs, classes, enums and interfaces.
//
// Author: Miguel de Icaza (miguel@gnu.org)
//         Marek Safar (marek.safar@seznam.cz)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
// Copyright 2011 Xamarin Inc
//
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Mono.CompilerServices.SymbolWriter;

#if NET_2_1
using XmlElement = System.Object;
#else
using System.Xml;
#endif

#if STATIC
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {

	//
	// Better name would be DottenName
	//
	[DebuggerDisplay ("{GetSignatureForError()}")]
	public class MemberName
	{
		public static readonly MemberName Null = new MemberName ("");

		public readonly string Name;
		public TypeParameters TypeParameters;
		public readonly FullNamedExpression ExplicitInterface;
		public readonly Location Location;

		public readonly MemberName Left;

		public MemberName (string name)
			: this (name, Location.Null)
		{ }

		public MemberName (string name, Location loc)
			: this (null, name, loc)
		{ }

		public MemberName (string name, TypeParameters tparams, Location loc)
		{
			this.Name = name;
			this.Location = loc;

			this.TypeParameters = tparams;
		}

		public MemberName (string name, TypeParameters tparams, FullNamedExpression explicitInterface, Location loc)
			: this (name, tparams, loc)
		{
			this.ExplicitInterface = explicitInterface;
		}

		public MemberName (MemberName left, string name, Location loc)
		{
			this.Name = name;
			this.Location = loc;
			this.Left = left;
		}

		public MemberName (MemberName left, string name, FullNamedExpression explicitInterface, Location loc)
			: this (left, name, loc)
		{
			this.ExplicitInterface = explicitInterface;
		}

		public MemberName (MemberName left, MemberName right)
		{
			this.Name = right.Name;
			this.Location = right.Location;
			this.TypeParameters = right.TypeParameters;
			this.Left = left;
		}

		public int Arity {
			get {
				return TypeParameters == null ? 0 : TypeParameters.Count;
			}
		}

		public bool IsGeneric {
			get {
				return TypeParameters != null;
			}
		}

		public string Basename {
			get {
				if (TypeParameters != null)
					return MakeName (Name, TypeParameters);
				return Name;
			}
		}

		public void CreateMetadataName (StringBuilder sb)
		{
			if (Left != null)
				Left.CreateMetadataName (sb);

			if (sb.Length != 0) {
				sb.Append (".");
			}

			sb.Append (Basename);
		}

		public string GetSignatureForDocumentation ()
		{
			var s = Basename;

			if (ExplicitInterface != null)
				s = ExplicitInterface.GetSignatureForError () + "." + s;

			if (Left == null)
				return s;

			return Left.GetSignatureForDocumentation () + "." + s;
		}

		public string GetSignatureForError ()
		{
			string s = TypeParameters == null ? null : "<" + TypeParameters.GetSignatureForError () + ">";
			s = Name + s;

			if (ExplicitInterface != null)
				s = ExplicitInterface.GetSignatureForError () + "." + s;

			if (Left == null)
				return s;

			return Left.GetSignatureForError () + "." + s;
		}

		public override bool Equals (object other)
		{
			return Equals (other as MemberName);
		}

		public bool Equals (MemberName other)
		{
			if (this == other)
				return true;
			if (other == null || Name != other.Name)
				return false;

			if ((TypeParameters != null) &&
			    (other.TypeParameters == null || TypeParameters.Count != other.TypeParameters.Count))
				return false;

			if ((TypeParameters == null) && (other.TypeParameters != null))
				return false;

			if (Left == null)
				return other.Left == null;

			return Left.Equals (other.Left);
		}

		public override int GetHashCode ()
		{
			int hash = Name.GetHashCode ();
			for (MemberName n = Left; n != null; n = n.Left)
				hash ^= n.Name.GetHashCode ();

			if (TypeParameters != null)
				hash ^= TypeParameters.Count << 5;

			return hash & 0x7FFFFFFF;
		}

		public static string MakeName (string name, TypeParameters args)
		{
			if (args == null)
				return name;

			return name + "`" + args.Count;
		}

		public static string MakeName (string name, int count)
		{
			return name + "`" + count;
		}
	}

	public class SimpleMemberName
	{
		public string Value;
		public Location Location;

		public SimpleMemberName (string name, Location loc)
		{
			this.Value = name;
			this.Location = loc;
		}
	}

	/// <summary>
	///   Base representation for members.  This is used to keep track
	///   of Name, Location and Modifier flags, and handling Attributes.
	/// </summary>
	[System.Diagnostics.DebuggerDisplay ("{GetSignatureForError()}")]
	public abstract class MemberCore : Attributable, IMemberContext, IMemberDefinition
	{
		string IMemberDefinition.Name {
			get {
				return member_name.Name;
			}
		}

                // Is not readonly because of IndexerName attribute
		private MemberName member_name;
		public MemberName MemberName {
			get { return member_name; }
		}

		/// <summary>
		///   Modifier flags that the user specified in the source code
		/// </summary>
		private Modifiers mod_flags;
		public Modifiers ModFlags {
			set {
				mod_flags = value;
				if ((value & Modifiers.COMPILER_GENERATED) != 0)
					caching_flags = Flags.IsUsed | Flags.IsAssigned;
			}
			get {
				return mod_flags;
			}
		}

		public virtual ModuleContainer Module {
			get {
				return Parent.Module;
			}
		}

		public /*readonly*/ TypeContainer Parent;

		/// <summary>
		///   Location where this declaration happens
		/// </summary>
		public Location Location {
			get { return member_name.Location; }
		}

		/// <summary>
		///   XML documentation comment
		/// </summary>
		protected string comment;

		/// <summary>
		///   Represents header string for documentation comment 
		///   for each member types.
		/// </summary>
		public abstract string DocCommentHeader { get; }

		[Flags]
		public enum Flags {
			Obsolete_Undetected = 1,		// Obsolete attribute has not been detected yet
			Obsolete = 1 << 1,			// Type has obsolete attribute
			ClsCompliance_Undetected = 1 << 2,	// CLS Compliance has not been detected yet
			ClsCompliant = 1 << 3,			// Type is CLS Compliant
			CloseTypeCreated = 1 << 4,		// Tracks whether we have Closed the type
			HasCompliantAttribute_Undetected = 1 << 5,	// Presence of CLSCompliantAttribute has not been detected
			HasClsCompliantAttribute = 1 << 6,			// Type has CLSCompliantAttribute
			ClsCompliantAttributeFalse = 1 << 7,			// Member has CLSCompliant(false)
			Excluded_Undetected = 1 << 8,		// Conditional attribute has not been detected yet
			Excluded = 1 << 9,					// Method is conditional
			MethodOverloadsExist = 1 << 10,		// Test for duplication must be performed
			IsUsed = 1 << 11,
			IsAssigned = 1 << 12,				// Field is assigned
			HasExplicitLayout	= 1 << 13,
			PartialDefinitionExists	= 1 << 14,	// Set when corresponding partial method definition exists
			HasStructLayout	= 1 << 15,			// Has StructLayoutAttribute
			HasInstanceConstructor = 1 << 16,
			HasUserOperators = 1 << 17,
			CanBeReused = 1 << 18,
			InterfacesExpanded = 1 << 19
		}

		/// <summary>
		///   MemberCore flags at first detected then cached
		/// </summary>
		internal Flags caching_flags;

		protected MemberCore (TypeContainer parent, MemberName name, Attributes attrs)
		{
			this.Parent = parent;
			member_name = name;
			caching_flags = Flags.Obsolete_Undetected | Flags.ClsCompliance_Undetected | Flags.HasCompliantAttribute_Undetected | Flags.Excluded_Undetected;
			AddAttributes (attrs, this);
		}

		protected virtual void SetMemberName (MemberName new_name)
		{
			member_name = new_name;
		}

		public virtual void Accept (StructuralVisitor visitor)
		{
			visitor.Visit (this);
		}

		protected bool CheckAbstractAndExtern (bool has_block)
		{
			if (Parent.PartialContainer.Kind == MemberKind.Interface)
				return true;

			if (has_block) {
				if ((ModFlags & Modifiers.EXTERN) != 0) {
					Report.Error (179, Location, "`{0}' cannot declare a body because it is marked extern",
						GetSignatureForError ());
					return false;
				}

				if ((ModFlags & Modifiers.ABSTRACT) != 0) {
					Report.Error (500, Location, "`{0}' cannot declare a body because it is marked abstract",
						GetSignatureForError ());
					return false;
				}
			} else {
				if ((ModFlags & (Modifiers.ABSTRACT | Modifiers.EXTERN | Modifiers.PARTIAL)) == 0 && !(Parent is Delegate)) {
					if (Compiler.Settings.Version >= LanguageVersion.V_3) {
						Property.PropertyMethod pm = this as Property.PropertyMethod;
						if (pm is Indexer.GetIndexerMethod || pm is Indexer.SetIndexerMethod)
							pm = null;

						if (pm != null && pm.Property.AccessorSecond == null) {
							Report.Error (840, Location,
								"`{0}' must have a body because it is not marked abstract or extern. The property can be automatically implemented when you define both accessors",
								GetSignatureForError ());
							return false;
						}
					}

					Report.Error (501, Location, "`{0}' must have a body because it is not marked abstract, extern, or partial",
					              GetSignatureForError ());
					return false;
				}
			}

			return true;
		}

		protected void CheckProtectedModifier ()
		{
			if ((ModFlags & Modifiers.PROTECTED) == 0)
				return;

			if (Parent.PartialContainer.Kind == MemberKind.Struct) {
				Report.Error (666, Location, "`{0}': Structs cannot contain protected members",
					GetSignatureForError ());
				return;
			}

			if ((Parent.ModFlags & Modifiers.STATIC) != 0) {
				Report.Error (1057, Location, "`{0}': Static classes cannot contain protected members",
					GetSignatureForError ());
				return;
			}

			if ((Parent.ModFlags & Modifiers.SEALED) != 0 && (ModFlags & Modifiers.OVERRIDE) == 0 &&
				!(this is Destructor)) {
				Report.Warning (628, 4, Location, "`{0}': new protected member declared in sealed class",
					GetSignatureForError ());
				return;
			}
		}

		public abstract bool Define ();

		public virtual string DocComment {
			get {
				return comment;
			}
			set {
				comment = value;
			}
		}

		// 
		// Returns full member name for error message
		//
		public virtual string GetSignatureForError ()
		{
			var parent = Parent.GetSignatureForError ();
			if (parent == null)
				return member_name.GetSignatureForError ();

			return parent + "." + member_name.GetSignatureForError ();
		}

		/// <summary>
		/// Base Emit method. This is also entry point for CLS-Compliant verification.
		/// </summary>
		public virtual void Emit ()
		{
			if (!Compiler.Settings.VerifyClsCompliance)
				return;

			VerifyClsCompliance ();
		}

		public bool IsAvailableForReuse {
			get {
				return (caching_flags & Flags.CanBeReused) != 0;
			}
			set {
				caching_flags = value ? (caching_flags | Flags.CanBeReused) : (caching_flags & ~Flags.CanBeReused);
			}
		}

		public bool IsCompilerGenerated {
			get	{
				if ((mod_flags & Modifiers.COMPILER_GENERATED) != 0)
					return true;

				return Parent != null && Parent.IsCompilerGenerated;
			}
		}

		public bool IsImported {
			get {
				return false;
			}
		}

		public virtual bool IsUsed {
			get {
				return (caching_flags & Flags.IsUsed) != 0;
			}
		}

		protected Report Report {
			get {
				return Compiler.Report;
			}
		}

		public void SetIsUsed ()
		{
			caching_flags |= Flags.IsUsed;
		}

		public void SetIsAssigned ()
		{
			caching_flags |= Flags.IsAssigned;
		}

		public virtual void SetConstraints (List<Constraints> constraints_list)
		{
			var tparams = member_name.TypeParameters;
			if (tparams == null) {
				Report.Error (80, Location, "Constraints are not allowed on non-generic declarations");
				return;
			}

			foreach (var c in constraints_list) {
				var tp = tparams.Find (c.TypeParameter.Value);
				if (tp == null) {
					Report.Error (699, c.Location, "`{0}': A constraint references nonexistent type parameter `{1}'",
						GetSignatureForError (), c.TypeParameter.Value);
					continue;
				}

				tp.Constraints = c;
			}
		}

		/// <summary>
		/// Returns instance of ObsoleteAttribute for this MemberCore
		/// </summary>
		public virtual ObsoleteAttribute GetAttributeObsolete ()
		{
			if ((caching_flags & (Flags.Obsolete_Undetected | Flags.Obsolete)) == 0)
				return null;

			caching_flags &= ~Flags.Obsolete_Undetected;

			if (OptAttributes == null)
				return null;

			Attribute obsolete_attr = OptAttributes.Search (Module.PredefinedAttributes.Obsolete);
			if (obsolete_attr == null)
				return null;

			caching_flags |= Flags.Obsolete;

			ObsoleteAttribute obsolete = obsolete_attr.GetObsoleteAttribute ();
			if (obsolete == null)
				return null;

			return obsolete;
		}

		/// <summary>
		/// Checks for ObsoleteAttribute presence. It's used for testing of all non-types elements
		/// </summary>
		public virtual void CheckObsoleteness (Location loc)
		{
			ObsoleteAttribute oa = GetAttributeObsolete ();
			if (oa != null)
				AttributeTester.Report_ObsoleteMessage (oa, GetSignatureForError (), loc, Report);
		}

		//
		// Checks whether the type P is as accessible as this member
		//
		public bool IsAccessibleAs (TypeSpec p)
		{
			//
			// if M is private, its accessibility is the same as this declspace.
			// we already know that P is accessible to T before this method, so we
			// may return true.
			//
			if ((mod_flags & Modifiers.PRIVATE) != 0)
				return true;

			while (TypeManager.HasElementType (p))
				p = TypeManager.GetElementType (p);

			if (p.IsGenericParameter)
				return true;

			for (TypeSpec p_parent; p != null; p = p_parent) {
				p_parent = p.DeclaringType;

				if (p.IsGeneric) {
					foreach (TypeSpec t in p.TypeArguments) {
						if (!IsAccessibleAs (t))
							return false;
					}
				}

				var pAccess = p.Modifiers & Modifiers.AccessibilityMask;
				if (pAccess == Modifiers.PUBLIC)
					continue;

				bool same_access_restrictions = false;
				for (MemberCore mc = this; !same_access_restrictions && mc != null && mc.Parent != null; mc = mc.Parent) {
					var al = mc.ModFlags & Modifiers.AccessibilityMask;
					switch (pAccess) {
					case Modifiers.INTERNAL:
						if (al == Modifiers.PRIVATE || al == Modifiers.INTERNAL)
							same_access_restrictions = p.MemberDefinition.IsInternalAsPublic (mc.Module.DeclaringAssembly);
						
						break;

					case Modifiers.PROTECTED:
						if (al == Modifiers.PROTECTED) {
							same_access_restrictions = mc.Parent.PartialContainer.IsBaseTypeDefinition (p_parent);
							break;
						}

						if (al == Modifiers.PRIVATE) {
							//
							// When type is private and any of its parents derives from
							// protected type then the type is accessible
							//
							while (mc.Parent != null && mc.Parent.PartialContainer != null) {
								if (mc.Parent.PartialContainer.IsBaseTypeDefinition (p_parent))
									same_access_restrictions = true;
								mc = mc.Parent; 
							}
						}
						
						break;

					case Modifiers.PROTECTED | Modifiers.INTERNAL:
						if (al == Modifiers.INTERNAL)
							same_access_restrictions = p.MemberDefinition.IsInternalAsPublic (mc.Module.DeclaringAssembly);
						else if (al == (Modifiers.PROTECTED | Modifiers.INTERNAL))
							same_access_restrictions = mc.Parent.PartialContainer.IsBaseTypeDefinition (p_parent) && p.MemberDefinition.IsInternalAsPublic (mc.Module.DeclaringAssembly);
						else
							goto case Modifiers.PROTECTED;

						break;

					case Modifiers.PRIVATE:
						//
						// Both are private and share same parent
						//
						if (al == Modifiers.PRIVATE) {
							var decl = mc.Parent;
							do {
								same_access_restrictions = decl.CurrentType.MemberDefinition == p_parent.MemberDefinition;
							} while (!same_access_restrictions && !decl.PartialContainer.IsTopLevel && (decl = decl.Parent) != null);
						}
						
						break;
						
					default:
						throw new InternalErrorException (al.ToString ());
					}
				}
				
				if (!same_access_restrictions)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Analyze whether CLS-Compliant verification must be execute for this MemberCore.
		/// </summary>
		public override bool IsClsComplianceRequired ()
		{
			if ((caching_flags & Flags.ClsCompliance_Undetected) == 0)
				return (caching_flags & Flags.ClsCompliant) != 0;

			caching_flags &= ~Flags.ClsCompliance_Undetected;

			if (HasClsCompliantAttribute) {
				if ((caching_flags & Flags.ClsCompliantAttributeFalse) != 0)
					return false;

				caching_flags |= Flags.ClsCompliant;
				return true;
			}

			if (Parent.IsClsComplianceRequired ()) {
				caching_flags |= Flags.ClsCompliant;
				return true;
			}

			return false;
		}

		public virtual string[] ConditionalConditions ()
		{
			return null;
		}

		/// <summary>
		/// Returns true when MemberCore is exposed from assembly.
		/// </summary>
		public bool IsExposedFromAssembly ()
		{
			if ((ModFlags & (Modifiers.PUBLIC | Modifiers.PROTECTED)) == 0)
				return this is NamespaceContainer;
			
			var parentContainer = Parent.PartialContainer;
			while (parentContainer != null) {
				if ((parentContainer.ModFlags & (Modifiers.PUBLIC | Modifiers.PROTECTED)) == 0)
					return false;

				parentContainer = parentContainer.Parent.PartialContainer;
			}

			return true;
		}

		//
		// Does extension methods look up to find a method which matches name and extensionType.
		// Search starts from this namespace and continues hierarchically up to top level.
		//
		public ExtensionMethodCandidates LookupExtensionMethod (TypeSpec extensionType, string name, int arity)
		{
			var m = Parent;
			do {
				var ns = m as NamespaceContainer;
				if (ns != null)
					return ns.LookupExtensionMethod (this, extensionType, name, arity, 0);

				m = m.Parent;
			} while (m != null);

			return null;
		}

		public virtual FullNamedExpression LookupNamespaceAlias (string name)
		{
			return Parent.LookupNamespaceAlias (name);
		}

		public virtual FullNamedExpression LookupNamespaceOrType (string name, int arity, LookupMode mode, Location loc)
		{
			return Parent.LookupNamespaceOrType (name, arity, mode, loc);
		}

		/// <summary>
		/// Goes through class hierarchy and gets value of first found CLSCompliantAttribute.
		/// If no is attribute exists then assembly CLSCompliantAttribute is returned.
		/// </summary>
		public bool? CLSAttributeValue {
			get {
				if ((caching_flags & Flags.HasCompliantAttribute_Undetected) == 0) {
					if ((caching_flags & Flags.HasClsCompliantAttribute) == 0)
						return null;

					return (caching_flags & Flags.ClsCompliantAttributeFalse) == 0;
				}

				caching_flags &= ~Flags.HasCompliantAttribute_Undetected;

				if (OptAttributes != null) {
					Attribute cls_attribute = OptAttributes.Search (Module.PredefinedAttributes.CLSCompliant);
					if (cls_attribute != null) {
						caching_flags |= Flags.HasClsCompliantAttribute;
						if (cls_attribute.GetClsCompliantAttributeValue ())
							return true;

						caching_flags |= Flags.ClsCompliantAttributeFalse;
						return false;
					}
				}

				return null;
			}
		}

		/// <summary>
		/// Returns true if MemberCore is explicitly marked with CLSCompliantAttribute
		/// </summary>
		protected bool HasClsCompliantAttribute {
			get {
				return CLSAttributeValue.HasValue;
			}
		}

		/// <summary>
		/// Returns true when a member supports multiple overloads (methods, indexers, etc)
		/// </summary>
		public virtual bool EnableOverloadChecks (MemberCore overload)
		{
			return false;
		}

		/// <summary>
		/// The main virtual method for CLS-Compliant verifications.
		/// The method returns true if member is CLS-Compliant and false if member is not
		/// CLS-Compliant which means that CLS-Compliant tests are not necessary. A descendants override it
		/// and add their extra verifications.
		/// </summary>
		protected virtual bool VerifyClsCompliance ()
		{
			if (HasClsCompliantAttribute) {
				if (!Module.DeclaringAssembly.HasCLSCompliantAttribute) {
					Attribute a = OptAttributes.Search (Module.PredefinedAttributes.CLSCompliant);
					if ((caching_flags & Flags.ClsCompliantAttributeFalse) != 0) {
						Report.Warning (3021, 2, a.Location,
							"`{0}' does not need a CLSCompliant attribute because the assembly is not marked as CLS-compliant",
							GetSignatureForError ());
					} else {
						Report.Warning (3014, 1, a.Location,
							"`{0}' cannot be marked as CLS-compliant because the assembly is not marked as CLS-compliant",
							GetSignatureForError ());
					}
					return false;
				}

				if (!IsExposedFromAssembly ()) {
					Attribute a = OptAttributes.Search (Module.PredefinedAttributes.CLSCompliant);
					Report.Warning (3019, 2, a.Location, "CLS compliance checking will not be performed on `{0}' because it is not visible from outside this assembly", GetSignatureForError ());
					return false;
				}

				if ((caching_flags & Flags.ClsCompliantAttributeFalse) != 0) {
					if (Parent is Interface && Parent.IsClsComplianceRequired ()) {
						Report.Warning (3010, 1, Location, "`{0}': CLS-compliant interfaces must have only CLS-compliant members", GetSignatureForError ());
					} else if (Parent.Kind == MemberKind.Class && (ModFlags & Modifiers.ABSTRACT) != 0 && Parent.IsClsComplianceRequired ()) {
						Report.Warning (3011, 1, Location, "`{0}': only CLS-compliant members can be abstract", GetSignatureForError ());
					}

					return false;
				}

				if (Parent.Kind != MemberKind.Namespace && Parent.Kind != 0 && !Parent.IsClsComplianceRequired ()) {
					Attribute a = OptAttributes.Search (Module.PredefinedAttributes.CLSCompliant);
					Report.Warning (3018, 1, a.Location, "`{0}' cannot be marked as CLS-compliant because it is a member of non CLS-compliant type `{1}'",
						GetSignatureForError (), Parent.GetSignatureForError ());
					return false;
				}
			} else {
				if (!IsExposedFromAssembly ())
					return false;

				if (!Parent.IsClsComplianceRequired ())
					return false;
			}

			if (member_name.Name [0] == '_') {
				Warning_IdentifierNotCompliant ();
			}

			if (member_name.TypeParameters != null)
				member_name.TypeParameters.VerifyClsCompliance ();

			return true;
		}

		protected void Warning_IdentifierNotCompliant ()
		{
			Report.Warning (3008, 1, MemberName.Location, "Identifier `{0}' is not CLS-compliant", GetSignatureForError ());
		}

		public virtual string GetCallerMemberName ()
		{
			return MemberName.Name;
		}

		//
		// Returns a string that represents the signature for this 
		// member which should be used in XML documentation.
		//
		public abstract string GetSignatureForDocumentation ();

		public virtual void GetCompletionStartingWith (string prefix, List<string> results)
		{
			Parent.GetCompletionStartingWith (prefix, results);
		}

		//
		// Generates xml doc comments (if any), and if required,
		// handle warning report.
		//
		internal virtual void GenerateDocComment (DocumentationBuilder builder)
		{
			if (DocComment == null) {
				if (IsExposedFromAssembly ()) {
					Constructor c = this as Constructor;
					if (c == null || !c.IsDefault ())
						Report.Warning (1591, 4, Location,
							"Missing XML comment for publicly visible type or member `{0}'", GetSignatureForError ());
				}

				return;
			}

			try {
				builder.GenerateDocumentationForMember (this);
			} catch (Exception e) {
				throw new InternalErrorException (this, e);
			}
		}

		public virtual void WriteDebugSymbol (MonoSymbolFile file)
		{
		}

		#region IMemberContext Members

		public virtual CompilerContext Compiler {
			get {
				return Module.Compiler;
			}
		}

		public virtual TypeSpec CurrentType {
			get { return Parent.CurrentType; }
		}

		public MemberCore CurrentMemberDefinition {
			get { return this; }
		}

		public virtual TypeParameters CurrentTypeParameters {
			get { return null; }
		}

		public bool IsObsolete {
			get {
				if (GetAttributeObsolete () != null)
					return true;

				return Parent != null && Parent.IsObsolete;
			}
		}

		public bool IsUnsafe {
			get {
				if ((ModFlags & Modifiers.UNSAFE) != 0)
					return true;

				return Parent != null && Parent.IsUnsafe;
			}
		}

		public bool IsStatic {
			get {
				return (ModFlags & Modifiers.STATIC) != 0;
			}
		}

		#endregion
	}

	//
	// Base member specification. A member specification contains
	// member details which can alter in the context (e.g. generic instances)
	//
	public abstract class MemberSpec
	{
		[Flags]
		public enum StateFlags
		{
			Obsolete_Undetected = 1,	// Obsolete attribute has not been detected yet
			Obsolete = 1 << 1,			// Member has obsolete attribute
			CLSCompliant_Undetected = 1 << 2,	// CLSCompliant attribute has not been detected yet
			CLSCompliant = 1 << 3,		// Member is CLS Compliant
			MissingDependency_Undetected = 1 << 4,
			MissingDependency = 1 << 5,
			HasDynamicElement = 1 << 6,
			ConstraintsChecked = 1 << 7,

			IsAccessor = 1 << 9,		// Method is an accessor
			IsGeneric = 1 << 10,		// Member contains type arguments

			PendingMetaInflate = 1 << 12,
			PendingMakeMethod = 1 << 13,
			PendingMemberCacheMembers = 1 << 14,
			PendingBaseTypeInflate = 1 << 15,
			InterfacesExpanded = 1 << 16,
			IsNotCSharpCompatible = 1 << 17,
			SpecialRuntimeType = 1 << 18,
			InflatedExpressionType = 1 << 19,
			InflatedNullableType = 1 << 20,
			GenericIterateInterface = 1 << 21,
			GenericTask = 1 << 22,
			InterfacesImported = 1 << 23,
		}

		//
		// Some flags can be copied directly from other member
		//
		protected const StateFlags SharedStateFlags =
			StateFlags.CLSCompliant | StateFlags.CLSCompliant_Undetected |
			StateFlags.Obsolete | StateFlags.Obsolete_Undetected |
			StateFlags.MissingDependency | StateFlags.MissingDependency_Undetected |
			StateFlags.HasDynamicElement;

		protected Modifiers modifiers;
		public StateFlags state;
		protected IMemberDefinition definition;
		public readonly MemberKind Kind;
		protected TypeSpec declaringType;

#if DEBUG
		static int counter;
		public int ID = counter++;
#endif

		protected MemberSpec (MemberKind kind, TypeSpec declaringType, IMemberDefinition definition, Modifiers modifiers)
		{
			this.Kind = kind;
			this.declaringType = declaringType;
			this.definition = definition;
			this.modifiers = modifiers;

			if (kind == MemberKind.MissingType)
				state = StateFlags.MissingDependency;
			else
				state = StateFlags.Obsolete_Undetected | StateFlags.CLSCompliant_Undetected | StateFlags.MissingDependency_Undetected;
		}

		#region Properties

		public virtual int Arity {
			get {
				return 0;
			}
		}

		public TypeSpec DeclaringType {
			get {
				return declaringType;
			}
			set {
				declaringType = value;
			}
		}

		public IMemberDefinition MemberDefinition {
			get {
				return definition;
			}
		}

		public Modifiers Modifiers {
			get {
				return modifiers;
			}
			set {
				modifiers = value;
			}
		}
		
		public virtual string Name {
			get {
				return definition.Name;
			}
		}

		public bool IsAbstract {
			get { return (modifiers & Modifiers.ABSTRACT) != 0; }
		}

		public bool IsAccessor {
			get {
				return (state & StateFlags.IsAccessor) != 0;
			}
			set {
				state = value ? state | StateFlags.IsAccessor : state & ~StateFlags.IsAccessor;
			}
		}

		//
		// Return true when this member is a generic in C# terms
		// A nested non-generic type of generic type will return false
		//
		public bool IsGeneric {
			get {
				return (state & StateFlags.IsGeneric) != 0;
			}
			set {
				state = value ? state | StateFlags.IsGeneric : state & ~StateFlags.IsGeneric;
			}
		}

		//
		// Returns true for imported members which are not compatible with C# language
		//
		public bool IsNotCSharpCompatible {
			get {
				return (state & StateFlags.IsNotCSharpCompatible) != 0;
			}
			set {
				state = value ? state | StateFlags.IsNotCSharpCompatible : state & ~StateFlags.IsNotCSharpCompatible;
			}
		}

		public bool IsPrivate {
			get { return (modifiers & Modifiers.PRIVATE) != 0; }
		}

		public bool IsPublic {
			get { return (modifiers & Modifiers.PUBLIC) != 0; }
		}

		public bool IsStatic {
			get { 
				return (modifiers & Modifiers.STATIC) != 0;
			}
		}

		#endregion

		public virtual ObsoleteAttribute GetAttributeObsolete ()
		{
			if ((state & (StateFlags.Obsolete | StateFlags.Obsolete_Undetected)) == 0)
				return null;

			state &= ~StateFlags.Obsolete_Undetected;

			var oa = definition.GetAttributeObsolete ();
			if (oa != null)
				state |= StateFlags.Obsolete;

			return oa;
		}

		//
		// Returns a list of missing dependencies of this member. The list
		// will contain types only but it can have numerous values for members
		// like methods where both return type and all parameters are checked
		//
		public List<MissingTypeSpecReference> GetMissingDependencies ()
		{
			return GetMissingDependencies (this);
		}

		public List<MissingTypeSpecReference> GetMissingDependencies (MemberSpec caller)
		{
			if ((state & (StateFlags.MissingDependency | StateFlags.MissingDependency_Undetected)) == 0)
				return null;

			state &= ~StateFlags.MissingDependency_Undetected;

			var imported = definition as ImportedDefinition;
			List<MissingTypeSpecReference> missing;
			if (imported != null) {
				missing = ResolveMissingDependencies (caller);
			} else if (this is ElementTypeSpec) {
				missing = ((ElementTypeSpec) this).Element.GetMissingDependencies (caller);
			} else {
				missing = null;
			}

			if (missing != null) {
				state |= StateFlags.MissingDependency;
			}

			return missing;
		}

		public abstract List<MissingTypeSpecReference> ResolveMissingDependencies (MemberSpec caller);

		protected virtual bool IsNotCLSCompliant (out bool attrValue)
		{
			var cls = MemberDefinition.CLSAttributeValue;
			attrValue = cls ?? false;
			return cls == false;
		}

		public virtual string GetSignatureForDocumentation ()
		{
			return DeclaringType.GetSignatureForDocumentation () + "." + Name;
		}

		public virtual string GetSignatureForError ()
		{
			var bf = MemberDefinition as Property.BackingField;
			string name;
			if (bf == null) {
				name = Name;
			} else {
				name = bf.OriginalProperty.MemberName.Name;
			}

			return DeclaringType.GetSignatureForError () + "." + name;
		}

		public virtual MemberSpec InflateMember (TypeParameterInflator inflator)
		{
			var inflated = (MemberSpec) MemberwiseClone ();
			inflated.declaringType = inflator.TypeInstance;
			if (DeclaringType.IsGenericOrParentIsGeneric)
				inflated.state |= StateFlags.PendingMetaInflate;
#if DEBUG
			inflated.ID += 1000000;
#endif
			return inflated;
		}

		//
		// Is this member accessible from invocation context
		//
		public bool IsAccessible (IMemberContext ctx)
		{
			var ma = Modifiers & Modifiers.AccessibilityMask;
			if (ma == Modifiers.PUBLIC)
				return true;

			var parentType = /* this as TypeSpec ?? */ DeclaringType;
			var ctype = ctx.CurrentType;

			if (ma == Modifiers.PRIVATE) {
				if (ctype == null || parentType == null)
					return false;
				//
				// It's only accessible to the current class or children
				//
				if (parentType.MemberDefinition == ctype.MemberDefinition)
					return true;

				return TypeManager.IsNestedChildOf (ctype, parentType.MemberDefinition);
			}

			if ((ma & Modifiers.INTERNAL) != 0) {
				bool b;
				var assembly = ctype == null ? ctx.Module.DeclaringAssembly : ctype.MemberDefinition.DeclaringAssembly;

				if (parentType == null) {
					b = ((ITypeDefinition) MemberDefinition).IsInternalAsPublic (assembly);
				} else {
					b = DeclaringType.MemberDefinition.IsInternalAsPublic (assembly);
				}

				if (b || ma == Modifiers.INTERNAL)
					return b;
			}

			//
			// Checks whether `ctype' is a subclass or nested child of `parentType'.
			//
			while (ctype != null) {
				if (TypeManager.IsFamilyAccessible (ctype, parentType))
					return true;

				// Handle nested types.
				ctype = ctype.DeclaringType;	// TODO: Untested ???
			}

			return false;
		}

		//
		// Returns member CLS compliance based on full member hierarchy
		//
		public bool IsCLSCompliant ()
		{
			if ((state & StateFlags.CLSCompliant_Undetected) != 0) {
				state &= ~StateFlags.CLSCompliant_Undetected;

				bool compliant;
				if (IsNotCLSCompliant (out compliant))
					return false;

				if (!compliant) {
					if (DeclaringType != null) {
						compliant = DeclaringType.IsCLSCompliant ();
					} else {
						compliant = ((ITypeDefinition) MemberDefinition).DeclaringAssembly.IsCLSCompliant;
					}
				}

				if (compliant)
					state |= StateFlags.CLSCompliant;
			}

			return (state & StateFlags.CLSCompliant) != 0;
		}

		public bool IsConditionallyExcluded (IMemberContext ctx)
		{
			if ((Kind & (MemberKind.Class | MemberKind.Method)) == 0)
				return false;

			var conditions = MemberDefinition.ConditionalConditions ();
			if (conditions == null)
				return false;

			var m = ctx.CurrentMemberDefinition;
			CompilationSourceFile unit = null;
			while (m != null && unit == null) {
				unit = m as CompilationSourceFile;
				m = m.Parent;
			}

			if (unit != null) {
				foreach (var condition in conditions) {
					if (unit.IsConditionalDefined (condition))
						return false;
				}
			}

			return true;
		}

		public override string ToString ()
		{
			return GetSignatureForError ();
		}
	}

	//
	// Member details which are same between all member
	// specifications
	//
	public interface IMemberDefinition
	{
		bool? CLSAttributeValue { get; }
		string Name { get; }
		bool IsImported { get; }

		string[] ConditionalConditions ();
		ObsoleteAttribute GetAttributeObsolete ();
		void SetIsAssigned ();
		void SetIsUsed ();
	}

	public interface IMethodDefinition : IMemberDefinition
	{
		MethodBase Metadata { get; }
	}

	public interface IParametersMember : IInterfaceMemberSpec
	{
		AParametersCollection Parameters { get; }
	}

	public interface IInterfaceMemberSpec
	{
		TypeSpec MemberType { get; }
	}
}
