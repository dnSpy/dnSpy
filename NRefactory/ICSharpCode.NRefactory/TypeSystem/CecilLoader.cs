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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;
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
		
		ModuleDefinition currentModule;
		CecilUnresolvedAssembly currentAssembly;
		
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
		[CLSCompliant(false)]
		public IUnresolvedAssembly LoadAssembly(AssemblyDefinition assemblyDefinition)
		{
			if (assemblyDefinition == null)
				throw new ArgumentNullException("assemblyDefinition");
			
			this.currentModule = assemblyDefinition.MainModule;
			
			// Read assembly and module attributes
			IList<IUnresolvedAttribute> assemblyAttributes = new List<IUnresolvedAttribute>();
			IList<IUnresolvedAttribute> moduleAttributes = new List<IUnresolvedAttribute>();
			AddAttributes(assemblyDefinition, assemblyAttributes);
			AddAttributes(assemblyDefinition.MainModule, moduleAttributes);
			
			if (this.InterningProvider != null) {
				assemblyAttributes = this.InterningProvider.InternList(assemblyAttributes);
				moduleAttributes = this.InterningProvider.InternList(moduleAttributes);
			}
			
			this.currentAssembly = new CecilUnresolvedAssembly(assemblyDefinition.Name.Name, this.DocumentationProvider);
			currentAssembly.AssemblyAttributes.AddRange(assemblyAttributes);
			currentAssembly.ModuleAttributes.AddRange(assemblyAttributes);
			
			// Register type forwarders:
			foreach (ExportedType type in assemblyDefinition.MainModule.ExportedTypes) {
				if (type.IsForwarder) {
					int typeParameterCount;
					string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name, out typeParameterCount);
					var typeRef = new GetClassTypeReference(GetAssemblyReference(type.Scope), type.Namespace, name, typeParameterCount);
					typeRef = this.InterningProvider.Intern(typeRef);
					var key = new FullNameAndTypeParameterCount(type.Namespace, name, typeParameterCount);
					currentAssembly.AddTypeForwarder(key, typeRef);
				}
			}
			
			// Create and register all types:
			List<TypeDefinition> cecilTypeDefs = new List<TypeDefinition>();
			List<DefaultUnresolvedTypeDefinition> typeDefs = new List<DefaultUnresolvedTypeDefinition>();
			foreach (ModuleDefinition module in assemblyDefinition.Modules) {
				foreach (TypeDefinition td in module.Types) {
					this.CancellationToken.ThrowIfCancellationRequested();
					if (this.IncludeInternalMembers || (td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public) {
						string name = td.Name;
						if (name.Length == 0 || name[0] == '<')
							continue;
						
						var t = CreateTopLevelTypeDefinition(td);
						cecilTypeDefs.Add(td);
						typeDefs.Add(t);
						currentAssembly.AddTypeDefinition(t);
					}
				}
			}
			// Initialize the type's members:
			for (int i = 0; i < typeDefs.Count; i++) {
				InitTypeDefinition(cecilTypeDefs[i], typeDefs[i]);
			}
			
			if (HasCecilReferences)
				typeSystemTranslationTable[this.currentAssembly] = assemblyDefinition;
			
			var result = this.currentAssembly;
			this.currentAssembly = null;
			this.currentModule = null;
			return result;
		}
		
		/// <summary>
		/// Loads a type from Cecil.
		/// </summary>
		/// <param name="typeDefinition">The Cecil TypeDefinition.</param>
		/// <returns>ITypeDefinition representing the Cecil type.</returns>
		[CLSCompliant(false)]
		public IUnresolvedTypeDefinition LoadType(TypeDefinition typeDefinition)
		{
			if (typeDefinition == null)
				throw new ArgumentNullException("typeDefinition");
			var td = CreateTopLevelTypeDefinition(typeDefinition);
			InitTypeDefinition(typeDefinition, td);
			return td;
		}
		#endregion
		
		#region IUnresolvedAssembly implementation
		[Serializable]
		sealed class CecilUnresolvedAssembly : DefaultUnresolvedAssembly, IDocumentationProvider
		{
			readonly IDocumentationProvider documentationProvider;
			
			public CecilUnresolvedAssembly(string assemblyName, IDocumentationProvider documentationProvider)
				: base(assemblyName)
			{
				Debug.Assert(assemblyName != null);
				this.documentationProvider = documentationProvider;
			}
			
			DocumentationComment IDocumentationProvider.GetDocumentation(IEntity entity)
			{
				if (documentationProvider != null)
					return documentationProvider.GetDocumentation(entity);
				else
					return null;
			}
		}
		#endregion
		
		#region Load Assembly From Disk
		public IUnresolvedAssembly LoadAssemblyFile(string fileName)
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
		[CLSCompliant(false)]
		public ITypeReference ReadTypeReference(TypeReference type, ICustomAttributeProvider typeAttributes = null)
		{
			int typeIndex = 0;
			return CreateType(type, typeAttributes, ref typeIndex);
		}
		
		ITypeReference CreateType(TypeReference type, ICustomAttributeProvider typeAttributes, ref int typeIndex)
		{
			while (type is OptionalModifierType || type is RequiredModifierType) {
				type = ((TypeSpecification)type).ElementType;
			}
			if (type == null) {
				return SpecialType.UnknownType;
			}
			
			if (type is Mono.Cecil.ByReferenceType) {
				typeIndex++;
				return new ByReferenceTypeReference(
					CreateType(
						(type as Mono.Cecil.ByReferenceType).ElementType,
						typeAttributes, ref typeIndex));
			} else if (type is Mono.Cecil.PointerType) {
				typeIndex++;
				return new PointerTypeReference(
					CreateType(
						(type as Mono.Cecil.PointerType).ElementType,
						typeAttributes, ref typeIndex));
			} else if (type is Mono.Cecil.ArrayType) {
				typeIndex++;
				return new ArrayTypeReference(
					CreateType(
						(type as Mono.Cecil.ArrayType).ElementType,
						typeAttributes, ref typeIndex),
					(type as Mono.Cecil.ArrayType).Rank);
			} else if (type is GenericInstanceType) {
				GenericInstanceType gType = (GenericInstanceType)type;
				ITypeReference baseType = CreateType(gType.ElementType, typeAttributes, ref typeIndex);
				ITypeReference[] para = new ITypeReference[gType.GenericArguments.Count];
				for (int i = 0; i < para.Length; ++i) {
					typeIndex++;
					para[i] = CreateType(gType.GenericArguments[i], typeAttributes, ref typeIndex);
				}
				return new ParameterizedTypeReference(baseType, para);
			} else if (type is GenericParameter) {
				GenericParameter typeGP = (GenericParameter)type;
				return new TypeParameterReference(typeGP.Owner is MethodDefinition ? EntityType.Method : EntityType.TypeDefinition, typeGP.Position);
			} else if (type.IsNested) {
				ITypeReference typeRef = CreateType(type.DeclaringType, typeAttributes, ref typeIndex);
				int partTypeParameterCount;
				string namepart = ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name, out partTypeParameterCount);
				return new NestedTypeReference(typeRef, namepart, partTypeParameterCount);
			} else {
				string ns = type.Namespace ?? string.Empty;
				string name = type.Name;
				if (name == null)
					throw new InvalidOperationException("type.Name returned null. Type: " + type.ToString());
				
				if (name == "Object" && ns == "System" && HasDynamicAttribute(typeAttributes, typeIndex)) {
					return SpecialType.Dynamic;
				} else {
					int typeParameterCount;
					name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name, out typeParameterCount);
					if (currentAssembly != null) {
						IUnresolvedTypeDefinition c = currentAssembly.GetTypeDefinition(ns, name, typeParameterCount);
						if (c != null)
							return c;
					}
					return new GetClassTypeReference(GetAssemblyReference(type.Scope), ns, name, typeParameterCount);
				}
			}
		}
		
		IAssemblyReference GetAssemblyReference(IMetadataScope scope)
		{
			if (scope == null || scope == currentModule)
				return DefaultAssemblyReference.CurrentAssembly;
			else
				return new DefaultAssemblyReference(scope.Name);
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
		static readonly ITypeReference assemblyVersionAttributeTypeRef = typeof(System.Reflection.AssemblyVersionAttribute).ToTypeReference();
		
		void AddAttributes(AssemblyDefinition assembly, IList<IUnresolvedAttribute> outputList)
		{
			if (assembly.HasCustomAttributes) {
				AddCustomAttributes(assembly.CustomAttributes, outputList);
			}
			if (assembly.HasSecurityDeclarations) {
				AddSecurityAttributes(assembly.SecurityDeclarations, outputList);
			}
			
			// AssemblyVersionAttribute
			if (assembly.Name.Version != null) {
				var assemblyVersion = new DefaultUnresolvedAttribute(assemblyVersionAttributeTypeRef, new[] { KnownTypeReference.String });
				assemblyVersion.PositionalArguments.Add(new SimpleConstantValue(KnownTypeReference.String, assembly.Name.Version.ToString()));
				outputList.Add(assemblyVersion);
			}
		}
		#endregion
		
		#region Module Attributes
		void AddAttributes(ModuleDefinition module, IList<IUnresolvedAttribute> outputList)
		{
			if (module.HasCustomAttributes) {
				AddCustomAttributes(module.CustomAttributes, outputList);
			}
		}
		#endregion
		
		#region Parameter Attributes
		static readonly IUnresolvedAttribute inAttribute = new DefaultUnresolvedAttribute(typeof(InAttribute).ToTypeReference());
		static readonly IUnresolvedAttribute outAttribute = new DefaultUnresolvedAttribute(typeof(OutAttribute).ToTypeReference());
		
		void AddAttributes(ParameterDefinition parameter, DefaultUnresolvedParameter targetParameter)
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
		static readonly SimpleConstantValue falseValue = new SimpleConstantValue(KnownTypeReference.Boolean, false);
		static readonly ITypeReference callingConventionTypeRef = typeof(CallingConvention).ToTypeReference();
		static readonly IUnresolvedAttribute preserveSigAttribute = new DefaultUnresolvedAttribute(typeof(PreserveSigAttribute).ToTypeReference());
		static readonly ITypeReference methodImplAttributeTypeRef = typeof(MethodImplAttribute).ToTypeReference();
		static readonly ITypeReference methodImplOptionsTypeRef = typeof(MethodImplOptions).ToTypeReference();
		
		static bool HasAnyAttributes(MethodDefinition methodDefinition)
		{
			if (methodDefinition.HasPInvokeInfo)
				return true;
			if ((methodDefinition.ImplAttributes & ~MethodImplAttributes.CodeTypeMask) != 0)
				return true;
			if (methodDefinition.MethodReturnType.HasFieldMarshal)
				return true;
			return methodDefinition.HasCustomAttributes || methodDefinition.MethodReturnType.HasCustomAttributes;
		}
		
		void AddAttributes(MethodDefinition methodDefinition, IList<IUnresolvedAttribute> attributes, IList<IUnresolvedAttribute> returnTypeAttributes)
		{
			MethodImplAttributes implAttributes = methodDefinition.ImplAttributes & ~MethodImplAttributes.CodeTypeMask;
			
			#region DllImportAttribute
			if (methodDefinition.HasPInvokeInfo) {
				PInvokeInfo info = methodDefinition.PInvokeInfo;
				var dllImport = new DefaultUnresolvedAttribute(dllImportAttributeTypeRef, new[] { KnownTypeReference.String });
				dllImport.PositionalArguments.Add(new SimpleConstantValue(KnownTypeReference.String, info.Module.Name));
				
				if (info.IsBestFitDisabled)
					dllImport.AddNamedFieldArgument("BestFitMapping", falseValue);
				if (info.IsBestFitEnabled)
					dllImport.AddNamedFieldArgument("BestFitMapping", trueValue);
				
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
					dllImport.AddNamedFieldArgument("CallingConvention", callingConventionTypeRef, (int)callingConvention);
				
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
					dllImport.AddNamedFieldArgument("CharSet", charSetTypeRef, (int)charSet);
				
				if (!string.IsNullOrEmpty(info.EntryPoint) && info.EntryPoint != methodDefinition.Name)
					dllImport.AddNamedFieldArgument("EntryPoint", KnownTypeReference.String, info.EntryPoint);
				
				if (info.IsNoMangle)
					dllImport.AddNamedFieldArgument("ExactSpelling", trueValue);
				
				if ((implAttributes & MethodImplAttributes.PreserveSig) == MethodImplAttributes.PreserveSig)
					implAttributes &= ~MethodImplAttributes.PreserveSig;
				else
					dllImport.AddNamedFieldArgument("PreserveSig", falseValue);
				
				if (info.SupportsLastError)
					dllImport.AddNamedFieldArgument("SetLastError", trueValue);
				
				if (info.IsThrowOnUnmappableCharDisabled)
					dllImport.AddNamedFieldArgument("ThrowOnUnmappableChar", falseValue);
				if (info.IsThrowOnUnmappableCharEnabled)
					dllImport.AddNamedFieldArgument("ThrowOnUnmappableChar", trueValue);
				
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
				var methodImpl = new DefaultUnresolvedAttribute(methodImplAttributeTypeRef, new[] { methodImplOptionsTypeRef });
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
		static readonly DefaultUnresolvedAttribute serializableAttribute = new DefaultUnresolvedAttribute(typeof(SerializableAttribute).ToTypeReference());
		static readonly DefaultUnresolvedAttribute comImportAttribute = new DefaultUnresolvedAttribute(typeof(ComImportAttribute).ToTypeReference());
		static readonly ITypeReference structLayoutAttributeTypeRef = typeof(StructLayoutAttribute).ToTypeReference();
		static readonly ITypeReference layoutKindTypeRef = typeof(LayoutKind).ToTypeReference();
		static readonly ITypeReference charSetTypeRef = typeof(CharSet).ToTypeReference();
		
		void AddAttributes(TypeDefinition typeDefinition, IUnresolvedTypeDefinition targetEntity)
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
				DefaultUnresolvedAttribute structLayout = new DefaultUnresolvedAttribute(structLayoutAttributeTypeRef, new[] { layoutKindTypeRef });
				structLayout.PositionalArguments.Add(new SimpleConstantValue(layoutKindTypeRef, (int)layoutKind));
				if (charSet != CharSet.Ansi) {
					structLayout.AddNamedFieldArgument("CharSet", charSetTypeRef, (int)charSet);
				}
				if (typeDefinition.PackingSize > 0) {
					structLayout.AddNamedFieldArgument("Pack", KnownTypeReference.Int32, (int)typeDefinition.PackingSize);
				}
				if (typeDefinition.ClassSize > 0) {
					structLayout.AddNamedFieldArgument("Size", KnownTypeReference.Int32, (int)typeDefinition.ClassSize);
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
		static readonly IUnresolvedAttribute nonSerializedAttribute = new DefaultUnresolvedAttribute(typeof(NonSerializedAttribute).ToTypeReference());
		
		void AddAttributes(FieldDefinition fieldDefinition, IUnresolvedEntity targetEntity)
		{
			// FieldOffsetAttribute
			if (fieldDefinition.HasLayoutInfo) {
				DefaultUnresolvedAttribute fieldOffset = new DefaultUnresolvedAttribute(fieldOffsetAttributeTypeRef, new[] { KnownTypeReference.Int32 });
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
		void AddAttributes(EventDefinition eventDefinition, IUnresolvedEntity targetEntity)
		{
			if (eventDefinition.HasCustomAttributes) {
				AddCustomAttributes(eventDefinition.CustomAttributes, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region Property Attributes
		void AddAttributes(PropertyDefinition propertyDefinition, IUnresolvedEntity targetEntity)
		{
			if (propertyDefinition.HasCustomAttributes) {
				AddCustomAttributes(propertyDefinition.CustomAttributes, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region MarshalAsAttribute (ConvertMarshalInfo)
		static readonly ITypeReference marshalAsAttributeTypeRef = typeof(MarshalAsAttribute).ToTypeReference();
		static readonly ITypeReference unmanagedTypeTypeRef = typeof(UnmanagedType).ToTypeReference();
		
		static IUnresolvedAttribute ConvertMarshalInfo(MarshalInfo marshalInfo)
		{
			DefaultUnresolvedAttribute attr = new DefaultUnresolvedAttribute(marshalAsAttributeTypeRef, new[] { unmanagedTypeTypeRef });
			attr.PositionalArguments.Add(new SimpleConstantValue(unmanagedTypeTypeRef, (int)marshalInfo.NativeType));
			
			FixedArrayMarshalInfo fami = marshalInfo as FixedArrayMarshalInfo;
			if (fami != null) {
				attr.AddNamedFieldArgument("SizeConst", KnownTypeReference.Int32, (int)fami.Size);
				if (fami.ElementType != NativeType.None)
					attr.AddNamedFieldArgument("ArraySubType", unmanagedTypeTypeRef, (int)fami.ElementType);
			}
			SafeArrayMarshalInfo sami = marshalInfo as SafeArrayMarshalInfo;
			if (sami != null && sami.ElementType != VariantType.None) {
				attr.AddNamedFieldArgument("SafeArraySubType", typeof(VarEnum).ToTypeReference(), (int)sami.ElementType);
			}
			ArrayMarshalInfo ami = marshalInfo as ArrayMarshalInfo;
			if (ami != null) {
				if (ami.ElementType != NativeType.Max)
					attr.AddNamedFieldArgument("ArraySubType", unmanagedTypeTypeRef, (int)ami.ElementType);
				if (ami.Size >= 0)
					attr.AddNamedFieldArgument("SizeConst", KnownTypeReference.Int32, (int)ami.Size);
				if (ami.SizeParameterMultiplier != 0 && ami.SizeParameterIndex >= 0)
					attr.AddNamedFieldArgument("SizeParamIndex", KnownTypeReference.Int16, (short)ami.SizeParameterIndex);
			}
			CustomMarshalInfo cmi = marshalInfo as CustomMarshalInfo;
			if (cmi != null) {
				attr.AddNamedFieldArgument("MarshalType", KnownTypeReference.String, cmi.ManagedType.FullName);
				if (!string.IsNullOrEmpty(cmi.Cookie))
					attr.AddNamedFieldArgument("MarshalCookie", KnownTypeReference.String, cmi.Cookie);
			}
			FixedSysStringMarshalInfo fssmi = marshalInfo as FixedSysStringMarshalInfo;
			if (fssmi != null) {
				attr.AddNamedFieldArgument("SizeConst", KnownTypeReference.Int32, (int)fssmi.Size);
			}
			
			return attr;
		}
		#endregion
		
		#region Custom Attributes (ReadAttribute)
		void AddCustomAttributes(Mono.Collections.Generic.Collection<CustomAttribute> attributes, IList<IUnresolvedAttribute> targetCollection)
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
		
		[CLSCompliant(false)]
		public IUnresolvedAttribute ReadAttribute(CustomAttribute attribute)
		{
			if (attribute == null)
				throw new ArgumentNullException("attribute");
			MethodReference ctor = attribute.Constructor;
			ITypeReference attributeType = ReadTypeReference(attribute.AttributeType);
			IList<ITypeReference> ctorParameterTypes = null;
			if (ctor.HasParameters) {
				ctorParameterTypes = new ITypeReference[ctor.Parameters.Count];
				for (int i = 0; i < ctorParameterTypes.Count; i++) {
					ctorParameterTypes[i] = ReadTypeReference(ctor.Parameters[i].ParameterType);
				}
			}
			if (this.InterningProvider != null) {
				attributeType = this.InterningProvider.Intern(attributeType);
				ctorParameterTypes = this.InterningProvider.InternList(ctorParameterTypes);
			}
			return new CecilUnresolvedAttribute(attributeType, ctorParameterTypes ?? EmptyList<ITypeReference>.Instance, attribute.GetBlob());
		}
		#endregion
		
		#region CecilUnresolvedAttribute
		static int GetBlobHashCode(byte[] blob)
		{
			unchecked {
				int hash = 0;
				foreach (byte b in blob) {
					hash *= 257;
					hash += b;
				}
				return hash;
			}
		}
		
		static bool BlobEquals(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
		
		[Serializable]
		sealed class CecilUnresolvedAttribute : IUnresolvedAttribute, ISupportsInterning
		{
			internal readonly ITypeReference attributeType;
			internal readonly IList<ITypeReference> ctorParameterTypes;
			internal readonly byte[] blob;
			
			public CecilUnresolvedAttribute(ITypeReference attributeType, IList<ITypeReference> ctorParameterTypes, byte[] blob)
			{
				Debug.Assert(attributeType != null);
				Debug.Assert(ctorParameterTypes != null);
				Debug.Assert(blob != null);
				this.attributeType = attributeType;
				this.ctorParameterTypes = ctorParameterTypes;
				this.blob = blob;
			}
			
			DomRegion IUnresolvedAttribute.Region {
				get { return DomRegion.Empty; }
			}
			
			IAttribute IUnresolvedAttribute.CreateResolvedAttribute(ITypeResolveContext context)
			{
				if (context.CurrentAssembly == null)
					throw new InvalidOperationException("Cannot resolve CecilUnresolvedAttribute without a parent assembly");
				return new CecilResolvedAttribute(context, this);
			}
			
			void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
			{
				// We already interned our child elements in ReadAttribute().
			}
			
			int ISupportsInterning.GetHashCodeForInterning()
			{
				return attributeType.GetHashCode() ^ ctorParameterTypes.GetHashCode() ^ GetBlobHashCode(blob);
			}
			
			bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
			{
				CecilUnresolvedAttribute o = other as CecilUnresolvedAttribute;
				return o != null && attributeType == o.attributeType && ctorParameterTypes == o.ctorParameterTypes
					&& BlobEquals(blob, o.blob);
			}
		}
		#endregion
		
		#region CecilResolvedAttribute
		sealed class CecilResolvedAttribute : IAttribute
		{
			readonly ITypeResolveContext context;
			readonly byte[] blob;
			readonly IList<ITypeReference> ctorParameterTypes;
			readonly IType attributeType;
			
			IMethod constructor;
			volatile bool constructorResolved;
			
			IList<ResolveResult> positionalArguments;
			IList<KeyValuePair<IMember, ResolveResult>> namedArguments;
			
			public CecilResolvedAttribute(ITypeResolveContext context, CecilUnresolvedAttribute unresolved)
			{
				this.context = context;
				this.blob = unresolved.blob;
				this.ctorParameterTypes = unresolved.ctorParameterTypes;
				this.attributeType = unresolved.attributeType.Resolve(context);
			}
			
			public CecilResolvedAttribute(ITypeResolveContext context, IType attributeType)
			{
				this.context = context;
				this.attributeType = attributeType;
				this.ctorParameterTypes = EmptyList<ITypeReference>.Instance;
			}
			
			public DomRegion Region {
				get { return DomRegion.Empty; }
			}
			
			public IType AttributeType {
				get { return attributeType; }
			}
			
			public IMethod Constructor {
				get {
					if (!constructorResolved) {
						constructor = ResolveConstructor();
						constructorResolved = true;
					}
					return constructor;
				}
			}
			
			IMethod ResolveConstructor()
			{
				var parameterTypes = ctorParameterTypes.Resolve(context);
				foreach (var ctor in attributeType.GetConstructors(m => m.Parameters.Count == parameterTypes.Count)) {
					bool ok = true;
					for (int i = 0; i < parameterTypes.Count; i++) {
						if (!ctor.Parameters[i].Type.Equals(parameterTypes[i])) {
							ok = false;
							break;
						}
					}
					if (ok)
						return ctor;
				}
				return null;
			}
			
			public IList<ResolveResult> PositionalArguments {
				get {
					var result = LazyInit.VolatileRead(ref this.positionalArguments);
					if (result != null) {
						return result;
					}
					DecodeBlob();
					return positionalArguments;
				}
			}
			
			public IList<KeyValuePair<IMember, ResolveResult>> NamedArguments {
				get {
					var result = LazyInit.VolatileRead(ref this.namedArguments);
					if (result != null) {
						return result;
					}
					DecodeBlob();
					return namedArguments;
				}
			}
			
			public override string ToString()
			{
				return "[" + attributeType.ToString() + "(...)]";
			}
			
			void DecodeBlob()
			{
				var positionalArguments = new List<ResolveResult>();
				var namedArguments = new List<KeyValuePair<IMember, ResolveResult>>();
				DecodeBlob(positionalArguments, namedArguments);
				Interlocked.CompareExchange(ref this.positionalArguments, positionalArguments, null);
				Interlocked.CompareExchange(ref this.namedArguments, namedArguments, null);
			}
			
			void DecodeBlob(List<ResolveResult> positionalArguments, List<KeyValuePair<IMember, ResolveResult>> namedArguments)
			{
				if (blob == null)
					return;
				BlobReader reader = new BlobReader(blob, context.CurrentAssembly);
				if (reader.ReadUInt16() != 0x0001) {
					Debug.WriteLine("Unknown blob prolog");
					return;
				}
				foreach (var ctorParameter in ctorParameterTypes.Resolve(context)) {
					positionalArguments.Add(reader.ReadFixedArg(ctorParameter));
				}
				ushort numNamed = reader.ReadUInt16();
				for (int i = 0; i < numNamed; i++) {
					var namedArg = reader.ReadNamedArg(attributeType);
					if (namedArg.Key != null)
						namedArguments.Add(namedArg);
				}
			}
		}
		#endregion
		
		#region class BlobReader
		class BlobReader
		{
			byte[] buffer;
			int position;
			readonly IAssembly currentResolvedAssembly;

			public BlobReader(byte[] buffer, IAssembly currentResolvedAssembly)
			{
				if (buffer == null)
					throw new ArgumentNullException("buffer");
				this.buffer = buffer;
				this.currentResolvedAssembly = currentResolvedAssembly;
			}
			
			public byte ReadByte()
			{
				return buffer[position++];
			}

			public sbyte ReadSByte()
			{
				unchecked {
					return(sbyte) ReadByte();
				}
			}
			
			public byte[] ReadBytes(int length)
			{
				var bytes = new byte[length];
				Buffer.BlockCopy(buffer, position, bytes, 0, length);
				position += length;
				return bytes;
			}

			public ushort ReadUInt16()
			{
				unchecked {
					ushort value =(ushort)(buffer[position]
					                       |(buffer[position + 1] << 8));
					position += 2;
					return value;
				}
			}

			public short ReadInt16()
			{
				unchecked {
					return(short) ReadUInt16();
				}
			}

			public uint ReadUInt32()
			{
				unchecked {
					uint value =(uint)(buffer[position]
					                   |(buffer[position + 1] << 8)
					                   |(buffer[position + 2] << 16)
					                   |(buffer[position + 3] << 24));
					position += 4;
					return value;
				}
			}

			public int ReadInt32()
			{
				unchecked {
					return(int) ReadUInt32();
				}
			}

			public ulong ReadUInt64()
			{
				unchecked {
					uint low = ReadUInt32();
					uint high = ReadUInt32();

					return(((ulong) high) << 32) | low;
				}
			}

			public long ReadInt64()
			{
				unchecked {
					return(long) ReadUInt64();
				}
			}

			public uint ReadCompressedUInt32()
			{
				unchecked {
					byte first = ReadByte();
					if((first & 0x80) == 0)
						return first;

					if((first & 0x40) == 0)
						return((uint)(first & ~0x80) << 8)
							| ReadByte();

					return((uint)(first & ~0xc0) << 24)
						|(uint) ReadByte() << 16
						|(uint) ReadByte() << 8
						| ReadByte();
				}
			}

			public float ReadSingle()
			{
				unchecked {
					if(!BitConverter.IsLittleEndian) {
						var bytes = ReadBytes(4);
						Array.Reverse(bytes);
						return BitConverter.ToSingle(bytes, 0);
					}

					float value = BitConverter.ToSingle(buffer, position);
					position += 4;
					return value;
				}
			}

			public double ReadDouble()
			{
				unchecked {
					if(!BitConverter.IsLittleEndian) {
						var bytes = ReadBytes(8);
						Array.Reverse(bytes);
						return BitConverter.ToDouble(bytes, 0);
					}

					double value = BitConverter.ToDouble(buffer, position);
					position += 8;
					return value;
				}
			}
			
			public ResolveResult ReadFixedArg(IType argType)
			{
				if (argType.Kind == TypeKind.Array) {
					if (((ArrayType)argType).Dimensions != 1) {
						// Only single-dimensional arrays are supported
						return ErrorResolveResult.UnknownError;
					}
					IType elementType = ((ArrayType)argType).ElementType;
					uint numElem = ReadUInt32();
					if (numElem == 0xffffffff) {
						// null reference
						return new ConstantResolveResult(argType, null);
					} else {
						ResolveResult[] elements = new ResolveResult[numElem];
						for (int i = 0; i < elements.Length; i++) {
							elements[i] = ReadElem(elementType);
						}
						return new ArrayCreateResolveResult(argType, null, elements);
					}
				} else {
					return ReadElem(argType);
				}
			}
			
			public ResolveResult ReadElem(IType elementType)
			{
				ITypeDefinition underlyingType;
				if (elementType.Kind == TypeKind.Enum) {
					underlyingType = elementType.GetDefinition().EnumUnderlyingType.GetDefinition();
				} else {
					underlyingType = elementType.GetDefinition();
				}
				if (underlyingType == null)
					return ErrorResolveResult.UnknownError;
				KnownTypeCode typeCode = underlyingType.KnownTypeCode;
				if (typeCode == KnownTypeCode.Object) {
					// boxed value type
					IType boxedTyped = ReadCustomAttributeFieldOrPropType();
					ResolveResult elem = ReadElem(boxedTyped);
					if (elem.IsCompileTimeConstant && elem.ConstantValue == null)
						return new ConstantResolveResult(elementType, null);
					else
						return new ConversionResolveResult(elementType, elem, Conversion.BoxingConversion);
				} else if (typeCode == KnownTypeCode.Type) {
					return new TypeOfResolveResult(underlyingType, ReadType());
				} else {
					return new ConstantResolveResult(elementType, ReadElemValue(typeCode));
				}
			}
			
			object ReadElemValue(KnownTypeCode typeCode)
			{
				switch (typeCode) {
					case KnownTypeCode.Boolean:
						return ReadByte() != 0;
					case KnownTypeCode.Char:
						return (char)ReadUInt16();
					case KnownTypeCode.SByte:
						return ReadSByte();
					case KnownTypeCode.Byte:
						return ReadByte();
					case KnownTypeCode.Int16:
						return ReadInt16();
					case KnownTypeCode.UInt16:
						return ReadUInt16();
					case KnownTypeCode.Int32:
						return ReadInt32();
					case KnownTypeCode.UInt32:
						return ReadUInt32();
					case KnownTypeCode.Int64:
						return ReadInt64();
					case KnownTypeCode.UInt64:
						return ReadUInt64();
					case KnownTypeCode.Single:
						return ReadSingle();
					case KnownTypeCode.Double:
						return ReadDouble();
					case KnownTypeCode.String:
						return ReadSerString();
					default:
						throw new NotSupportedException();
				}
			}
			
			public string ReadSerString ()
			{
				if (buffer [position] == 0xff) {
					position++;
					return null;
				}

				int length = (int) ReadCompressedUInt32();
				if (length == 0)
					return string.Empty;

				string @string = System.Text.Encoding.UTF8.GetString(
					buffer, position,
					buffer [position + length - 1] == 0 ? length - 1 : length);

				position += length;
				return @string;
			}
			
			public KeyValuePair<IMember, ResolveResult> ReadNamedArg(IType attributeType)
			{
				EntityType memberType;
				switch (ReadByte()) {
					case 0x53:
						memberType = EntityType.Field;
						break;
					case 0x54:
						memberType = EntityType.Property;
						break;
					default:
						throw new NotSupportedException();
				}
				IType type = ReadCustomAttributeFieldOrPropType();
				string name = ReadSerString();
				ResolveResult val = ReadFixedArg(type);
				IMember member = null;
				// Use last matching member, as GetMembers() returns members from base types first.
				foreach (IMember m in attributeType.GetMembers(m => m.EntityType == memberType && m.Name == name)) {
					if (m.ReturnType.Equals(type))
						member = m;
				}
				return new KeyValuePair<IMember, ResolveResult>(member, val);
			}
			
			IType ReadCustomAttributeFieldOrPropType()
			{
				ICompilation compilation = currentResolvedAssembly.Compilation;
				switch (ReadByte()) {
					case 0x02:
						return compilation.FindType(KnownTypeCode.Boolean);
					case 0x03:
						return compilation.FindType(KnownTypeCode.Char);
					case 0x04:
						return compilation.FindType(KnownTypeCode.SByte);
					case 0x05:
						return compilation.FindType(KnownTypeCode.Byte);
					case 0x06:
						return compilation.FindType(KnownTypeCode.Int16);
					case 0x07:
						return compilation.FindType(KnownTypeCode.UInt16);
					case 0x08:
						return compilation.FindType(KnownTypeCode.Int32);
					case 0x09:
						return compilation.FindType(KnownTypeCode.UInt32);
					case 0x0a:
						return compilation.FindType(KnownTypeCode.Int64);
					case 0x0b:
						return compilation.FindType(KnownTypeCode.UInt64);
					case 0x0c:
						return compilation.FindType(KnownTypeCode.Single);
					case 0x0d:
						return compilation.FindType(KnownTypeCode.Double);
					case 0x0e:
						return compilation.FindType(KnownTypeCode.String);
					case 0x1d:
						return new ArrayType(compilation, ReadCustomAttributeFieldOrPropType());
					case 0x50:
						return compilation.FindType(KnownTypeCode.Type);
					case 0x51: // boxed value type
						return compilation.FindType(KnownTypeCode.Object);
					case 0x55: // enum
						return ReadType();
					default:
						throw new NotSupportedException();
				}
			}
			
			IType ReadType()
			{
				string typeName = ReadSerString();
				ITypeReference typeReference = ReflectionHelper.ParseReflectionName(typeName);
				IType typeInCurrentAssembly = typeReference.Resolve(new SimpleTypeResolveContext(currentResolvedAssembly));
				if (typeInCurrentAssembly.Kind != TypeKind.Unknown)
					return typeInCurrentAssembly;
				
				// look for the type in mscorlib
				ITypeDefinition systemObject = currentResolvedAssembly.Compilation.FindType(KnownTypeCode.Object).GetDefinition();
				if (systemObject != null) {
					return typeReference.Resolve(new SimpleTypeResolveContext(systemObject.ParentAssembly));
				} else {
					// couldn't find corlib - return the unknown IType for the current assembly
					return typeInCurrentAssembly;
				}
			}
		}
		#endregion
		
		#region Security Attributes
		static readonly ITypeReference securityActionTypeReference = typeof(System.Security.Permissions.SecurityAction).ToTypeReference();
		static readonly ITypeReference permissionSetAttributeTypeReference = typeof(System.Security.Permissions.PermissionSetAttribute).ToTypeReference();
		
		/// <summary>
		/// Reads a security declaration.
		/// </summary>
		[CLSCompliant(false)]
		public IList<IUnresolvedAttribute> ReadSecurityDeclaration(SecurityDeclaration secDecl)
		{
			if (secDecl == null)
				throw new ArgumentNullException("secDecl");
			var result = new List<IUnresolvedAttribute>();
			AddSecurityAttributes(secDecl, result);
			return result;
		}
		
		void AddSecurityAttributes(Mono.Collections.Generic.Collection<SecurityDeclaration> securityDeclarations, IList<IUnresolvedAttribute> targetCollection)
		{
			foreach (var secDecl in securityDeclarations) {
				AddSecurityAttributes(secDecl, targetCollection);
			}
		}
		
		void AddSecurityAttributes(SecurityDeclaration secDecl, IList<IUnresolvedAttribute> targetCollection)
		{
			byte[] blob = secDecl.GetBlob();
			BlobReader reader = new BlobReader(blob, null);
			var securityAction = new SimpleConstantValue(securityActionTypeReference, (int)secDecl.Action);
			if (reader.ReadByte() == '.') {
				// binary attribute
				uint attributeCount = reader.ReadCompressedUInt32();
				UnresolvedSecurityDeclaration unresolvedSecDecl = new UnresolvedSecurityDeclaration(securityAction, blob);
				if (this.InterningProvider != null) {
					unresolvedSecDecl = this.InterningProvider.Intern(unresolvedSecDecl);
				}
				for (uint i = 0; i < attributeCount; i++) {
					targetCollection.Add(new UnresolvedSecurityAttribute(unresolvedSecDecl, (int)i));
				}
			} else {
				// for backward compatibility with .NET 1.0: XML-encoded attribute
				var attr = new DefaultUnresolvedAttribute(permissionSetAttributeTypeReference);
				attr.ConstructorParameterTypes.Add(securityActionTypeReference);
				attr.PositionalArguments.Add(securityAction);
				string xml = System.Text.Encoding.Unicode.GetString(blob);
				attr.AddNamedPropertyArgument("XML", KnownTypeReference.String, xml);
				targetCollection.Add(attr);
			}
		}
		
		[Serializable]
		sealed class UnresolvedSecurityDeclaration : ISupportsInterning
		{
			IConstantValue securityAction;
			byte[] blob;
			
			public UnresolvedSecurityDeclaration(IConstantValue securityAction, byte[] blob)
			{
				Debug.Assert(securityAction != null);
				Debug.Assert(blob != null);
				this.securityAction = securityAction;
				this.blob = blob;
			}
			
			public IList<IAttribute> Resolve(IAssembly currentAssembly)
			{
				// TODO: make this a per-assembly cache
//				CacheManager cache = currentAssembly.Compilation.CacheManager;
//				IList<IAttribute> result = (IList<IAttribute>)cache.GetShared(this);
//				if (result != null)
//					return result;
				
				ITypeResolveContext context = new SimpleTypeResolveContext(currentAssembly);
				BlobReader reader = new BlobReader(blob, currentAssembly);
				if (reader.ReadByte() != '.') {
					// should not use UnresolvedSecurityDeclaration for XML secdecls
					throw new InvalidOperationException();
				}
				ResolveResult securityActionRR = securityAction.Resolve(context);
				uint attributeCount = reader.ReadCompressedUInt32();
				IAttribute[] attributes = new IAttribute[attributeCount];
				try {
					ReadSecurityBlob(reader, attributes, context, securityActionRR);
				} catch (NotSupportedException ex) {
					// ignore invalid blobs
					Debug.WriteLine(ex.ToString());
				}
				for (int i = 0; i < attributes.Length; i++) {
					if (attributes[i] == null)
						attributes[i] = new CecilResolvedAttribute(context, SpecialType.UnknownType);
				}
				return attributes;
//				return (IList<IAttribute>)cache.GetOrAddShared(this, attributes);
			}
			
			void ReadSecurityBlob(BlobReader reader, IAttribute[] attributes, ITypeResolveContext context, ResolveResult securityActionRR)
			{
				for (int i = 0; i < attributes.Length; i++) {
					string attributeTypeName = reader.ReadSerString();
					ITypeReference attributeTypeRef = ReflectionHelper.ParseReflectionName(attributeTypeName);
					IType attributeType = attributeTypeRef.Resolve(context);
					
					reader.ReadCompressedUInt32(); // ??
					// The specification seems to be incorrect here, so I'm using the logic from Cecil instead.
					uint numNamed = reader.ReadCompressedUInt32();
					
					var namedArgs = new List<KeyValuePair<IMember, ResolveResult>>((int)numNamed);
					for (uint j = 0; j < numNamed; j++) {
						var namedArg = reader.ReadNamedArg(attributeType);
						if (namedArg.Key != null)
							namedArgs.Add(namedArg);
						
					}
					attributes[i] = new ResolvedSecurityAttribute {
						AttributeType = attributeType,
						NamedArguments = namedArgs,
						PositionalArguments = new ResolveResult[] { securityActionRR }
					};
				}
			}
			
			void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
			{
				securityAction = provider.Intern(securityAction);
			}
			
			int ISupportsInterning.GetHashCodeForInterning()
			{
				return securityAction.GetHashCode() ^ GetBlobHashCode(blob);
			}
			
			bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
			{
				UnresolvedSecurityDeclaration o = other as UnresolvedSecurityDeclaration;
				return o != null && securityAction == o.securityAction && BlobEquals(blob, o.blob);
			}
		}
		
		[Serializable]
		sealed class UnresolvedSecurityAttribute : IUnresolvedAttribute
		{
			readonly UnresolvedSecurityDeclaration secDecl;
			readonly int index;
			
			public UnresolvedSecurityAttribute(UnresolvedSecurityDeclaration secDecl, int index)
			{
				Debug.Assert(secDecl != null);
				this.secDecl = secDecl;
				this.index = index;
			}
			
			DomRegion IUnresolvedAttribute.Region {
				get { return DomRegion.Empty; }
			}
			
			IAttribute IUnresolvedAttribute.CreateResolvedAttribute(ITypeResolveContext context)
			{
				return secDecl.Resolve(context.CurrentAssembly)[index];
			}
		}
		
		sealed class ResolvedSecurityAttribute : IAttribute
		{
			public IType AttributeType { get; internal set; }
			
			DomRegion IAttribute.Region {
				get { return DomRegion.Empty; }
			}
			
			volatile IMethod constructor;
			
			public IMethod Constructor {
				get {
					IMethod ctor = this.constructor;
					if (ctor == null) {
						foreach (IMethod candidate in this.AttributeType.GetConstructors(m => m.Parameters.Count == 1)) {
							if (candidate.Parameters[0].Type.Equals(this.PositionalArguments[0].Type)) {
								ctor = candidate;
								break;
							}
						}
						this.constructor = ctor;
					}
					return ctor;
				}
			}
			
			public IList<ResolveResult> PositionalArguments { get; internal set; }
			
			public IList<KeyValuePair<IMember, ResolveResult>> NamedArguments { get; internal set; }
		}
		#endregion
		#endregion
		
		#region Read Type Definition
		DefaultUnresolvedTypeDefinition CreateTopLevelTypeDefinition(TypeDefinition typeDefinition)
		{
			string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(typeDefinition.Name);
			var td = new DefaultUnresolvedTypeDefinition(typeDefinition.Namespace, name);
			InitTypeParameters(typeDefinition, td);
			return td;
		}
		
		static void InitTypeParameters(TypeDefinition typeDefinition, DefaultUnresolvedTypeDefinition td)
		{
			// Type parameters are initialized within the constructor so that the class can be put into the type storage
			// before the rest of the initialization runs - this allows it to be available for early binding as soon as possible.
			for (int i = 0; i < typeDefinition.GenericParameters.Count; i++) {
				if (typeDefinition.GenericParameters[i].Position != i)
					throw new InvalidOperationException("g.Position != i");
				td.TypeParameters.Add(new DefaultUnresolvedTypeParameter(
					EntityType.TypeDefinition, i, typeDefinition.GenericParameters[i].Name));
			}
		}
		
		void InitTypeDefinition(TypeDefinition typeDefinition, DefaultUnresolvedTypeDefinition td)
		{
			InitTypeModifiers(typeDefinition, td);
			
			if (typeDefinition.HasGenericParameters) {
				for (int i = 0; i < typeDefinition.GenericParameters.Count; i++) {
					AddConstraints((DefaultUnresolvedTypeParameter)td.TypeParameters[i], typeDefinition.GenericParameters[i]);
				}
			}
			
			InitNestedTypes(typeDefinition, td); // nested types can be initialized only after generic parameters were created
			AddAttributes(typeDefinition, td);
			td.HasExtensionMethods = HasExtensionAttribute(typeDefinition);
			
			// set base classes
			if (typeDefinition.IsEnum) {
				foreach (FieldDefinition enumField in typeDefinition.Fields) {
					if (!enumField.IsStatic) {
						td.BaseTypes.Add(ReadTypeReference(enumField.FieldType));
						break;
					}
				}
			} else {
				if (typeDefinition.BaseType != null) {
					td.BaseTypes.Add(ReadTypeReference(typeDefinition.BaseType));
				}
				if (typeDefinition.HasInterfaces) {
					foreach (TypeReference iface in typeDefinition.Interfaces) {
						td.BaseTypes.Add(ReadTypeReference(iface));
					}
				}
			}
			
			InitMembers(typeDefinition, td);
			if (HasCecilReferences)
				typeSystemTranslationTable[td] = typeDefinition;
			if (this.InterningProvider != null) {
				td.ApplyInterningProvider(this.InterningProvider);
			}
			td.Freeze();
		}
		
		void InitNestedTypes(TypeDefinition typeDefinition, DefaultUnresolvedTypeDefinition td)
		{
			if (!typeDefinition.HasNestedTypes)
				return;
			foreach (TypeDefinition nestedTypeDef in typeDefinition.NestedTypes) {
				TypeAttributes visibility = nestedTypeDef.Attributes & TypeAttributes.VisibilityMask;
				if (this.IncludeInternalMembers
				    || visibility == TypeAttributes.NestedPublic
				    || visibility == TypeAttributes.NestedFamily
				    || visibility == TypeAttributes.NestedFamORAssem)
				{
					string name = nestedTypeDef.Name;
					int pos = name.LastIndexOf('/');
					if (pos > 0)
						name = name.Substring(pos + 1);
					if (name.Length == 0 || name[0] == '<')
						continue;
					name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name);
					var nestedType = new DefaultUnresolvedTypeDefinition(td, name);
					InitTypeParameters(nestedTypeDef, nestedType);
					td.NestedTypes.Add(nestedType);
					InitTypeDefinition(nestedTypeDef, nestedType);
				}
			}
		}
		
		static void InitTypeModifiers(TypeDefinition typeDefinition, DefaultUnresolvedTypeDefinition td)
		{
			// set classtype
			if (typeDefinition.IsInterface) {
				td.Kind = TypeKind.Interface;
			} else if (typeDefinition.IsEnum) {
				td.Kind = TypeKind.Enum;
			} else if (typeDefinition.IsValueType) {
				td.Kind = TypeKind.Struct;
			} else if (IsDelegate(typeDefinition)) {
				td.Kind = TypeKind.Delegate;
			} else if (IsModule(typeDefinition)) {
				td.Kind = TypeKind.Module;
			} else {
				td.Kind = TypeKind.Class;
			}
			td.IsSealed = typeDefinition.IsSealed;
			td.IsAbstract = typeDefinition.IsAbstract;
			switch (typeDefinition.Attributes & TypeAttributes.VisibilityMask) {
				case TypeAttributes.NotPublic:
				case TypeAttributes.NestedAssembly:
					td.Accessibility = Accessibility.Internal;
					break;
				case TypeAttributes.Public:
				case TypeAttributes.NestedPublic:
					td.Accessibility = Accessibility.Public;
					break;
				case TypeAttributes.NestedPrivate:
					td.Accessibility = Accessibility.Private;
					break;
				case TypeAttributes.NestedFamily:
					td.Accessibility = Accessibility.Protected;
					break;
				case TypeAttributes.NestedFamANDAssem:
					td.Accessibility = Accessibility.ProtectedAndInternal;
					break;
				case TypeAttributes.NestedFamORAssem:
					td.Accessibility = Accessibility.ProtectedOrInternal;
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
		
		void InitMembers(TypeDefinition typeDefinition, DefaultUnresolvedTypeDefinition td)
		{
			td.AddDefaultConstructorIfRequired = (td.Kind == TypeKind.Struct || td.Kind == TypeKind.Enum);
			if (typeDefinition.HasMethods) {
				foreach (MethodDefinition method in typeDefinition.Methods) {
					if (IsVisible(method.Attributes) && !IsAccessor(method.SemanticsAttributes)) {
						EntityType type = EntityType.Method;
						if (method.IsSpecialName) {
							if (method.IsConstructor)
								type = EntityType.Constructor;
							else if (method.Name.StartsWith("op_", StringComparison.Ordinal))
								type = EntityType.Operator;
						}
						td.Members.Add(ReadMethod(method, td, type));
					}
				}
			}
			if (typeDefinition.HasFields) {
				foreach (FieldDefinition field in typeDefinition.Fields) {
					if (IsVisible(field.Attributes) && !field.IsSpecialName) {
						td.Members.Add(ReadField(field, td));
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
					bool getterVisible = property.GetMethod != null && IsVisible(property.GetMethod.Attributes);
					bool setterVisible = property.SetMethod != null && IsVisible(property.SetMethod.Attributes);
					if (getterVisible || setterVisible) {
						EntityType type = property.Name == defaultMemberName ? EntityType.Indexer : EntityType.Property;
						td.Members.Add(ReadProperty(property, td, type));
					}
				}
			}
			if (typeDefinition.HasEvents) {
				foreach (EventDefinition ev in typeDefinition.Events) {
					if (ev.AddMethod != null && IsVisible(ev.AddMethod.Attributes)) {
						td.Members.Add(ReadEvent(ev, td));
					}
				}
			}
		}
		
		static bool IsAccessor(MethodSemanticsAttributes semantics)
		{
			return !(semantics == MethodSemanticsAttributes.None || semantics == MethodSemanticsAttributes.Other);
		}
		#endregion
		
		#region Read Method
		[CLSCompliant(false)]
		public IUnresolvedMethod ReadMethod(MethodDefinition method, IUnresolvedTypeDefinition parentType, EntityType methodType = EntityType.Method)
		{
			if (method == null)
				return null;
			DefaultUnresolvedMethod m = new DefaultUnresolvedMethod(parentType, method.Name);
			m.EntityType = methodType;
			if (method.HasGenericParameters) {
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					if (method.GenericParameters[i].Position != i)
						throw new InvalidOperationException("g.Position != i");
					m.TypeParameters.Add(new DefaultUnresolvedTypeParameter(
						EntityType.Method, i, method.GenericParameters[i].Name));
				}
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					AddConstraints((DefaultUnresolvedTypeParameter)m.TypeParameters[i], method.GenericParameters[i]);
				}
			}
			
			m.ReturnType = ReadTypeReference(method.ReturnType, typeAttributes: method.MethodReturnType);
			
			if (HasAnyAttributes(method))
				AddAttributes(method, m.Attributes, m.ReturnTypeAttributes);
			TranslateModifiers(method, m);
			
			if (method.HasParameters) {
				foreach (ParameterDefinition p in method.Parameters) {
					m.Parameters.Add(ReadParameter(p));
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
		
		void TranslateModifiers(MethodDefinition method, AbstractUnresolvedMember m)
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
		[CLSCompliant(false)]
		public IUnresolvedParameter ReadParameter(ParameterDefinition parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");
			var type = ReadTypeReference(parameter.ParameterType, typeAttributes: parameter);
			var p = new DefaultUnresolvedParameter(type, parameter.Name);
			
			if (parameter.ParameterType is Mono.Cecil.ByReferenceType) {
				if (!parameter.IsIn && parameter.IsOut)
					p.IsOut = true;
				else
					p.IsRef = true;
			}
			AddAttributes(parameter, p);
			
			if (parameter.IsOptional) {
				p.DefaultValue = new SimpleConstantValue(type, parameter.Constant);
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
		
		[CLSCompliant(false)]
		public IUnresolvedField ReadField(FieldDefinition field, IUnresolvedTypeDefinition parentType)
		{
			if (field == null)
				throw new ArgumentNullException("field");
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			
			DefaultUnresolvedField f = new DefaultUnresolvedField(parentType, field.Name);
			f.Accessibility = GetAccessibility(field.Attributes);
			f.IsReadOnly = field.IsInitOnly;
			f.IsStatic = field.IsStatic;
			f.ReturnType = ReadTypeReference(field.FieldType, typeAttributes: field);
			if (field.HasConstant) {
				f.ConstantValue = new SimpleConstantValue(f.ReturnType, field.Constant);
			}
			AddAttributes(field, f);
			
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
		void AddConstraints(DefaultUnresolvedTypeParameter tp, GenericParameter g)
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
					tp.Constraints.Add(ReadTypeReference(constraint));
				}
			}
		}
		#endregion
		
		#region Read Property
		[CLSCompliant(false)]
		public IUnresolvedProperty ReadProperty(PropertyDefinition property, IUnresolvedTypeDefinition parentType, EntityType propertyType = EntityType.Property)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			DefaultUnresolvedProperty p = new DefaultUnresolvedProperty(parentType, property.Name);
			p.EntityType = propertyType;
			TranslateModifiers(property.GetMethod ?? property.SetMethod, p);
			p.ReturnType = ReadTypeReference(property.PropertyType, typeAttributes: property);
			
			p.Getter = ReadMethod(property.GetMethod, parentType);
			p.Setter = ReadMethod(property.SetMethod, parentType);
			
			if (property.HasParameters) {
				foreach (ParameterDefinition par in property.Parameters) {
					p.Parameters.Add(ReadParameter(par));
				}
			}
			AddAttributes(property, p);
			
			FinishReadMember(p, property);
			return p;
		}
		#endregion
		
		#region Read Event
		[CLSCompliant(false)]
		public IUnresolvedEvent ReadEvent(EventDefinition ev, IUnresolvedTypeDefinition parentType)
		{
			if (ev == null)
				throw new ArgumentNullException("ev");
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			
			DefaultUnresolvedEvent e = new DefaultUnresolvedEvent(parentType, ev.Name);
			TranslateModifiers(ev.AddMethod, e);
			e.ReturnType = ReadTypeReference(ev.EventType, typeAttributes: ev);
			
			e.AddAccessor = ReadMethod(ev.AddMethod, parentType);
			e.RemoveAccessor = ReadMethod(ev.RemoveMethod, parentType);
			e.InvokeAccessor = ReadMethod(ev.InvokeMethod, parentType);
			
			AddAttributes(ev, e);
			
			FinishReadMember(e, ev);
			
			return e;
		}
		#endregion
		
		void FinishReadMember(AbstractUnresolvedMember member, object cecilDefinition)
		{
			member.ApplyInterningProvider(this.InterningProvider);
			member.Freeze();
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
				return null;
			return result as T;
		}
		
		[CLSCompliant(false)]
		public AssemblyDefinition GetCecilObject (IUnresolvedAssembly content)
		{
			return InternalGetCecilObject<AssemblyDefinition> (content);
		}
		
		[CLSCompliant(false)]
		public TypeDefinition GetCecilObject (IUnresolvedTypeDefinition type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			return InternalGetCecilObject<TypeDefinition> (type);
		}
		
		[CLSCompliant(false)]
		public MethodDefinition GetCecilObject (IUnresolvedMethod method)
		{
			return InternalGetCecilObject<MethodDefinition> (method);
		}
		
		[CLSCompliant(false)]
		public FieldDefinition GetCecilObject (IUnresolvedField field)
		{
			return InternalGetCecilObject<FieldDefinition> (field);
		}
		
		[CLSCompliant(false)]
		public EventDefinition GetCecilObject (IUnresolvedEvent evt)
		{
			return InternalGetCecilObject<EventDefinition> (evt);
		}
		
		[CLSCompliant(false)]
		public PropertyDefinition GetCecilObject (IUnresolvedProperty property)
		{
			return InternalGetCecilObject<PropertyDefinition> (property);
		}
		#endregion
	}
}
