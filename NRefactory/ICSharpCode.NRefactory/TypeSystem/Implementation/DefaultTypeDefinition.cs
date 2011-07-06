// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public class DefaultTypeDefinition : AbstractFreezable, ITypeDefinition
	{
		readonly IProjectContent projectContent;
		readonly ITypeDefinition declaringTypeDefinition;
		
		string ns;
		string name;
		
		IList<ITypeReference> baseTypes;
		IList<ITypeParameter> typeParameters;
		IList<ITypeDefinition> nestedTypes;
		IList<IField> fields;
		IList<IMethod> methods;
		IList<IProperty> properties;
		IList<IEvent> events;
		IList<IAttribute> attributes;
		
		DomRegion region;
		DomRegion bodyRegion;
		
		// 1 byte per enum + 2 bytes for flags
		ClassType classType;
		Accessibility accessibility;
		BitVector16 flags;
		const ushort FlagSealed    = 0x0001;
		const ushort FlagAbstract  = 0x0002;
		const ushort FlagShadowing = 0x0004;
		const ushort FlagSynthetic = 0x0008;
		const ushort FlagAddDefaultConstructorIfRequired = 0x0010;
		const ushort FlagHasExtensionMethods = 0x0020;
		
		protected override void FreezeInternal()
		{
			baseTypes = FreezeList(baseTypes);
			typeParameters = FreezeList(typeParameters);
			nestedTypes = FreezeList(nestedTypes);
			fields = FreezeList(fields);
			methods = FreezeList(methods);
			properties = FreezeList(properties);
			events = FreezeList(events);
			attributes = FreezeList(attributes);
			base.FreezeInternal();
		}
		
		public DefaultTypeDefinition(ITypeDefinition declaringTypeDefinition, string name)
		{
			if (declaringTypeDefinition == null)
				throw new ArgumentNullException("declaringTypeDefinition");
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("name");
			this.projectContent = declaringTypeDefinition.ProjectContent;
			this.declaringTypeDefinition = declaringTypeDefinition;
			this.name = name;
			this.ns = declaringTypeDefinition.Namespace;
		}
		
		public DefaultTypeDefinition(IProjectContent projectContent, string ns, string name)
		{
			if (projectContent == null)
				throw new ArgumentNullException("projectContent");
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("name");
			this.projectContent = projectContent;
			this.ns = ns ?? string.Empty;
			this.name = name;
		}
		
		public ClassType ClassType {
			get { return classType; }
			set {
				CheckBeforeMutation();
				classType = value;
			}
		}
		
		public IList<ITypeReference> BaseTypes {
			get {
				if (baseTypes == null)
					baseTypes = new List<ITypeReference>();
				return baseTypes;
			}
		}
		
		public void ApplyInterningProvider(IInterningProvider provider)
		{
			if (provider != null) {
				ns = provider.Intern(ns);
				name = provider.Intern(name);
				baseTypes = provider.InternList(baseTypes);
				typeParameters = provider.InternList(typeParameters);
				attributes = provider.InternList(attributes);
			}
		}
		
		public IList<ITypeParameter> TypeParameters {
			get {
				if (typeParameters == null)
					typeParameters = new List<ITypeParameter>();
				return typeParameters;
			}
		}
		
		public IList<ITypeDefinition> NestedTypes {
			get {
				if (nestedTypes == null)
					nestedTypes = new List<ITypeDefinition>();
				return nestedTypes;
			}
		}
		
		public IList<IField> Fields {
			get {
				if (fields == null)
					fields = new List<IField>();
				return fields;
			}
		}
		
		public IList<IProperty> Properties {
			get {
				if (properties == null)
					properties = new List<IProperty>();
				return properties;
			}
		}
		
		public IList<IMethod> Methods {
			get {
				if (methods == null)
					methods = new List<IMethod>();
				return methods;
			}
		}
		
		public IList<IEvent> Events {
			get {
				if (events == null)
					events = new List<IEvent>();
				return events;
			}
		}
		
		public IEnumerable<IMember> Members {
			get {
				return this.Fields.SafeCast<IField, IMember>()
					.Concat(this.Properties.SafeCast<IProperty, IMember>())
					.Concat(this.Methods.SafeCast<IMethod, IMember>())
					.Concat(this.Events.SafeCast<IEvent, IMember>());
			}
		}
		
		public bool? IsReferenceType(ITypeResolveContext context)
		{
			switch (this.ClassType) {
				case ClassType.Class:
				case ClassType.Interface:
				case ClassType.Delegate:
					return true;
				case ClassType.Enum:
				case ClassType.Struct:
					return false;
				default:
					return null;
			}
		}
		
		public string FullName {
			get {
				if (declaringTypeDefinition != null) {
					return declaringTypeDefinition.FullName + "." + this.name;
				} else if (string.IsNullOrEmpty(ns)) {
					return this.name;
				} else {
					return this.ns + "." + this.name;
				}
			}
		}
		
		public string Name {
			get { return this.name; }
		}
		
		public string Namespace {
			get { return this.ns; }
		}
		
		public string ReflectionName {
			get {
				if (declaringTypeDefinition != null) {
					int tpCount = this.TypeParameterCount - declaringTypeDefinition.TypeParameterCount;
					string combinedName;
					if (tpCount > 0)
						combinedName = declaringTypeDefinition.ReflectionName + "+" + this.Name + "`" + tpCount.ToString(CultureInfo.InvariantCulture);
					else
						combinedName = declaringTypeDefinition.ReflectionName + "+" + this.Name;
					return combinedName;
				} else {
					int tpCount = this.TypeParameterCount;
					if (string.IsNullOrEmpty(ns)) {
						if (tpCount > 0)
							return this.Name + "`" + tpCount.ToString(CultureInfo.InvariantCulture);
						else
							return this.Name;
					} else {
						if (tpCount > 0)
							return this.Namespace + "." + this.Name + "`" + tpCount.ToString(CultureInfo.InvariantCulture);
						else
							return this.Namespace + "." + this.Name;
					}
				}
			}
		}
		
		public int TypeParameterCount {
			get { return typeParameters != null ? typeParameters.Count : 0; }
		}
		
		public EntityType EntityType {
			get { return EntityType.TypeDefinition; }
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public DomRegion BodyRegion {
			get { return bodyRegion; }
			set {
				CheckBeforeMutation();
				bodyRegion = value;
			}
		}
		
		public ITypeDefinition DeclaringTypeDefinition {
			get { return declaringTypeDefinition; }
		}
		
		public IType DeclaringType {
			get { return declaringTypeDefinition; }
		}
		
		public IList<IAttribute> Attributes {
			get {
				if (attributes == null)
					attributes = new List<IAttribute>();
				return attributes;
			}
		}
		
		public virtual string Documentation {
			get { return null; }
		}
		
		public Accessibility Accessibility {
			get { return accessibility; }
			set {
				CheckBeforeMutation();
				accessibility = value;
			}
		}
		
		public bool IsStatic {
			get { return IsAbstract && IsSealed; }
		}
		
		public bool IsAbstract {
			get { return flags[FlagAbstract]; }
			set {
				CheckBeforeMutation();
				flags[FlagAbstract] = value;
			}
		}
		
		public bool IsSealed {
			get { return flags[FlagSealed]; }
			set {
				CheckBeforeMutation();
				flags[FlagSealed] = value;
			}
		}
		
		public bool IsShadowing {
			get { return flags[FlagShadowing]; }
			set {
				CheckBeforeMutation();
				flags[FlagShadowing] = value;
			}
		}
		
		public bool IsSynthetic {
			get { return flags[FlagSynthetic]; }
			set {
				CheckBeforeMutation();
				flags[FlagSynthetic] = value;
			}
		}
		
		public bool IsPrivate {
			get { return Accessibility == Accessibility.Private; }
		}
		
		public bool IsPublic {
			get { return Accessibility == Accessibility.Public; }
		}
		
		public bool IsProtected {
			get { return Accessibility == Accessibility.Protected; }
		}
		
		public bool IsInternal {
			get { return Accessibility == Accessibility.Internal; }
		}
		
		public bool IsProtectedOrInternal {
			get { return Accessibility == Accessibility.ProtectedOrInternal; }
		}
		
		public bool IsProtectedAndInternal {
			get { return Accessibility == Accessibility.ProtectedAndInternal; }
		}
		
		public bool HasExtensionMethods {
			get { return flags[FlagHasExtensionMethods]; }
			set {
				CheckBeforeMutation();
				flags[FlagHasExtensionMethods] = value;
			}
		}
		
		public IProjectContent ProjectContent {
			get { return projectContent; }
		}
		
		public IEnumerable<IType> GetBaseTypes(ITypeResolveContext context)
		{
			bool hasNonInterface = false;
			if (baseTypes != null && this.ClassType != ClassType.Enum) {
				foreach (ITypeReference baseTypeRef in baseTypes) {
					IType baseType = baseTypeRef.Resolve(context);
					ITypeDefinition baseTypeDef = baseType.GetDefinition();
					if (baseTypeDef == null || baseTypeDef.ClassType != ClassType.Interface)
						hasNonInterface = true;
					yield return baseType;
				}
			}
			if (!hasNonInterface && !(this.Name == "Object" && this.Namespace == "System" && this.TypeParameterCount == 0)) {
				Type primitiveBaseType;
				switch (classType) {
					case ClassType.Enum:
						primitiveBaseType = typeof(Enum);
						break;
					case ClassType.Struct:
						primitiveBaseType = typeof(ValueType);
						break;
					case ClassType.Delegate:
						primitiveBaseType = typeof(Delegate);
						break;
					default:
						primitiveBaseType = typeof(object);
						break;
				}
				IType t = context.GetTypeDefinition(primitiveBaseType);
				if (t != null)
					yield return t;
			}
		}
		
		public virtual ITypeDefinition GetCompoundClass()
		{
			return this;
		}
		
		public virtual IList<ITypeDefinition> GetParts()
		{
			return new ITypeDefinition[] { this };
		}
		
		public ITypeDefinition GetDefinition()
		{
			return this;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			return this;
		}
		
		public virtual IEnumerable<IType> GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter = null)
		{
			ITypeDefinition compound = GetCompoundClass();
			if (compound != this)
				return compound.GetNestedTypes(context, filter);
			
			List<IType> nestedTypes = new List<IType>();
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					foreach (var baseTypeRef in this.BaseTypes) {
						IType baseType = baseTypeRef.Resolve(context);
						ITypeDefinition baseTypeDef = baseType.GetDefinition();
						if (baseTypeDef != null && baseTypeDef.ClassType != ClassType.Interface) {
							// get nested types from baseType (not baseTypeDef) so that generics work correctly
							nestedTypes.AddRange(baseType.GetNestedTypes(context, filter));
							break; // there is at most 1 non-interface base
						}
					}
					foreach (ITypeDefinition nestedType in this.NestedTypes) {
						if (filter == null || filter(nestedType)) {
							nestedTypes.Add(nestedType);
						}
					}
				}
			}
			return nestedTypes;
		}
		
		public virtual IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter = null)
		{
			ITypeDefinition compound = GetCompoundClass();
			if (compound != this)
				return compound.GetMethods(context, filter);
			
			List<IMethod> methods = new List<IMethod>();
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					int baseCount = 0;
					foreach (var baseType in GetBaseTypes(context)) {
						ITypeDefinition baseTypeDef = baseType.GetDefinition();
						if (baseTypeDef != null && (baseTypeDef.ClassType != ClassType.Interface || this.ClassType == ClassType.Interface)) {
							methods.AddRange(baseType.GetMethods(context, filter));
							baseCount++;
						}
					}
					if (baseCount > 1)
						RemoveDuplicates(methods);
					AddFilteredRange(methods, this.Methods.Where(m => !m.IsConstructor), filter);
				}
			}
			return methods;
		}
		
		public virtual IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter = null)
		{
			ITypeDefinition compound = GetCompoundClass();
			if (compound != this)
				return compound.GetConstructors(context, filter);
			
			List<IMethod> methods = new List<IMethod>();
			AddFilteredRange(methods, this.Methods.Where(m => m.IsConstructor && !m.IsStatic), filter);
			
			if (this.AddDefaultConstructorIfRequired) {
				if (this.ClassType == ClassType.Class && methods.Count == 0
				    || this.ClassType == ClassType.Enum || this.ClassType == ClassType.Struct)
				{
					var m = DefaultMethod.CreateDefaultConstructor(this);
					if (filter == null || filter(m))
						methods.Add(m);
				}
			}
			return methods;
		}
		
		public virtual IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter = null)
		{
			ITypeDefinition compound = GetCompoundClass();
			if (compound != this)
				return compound.GetProperties(context, filter);
			
			List<IProperty> properties = new List<IProperty>();
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					int baseCount = 0;
					foreach (var baseType in GetBaseTypes(context)) {
						ITypeDefinition baseTypeDef = baseType.GetDefinition();
						if (baseTypeDef != null && (baseTypeDef.ClassType != ClassType.Interface || this.ClassType == ClassType.Interface)) {
							properties.AddRange(baseType.GetProperties(context, filter));
							baseCount++;
						}
					}
					if (baseCount > 1)
						RemoveDuplicates(properties);
					AddFilteredRange(properties, this.Properties, filter);
				}
			}
			return properties;
		}
		
		public virtual IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter = null)
		{
			ITypeDefinition compound = GetCompoundClass();
			if (compound != this)
				return compound.GetFields(context, filter);
			
			List<IField> fields = new List<IField>();
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					int baseCount = 0;
					foreach (var baseType in GetBaseTypes(context)) {
						ITypeDefinition baseTypeDef = baseType.GetDefinition();
						if (baseTypeDef != null && (baseTypeDef.ClassType != ClassType.Interface || this.ClassType == ClassType.Interface)) {
							fields.AddRange(baseType.GetFields(context, filter));
							baseCount++;
						}
					}
					if (baseCount > 1)
						RemoveDuplicates(fields);
					AddFilteredRange(fields, this.Fields, filter);
				}
			}
			return fields;
		}
		
		public virtual IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter = null)
		{
			ITypeDefinition compound = GetCompoundClass();
			if (compound != this)
				return compound.GetEvents(context, filter);
			
			List<IEvent> events = new List<IEvent>();
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					int baseCount = 0;
					foreach (var baseType in GetBaseTypes(context)) {
						ITypeDefinition baseTypeDef = baseType.GetDefinition();
						if (baseTypeDef != null && (baseTypeDef.ClassType != ClassType.Interface || this.ClassType == ClassType.Interface)) {
							events.AddRange(baseType.GetEvents(context, filter));
							baseCount++;
						}
					}
					if (baseCount > 1)
						RemoveDuplicates(events);
					AddFilteredRange(events, this.Events, filter);
				}
			}
			return events;
		}
		
		public virtual IEnumerable<IMember> GetMembers(ITypeResolveContext context, Predicate<IMember> filter = null)
		{
			ITypeDefinition compound = GetCompoundClass();
			if (compound != this)
				return compound.GetMembers(context, filter);
			
			List<IMember> members = new List<IMember>();
			using (var busyLock = BusyManager.Enter(this)) {
				if (busyLock.Success) {
					int baseCount = 0;
					foreach (var baseType in GetBaseTypes(context)) {
						ITypeDefinition baseTypeDef = baseType.GetDefinition();
						if (baseTypeDef != null && (baseTypeDef.ClassType != ClassType.Interface || this.ClassType == ClassType.Interface)) {
							members.AddRange(baseType.GetMembers(context, filter));
							baseCount++;
						}
					}
					if (baseCount > 1)
						RemoveDuplicates(members);
					AddFilteredRange(members, this.Methods.Where(m => !m.IsConstructor), filter);
					AddFilteredRange(members, this.Properties, filter);
					AddFilteredRange(members, this.Fields, filter);
					AddFilteredRange(members, this.Events, filter);
				}
			}
			return members;
		}
		
		static void AddFilteredRange<T>(List<T> targetList, IEnumerable<T> sourceList, Predicate<T> filter) where T : class
		{
			if (filter == null) {
				targetList.AddRange(sourceList);
			} else {
				foreach (T element in sourceList) {
					if (filter(element))
						targetList.Add(element);
				}
			}
		}
		
		/// <summary>
		/// Removes duplicate members from the list.
		/// This is necessary when the same member can be inherited twice due to multiple inheritance.
		/// </summary>
		static void RemoveDuplicates<T>(List<T> list) where T : class
		{
			if (list.Count > 1) {
				HashSet<T> hash = new HashSet<T>();
				list.RemoveAll(m => !hash.Add(m));
			}
		}
		
		// we use reference equality
		bool IEquatable<IType>.Equals(IType other)
		{
			return this == other;
		}
		
		public override string ToString()
		{
			return ReflectionName;
		}
		
		/// <summary>
		/// Gets whether a default constructor should be added to this class if it is required.
		/// Such automatic default constructors will not appear in ITypeDefinition.Methods, but will be present
		/// in IType.GetMethods().
		/// </summary>
		/// <remarks>This way of creating the default constructor is necessary because
		/// we cannot create it directly in the IClass - we need to consider partial classes.</remarks>
		public bool AddDefaultConstructorIfRequired {
			get { return flags[FlagAddDefaultConstructorIfRequired]; }
			set {
				CheckBeforeMutation();
				flags[FlagAddDefaultConstructorIfRequired] = value;
			}
		}
		
		public IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitTypeDefinition(this);
		}
		
		public IType VisitChildren(TypeVisitor visitor)
		{
			return this;
		}
	}
}
