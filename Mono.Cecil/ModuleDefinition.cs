//
// ModuleDefinition.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2010 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using SR = System.Reflection;

using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil {

	public enum ReadingMode {
		Immediate = 1,
		Deferred = 2,
	}

	public sealed class ReaderParameters {

		ReadingMode reading_mode;
		IAssemblyResolver assembly_resolver;
		Stream symbol_stream;
		ISymbolReaderProvider symbol_reader_provider;
		bool read_symbols;

		public ReadingMode ReadingMode {
			get { return reading_mode; }
			set { reading_mode = value; }
		}

		public IAssemblyResolver AssemblyResolver {
			get { return assembly_resolver; }
			set { assembly_resolver = value; }
		}

		public Stream SymbolStream {
			get { return symbol_stream; }
			set { symbol_stream = value; }
		}

		public ISymbolReaderProvider SymbolReaderProvider {
			get { return symbol_reader_provider; }
			set { symbol_reader_provider = value; }
		}

		public bool ReadSymbols {
			get { return read_symbols; }
			set { read_symbols = value; }
		}

		public ReaderParameters ()
			: this (ReadingMode.Deferred)
		{
		}

		public ReaderParameters (ReadingMode readingMode)
		{
			this.reading_mode = readingMode;
		}
	}

#if !READ_ONLY

	public sealed class ModuleParameters {

		ModuleKind kind;
		TargetRuntime runtime;
		TargetArchitecture architecture;
		IAssemblyResolver assembly_resolver;

		public ModuleKind Kind {
			get { return kind; }
			set { kind = value; }
		}

		public TargetRuntime Runtime {
			get { return runtime; }
			set { runtime = value; }
		}

		public TargetArchitecture Architecture {
			get { return architecture; }
			set { architecture = value; }
		}

		public IAssemblyResolver AssemblyResolver {
			get { return assembly_resolver; }
			set { assembly_resolver = value; }
		}

		public ModuleParameters ()
		{
			this.kind = ModuleKind.Dll;
			this.runtime = GetCurrentRuntime ();
			this.architecture = TargetArchitecture.I386;
		}

		static TargetRuntime GetCurrentRuntime ()
		{
#if !CF
			return typeof (object).Assembly.ImageRuntimeVersion.ParseRuntime ();
#else
			var corlib_version = typeof (object).Assembly.GetName ().Version;
			switch (corlib_version.Major) {
			case 1:
				return corlib_version.Minor == 0
					? TargetRuntime.Net_1_0
					: TargetRuntime.Net_1_1;
			case 2:
				return TargetRuntime.Net_2_0;
			case 4:
				return TargetRuntime.Net_4_0;
			default:
				throw new NotSupportedException ();
			}
#endif
		}
	}

	public sealed class WriterParameters {

		Stream symbol_stream;
		ISymbolWriterProvider symbol_writer_provider;
		bool write_symbols;
#if !SILVERLIGHT && !CF
		SR.StrongNameKeyPair key_pair;
#endif
		public Stream SymbolStream {
			get { return symbol_stream; }
			set { symbol_stream = value; }
		}

		public ISymbolWriterProvider SymbolWriterProvider {
			get { return symbol_writer_provider; }
			set { symbol_writer_provider = value; }
		}

		public bool WriteSymbols {
			get { return write_symbols; }
			set { write_symbols = value; }
		}
#if !SILVERLIGHT && !CF
		public SR.StrongNameKeyPair StrongNameKeyPair {
			get { return key_pair; }
			set { key_pair = value; }
		}
#endif
	}

#endif

	public sealed class ModuleDefinition : ModuleReference, ICustomAttributeProvider {

		internal Image Image;
		internal MetadataSystem MetadataSystem;
		internal ReadingMode ReadingMode;
		internal ISymbolReaderProvider SymbolReaderProvider;
		internal ISymbolReader SymbolReader;

		internal IAssemblyResolver assembly_resolver;
		internal TypeSystem type_system;

		readonly MetadataReader reader;
		readonly string fq_name;

		internal ModuleKind kind;
		TargetRuntime runtime;
		TargetArchitecture architecture;
		ModuleAttributes attributes;
		Guid mvid;

		internal AssemblyDefinition assembly;
		MethodDefinition entry_point;

#if !READ_ONLY
		MetadataImporter importer;
#endif
		Collection<CustomAttribute> custom_attributes;
		Collection<AssemblyNameReference> references;
		Collection<ModuleReference> modules;
		Collection<Resource> resources;
		Collection<ExportedType> exported_types;
		TypeDefinitionCollection types;

		public bool IsMain {
			get { return kind != ModuleKind.NetModule; }
		}

		public ModuleKind Kind {
			get { return kind; }
			set { kind = value; }
		}

		public TargetRuntime Runtime {
			get { return runtime; }
			set { runtime = value; }
		}

		public TargetArchitecture Architecture {
			get { return architecture; }
			set { architecture = value; }
		}

		public ModuleAttributes Attributes {
			get { return attributes; }
			set { attributes = value; }
		}

		public string FullyQualifiedName {
			get { return fq_name; }
		}

		public Guid Mvid {
			get { return mvid; }
			set { mvid = value; }
		}

		internal bool HasImage {
			get { return Image != null; }
		}

		public bool HasSymbols {
			get { return SymbolReader != null; }
		}

		public override MetadataScopeType MetadataScopeType {
			get { return MetadataScopeType.ModuleDefinition; }
		}

		public AssemblyDefinition Assembly {
			get { return assembly; }
		}

#if !READ_ONLY
		internal MetadataImporter MetadataImporter {
			get { return importer ?? (importer = new MetadataImporter (this)); }
		}
#endif

		public IAssemblyResolver AssemblyResolver {
			get { return assembly_resolver; }
		}

		public TypeSystem TypeSystem {
			get { return type_system ?? (type_system = TypeSystem.CreateTypeSystem (this)); }
		}

		public bool HasAssemblyReferences {
			get {
				if (references != null)
					return references.Count > 0;

				return HasImage && Image.HasTable (Table.AssemblyRef);
			}
		}

		public Collection<AssemblyNameReference> AssemblyReferences {
			get {
				if (references != null)
					return references;

				if (HasImage)
					return references = Read (this, (_, reader) => reader.ReadAssemblyReferences ());

				return references = new Collection<AssemblyNameReference> ();
			}
		}

		public bool HasModuleReferences {
			get {
				if (modules != null)
					return modules.Count > 0;

				return HasImage && Image.HasTable (Table.ModuleRef);
			}
		}

		public Collection<ModuleReference> ModuleReferences {
			get {
				if (modules != null)
					return modules;

				if (HasImage)
					return modules = Read (this, (_, reader) => reader.ReadModuleReferences ());

				return modules = new Collection<ModuleReference> ();
			}
		}

		public bool HasResources {
			get {
				if (resources != null)
					return resources.Count > 0;

				if (HasImage)
					return Image.HasTable (Table.ManifestResource) || Read (this, (_, reader) => reader.HasFileResource ());

				return false;
			}
		}

		public Collection<Resource> Resources {
			get {
				if (resources != null)
					return resources;

				if (HasImage)
					return resources = Read (this, (_, reader) => reader.ReadResources ());

				return resources = new Collection<Resource> ();
			}
		}

		public bool HasCustomAttributes {
			get {
				if (custom_attributes != null)
					return custom_attributes.Count > 0;

				return this.GetHasCustomAttributes (this);
			}
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return custom_attributes ?? (custom_attributes = this.GetCustomAttributes (this)); }
		}

		public bool HasTypes {
			get {
				if (types != null)
					return types.Count > 0;

				return HasImage && Image.HasTable (Table.TypeDef);
			}
		}

		public Collection<TypeDefinition> Types {
			get {
				if (types != null)
					return types;

				if (HasImage)
					return types = Read (this, (_, reader) => reader.ReadTypes ());

				return types = new TypeDefinitionCollection (this);
			}
		}

		public bool HasExportedTypes {
			get {
				if (exported_types != null)
					return exported_types.Count > 0;

				return HasImage && Image.HasTable (Table.ExportedType);
			}
		}

		public Collection<ExportedType> ExportedTypes {
			get {
				if (exported_types != null)
					return exported_types;

				if (HasImage)
					return exported_types = Read (this, (_, reader) => reader.ReadExportedTypes ());

				return exported_types = new Collection<ExportedType> ();
			}
		}

		public MethodDefinition EntryPoint {
			get {
				if (entry_point != null)
					return entry_point;

				if (HasImage)
					return entry_point = Read (this, (_, reader) => reader.ReadEntryPoint ());

				return entry_point = null;
			}
			set { entry_point = value; }
		}

		internal ModuleDefinition ()
		{
			this.MetadataSystem = new MetadataSystem ();
			this.token = new MetadataToken (TokenType.Module, 1);
			this.assembly_resolver = GlobalAssemblyResolver.Instance;
		}

		internal ModuleDefinition (Image image)
			: this ()
		{
			this.Image = image;
			this.kind = image.Kind;
			this.runtime = image.Runtime;
			this.architecture = image.Architecture;
			this.attributes = image.Attributes;
			this.fq_name = image.FileName;

			this.reader = new MetadataReader (this);
		}

		public bool HasTypeReference (string fullName)
		{
			return HasTypeReference (string.Empty, fullName);
		}

		public bool HasTypeReference (string scope, string fullName)
		{
			CheckFullName (fullName);

			if (!HasImage)
				return false;

			return Read (this, (_, reader) => reader.GetTypeReference (scope, fullName) != null);
		}

		public bool TryGetTypeReference (string fullName, out TypeReference type)
		{
			return TryGetTypeReference (string.Empty, fullName, out type);
		}

		public bool TryGetTypeReference (string scope, string fullName, out TypeReference type)
		{
			CheckFullName (fullName);

			if (!HasImage) {
				type = null;
				return false;
			}

			return (type = Read (this, (_, reader) => reader.GetTypeReference (scope, fullName))) != null;
		}

		public IEnumerable<TypeReference> GetTypeReferences ()
		{
			if (!HasImage)
				return Empty<TypeReference>.Array;

			return Read (this, (_, reader) => reader.GetTypeReferences ());
		}

		public IEnumerable<MemberReference> GetMemberReferences ()
		{
			if (!HasImage)
				return Empty<MemberReference>.Array;

			return Read (this, (_, reader) => reader.GetMemberReferences ());
		}

		public TypeDefinition GetType (string fullName)
		{
			CheckFullName (fullName);

			var position = fullName.IndexOf ('/');
			if (position > 0)
				return GetNestedType (fullName);

			return ((TypeDefinitionCollection) this.Types).GetType (fullName);
		}

		public TypeDefinition GetType (string @namespace, string name)
		{
			Mixin.CheckName (name);

			return ((TypeDefinitionCollection) this.Types).GetType (@namespace ?? string.Empty, name);
		}

		static void CheckFullName (string fullName)
		{
			if (fullName == null)
				throw new ArgumentNullException ("fullName");
			if (fullName.Length == 0)
				throw new ArgumentException ();
		}

		TypeDefinition GetNestedType (string fullname)
		{
			var names = fullname.Split ('/');
			var type = GetType (names [0]);

			if (type == null)
				return null;

			for (int i = 1; i < names.Length; i++) {
				var nested_type = type.GetNestedType (names [i]);
				if (nested_type == null)
					return null;

				type = nested_type;
			}

			return type;
		}

		internal FieldDefinition Resolve (FieldReference field)
		{
			return MetadataResolver.Resolve (AssemblyResolver, field);
		}

		internal MethodDefinition Resolve (MethodReference method)
		{
			return MetadataResolver.Resolve (AssemblyResolver, method);
		}

		internal TypeDefinition Resolve (TypeReference type)
		{
			return MetadataResolver.Resolve (AssemblyResolver, type);
		}

#if !READ_ONLY

		static void CheckType (object type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
		}

		static void CheckField (object field)
		{
			if (field == null)
				throw new ArgumentNullException ("field");
		}

		static void CheckMethod (object method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");
		}

		static void CheckContext (IGenericParameterProvider context, ModuleDefinition module)
		{
			if (context == null)
				return;

			if (context.Module != module)
				throw new ArgumentException ();
		}

#if !CF
		public TypeReference Import (Type type)
		{
			CheckType (type);

			return MetadataImporter.ImportType (type, null, ImportGenericKind.Definition);
		}

		public TypeReference Import (Type type, TypeReference context)
		{
			return Import (type, (IGenericParameterProvider) context);
		}

		public TypeReference Import (Type type, MethodReference context)
		{
			return Import (type, (IGenericParameterProvider) context);
		}

		TypeReference Import (Type type, IGenericParameterProvider context)
		{
			CheckType (type);
			CheckContext (context, this);

			return MetadataImporter.ImportType (
				type,
				(IGenericContext) context,
				context != null
					? ImportGenericKind.Open
					: ImportGenericKind.Definition);
		}

		public FieldReference Import (SR.FieldInfo field)
		{
			CheckField (field);

			return MetadataImporter.ImportField (field, null);
		}

		public FieldReference Import (SR.FieldInfo field, TypeReference context)
		{
			return Import (field, (IGenericParameterProvider) context);
		}

		public FieldReference Import (SR.FieldInfo field, MethodReference context)
		{
			return Import (field, (IGenericParameterProvider) context);
		}

		FieldReference Import (SR.FieldInfo field, IGenericParameterProvider context)
		{
			CheckField (field);
			CheckContext (context, this);

			return MetadataImporter.ImportField (field, (IGenericContext) context);
		}

		public MethodReference Import (SR.MethodBase method)
		{
			CheckMethod (method);

			return MetadataImporter.ImportMethod (method, null, ImportGenericKind.Definition);
		}

		public MethodReference Import (SR.MethodBase method, TypeReference context)
		{
			return Import (method, (IGenericParameterProvider) context);
		}

		public MethodReference Import (SR.MethodBase method, MethodReference context)
		{
			return Import (method, (IGenericParameterProvider) context);
		}

		MethodReference Import (SR.MethodBase method, IGenericParameterProvider context)
		{
			CheckMethod (method);
			CheckContext (context, this);

			return MetadataImporter.ImportMethod (method,
				(IGenericContext) context,
				context != null
					? ImportGenericKind.Open
					: ImportGenericKind.Definition);
		}
#endif

		public TypeReference Import (TypeReference type)
		{
			CheckType (type);

			if (type.Module == this)
				return type;

			return MetadataImporter.ImportType (type, null);
		}

		public TypeReference Import (TypeReference type, TypeReference context)
		{
			return Import (type, (IGenericParameterProvider) context);
		}

		public TypeReference Import (TypeReference type, MethodReference context)
		{
			return Import (type, (IGenericParameterProvider) context);
		}

		TypeReference Import (TypeReference type, IGenericParameterProvider context)
		{
			CheckType (type);

			if (type.Module == this)
				return type;

			CheckContext (context, this);

			return MetadataImporter.ImportType (type, (IGenericContext) context);
		}

		public FieldReference Import (FieldReference field)
		{
			CheckField (field);

			if (field.Module == this)
				return field;

			return MetadataImporter.ImportField (field, null);
		}

		public FieldReference Import (FieldReference field, TypeReference context)
		{
			return Import (field, (IGenericParameterProvider) context);
		}

		public FieldReference Import (FieldReference field, MethodReference context)
		{
			return Import (field, (IGenericParameterProvider) context);
		}

		FieldReference Import (FieldReference field, IGenericParameterProvider context)
		{
			CheckField (field);

			if (field.Module == this)
				return field;

			CheckContext (context, this);

			return MetadataImporter.ImportField (field, (IGenericContext) context);
		}

		public MethodReference Import (MethodReference method)
		{
			CheckMethod (method);

			if (method.Module == this)
				return method;

			return MetadataImporter.ImportMethod (method, null);
		}

		public MethodReference Import (MethodReference method, TypeReference context)
		{
			return Import (method, (IGenericParameterProvider) context);
		}

		public MethodReference Import (MethodReference method, MethodReference context)
		{
			return Import (method, (IGenericParameterProvider) context);
		}

		MethodReference Import (MethodReference method, IGenericParameterProvider context)
		{
			CheckMethod (method);

			if (method.Module == this)
				return method;

			CheckContext (context, this);

			return MetadataImporter.ImportMethod (method, (IGenericContext) context);
		}

#endif

		public IMetadataTokenProvider LookupToken (int token)
		{
			return LookupToken (new MetadataToken ((uint) token));
		}

		public IMetadataTokenProvider LookupToken (MetadataToken token)
		{
			return Read (this, (_, reader) => reader.LookupToken (token));
		}

		internal TRet Read<TItem, TRet> (TItem item, Func<TItem, MetadataReader, TRet> read)
		{
			var position = reader.position;
			var context = reader.context;

			var ret = read (item, reader);

			reader.position = position;
			reader.context = context;

			return ret;
		}

		void ProcessDebugHeader ()
		{
			if (Image == null || Image.Debug.IsZero)
				return;

			byte [] header;
			var directory = Image.GetDebugHeader (out header);

			if (!SymbolReader.ProcessDebugHeader (directory, header))
				throw new InvalidOperationException ();
		}

#if !READ_ONLY

		public static ModuleDefinition CreateModule (string name, ModuleKind kind)
		{
			return CreateModule (name, new ModuleParameters { Kind = kind });
		}

		public static ModuleDefinition CreateModule (string name, ModuleParameters parameters)
		{
			Mixin.CheckName (name);
			Mixin.CheckParameters (parameters);

			var module = new ModuleDefinition {
				Name = name,
				kind = parameters.Kind,
				runtime = parameters.Runtime,
				architecture = parameters.Architecture,
				mvid = Guid.NewGuid (),
				Attributes = ModuleAttributes.ILOnly,
			};

			if (parameters.AssemblyResolver != null)
				module.assembly_resolver = parameters.AssemblyResolver;

			if (parameters.Kind != ModuleKind.NetModule) {
				var assembly = new AssemblyDefinition ();
				module.assembly = assembly;
				module.assembly.Name = new AssemblyNameDefinition (name, new Version (0, 0));
				assembly.main_module = module;
			}

			module.Types.Add (new TypeDefinition (string.Empty, "<Module>", TypeAttributes.NotPublic));

			return module;
		}

#endif

		public void ReadSymbols ()
		{
			if (string.IsNullOrEmpty (fq_name))
				throw new InvalidOperationException ();

			var provider = SymbolProvider.GetPlatformReaderProvider ();

			SymbolReader = provider.GetSymbolReader (this, fq_name);

			ProcessDebugHeader ();
		}

		public void ReadSymbols (ISymbolReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException ("reader");

			SymbolReader = reader;

			ProcessDebugHeader ();
		}

		public static ModuleDefinition ReadModule (string fileName)
		{
			return ReadModule (fileName, new ReaderParameters (ReadingMode.Deferred));
		}

		public static ModuleDefinition ReadModule (Stream stream)
		{
			return ReadModule (stream, new ReaderParameters (ReadingMode.Deferred));
		}

		public static ModuleDefinition ReadModule (string fileName, ReaderParameters parameters)
		{
			using (var stream = GetFileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				return ReadModule (stream, parameters);
			}
		}

		static void CheckStream (object stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");
		}

		public static ModuleDefinition ReadModule (Stream stream, ReaderParameters parameters)
		{
			CheckStream (stream);
			if (!stream.CanRead || !stream.CanSeek)
				throw new ArgumentException ();
			Mixin.CheckParameters (parameters);

			return ModuleReader.CreateModuleFrom (
				ImageReader.ReadImageFrom (stream),
				parameters);
		}

		static Stream GetFileStream (string fileName, FileMode mode, FileAccess access, FileShare share)
		{
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			if (fileName.Length == 0)
				throw new ArgumentException ();

			return new FileStream (fileName, mode, access, share);
		}

#if !READ_ONLY

		public void Write (string fileName)
		{
			Write (fileName, new WriterParameters ());
		}

		public void Write (Stream stream)
		{
			Write (stream, new WriterParameters ());
		}

		public void Write (string fileName, WriterParameters parameters)
		{
			using (var stream = GetFileStream (fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None)) {
				Write (stream, parameters);
			}
		}

		public void Write (Stream stream, WriterParameters parameters)
		{
			CheckStream (stream);
			if (!stream.CanWrite || !stream.CanSeek)
				throw new ArgumentException ();
			Mixin.CheckParameters (parameters);

			ModuleWriter.WriteModuleTo (this, stream, parameters);
		}

#endif

	}

	static partial class Mixin {

		public static void CheckParameters (object parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException ("parameters");
		}

		public static bool HasImage (this ModuleDefinition self)
		{
			return self != null && self.HasImage;
		}

		public static string GetFullyQualifiedName (this Stream self)
		{
#if !SILVERLIGHT
			var file_stream = self as FileStream;
			if (file_stream == null)
				return string.Empty;

			return Path.GetFullPath (file_stream.Name);
#else
			return string.Empty;
#endif
		}

		public static TargetRuntime ParseRuntime (this string self)
		{
			switch (self [1]) {
			case '1':
				return self [3] == '0'
					? TargetRuntime.Net_1_0
					: TargetRuntime.Net_1_1;
			case '2':
				return TargetRuntime.Net_2_0;
			case '4':
			default:
				return TargetRuntime.Net_4_0;
			}
		}
	}
}
