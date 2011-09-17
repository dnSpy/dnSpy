// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	[Serializable]
	public class DefaultTypeDefinition : AbstractFreezable, ITypeDefinition
	{
		readonly IProjectContent projectContent;
		readonly IParsedFile parsedFile;
		readonly ITypeDefinition declaringTypeDefinition;
		
		volatile ITypeDefinition compoundTypeDefinition;
		
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
		TypeKind kind = TypeKind.Class;
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
			this.parsedFile = declaringTypeDefinition.ParsedFile;
			this.declaringTypeDefinition = declaringTypeDefinition;
			
			this.name = name;
			this.ns = declaringTypeDefinition.Namespace;
			
			this.compoundTypeDefinition = this;
		}
		
		public DefaultTypeDefinition(IParsedFile parsedFile, string ns, string name)
		{
			if (parsedFile == null)
				throw new ArgumentNullException("parsedFile");
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("name");
			this.parsedFile = parsedFile;
			this.projectContent = parsedFile.ProjectContent;
			this.ns = ns ?? string.Empty;
			this.name = name;
			
			this.compoundTypeDefinition = this;
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
			
			this.compoundTypeDefinition = this;
		}
		
		public TypeKind Kind {
			get { return kind; }
			set {
				CheckBeforeMutation();
				kind = value;
			}
		}
		
		public bool? IsReferenceType(ITypeResolveContext context)
		{
			switch (kind) {
				case TypeKind.Class:
				case TypeKind.Interface:
				case TypeKind.Delegate:
					return true;
				case TypeKind.Enum:
				case TypeKind.Struct:
					return false;
				default:
					return null;
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
				IEnumerable<IMember> members = this.Fields;
				return members
					.Concat(this.Properties)
					.Concat(this.Methods)
					.Concat(this.Events);
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
			get {
				// To save memory, we don't store the documentation provider within the type,
				// but use our the project content as a documentation provider:
				IDocumentationProvider provider = projectContent as IDocumentationProvider;
				if (provider != null)
					return provider.GetDocumentation(this);
				else
					return null;
			}
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
		
		public IParsedFile ParsedFile {
			get { return parsedFile; }
		}
		
		public IEnumerable<IType> GetBaseTypes(ITypeResolveContext context)
		{
			ITypeDefinition compound = this.compoundTypeDefinition;
			if (compound != this)
				return compound.GetBaseTypes(context);
			else
				return GetBaseTypesImpl(context);
		}
		
		IEnumerable<IType> GetBaseTypesImpl(ITypeResolveContext context)
		{
			bool hasNonInterface = false;
			if (baseTypes != null && kind != TypeKind.Enum) {
				foreach (ITypeReference baseTypeRef in baseTypes) {
					IType baseType = baseTypeRef.Resolve(context);
					if (baseType.Kind != TypeKind.Interface)
						hasNonInterface = true;
					yield return baseType;
				}
			}
			if (!hasNonInterface && !(this.Name == "Object" && this.Namespace == "System" && this.TypeParameterCount == 0)) {
				string primitiveBaseType;
				switch (kind) {
					case TypeKind.Enum:
						primitiveBaseType = "Enum";
						break;
					case TypeKind.Struct:
					case TypeKind.Void:
						primitiveBaseType = "ValueType";
						break;
					case TypeKind.Delegate:
						primitiveBaseType = "Delegate";
						break;
					default:
						primitiveBaseType = "Object";
						break;
				}
				IType t = context.GetTypeDefinition("System", primitiveBaseType, 0, StringComparer.Ordinal);
				if (t != null)
					yield return t;
			}
		}
		
		internal void SetCompoundTypeDefinition(ITypeDefinition compoundTypeDefinition)
		{
			this.compoundTypeDefinition = compoundTypeDefinition;
		}
		
		public virtual IList<ITypeDefinition> GetParts()
		{
			return new ITypeDefinition[] { this };
		}
		
		public ITypeDefinition GetDefinition()
		{
			return compoundTypeDefinition;
		}
		
		IType ITypeReference.Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			return this;
		}
		
		#region GetMembers
		public IEnumerable<IType> GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			const GetMemberOptions opt = GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions;
			if ((options & opt) == opt) {
				ITypeDefinition compound = this.compoundTypeDefinition;
				if (compound != this)
					return compound.GetNestedTypes(context, filter, options);
				
				return ApplyFilter(this.NestedTypes, filter);
			} else {
				return GetMembersHelper.GetNestedTypes(this, context, filter, options);
			}
		}
		
		public IEnumerable<IType> GetNestedTypes(IList<IType> typeArguments, ITypeResolveContext context, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return GetMembersHelper.GetNestedTypes(this, typeArguments, context, filter, options);
		}
		
		public virtual IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				ITypeDefinition compound = this.compoundTypeDefinition;
				if (compound != this)
					return compound.GetMethods(context, filter, options);
				
				return ApplyFilter(this.Methods, Utils.ExtensionMethods.And(m => !m.IsConstructor, filter));
			} else {
				return GetMembersHelper.GetMethods(this, context, filter, options);
			}
		}
		
		public virtual IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return GetMembersHelper.GetMethods(this, typeArguments, context, filter, options);
		}
		
		public virtual IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				ITypeDefinition compound = this.compoundTypeDefinition;
				if (compound != this)
					return compound.GetConstructors(context, filter, options);
				
				return GetConstructorsImpl(filter);
			} else {
				return GetMembersHelper.GetConstructors(this, context, filter, options);
			}
		}
		
		IEnumerable<IMethod> GetConstructorsImpl(Predicate<IMethod> filter)
		{
			bool foundCtor = false;
			foreach (IMethod m in this.Methods) {
				if (m.IsConstructor && !m.IsStatic) {
					foundCtor = true;
					if (filter == null || filter(m)) {
						yield return m;
					}
				}
			}
			
			if (this.AddDefaultConstructorIfRequired) {
				if (kind == TypeKind.Class && !foundCtor && !this.IsStatic
				    || kind == TypeKind.Enum || kind == TypeKind.Struct)
				{
					var m = DefaultMethod.CreateDefaultConstructor(this);
					if (filter == null || filter(m))
						yield return m;
				}
			}
		}
		
		public virtual IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				ITypeDefinition compound = this.compoundTypeDefinition;
				if (compound != this)
					return compound.GetProperties(context, filter, options);
				
				return ApplyFilter(this.Properties, filter);
			} else {
				return GetMembersHelper.GetProperties(this, context, filter, options);
			}
		}
		
		public virtual IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				ITypeDefinition compound = this.compoundTypeDefinition;
				if (compound != this)
					return compound.GetFields(context, filter, options);
				
				return ApplyFilter(this.Fields, filter);
			} else {
				return GetMembersHelper.GetFields(this, context, filter, options);
			}
		}
		
		public virtual IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				ITypeDefinition compound = this.compoundTypeDefinition;
				if (compound != this)
					return compound.GetEvents(context, filter, options);
				
				return ApplyFilter(this.Events, filter);
			} else {
				return GetMembersHelper.GetEvents(this, context, filter, options);
			}
		}
		
		public virtual IEnumerable<IMember> GetMembers(ITypeResolveContext context, Predicate<IMember> filter = null, GetMemberOptions options = GetMemberOptions.None)
		{
			return GetMembersHelper.GetMembers(this, context, filter, options);
		}
		
		static IEnumerable<T> ApplyFilter<T>(IList<T> enumerable, Predicate<T> filter) where T : class
		{
			if (enumerable.Count == 0)
				return EmptyList<T>.Instance;
			if (filter == null)
				return enumerable;
			else
				return ApplyFilterImpl(enumerable, filter);
		}
		
		static IEnumerable<T> ApplyFilterImpl<T>(IList<T> enumerable, Predicate<T> filter) where T : class
		{
			foreach (T item in enumerable)
				if (filter(item))
					yield return item;
		}
		#endregion
		
		#region Equals / GetHashCode
		bool IEquatable<IType>.Equals(IType other)
		{
			// Two ITypeDefinitions are considered to be equal if they have the same compound class.
			ITypeDefinition typeDef = other as ITypeDefinition;
			return typeDef != null && this.GetDefinition() == typeDef.GetDefinition();
		}
		
		public override bool Equals(object obj)
		{
			ITypeDefinition typeDef = obj as ITypeDefinition;
			return typeDef != null && this.GetDefinition() == typeDef.GetDefinition();
		}
		
		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(compoundTypeDefinition);
		}
		#endregion
		
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
