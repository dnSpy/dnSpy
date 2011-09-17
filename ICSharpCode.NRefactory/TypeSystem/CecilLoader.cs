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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
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
		/// Gets/Sets the documentation provider that is used to retrieve the XML documentation for all members.
		/// </summary>
		public IDocumentationProvider DocumentationProvider { get; set; }
		
		/// <summary>
		/// Gets/Sets the interning provider.
		/// </summary>
		public IInterningProvider InterningProvider { get; set; }
		
		/// <summary>
		/// Gets/Sets the cancellation token used by the cecil loader.
		/// </summary>
		public CancellationToken CancellationToken { get; set; }
		
		/// <summary>
		/// Gets a value indicating whether this instance stores references to the cecil objects.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance has references to the cecil objects; otherwise, <c>false</c>.
		/// </value>
		public bool HasCecilReferences { get { return typeSystemTranslationTable != null; } }
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.CecilLoader"/> class.
		/// </summary>
		/// <param name='createCecilReferences'>
		/// If true references to the cecil objects are hold. In this case the cecil loader can do a type system -> cecil mapping.
		/// </param>
		public CecilLoader (bool createCecilReferences = false)
		{
			if (createCecilReferences)
				typeSystemTranslationTable = new Dictionary<object, object> ();
			
			// Enable interning by default.
			this.InterningProvider = new SimpleInterningProvider();
		}
		
		#region Load From AssemblyDefinition
		/// <summary>
		/// Loads the assembly definition into a project content.
		/// </summary>
		/// <returns>IProjectContent that represents the assembly</returns>
		public IProjectContent LoadAssembly(AssemblyDefinition assemblyDefinition)
		{
			if (assemblyDefinition == null)
				throw new ArgumentNullException("assemblyDefinition");
			ITypeResolveContext oldEarlyBindContext = this.EarlyBindContext;
			try {
				// Read assembly and module attributes
				IList<IAttribute> assemblyAttributes = new List<IAttribute>();
				IList<IAttribute> moduleAttributes = new List<IAttribute>();
				AddAttributes(assemblyDefinition, assemblyAttributes);
				AddAttributes(assemblyDefinition.MainModule, moduleAttributes);
				
				if (this.InterningProvider != null) {
					assemblyAttributes = this.InterningProvider.InternList(assemblyAttributes);
					moduleAttributes = this.InterningProvider.InternList(moduleAttributes);
				} else {
					assemblyAttributes = new ReadOnlyCollection<IAttribute>(assemblyAttributes);
					moduleAttributes = new ReadOnlyCollection<IAttribute>(moduleAttributes);
				}
				TypeStorage typeStorage = new TypeStorage();
				CecilProjectContent pc = new CecilProjectContent(typeStorage, assemblyDefinition.Name.FullName, assemblyAttributes, moduleAttributes, this.DocumentationProvider);
				
				this.EarlyBindContext = CompositeTypeResolveContext.Combine(pc, this.EarlyBindContext);
				List<CecilTypeDefinition> types = new List<CecilTypeDefinition>();
				foreach (ModuleDefinition module in assemblyDefinition.Modules) {
					foreach (TypeDefinition td in module.Types) {
						this.CancellationToken.ThrowIfCancellationRequested();
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
				if (HasCecilReferences)
					typeSystemTranslationTable[pc] = assemblyDefinition;
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
		[Serializable]
		sealed class CecilProjectContent : ProxyTypeResolveContext, IProjectContent, ISynchronizedTypeResolveContext, IDocumentationProvider
		{
			readonly string assemblyName;
			readonly IList<IAttribute> assemblyAttributes;
			readonly IList<IAttribute> moduleAttributes;
			readonly IDocumentationProvider documentationProvider;
			
			public CecilProjectContent(TypeStorage types, string assemblyName, IList<IAttribute> assemblyAttributes, IList<IAttribute> moduleAttributes, IDocumentationProvider documentationProvider)
				: base(types)
			{
				Debug.Assert(assemblyName != null);
				Debug.Assert(assemblyAttributes != null);
				Debug.Assert(moduleAttributes != null);
				this.assemblyName = assemblyName;
				this.assemblyAttributes = assemblyAttributes;
				this.moduleAttributes = moduleAttributes;
				this.documentationProvider = documentationProvider;
			}
			
			public IList<IAttribute> AssemblyAttributes {
				get { return assemblyAttributes; }
			}
			
			public IList<IAttribute> ModuleAttributes {
				get { return moduleAttributes; }
			}
			
			public string AssemblyName {
				get { return assemblyName; }
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
			
			void IDisposable.Dispose()
			{
				// Disposing the synchronization context has no effect
			}
			
			IParsedFile IProjectContent.GetFile(string fileName)
			{
				return null;
			}
			
			IEnumerable<IParsedFile> IProjectContent.Files {
				get {
					return EmptyList<IParsedFile>.Instance;
				}
			}
			
			void IProjectContent.UpdateProjectContent(IParsedFile oldFile, IParsedFile newFile)
			{
				throw new NotSupportedException();
			}
			
			string IDocumentationProvider.GetDocumentation(IEntity entity)
			{
				if (documentationProvider != null)
					return documentationProvider.GetDocumentation(entity);
				else
					return null;
			}
			
			IEnumerable<object> IAnnotatable.Annotations {
				get { return EmptyList<object>.Instance; }
			}
			
			T IAnnotatable.Annotation<T>()
			{
				return null;
			}
			
			object IAnnotatable.Annotation(Type type)
			{
				return null;
			}
			
			void IAnnotatable.AddAnnotation(object annotation)
			{
				throw new NotSupportedException();
			}
			
			void IAnnotatable.RemoveAnnotations<T>()
			{
			}
			
			void IAnnotatable.RemoveAnnotations(Type type)
			{
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
			var result = LoadAssembly(asm);
			if (HasCecilReferences)
				typeSystemTranslationTable[result] = asm;
			return result;
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
						IType c = earlyBindContext.GetTypeDefinition(ns, name, typeParameterCount, StringComparer.Ordinal);
						if (c != null)
							return c;
					}
					return new GetClassTypeReference(ns, name, typeParameterCount);
				}
			}
		}
		
		static bool HasDynamicAttribute(ICustomAttributeProvider attributeProvider, int typeIndex)
		{
			if (attributeProvider == null || !attributeProvider.HasCustomAttributes)
				return false;
			foreach (CustomAttribute a in attributeProvider.CustomAttributes) {
				TypeReference type = a.AttributeType;
				if (type.Name == "DynamicAttribute" && type.Namespace == "System.Runtime.CompilerServices") {
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
		#region Assembly Attributes
		static readonly ITypeReference typeForwardedToAttributeTypeRef = typeof(TypeForwardedToAttribute).ToTypeReference();
		static readonly ITypeReference assemblyVersionAttributeTypeRef = typeof(System.Reflection.AssemblyVersionAttribute).ToTypeReference();
		
		void AddAttributes(AssemblyDefinition assembly, IList<IAttribute> outputList)
		{
			if (assembly.HasCustomAttributes) {
				AddCustomAttributes(assembly.CustomAttributes, outputList);
			}
			if (assembly.HasSecurityDeclarations) {
				AddSecurityAttributes(assembly.SecurityDeclarations, outputList);
			}
			
			// AssemblyVersionAttribute
			if (assembly.Name.Version != null) {
				var assemblyVersion = new DefaultAttribute(assemblyVersionAttributeTypeRef, new[] { KnownTypeReference.String });
				assemblyVersion.PositionalArguments.Add(new SimpleConstantValue(KnownTypeReference.String, assembly.Name.Version.ToString()));
				outputList.Add(assemblyVersion);
			}
			
			// TypeForwardedToAttribute
			foreach (ExportedType type in assembly.MainModule.ExportedTypes) {
				if (type.IsForwarder) {
					int typeParameterCount;
					string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name, out typeParameterCount);
					var typeForwardedTo = new DefaultAttribute(typeForwardedToAttributeTypeRef, new[] { KnownTypeReference.Type });
					var typeRef = new GetClassTypeReference(type.Namespace, name, typeParameterCount);
					typeForwardedTo.PositionalArguments.Add(new SimpleConstantValue(KnownTypeReference.Type, typeRef));
					outputList.Add(typeForwardedTo);
				}
			}
		}
		#endregion
		
		#region Module Attributes
		void AddAttributes(ModuleDefinition module, IList<IAttribute> outputList)
		{
			if (module.HasCustomAttributes) {
				AddCustomAttributes(module.CustomAttributes, outputList);
			}
		}
		#endregion
		
		#region Parameter Attributes
		static readonly IAttribute inAttribute = new DefaultAttribute(typeof(InAttribute).ToTypeReference(), null);
		static readonly IAttribute outAttribute = new DefaultAttribute(typeof(OutAttribute).ToTypeReference(), null);
		
		void AddAttributes(ParameterDefinition parameter, DefaultParameter targetParameter)
		{
			if (!targetParameter.IsOut) {
				if (parameter.IsIn)
					targetParameter.Attributes.Add(inAttribute);
				if (parameter.IsOut)
					targetParameter.Attributes.Add(outAttribute);
			}
			if (parameter.HasCustomAttributes) {
				AddCustomAttributes(parameter.CustomAttributes, targetParameter.Attributes);
			}
			if (parameter.HasMarshalInfo) {
				targetParameter.Attributes.Add(ConvertMarshalInfo(parameter.MarshalInfo));
			}
		}
		#endregion
		
		#region Method Attributes
		static readonly ITypeReference dllImportAttributeTypeRef = typeof(DllImportAttribute).ToTypeReference();
		static readonly SimpleConstantValue trueValue = new SimpleConstantValue(KnownTypeReference.Boolean, true);
		static readonly SimpleConstantValue falseValue = new SimpleConstantValue(KnownTypeReference.Boolean, true);
		static readonly ITypeReference callingConventionTypeRef = typeof(CallingConvention).ToTypeReference();
		static readonly IAttribute preserveSigAttribute = new DefaultAttribute(typeof(PreserveSigAttribute).ToTypeReference(), null);
		static readonly ITypeReference methodImplAttributeTypeRef = typeof(MethodImplAttribute).ToTypeReference();
		static readonly ITypeReference methodImplOptionsTypeRef = typeof(MethodImplOptions).ToTypeReference();
		
		bool HasAnyAttributes(MethodDefinition methodDefinition)
		{
			if (methodDefinition.HasPInvokeInfo)
				return true;
			if ((methodDefinition.ImplAttributes & ~MethodImplAttributes.CodeTypeMask) != 0)
				return true;
			if (methodDefinition.MethodReturnType.HasFieldMarshal)
				return true;
			return methodDefinition.HasCustomAttributes || methodDefinition.MethodReturnType.HasCustomAttributes;
		}
		
		void AddAttributes(MethodDefinition methodDefinition, IList<IAttribute> attributes, IList<IAttribute> returnTypeAttributes)
		{
			MethodImplAttributes implAttributes = methodDefinition.ImplAttributes & ~MethodImplAttributes.CodeTypeMask;
			
			#region DllImportAttribute
			if (methodDefinition.HasPInvokeInfo) {
				PInvokeInfo info = methodDefinition.PInvokeInfo;
				DefaultAttribute dllImport = new DefaultAttribute(dllImportAttributeTypeRef, new[] { KnownTypeReference.String });
				dllImport.PositionalArguments.Add(new SimpleConstantValue(KnownTypeReference.String, info.Module.Name));
				
				if (info.IsBestFitDisabled)
					dllImport.AddNamedArgument("BestFitMapping", falseValue);
				if (info.IsBestFitEnabled)
					dllImport.AddNamedArgument("BestFitMapping", trueValue);
				
				CallingConvention callingConvention;
				switch (info.Attributes & PInvokeAttributes.CallConvMask) {
					case PInvokeAttributes.CallConvCdecl:
						callingConvention = CallingConvention.Cdecl;
						break;
					case PInvokeAttributes.CallConvFastcall:
						callingConvention = CallingConvention.FastCall;
						break;
					case PInvokeAttributes.CallConvStdCall:
						callingConvention = CallingConvention.StdCall;
						break;
					case PInvokeAttributes.CallConvThiscall:
						callingConvention = CallingConvention.ThisCall;
						break;
					case PInvokeAttributes.CallConvWinapi:
						callingConvention = CallingConvention.Winapi;
						break;
					default:
						throw new NotSupportedException("unknown calling convention");
				}
				if (callingConvention != CallingConvention.Winapi)
					dllImport.AddNamedArgument("CallingConvention", callingConventionTypeRef, (int)callingConvention);
				
				CharSet charSet = CharSet.None;
				switch (info.Attributes & PInvokeAttributes.CharSetMask) {
					case PInvokeAttributes.CharSetAnsi:
						charSet = CharSet.Ansi;
						break;
					case PInvokeAttributes.CharSetAuto:
						charSet = CharSet.Auto;
						break;
					case PInvokeAttributes.CharSetUnicode:
						charSet = CharSet.Unicode;
						break;
				}
				if (charSet != CharSet.None)
					dllImport.AddNamedArgument("CharSet", charSetTypeRef, (int)charSet);
				
				if (!string.IsNullOrEmpty(info.EntryPoint) && info.EntryPoint != methodDefinition.Name)
					dllImport.AddNamedArgument("EntryPoint", KnownTypeReference.String, info.EntryPoint);
				
				if (info.IsNoMangle)
					dllImport.AddNamedArgument("ExactSpelling", trueValue);
				
				if ((implAttributes & MethodImplAttributes.PreserveSig) == MethodImplAttributes.PreserveSig)
					implAttributes &= ~MethodImplAttributes.PreserveSig;
				else
					dllImport.AddNamedArgument("PreserveSig", falseValue);
				
				if (info.SupportsLastError)
					dllImport.AddNamedArgument("SetLastError", trueValue);
				
				if (info.IsThrowOnUnmappableCharDisabled)
					dllImport.AddNamedArgument("ThrowOnUnmappableChar", falseValue);
				if (info.IsThrowOnUnmappableCharEnabled)
					dllImport.AddNamedArgument("ThrowOnUnmappableChar", trueValue);
				
				attributes.Add(dllImport);
			}
			#endregion
			
			#region PreserveSigAttribute
			if (implAttributes == MethodImplAttributes.PreserveSig) {
				attributes.Add(preserveSigAttribute);
				implAttributes = 0;
			}
			#endregion
			
			#region MethodImplAttribute
			if (implAttributes != 0) {
				DefaultAttribute methodImpl = new DefaultAttribute(methodImplAttributeTypeRef, new[] { methodImplOptionsTypeRef });
				methodImpl.PositionalArguments.Add(new SimpleConstantValue(methodImplOptionsTypeRef, (int)implAttributes));
				attributes.Add(methodImpl);
			}
			#endregion
			
			if (methodDefinition.HasCustomAttributes) {
				AddCustomAttributes(methodDefinition.CustomAttributes, attributes);
			}
			if (methodDefinition.HasSecurityDeclarations) {
				AddSecurityAttributes(methodDefinition.SecurityDeclarations, attributes);
			}
			if (methodDefinition.MethodReturnType.HasMarshalInfo) {
				returnTypeAttributes.Add(ConvertMarshalInfo(methodDefinition.MethodReturnType.MarshalInfo));
			}
			if (methodDefinition.MethodReturnType.HasCustomAttributes) {
				AddCustomAttributes(methodDefinition.MethodReturnType.CustomAttributes, returnTypeAttributes);
			}
		}
		#endregion
		
		#region Type Attributes
		static readonly DefaultAttribute serializableAttribute = new DefaultAttribute(typeof(SerializableAttribute).ToTypeReference(), null);
		static readonly DefaultAttribute comImportAttribute = new DefaultAttribute(typeof(ComImportAttribute).ToTypeReference(), null);
		static readonly ITypeReference structLayoutAttributeTypeRef = typeof(StructLayoutAttribute).ToTypeReference();
		static readonly ITypeReference layoutKindTypeRef = typeof(LayoutKind).ToTypeReference();
		static readonly ITypeReference charSetTypeRef = typeof(CharSet).ToTypeReference();
		
		void AddAttributes(TypeDefinition typeDefinition, ITypeDefinition targetEntity)
		{
			// SerializableAttribute
			if (typeDefinition.IsSerializable)
				targetEntity.Attributes.Add(serializableAttribute);
			
			// ComImportAttribute
			if (typeDefinition.IsImport)
				targetEntity.Attributes.Add(comImportAttribute);
			
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
			LayoutKind defaultLayoutKind = (typeDefinition.IsValueType && !typeDefinition.IsEnum) ? LayoutKind.Sequential: LayoutKind.Auto;
			if (layoutKind != defaultLayoutKind || charSet != CharSet.Ansi || typeDefinition.PackingSize > 0 || typeDefinition.ClassSize > 0) {
				DefaultAttribute structLayout = new DefaultAttribute(structLayoutAttributeTypeRef, new[] { layoutKindTypeRef });
				structLayout.PositionalArguments.Add(new SimpleConstantValue(layoutKindTypeRef, (int)layoutKind));
				if (charSet != CharSet.Ansi) {
					structLayout.AddNamedArgument("CharSet", charSetTypeRef, (int)charSet);
				}
				if (typeDefinition.PackingSize > 0) {
					structLayout.AddNamedArgument("Pack", KnownTypeReference.Int32, (int)typeDefinition.PackingSize);
				}
				if (typeDefinition.ClassSize > 0) {
					structLayout.AddNamedArgument("Size", KnownTypeReference.Int32, (int)typeDefinition.ClassSize);
				}
				targetEntity.Attributes.Add(structLayout);
			}
			#endregion
			
			if (typeDefinition.HasCustomAttributes) {
				AddCustomAttributes(typeDefinition.CustomAttributes, targetEntity.Attributes);
			}
			if (typeDefinition.HasSecurityDeclarations) {
				AddSecurityAttributes(typeDefinition.SecurityDeclarations, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region Field Attributes
		static readonly ITypeReference fieldOffsetAttributeTypeRef = typeof(FieldOffsetAttribute).ToTypeReference();
		static readonly DefaultAttribute nonSerializedAttribute = new DefaultAttribute(typeof(NonSerializedAttribute).ToTypeReference(), null);
		
		void AddAttributes(FieldDefinition fieldDefinition, IEntity targetEntity)
		{
			// FieldOffsetAttribute
			if (fieldDefinition.HasLayoutInfo) {
				DefaultAttribute fieldOffset = new DefaultAttribute(fieldOffsetAttributeTypeRef, new[] { KnownTypeReference.Int32 });
				fieldOffset.PositionalArguments.Add(new SimpleConstantValue(KnownTypeReference.Int32, fieldDefinition.Offset));
				targetEntity.Attributes.Add(fieldOffset);
			}
			
			// NonSerializedAttribute
			if (fieldDefinition.IsNotSerialized) {
				targetEntity.Attributes.Add(nonSerializedAttribute);
			}
			
			if (fieldDefinition.HasMarshalInfo) {
				targetEntity.Attributes.Add(ConvertMarshalInfo(fieldDefinition.MarshalInfo));
			}
			
			if (fieldDefinition.HasCustomAttributes) {
				AddCustomAttributes(fieldDefinition.CustomAttributes, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region Event Attributes
		void AddAttributes(EventDefinition eventDefinition, IEntity targetEntity)
		{
			if (eventDefinition.HasCustomAttributes) {
				AddCustomAttributes(eventDefinition.CustomAttributes, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region Property Attributes
		void AddAttributes(PropertyDefinition propertyDefinition, IEntity targetEntity)
		{
			if (propertyDefinition.HasCustomAttributes) {
				AddCustomAttributes(propertyDefinition.CustomAttributes, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region MarshalAsAttribute (ConvertMarshalInfo)
		static readonly ITypeReference marshalAsAttributeTypeRef = typeof(MarshalAsAttribute).ToTypeReference();
		static readonly ITypeReference unmanagedTypeTypeRef = typeof(UnmanagedType).ToTypeReference();
		
		static IAttribute ConvertMarshalInfo(MarshalInfo marshalInfo)
		{
			DefaultAttribute attr = new DefaultAttribute(marshalAsAttributeTypeRef, new[] { unmanagedTypeTypeRef });
			attr.PositionalArguments.Add(new SimpleConstantValue(unmanagedTypeTypeRef, (int)marshalInfo.NativeType));
			
			FixedArrayMarshalInfo fami = marshalInfo as FixedArrayMarshalInfo;
			if (fami != null) {
				attr.AddNamedArgument("SizeConst", KnownTypeReference.Int32, (int)fami.Size);
				if (fami.ElementType != NativeType.None)
					attr.AddNamedArgument("ArraySubType", unmanagedTypeTypeRef, (int)fami.ElementType);
			}
			SafeArrayMarshalInfo sami = marshalInfo as SafeArrayMarshalInfo;
			if (sami != null && sami.ElementType != VariantType.None) {
				attr.AddNamedArgument("SafeArraySubType", typeof(VarEnum).ToTypeReference(), (int)sami.ElementType);
			}
			ArrayMarshalInfo ami = marshalInfo as ArrayMarshalInfo;
			if (ami != null) {
				if (ami.ElementType != NativeType.Max)
					attr.AddNamedArgument("ArraySubType", unmanagedTypeTypeRef, (int)ami.ElementType);
				if (ami.Size >= 0)
					attr.AddNamedArgument("SizeConst", KnownTypeReference.Int32, (int)ami.Size);
				if (ami.SizeParameterMultiplier != 0 && ami.SizeParameterIndex >= 0)
					attr.AddNamedArgument("SizeParamIndex", KnownTypeReference.Int16, (short)ami.SizeParameterIndex);
			}
			CustomMarshalInfo cmi = marshalInfo as CustomMarshalInfo;
			if (cmi != null) {
				attr.AddNamedArgument("MarshalType", KnownTypeReference.String, cmi.ManagedType.FullName);
				if (!string.IsNullOrEmpty(cmi.Cookie))
					attr.AddNamedArgument("MarshalCookie", KnownTypeReference.String, cmi.Cookie);
			}
			FixedSysStringMarshalInfo fssmi = marshalInfo as FixedSysStringMarshalInfo;
			if (fssmi != null) {
				attr.AddNamedArgument("SizeConst", KnownTypeReference.Int32, (int)fssmi.Size);
			}
			
			return attr;
		}
		#endregion
		
		#region Custom Attributes (ReadAttribute)
		void AddCustomAttributes(Mono.Collections.Generic.Collection<CustomAttribute> attributes, IList<IAttribute> targetCollection)
		{
			foreach (var cecilAttribute in attributes) {
				TypeReference type = cecilAttribute.AttributeType;
				if (type.Namespace == "System.Runtime.CompilerServices") {
					if (type.Name == "DynamicAttribute" || type.Name == "ExtensionAttribute")
						continue;
				} else if (type.Name == "ParamArrayAttribute" && type.Namespace == "System") {
					continue;
				}
				targetCollection.Add(ReadAttribute(cecilAttribute));
			}
		}
		
		public IAttribute ReadAttribute(CustomAttribute attribute)
		{
			if (attribute == null)
				throw new ArgumentNullException("attribute");
			MethodReference ctor = attribute.Constructor;
			ITypeReference[] ctorParameters = null;
			if (ctor.HasParameters) {
				ctorParameters = new ITypeReference[ctor.Parameters.Count];
				for (int i = 0; i < ctorParameters.Length; i++) {
					ctorParameters[i] = ReadTypeReference(ctor.Parameters[i].ParameterType);
				}
			}
			DefaultAttribute a = new DefaultAttribute(ReadTypeReference(attribute.AttributeType), ctorParameters);
			if (attribute.HasConstructorArguments) {
				foreach (var arg in attribute.ConstructorArguments) {
					a.PositionalArguments.Add(ReadConstantValue(arg));
				}
			}
			if (attribute.HasFields || attribute.HasProperties) {
				foreach (var arg in attribute.Fields) {
					a.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(arg.Name, ReadConstantValue(arg.Argument)));
				}
				foreach (var arg in attribute.Properties) {
					a.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(arg.Name, ReadConstantValue(arg.Argument)));
				}
			}
			return a;
		}
		#endregion
		
		#region Security Attributes
		static readonly ITypeReference securityActionTypeReference = typeof(SecurityAction).ToTypeReference();
		
		void AddSecurityAttributes(Mono.Collections.Generic.Collection<SecurityDeclaration> securityDeclarations, IList<IAttribute> targetCollection)
		{
			foreach (var secDecl in securityDeclarations) {
				try {
					foreach (var secAttribute in secDecl.SecurityAttributes) {
						ITypeReference attributeType = ReadTypeReference(secAttribute.AttributeType);
						var a = new DefaultAttribute(attributeType, new[] { securityActionTypeReference });
						a.PositionalArguments.Add(new SimpleConstantValue(securityActionTypeReference, (ushort)secDecl.Action));
						
						if (secAttribute.HasFields || secAttribute.HasProperties) {
							foreach (var arg in secAttribute.Fields) {
								a.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(arg.Name, ReadConstantValue(arg.Argument)));
							}
							foreach (var arg in secAttribute.Properties) {
								a.NamedArguments.Add(new KeyValuePair<string, IConstantValue>(arg.Name, ReadConstantValue(arg.Argument)));
							}
						}
						targetCollection.Add(a);
					}
				} catch (ResolutionException) {
					// occurs when Cecil can't decode an argument
				}
			}
		}
		#endregion
		#endregion
		
		#region Read Constant Value
		public IConstantValue ReadConstantValue(CustomAttributeArgument arg)
		{
			object value = arg.Value;
			if (value is CustomAttributeArgument) {
				// Cecil uses this representation for boxed values
				arg = (CustomAttributeArgument)value;
				value = arg.Value;
			}
			ITypeReference type = ReadTypeReference(arg.Type);
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
		[Serializable]
		sealed class CecilTypeDefinition : DefaultTypeDefinition
		{
			[NonSerialized]
			internal TypeDefinition typeDefinition;
			
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
					this.TypeParameters.Add(new DefaultTypeParameter(
						EntityType.TypeDefinition, i, typeDefinition.GenericParameters[i].Name));
				}
			}
			
			public void Init(CecilLoader loader)
			{
				loader.CancellationToken.ThrowIfCancellationRequested();
				InitModifiers();
				
				if (typeDefinition.HasGenericParameters) {
					for (int i = 0; i < typeDefinition.GenericParameters.Count; i++) {
						loader.AddConstraints(this, (DefaultTypeParameter)this.TypeParameters[i], typeDefinition.GenericParameters[i]);
					}
				}
				
				InitNestedTypes(loader); // nested types can be initialized only after generic parameters were created
				
				loader.AddAttributes(typeDefinition, this);
				
				this.HasExtensionMethods = HasExtensionAttribute(typeDefinition);
				
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
				if (!loader.HasCecilReferences)
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
						NestedTypes.Add(new CecilTypeDefinition(this, name, nestedType));
					}
				}
				foreach (CecilTypeDefinition nestedType in this.NestedTypes) {
					nestedType.Init(loader);
				}
			}
			
			void InitModifiers()
			{
				TypeDefinition td = this.typeDefinition;
				// set classtype
				if (td.IsInterface) {
					this.Kind = TypeKind.Interface;
				} else if (td.IsEnum) {
					this.Kind = TypeKind.Enum;
				} else if (td.IsValueType) {
					this.Kind = TypeKind.Struct;
				} else if (IsDelegate(td)) {
					this.Kind = TypeKind.Delegate;
				} else if (IsModule(td)) {
					this.Kind = TypeKind.Module;
				} else {
					this.Kind = TypeKind.Class;
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
				this.AddDefaultConstructorIfRequired = (this.Kind == TypeKind.Struct || this.Kind == TypeKind.Enum);
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
					m.TypeParameters.Add(new DefaultTypeParameter(
						EntityType.Method, i, method.GenericParameters[i].Name));
				}
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					AddConstraints(m, (DefaultTypeParameter)m.TypeParameters[i], method.GenericParameters[i]);
				}
			}
			
			if (method.IsConstructor)
				m.ReturnType = parentType;
			else
				m.ReturnType = ReadTypeReference(method.ReturnType, typeAttributes: method.MethodReturnType, entity: m);
			
			if (HasAnyAttributes(method))
				AddAttributes(method, m.Attributes, m.ReturnTypeAttributes);
			TranslateModifiers(method, m);
			
			if (method.HasParameters) {
				foreach (ParameterDefinition p in method.Parameters) {
					m.Parameters.Add(ReadParameter(p, parentMember: m));
				}
			}
			
			// mark as extension method if the attribute is set
			if (method.IsStatic && HasExtensionAttribute(method)) {
				m.IsExtensionMethod = true;
			}
			
			FinishReadMember(m, method);
			return m;
		}
		
		static bool HasExtensionAttribute(ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes) {
				foreach (var attr in provider.CustomAttributes) {
					if (attr.AttributeType.Name == "ExtensionAttribute" && attr.AttributeType.Namespace == "System.Runtime.CompilerServices")
						return true;
				}
			}
			return false;
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
			if (m.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
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
			
			if (parameter.ParameterType is Mono.Cecil.ByReferenceType) {
				if (!parameter.IsIn && parameter.IsOut)
					p.IsOut = true;
				else
					p.IsRef = true;
			}
			AddAttributes(parameter, p);
			
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
			
			FinishReadMember(f, field);
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
		void AddConstraints(IEntity parentEntity, DefaultTypeParameter tp, GenericParameter g)
		{
			switch (g.Attributes & GenericParameterAttributes.VarianceMask) {
				case GenericParameterAttributes.Contravariant:
					tp.Variance = VarianceModifier.Contravariant;
					break;
				case GenericParameterAttributes.Covariant:
					tp.Variance = VarianceModifier.Covariant;
					break;
			}
			
			tp.HasReferenceTypeConstraint = g.HasReferenceTypeConstraint;
			tp.HasValueTypeConstraint = g.HasNotNullableValueTypeConstraint;
			tp.HasDefaultConstructorConstraint = g.HasDefaultConstructorConstraint;
			
			if (g.HasConstraints) {
				foreach (TypeReference constraint in g.Constraints) {
					tp.Constraints.Add(ReadTypeReference(constraint, entity: parentEntity));
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
			
			FinishReadMember(p, property);
			return p;
		}
		
		IAccessor ReadAccessor(MethodDefinition accessorMethod)
		{
			if (accessorMethod != null && IsVisible(accessorMethod.Attributes)) {
				Accessibility accessibility = GetAccessibility(accessorMethod.Attributes);
				if (HasAnyAttributes(accessorMethod)) {
					DefaultAccessor a = new DefaultAccessor();
					a.Accessibility = accessibility;
					AddAttributes(accessorMethod, a.Attributes, a.ReturnTypeAttributes);
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
			
			FinishReadMember(e, ev);
			
			return e;
		}
		#endregion
		
		void FinishReadMember(AbstractMember member, object cecilDefinition)
		{
			member.Freeze();
			member.ApplyInterningProvider(this.InterningProvider);
			if (HasCecilReferences)
				typeSystemTranslationTable[member] = cecilDefinition;
		}
		
		#region Type system translation table
		Dictionary<object, object> typeSystemTranslationTable;
		
		T InternalGetCecilObject<T> (object typeSystemObject) where T : class
		{
			if (typeSystemObject == null)
				throw new ArgumentNullException ("typeSystemObject");
			if (!HasCecilReferences)
				throw new NotSupportedException ("This instance contains no cecil references.");
			object result;
			if (!typeSystemTranslationTable.TryGetValue (typeSystemObject, out result))
				throw new InvalidOperationException ("No cecil reference stored for " + typeSystemObject);
			return result as T;
		}
		
		public AssemblyDefinition GetCecilObject (IProjectContent content)
		{
			return InternalGetCecilObject<AssemblyDefinition> (content);
		}
		
		public TypeDefinition GetCecilObject (ITypeDefinition type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			var cecilType = type as CecilTypeDefinition;
			if (cecilType == null)
				throw new ArgumentException ("type is no cecil type definition.");
			return cecilType.typeDefinition;
		}
		
		public MethodDefinition GetCecilObject (IMethod method)
		{
			return InternalGetCecilObject<MethodDefinition> (method);
		}
		
		public FieldDefinition GetCecilObject (IField field)
		{
			return InternalGetCecilObject<FieldDefinition> (field);
		}
		
		public EventDefinition GetCecilObject (IEvent evt)
		{
			return InternalGetCecilObject<EventDefinition> (evt);
		}
		
		public PropertyDefinition GetCecilObject (IProperty property)
		{
			return InternalGetCecilObject<PropertyDefinition> (property);
		}
		#endregion
	}
}
