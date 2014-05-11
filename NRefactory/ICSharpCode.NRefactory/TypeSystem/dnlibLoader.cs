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
using dnlib.DotNet;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Allows loading an IProjectContent from an already compiled assembly.
	/// </summary>
	/// <remarks>Instance methods are not thread-safe; you need to create multiple instances of dnlibLoader
	/// if you want to load multiple project contents in parallel.</remarks>
	public class dnlibLoader
	{
		#region Options
		/// <summary>
		/// Specifies whether to include internal members. The default is false.
		/// </summary>
		public bool IncludeInternalMembers { get; set; }
		
		/// <summary>
		/// Specifies whether to use lazy loading. The default is false.
		/// If this property is set to true, the dnlibLoader will not copy all the relevant information
		/// out of the dnlib object model, but will maintain references to the Cecil objects.
		/// This speeds up the loading process and avoids loading unnecessary information, but it causes
		/// the dnlib objects to stay in memory (which can significantly increase memory usage).
		/// It also prevents serialization of the dnlib-loaded type system.
		/// </summary>
		/// <remarks>
		/// Because the type system can be used on multiple threads, but dnlib is not
		/// thread-safe for concurrent read access, the dnlibLoader will lock on the <see cref="ModuleDef"/> instance
		/// for every delay-loading operation.
		/// If you access the dnlib objects directly in your application, you may need to take the same lock.
		/// </remarks>
		public bool LazyLoad { get; set; }
		
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
		/// This delegate gets executed whenever an entity was loaded.
		/// </summary>
		/// <remarks>
		/// This callback may be to build a dictionary that maps between
		/// entities and dnlib objects.
		/// Warning: if delay-loading is used and the type system is accessed by multiple threads,
		/// the callback may be invoked concurrently on multiple threads.
		/// </remarks>
		public Action<IUnresolvedEntity, IMemberRef> OnEntityLoaded { get; set; }
		
		/// <summary>
		/// Gets a value indicating whether this instance stores references to the dnlib objects.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance has references to the dnlib objects; otherwise, <c>false</c>.
		/// </value>
		public bool HasDnlibReferences { get { return typeSystemTranslationTable != null; } }
		#endregion
		
		ModuleDef currentModule;
		DnlibUnresolvedAssembly currentAssembly;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ICSharpCode.NRefactory.TypeSystem.dnlibLoader"/> class.
		/// </summary>
		public dnlibLoader()
		{
			// Enable interning by default.
			this.InterningProvider = new SimpleInterningProvider();
		}
		
		/// <summary>
		/// Creates a nested dnlibLoader for lazy-loading.
		/// </summary>
		private dnlibLoader(dnlibLoader loader)
		{
			// use a shared typeSystemTranslationTable
			this.typeSystemTranslationTable = loader.typeSystemTranslationTable;
			this.IncludeInternalMembers = loader.IncludeInternalMembers;
			this.LazyLoad = loader.LazyLoad;
			this.OnEntityLoaded = loader.OnEntityLoaded;
			this.currentModule = loader.currentModule;
			this.currentAssembly = loader.currentAssembly;
			// don't use interning - the interning provider is most likely not thread-safe
			// don't use cancellation for delay-loaded members
		}

		#region Load From AssemblyDefinition
		/// <summary>
		/// Loads the assembly definition into a project content.
		/// </summary>
		/// <returns>IProjectContent that represents the assembly</returns>
		[CLSCompliant(false)]
		public IUnresolvedAssembly LoadAssembly(AssemblyDef assemblyDef)
		{
			if (assemblyDef == null)
				throw new ArgumentNullException("assemblyDefinition");
			
			this.currentModule = assemblyDef.ManifestModule;
			
			// Read assembly and module attributes
			IList<IUnresolvedAttribute> assemblyAttributes = new List<IUnresolvedAttribute>();
			IList<IUnresolvedAttribute> moduleAttributes = new List<IUnresolvedAttribute>();
			AddAttributes(assemblyDef, assemblyAttributes);
			AddAttributes(assemblyDef.ManifestModule, moduleAttributes);
			
			if (this.InterningProvider != null) {
				assemblyAttributes = this.InterningProvider.InternList(assemblyAttributes);
				moduleAttributes = this.InterningProvider.InternList(moduleAttributes);
			}
			
			this.currentAssembly = new DnlibUnresolvedAssembly(assemblyDef.Name, this.DocumentationProvider);
			currentAssembly.Location = assemblyDef.ManifestModule.Location;
			currentAssembly.AssemblyAttributes.AddRange(assemblyAttributes);
			currentAssembly.ModuleAttributes.AddRange(assemblyAttributes);
			
			// Register type forwarders:
			foreach (ExportedType type in assemblyDef.ManifestModule.ExportedTypes) {
				if (type.IsForwarder) {
					int typeParameterCount;
					string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.TypeName, out typeParameterCount);
					var typeRef = new GetClassTypeReference(GetAssemblyReference(type.Scope), type.TypeNamespace, name, typeParameterCount);
					if (this.InterningProvider != null)
						typeRef = this.InterningProvider.Intern(typeRef);
					var key = new FullNameAndTypeParameterCount(type.Namespace, name, typeParameterCount);
					currentAssembly.AddTypeForwarder(key, typeRef);
				}
			}
			
			// Create and register all types:
			dnlibLoader dnlibLoaderCloneForLazyLoading = LazyLoad ? new dnlibLoader(this) : null;
			List<TypeDef> dnlibTypeDefs = new List<TypeDef>();
			List<DefaultUnresolvedTypeDefinition> typeDefs = new List<DefaultUnresolvedTypeDefinition>();
			foreach (ModuleDef module in assemblyDef.Modules) {
				foreach (TypeDef td in module.Types) {
					this.CancellationToken.ThrowIfCancellationRequested();
					if (this.IncludeInternalMembers || (td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public) {
						string name = td.Name;
						if (name.Length == 0)
							continue;
						
						if (this.LazyLoad) {
							var t = new LazyDnlibTypeDefinition(dnlibLoaderCloneForLazyLoading, td);
							currentAssembly.AddTypeDefinition(t);
							RegisterDnlibObject(t, td);
						} else {
							var t = CreateTopLevelTypeDefinition(td);
							dnlibTypeDefs.Add(td);
							typeDefs.Add(t);
							currentAssembly.AddTypeDefinition(t);
							// The registration will happen after the members are initialized
						}
					}
				}
			}
			// Initialize the type's members:
			for (int i = 0; i < typeDefs.Count; i++) {
				InitTypeDefinition(dnlibTypeDefs[i], typeDefs[i]);
			}
			
			AddToTypeSystemTranslationTable(this.currentAssembly, assemblyDef);
			
			var result = this.currentAssembly;
			this.currentAssembly = null;
			this.currentModule = null;
			return result;
		}
		
		/// <summary>
		/// Sets the current module.
		/// This causes ReadTypeReference() to use <see cref="DefaultAssemblyReference.CurrentAssembly"/> for references
		/// in that module.
		/// </summary>
		[CLSCompliant(false)]
		public void SetCurrentModule(ModuleDef module)
		{
			this.currentModule = module;
		}
		
		/// <summary>
		/// Loads a type from dnlib.
		/// </summary>
		/// <param name="typeDef">The dnlib TypeDef.</param>
		/// <returns>ITypeDefinition representing the dnlib type.</returns>
		[CLSCompliant(false)]
		public IUnresolvedTypeDefinition LoadType(TypeDef typeDef)
		{
			if (typeDef == null)
				throw new ArgumentNullException("typeDefinition");
			var td = CreateTopLevelTypeDefinition(typeDef);
			InitTypeDefinition(typeDef, td);
			return td;
		}
		#endregion
		
		#region IUnresolvedAssembly implementation
		[Serializable]
		sealed class DnlibUnresolvedAssembly : DefaultUnresolvedAssembly, IDocumentationProvider
		{
			readonly IDocumentationProvider documentationProvider;
			
			public DnlibUnresolvedAssembly(string assemblyName, IDocumentationProvider documentationProvider)
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
			var param = new ModuleContext(new DummyAssemblyResolver());
			AssemblyDef asm = AssemblyDef.Load(fileName, param);
			return LoadAssembly(asm);
		}
		
		// used to prevent dnlib from loading referenced assemblies
		sealed class DummyAssemblyResolver : IAssemblyResolver
		{
			public AssemblyDef Resolve(dnlib.DotNet.IAssembly assembly, ModuleDef sourceModule)
			{
				return null;
			}

			public bool AddToCache(AssemblyDef asm)
			{
				return false;
			}

			public bool Remove(AssemblyDef asm)
			{
				return false;
			}
		}
		#endregion
		
		#region Read Type Reference
		/// <summary>
		/// Reads a type reference.
		/// </summary>
		/// <param name="type">The dnlib type reference that should be converted into
		/// a type system type reference.</param>
		/// <param name="typeAttributes">Attributes associated with the dnlib type reference.
		/// This is used to support the 'dynamic' type.</param>
		[CLSCompliant(false)]
		public ITypeReference ReadTypeReference(ITypeDefOrRef type, IHasCustomAttribute typeAttributes = null)
		{
			int typeIndex = 0;
			return CreateType(type, typeAttributes, ref typeIndex);
		}
		
		ITypeReference CreateType(ITypeDefOrRef type, IHasCustomAttribute typeAttributes, ref int typeIndex)
		{
			if (type == null)
			{
				return SpecialType.UnknownType;
			}
			if (type is TypeSpec)
			{
				return CreateType(((TypeSpec)type).TypeSig, typeAttributes, ref typeIndex);
			}
			ITypeDefOrRef declType;
			if (type is TypeRef)
				declType = ((TypeRef)type).DeclaringType;
			else
				declType = ((TypeDef)type).DeclaringType;

			if (declType != null)
			{
				ITypeReference typeRef = CreateType(declType, typeAttributes, ref typeIndex);
				int partTypeParameterCount;
				string namepart = ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name, out partTypeParameterCount);
				return new NestedTypeReference(typeRef, namepart, partTypeParameterCount);
			}
			else
			{
				string ns = type.Namespace ?? string.Empty;
				string name = type.Name;
				if (name == null)
					throw new InvalidOperationException("type.Name returned null. Type: " + type.ToString());

				if (name == "Object" && ns == "System" && HasDynamicAttribute(typeAttributes, typeIndex))
				{
					return SpecialType.Dynamic;
				}
				else
				{
					int typeParameterCount;
					name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name, out typeParameterCount);
					if (currentAssembly != null)
					{
						IUnresolvedTypeDefinition c = currentAssembly.GetTypeDefinition(ns, name, typeParameterCount);
						if (c != null)
							return c;
					}
					return new GetClassTypeReference(GetAssemblyReference(type.Scope), ns, name, typeParameterCount);
				}
			}
		}

		ITypeReference CreateType(TypeSig type, IHasCustomAttribute typeAttributes, ref int typeIndex)
		{
			type = type.RemoveModifiers();
			if (type == null)
			{
				return SpecialType.UnknownType;
			}

			if (type is ByRefSig)
			{
				typeIndex++;
				return new ByReferenceTypeReference(
					CreateType((type as ByRefSig).Next, typeAttributes, ref typeIndex));
			}
			else if (type is PtrSig)
			{
				typeIndex++;
				return new PointerTypeReference(
					CreateType((type as PtrSig).Next, typeAttributes, ref typeIndex));
			}
			else if (type is ArraySig)
			{
				typeIndex++;
				return new ArrayTypeReference(
					CreateType((type as ArraySig).Next, typeAttributes, ref typeIndex),
					(int)(type as ArraySig).Rank);
			}
			else if (type is SZArraySig)
			{
				typeIndex++;
				return new ArrayTypeReference(
					CreateType((type as SZArraySig).Next, typeAttributes, ref typeIndex), 1);
			}
			else if (type is GenericInstSig)
			{
				GenericInstSig gType = (GenericInstSig)type;
				ITypeReference baseType = CreateType(gType.GenericType, typeAttributes, ref typeIndex);
				ITypeReference[] para = new ITypeReference[gType.GenericArguments.Count];
				for (int i = 0; i < para.Length; ++i)
				{
					typeIndex++;
					para[i] = CreateType(gType.GenericArguments[i], typeAttributes, ref typeIndex);
				}
				return new ParameterizedTypeReference(baseType, para);
			}
			else if (type is GenericSig)
			{
				GenericSig typeGP = (GenericSig)type;
				return new TypeParameterReference(typeGP.IsMethodVar ? EntityType.Method : EntityType.TypeDefinition, (int)typeGP.Number);
			}
			else if (type is TypeDefOrRefSig)
			{
				return CreateType((type as TypeDefOrRefSig).TypeDefOrRef, typeAttributes, ref typeIndex);
			}
			else
			{
				return SpecialType.UnknownType;
			}
		}
		
		IAssemblyReference GetAssemblyReference(IScope scope)
		{
			if (scope == null || scope == currentModule)
				return DefaultAssemblyReference.CurrentAssembly;
			else
				return new DefaultAssemblyReference(scope.ScopeName);
		}
		
		static bool HasDynamicAttribute(IHasCustomAttribute attributeProvider, int typeIndex)
		{
			if (attributeProvider == null || !attributeProvider.HasCustomAttributes)
				return false;
			foreach (CustomAttribute a in attributeProvider.CustomAttributes) {
				ITypeDefOrRef type = a.AttributeType;
				if (type.Name == "DynamicAttribute" && type.Namespace == "System.Runtime.CompilerServices") {
					if (a.ConstructorArguments.Count == 1) {
						IList<CAArgument> values = a.ConstructorArguments[0].Value as IList<CAArgument>;
						if (values != null && typeIndex < values.Count && values[typeIndex].Value is bool)
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
		
		void AddAttributes(AssemblyDef assembly, IList<IUnresolvedAttribute> outputList)
		{
			if (assembly.HasCustomAttributes) {
				AddCustomAttributes(assembly.CustomAttributes, outputList);
			}
			if (assembly.DeclSecurities.Count > 0) {
				AddSecurityAttributes(assembly.DeclSecurities, outputList);
			}
			
			// AssemblyVersionAttribute
			if (assembly.Version != null) {
				var assemblyVersion = new DefaultUnresolvedAttribute(assemblyVersionAttributeTypeRef, new[] { KnownTypeReference.String });
				assemblyVersion.PositionalArguments.Add(new SimpleConstantValue(KnownTypeReference.String, assembly.Version.ToString()));
				outputList.Add(assemblyVersion);
			}
		}
		#endregion
		
		#region Module Attributes
		void AddAttributes(ModuleDef module, IList<IUnresolvedAttribute> outputList)
		{
			if (module.HasCustomAttributes) {
				AddCustomAttributes(module.CustomAttributes, outputList);
			}
		}
		#endregion
		
		#region Parameter Attributes
		static readonly IUnresolvedAttribute inAttribute = new DefaultUnresolvedAttribute(typeof(InAttribute).ToTypeReference());
		static readonly IUnresolvedAttribute outAttribute = new DefaultUnresolvedAttribute(typeof(OutAttribute).ToTypeReference());
		
		void AddAttributes(ParamDef parameter, DefaultUnresolvedParameter targetParameter)
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
			if (parameter.HasMarshalType) {
				targetParameter.Attributes.Add(ConvertMarshalInfo(parameter.MarshalType));
			}
		}
		#endregion
		
		#region Method Attributes
		static readonly ITypeReference dllImportAttributeTypeRef = typeof(DllImportAttribute).ToTypeReference();
		static readonly SimpleConstantValue trueValue = new SimpleConstantValue(KnownTypeReference.Boolean, true);
		static readonly SimpleConstantValue falseValue = new SimpleConstantValue(KnownTypeReference.Boolean, false);
		static readonly ITypeReference callingConventionTypeRef = typeof(System.Runtime.InteropServices.CallingConvention).ToTypeReference();
		static readonly IUnresolvedAttribute preserveSigAttribute = new DefaultUnresolvedAttribute(typeof(PreserveSigAttribute).ToTypeReference());
		static readonly ITypeReference methodImplAttributeTypeRef = typeof(MethodImplAttribute).ToTypeReference();
		static readonly ITypeReference methodImplOptionsTypeRef = typeof(MethodImplOptions).ToTypeReference();
		
		static bool HasAnyAttributes(MethodDef methodDef)
		{
			if (methodDef.HasImplMap)
				return true;
			if ((methodDef.ImplAttributes & ~MethodImplAttributes.CodeTypeMask) != 0)
				return true;
			if (methodDef.Parameters.ReturnParameter.HasParamDef)
			{
				var returnParam = methodDef.Parameters.ReturnParameter.ParamDef;
				if (returnParam.HasMarshalType || returnParam.HasCustomAttributes)
					return true;
			}
			return methodDef.HasCustomAttributes;
		}
		
		void AddAttributes(MethodDef methodDef, IList<IUnresolvedAttribute> attributes, IList<IUnresolvedAttribute> returnTypeAttributes)
		{
			MethodImplAttributes implAttributes = methodDef.ImplAttributes & ~MethodImplAttributes.CodeTypeMask;
			
			#region DllImportAttribute
			if (methodDef.HasImplMap) {
				ImplMap impl = methodDef.ImplMap;
				var dllImport = new DefaultUnresolvedAttribute(dllImportAttributeTypeRef, new[] { KnownTypeReference.String });
				dllImport.PositionalArguments.Add(new SimpleConstantValue(KnownTypeReference.String, impl.Module.Name));
				
				if (impl.IsBestFitDisabled)
					dllImport.AddNamedFieldArgument("BestFitMapping", falseValue);
				if (impl.IsBestFitEnabled)
					dllImport.AddNamedFieldArgument("BestFitMapping", trueValue);
				
				System.Runtime.InteropServices.CallingConvention callingConvention;
				switch (impl.Attributes & PInvokeAttributes.CallConvMask) {
					case (PInvokeAttributes)0:
						Debug.WriteLine ("P/Invoke calling convention not set on:" + methodDef.FullName);
						callingConvention = System.Runtime.InteropServices.CallingConvention.StdCall;
						break;
					case PInvokeAttributes.CallConvCdecl:
						callingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl;
						break;
					case PInvokeAttributes.CallConvFastcall:
						callingConvention = System.Runtime.InteropServices.CallingConvention.FastCall;
						break;
					case PInvokeAttributes.CallConvStdcall:
						callingConvention = System.Runtime.InteropServices.CallingConvention.StdCall;
						break;
					case PInvokeAttributes.CallConvThiscall:
						callingConvention = System.Runtime.InteropServices.CallingConvention.ThisCall;
						break;
					case PInvokeAttributes.CallConvWinapi:
						callingConvention = System.Runtime.InteropServices.CallingConvention.Winapi;
						break;
					default:
						throw new NotSupportedException("unknown calling convention");
				}
				if (callingConvention != System.Runtime.InteropServices.CallingConvention.Winapi)
					dllImport.AddNamedFieldArgument("CallingConvention", callingConventionTypeRef, (int)callingConvention);
				
				CharSet charSet = CharSet.None;
				switch (impl.Attributes & PInvokeAttributes.CharSetMask) {
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
				
				if (!string.IsNullOrEmpty(impl.Name) && impl.Name != methodDef.Name)
					dllImport.AddNamedFieldArgument("EntryPoint", KnownTypeReference.String, impl.Name);
				
				if (impl.IsNoMangle)
					dllImport.AddNamedFieldArgument("ExactSpelling", trueValue);
				
				if ((implAttributes & MethodImplAttributes.PreserveSig) == MethodImplAttributes.PreserveSig)
					implAttributes &= ~MethodImplAttributes.PreserveSig;
				else
					dllImport.AddNamedFieldArgument("PreserveSig", falseValue);
				
				if (impl.SupportsLastError)
					dllImport.AddNamedFieldArgument("SetLastError", trueValue);
				
				if (impl.IsThrowOnUnmappableCharDisabled)
					dllImport.AddNamedFieldArgument("ThrowOnUnmappableChar", falseValue);
				if (impl.IsThrowOnUnmappableCharEnabled)
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
			
			if (methodDef.HasCustomAttributes) {
				AddCustomAttributes(methodDef.CustomAttributes, attributes);
			}
			if (methodDef.DeclSecurities.Count > 0) {
				AddSecurityAttributes(methodDef.DeclSecurities, attributes);
			}
			ParamDef returnParam = methodDef.Parameters.ReturnParameter.ParamDef;
			if (returnParam != null && returnParam.HasMarshalType) {
				returnTypeAttributes.Add(ConvertMarshalInfo(returnParam.MarshalType));
			}
			if (returnParam != null && returnParam.HasCustomAttributes) {
				AddCustomAttributes(returnParam.CustomAttributes, returnTypeAttributes);
			}
		}
		#endregion
		
		#region Type Attributes
		static readonly DefaultUnresolvedAttribute serializableAttribute = new DefaultUnresolvedAttribute(typeof(SerializableAttribute).ToTypeReference());
		static readonly DefaultUnresolvedAttribute comImportAttribute = new DefaultUnresolvedAttribute(typeof(ComImportAttribute).ToTypeReference());
		static readonly ITypeReference structLayoutAttributeTypeRef = typeof(StructLayoutAttribute).ToTypeReference();
		static readonly ITypeReference layoutKindTypeRef = typeof(LayoutKind).ToTypeReference();
		static readonly ITypeReference charSetTypeRef = typeof(CharSet).ToTypeReference();
		
		void AddAttributes(TypeDef typeDef, IUnresolvedTypeDefinition targetEntity)
		{
			// SerializableAttribute
			if (typeDef.IsSerializable)
				targetEntity.Attributes.Add(serializableAttribute);
			
			// ComImportAttribute
			if (typeDef.IsImport)
				targetEntity.Attributes.Add(comImportAttribute);
			
			#region StructLayoutAttribute
			LayoutKind layoutKind = LayoutKind.Auto;
			switch (typeDef.Attributes & TypeAttributes.LayoutMask) {
				case TypeAttributes.SequentialLayout:
					layoutKind = LayoutKind.Sequential;
					break;
				case TypeAttributes.ExplicitLayout:
					layoutKind = LayoutKind.Explicit;
					break;
			}
			CharSet charSet = CharSet.None;
			switch (typeDef.Attributes & TypeAttributes.StringFormatMask) {
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
			LayoutKind defaultLayoutKind = (typeDef.IsValueType && !typeDef.IsEnum) ? LayoutKind.Sequential : LayoutKind.Auto;
			if (layoutKind != defaultLayoutKind || charSet != CharSet.Ansi || typeDef.HasClassLayout) {
				DefaultUnresolvedAttribute structLayout = new DefaultUnresolvedAttribute(structLayoutAttributeTypeRef, new[] { layoutKindTypeRef });
				structLayout.PositionalArguments.Add(new SimpleConstantValue(layoutKindTypeRef, (int)layoutKind));
				if (charSet != CharSet.Ansi) {
					structLayout.AddNamedFieldArgument("CharSet", charSetTypeRef, (int)charSet);
				}
				if (typeDef.ClassLayout.PackingSize > 0) {
					structLayout.AddNamedFieldArgument("Pack", KnownTypeReference.Int32, (int)typeDef.ClassLayout.PackingSize);
				}
				if (typeDef.ClassLayout.ClassSize > 0) {
					structLayout.AddNamedFieldArgument("Size", KnownTypeReference.Int32, (int)typeDef.ClassLayout.ClassSize);
				}
				targetEntity.Attributes.Add(structLayout);
			}
			#endregion
			
			if (typeDef.HasCustomAttributes) {
				AddCustomAttributes(typeDef.CustomAttributes, targetEntity.Attributes);
			}
			if (typeDef.DeclSecurities.Count > 0) {
				AddSecurityAttributes(typeDef.DeclSecurities, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region Field Attributes
		static readonly ITypeReference fieldOffsetAttributeTypeRef = typeof(FieldOffsetAttribute).ToTypeReference();
		static readonly IUnresolvedAttribute nonSerializedAttribute = new DefaultUnresolvedAttribute(typeof(NonSerializedAttribute).ToTypeReference());
		
		void AddAttributes(FieldDef fieldDef, IUnresolvedEntity targetEntity)
		{
			// FieldOffsetAttribute
			if (fieldDef.HasLayoutInfo) {
				DefaultUnresolvedAttribute fieldOffset = new DefaultUnresolvedAttribute(fieldOffsetAttributeTypeRef, new[] { KnownTypeReference.Int32 });
				fieldOffset.PositionalArguments.Add(new SimpleConstantValue(KnownTypeReference.Int32, (int)fieldDef.FieldOffset));
				targetEntity.Attributes.Add(fieldOffset);
			}
			
			// NonSerializedAttribute
			if (fieldDef.IsNotSerialized) {
				targetEntity.Attributes.Add(nonSerializedAttribute);
			}
			
			if (fieldDef.HasMarshalType) {
				targetEntity.Attributes.Add(ConvertMarshalInfo(fieldDef.MarshalType));
			}
			
			if (fieldDef.HasCustomAttributes) {
				AddCustomAttributes(fieldDef.CustomAttributes, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region Event Attributes
		void AddAttributes(EventDef eventDef, IUnresolvedEntity targetEntity)
		{
			if (eventDef.HasCustomAttributes) {
				AddCustomAttributes(eventDef.CustomAttributes, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region Property Attributes
		void AddAttributes(PropertyDef propertyDef, IUnresolvedEntity targetEntity)
		{
			if (propertyDef.HasCustomAttributes) {
				AddCustomAttributes(propertyDef.CustomAttributes, targetEntity.Attributes);
			}
		}
		#endregion
		
		#region MarshalAsAttribute (ConvertMarshalInfo)
		static readonly ITypeReference marshalAsAttributeTypeRef = typeof(MarshalAsAttribute).ToTypeReference();
		static readonly ITypeReference unmanagedTypeTypeRef = typeof(UnmanagedType).ToTypeReference();

		static IUnresolvedAttribute ConvertMarshalInfo(MarshalType marshalInfo)
		{
			DefaultUnresolvedAttribute attr = new DefaultUnresolvedAttribute(marshalAsAttributeTypeRef, new[] { unmanagedTypeTypeRef });
			attr.PositionalArguments.Add(new SimpleConstantValue(unmanagedTypeTypeRef, (int)marshalInfo.NativeType));

			var fami = marshalInfo as FixedArrayMarshalType;
			if (fami != null) {
				if (fami.IsSizeValid)
					attr.AddNamedFieldArgument("SizeConst", KnownTypeReference.Int32, (int)fami.Size);
				if (fami.IsElementTypeValid)
					attr.AddNamedFieldArgument("ArraySubType", unmanagedTypeTypeRef, (int)fami.ElementType);
			}
			var sami = marshalInfo as SafeArrayMarshalType;
			if (sami != null) {
				if (sami.IsVariantTypeValid)
					attr.AddNamedFieldArgument("SafeArraySubType", typeof(VarEnum).ToTypeReference(), (int)sami.VariantType);
				if (sami.IsUserDefinedSubTypeValid) {
					//TODO:
				}
			}
			var ami = marshalInfo as ArrayMarshalType;
			if (ami != null) {
				if (ami.IsElementTypeValid && ami.ElementType != NativeType.Max)
					attr.AddNamedFieldArgument("ArraySubType", unmanagedTypeTypeRef, (int)ami.ElementType);
				if (ami.IsSizeValid)
					attr.AddNamedFieldArgument("SizeConst", KnownTypeReference.Int32, (int)ami.Size);
				if (ami.Flags != 0 && ami.IsParamNumberValid)
					attr.AddNamedFieldArgument("SizeParamIndex", KnownTypeReference.Int16, (short)ami.ParamNumber);
			}
			var cmi = marshalInfo as CustomMarshalType;
			if (cmi != null) {
				if (cmi.CustomMarshaler != null)
					attr.AddNamedFieldArgument("MarshalType", KnownTypeReference.String, cmi.CustomMarshaler.FullName);
				if (!UTF8String.IsNullOrEmpty(cmi.Cookie))
					attr.AddNamedFieldArgument("MarshalCookie", KnownTypeReference.String, cmi.Cookie.String);
			}
			var fssmi = marshalInfo as FixedSysStringMarshalType;
			if (fssmi != null) {
				if (fssmi.IsSizeValid)
					attr.AddNamedFieldArgument("SizeConst", KnownTypeReference.Int32, (int)fssmi.Size);
			}
			var imti = marshalInfo as InterfaceMarshalType;
			if (imti != null) {
				//TODO:
			}
			var rmti = marshalInfo as RawMarshalType;
			if (rmti != null) {
				//TODO:
			}
			
			return attr;
		}
		#endregion
		
		#region Custom Attributes (ReadAttribute)
		void AddCustomAttributes(CustomAttributeCollection attributes, IList<IUnresolvedAttribute> targetCollection)
		{
			foreach (var attribute in attributes) {
				ITypeDefOrRef type = attribute.AttributeType;
				if (type.Namespace == "System.Runtime.CompilerServices") {
					if (type.Name == "DynamicAttribute" || type.Name == "ExtensionAttribute")
						continue;
				} else if (type.Name == "ParamArrayAttribute" && type.Namespace == "System") {
					continue;
				}
				targetCollection.Add(ReadAttribute(attribute));
			}
		}
		
		[CLSCompliant(false)]
		public IUnresolvedAttribute ReadAttribute(CustomAttribute attribute)
		{
			if (attribute == null)
				throw new ArgumentNullException("attribute");
			var ctor = attribute.Constructor;
			ITypeReference attributeType = ReadTypeReference(attribute.AttributeType);
			IList<ITypeReference> ctorParameterTypes = null;
			if (ctor.MethodSig.GetParamCount() > 0) {
				var parameters = ctor.MethodSig.GetParams();
				ctorParameterTypes = new ITypeReference[parameters.Count];
				for (int i = 0; i < ctorParameterTypes.Count; i++) {
					ctorParameterTypes[i] = ReadTypeReference(parameters[i].ToTypeDefOrRef());
				}
			}
			if (this.InterningProvider != null) {
				attributeType = this.InterningProvider.Intern(attributeType);
				ctorParameterTypes = this.InterningProvider.InternList(ctorParameterTypes);
			}
			return new dnlibUnresolvedAttribute(attributeType, ctorParameterTypes ?? EmptyList<ITypeReference>.Instance, attribute.GetBlob());
		}
		#endregion
		
		#region dnlibUnresolvedAttribute
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
		sealed class dnlibUnresolvedAttribute : IUnresolvedAttribute, ISupportsInterning
		{
			internal readonly ITypeReference attributeType;
			internal readonly IList<ITypeReference> ctorParameterTypes;
			internal readonly byte[] blob;

			public dnlibUnresolvedAttribute(ITypeReference attributeType, IList<ITypeReference> ctorParameterTypes, byte[] blob)
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
					throw new InvalidOperationException("Cannot resolve dnlibUnresolvedAttribute without a parent assembly");
				return new dnlibResolvedAttribute(context, this);
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
				dnlibUnresolvedAttribute o = other as dnlibUnresolvedAttribute;
				return o != null && attributeType == o.attributeType && ctorParameterTypes == o.ctorParameterTypes
					&& BlobEquals(blob, o.blob);
			}
		}
		#endregion
		
		#region dnlibResolvedAttribute
		sealed class dnlibResolvedAttribute : IAttribute
		{
			readonly ITypeResolveContext context;
			readonly byte[] blob;
			readonly IList<ITypeReference> ctorParameterTypes;
			readonly IType attributeType;
			
			IMethod constructor;
			volatile bool constructorResolved;
			
			IList<ResolveResult> positionalArguments;
			IList<KeyValuePair<IMember, ResolveResult>> namedArguments;
			
			public dnlibResolvedAttribute(ITypeResolveContext context, dnlibUnresolvedAttribute unresolved)
			{
				this.context = context;
				this.blob = unresolved.blob;
				this.ctorParameterTypes = unresolved.ctorParameterTypes;
				this.attributeType = unresolved.attributeType.Resolve(context);
			}

			public dnlibResolvedAttribute(ITypeResolveContext context, IType attributeType)
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
					ResolveResult arg = reader.ReadFixedArg(ctorParameter);
					positionalArguments.Add(arg);
					if (arg.IsError) {
						// After a decoding error, we must stop decoding the blob because
						// we might have read too few bytes due to the error.
						// Just fill up the remaining arguments with ErrorResolveResult:
						while (positionalArguments.Count < ctorParameterTypes.Count)
							positionalArguments.Add(ErrorResolveResult.UnknownError);
						return;
					}
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
							// Stop decoding when encountering an error:
							if (elements[i].IsError)
								return ErrorResolveResult.UnknownError;
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
				var b = ReadByte();
				switch (b) {
					case 0x53:
						memberType = EntityType.Field;
						break;
					case 0x54:
						memberType = EntityType.Property;
						break;
					default:
						throw new NotSupportedException(string.Format("Custom member type 0x{0:x} is not supported.", b));
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
				var b = ReadByte();
				switch (b) {
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
						throw new NotSupportedException(string.Format("Custom attribute type 0x{0:x} is not supported.", b));
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
		public IList<IUnresolvedAttribute> ReadSecurityDeclaration(DeclSecurity declSec)
		{
			if (declSec == null)
				throw new ArgumentNullException("secDecl");
			var result = new List<IUnresolvedAttribute>();
			AddSecurityAttributes(declSec, result);
			return result;
		}

		void AddSecurityAttributes(IList<DeclSecurity> declSecs, IList<IUnresolvedAttribute> targetCollection)
		{
			foreach (var secDecl in declSecs) {
				AddSecurityAttributes(secDecl, targetCollection);
			}
		}

		void AddSecurityAttributes(DeclSecurity declSec, IList<IUnresolvedAttribute> targetCollection)
		{
			byte[] blob = declSec.GetBlob();
			BlobReader reader = new BlobReader(blob, null);
			var securityAction = new SimpleConstantValue(securityActionTypeReference, (int)declSec.Action);
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
						attributes[i] = new dnlibResolvedAttribute(context, SpecialType.UnknownType);
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
					attributes[i] = new DefaultAttribute(
						attributeType,
						positionalArguments: new ResolveResult[] { securityActionRR },
						namedArguments: namedArgs);
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
		#endregion
		#endregion
		
		#region Read Type Definition
		DefaultUnresolvedTypeDefinition CreateTopLevelTypeDefinition(TypeDef typeDef)
		{
			string name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(typeDef.Name);
			var td = new DefaultUnresolvedTypeDefinition(typeDef.Namespace, name);
			if (typeDef.HasGenericParameters)
				InitTypeParameters(typeDef, td.TypeParameters);
			return td;
		}
		
		static void InitTypeParameters(TypeDef typeDef, IList<IUnresolvedTypeParameter> typeParameters)
		{
			// Type parameters are initialized within the constructor so that the class can be put into the type storage
			// before the rest of the initialization runs - this allows it to be available for early binding as soon as possible.
			for (int i = 0; i < typeDef.GenericParameters.Count; i++) {
				if (typeDef.GenericParameters[i].Number != i)
					throw new InvalidOperationException("g.Position != i");
				typeParameters.Add(new DefaultUnresolvedTypeParameter(
					EntityType.TypeDefinition, i, typeDef.GenericParameters[i].Name));
			}
		}
		
		void InitTypeParameterConstraints(TypeDef typeDefinition, IList<IUnresolvedTypeParameter> typeParameters)
		{
			for (int i = 0; i < typeParameters.Count; i++) {
				AddConstraints((DefaultUnresolvedTypeParameter)typeParameters[i], typeDefinition.GenericParameters[i]);
			}
		}
		
		void InitTypeDefinition(TypeDef typeDefinition, DefaultUnresolvedTypeDefinition td)
		{
			td.Kind = GetTypeKind(typeDefinition);
			InitTypeModifiers(typeDefinition, td);
			InitTypeParameterConstraints(typeDefinition, td.TypeParameters);
			
			// nested types can be initialized only after generic parameters were created
			InitNestedTypes(typeDefinition, td, td.NestedTypes);
			AddAttributes(typeDefinition, td);
			td.HasExtensionMethods = HasExtensionAttribute(typeDefinition);
			
			InitBaseTypes(typeDefinition, td.BaseTypes);
			
			td.AddDefaultConstructorIfRequired = (td.Kind == TypeKind.Struct || td.Kind == TypeKind.Enum);
			InitMembers(typeDefinition, td, td.Members);
			if (this.InterningProvider != null) {
				td.ApplyInterningProvider(this.InterningProvider);
			}
			td.Freeze();
			RegisterDnlibObject(td, typeDefinition);
		}
		
		void InitBaseTypes(TypeDef typeDef, IList<ITypeReference> baseTypes)
		{
			// set base classes
			if (typeDef.IsEnum) {
				foreach (FieldDef enumField in typeDef.Fields) {
					if (!enumField.IsStatic) {
						baseTypes.Add(ReadTypeReference(enumField.FieldType.ToTypeDefOrRef()));
						break;
					}
				}
			} else {
				if (typeDef.BaseType != null) {
					baseTypes.Add(ReadTypeReference(typeDef.BaseType));
				}
				if (typeDef.HasInterfaces) {
					foreach (InterfaceImpl iface in typeDef.Interfaces) {
						baseTypes.Add(ReadTypeReference(iface.Interface));
					}
				}
			}
		}
		
		void InitNestedTypes(TypeDef typeDef, IUnresolvedTypeDefinition declaringTypeDefinition, IList<IUnresolvedTypeDefinition> nestedTypes)
		{
			if (!typeDef.HasNestedTypes)
				return;
			foreach (TypeDef nestedTypeDef in typeDef.NestedTypes) {
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
					name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(name);
					var nestedType = new DefaultUnresolvedTypeDefinition(declaringTypeDefinition, name);
					InitTypeParameters(nestedTypeDef, nestedType.TypeParameters);
					nestedTypes.Add(nestedType);
					InitTypeDefinition(nestedTypeDef, nestedType);
				}
			}
		}
		
		static TypeKind GetTypeKind(TypeDef typeDef)
		{
			// set classtype
			if (typeDef.IsInterface) {
				return TypeKind.Interface;
			} else if (typeDef.IsEnum) {
				return TypeKind.Enum;
			} else if (typeDef.IsValueType) {
				return TypeKind.Struct;
			} else if (IsDelegate(typeDef)) {
				return TypeKind.Delegate;
			} else if (IsModule(typeDef)) {
				return TypeKind.Module;
			} else {
				return TypeKind.Class;
			}
		}
		
		static void InitTypeModifiers(TypeDef typeDef, AbstractUnresolvedEntity td)
		{
			td.IsSealed = typeDef.IsSealed;
			td.IsAbstract = typeDef.IsAbstract;
			switch (typeDef.Attributes & TypeAttributes.VisibilityMask) {
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
		
		static bool IsDelegate(TypeDef type)
		{
			if (type.BaseType != null && type.BaseType.Namespace == "System") {
				if (type.BaseType.Name == "MulticastDelegate")
					return true;
				if (type.BaseType.Name == "Delegate" && type.Name != "MulticastDelegate")
					return true;
			}
			return false;
		}
		
		static bool IsModule(TypeDef type)
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
		
		void InitMembers(TypeDef typeDef, IUnresolvedTypeDefinition td, IList<IUnresolvedMember> members)
		{
			HashSet<MethodDef> accessors = new HashSet<MethodDef>();
			foreach (var propertyDef in typeDef.Properties)
			{
				if (propertyDef.GetMethod != null)
					accessors.Add(propertyDef.GetMethod);
				if (propertyDef.SetMethod != null)
					accessors.Add(propertyDef.SetMethod);
			}
			foreach (var eventDef in typeDef.Events)
			{
				if (eventDef.AddMethod != null)
					accessors.Add(eventDef.AddMethod);
				if (eventDef.RemoveMethod != null)
					accessors.Add(eventDef.RemoveMethod);
				if (eventDef.InvokeMethod != null)
					accessors.Add(eventDef.InvokeMethod);
			}
			if (typeDef.HasMethods) {
				foreach (MethodDef method in typeDef.Methods) {
					if (IsVisible(method.Attributes) && !accessors.Contains(method)) {
						EntityType type = EntityType.Method;
						if (method.IsSpecialName) {
							if (method.IsConstructor)
								type = EntityType.Constructor;
							else if (method.Name.StartsWith("op_", StringComparison.Ordinal))
								type = EntityType.Operator;
						}
						members.Add(ReadMethod(method, td, type));
					}
				}
			}
			if (typeDef.HasFields) {
				foreach (FieldDef field in typeDef.Fields) {
					if (IsVisible(field.Attributes) && !field.IsSpecialName) {
						members.Add(ReadField(field, td));
					}
				}
			}
			if (typeDef.HasProperties) {
				string defaultMemberName = null;
				var defaultMemberAttribute = typeDef.CustomAttributes.Find(typeof(System.Reflection.DefaultMemberAttribute).FullName);
				if (defaultMemberAttribute != null && defaultMemberAttribute.ConstructorArguments.Count == 1) {
					defaultMemberName = (UTF8String)defaultMemberAttribute.ConstructorArguments[0].Value;
				}
				foreach (PropertyDef property in typeDef.Properties) {
					bool getterVisible = property.GetMethod != null && IsVisible(property.GetMethod.Attributes);
					bool setterVisible = property.SetMethod != null && IsVisible(property.SetMethod.Attributes);
					if (getterVisible || setterVisible) {
						EntityType type = EntityType.Property;
						if (property.PropertySig.Params.Count > 0) {
							// Try to detect indexer:
							if (property.Name == defaultMemberName) {
								type = EntityType.Indexer; // normal indexer
							} else if (property.Name.EndsWith(".Item", StringComparison.Ordinal) && (property.GetMethod ?? property.SetMethod).HasOverrides) {
								// explicit interface implementation of indexer
								type = EntityType.Indexer;
								// We can't really tell parameterized properties and indexers apart in this case without
								// resolving the interface, so we rely on the "Item" naming convention instead.
							}
						}
						members.Add(ReadProperty(property, td, type));
					}
				}
			}
			if (typeDef.HasEvents) {
				foreach (EventDef ev in typeDef.Events) {
					if (ev.AddMethod != null && IsVisible(ev.AddMethod.Attributes)) {
						members.Add(ReadEvent(ev, td));
					}
				}
			}
		}
		#endregion
		
		#region Lazy-Loaded Type Definition
		sealed class LazyDnlibTypeDefinition : AbstractUnresolvedEntity, IUnresolvedTypeDefinition
		{
			readonly dnlibLoader loader;
			readonly string namespaceName;
			readonly TypeDef dnlibTypeDef;
			readonly TypeKind kind;
			readonly IList<IUnresolvedTypeParameter> typeParameters;
			
			IList<ITypeReference> baseTypes;
			IList<IUnresolvedTypeDefinition> nestedTypes;
			IList<IUnresolvedMember> members;
			
			public LazyDnlibTypeDefinition(dnlibLoader loader, TypeDef typeDef)
			{
				this.loader = loader;
				this.dnlibTypeDef = typeDef;
				this.EntityType = EntityType.TypeDefinition;
				this.namespaceName = dnlibTypeDef.Namespace;
				this.Name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(dnlibTypeDef.Name);
				var tps = new List<IUnresolvedTypeParameter>();
				InitTypeParameters(dnlibTypeDef, tps);
				this.typeParameters = FreezableHelper.FreezeList(tps);

				this.kind = GetTypeKind(typeDef);
				InitTypeModifiers(typeDef, this);
				loader.InitTypeParameterConstraints(typeDef, typeParameters);

				loader.AddAttributes(typeDef, this);
				flags[FlagHasExtensionMethods] = HasExtensionAttribute(typeDef);
				
				if (loader.InterningProvider != null) {
					this.ApplyInterningProvider(loader.InterningProvider);
				}
				this.Freeze();
			}
			
			public override string Namespace {
				get { return namespaceName; }
				set { throw new NotSupportedException(); }
			}
			
			public override string FullName {
				// This works because LazyDnlibTypeDefinition is only used for top-level types
				get { return dnlibTypeDef.FullName; }
			}
			
			public override string ReflectionName {
				get { return dnlibTypeDef.FullName; }
			}
			
			public TypeKind Kind {
				get { return kind; }
			}
			
			public IList<IUnresolvedTypeParameter> TypeParameters {
				get { return typeParameters; }
			}
			
			public IList<ITypeReference> BaseTypes {
				get {
					var result = LazyInit.VolatileRead(ref this.baseTypes);
					if (result != null) {
						return result;
					}
					lock (loader.currentModule) {
						result = new List<ITypeReference>();
						loader.InitBaseTypes(dnlibTypeDef, result);
						return LazyInit.GetOrSet(ref this.baseTypes, FreezableHelper.FreezeList(result));
					}
				}
			}
			
			public IList<IUnresolvedTypeDefinition> NestedTypes {
				get {
					var result = LazyInit.VolatileRead(ref this.nestedTypes);
					if (result != null) {
						return result;
					}
					lock (loader.currentModule) {
						if (this.nestedTypes != null)
							return this.nestedTypes;
						result = new List<IUnresolvedTypeDefinition>();
						loader.InitNestedTypes(dnlibTypeDef, this, result);
						return LazyInit.GetOrSet(ref this.nestedTypes, FreezableHelper.FreezeList(result));
					}
				}
			}
			
			public IList<IUnresolvedMember> Members {
				get {
					var result = LazyInit.VolatileRead(ref this.members);
					if (result != null) {
						return result;
					}
					lock (loader.currentModule) {
						if (this.members != null)
							return this.members;
						result = new List<IUnresolvedMember>();
						loader.InitMembers(dnlibTypeDef, this, result);
						return LazyInit.GetOrSet(ref this.members, FreezableHelper.FreezeList(result));
					}
				}
			}
			
			public IEnumerable<IUnresolvedMethod> Methods {
				get { return Members.OfType<IUnresolvedMethod>(); }
			}
			
			public IEnumerable<IUnresolvedProperty> Properties {
				get { return Members.OfType<IUnresolvedProperty>(); }
			}
			
			public IEnumerable<IUnresolvedField> Fields {
				get { return Members.OfType<IUnresolvedField>(); }
			}
			
			public IEnumerable<IUnresolvedEvent> Events {
				get { return Members.OfType<IUnresolvedEvent>(); }
			}
			
			public bool AddDefaultConstructorIfRequired {
				get { return kind == TypeKind.Struct || kind == TypeKind.Enum; }
			}
			
			public bool? HasExtensionMethods {
				get { return flags[FlagHasExtensionMethods]; }
				// we always return true or false, never null.
				// FlagHasNoExtensionMethods is unused in LazyDnlibTypeDefinition
			}
			
			public override object Clone()
			{
				throw new NotSupportedException();
			}
			
			public IType Resolve(ITypeResolveContext context)
			{
				if (context == null)
					throw new ArgumentNullException("context");
				if (context.CurrentAssembly == null)
					throw new ArgumentException("An ITypeDefinition cannot be resolved in a context without a current assembly.");
				return context.CurrentAssembly.GetTypeDefinition(this)
					?? (IType)new UnknownType(this.Namespace, this.Name, this.TypeParameters.Count);
			}
			
			public ITypeResolveContext CreateResolveContext(ITypeResolveContext parentContext)
			{
				return parentContext;
			}
		}
		#endregion
		
		#region Read Method
		[CLSCompliant(false)]
		public IUnresolvedMethod ReadMethod(MethodDef method, IUnresolvedTypeDefinition parentType, EntityType methodType = EntityType.Method)
		{
			return ReadMethod(method, parentType, methodType, null);
		}
		
		IUnresolvedMethod ReadMethod(MethodDef method, IUnresolvedTypeDefinition parentType, EntityType methodType, IUnresolvedMember accessorOwner)
		{
			if (method == null)
				return null;
			DefaultUnresolvedMethod m = new DefaultUnresolvedMethod(parentType, method.Name);
			m.EntityType = methodType;
			m.AccessorOwner = accessorOwner;
			m.HasBody = method.HasBody;
			if (method.HasGenericParameters) {
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					if (method.GenericParameters[i].Number != i)
						throw new InvalidOperationException("g.Position != i");
					m.TypeParameters.Add(new DefaultUnresolvedTypeParameter(
						EntityType.Method, i, method.GenericParameters[i].Name));
				}
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					AddConstraints((DefaultUnresolvedTypeParameter)m.TypeParameters[i], method.GenericParameters[i]);
				}
			}
			
			m.ReturnType = ReadTypeReference(method.ReturnType.ToTypeDefOrRef(), typeAttributes: method.Parameters.ReturnParameter.ParamDef);
			
			if (HasAnyAttributes(method))
				AddAttributes(method, m.Attributes, m.ReturnTypeAttributes);
			TranslateModifiers(method, m);
			
			if (method.Parameters.Count > 0) {
				foreach (Parameter p in method.Parameters) {
					if (p.IsHiddenThisParameter)
						continue;
					m.Parameters.Add(ReadParameter(p));
				}
			}
			
			// mark as extension method if the attribute is set
			if (method.IsStatic && HasExtensionAttribute(method)) {
				m.IsExtensionMethod = true;
			}

			int lastDot = method.Name.LastIndexOf('.');
			if (lastDot >= 0 && method.HasOverrides) {
				// To be consistent with the parser-initialized type system, shorten the method name:
				m.Name = method.Name.Substring(lastDot + 1);
				m.IsExplicitInterfaceImplementation = true;
				foreach (var or in method.Overrides) {
					m.ExplicitInterfaceImplementations.Add(new DefaultMemberReference(
						accessorOwner != null ? EntityType.Accessor : EntityType.Method,
						ReadTypeReference(or.MethodDeclaration.DeclaringType),
						or.MethodDeclaration.Name, or.MethodDeclaration.NumberOfGenericParameters, m.Parameters.Select(p => p.Type).ToList()));
				}
			}

			FinishReadMember(m, method);
			return m;
		}
		
		static bool HasExtensionAttribute(IHasCustomAttribute provider)
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
		
		void TranslateModifiers(MethodDef method, AbstractUnresolvedMember m)
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
		public IUnresolvedParameter ReadParameter(Parameter parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");
			var type = ReadTypeReference(parameter.Type.ToTypeDefOrRef(), typeAttributes: parameter.ParamDef);
			var p = new DefaultUnresolvedParameter(type, parameter.Name);
			
			if (parameter.Type is ByRefSig) {
				if (parameter.ParamDef != null && !parameter.ParamDef.IsIn && parameter.ParamDef.IsOut)
					p.IsOut = true;
				else
					p.IsRef = true;
			}
			AddAttributes(parameter.ParamDef, p);
			
			if (parameter.ParamDef != null && parameter.ParamDef.IsOptional) {
				p.DefaultValue = new SimpleConstantValue(type, parameter.ParamDef.Constant.Value);
			}
			
			if ((parameter.Type is ArraySig || parameter.Type is SZArraySig) && parameter.ParamDef != null) {
				foreach (CustomAttribute att in parameter.ParamDef.CustomAttributes) {
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
		public IUnresolvedField ReadField(FieldDef field, IUnresolvedTypeDefinition parentType)
		{
			if (field == null)
				throw new ArgumentNullException("field");
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			
			DefaultUnresolvedField f = new DefaultUnresolvedField(parentType, field.Name);
			f.Accessibility = GetAccessibility(field.Attributes);
			f.IsReadOnly = field.IsInitOnly;
			f.IsStatic = field.IsStatic;
			f.ReturnType = ReadTypeReference(field.FieldType.ToTypeDefOrRef(), typeAttributes: field);
			if (field.HasConstant) {
				f.ConstantValue = new SimpleConstantValue(f.ReturnType, field.Constant.Value);
			}
			AddAttributes(field, f);
			
			CModReqdSig modreq = field.FieldType as CModReqdSig;
			if (modreq != null && modreq.Modifier.FullName == typeof(IsVolatile).FullName) {
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
		void AddConstraints(DefaultUnresolvedTypeParameter tp, GenericParam g)
		{
			switch (g.Flags & GenericParamAttributes.VarianceMask) {
				case GenericParamAttributes.Contravariant:
					tp.Variance = VarianceModifier.Contravariant;
					break;
				case GenericParamAttributes.Covariant:
					tp.Variance = VarianceModifier.Covariant;
					break;
			}
			
			tp.HasReferenceTypeConstraint = (g.Flags & GenericParamAttributes.ReferenceTypeConstraint) != 0;
			tp.HasValueTypeConstraint = (g.Flags & GenericParamAttributes.NotNullableValueTypeConstraint) != 0;
			tp.HasDefaultConstructorConstraint = (g.Flags & GenericParamAttributes.DefaultConstructorConstraint) != 0;
			
			if (g.GenericParamConstraints.Count > 0) {
				foreach (var constraint in g.GenericParamConstraints) {
					tp.Constraints.Add(ReadTypeReference(constraint.Constraint));
				}
			}
		}
		#endregion
		
		#region Read Property

		[CLSCompliant(false)]
		public IUnresolvedProperty ReadProperty(PropertyDef property, IUnresolvedTypeDefinition parentType, EntityType propertyType = EntityType.Property)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			DefaultUnresolvedProperty p = new DefaultUnresolvedProperty(parentType, property.Name);
			p.EntityType = propertyType;
			TranslateModifiers(property.GetMethod ?? property.SetMethod, p);
			p.ReturnType = ReadTypeReference(property.PropertySig.RetType.ToTypeDefOrRef(), typeAttributes: property);
			
			p.Getter = ReadMethod(property.GetMethod, parentType, EntityType.Accessor, p);
			p.Setter = ReadMethod(property.SetMethod, parentType, EntityType.Accessor, p);
			
			var parameters = property.PropertySig.GetParams();
			if (parameters.Count > 0) {
				for (int i = 0; i < parameters.Count; i++) {
					p.Parameters.Add(ReadParameter(new Parameter(i, i, parameters[i])));
				}
			}
			AddAttributes(property, p);

			var accessor = p.Getter ?? p.Setter;
			if (accessor != null && accessor.IsExplicitInterfaceImplementation) {
				p.Name = property.Name.Substring(property.Name.LastIndexOf('.') + 1);
				p.IsExplicitInterfaceImplementation = true;
				foreach (var mr in accessor.ExplicitInterfaceImplementations) {
					p.ExplicitInterfaceImplementations.Add(new AccessorOwnerMemberReference(mr));
				}
			}

			FinishReadMember(p, property);
			return p;
		}
		#endregion
		
		#region Read Event
		[CLSCompliant(false)]
		public IUnresolvedEvent ReadEvent(EventDef ev, IUnresolvedTypeDefinition parentType)
		{
			if (ev == null)
				throw new ArgumentNullException("ev");
			if (parentType == null)
				throw new ArgumentNullException("parentType");
			
			DefaultUnresolvedEvent e = new DefaultUnresolvedEvent(parentType, ev.Name);
			TranslateModifiers(ev.AddMethod, e);
			e.ReturnType = ReadTypeReference(ev.EventType, typeAttributes: ev);
			
			e.AddAccessor    = ReadMethod(ev.AddMethod,    parentType, EntityType.Accessor, e);
			e.RemoveAccessor = ReadMethod(ev.RemoveMethod, parentType, EntityType.Accessor, e);
			e.InvokeAccessor = ReadMethod(ev.InvokeMethod, parentType, EntityType.Accessor, e);
			
			AddAttributes(ev, e);
			
			var accessor = e.AddAccessor ?? e.RemoveAccessor ?? e.InvokeAccessor;
			if (accessor != null && accessor.IsExplicitInterfaceImplementation) {
				e.Name = ev.Name.Substring(ev.Name.LastIndexOf('.') + 1);
				e.IsExplicitInterfaceImplementation = true;
				foreach (var mr in accessor.ExplicitInterfaceImplementations) {
					e.ExplicitInterfaceImplementations.Add(new AccessorOwnerMemberReference(mr));
				}
			}

			FinishReadMember(e, ev);
			
			return e;
		}
		#endregion
		
		void FinishReadMember(AbstractUnresolvedMember member, IMemberRef dnlibDefinition)
		{
			if (this.InterningProvider != null)
				member.ApplyInterningProvider(this.InterningProvider);
			member.Freeze();
			RegisterDnlibObject(member, dnlibDefinition);
		}
		
		#region Type system translation table
		readonly Dictionary<object, object> typeSystemTranslationTable;
		
		void RegisterDnlibObject(IUnresolvedEntity typeSystemObject, IMemberRef dnlibObject)
		{
			if (OnEntityLoaded != null)
				OnEntityLoaded(typeSystemObject, dnlibObject);

			AddToTypeSystemTranslationTable(typeSystemObject, dnlibObject);
		}

		void AddToTypeSystemTranslationTable(object typeSystemObject, object dnlibObject)
		{
			if (typeSystemTranslationTable != null) {
				// When lazy-loading, the dictionary might be shared between multiple dnlib-loaders that are used concurrently
				lock (typeSystemTranslationTable) {
					typeSystemTranslationTable[typeSystemObject] = dnlibObject;
				}
			}
		}
		
		T InternalGetDnlibObject<T> (object typeSystemObject) where T : class
		{
			if (typeSystemObject == null)
				throw new ArgumentNullException ("typeSystemObject");
			if (!HasDnlibReferences)
				throw new NotSupportedException ("This instance contains no dnlib references.");
			object result;
			lock (typeSystemTranslationTable) {
				if (!typeSystemTranslationTable.TryGetValue (typeSystemObject, out result))
					return null;
			}
			return result as T;
		}
		
		[CLSCompliant(false)]
		public AssemblyDef GetDnlibObject (IUnresolvedAssembly content)
		{
			return InternalGetDnlibObject<AssemblyDef> (content);
		}
		
		[CLSCompliant(false)]
		public TypeDef GetDnlibObject (IUnresolvedTypeDefinition type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			return InternalGetDnlibObject<TypeDef> (type);
		}
		
		[CLSCompliant(false)]
		public MethodDef GetDnlibObject (IUnresolvedMethod method)
		{
			return InternalGetDnlibObject<MethodDef> (method);
		}
		
		[CLSCompliant(false)]
		public FieldDef GetDnlibObject (IUnresolvedField field)
		{
			return InternalGetDnlibObject<FieldDef> (field);
		}
		
		[CLSCompliant(false)]
		public EventDef GetDnlibObject (IUnresolvedEvent evt)
		{
			return InternalGetDnlibObject<EventDef> (evt);
		}
		
		[CLSCompliant(false)]
		public PropertyDef GetDnlibObject (IUnresolvedProperty property)
		{
			return InternalGetDnlibObject<PropertyDef> (property);
		}
		#endregion
	}
}
