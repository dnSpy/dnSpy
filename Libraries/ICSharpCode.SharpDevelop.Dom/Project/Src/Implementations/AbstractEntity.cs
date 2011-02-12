// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	public abstract class AbstractEntity : AbstractFreezable, IEntity
	{
		ModifierEnum modifiers = ModifierEnum.None;
		IList<IAttribute> attributes;
		DomRegion bodyRegion;
		
		IClass declaringType;
		
		string fullyQualifiedName;
		string name;
		string nspace;
		
		public AbstractEntity(IClass declaringType)
		{
			this.declaringType = declaringType;
		}
		
		public AbstractEntity(IClass declaringType, string name)
		{
			this.declaringType = declaringType;
			this.name = name;
			if (declaringType != null)
				nspace = declaringType.FullyQualifiedName;
			
			// lazy-computing the fully qualified name for class members saves ~7 MB RAM (when loading the SharpDevelop solution).
			//fullyQualifiedName = nspace + '.' + name;
		}
		
		public override string ToString()
		{
			return String.Format("[{0}: {1}]", GetType().Name, FullyQualifiedName);
		}
		
		#region Naming
		static readonly char[] nameDelimiters = { '.', '+' };
		
		
		public string FullyQualifiedName {
			get {
				if (fullyQualifiedName == null) {
					if (name != null && nspace != null) {
						fullyQualifiedName = nspace + '.' + name;
					} else {
						return String.Empty;
					}
				}
				return fullyQualifiedName;
			}
			set {
				CheckBeforeMutation();
				if (fullyQualifiedName == value)
					return;
				fullyQualifiedName = value;
				name   = null;
				nspace = null;
				OnFullyQualifiedNameChanged(EventArgs.Empty);
			}
		}
		
		protected virtual void OnFullyQualifiedNameChanged(EventArgs e)
		{
		}
		
		public virtual string DotNetName {
			get {
				if (this.DeclaringType != null) {
					return this.DeclaringType.DotNetName + "." + this.Name;
				} else {
					return FullyQualifiedName;
				}
			}
		}
		
		public string Name {
			get {
				if (name == null && FullyQualifiedName != null) {
					int lastIndex = FullyQualifiedName.LastIndexOfAny(nameDelimiters);
					
					if (lastIndex < 0) {
						name = FullyQualifiedName;
					} else {
						name = FullyQualifiedName.Substring(lastIndex + 1);
					}
				}
				return name;
			}
		}

		public string Namespace {
			get {
				if (nspace == null && FullyQualifiedName != null) {
					int lastIndex = FullyQualifiedName.LastIndexOf('.');
					
					if (lastIndex < 0) {
						nspace = String.Empty;
					} else {
						nspace = FullyQualifiedName.Substring(0, lastIndex);
					}
				}
				return nspace;
			}
		}
		
		
		#endregion
		
		protected override void FreezeInternal()
		{
			attributes = FreezeList(attributes);
			base.FreezeInternal();
		}
		
		public IClass DeclaringType {
			get {
				return declaringType;
			}
		}
		
		public virtual DomRegion BodyRegion {
			get {
				return bodyRegion;
			}
			set {
				CheckBeforeMutation();
				bodyRegion = value;
			}
		}
		
		public object UserData { get; set; }
		
		public IList<IAttribute> Attributes {
			get {
				if (attributes == null) {
					attributes = new List<IAttribute>();
				}
				return attributes;
			}
			set {
				CheckBeforeMutation();
				attributes = value;
			}
		}
		
		string documentation;
		
		public string Documentation {
			get {
				if (documentation == null) {
					string documentationTag = this.DocumentationTag;
					if (documentationTag != null) {
						IProjectContent pc = null;
						if (this is IClass) {
							pc = ((IClass)this).ProjectContent;
						} else if (declaringType != null) {
							pc = declaringType.ProjectContent;
						}
						if (pc != null) {
							return pc.GetXmlDocumentation(documentationTag);
						}
					}
				}
				return documentation;
			}
			set {
				CheckBeforeMutation();
				documentation = value;
			}
		}
		
		protected void CopyDocumentationFrom(IEntity entity)
		{
			AbstractEntity ae = entity as AbstractEntity;
			if (ae != null) {
				this.Documentation = ae.documentation; // do not cause pc.GetXmlDocumentation call for documentation copy
			} else {
				this.Documentation = entity.Documentation;
			}
		}
		
		public abstract string DocumentationTag {
			get;
		}
		
		#region Modifiers
		public ModifierEnum Modifiers {
			get {
				return modifiers;
			}
			set {
				CheckBeforeMutation();
				modifiers = value;
			}
		}
		
		public bool IsAbstract {
			get {
				return (modifiers & ModifierEnum.Abstract) == ModifierEnum.Abstract;
			}
		}
		
		public bool IsSealed {
			get {
				return (modifiers & ModifierEnum.Sealed) == ModifierEnum.Sealed;
			}
		}
		
		public bool IsStatic {
			get {
				return ((modifiers & ModifierEnum.Static) == ModifierEnum.Static) || IsConst;
			}
		}
		
		public bool IsConst {
			get {
				return (modifiers & ModifierEnum.Const) == ModifierEnum.Const;
			}
		}
		
		public bool IsVirtual {
			get {
				return (modifiers & ModifierEnum.Virtual) == ModifierEnum.Virtual;
			}
		}
		
		public bool IsPublic {
			get {
				return (modifiers & ModifierEnum.Public) == ModifierEnum.Public;
			}
		}
		
		public bool IsProtected {
			get {
				return (modifiers & ModifierEnum.Protected) == ModifierEnum.Protected;
			}
		}
		
		public bool IsPrivate {
			get {
				return (modifiers & ModifierEnum.Private) == ModifierEnum.Private;
			}
		}
		
		public bool IsInternal {
			get {
				return (modifiers & ModifierEnum.Internal) == ModifierEnum.Internal;
			}
		}
		
		[Obsolete("This property does not do what one would expect - it merely checks if protected+internal are set, it is not the equivalent of AssemblyAndFamily in Reflection!")]
		public bool IsProtectedAndInternal {
			get {
				return (modifiers & (ModifierEnum.Internal | ModifierEnum.Protected)) == (ModifierEnum.Internal | ModifierEnum.Protected);
			}
		}
		
		[Obsolete("This property does not do what one would expect - it merely checks if one of protected+internal is set, it is not the equivalent of AssemblyOrFamily in Reflection!")]
		public bool IsProtectedOrInternal {
			get {
				return IsProtected || IsInternal;
			}
		}
		
		public bool IsReadonly {
			get {
				return (modifiers & ModifierEnum.Readonly) == ModifierEnum.Readonly;
			}
		}
		
		public bool IsOverride {
			get {
				return (modifiers & ModifierEnum.Override) == ModifierEnum.Override;
			}
		}
		public bool IsOverridable {
			get {
				return (IsOverride || IsVirtual || IsAbstract || 
				    // Interface members have IsVirtual == IsAbstract == false. These properties are based on modifiers only.
				    (this.DeclaringType != null && this.DeclaringType.ClassType == ClassType.Interface)) 
					&& !IsSealed;
			}
		}
		public bool IsNew {
			get {
				return (modifiers & ModifierEnum.New) == ModifierEnum.New;
			}
		}
		public bool IsSynthetic {
			get {
				return (modifiers & ModifierEnum.Synthetic) == ModifierEnum.Synthetic;
			}
		}
		#endregion
		
		public bool IsAccessible(IClass callingClass, bool isAccessThoughReferenceOfCurrentClass)
		{
			if (IsPublic) {
				return true;
			} else if (IsInternal) {
				// members can be both internal and protected: in that case, we want to return true if it is visible
				// through any of the modifiers
				if (callingClass != null && this.DeclaringType.ProjectContent.InternalsVisibleTo(callingClass.ProjectContent))
					return true;
			}
			// protected or private:
			// search in callingClass and, if callingClass is a nested class, in its outer classes
			while (callingClass != null) {
				if (IsProtected) {
					if (!isAccessThoughReferenceOfCurrentClass && !IsStatic)
						return false;
					return callingClass.IsTypeInInheritanceTree(this.DeclaringType);
				} else {
					// private
					if (DeclaringType.FullyQualifiedName == callingClass.FullyQualifiedName
					    && DeclaringType.TypeParameters.Count == callingClass.TypeParameters.Count)
					{
						return true;
					}
				}
				
				callingClass = callingClass.DeclaringType;
			}
			return false;
		}
		
		public abstract ICompilationUnit CompilationUnit {
			get;
		}
		
		public IProjectContent ProjectContent {
			[System.Diagnostics.DebuggerStepThrough]
			get {
				return this.CompilationUnit.ProjectContent;
			}
		}
		
		public virtual int CompareTo(IEntity value)
		{
			return this.Modifiers - value.Modifiers;
		}
		
		int IComparable.CompareTo(object value)
		{
			return CompareTo((IEntity)value);
		}
		
		public abstract EntityType EntityType {
			get;
		}
	}
}
