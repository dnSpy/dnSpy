// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Mono.Cecil;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Allows loading an IProjectContent from an already compiled assembly.
	/// </summary>
	/// <remarks>Instance methods are not thread-safe; you need to create multiple instances of CecilLoader
	/// if you want to load multiple project contents in parallel.</remarks>
	public class CecilLoader
	{
		#region Options
		/// <summary>
		/// Gets/Sets the early bind context.
		/// This context is used to pre-resolve type references - setting this property will cause the CecilLoader
		/// to directly reference the resolved types, and create links (<see cref="GetClassTypeReference"/>) to types
		/// that could not be resolved.
		/// </summary>
		public ITypeResolveContext EarlyBindContext { get; set; }
		
		/// <summary>
		/// Specifies whether to include internal members. The default is false.
		/// </summary>
		public bool IncludeInternalMembers { get; set; }
		
		/// <summary>
		/// Gets/Sets the documentation provider that is used to retrive the XML documentation for all members.
		/// </summary>
		public IDocumentationProvider DocumentationProvider { get; set; }
		
		/// <summary>
		/// Gets/Sets the interning provider.
		/// </summary>
		public IInterningProvider InterningProvider { get; set; }
		#endregion
		
		#region Load From AssemblyDefinition
		public IProjectContent LoadAssembly(AssemblyDefinition assemblyDefinition)
		{
			if (assemblyDefinition == null)
				throw new ArgumentNullException("assemblyDefinition");
			ITypeResolveContext oldEarlyBindContext = this.EarlyBindContext;
			try {
				IList<IAttribute> assemblyAttributes = new List<IAttribute>();
				foreach (var attr in assemblyDefinition.CustomAttributes) {
					assemblyAttributes.Add(ReadAttribute(attr));
				}
				if (this.InterningProvider != null)
					assemblyAttributes = this.InterningProvider.InternList(assemblyAttributes);
				else
					assemblyAttributes = new ReadOnlyCollection<IAttribute>(assemblyAttributes);
				TypeStorage typeStorage = new TypeStorage();
				CecilProjectContent pc = new CecilProjectContent(typeStorage, assemblyDefinition.Name.FullName, assemblyAttributes, this.DocumentationProvider);
				
				this.EarlyBindContext = CompositeTypeResolveContext.Combine(pc, this.EarlyBindContext);
				List<CecilTypeDefinition> types = new List<CecilTypeDefinition>();
				foreach (ModuleDefinition module in assemblyDefinition.Modules) {
					foreach (TypeDefinition td in module.Types) {
						if (this.IncludeInternalMembers || (td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public) {
							string name = td.FullName;
							if (name.Length == 0 || name[0] == '<')
								continue;
							if (name == "System.Void") {
								var c = new VoidTypeDefinition(pc);
								AddAttributes(td, c);
								typeStorage.UpdateType(c);
							} else {
								CecilTypeDefinition c = new CecilTypeDefinition(pc, td);
								types.Add(c);
								typeStorage.UpdateType(c);
							}
						}
					}
				}
				foreach (CecilTypeDefinition c in types) {
					c.Init(this);
				}
				return pc;
			} finally {
				this.EarlyBindContext = oldEarlyBindContext;
			}
		}
		
		/// <summary>
		/// Loads a type from Cecil.
		/// </summary>
		/// <param name="typeDefinition">The Cecil TypeDefinition.</param>
		/// <param name="projectContent">The project content used as parent for the new type.</param>
		/// <returns>ITypeDefinition representing the Cecil type.</returns>
		public ITypeDefinition LoadType(TypeDefinition typeDefinition, IProjectContent projectContent)
		{
			if (typeDefinition == null)
				throw new ArgumentNullException("typeDefinition");
			if (projectContent == null)
				throw new ArgumentNullException("projectContent");
			var c = new CecilTypeDefinition(projectContent, typeDefinition);
			c.Init(this);
			return c;
		}
		#endregion
		
		#region IProjectContent implementation
		sealed class CecilProjectContent : ProxyTypeResolveContext, IProjectContent, ISynchronizedTypeResolveContext, IDocumentationProvider
		{
			readonly string assemblyName;
			readonly IList<IAttribute> assemblyAttributes;
			readonly IDocumentationProvider documentationProvider;
			
			public CecilProjectContent(TypeStorage types, string assemblyName, IList<IAttribute> assemblyAttributes, IDocumentationProvider documentationProvider)
				: base(types)
			{
				Debug.Assert(assemblyName != null);
				Debug.Assert(assemblyAttributes != null);
				this.assemblyName = assemblyName;
				this.assemblyAttributes = assemblyAttributes;
				this.documentationProvider = documentationProvider;
			}
			
			public IList<IAttribute> AssemblyAttributes {
				get { return assemblyAttributes; }
			}
			
			public override string ToString()
			{
				return "[CecilProjectContent " + assemblyName + "]";
			}
			
			public override ISynchronizedTypeResolveContext Synchronize()
			{
				// CecilProjectContent is immutable, so we don't need to synchronize
				return this;
			}
			
			public void Dispose()
			{
				// Disposibng the synchronization context has no effect
			}
			
			string IDocumentationProvider.GetDocumentation(IEntity entity)
			{
				if (documentationProvider != null)
					return documentationProvider.GetDocumentation(entity);
				else
					return null;
			}
		}
		#endregion
		
		#region Load Assembly From Disk
		public IProjectContent LoadAssemblyFile(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			var param = new ReaderParameters { AssemblyResolver = new DummyAssemblyResolver() };
			AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(fileName, param);
			return LoadAssembly(asm);
		}
		
		// used to prevent Cecil from loading referenced assemblies
		sealed class DummyAssemblyResolver : IAssemblyResolver
		{
			public AssemblyDefinition Resolve(AssemblyNameReference name)
			{
				return null;
			}
			
			public AssemblyDefinition Resolve(string fullName)
			{
				return null;
			}
			
			public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
			{
				return null;
			}
			
			public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
			{
				return null;
			}
		}
		#endregion
		
		#region Read Type Reference
		/// <summary>
		/// Reads a type reference.
		/// </summary>
		/// <param name="type">The Cecil type reference that should be converted into
		/// a type system type reference.</param>
		/// <param name="typeAttributes">Attributes associated with the Cecil type reference.
		/// This is used to support the 'dynamic' type.</param>
		/// <param name="entity">The entity that owns this type reference.
		/// Used for generic type references.</param>
		public ITypeReference ReadTypeReference(
			TypeReference type,
			ICustomAttributeProvider typeAttributes = null,
			IEntity entity = null)
		{
			int typeIndex = 0;
			return CreateType(type, entity, typeAttributes, ref typeIndex);
		}
		
		ITypeReference CreateType(
			TypeReference type,
			IEntity entity,
			ICustomAttributeProvider typeAttributes, ref int typeIndex)
		{
			while (type is OptionalModifierType || type is RequiredModifierType) {
				type = ((TypeSpecification)type).ElementType;
			}
			if (type == null) {
				return SharedTypes.UnknownType;
			}
			
			if (type is Mono.Cecil.ByReferenceType) {
				typeIndex++;
				return ByReferenceTypeReference.Create(
					CreateType(
						(type as Mono.Cecil.ByReferenceType).ElementType,
						entity, typeAttributes, ref typeIndex));
			} else if (type is Mono.Cecil.PointerType) {
				typeIndex++;
				return PointerTypeReference.Create(
					CreateType(
						(type as Mono.Cecil.PointerType).ElementType,
						entity, typeAttributes, ref typeIndex));
			} else if (type is Mono.Cecil.ArrayType) {
				typeIndex++;
				return ArrayTypeReference.Create(
					CreateType(
						(type as Mono.Cecil.ArrayType).ElementType,
						entity, typeAttributes, ref typeIndex),
					(type as Mono.Cecil.ArrayType).Rank);
			} else if (type is GenericInstanceType) {
				GenericInstanceType gType = (GenericInstanceType)type;
				ITypeReference baseType = CreateType(gType.ElementType, entity, typeAttributes, ref typeIndex);
				ITypeReference[] para = new ITypeReference[gType.GenericArguments.Count];
				for (int i = 0; i < para.Length; ++i) {
					typeIndex++;
					para[i] = CreateType(gType.GenericArguments[i], entity, typeAttributes, ref typeIndex);
				}
				return ParameterizedTypeReference.Create(baseType, para);
			} else if (type is GenericParameter) {
				GenericParameter typeGP = type as GenericParameter;
				if (typeGP.Owner is MethodDefinition) {
					IMethod method = entity as IMethod;
					if (method != null) {
						if (typeGP.Position < method.TypeParameters.Count) {
							return method.TypeParameters[typeGP.Position];
						}
					}
					return SharedTypes.UnknownType;
				} else {
					ITypeDefinition c = (entity as ITypeDefinition) ?? (entity is IMember ? ((IMember)entity).DeclaringTypeDefinition : null);
					if (c != null && typeGP.Position < c.TypeParameters.Count) {
						if (c.TypeParameters[typeGP.Position].Name == type.Name) {
							return c.TypeParameters[typeGP.Position];
						}
					}
					return SharedTypes.UnknownType;
				}
			} else if (type.IsNested) {
				ITypeReference typeRef = CreateType(type.DeclaringType, entity, typeAttributes, ref typeIndex);
				int partTypeParameterCount;
				string namepart = ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name, out partTypeParameterCount);
				return new NestedTypeReference(typeRef, namepart, partTypeParameterCount);
			} else {
				string ns = type.Namespace ?? string.Empty;
				string name = type.Name;
				if (name == null)
					throw new InvalidOperationException("type.Name returned null. Type: " + type.ToString());
				
				if (name == "Object" && ns == "System" && HasDynamicAttribute(typeAttributes, typeIndex)) {
					return SharedTypes.Dynamic;
				} else {
					int typeParameterCount;
					name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name, out typeParameterCount);
					var earlyBindContext = this.EarlyBindContext;
					if (earlyBindContext != null) {
						IType c = earlyBindContext.GetClass(ns, name, typeParameterCount, StringComparer.Ordinal);
						if (c != null)
							return c;
					}
					return new GetClassTypeReference(ns, name, typeParameterCount);
				}
			}
		}
		
		const string DynamicAttributeFullName = "System.Runtime.CompilerServices.DynamicAttribute";
		
		static bool HasDynamicAttribute(ICustomAttributeProvider attributeProvider, int typeIndex)
		{
			if (attributeProvider == null || !attributeProvider.HasCustomAttributes)
				return false;
			foreach (CustomAttribute a in attributeProvider.CustomAttributes) {
				if (a.Constructor.DeclaringType.FullName == DynamicAttributeFullName) {
					if (a.ConstructorArguments.Count == 1) {
						CustomAttributeArgument[] values = a.ConstructorArguments[0].Value as CustomAttributeArgument[];
						if (values != null && typeIndex < values.Length && values[typeIndex].Value is bool)
							return (bool)values[typeIndex].Value;
					}
					return true;
				}
			}
			return false;
		}
		#endregion
		
		#region Read Attributes
		void AddAttributes(ICustomAttributeProvider customAttributeProvider, IEntity targetEntity)
		{
			if (customAttributeProvider.HasCustomAttributes) {
				AddCustomAttributes(customAttributeProvider.CustomAttributes, targetEntity.Attributes);
			}
		}
		
		void AddAttributes(ParameterDefinition parameter, DefaultParameter targetParameter)
		{
			if (parameter.HasCustomAttributes) {
				AddCustomAttributes(parameter.CustomAttributes, targetParameter.Attributes);
			}
		}
		
		void AddAttributes(MethodDefinition accessorMethod, DefaultAccessor targetAccessor)
		{
			if (accessorMethod.HasCustomAttributes) {
				AddCustomAttributes(accessorMethod.CustomAttributes, targetAccessor.Attributes);
			}
		}
		
		static readonly DefaultAttribute serializableAttribute = new DefaultAttribute(typeof(SerializableAttribute).ToTypeReference());
		static readonly ITypeReference structLayoutAttributeTypeRef = typeof(StructLayoutAttribute).ToTypeReference();
		static readonly ITypeReference layoutKindTypeRef = typeof(LayoutKind).ToTypeReference();
		static readonly ITypeReference charSetTypeRef = typeof(CharSet).ToTypeReference();
		
		void AddAttributes(TypeDefinition typeDefinition, ITypeDefinition targetEntity)
		{
			#region SerializableAttribute
			if (typeDefinition.IsSerializable)
				targetEntity.Attributes.Add(serializableAttribute);
			#endregion
			
			#region StructLayoutAttribute
			LayoutKind layoutKind = LayoutKind.Auto;
			switch (typeDefinition.Attributes & TypeAttributes.LayoutMask) {
				case TypeAttributes.SequentialLayout:
					layoutKind = LayoutKind.Sequential;
					break;
				case TypeAttributes.ExplicitLayout:
					layoutKind = LayoutKind.Explicit;
					break;
			}
			CharSet charSet = CharSet.None;
			switch (typeDefinition.Attributes & TypeAttributes.StringFormatMask) {
				case TypeAttributes.AnsiClass:
					charSet = CharSet.Ansi;
					break;
				case TypeAttributes.AutoClass:
					charSet = CharSet.Auto;
					break;
				case TypeAttributes.UnicodeClass:
					charSet = CharSet.Unicode;
					break;
			}
			if (layoutKind != LayoutKind.Auto || charSet != CharSet.Ansi || typeDefinition.PackingSize > 0 || typeDefinition.ClassSize > 0) {
				DefaultAttribute structLayout = new DefaultAttribute(structLayoutAttributeTypeRef);
				structLayout.PositionalArguments.Add(new SimpleConstantValue(layoutKindTypeRef, (int)layoutKind));
				if (charSet != CharSet.Ansi) {
					structLayout.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(
						"CharSet",
						new SimpleConstantValue(charSetTypeRef, (int)charSet)));
				}
				if (typeDefinition.PackingSize > 0) {
					structLayout.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(
						"Pack",
						new SimpleConstantValue(KnownTypeReference.Int32, (int)typeDefinition.PackingSize)));
				}
				if (typeDefinition.ClassSize > 0) {
					structLayout.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(
						"Size",
						new SimpleConstantValue(KnownTypeReference.Int32, (int)typeDefinition.ClassSize)));
				}
				targetEntity.Attributes.Add(structLayout);
			}
			#endregion
			
			if (typeDefinition.HasCustomAttributes) {
				AddCustomAttributes(typeDefinition.CustomAttributes, targetEntity.Attributes);
			}
		}
		
		void AddCustomAttributes(Mono.Collections.Generic.Collection<CustomAttribute> attributes, IList<IAttribute> targetCollection)
		{
			foreach (var cecilAttribute in attributes) {
				if (cecilAttribute.AttributeType.FullName != DynamicAttributeFullName) {
					targetCollection.Add(ReadAttribute(cecilAttribute));
				}
			}
		}
		
		public IAttribute ReadAttribute(CustomAttribute attribute)
		{
			if (attribute == null)
				throw new ArgumentNullException("attribute");
			DefaultAttribute a = new DefaultAttribute(ReadTypeReference(attribute.AttributeType));
			try {
				if (attribute.HasConstructorArguments) {
					foreach (var arg in attribute.ConstructorArguments) {
						a.PositionalArguments.Add(ReadConstantValue(arg));
					}
				}
			} catch (InvalidOperationException) {
				// occurs when Cecil can't decode an argument
			}
			try {
				if (attribute.HasFields || attribute.HasProperties) {
					foreach (var arg in attribute.Fields) {
						a.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(arg.Name, ReadConstantValue(arg.Argument)));
					}
					foreach (var arg in attribute.Properties) {
						a.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(arg.Name, ReadConstantValue(arg.Argument)));
					}
				}
			} catch (InvalidOperationException) {
				// occurs when Cecil can't decode an argument
			}
			return a;
		}
		#endregion
		
		#region Read Constant Value
		public IConstantValue ReadConstantValue(CustomAttributeArgument arg)
		{
			ITypeReference type = ReadTypeReference(arg.Type);
			object value = arg.Value;
			CustomAttributeArgument[] array = value as CustomAttributeArgument[];
			if (array != null) {
				// TODO: write unit test for this
				// TODO: are multi-dimensional arrays possible as well?
				throw new NotImplementedException();
			}
			
			TypeReference valueType = value as TypeReference;
			if (valueType != null)
				value = ReadTypeReference(valueType);
			return new SimpleConstantValue(type, value);
		}
		#endregion
		
		#region Read Type Definition
		sealed class CecilTypeDefinition : DefaultTypeDefinition
		{
			TypeDefinition typeDefinition;
			
			public CecilTypeDefinition(IProjectContent pc, TypeDefinition typeDefinition)
				: base(pc, typeDefinition.Namespace, ReflectionHelper.SplitTypeParameterCountFromReflectionName(typeDefinition.Name))
			{
				this.typeDefinition = typeDefinition;
				InitTypeParameters();
			}
			
			public CecilTypeDefinition(CecilTypeDefinition parentType, string name, TypeDefinition typeDefinition)
				: base(parentType, name)
			{
				this.typeDefinition = typeDefinition;
				InitTypeParameters();
			}
			
			void InitTypeParameters()
			{
				// Type parameters are initialized within the constructor so that the class can be put into the type storage
				// before the rest of the initialization runs - this allows it to be available for early binding as soon as possible.
				for (int i = 0; i < typeDefinition.GenericParameters.Count; i++) {
					if (typeDefinition.GenericParameters[i].Position != i)
						throw new InvalidOperationException("g.Position != i");
					this.TypeParameters.Add(new DefaultTypeParameter(this, i, typeDefinition.GenericParameters[i].Name));
				}
			}
			
			public void Init(CecilLoader loader)
			{
				InitModifiers();
				
				if (typeDefinition.HasGenericParameters) {
					for (int i = 0; i < typeDefinition.GenericParameters.Count; i++) {
						loader.AddConstraints((DefaultTypeParameter)this.TypeParameters[i], typeDefinition.GenericParameters[i]);
					}
				}
				
				InitNestedTypes(loader); // nested types can be initialized only after generic parameters were created
				
				if (typeDefinition.HasCustomAttributes) {
					loader.AddAttributes(typeDefinition, this);
				}
				
				// set base classes
				if (typeDefinition.IsEnum) {
					foreach (FieldDefinition enumField in typeDefinition.Fields) {
						if (!enumField.IsStatic) {
							BaseTypes.Add(loader.ReadTypeReference(enumField.FieldType, entity: this));
							break;
						}
					}
				} else {
					if (typeDefinition.BaseType != null) {
						BaseTypes.Add(loader.ReadTypeReference(typeDefinition.BaseType, entity: this));
					}
					if (typeDefinition.HasInterfaces) {
						foreach (TypeReference iface in typeDefinition.Interfaces) {
							BaseTypes.Add(loader.ReadTypeReference(iface, entity: this));
						}
					}
				}
				
				InitMembers(loader);
				
				this.typeDefinition = null;
				Freeze(); // freeze after initialization
				ApplyInterningProvider(loader.InterningProvider);
			}
			
			void InitNestedTypes(CecilLoader loader)
			{
				if (!typeDefinition.HasNestedTypes)
					return;
				foreach (TypeDefinition nestedType in typeDefinition.NestedTypes) {
					TypeAttributes visibility = nestedType.Attributes & TypeAttributes.VisibilityMask;
					if (loader.IncludeInternalMembers
					    || visibility == TypeAttributes.NestedPublic
					    || visibility == TypeAttributes.NestedFamily
					    || visibility == TypeAttributes.NestedFamORAssem)
					{
						string name = nestedType.Name;
						int pos = name.LastIndexOf('/');
						if (pos > 0)
							name = name.Substring(pos + 1);
						if (name.Length == 0 || name[0] == '<')
							continue;
						name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name);
						InnerClasses.Add(new CecilTypeDefinition(this, name, nestedType));
					}
				}
				foreach (CecilTypeDefinition innerClass in this.InnerClasses) {
					innerClass.Init(loader);
				}
			}
			
			void InitModifiers()
			{
				TypeDefinition td = this.typeDefinition;
				// set classtype
				if (td.IsInterface) {
					this.ClassType = ClassType.Interface;
				} else if (td.IsEnum) {
					this.ClassType = ClassType.Enum;
				} else if (td.IsValueType) {
					this.ClassType = ClassType.Struct;
				} else if (IsDelegate(td)) {
					this.ClassType = ClassType.Delegate;
				} else if (IsModule(td)) {
					this.ClassType = ClassType.Module;
				} else {
					this.ClassType = ClassType.Class;
				}
				this.IsSealed = td.IsSealed;
				this.IsAbstract = td.IsAbstract;
				switch (td.Attributes & TypeAttributes.VisibilityMask) {
					case TypeAttributes.NotPublic:
					case TypeAttributes.NestedAssembly:
						this.Accessibility = Accessibility.Internal;
						break;
					case TypeAttributes.Public:
					case TypeAttributes.NestedPublic:
						this.Accessibility = Accessibility.Public;
						break;
					case TypeAttributes.NestedPrivate:
						this.Accessibility = Accessibility.Private;
						break;
					case TypeAttributes.NestedFamily:
						this.Accessibility = Accessibility.Protected;
						break;
					case TypeAttributes.NestedFamANDAssem:
						this.Accessibility = Accessibility.ProtectedAndInternal;
						break;
					case TypeAttributes.NestedFamORAssem:
						this.Accessibility = Accessibility.ProtectedOrInternal;
						break;
					case TypeAttributes.LayoutMask:
						this.Accessibility = Accessibility;
						break;
				}
			}
			
			static bool IsDelegate(TypeDefinition type)
			{
				if (type.BaseType == null)
					return false;
				else
					return type.BaseType.FullName == "System.Delegate"
						|| type.BaseType.FullName == "System.MulticastDelegate";
			}
			
			static bool IsModule(TypeDefinition type)
			{
				if (!type.HasCustomAttributes)
					return false;
				foreach (var att in type.CustomAttributes) {
					if (att.AttributeType.FullName == "Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute"
					    || att.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGlobalScopeAttribute")
					{
						return true;
					}
				}
				return false;
			}
			
			void InitMembers(CecilLoader loader)
			{
				this.AddDefaultConstructorIfRequired = (this.ClassType == ClassType.Struct || this.ClassType == ClassType.Enum);
				if (typeDefinition.HasMethods) {
					foreach (MethodDefinition method in typeDefinition.Methods) {
						if (loader.IsVisible(method.Attributes)) {
							EntityType type = EntityType.Method;
							if (method.IsSpecialName) {
								if (method.IsConstructor)
									type = EntityType.Constructor;
								else if (method.Name.StartsWith("op_", StringComparison.Ordinal))
									type = EntityType.Operator;
								else
									continue;
							}
							this.Methods.Add(loader.ReadMethod(method, this, type));
						}
					}
				}
				if (typeDefinition.HasFields) {
					foreach (FieldDefinition field in typeDefinition.Fields) {
						if (loader.IsVisible(field.Attributes) && !field.IsSpecialName) {
							this.Fields.Add(loader.ReadField(field, this));
						}
					}
				}
				if (typeDefinition.HasProperties) {
					string defaultMemberName = null;
					var defaultMemberAttribute = typeDefinition.CustomAttributes.FirstOrDefault(
						a => a.AttributeType.FullName == typeof(System.Reflection.DefaultMemberAttribute).FullName);
					if (defaultMemberAttribute != null && defaultMemberAttribute.ConstructorArguments.Count == 1) {
						defaultMemberName = defaultMemberAttribute.ConstructorArguments[0].Value as string;
					}
					foreach (PropertyDefinition property in typeDefinition.Properties) {
						bool getterVisible = property.GetMethod != null && loader.IsVisible(property.GetMethod.Attributes);
						bool setterVisible = property.SetMethod != null && loader.IsVisible(property.SetMethod.Attributes);
						if (getterVisible || setterVisible) {
							EntityType type = property.Name == defaultMemberName ? EntityType.Indexer : EntityType.Property;
							this.Properties.Add(loader.ReadProperty(property, this, type));
						}
					}
				}
				if (typeDefinition.HasEvents) {
					foreach (EventDefinition ev in typeDefinition.Events) {
						if (ev.AddMethod != null && loader.IsVisible(ev.AddMethod.Attributes)) {
							this.Events.Add(loader.ReadEvent(ev, this));
						}
					}
				}
			}
		}
		#endregion
		
		#region Read Method
		public IMethod ReadMethod(MethodDefinition method, ITypeDefinition parentType, EntityType methodType = EntityType.Method)
		{
			DefaultMethod m = new DefaultMethod(parentType, method.Name);
			m.EntityType = methodType;
			if (method.HasGenericParameters) {
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					if (method.GenericParameters[i].Position != i)
						throw new InvalidOperationException("g.Position != i");
					m.TypeParameters.Add(new DefaultTypeParameter(m, i, method.GenericParameters[i].Name));
				}
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					AddConstraints((DefaultTypeParameter)m.TypeParameters[i], method.GenericParameters[i]);
				}
			}
			
			if (method.IsConstructor)
				m.ReturnType = parentType;
			else
				m.ReturnType = ReadTypeReference(method.ReturnType, typeAttributes: method.MethodReturnType, entity: m);
			
			AddAttributes(method, m);
			TranslateModifiers(method, m);
			
			if (method.HasParameters) {
				foreach (ParameterDefinition p in method.Parameters) {
					m.Parameters.Add(ReadParameter(p, parentMember: m));
				}
			}
			
			// mark as extension method is the attribute is set
			if (method.IsStatic && method.HasCustomAttributes) {
				foreach (var attr in method.CustomAttributes) {
					if (attr.AttributeType.FullName == typeof(ExtensionAttribute).FullName)
						m.IsExtensionMethod = true;
				}
			}
			
			FinishReadMember(m);
			return m;
		}
		
		bool IsVisible(MethodAttributes att)
		{
			att &= MethodAttributes.MemberAccessMask;
			return IncludeInternalMembers
				|| att == MethodAttributes.Public
				|| att == MethodAttributes.Family
				|| att == MethodAttributes.FamORAssem;
		}
		
		static Accessibility GetAccessibility(MethodAttributes attr)
		{
			switch (attr & MethodAttributes.MemberAccessMask) {
				case MethodAttributes.Public:
					return Accessibility.Public;
				case MethodAttributes.FamANDAssem:
					return Accessibility.ProtectedAndInternal;
				case MethodAttributes.Assembly:
					return Accessibility.Internal;
				case MethodAttributes.Family:
					return Accessibility.Protected;
				case MethodAttributes.FamORAssem:
					return Accessibility.ProtectedOrInternal;
				default:
					return Accessibility.Private;
			}
		}
		
		void TranslateModifiers(MethodDefinition method, AbstractMember m)
		{
			if (m.DeclaringTypeDefinition.ClassType == ClassType.Interface) {
				// interface members don't have modifiers, but we want to handle them as "public abstract"
				m.Accessibility = Accessibility.Public;
				m.IsAbstract = true;
			} else {
				m.Accessibility = GetAccessibility(method.Attributes);
				if (method.IsAbstract) {
					m.IsAbstract = true;
					m.IsOverride = !method.IsNewSlot;
				} else if (method.IsFinal) {
					if (!method.IsNewSlot) {
						m.IsSealed = true;
						m.IsOverride = true;
					}
				} else if (method.IsVirtual) {
					if (method.IsNewSlot)
						m.IsVirtual = true;
					else
						m.IsOverride = true;
				}
				m.IsStatic = method.IsStatic;
			}
		}
		#endregion
		
		#region Read Parameter
		public IParameter ReadParameter(ParameterDefinition parameter, IParameterizedMember parentMember = null)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");
			var type = ReadTypeReference(parameter.ParameterType, typeAttributes: parameter, entity: parentMember);
			DefaultParameter p = new DefaultParameter(type, parameter.Name);
			
			AddAttributes(parameter, p);
			
			if (parameter.ParameterType is Mono.Cecil.ByReferenceType) {
				if (parameter.IsOut)
					p.IsOut = true;
				else
					p.IsRef = true;
			}
			
			if (parameter.IsOptional) {
				p.DefaultValue = ReadConstantValue(new CustomAttributeArgument(parameter.ParameterType, parameter.Constant));
			}
			
			if (parameter.ParameterType is Mono.Cecil.ArrayType) {
				foreach (CustomAttribute att in parameter.CustomAttributes) {
					if (att.AttributeType.FullName == typeof(ParamArrayAttribute).FullName) {
						p.IsParams = true;
						break;
					}
				}
			}
			
			return p;
		}
		#endregion
		
		#region Read Field
		bool IsVisible(FieldAttributes att)
		{
			att &= FieldAttributes.FieldAccessMask;
			return IncludeInternalMembers
				|| att == FieldAttributes.Public
				|| att == FieldAttributes.Family
				|| att == FieldAttributes.FamORAssem;
		}
		
		public IField ReadField(FieldDefinition field, ITypeDefinition parentType)
		{
			if (field == null)
				throw new ArgumentNullException("field");
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			
			DefaultField f = new DefaultField(parentType, field.Name);
			f.Accessibility = GetAccessibility(field.Attributes);
			f.IsReadOnly = field.IsInitOnly;
			f.IsStatic = field.IsStatic;
			if (field.HasConstant) {
				f.ConstantValue = ReadConstantValue(new CustomAttributeArgument(field.FieldType, field.Constant));
			}
			AddAttributes(field, f);
			
			f.ReturnType = ReadTypeReference(field.FieldType, typeAttributes: field, entity: f);
			RequiredModifierType modreq = field.FieldType as RequiredModifierType;
			if (modreq != null && modreq.ModifierType.FullName == typeof(IsVolatile).FullName) {
				f.IsVolatile = true;
			}
			
			FinishReadMember(f);
			return f;
		}
		
		static Accessibility GetAccessibility(FieldAttributes attr)
		{
			switch (attr & FieldAttributes.FieldAccessMask) {
				case FieldAttributes.Public:
					return Accessibility.Public;
				case FieldAttributes.FamANDAssem:
					return Accessibility.ProtectedAndInternal;
				case FieldAttributes.Assembly:
					return Accessibility.Internal;
				case FieldAttributes.Family:
					return Accessibility.Protected;
				case FieldAttributes.FamORAssem:
					return Accessibility.ProtectedOrInternal;
				default:
					return Accessibility.Private;
			}
		}
		#endregion
		
		#region Type Parameter Constraints
		void AddConstraints(DefaultTypeParameter tp, GenericParameter g)
		{
			switch (g.Attributes & GenericParameterAttributes.VarianceMask) {
				case GenericParameterAttributes.Contravariant:
					tp.Variance = VarianceModifier.Contravariant;
					break;
				case GenericParameterAttributes.Covariant:
					tp.Variance = VarianceModifier.Covariant;
					break;
			}
			
			tp.HasDefaultConstructorConstraint = g.HasReferenceTypeConstraint;
			tp.HasValueTypeConstraint = g.HasNotNullableValueTypeConstraint;
			tp.HasDefaultConstructorConstraint = g.HasDefaultConstructorConstraint;
			
			if (g.HasConstraints) {
				foreach (TypeReference constraint in g.Constraints) {
					tp.Constraints.Add(ReadTypeReference(constraint, entity: tp.Parent));
				}
			}
		}
		#endregion
		
		#region Read Property
		public IProperty ReadProperty(PropertyDefinition property, ITypeDefinition parentType, EntityType propertyType = EntityType.Property)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			DefaultProperty p = new DefaultProperty(parentType, property.Name);
			p.EntityType = propertyType;
			TranslateModifiers(property.GetMethod ?? property.SetMethod, p);
			p.ReturnType = ReadTypeReference(property.PropertyType, typeAttributes: property, entity: p);
			
			p.Getter = ReadAccessor(property.GetMethod);
			p.Setter = ReadAccessor(property.SetMethod);
			
			if (property.HasParameters) {
				foreach (ParameterDefinition par in property.Parameters) {
					p.Parameters.Add(ReadParameter(par, parentMember: p));
				}
			}
			AddAttributes(property, p);
			
			FinishReadMember(p);
			return p;
		}
		
		IAccessor ReadAccessor(MethodDefinition accessorMethod)
		{
			if (accessorMethod != null && IsVisible(accessorMethod.Attributes)) {
				Accessibility accessibility = GetAccessibility(accessorMethod.Attributes);
				if (accessorMethod.HasCustomAttributes) {
					DefaultAccessor a = new DefaultAccessor();
					a.Accessibility = accessibility;
					AddAttributes(accessorMethod, a);
					return a;
				} else {
					return DefaultAccessor.GetFromAccessibility(accessibility);
				}
			} else {
				return null;
			}
		}
		#endregion
		
		#region Read Event
		public IEvent ReadEvent(EventDefinition ev, ITypeDefinition parentType)
		{
			if (ev == null)
				throw new ArgumentNullException("ev");
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			
			DefaultEvent e = new DefaultEvent(parentType, ev.Name);
			TranslateModifiers(ev.AddMethod, e);
			e.ReturnType = ReadTypeReference(ev.EventType, typeAttributes: ev, entity: e);
			
			e.AddAccessor = ReadAccessor(ev.AddMethod);
			e.RemoveAccessor = ReadAccessor(ev.RemoveMethod);
			e.InvokeAccessor = ReadAccessor(ev.InvokeMethod);
			
			AddAttributes(ev, e);
			
			FinishReadMember(e);
			return e;
		}
		#endregion
		
		void FinishReadMember(AbstractMember member)
		{
			member.Freeze();
			member.ApplyInterningProvider(this.InterningProvider);
		}
	}
}
