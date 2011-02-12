// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class DefaultClass : AbstractEntity, IClass, IComparable
	{
		ClassType classType;
		DomRegion region;
		
		ICompilationUnit compilationUnit;
		
		IList<IReturnType> baseTypes;
		
		IList<IClass>    innerClasses;
		IList<IField>    fields;
		IList<IProperty> properties;
		IList<IMethod>   methods;
		IList<IEvent>    events;
		IList<ITypeParameter> typeParameters;
		IUsingScope usingScope;
		
		protected override void FreezeInternal()
		{
			baseTypes = FreezeList(baseTypes);
			innerClasses = FreezeList(innerClasses);
			fields = FreezeList(fields);
			properties = FreezeList(properties);
			methods = FreezeList(methods);
			events = FreezeList(events);
			typeParameters = FreezeList(typeParameters);
			base.FreezeInternal();
		}
		
		/*
		public virtual IClass Unfreeze()
		{
			DefaultClass copy = new DefaultClass(compilationUnit, DeclaringType);
			copy.FullyQualifiedName = this.FullyQualifiedName;
			copy.Attributes.AddRange(this.Attributes);
			copy.BaseTypes.AddRange(this.BaseTypes);
			copy.BodyRegion = this.BodyRegion;
			copy.ClassType = this.ClassType;
			copy.Documentation = this.Documentation;
			copy.Events.AddRange(this.Events);
			copy.Fields.AddRange(this.Fields);
			copy.InnerClasses.AddRange(this.InnerClasses);
			copy.Methods.AddRange(this.Methods);
			copy.Modifiers = this.Modifiers;
			copy.Properties.AddRange(this.Properties);
			copy.Region = this.Region;
			copy.TypeParameters.AddRange(this.TypeParameters);
			copy.UserData = this.UserData;
			return copy;
		}
		 */
		
		byte flags = addDefaultConstructorIfRequiredFlag;
		const byte calculatedFlagsReady                 = 0x01;
		const byte hasPublicOrInternalStaticMembersFlag = 0x02;
		const byte hasExtensionMethodsFlag              = 0x04;
		const byte addDefaultConstructorIfRequiredFlag  = 0x08;
		
		internal byte CalculatedFlags {
			get {
				if ((flags & calculatedFlagsReady) == 0) {
					flags |= calculatedFlagsReady;
					foreach (IMember m in this.Fields) {
						if (m.IsStatic && (m.IsPublic || m.IsInternal)) {
							flags |= hasPublicOrInternalStaticMembersFlag;
						}
					}
					foreach (IProperty m in this.Properties) {
						if (m.IsStatic && (m.IsPublic || m.IsInternal)) {
							flags |= hasPublicOrInternalStaticMembersFlag;
						}
						if (m.IsExtensionMethod) {
							flags |= hasExtensionMethodsFlag;
						}
					}
					foreach (IMethod m in this.Methods) {
						if (m.IsStatic && (m.IsPublic || m.IsInternal)) {
							flags |= hasPublicOrInternalStaticMembersFlag;
						}
						if (m.IsExtensionMethod) {
							flags |= hasExtensionMethodsFlag;
						}
					}
					foreach (IMember m in this.Events) {
						if (m.IsStatic && (m.IsPublic || m.IsInternal)) {
							flags |= hasPublicOrInternalStaticMembersFlag;
						}
					}
					foreach (IClass c in this.InnerClasses) {
						if (c.IsPublic || c.IsInternal) {
							flags |= hasPublicOrInternalStaticMembersFlag;
						}
					}
				}
				return flags;
			}
			set {
				CheckBeforeMutation();
				flags = value;
			}
		}
		public bool HasPublicOrInternalStaticMembers {
			get {
				return (CalculatedFlags & hasPublicOrInternalStaticMembersFlag) == hasPublicOrInternalStaticMembersFlag;
			}
		}
		public bool HasExtensionMethods {
			get {
				return (CalculatedFlags & hasExtensionMethodsFlag) == hasExtensionMethodsFlag;
			}
		}
		public bool AddDefaultConstructorIfRequired {
			get {
				return (flags & addDefaultConstructorIfRequiredFlag) == addDefaultConstructorIfRequiredFlag;
			}
			set {
				if (value)
					flags |= addDefaultConstructorIfRequiredFlag;
				else
					flags &= unchecked((byte)~addDefaultConstructorIfRequiredFlag);
			}
		}
		
		/// <summary>
		/// Gets the using scope of contains this class.
		/// </summary>
		public IUsingScope UsingScope {
			get { return usingScope; }
			set {
				if (value == null)
					throw new ArgumentNullException("UsingScope");
				CheckBeforeMutation();
				usingScope = value;
			}
		}
		
		public DefaultClass(ICompilationUnit compilationUnit, string fullyQualifiedName) : base(null)
		{
			if (compilationUnit == null)
				throw new ArgumentNullException("compilationUnit");
			if (fullyQualifiedName == null)
				throw new ArgumentNullException("fullyQualifiedName");
			this.compilationUnit = compilationUnit;
			this.FullyQualifiedName = fullyQualifiedName;
			this.UsingScope = compilationUnit.UsingScope;
		}
		
		public DefaultClass(ICompilationUnit compilationUnit, IClass declaringType) : base(declaringType)
		{
			if (compilationUnit == null)
				throw new ArgumentNullException("compilationUnit");
			this.compilationUnit = compilationUnit;
			this.UsingScope = compilationUnit.UsingScope;
		}
		
		public DefaultClass(ICompilationUnit compilationUnit, ClassType classType, ModifierEnum modifiers, DomRegion region, IClass declaringType) : base(declaringType)
		{
			if (compilationUnit == null)
				throw new ArgumentNullException("compilationUnit");
			this.compilationUnit = compilationUnit;
			this.region = region;
			this.classType = classType;
			Modifiers = modifiers;
			this.UsingScope = compilationUnit.UsingScope;
		}
		
		// fields must be volatile to ensure that the optimizer doesn't reorder accesses to it
		// or causes DefaultReturnType to return null when the local copy of this.defaultReturnType is
		// optimized away.
		volatile IReturnType defaultReturnType;
		bool hasCompoundClass;
		
		public IReturnType DefaultReturnType {
			get {
				IReturnType defaultReturnType = this.defaultReturnType;
				if (defaultReturnType == null) {
					lock (this) {
						this.defaultReturnType = defaultReturnType = CreateDefaultReturnType();
					}
				}
				return defaultReturnType;
			}
		}
		
		protected virtual IReturnType CreateDefaultReturnType()
		{
			if (hasCompoundClass) {
				return new GetClassReturnType(ProjectContent, FullyQualifiedName, TypeParameters.Count);
			} else {
				return new DefaultReturnType(this);
			}
		}
		
		bool IClass.HasCompoundClass {
			get { return hasCompoundClass; }
			set {
				if (hasCompoundClass != value) {
					lock (this) {
						hasCompoundClass = value;
						defaultReturnType = null;
					}
				}
			}
		}
		
		public bool IsPartial {
			get {
				return (this.Modifiers & ModifierEnum.Partial) == ModifierEnum.Partial;
			}
			set {
				CheckBeforeMutation();
				if (value)
					this.Modifiers |= ModifierEnum.Partial;
				else
					this.Modifiers &= ~ModifierEnum.Partial;
			}
		}
		
		public IClass GetCompoundClass()
		{
			return this.DefaultReturnType.GetUnderlyingClass() ?? this;
		}
		
		protected override void OnFullyQualifiedNameChanged(EventArgs e)
		{
			base.OnFullyQualifiedNameChanged(e);
			defaultReturnType = null; // re-create default return type
		}
		
		public sealed override ICompilationUnit CompilationUnit {
			[System.Diagnostics.DebuggerStepThrough]
			get {
				return compilationUnit;
			}
		}
		
		public ClassType ClassType {
			get {
				return classType;
			}
			set {
				CheckBeforeMutation();
				classType = value;
			}
		}
		
		public DomRegion Region {
			get {
				return region;
			}
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public override string DotNetName {
			get {
				string fullName;
				int typeParametersCount = this.TypeParameters.Count;
				if (this.DeclaringType != null) {
					fullName = this.DeclaringType.DotNetName + "+" + this.Name;
					typeParametersCount -= this.DeclaringType.TypeParameters.Count;
				} else {
					fullName = this.FullyQualifiedName;
				}
				if (typeParametersCount == 0) {
					return fullName;
				} else {
					return fullName + "`" + typeParametersCount;
				}
			}
		}
		
		public override string DocumentationTag {
			get {
				return "T:" + DotNetName;
			}
		}
		
		public IList<IReturnType> BaseTypes {
			get {
				if (baseTypes == null) {
					baseTypes = new List<IReturnType>();
				}
				return baseTypes;
			}
		}
		
		public virtual IList<IClass> InnerClasses {
			get {
				if (innerClasses == null) {
					innerClasses = new List<IClass>();
				}
				return innerClasses;
			}
		}
		
		public virtual IList<IField> Fields {
			get {
				if (fields == null) {
					fields = new List<IField>();
				}
				return fields;
			}
		}
		
		public virtual IList<IProperty> Properties {
			get {
				if (properties == null) {
					properties = new List<IProperty>();
				}
				return properties;
			}
		}
		
		public virtual IList<IMethod> Methods {
			get {
				if (methods == null) {
					methods = new List<IMethod>();
				}
				return methods;
			}
		}
		
		public virtual IList<IEvent> Events {
			get {
				if (events == null) {
					events = new List<IEvent>();
				}
				return events;
			}
		}
		
		public virtual IList<ITypeParameter> TypeParameters {
			get {
				if (typeParameters == null) {
					typeParameters = new List<ITypeParameter>();
				}
				return typeParameters;
			}
			set {
				CheckBeforeMutation();
				typeParameters = value;
			}
		}
		
		public virtual int CompareTo(IClass value)
		{
			int cmp;
			
			if(0 != (cmp = base.CompareTo((IEntity)value))) {
				return cmp;
			}
			
			if (FullyQualifiedName != null) {
				cmp = FullyQualifiedName.CompareTo(value.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
				return this.TypeParameters.Count - value.TypeParameters.Count;
			}
			return -1;
		}
		
		int IComparable.CompareTo(object o)
		{
			return CompareTo((IClass)o);
		}
		
		volatile IClass[] inheritanceTreeCache;
		volatile IClass[] inheritanceTreeClassesOnlyCache;
		
		public IEnumerable<IClass> ClassInheritanceTree {
			get {
				IClass compoundClass = GetCompoundClass();
				if (compoundClass != this)
					return compoundClass.ClassInheritanceTree;
				
				// Notes:
				// the ClassInheritanceTree must work even if the following things happen:
				// - cyclic inheritance
				// - multithreaded calls
				
				// Recursive calls are possible if the SearchType request done by GetUnderlyingClass()
				// uses ClassInheritanceTree.
				// Such recursive calls are tricky, they have caused incorrect behavior (SD2-1474)
				// or performance problems (SD2-1510) in the past.
				// As of revision 3769, NRefactoryAstConvertVisitor sets up the SearchClassReturnType
				// used for base types so that it does not look up inner classes in the class itself,
				// so the ClassInheritanceTree is not used created in those cases.
				// However, other language bindings might not set up base types correctly, so it's
				// still possible that ClassInheritanceTree is called recursivly.
				// In that case, we'll return an invalid inheritance tree because of
				// ProxyReturnType's automatic stack overflow prevention.
				
				// We do not use locks to protect against multithreaded calls because
				// resolving one class's base types can cause getting the inheritance tree
				// of another class -> beware of deadlocks
				
				IClass[] inheritanceTree = this.inheritanceTreeCache;
				if (inheritanceTree != null) {
					return inheritanceTree;
				}
				
				inheritanceTree = CalculateClassInheritanceTree(false);
				
				this.inheritanceTreeCache = inheritanceTree;
				if (!KeepInheritanceTree)
					DomCache.RegisterForClear(ClearCachedInheritanceTree);
				
				return inheritanceTree;
			}
		}
		
		public IEnumerable<IClass> ClassInheritanceTreeClassesOnly {
			get {
				IClass compoundClass = GetCompoundClass();
				if (compoundClass != this)
					return compoundClass.ClassInheritanceTreeClassesOnly;
				
				// Notes:
				// the ClassInheritanceTree must work even if the following things happen:
				// - cyclic inheritance
				// - multithreaded calls
				
				// Recursive calls are possible if the SearchType request done by GetUnderlyingClass()
				// uses ClassInheritanceTree.
				// Such recursive calls are tricky, they have caused incorrect behavior (SD2-1474)
				// or performance problems (SD2-1510) in the past.
				// As of revision 3769, NRefactoryAstConvertVisitor sets up the SearchClassReturnType
				// used for base types so that it does not look up inner classes in the class itself,
				// so the ClassInheritanceTree is not used created in those cases.
				// However, other language bindings might not set up base types correctly, so it's
				// still possible that ClassInheritanceTree is called recursivly.
				// In that case, we'll return an invalid inheritance tree because of
				// ProxyReturnType's automatic stack overflow prevention.
				
				// We do not use locks to protect against multithreaded calls because
				// resolving one class's base types can cause getting the inheritance tree
				// of another class -> beware of deadlocks
				
				IClass[] inheritanceTreeClassesOnly = this.inheritanceTreeClassesOnlyCache;
				if (inheritanceTreeClassesOnly != null) {
					return inheritanceTreeClassesOnly;
				}
				
				inheritanceTreeClassesOnly = CalculateClassInheritanceTree(true);
				
				this.inheritanceTreeClassesOnlyCache = inheritanceTreeClassesOnly;
				if (!KeepInheritanceTree)
					DomCache.RegisterForClear(ClearCachedInheritanceTree);
				
				return inheritanceTreeClassesOnly;
			}
		}
		
		void ClearCachedInheritanceTree()
		{
			inheritanceTreeClassesOnlyCache = null;
			inheritanceTreeCache = null;
		}
		
		IClass[] CalculateClassInheritanceTree(bool ignoreInterfaces)
		{
			List<IClass> visitedList = new List<IClass>();
			Queue<IReturnType> typesToVisit = new Queue<IReturnType>();
			bool enqueuedLastBaseType = false;
			IClass currentClass = this;
			IReturnType nextType;
			do {
				if (currentClass != null) {
					if ((!ignoreInterfaces || currentClass.ClassType != ClassType.Interface) && !visitedList.Contains(currentClass)) {
						visitedList.Add(currentClass);
						foreach (IReturnType type in currentClass.BaseTypes) {
							typesToVisit.Enqueue(type);
						}
					}
				}
				if (typesToVisit.Count > 0) {
					nextType = typesToVisit.Dequeue();
				} else {
					nextType = enqueuedLastBaseType ? null : GetBaseTypeByClassType(this);
					enqueuedLastBaseType = true;
				}
				if (nextType != null) {
					currentClass = nextType.GetUnderlyingClass();
				}
			} while (nextType != null);
			return visitedList.ToArray();
		}
		
		/// <summary>
		/// Specifies whether to keep the inheritance tree when the DomCache is cleared.
		/// </summary>
		protected virtual bool KeepInheritanceTree {
			get { return false; }
		}
		
		IReturnType cachedBaseType;
		
		public IReturnType BaseType {
			get {
				if (cachedBaseType == null) {
					foreach (IReturnType baseType in this.BaseTypes) {
						IClass baseClass = baseType.GetUnderlyingClass();
						if (baseClass != null && baseClass.ClassType == this.ClassType) {
							cachedBaseType = baseType;
							break;
						}
					}
				}
				if (cachedBaseType == null) {
					return GetBaseTypeByClassType(this);
				} else {
					return cachedBaseType;
				}
			}
		}
		
		internal static IReturnType GetBaseTypeByClassType(IClass c)
		{
			switch (c.ClassType) {
				case ClassType.Class:
				case ClassType.Interface:
					if (c.FullyQualifiedName != "System.Object") {
						return c.ProjectContent.SystemTypes.Object;
					}
					break;
				case ClassType.Enum:
					return c.ProjectContent.SystemTypes.Enum;
				case ClassType.Delegate:
					return c.ProjectContent.SystemTypes.Delegate;
				case ClassType.Struct:
					return c.ProjectContent.SystemTypes.ValueType;
			}
			return null;
		}
		
		public IClass BaseClass {
			get {
				foreach (IReturnType baseType in this.BaseTypes) {
					IClass baseClass = baseType.GetUnderlyingClass();
					if (baseClass != null && baseClass.ClassType == this.ClassType)
						return baseClass;
				}
				IReturnType defaultBaseType = GetBaseTypeByClassType(this);
				if (defaultBaseType != null)
					return defaultBaseType.GetUnderlyingClass();
				else
					return null;
			}
		}
		
		public bool IsTypeInInheritanceTree(IClass possibleBaseClass)
		{
			if (possibleBaseClass == null) {
				return false;
			}
			foreach (IClass baseClass in this.ClassInheritanceTree) {
				if (possibleBaseClass.FullyQualifiedName == baseClass.FullyQualifiedName
				    && possibleBaseClass.TypeParameters.Count == baseClass.TypeParameters.Count)
					return true;
			}
			return false;
		}
		
		/// <summary>
		/// Searches the member with the specified name. Returns the first member/overload found.
		/// </summary>
		public IMember SearchMember(string memberName, LanguageProperties language)
		{
			if (memberName == null || memberName.Length == 0) {
				return null;
			}
			StringComparer cmp = language.NameComparer;
			foreach (IProperty p in Properties) {
				if (cmp.Equals(p.Name, memberName)) {
					return p;
				}
			}
			foreach (IEvent e in Events) {
				if (cmp.Equals(e.Name, memberName)) {
					return e;
				}
			}
			foreach (IField f in Fields) {
				if (cmp.Equals(f.Name, memberName)) {
					return f;
				}
			}
			foreach (IMethod m in Methods) {
				if (cmp.Equals(m.Name, memberName)) {
					return m;
				}
			}
			return null;
		}
		
		public IClass GetInnermostClass(int caretLine, int caretColumn)
		{
			foreach (IClass c in InnerClasses) {
				if (c != null && IsInside(c, caretLine, caretColumn)) {
					return c.GetInnermostClass(caretLine, caretColumn);
				}
			}
			return this;
		}
		
		internal static bool IsInside(IClass c, int caretLine, int caretColumn)
		{
			return c.Region.IsInside(caretLine, caretColumn)
				|| c.Attributes.Any((IAttribute a) => a.Region.IsInside(caretLine, caretColumn));
		}
		
		public List<IClass> GetAccessibleTypes(IClass callingClass)
		{
			List<IClass> types = new List<IClass>();
			List<IClass> visitedTypes = new List<IClass>();
			
			IClass currentClass = this;
			do {
				if (visitedTypes.Contains(currentClass))
					break;
				visitedTypes.Add(currentClass);
				bool isClassInInheritanceTree = callingClass != null ? callingClass.IsTypeInInheritanceTree(currentClass) : false;
				foreach (IClass c in currentClass.InnerClasses) {
					if (c.IsAccessible(callingClass, isClassInInheritanceTree)) {
						types.Add(c);
					}
				}
				currentClass = currentClass.BaseClass;
			} while (currentClass != null);
			return types;
		}
		
		public IEnumerable<IMember> AllMembers {
			get {
				IEnumerable<IMember> p = properties;
				return p.Concat(methods)
					.Concat(fields)
					.Concat(events);
			}
		}
		
		public override EntityType EntityType {
			get {
				return EntityType.Class;
			}
		}
	}
}
