//
// AssemblyReader.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
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
using System.Text;

using Mono.Collections.Generic;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;

using RVA = System.UInt32;

namespace Mono.Cecil {

	abstract class ModuleReader {

		readonly protected Image image;
		readonly protected ModuleDefinition module;

		protected ModuleReader (Image image, ReadingMode mode)
		{
			this.image = image;
			this.module = new ModuleDefinition (image);
			this.module.ReadingMode = mode;
		}

		protected abstract void ReadModule ();

		protected void ReadModuleManifest (MetadataReader reader)
		{
			reader.Populate (module);

			ReadAssembly (reader);
		}

		void ReadAssembly (MetadataReader reader)
		{
			var name = reader.ReadAssemblyNameDefinition ();
			if (name == null) {
				module.kind = ModuleKind.NetModule;
				return;
			}

			var assembly = new AssemblyDefinition ();
			assembly.Name = name;

			module.assembly = assembly;
			assembly.main_module = module;
		}

		public static ModuleDefinition CreateModuleFrom (Image image, ReaderParameters parameters)
		{
			var module = ReadModule (image, parameters);

			ReadSymbols (module, parameters);

			if (parameters.AssemblyResolver != null)
				module.assembly_resolver = parameters.AssemblyResolver;

			return module;
		}

		static void ReadSymbols (ModuleDefinition module, ReaderParameters parameters)
		{
			var symbol_reader_provider = parameters.SymbolReaderProvider;

			if (symbol_reader_provider == null && parameters.ReadSymbols)
				symbol_reader_provider = SymbolProvider.GetPlatformReaderProvider ();

			if (symbol_reader_provider != null) {
				module.SymbolReaderProvider = symbol_reader_provider;

				var reader = parameters.SymbolStream != null
					? symbol_reader_provider.GetSymbolReader (module, parameters.SymbolStream)
					: symbol_reader_provider.GetSymbolReader (module, module.FullyQualifiedName);

				module.ReadSymbols (reader);
			}
		}

		static ModuleDefinition ReadModule (Image image, ReaderParameters parameters)
		{
			var reader = CreateModuleReader (image, parameters.ReadingMode);
			reader.ReadModule ();
			return reader.module;
		}

		static ModuleReader CreateModuleReader (Image image, ReadingMode mode)
		{
			switch (mode) {
			case ReadingMode.Immediate:
				return new ImmediateModuleReader (image);
			case ReadingMode.Deferred:
				return new DeferredModuleReader (image);
			default:
				throw new ArgumentException ();
			}
		}
	}

	sealed class ImmediateModuleReader : ModuleReader {

		public ImmediateModuleReader (Image image)
			: base (image, ReadingMode.Immediate)
		{
		}

		protected override void ReadModule ()
		{
			this.module.Read (this.module, (module, reader) => {
				ReadModuleManifest (reader);
				ReadModule (module);
				return module;
			});
		}

		public static void ReadModule (ModuleDefinition module)
		{
			if (module.HasAssemblyReferences)
				Read (module.AssemblyReferences);
			if (module.HasResources)
				Read (module.Resources);
			if (module.HasModuleReferences)
				Read (module.ModuleReferences);
			if (module.HasTypes)
				ReadTypes (module.Types);
			if (module.HasExportedTypes)
				Read (module.ExportedTypes);
			if (module.HasCustomAttributes)
				Read (module.CustomAttributes);

			var assembly = module.Assembly;
			if (assembly == null)
				return;

			if (assembly.HasCustomAttributes)
				Read (assembly.CustomAttributes);
			if (assembly.HasSecurityDeclarations)
				Read (assembly.SecurityDeclarations);
		}

		static void ReadTypes (Collection<TypeDefinition> types)
		{
			for (int i = 0; i < types.Count; i++)
				ReadType (types [i]);
		}

		static void ReadType (TypeDefinition type)
		{
			ReadGenericParameters (type);

			if (type.HasInterfaces)
				Read (type.Interfaces);

			if (type.HasNestedTypes)
				ReadTypes (type.NestedTypes);

			if (type.HasLayoutInfo)
				Read (type.ClassSize);

			if (type.HasFields)
				ReadFields (type);

			if (type.HasMethods)
				ReadMethods (type);

			if (type.HasProperties)
				ReadProperties (type);

			if (type.HasEvents)
				ReadEvents (type);

			ReadSecurityDeclarations (type);
			ReadCustomAttributes (type);
		}

		static void ReadGenericParameters (IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
				return;

			var parameters = provider.GenericParameters;

			for (int i = 0; i < parameters.Count; i++) {
				var parameter = parameters [i];

				if (parameter.HasConstraints)
					Read (parameter.Constraints);

				if (parameter.HasCustomAttributes)
					Read (parameter.CustomAttributes);
			}
		}

		static void ReadSecurityDeclarations (ISecurityDeclarationProvider provider)
		{
			if (provider.HasSecurityDeclarations)
				Read (provider.SecurityDeclarations);
		}

		static void ReadCustomAttributes (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes)
				Read (provider.CustomAttributes);
		}

		static void ReadFields (TypeDefinition type)
		{
			var fields = type.Fields;

			for (int i = 0; i < fields.Count; i++) {
				var field = fields [i];

				if (field.HasConstant)
					Read (field.Constant);

				if (field.HasLayoutInfo)
					Read (field.Offset);

				if (field.RVA > 0)
					Read (field.InitialValue);

				if (field.HasMarshalInfo)
					Read (field.MarshalInfo);

				ReadCustomAttributes (field);
			}
		}

		static void ReadMethods (TypeDefinition type)
		{
			var methods = type.Methods;

			for (int i = 0; i < methods.Count; i++) {
				var method = methods [i];

				ReadGenericParameters (method);

				if (method.HasParameters)
					ReadParameters (method);

				if (method.HasOverrides)
					Read (method.Overrides);

				if (method.IsPInvokeImpl)
					Read (method.PInvokeInfo);

				ReadSecurityDeclarations (method);
				ReadCustomAttributes (method);

				var return_type = method.MethodReturnType;
				if (return_type.HasConstant)
					Read (return_type.Constant);

				if (return_type.HasMarshalInfo)
					Read (return_type.MarshalInfo);

				ReadCustomAttributes (return_type);
			}
		}

		static void ReadParameters (MethodDefinition method)
		{
			var parameters = method.Parameters;

			for (int i = 0; i < parameters.Count; i++) {
				var parameter = parameters [i];

				if (parameter.HasConstant)
					Read (parameter.Constant);

				if (parameter.HasMarshalInfo)
					Read (parameter.MarshalInfo);

				ReadCustomAttributes (parameter);
			}
		}

		static void ReadProperties (TypeDefinition type)
		{
			var properties = type.Properties;

			for (int i = 0; i < properties.Count; i++) {
				var property = properties [i];

				Read (property.GetMethod);

				if (property.HasConstant)
					Read (property.Constant);

				ReadCustomAttributes (property);
			}
		}

		static void ReadEvents (TypeDefinition type)
		{
			var events = type.Events;

			for (int i = 0; i < events.Count; i++) {
				var @event = events [i];

				Read (@event.AddMethod);

				ReadCustomAttributes (@event);
			}
		}

		static void Read (object collection)
		{
		}
	}

	sealed class DeferredModuleReader : ModuleReader {

		public DeferredModuleReader (Image image)
			: base (image, ReadingMode.Deferred)
		{
		}

		protected override void ReadModule ()
		{
			this.module.Read (this.module, (module, reader) => {
				ReadModuleManifest (reader);
				return module;
			});
		}
	}

	sealed class MetadataReader : ByteBuffer {

		readonly internal Image image;
		readonly internal ModuleDefinition module;
		readonly internal MetadataSystem metadata;

		internal IGenericContext context;
		internal CodeReader code;

		uint Position {
			get { return (uint) base.position; }
			set { base.position = (int) value; }
		}

		public MetadataReader (ModuleDefinition module)
			: base (module.Image.MetadataSection.Data)
		{
			this.image = module.Image;
			this.module = module;
			this.metadata = module.MetadataSystem;
			this.code = new CodeReader (image.MetadataSection, this);
		}

		int GetCodedIndexSize (CodedIndex index)
		{
			return image.GetCodedIndexSize (index);
		}

		uint ReadByIndexSize (int size)
		{
			if (size == 4)
				return ReadUInt32 ();
			else
				return ReadUInt16 ();
		}

		byte [] ReadBlob ()
		{
			var blob_heap = image.BlobHeap;
			if (blob_heap == null) {
				position += 2;
				return Empty<byte>.Array;
			}

			return blob_heap.Read (ReadBlobIndex ());
		}

		byte [] ReadBlob (uint signature)
		{
			var blob_heap = image.BlobHeap;
			if (blob_heap == null)
				return Empty<byte>.Array;

			return blob_heap.Read (signature);
		}

		uint ReadBlobIndex ()
		{
			var blob_heap = image.BlobHeap;
			return ReadByIndexSize (blob_heap != null ? blob_heap.IndexSize : 2);
		}

		string ReadString ()
		{
			return image.StringHeap.Read (ReadByIndexSize (image.StringHeap.IndexSize));
		}

		uint ReadStringIndex ()
		{
			return ReadByIndexSize (image.StringHeap.IndexSize);
		}

		uint ReadTableIndex (Table table)
		{
			return ReadByIndexSize (image.GetTableIndexSize (table));
		}

		MetadataToken ReadMetadataToken (CodedIndex index)
		{
			return index.GetMetadataToken (ReadByIndexSize (GetCodedIndexSize (index)));
		}

		int MoveTo (Table table)
		{
			var info = image.TableHeap [table];
			if (info.Length != 0)
				Position = info.Offset;

			return (int) info.Length;
		}

		bool MoveTo (Table table, uint row)
		{
			var info = image.TableHeap [table];
			var length = info.Length;
			if (length == 0 || row > length)
				return false;

			Position = info.Offset + (info.RowSize * (row - 1));
			return true;
		}

		public AssemblyNameDefinition ReadAssemblyNameDefinition ()
		{
			if (MoveTo (Table.Assembly) == 0)
				return null;

			var name = new AssemblyNameDefinition ();

			name.HashAlgorithm = (AssemblyHashAlgorithm) ReadUInt32 ();

			PopulateVersionAndFlags (name);

			name.PublicKey = ReadBlob ();

			PopulateNameAndCulture (name);

			return name;
		}

		public ModuleDefinition Populate (ModuleDefinition module)
		{
			if (MoveTo (Table.Module) == 0)
				return module;

			Advance (2); // Generation

			module.Name = ReadString ();
			module.Mvid = image.GuidHeap.Read (ReadByIndexSize (image.GuidHeap.IndexSize));

			return module;
		}

		void InitializeAssemblyReferences ()
		{
			if (metadata.AssemblyReferences != null)
				return;

			int length = MoveTo (Table.AssemblyRef);
			var references = metadata.AssemblyReferences = new AssemblyNameReference [length];

			for (uint i = 0; i < length; i++) {
				var reference = new AssemblyNameReference ();
				reference.token = new MetadataToken (TokenType.AssemblyRef, i + 1);

				PopulateVersionAndFlags (reference);

				var key_or_token = ReadBlob ();

				if (reference.HasPublicKey)
					reference.PublicKey = key_or_token;
				else
					reference.PublicKeyToken = key_or_token;

				PopulateNameAndCulture (reference);

				reference.Hash = ReadBlob ();

				references [i] = reference;
			}
		}

		public Collection<AssemblyNameReference> ReadAssemblyReferences ()
		{
			InitializeAssemblyReferences ();

			return new Collection<AssemblyNameReference> (metadata.AssemblyReferences);
		}

		public MethodDefinition ReadEntryPoint ()
		{
			if (module.Kind != ModuleKind.Console && module.Kind != ModuleKind.Windows)
				return null;

			var token = new MetadataToken (module.Image.EntryPointToken);

			return GetMethodDefinition (token.RID);
		}

		public Collection<ModuleDefinition> ReadModules ()
		{
			var modules = new Collection<ModuleDefinition> (1);
			modules.Add (this.module);

			int length = MoveTo (Table.File);
			for (uint i = 1; i <= length; i++) {
				var attributes = (FileAttributes) ReadUInt32 ();
				var name = ReadString ();
				ReadBlobIndex ();

				if (attributes != FileAttributes.ContainsMetaData)
					continue;

				var parameters = new ReaderParameters {
					ReadingMode = module.ReadingMode,
					SymbolReaderProvider = module.SymbolReaderProvider,
				};

				modules.Add (ModuleDefinition.ReadModule (
					GetModuleFileName (name), parameters));
			}

			return modules;
		}

		string GetModuleFileName (string name)
		{
			if (module.FullyQualifiedName == null)
				throw new NotSupportedException ();

			var path = Path.GetDirectoryName (module.FullyQualifiedName);
			return Path.Combine (path, name);
		}

		void InitializeModuleReferences ()
		{
			if (metadata.ModuleReferences != null)
				return;

			int length = MoveTo (Table.ModuleRef);
			var references = metadata.ModuleReferences = new ModuleReference [length];

			for (uint i = 0; i < length; i++) {
				var reference = new ModuleReference (ReadString ());
				reference.token = new MetadataToken (TokenType.ModuleRef, i + 1);

				references [i] = reference;
			}
		}

		public Collection<ModuleReference> ReadModuleReferences ()
		{
			InitializeModuleReferences ();

			return new Collection<ModuleReference> (metadata.ModuleReferences);
		}

		public bool HasFileResource ()
		{
			int length = MoveTo (Table.File);
			if (length == 0)
				return false;

			for (uint i = 1; i <= length; i++)
				if (ReadFileRecord (i).Col1 == FileAttributes.ContainsNoMetaData)
					return true;

			return false;
		}

		public Collection<Resource> ReadResources ()
		{
			int length = MoveTo (Table.ManifestResource);
			var resources = new Collection<Resource> (length);

			for (int i = 1; i <= length; i++) {
				var offset = ReadUInt32 ();
				var flags = (ManifestResourceAttributes) ReadUInt32 ();
				var name = ReadString ();
				var implementation = ReadMetadataToken (CodedIndex.Implementation);

				Resource resource;

				if (implementation.RID == 0) {
					resource = new EmbeddedResource (name, flags, offset, this);
				} else if (implementation.TokenType == TokenType.AssemblyRef) {
					resource = new AssemblyLinkedResource (name, flags) {
						Assembly = (AssemblyNameReference) GetTypeReferenceScope (implementation),
					};
				} else if (implementation.TokenType == TokenType.File) {
					var file_record = ReadFileRecord (implementation.RID);

					resource = new LinkedResource (name, flags) {
						File = file_record.Col2,
						hash = ReadBlob (file_record.Col3)
					};
				} else
					throw new NotSupportedException ();

				resources.Add (resource);
			}

			return resources;
		}

		Row<FileAttributes, string, uint> ReadFileRecord (uint rid)
		{
			var position = this.position;

			if (!MoveTo (Table.File, rid))
				throw new ArgumentException ();

			var record = new Row<FileAttributes, string, uint> (
				(FileAttributes) ReadUInt32 (),
				ReadString (),
				ReadBlobIndex ());

			this.position = position;

			return record;
		}

		public MemoryStream GetManagedResourceStream (uint offset)
		{
			var rva = image.Resources.VirtualAddress;
			var section = image.GetSectionAtVirtualAddress (rva);
			var position = (rva - section.VirtualAddress) + offset;
			var buffer = section.Data;

			var length = buffer [position]
				| (buffer [position + 1] << 8)
				| (buffer [position + 2] << 16)
				| (buffer [position + 3] << 24);

			return new MemoryStream (buffer, (int) position + 4, length);
		}

		void PopulateVersionAndFlags (AssemblyNameReference name)
		{
			name.Version = new Version (
				ReadUInt16 (),
				ReadUInt16 (),
				ReadUInt16 (),
				ReadUInt16 ());

			name.Attributes = (AssemblyAttributes) ReadUInt32 ();
		}

		void PopulateNameAndCulture (AssemblyNameReference name)
		{
			name.Name = ReadString ();
			name.Culture = ReadString ();
		}

		public TypeDefinitionCollection ReadTypes ()
		{
			InitializeTypeDefinitions ();
			var mtypes = metadata.Types;
			var type_count = mtypes.Length - metadata.NestedTypes.Count;
			var types = new TypeDefinitionCollection (module, type_count);

			for (int i = 0; i < mtypes.Length; i++) {
				var type = mtypes [i];
				if (IsNested (type.Attributes))
					continue;

				types.Add (type);
			}

			return types;
		}

		void InitializeTypeDefinitions ()
		{
			if (metadata.Types != null)
				return;

			InitializeNestedTypes ();
			InitializeFields ();
			InitializeMethods ();

			int length = MoveTo (Table.TypeDef);
			var types = metadata.Types = new TypeDefinition [length];

			for (uint i = 0; i < length; i++) {
				if (types [i] != null)
					continue;

				types [i] = ReadType (i + 1);
			}
		}

		static bool IsNested (TypeAttributes attributes)
		{
			switch (attributes & TypeAttributes.VisibilityMask) {
			case TypeAttributes.NestedAssembly:
			case TypeAttributes.NestedFamANDAssem:
			case TypeAttributes.NestedFamily:
			case TypeAttributes.NestedFamORAssem:
			case TypeAttributes.NestedPrivate:
			case TypeAttributes.NestedPublic:
				return true;
			default:
				return false;
			}
		}

		public bool HasNestedTypes (TypeDefinition type)
		{
			uint [] mapping;
			InitializeNestedTypes ();

			if (!metadata.TryGetNestedTypeMapping (type, out mapping))
				return false;

			return mapping.Length > 0;
		}

		public Collection<TypeDefinition> ReadNestedTypes (TypeDefinition type)
		{
			InitializeNestedTypes ();
			uint [] mapping;
			if (!metadata.TryGetNestedTypeMapping (type, out mapping))
				return new MemberDefinitionCollection<TypeDefinition> (type);

			var nested_types = new MemberDefinitionCollection<TypeDefinition> (type, mapping.Length);

			for (int i = 0; i < mapping.Length; i++)
				nested_types.Add (GetTypeDefinition (mapping [i]));

			metadata.RemoveNestedTypeMapping (type);

			return nested_types;
		}

		void InitializeNestedTypes ()
		{
			if (metadata.NestedTypes != null)
				return;

			var length = MoveTo (Table.NestedClass);

			metadata.NestedTypes = new Dictionary<uint, uint []> (length);
			metadata.ReverseNestedTypes = new Dictionary<uint, uint> (length);

			if (length == 0)
				return;

			for (int i = 1; i <= length; i++) {
				var nested = ReadTableIndex (Table.TypeDef);
				var declaring = ReadTableIndex (Table.TypeDef);

				AddNestedMapping (declaring, nested);
			}
		}

		void AddNestedMapping (uint declaring, uint nested)
		{
			metadata.SetNestedTypeMapping (declaring, AddMapping (metadata.NestedTypes, declaring, nested));
			metadata.SetReverseNestedTypeMapping (nested, declaring);
		}

		static TValue [] AddMapping<TKey, TValue> (Dictionary<TKey, TValue []> cache, TKey key, TValue value)
		{
			TValue [] mapped;
			if (!cache.TryGetValue (key, out mapped)) {
				mapped = new [] { value };
				return mapped;
			}

			var new_mapped = new TValue [mapped.Length + 1];
			Array.Copy (mapped, new_mapped, mapped.Length);
			new_mapped [mapped.Length] = value;
			return new_mapped;
		}

		TypeDefinition ReadType (uint rid)
		{
			if (!MoveTo (Table.TypeDef, rid))
				return null;

			var attributes = (TypeAttributes) ReadUInt32 ();
			var name = ReadString ();
			var @namespace = ReadString ();
			var type = new TypeDefinition (@namespace, name, attributes);
			type.token = new MetadataToken (TokenType.TypeDef, rid);
			type.scope = module;
			type.module = module;

			metadata.AddTypeDefinition (type);

			this.context = type;

			type.BaseType = GetTypeDefOrRef (ReadMetadataToken (CodedIndex.TypeDefOrRef));

			type.fields_range = ReadFieldsRange (rid);
			type.methods_range = ReadMethodsRange (rid);

			if (IsNested (attributes))
				type.DeclaringType = GetNestedTypeDeclaringType (type);

			return type;
		}

		TypeDefinition GetNestedTypeDeclaringType (TypeDefinition type)
		{
			uint declaring_rid;
			if (!metadata.TryGetReverseNestedTypeMapping (type, out declaring_rid))
				return null;

			metadata.RemoveReverseNestedTypeMapping (type);
			return GetTypeDefinition (declaring_rid);
		}

		Range ReadFieldsRange (uint type_index)
		{
			return ReadListRange (type_index, Table.TypeDef, Table.Field);
		}

		Range ReadMethodsRange (uint type_index)
		{
			return ReadListRange (type_index, Table.TypeDef, Table.Method);
		}

		Range ReadListRange (uint current_index, Table current, Table target)
		{
			var list = new Range ();

			list.Start = ReadTableIndex (target);

			uint next_index;
			var current_table = image.TableHeap [current];

			if (current_index == current_table.Length)
				next_index = image.TableHeap [target].Length + 1;
			else {
				var position = Position;
				Position += (uint) (current_table.RowSize - image.GetTableIndexSize (target));
				next_index = ReadTableIndex (target);
				Position = position;
			}

			list.Length = next_index - list.Start;

			return list;
		}

		public Row<short, int> ReadTypeLayout (TypeDefinition type)
		{
			InitializeTypeLayouts ();
			Row<ushort, uint> class_layout;
			var rid = type.token.RID;
			if (!metadata.ClassLayouts.TryGetValue (rid, out class_layout))
				return new Row<short, int> (Mixin.NoDataMarker, Mixin.NoDataMarker);

			type.PackingSize = (short) class_layout.Col1;
			type.ClassSize = (int) class_layout.Col2;

			metadata.ClassLayouts.Remove (rid);

			return new Row<short, int> ((short) class_layout.Col1, (int) class_layout.Col2);
		}

		void InitializeTypeLayouts ()
		{
			if (metadata.ClassLayouts != null)
				return;

			int length = MoveTo (Table.ClassLayout);

			var class_layouts = metadata.ClassLayouts = new Dictionary<uint, Row<ushort, uint>> (length);

			for (uint i = 0; i < length; i++) {
				var packing_size = ReadUInt16 ();
				var class_size = ReadUInt32 ();

				var parent = ReadTableIndex (Table.TypeDef);

				class_layouts.Add (parent, new Row<ushort, uint> (packing_size, class_size));
			}
		}

		public TypeReference GetTypeDefOrRef (MetadataToken token)
		{
			return (TypeReference) LookupToken (token);
		}

		public TypeDefinition GetTypeDefinition (uint rid)
		{
			InitializeTypeDefinitions ();

			var type = metadata.GetTypeDefinition (rid);
			if (type != null)
				return type;

			return ReadTypeDefinition (rid);
		}

		TypeDefinition ReadTypeDefinition (uint rid)
		{
			if (!MoveTo (Table.TypeDef, rid))
				return null;

			return ReadType (rid);
		}

		void InitializeTypeReferences ()
		{
			if (metadata.TypeReferences != null)
				return;

			metadata.TypeReferences = new TypeReference [image.GetTableLength (Table.TypeRef)];
		}

		public TypeReference GetTypeReference (string scope, string full_name)
		{
			InitializeTypeReferences ();

			var length = metadata.TypeReferences.Length;

			for (uint i = 1; i <= length; i++) {
				var type = GetTypeReference (i);

				if (type.FullName != full_name)
					continue;

				if (string.IsNullOrEmpty (scope))
					return type;

				if (type.Scope.Name == scope)
					return type;
			}

			return null;
		}

		TypeReference GetTypeReference (uint rid)
		{
			InitializeTypeReferences ();

			var type = metadata.GetTypeReference (rid);
			if (type != null)
				return type;

			return ReadTypeReference (rid);
		}

		TypeReference ReadTypeReference (uint rid)
		{
			if (!MoveTo (Table.TypeRef, rid))
				return null;

			TypeReference declaring_type = null;
			IMetadataScope scope;

			var scope_token = ReadMetadataToken (CodedIndex.ResolutionScope);

			var name = ReadString ();
			var @namespace = ReadString ();

			var type = new TypeReference (
				@namespace,
				name,
				module,
				null);

			type.token = new MetadataToken (TokenType.TypeRef, rid);

			metadata.AddTypeReference (type);

			if (scope_token.TokenType == TokenType.TypeRef) {
				declaring_type = GetTypeDefOrRef (scope_token);

				scope = declaring_type != null
					? declaring_type.Scope
					: module;
			} else
				scope = GetTypeReferenceScope (scope_token);

			type.scope = scope;
			type.DeclaringType = declaring_type;

			MetadataSystem.TryProcessPrimitiveType (type);

			return type;
		}

		IMetadataScope GetTypeReferenceScope (MetadataToken scope)
		{
			switch (scope.TokenType) {
			case TokenType.AssemblyRef:
				InitializeAssemblyReferences ();
				return metadata.AssemblyReferences [(int) scope.RID - 1];
			case TokenType.ModuleRef:
				InitializeModuleReferences ();
				return metadata.ModuleReferences [(int) scope.RID - 1];
			case TokenType.Module:
				return module;
			default:
				throw new NotSupportedException ();
			}
		}

		public IEnumerable<TypeReference> GetTypeReferences ()
		{
			InitializeTypeReferences ();

			var length = image.GetTableLength (Table.TypeRef);

			var type_references = new TypeReference [length];

			for (uint i = 1; i <= length; i++)
				type_references [i - 1] = GetTypeReference (i);

			return type_references;
		}

		TypeReference GetTypeSpecification (uint rid)
		{
			if (!MoveTo (Table.TypeSpec, rid))
				return null;

			var reader = ReadSignature (ReadBlobIndex ());
			var type = reader.ReadTypeSignature ();
			if (type.token.RID == 0)
				type.token = new MetadataToken (TokenType.TypeSpec, rid);

			return type;
		}

		SignatureReader ReadSignature (uint signature)
		{
			return new SignatureReader (signature, this);
		}

		public bool HasInterfaces (TypeDefinition type)
		{
			InitializeInterfaces ();
			MetadataToken [] mapping;

			return metadata.TryGetInterfaceMapping (type, out mapping);
		}

		public Collection<TypeReference> ReadInterfaces (TypeDefinition type)
		{
			InitializeInterfaces ();
			MetadataToken [] mapping;

			if (!metadata.TryGetInterfaceMapping (type, out mapping))
				return new Collection<TypeReference> ();

			var interfaces = new Collection<TypeReference> (mapping.Length);

			this.context = type;

			for (int i = 0; i < mapping.Length; i++)
				interfaces.Add (GetTypeDefOrRef (mapping [i]));

			metadata.RemoveInterfaceMapping (type);

			return interfaces;
		}

		void InitializeInterfaces ()
		{
			if (metadata.Interfaces != null)
				return;

			int length = MoveTo (Table.InterfaceImpl);

			metadata.Interfaces = new Dictionary<uint, MetadataToken []> (length);

			for (int i = 0; i < length; i++) {
				var type = ReadTableIndex (Table.TypeDef);
				var @interface = ReadMetadataToken (CodedIndex.TypeDefOrRef);

				AddInterfaceMapping (type, @interface);
			}
		}

		void AddInterfaceMapping (uint type, MetadataToken @interface)
		{
			metadata.SetInterfaceMapping (type, AddMapping (metadata.Interfaces, type, @interface));
		}

		public Collection<FieldDefinition> ReadFields (TypeDefinition type)
		{
			var fields_range = type.fields_range;
			if (fields_range.Length == 0)
				return new MemberDefinitionCollection<FieldDefinition> (type);

			var fields = new MemberDefinitionCollection<FieldDefinition> (type, (int) fields_range.Length);
			this.context = type;

			if (!MoveTo (Table.FieldPtr, fields_range.Start)) {
				if (!MoveTo (Table.Field, fields_range.Start))
					return fields;

				for (uint i = 0; i < fields_range.Length; i++)
					ReadField (fields_range.Start + i, fields);
			} else
				ReadPointers (Table.FieldPtr, Table.Field, fields_range, fields, ReadField);

			return fields;
		}

		void ReadField (uint field_rid, Collection<FieldDefinition> fields)
		{
			var attributes = (FieldAttributes) ReadUInt16 ();
			var name = ReadString ();
			var signature = ReadBlobIndex ();

			var field = new FieldDefinition (name, attributes, ReadFieldType (signature));
			field.token = new MetadataToken (TokenType.Field, field_rid);
			metadata.AddFieldDefinition (field);

			if (IsDeleted (field))
				return;

			fields.Add (field);
		}

		void InitializeFields ()
		{
			if (metadata.Fields != null)
				return;

			metadata.Fields = new FieldDefinition [image.GetTableLength (Table.Field)];
		}

		TypeReference ReadFieldType (uint signature)
		{
			var reader = ReadSignature (signature);

			const byte field_sig = 0x6;

			if (reader.ReadByte () != field_sig)
				throw new NotSupportedException ();

			return reader.ReadTypeSignature ();
		}

		public int ReadFieldRVA (FieldDefinition field)
		{
			InitializeFieldRVAs ();
			var rid = field.token.RID;

			RVA rva;
			if (!metadata.FieldRVAs.TryGetValue (rid, out rva))
				return 0;

			var size = GetFieldTypeSize (field.FieldType);

			if (size == 0 || rva == 0)
				return 0;

			metadata.FieldRVAs.Remove (rid);

			field.InitialValue = GetFieldInitializeValue (size, rva);

			return (int) rva;
		}

		byte [] GetFieldInitializeValue (int size, RVA rva)
		{
			var section = image.GetSectionAtVirtualAddress (rva);
			if (section == null)
				return Empty<byte>.Array;

			var value = new byte [size];
			Buffer.BlockCopy (section.Data, (int) (rva - section.VirtualAddress), value, 0, size);
			return value;
		}

		static int GetFieldTypeSize (TypeReference type)
		{
			int size = 0;

			switch (type.etype) {
			case ElementType.Boolean:
			case ElementType.U1:
			case ElementType.I1:
				size = 1;
				break;
			case ElementType.U2:
			case ElementType.I2:
			case ElementType.Char:
				size = 2;
				break;
			case ElementType.U4:
			case ElementType.I4:
			case ElementType.R4:
				size = 4;
				break;
			case ElementType.U8:
			case ElementType.I8:
			case ElementType.R8:
				size = 8;
				break;
			case ElementType.Ptr:
			case ElementType.FnPtr:
				size = IntPtr.Size;
				break;
			case ElementType.CModOpt:
			case ElementType.CModReqD:
				return GetFieldTypeSize (((IModifierType) type).ElementType);
			default:
				var field_type = type.CheckedResolve ();
				if (field_type.HasLayoutInfo)
					size = field_type.ClassSize;

				break;
			}

			return size;
		}

		void InitializeFieldRVAs ()
		{
			if (metadata.FieldRVAs != null)
				return;

			int length = MoveTo (Table.FieldRVA);

			var field_rvas = metadata.FieldRVAs = new Dictionary<uint, uint> (length);

			for (int i = 0; i < length; i++) {
				var rva = ReadUInt32 ();
				var field = ReadTableIndex (Table.Field);

				field_rvas.Add (field, rva);
			}
		}

		public int ReadFieldLayout (FieldDefinition field)
		{
			InitializeFieldLayouts ();
			var rid = field.token.RID;
			uint offset;
			if (!metadata.FieldLayouts.TryGetValue (rid, out offset))
				return Mixin.NoDataMarker;

			metadata.FieldLayouts.Remove (rid);

			return (int) offset;
		}

		void InitializeFieldLayouts ()
		{
			if (metadata.FieldLayouts != null)
				return;

			int length = MoveTo (Table.FieldLayout);

			var field_layouts = metadata.FieldLayouts = new Dictionary<uint, uint> (length);

			for (int i = 0; i < length; i++) {
				var offset = ReadUInt32 ();
				var field = ReadTableIndex (Table.Field);

				field_layouts.Add (field, offset);
			}
		}

		public bool HasEvents (TypeDefinition type)
		{
			InitializeEvents ();

			Range range;
			if (!metadata.TryGetEventsRange (type, out range))
				return false;

			return range.Length > 0;
		}

		public Collection<EventDefinition> ReadEvents (TypeDefinition type)
		{
			InitializeEvents ();
			Range range;

			if (!metadata.TryGetEventsRange (type, out range))
				return new MemberDefinitionCollection<EventDefinition> (type);

			var events = new MemberDefinitionCollection<EventDefinition> (type, (int) range.Length);

			metadata.RemoveEventsRange (type);

			if (range.Length == 0)
				return events;

			this.context = type;

			if (!MoveTo (Table.EventPtr, range.Start)) {
				if (!MoveTo (Table.Event, range.Start))
					return events;

				for (uint i = 0; i < range.Length; i++)
					ReadEvent (range.Start + i, events);
			} else
				ReadPointers (Table.EventPtr, Table.Event, range, events, ReadEvent);

			return events;
		}

		void ReadEvent (uint event_rid, Collection<EventDefinition> events)
		{
			var attributes = (EventAttributes) ReadUInt16 ();
			var name = ReadString ();
			var event_type = GetTypeDefOrRef (ReadMetadataToken (CodedIndex.TypeDefOrRef));

			var @event = new EventDefinition (name, attributes, event_type);
			@event.token = new MetadataToken (TokenType.Event, event_rid);

			if (IsDeleted (@event))
				return;

			events.Add (@event);
		}

		void InitializeEvents ()
		{
			if (metadata.Events != null)
				return;

			int length = MoveTo (Table.EventMap);

			metadata.Events = new Dictionary<uint, Range> (length);

			for (uint i = 1; i <= length; i++) {
				var type_rid = ReadTableIndex (Table.TypeDef);
				Range events_range = ReadEventsRange (i);
				metadata.AddEventsRange (type_rid, events_range);
			}
		}

		Range ReadEventsRange (uint rid)
		{
			return ReadListRange (rid, Table.EventMap, Table.Event);
		}

		public bool HasProperties (TypeDefinition type)
		{
			InitializeProperties ();

			Range range;
			if (!metadata.TryGetPropertiesRange (type, out range))
				return false;

			return range.Length > 0;
		}

		public Collection<PropertyDefinition> ReadProperties (TypeDefinition type)
		{
			InitializeProperties ();

			Range range;

			if (!metadata.TryGetPropertiesRange (type, out range))
				return new MemberDefinitionCollection<PropertyDefinition> (type);

			metadata.RemovePropertiesRange (type);

			var properties = new MemberDefinitionCollection<PropertyDefinition> (type, (int) range.Length);

			if (range.Length == 0)
				return properties;

			this.context = type;

			if (!MoveTo (Table.PropertyPtr, range.Start)) {
				if (!MoveTo (Table.Property, range.Start))
					return properties;
				for (uint i = 0; i < range.Length; i++)
					ReadProperty (range.Start + i, properties);
			} else
				ReadPointers (Table.PropertyPtr, Table.Property, range, properties, ReadProperty);

			return properties;
		}

		void ReadProperty (uint property_rid, Collection<PropertyDefinition> properties)
		{
			var attributes = (PropertyAttributes) ReadUInt16 ();
			var name = ReadString ();
			var signature = ReadBlobIndex ();

			var reader = ReadSignature (signature);
			const byte property_signature = 0x8;

			var calling_convention = reader.ReadByte ();

			if ((calling_convention & property_signature) == 0)
				throw new NotSupportedException ();

			var has_this = (calling_convention & 0x20) != 0;

			reader.ReadCompressedUInt32 (); // count

			var property = new PropertyDefinition (name, attributes, reader.ReadTypeSignature ());
			property.HasThis = has_this;
			property.token = new MetadataToken (TokenType.Property, property_rid);

			if (IsDeleted (property))
				return;

			properties.Add (property);
		}

		void InitializeProperties ()
		{
			if (metadata.Properties != null)
				return;

			int length = MoveTo (Table.PropertyMap);

			metadata.Properties = new Dictionary<uint, Range> (length);

			for (uint i = 1; i <= length; i++) {
				var type_rid = ReadTableIndex (Table.TypeDef);
				var properties_range = ReadPropertiesRange (i);
				metadata.AddPropertiesRange (type_rid, properties_range);
			}
		}

		Range ReadPropertiesRange (uint rid)
		{
			return ReadListRange (rid, Table.PropertyMap, Table.Property);
		}

		MethodSemanticsAttributes ReadMethodSemantics (MethodDefinition method)
		{
			InitializeMethodSemantics ();
			Row<MethodSemanticsAttributes, MetadataToken> row;
			if (!metadata.Semantics.TryGetValue (method.token.RID, out row))
				return MethodSemanticsAttributes.None;

			var type = method.DeclaringType;

			switch (row.Col1) {
			case MethodSemanticsAttributes.AddOn:
				GetEvent (type, row.Col2).add_method = method;
				break;
			case MethodSemanticsAttributes.Fire:
				GetEvent (type, row.Col2).invoke_method = method;
				break;
			case MethodSemanticsAttributes.RemoveOn:
				GetEvent (type, row.Col2).remove_method = method;
				break;
			case MethodSemanticsAttributes.Getter:
				GetProperty (type, row.Col2).get_method = method;
				break;
			case MethodSemanticsAttributes.Setter:
				GetProperty (type, row.Col2).set_method = method;
				break;
			case MethodSemanticsAttributes.Other:
				switch (row.Col2.TokenType) {
				case TokenType.Event: {
					var @event = GetEvent (type, row.Col2);
					if (@event.other_methods == null)
						@event.other_methods = new Collection<MethodDefinition> ();

					@event.other_methods.Add (method);
					break;
				}
				case TokenType.Property: {
					var property = GetProperty (type, row.Col2);
					if (property.other_methods == null)
						property.other_methods = new Collection<MethodDefinition> ();

					property.other_methods.Add (method);

					break;
				}
				default:
					throw new NotSupportedException ();
				}
				break;
			default:
				throw new NotSupportedException ();
			}

			metadata.Semantics.Remove (method.token.RID);

			return row.Col1;
		}

		static EventDefinition GetEvent (TypeDefinition type, MetadataToken token)
		{
			if (token.TokenType != TokenType.Event)
				throw new ArgumentException ();

			return GetMember (type.Events, token);
		}

		static PropertyDefinition GetProperty (TypeDefinition type, MetadataToken token)
		{
			if (token.TokenType != TokenType.Property)
				throw new ArgumentException ();

			return GetMember (type.Properties, token);
		}

		static TMember GetMember<TMember> (Collection<TMember> members, MetadataToken token) where TMember : IMemberDefinition
		{
			for (int i = 0; i < members.Count; i++) {
				var member = members [i];
				if (member.MetadataToken == token)
					return member;
			}

			throw new ArgumentException ();
		}

		void InitializeMethodSemantics ()
		{
			if (metadata.Semantics != null)
				return;

			int length = MoveTo (Table.MethodSemantics);

			var semantics = metadata.Semantics = new Dictionary<uint, Row<MethodSemanticsAttributes, MetadataToken>> (0);

			for (uint i = 0; i < length; i++) {
				var attributes = (MethodSemanticsAttributes) ReadUInt16 ();
				var method_rid = ReadTableIndex (Table.Method);
				var association = ReadMetadataToken (CodedIndex.HasSemantics);

				semantics [method_rid] = new Row<MethodSemanticsAttributes, MetadataToken> (attributes, association);
			}
		}

		public PropertyDefinition ReadMethods (PropertyDefinition property)
		{
			ReadAllSemantics (property.DeclaringType);
			return property;
		}

		public EventDefinition ReadMethods (EventDefinition @event)
		{
			ReadAllSemantics (@event.DeclaringType);
			return @event;
		}

		public MethodSemanticsAttributes ReadAllSemantics (MethodDefinition method)
		{
			ReadAllSemantics (method.DeclaringType);

			return method.SemanticsAttributes;
		}

		void ReadAllSemantics (TypeDefinition type)
		{
			var methods = type.Methods;
			for (int i = 0; i < methods.Count; i++) {
				var method = methods [i];
				if (method.sem_attrs_ready)
					continue;

				method.sem_attrs = ReadMethodSemantics (method);
				method.sem_attrs_ready = true;
			}
		}

		Range ReadParametersRange (uint method_rid)
		{
			return ReadListRange (method_rid, Table.Method, Table.Param);
		}

		public Collection<MethodDefinition> ReadMethods (TypeDefinition type)
		{
			var methods_range = type.methods_range;
			if (methods_range.Length == 0)
				return new MemberDefinitionCollection<MethodDefinition> (type);

			var methods = new MemberDefinitionCollection<MethodDefinition> (type, (int) methods_range.Length);
			if (!MoveTo (Table.MethodPtr, methods_range.Start)) {
				if (!MoveTo (Table.Method, methods_range.Start))
					return methods;

				for (uint i = 0; i < methods_range.Length; i++)
					ReadMethod (methods_range.Start + i, methods);
			} else
				ReadPointers (Table.MethodPtr, Table.Method, methods_range, methods, ReadMethod);

			return methods;
		}

		void ReadPointers<TMember> (Table ptr, Table table, Range range, Collection<TMember> members, Action<uint, Collection<TMember>> reader)
			where TMember : IMemberDefinition
		{
			for (uint i = 0; i < range.Length; i++) {
				MoveTo (ptr, range.Start + i);

				var rid = ReadTableIndex (table);
				MoveTo (table, rid);

				reader (rid, members);
			}
		}

		static bool IsDeleted (IMemberDefinition member)
		{
			return member.IsSpecialName && member.Name == "_Deleted";
		}

		void InitializeMethods ()
		{
			if (metadata.Methods != null)
				return;

			metadata.Methods = new MethodDefinition [image.GetTableLength (Table.Method)];
		}

		void ReadMethod (uint method_rid, Collection<MethodDefinition> methods)
		{
			var method = new MethodDefinition ();
			method.rva = ReadUInt32 ();
			method.ImplAttributes = (MethodImplAttributes) ReadUInt16 ();
			method.Attributes = (MethodAttributes) ReadUInt16 ();
			method.Name = ReadString ();
			method.token = new MetadataToken (TokenType.Method, method_rid);

			if (IsDeleted (method))
				return;

			methods.Add (method); // attach method

			var signature = ReadBlobIndex ();
			var param_range = ReadParametersRange (method_rid);

			this.context = method;

			ReadMethodSignature (signature, method);
			metadata.AddMethodDefinition (method);

			if (param_range.Length == 0)
				return;

			var position = base.position;
			ReadParameters (method, param_range);
			base.position = position;
		}

		void ReadParameters (MethodDefinition method, Range param_range)
		{
			if (!MoveTo (Table.ParamPtr, param_range.Start)) {
				if (!MoveTo (Table.Param, param_range.Start))
					return;

				for (uint i = 0; i < param_range.Length; i++)
					ReadParameter (param_range.Start + i, method);
			} else
				ReadParameterPointers (method, param_range);
		}

		void ReadParameterPointers (MethodDefinition method, Range range)
		{
			for (uint i = 0; i < range.Length; i++) {
				MoveTo (Table.ParamPtr, range.Start + i);

				var rid = ReadTableIndex (Table.Param);

				MoveTo (Table.Param, rid);

				ReadParameter (rid, method);
			}
		}

		void ReadParameter (uint param_rid, MethodDefinition method)
		{
			var attributes = (ParameterAttributes) ReadUInt16 ();
			var sequence = ReadUInt16 ();
			var name = ReadString ();

			var parameter = sequence == 0
				? method.MethodReturnType.Parameter
				: method.Parameters [sequence - 1];

			parameter.token = new MetadataToken (TokenType.Param, param_rid);
			parameter.Name = name;
			parameter.Attributes = attributes;
		}

		void ReadMethodSignature (uint signature, IMethodSignature method)
		{
			var reader = ReadSignature (signature);
			reader.ReadMethodSignature (method);
		}

		public PInvokeInfo ReadPInvokeInfo (MethodDefinition method)
		{
			InitializePInvokes ();
			Row<PInvokeAttributes, uint, uint> row;

			var rid = method.token.RID;

			if (!metadata.PInvokes.TryGetValue (rid, out row))
				return null;

			metadata.PInvokes.Remove (rid);

			return new PInvokeInfo (
				row.Col1,
				image.StringHeap.Read (row.Col2),
				module.ModuleReferences [(int) row.Col3 - 1]);
		}

		void InitializePInvokes ()
		{
			if (metadata.PInvokes != null)
				return;

			int length = MoveTo (Table.ImplMap);

			var pinvokes = metadata.PInvokes = new Dictionary<uint, Row<PInvokeAttributes, uint, uint>> (length);

			for (int i = 1; i <= length; i++) {
				var attributes = (PInvokeAttributes) ReadUInt16 ();
				var method = ReadMetadataToken (CodedIndex.MemberForwarded);
				var name = ReadStringIndex ();
				var scope = ReadTableIndex (Table.File);

				if (method.TokenType != TokenType.Method)
					continue;

				pinvokes.Add (method.RID, new Row<PInvokeAttributes, uint, uint> (attributes, name, scope));
			}
		}

		public bool HasGenericParameters (IGenericParameterProvider provider)
		{
			InitializeGenericParameters ();

			Range range;
			if (!metadata.TryGetGenericParameterRange (provider, out range))
				return false;

			return range.Length > 0;
		}

		public Collection<GenericParameter> ReadGenericParameters (IGenericParameterProvider provider)
		{
			InitializeGenericParameters ();

			Range range;
			if (!metadata.TryGetGenericParameterRange (provider, out range)
				|| !MoveTo (Table.GenericParam, range.Start))
				return new Collection<GenericParameter> ();

			metadata.RemoveGenericParameterRange (provider);

			var generic_parameters = new Collection<GenericParameter> ((int) range.Length);

			for (uint i = 0; i < range.Length; i++) {
				ReadUInt16 (); // index
				var flags = (GenericParameterAttributes) ReadUInt16 ();
				ReadMetadataToken (CodedIndex.TypeOrMethodDef);
				var name = ReadString ();

				var parameter = new GenericParameter (name, provider);
				parameter.token = new MetadataToken (TokenType.GenericParam, range.Start + i);
				parameter.Attributes = flags;

				generic_parameters.Add (parameter);
			}

			return generic_parameters;
		}

		void InitializeGenericParameters ()
		{
			if (metadata.GenericParameters != null)
				return;

			metadata.GenericParameters = InitializeRanges (
				Table.GenericParam, () => {
					Advance (4);
					var next = ReadMetadataToken (CodedIndex.TypeOrMethodDef);
					ReadStringIndex ();
					return next;
			});
		}

		Dictionary<MetadataToken, Range> InitializeRanges (Table table, Func<MetadataToken> get_next)
		{
			int length = MoveTo (table);
			var ranges = new Dictionary<MetadataToken, Range> (length);

			if (length == 0)
				return ranges;

			MetadataToken owner = MetadataToken.Zero;
			Range range = new Range (1, 0);

			for (uint i = 1; i <= length; i++) {
				var next = get_next ();

				if (i == 1) {
					owner = next;
					range.Length++;
				} else if (next != owner) {
					if (owner.RID != 0)
						ranges.Add (owner, range);
					range = new Range (i, 1);
					owner = next;
				} else
					range.Length++;
			}

			if (owner != MetadataToken.Zero && !ranges.ContainsKey (owner))
				ranges.Add (owner, range);

			return ranges;
		}

		public bool HasGenericConstraints (GenericParameter generic_parameter)
		{
			InitializeGenericConstraints ();

			MetadataToken [] mapping;
			if (!metadata.TryGetGenericConstraintMapping (generic_parameter, out mapping))
				return false;

			return mapping.Length > 0;
		}

		public Collection<TypeReference> ReadGenericConstraints (GenericParameter generic_parameter)
		{
			InitializeGenericConstraints ();

			MetadataToken [] mapping;
			if (!metadata.TryGetGenericConstraintMapping (generic_parameter, out mapping))
				return new Collection<TypeReference> ();

			var constraints = new Collection<TypeReference> (mapping.Length);

			this.context = (IGenericContext) generic_parameter.Owner;

			for (int i = 0; i < mapping.Length; i++)
				constraints.Add (GetTypeDefOrRef (mapping [i]));

			metadata.RemoveGenericConstraintMapping (generic_parameter);

			return constraints;
		}

		void InitializeGenericConstraints ()
		{
			if (metadata.GenericConstraints != null)
				return;

			var length = MoveTo (Table.GenericParamConstraint);

			metadata.GenericConstraints = new Dictionary<uint, MetadataToken []> (length);

			for (int i = 1; i <= length; i++)
				AddGenericConstraintMapping (
					ReadTableIndex (Table.GenericParam),
					ReadMetadataToken (CodedIndex.TypeDefOrRef));
		}

		void AddGenericConstraintMapping (uint generic_parameter, MetadataToken constraint)
		{
			metadata.SetGenericConstraintMapping (
				generic_parameter,
				AddMapping (metadata.GenericConstraints, generic_parameter, constraint));
		}

		public bool HasOverrides (MethodDefinition method)
		{
			InitializeOverrides ();
			MetadataToken [] mapping;

			if (!metadata.TryGetOverrideMapping (method, out mapping))
				return false;

			return mapping.Length > 0;
		}

		public Collection<MethodReference> ReadOverrides (MethodDefinition method)
		{
			InitializeOverrides ();

			MetadataToken [] mapping;
			if (!metadata.TryGetOverrideMapping (method, out mapping))
				return new Collection<MethodReference> ();

			var overrides = new Collection<MethodReference> (mapping.Length);

			this.context = method;

			for (int i = 0; i < mapping.Length; i++)
				overrides.Add ((MethodReference) LookupToken (mapping [i]));

			metadata.RemoveOverrideMapping (method);

			return overrides;
		}

		void InitializeOverrides ()
		{
			if (metadata.Overrides != null)
				return;

			var length = MoveTo (Table.MethodImpl);

			metadata.Overrides = new Dictionary<uint, MetadataToken []> (length);

			for (int i = 1; i <= length; i++) {
				ReadTableIndex (Table.TypeDef);

				var method = ReadMetadataToken (CodedIndex.MethodDefOrRef);
				if (method.TokenType != TokenType.Method)
					throw new NotSupportedException ();

				var @override = ReadMetadataToken (CodedIndex.MethodDefOrRef);

				AddOverrideMapping (method.RID, @override);
			}
		}

		void AddOverrideMapping (uint method_rid, MetadataToken @override)
		{
			metadata.SetOverrideMapping (
				method_rid,
				AddMapping (metadata.Overrides, method_rid, @override));
		}

		public MethodBody ReadMethodBody (MethodDefinition method)
		{
			return code.ReadMethodBody (method);
		}

		public CallSite ReadCallSite (MetadataToken token)
		{
			if (!MoveTo (Table.StandAloneSig, token.RID))
				return null;

			var signature = ReadBlobIndex ();

			var call_site = new CallSite ();

			ReadMethodSignature (signature, call_site);

			call_site.MetadataToken = token;

			return call_site;
		}

		public VariableDefinitionCollection ReadVariables (MetadataToken local_var_token)
		{
			if (!MoveTo (Table.StandAloneSig, local_var_token.RID))
				return null;

			var reader = ReadSignature (ReadBlobIndex ());
			const byte local_sig = 0x7;

			if (reader.ReadByte () != local_sig)
				throw new NotSupportedException ();

			var count = reader.ReadCompressedUInt32 ();
			if (count == 0)
				return null;

			var variables = new VariableDefinitionCollection ((int) count);

			for (int i = 0; i < count; i++)
				variables.Add (new VariableDefinition (reader.ReadTypeSignature ()));

			return variables;
		}

		public IMetadataTokenProvider LookupToken (MetadataToken token)
		{
			var rid = token.RID;

			if (rid == 0)
				return null;

			IMetadataTokenProvider element;
			var position = this.position;
			var context = this.context;

			switch (token.TokenType) {
			case TokenType.TypeDef:
				element = GetTypeDefinition (rid);
				break;
			case TokenType.TypeRef:
				element = GetTypeReference (rid);
				break;
			case TokenType.TypeSpec:
				element = GetTypeSpecification (rid);
				break;
			case TokenType.Field:
				element = GetFieldDefinition (rid);
				break;
			case TokenType.Method:
				element = GetMethodDefinition (rid);
				break;
			case TokenType.MemberRef:
				element = GetMemberReference (rid);
				break;
			case TokenType.MethodSpec:
				element = GetMethodSpecification (rid);
				break;
			default:
				return null;
			}

			this.position = position;
			this.context = context;

			return element;
		}

		public FieldDefinition GetFieldDefinition (uint rid)
		{
			InitializeTypeDefinitions ();

			var field = metadata.GetFieldDefinition (rid);
			if (field != null)
				return field;

			return LookupField (rid);
		}

		FieldDefinition LookupField (uint rid)
		{
			var type = metadata.GetFieldDeclaringType (rid);
			if (type == null)
				return null;

			InitializeCollection (type.Fields);

			return metadata.GetFieldDefinition (rid);
		}

		public MethodDefinition GetMethodDefinition (uint rid)
		{
			InitializeTypeDefinitions ();

			var method = metadata.GetMethodDefinition (rid);
			if (method != null)
				return method;

			return LookupMethod (rid);
		}

		MethodDefinition LookupMethod (uint rid)
		{
			var type = metadata.GetMethodDeclaringType (rid);
			if (type == null)
				return null;

			InitializeCollection (type.Methods);

			return metadata.GetMethodDefinition (rid);
		}

		MethodSpecification GetMethodSpecification (uint rid)
		{
			if (!MoveTo (Table.MethodSpec, rid))
				return null;

			var element_method = (MethodReference) LookupToken (
				ReadMetadataToken (CodedIndex.MethodDefOrRef));
			var signature = ReadBlobIndex ();

			var method_spec = ReadMethodSpecSignature (signature, element_method);
			method_spec.token = new MetadataToken (TokenType.MethodSpec, rid);
			return method_spec;
		}

		MethodSpecification ReadMethodSpecSignature (uint signature, MethodReference method)
		{
			var reader = ReadSignature (signature);
			const byte methodspec_sig = 0x0a;

			var call_conv = reader.ReadByte ();

			if (call_conv != methodspec_sig)
				throw new NotSupportedException ();

			var instance = new GenericInstanceMethod (method);

			reader.ReadGenericInstanceSignature (method, instance);

			return instance;
		}

		MemberReference GetMemberReference (uint rid)
		{
			InitializeMemberReferences ();

			var member = metadata.GetMemberReference (rid);
			if (member != null)
				return member;

			member = ReadMemberReference (rid);
			if (member != null && !member.ContainsGenericParameter)
				metadata.AddMemberReference (member);
			return member;
		}

		MemberReference ReadMemberReference (uint rid)
		{
			if (!MoveTo (Table.MemberRef, rid))
				return null;

			var token = ReadMetadataToken (CodedIndex.MemberRefParent);
			var name = ReadString ();
			var signature = ReadBlobIndex ();

			MemberReference member;

			switch (token.TokenType) {
			case TokenType.TypeDef:
			case TokenType.TypeRef:
			case TokenType.TypeSpec:
				member = ReadTypeMemberReference (token, name, signature);
				break;
			case TokenType.Method:
				member = ReadMethodMemberReference (token, name, signature);
				break;
			default:
				throw new NotSupportedException ();
			}

			member.token = new MetadataToken (TokenType.MemberRef, rid);

			return member;
		}

		MemberReference ReadTypeMemberReference (MetadataToken type, string name, uint signature)
		{
			var declaring_type = GetTypeDefOrRef (type);

			this.context = declaring_type;

			var member = ReadMemberReferenceSignature (signature, declaring_type);
			member.Name = name;

			return member;
		}

		MemberReference ReadMemberReferenceSignature (uint signature, TypeReference declaring_type)
		{
			var reader = ReadSignature (signature);
			const byte field_sig = 0x6;

			if (reader.buffer [reader.position] == field_sig) {
				reader.position++;
				var field = new FieldReference ();
				field.DeclaringType = declaring_type;
				field.FieldType = reader.ReadTypeSignature ();
				return field;
			} else {
				var method = new MethodReference ();
				method.DeclaringType = declaring_type;
				reader.ReadMethodSignature (method);
				return method;
			}
		}

		MemberReference ReadMethodMemberReference (MetadataToken token, string name, uint signature)
		{
			var method = GetMethodDefinition (token.RID);

			this.context = method;

			var member = ReadMemberReferenceSignature (signature, method.DeclaringType);
			member.Name = name;

			return member;
		}

		void InitializeMemberReferences ()
		{
			if (metadata.MemberReferences != null)
				return;

			metadata.MemberReferences = new MemberReference [image.GetTableLength (Table.MemberRef)];
		}

		public IEnumerable<MemberReference> GetMemberReferences ()
		{
			InitializeMemberReferences ();

			var length = image.GetTableLength (Table.MemberRef);

			var type_system = module.TypeSystem;

			var context = new MethodReference (string.Empty, type_system.Void);
			context.DeclaringType = new TypeReference (string.Empty, string.Empty, module, type_system.Corlib);

			var member_references = new MemberReference [length];

			for (uint i = 1; i <= length; i++) {
				this.context = context;
				member_references [i - 1] = GetMemberReference (i);
			}

			return member_references;
		}

		void InitializeConstants ()
		{
			if (metadata.Constants != null)
				return;

			var length = MoveTo (Table.Constant);

			var constants = metadata.Constants = new Dictionary<MetadataToken, Row<ElementType, uint>> (length);

			for (uint i = 1; i <= length; i++) {
				var type = (ElementType) ReadUInt16 ();
				var owner = ReadMetadataToken (CodedIndex.HasConstant);
				var signature = ReadBlobIndex ();

				constants.Add (owner, new Row<ElementType, uint> (type, signature));
			}
		}

		public object ReadConstant (IConstantProvider owner)
		{
			InitializeConstants ();

			Row<ElementType, uint> row;
			if (!metadata.Constants.TryGetValue (owner.MetadataToken, out row))
				return Mixin.NoValue;

			metadata.Constants.Remove (owner.MetadataToken);

			switch (row.Col1) {
			case ElementType.Class:
			case ElementType.Object:
				return null;
			case ElementType.String:
				return ReadConstantString (ReadBlob (row.Col2));
			default:
				return ReadConstantPrimitive (row.Col1, row.Col2);
			}
		}

		static string ReadConstantString (byte [] blob)
		{
			var length = blob.Length;
			if ((length & 1) == 1)
				length--;

			return Encoding.Unicode.GetString (blob, 0, length);
		}

		object ReadConstantPrimitive (ElementType type, uint signature)
		{
			var reader = ReadSignature (signature);
			return reader.ReadConstantSignature (type);
		}

		void InitializeCustomAttributes ()
		{
			if (metadata.CustomAttributes != null)
				return;

			metadata.CustomAttributes = InitializeRanges (
				Table.CustomAttribute, () => {
					var next = ReadMetadataToken (CodedIndex.HasCustomAttribute);
					ReadMetadataToken (CodedIndex.CustomAttributeType);
					ReadBlobIndex ();
					return next;
			});
		}

		public bool HasCustomAttributes (ICustomAttributeProvider owner)
		{
			InitializeCustomAttributes ();

			Range range;
			if (!metadata.TryGetCustomAttributeRange (owner, out range))
				return false;

			return range.Length > 0;
		}

		public Collection<CustomAttribute> ReadCustomAttributes (ICustomAttributeProvider owner)
		{
			InitializeCustomAttributes ();

			Range range;
			if (!metadata.TryGetCustomAttributeRange (owner, out range)
				|| !MoveTo (Table.CustomAttribute, range.Start))
				return new Collection<CustomAttribute> ();

			var custom_attributes = new Collection<CustomAttribute> ((int) range.Length);

			for (int i = 0; i < range.Length; i++) {
				ReadMetadataToken (CodedIndex.HasCustomAttribute);

				var constructor = (MethodReference) LookupToken (
					ReadMetadataToken (CodedIndex.CustomAttributeType));

				var signature = ReadBlobIndex ();

				custom_attributes.Add (new CustomAttribute (signature, constructor));
			}

			metadata.RemoveCustomAttributeRange (owner);

			return custom_attributes;
		}

		public byte [] ReadCustomAttributeBlob (uint signature)
		{
			return ReadBlob (signature);
		}

		public void ReadCustomAttributeSignature (CustomAttribute attribute)
		{
			var reader = ReadSignature (attribute.signature);
			if (reader.ReadUInt16 () != 0x0001)
			    throw new InvalidOperationException ();

			var constructor = attribute.Constructor;
			if (constructor.HasParameters)
				reader.ReadCustomAttributeConstructorArguments (attribute, constructor.Parameters);

			if (!reader.CanReadMore ())
				return;

			var named = reader.ReadUInt16 ();

			if (named == 0)
				return;

			reader.ReadCustomAttributeNamedArguments (named, ref attribute.fields, ref attribute.properties);
		}

		void InitializeMarshalInfos ()
		{
			if (metadata.FieldMarshals != null)
				return;

			var length = MoveTo (Table.FieldMarshal);

			var marshals = metadata.FieldMarshals = new Dictionary<MetadataToken, uint> (length);

			for (int i = 0; i < length; i++) {
				var token = ReadMetadataToken (CodedIndex.HasFieldMarshal);
				var signature = ReadBlobIndex ();
				if (token.RID == 0)
					continue;

				marshals.Add (token, signature);
			}
		}

		public bool HasMarshalInfo (IMarshalInfoProvider owner)
		{
			InitializeMarshalInfos ();

			return metadata.FieldMarshals.ContainsKey (owner.MetadataToken);
		}

		public MarshalInfo ReadMarshalInfo (IMarshalInfoProvider owner)
		{
			InitializeMarshalInfos ();

			uint signature;
			if (!metadata.FieldMarshals.TryGetValue (owner.MetadataToken, out signature))
				return null;

			var reader = ReadSignature (signature);

			metadata.FieldMarshals.Remove (owner.MetadataToken);

			return reader.ReadMarshalInfo ();
		}

		void InitializeSecurityDeclarations ()
		{
			if (metadata.SecurityDeclarations != null)
				return;

			metadata.SecurityDeclarations = InitializeRanges (
				Table.DeclSecurity, () => {
					ReadUInt16 ();
					var next = ReadMetadataToken (CodedIndex.HasDeclSecurity);
					ReadBlobIndex ();
					return next;
			});
		}

		public bool HasSecurityDeclarations (ISecurityDeclarationProvider owner)
		{
			InitializeSecurityDeclarations ();

			Range range;
			if (!metadata.TryGetSecurityDeclarationRange (owner, out range))
				return false;

			return range.Length > 0;
		}

		public Collection<SecurityDeclaration> ReadSecurityDeclarations (ISecurityDeclarationProvider owner)
		{
			InitializeSecurityDeclarations ();

			Range range;
			if (!metadata.TryGetSecurityDeclarationRange (owner, out range)
				|| !MoveTo (Table.DeclSecurity, range.Start))
				return new Collection<SecurityDeclaration> ();

			var security_declarations = new Collection<SecurityDeclaration> ((int) range.Length);

			for (int i = 0; i < range.Length; i++) {
				var action = (SecurityAction) ReadUInt16 ();
				ReadMetadataToken (CodedIndex.HasDeclSecurity);
				var signature = ReadBlobIndex ();

				security_declarations.Add (new SecurityDeclaration (action, signature, module));
			}

			metadata.RemoveSecurityDeclarationRange (owner);

			return security_declarations;
		}

		public byte [] ReadSecurityDeclarationBlob (uint signature)
		{
			return ReadBlob (signature);
		}

		public void ReadSecurityDeclarationSignature (SecurityDeclaration declaration)
		{
			var signature = declaration.signature;
			var reader = ReadSignature (signature);

			if (reader.buffer [reader.position] != '.') {
				ReadXmlSecurityDeclaration (signature, declaration);
				return;
			}

			reader.position++;
			var count = reader.ReadCompressedUInt32 ();
			var attributes = new Collection<SecurityAttribute> ((int) count);

			for (int i = 0; i < count; i++)
				attributes.Add (reader.ReadSecurityAttribute ());

			declaration.security_attributes = attributes;
		}

		void ReadXmlSecurityDeclaration (uint signature, SecurityDeclaration declaration)
		{
			var blob = ReadBlob (signature);
			var attributes = new Collection<SecurityAttribute> (1);

			var attribute = new SecurityAttribute (
				module.TypeSystem.LookupType ("System.Security.Permissions", "PermissionSetAttribute"));

			attribute.properties = new Collection<CustomAttributeNamedArgument> (1);
			attribute.properties.Add (
				new CustomAttributeNamedArgument (
					"XML",
					new CustomAttributeArgument (
						module.TypeSystem.String,
						Encoding.Unicode.GetString (blob, 0, blob.Length))));

			attributes.Add (attribute);

			declaration.security_attributes = attributes;
		}

		public Collection<ExportedType> ReadExportedTypes ()
		{
			var length = MoveTo (Table.ExportedType);
			if (length == 0)
				return new Collection<ExportedType> ();

			var exported_types = new Collection<ExportedType> (length);

			for (int i = 1; i <= length; i++) {
				var attributes = (TypeAttributes) ReadUInt32 ();
				var identifier = ReadUInt32 ();
				var name = ReadString ();
				var @namespace = ReadString ();
				var implementation = ReadMetadataToken (CodedIndex.Implementation);

				ExportedType declaring_type = null;
				IMetadataScope scope = null;

				switch (implementation.TokenType) {
				case TokenType.AssemblyRef:
				case TokenType.File:
					scope = GetExportedTypeScope (implementation);
					break;
				case TokenType.ExportedType:
					// FIXME: if the table is not properly sorted
					declaring_type = exported_types [(int) implementation.RID - 1];
					break;
				}

				var exported_type = new ExportedType (@namespace, name, module, scope) {
					Attributes = attributes,
					Identifier = (int) identifier,
					DeclaringType = declaring_type,
				};
				exported_type.token = new MetadataToken (TokenType.ExportedType, i);

				exported_types.Add (exported_type);
			}

			return exported_types;
		}

		IMetadataScope GetExportedTypeScope (MetadataToken token)
		{
			var position = this.position;
			IMetadataScope scope;

			switch (token.TokenType) {
			case TokenType.AssemblyRef:
				InitializeAssemblyReferences ();
				scope = metadata.AssemblyReferences [(int) token.RID - 1];
				break;
			case TokenType.File:
				scope = GetModuleReferenceFromFile (token);
				if (scope == null)
					throw new NotSupportedException ();

				break;
			default:
				throw new NotSupportedException ();
			}

			this.position = position;
			return scope;
		}

		ModuleReference GetModuleReferenceFromFile (MetadataToken token)
		{
			if (!MoveTo (Table.File, token.RID))
				return null;

			ReadUInt32 ();
			var file_name = ReadString ();
			var modules = module.ModuleReferences;

			ModuleReference reference = null;
			for (int i = 0; i < modules.Count; i++) {
				var module_reference = modules [i];
				if (module_reference.Name != file_name)
					continue;

				reference = module_reference;
				break;
			}

			return reference;
		}

		static void InitializeCollection (object o)
		{
		}
	}

	sealed class SignatureReader : ByteBuffer {

		readonly MetadataReader reader;
		readonly uint start, sig_length;

		TypeSystem TypeSystem {
			get { return reader.module.TypeSystem; }
		}

		public SignatureReader (uint blob, MetadataReader reader)
			: base (reader.buffer)
		{
			this.reader = reader;

			MoveToBlob (blob);

			this.sig_length = ReadCompressedUInt32 ();
			this.start = (uint) position;
		}

		void MoveToBlob (uint blob)
		{
			position = (int) (reader.image.BlobHeap.Offset + blob);
		}

		MetadataToken ReadTypeTokenSignature ()
		{
			return CodedIndex.TypeDefOrRef.GetMetadataToken (ReadCompressedUInt32 ());
		}

		GenericParameter GetGenericParameter (GenericParameterType type, uint var)
		{
			var context = reader.context;

			if (context == null)
				throw new NotSupportedException ();

			IGenericParameterProvider provider;

			switch (type) {
			case GenericParameterType.Type:
				provider = context.Type;
				break;
			case GenericParameterType.Method:
				provider = context.Method;
				break;
			default:
				throw new NotSupportedException ();
			}

			int index = (int) var;

			if (!context.IsDefinition)
				CheckGenericContext (provider, index);

			return provider.GenericParameters [index];
		}

		static void CheckGenericContext (IGenericParameterProvider owner, int index)
		{
			var owner_parameters = owner.GenericParameters;

			for (int i = owner_parameters.Count; i <= index; i++)
				owner_parameters.Add (new GenericParameter (owner));
		}

		public void ReadGenericInstanceSignature (IGenericParameterProvider provider, IGenericInstance instance)
		{
			var arity = ReadCompressedUInt32 ();

			if (!provider.IsDefinition)
				CheckGenericContext (provider, (int) arity - 1);

			var instance_arguments = instance.GenericArguments;

			for (int i = 0; i < arity; i++)
				instance_arguments.Add (ReadTypeSignature ());
		}

		ArrayType ReadArrayTypeSignature ()
		{
			var array = new ArrayType (ReadTypeSignature ());

			var rank = ReadCompressedUInt32 ();

			var sizes = new uint [ReadCompressedUInt32 ()];
			for (int i = 0; i < sizes.Length; i++)
				sizes [i] = ReadCompressedUInt32 ();

			var low_bounds = new int [ReadCompressedUInt32 ()];
			for (int i = 0; i < low_bounds.Length; i++)
				low_bounds [i] = ReadCompressedInt32 ();

			array.Dimensions.Clear ();

			for (int i = 0; i < rank; i++) {
				int? lower = null, upper = null;

				if (i < low_bounds.Length)
					lower = low_bounds [i];

				if (i < sizes.Length)
					upper = lower + (int) sizes [i] - 1;

				array.Dimensions.Add (new ArrayDimension (lower, upper));
			}

			return array;
		}

		TypeReference GetTypeDefOrRef (MetadataToken token)
		{
			return reader.GetTypeDefOrRef (token);
		}

		public TypeReference ReadTypeSignature ()
		{
			return ReadTypeSignature ((ElementType) ReadByte ());
		}

		TypeReference ReadTypeSignature (ElementType etype)
		{
			switch (etype) {
			case ElementType.ValueType: {
				var value_type = GetTypeDefOrRef (ReadTypeTokenSignature ());
				value_type.IsValueType = true;
				return value_type;
			}
			case ElementType.Class:
				return GetTypeDefOrRef (ReadTypeTokenSignature ());
			case ElementType.Ptr:
				return new PointerType (ReadTypeSignature ());
			case ElementType.FnPtr: {
				var fptr = new FunctionPointerType ();
				ReadMethodSignature (fptr);
				return fptr;
			}
			case ElementType.ByRef:
				return new ByReferenceType (ReadTypeSignature ());
			case ElementType.Pinned:
				return new PinnedType (ReadTypeSignature ());
			case ElementType.SzArray:
				return new ArrayType (ReadTypeSignature ());
			case ElementType.Array:
				return ReadArrayTypeSignature ();
			case ElementType.CModOpt:
				return new OptionalModifierType (
					GetTypeDefOrRef (ReadTypeTokenSignature ()), ReadTypeSignature ());
			case ElementType.CModReqD:
				return new RequiredModifierType (
					GetTypeDefOrRef (ReadTypeTokenSignature ()), ReadTypeSignature ());
			case ElementType.Sentinel:
				return new SentinelType (ReadTypeSignature ());
			case ElementType.Var:
				return GetGenericParameter (GenericParameterType.Type, ReadCompressedUInt32 ());
			case ElementType.MVar:
				return GetGenericParameter (GenericParameterType.Method, ReadCompressedUInt32 ());
			case ElementType.GenericInst: {
				var is_value_type = ReadByte () == (byte) ElementType.ValueType;
				var element_type = GetTypeDefOrRef (ReadTypeTokenSignature ());
				var generic_instance = new GenericInstanceType (element_type);

				ReadGenericInstanceSignature (element_type, generic_instance);

				if (is_value_type) {
					generic_instance.IsValueType = true;
					element_type.GetElementType ().IsValueType = true;
				}

				return generic_instance;
			}
			case ElementType.Object: return TypeSystem.Object;
			case ElementType.Void: return TypeSystem.Void;
			case ElementType.TypedByRef: return TypeSystem.TypedReference;
			case ElementType.I: return TypeSystem.IntPtr;
			case ElementType.U: return TypeSystem.UIntPtr;
			default: return GetPrimitiveType (etype);
			}
		}

		public void ReadMethodSignature (IMethodSignature method)
		{
			var calling_convention = ReadByte ();

			const byte has_this = 0x20;
			const byte explicit_this = 0x40;

			if ((calling_convention & has_this) != 0) {
				method.HasThis = true;
				calling_convention = (byte) (calling_convention & ~has_this);
			}

			if ((calling_convention & explicit_this) != 0) {
				method.ExplicitThis = true;
				calling_convention = (byte) (calling_convention & ~explicit_this);
			}

			method.CallingConvention = (MethodCallingConvention) calling_convention;

			var generic_context = method as MethodReference;
			if (generic_context != null)
				reader.context = generic_context;

			if ((calling_convention & 0x10) != 0) {
				var arity = ReadCompressedUInt32 ();

				if (generic_context != null && !generic_context.IsDefinition)
					CheckGenericContext (generic_context, (int) arity -1 );
			}

			var param_count = ReadCompressedUInt32 ();

			method.MethodReturnType.ReturnType = ReadTypeSignature ();

			if (param_count == 0)
				return;

			Collection<ParameterDefinition> parameters;

			var method_ref = method as MethodReference;
			if (method_ref != null)
				parameters = method_ref.parameters = new ParameterDefinitionCollection (method, (int) param_count);
			else
				parameters = method.Parameters;

			for (int i = 0; i < param_count; i++)
				parameters.Add (new ParameterDefinition (ReadTypeSignature ()));
		}

		public object ReadConstantSignature (ElementType type)
		{
			return ReadPrimitiveValue (type);
		}

		public void ReadCustomAttributeConstructorArguments (CustomAttribute attribute, Collection<ParameterDefinition> parameters)
		{
			var count = parameters.Count;
			if (count == 0)
				return;

			attribute.arguments = new Collection<CustomAttributeArgument> (count);

			for (int i = 0; i < count; i++)
				attribute.arguments.Add (
					ReadCustomAttributeFixedArgument (parameters [i].ParameterType));
		}

		CustomAttributeArgument ReadCustomAttributeFixedArgument (TypeReference type)
		{
			if (type.IsArray)
				return ReadCustomAttributeFixedArrayArgument ((ArrayType) type);

			return ReadCustomAttributeElement (type);
		}

		public void ReadCustomAttributeNamedArguments (ushort count, ref Collection<CustomAttributeNamedArgument> fields, ref Collection<CustomAttributeNamedArgument> properties)
		{
			for (int i = 0; i < count; i++)
				ReadCustomAttributeNamedArgument (ref fields, ref properties);
		}

		void ReadCustomAttributeNamedArgument (ref Collection<CustomAttributeNamedArgument> fields, ref Collection<CustomAttributeNamedArgument> properties)
		{
			var kind = ReadByte ();
			var type = ReadCustomAttributeFieldOrPropType ();
			var name = ReadUTF8String ();

			Collection<CustomAttributeNamedArgument> container;
			switch (kind) {
			case 0x53:
				container = GetCustomAttributeNamedArgumentCollection (ref fields);
				break;
			case 0x54:
				container = GetCustomAttributeNamedArgumentCollection (ref properties);
				break;
			default:
				throw new NotSupportedException ();
			}

			container.Add (new CustomAttributeNamedArgument (name, ReadCustomAttributeFixedArgument (type)));
		}

		static Collection<CustomAttributeNamedArgument> GetCustomAttributeNamedArgumentCollection (ref Collection<CustomAttributeNamedArgument> collection)
		{
			if (collection != null)
				return collection;

			return collection = new Collection<CustomAttributeNamedArgument> ();
		}

		CustomAttributeArgument ReadCustomAttributeFixedArrayArgument (ArrayType type)
		{
			var length = ReadUInt32 ();

			if (length == 0xffffffff)
				return new CustomAttributeArgument (type, null);

			if (length == 0)
				return new CustomAttributeArgument (type, Empty<CustomAttributeArgument>.Array);

			var arguments = new CustomAttributeArgument [length];
			var element_type = type.ElementType;

			for (int i = 0; i < length; i++)
				arguments [i] = ReadCustomAttributeElement (element_type);

			return new CustomAttributeArgument (type, arguments);
		}

		CustomAttributeArgument ReadCustomAttributeElement (TypeReference type)
		{
			if (type.IsArray)
				return ReadCustomAttributeFixedArrayArgument ((ArrayType) type);

			return new CustomAttributeArgument (
				type,
				type.etype == ElementType.Object
					? ReadCustomAttributeElement (ReadCustomAttributeFieldOrPropType ())
					: ReadCustomAttributeElementValue (type));
		}

		object ReadCustomAttributeElementValue (TypeReference type)
		{
			var etype = type.etype;

			switch (etype) {
			case ElementType.String:
				return ReadUTF8String ();
			case ElementType.None:
				if (type.IsTypeOf ("System", "Type"))
					return ReadTypeReference ();

				return ReadCustomAttributeEnum (type);
			default:
				return ReadPrimitiveValue (etype);
			}
		}

		object ReadPrimitiveValue (ElementType type)
		{
			switch (type) {
			case ElementType.Boolean:
				return ReadByte () == 1;
			case ElementType.I1:
				return (sbyte) ReadByte ();
			case ElementType.U1:
				return ReadByte ();
			case ElementType.Char:
				return (char) ReadUInt16 ();
			case ElementType.I2:
				return ReadInt16 ();
			case ElementType.U2:
				return ReadUInt16 ();
			case ElementType.I4:
				return ReadInt32 ();
			case ElementType.U4:
				return ReadUInt32 ();
			case ElementType.I8:
				return ReadInt64 ();
			case ElementType.U8:
				return ReadUInt64 ();
			case ElementType.R4:
				return ReadSingle ();
			case ElementType.R8:
				return ReadDouble ();
			default:
				throw new NotImplementedException (type.ToString ());
			}
		}

		TypeReference GetPrimitiveType (ElementType etype)
		{
			switch (etype) {
			case ElementType.Boolean:
				return TypeSystem.Boolean;
			case ElementType.Char:
				return TypeSystem.Char;
			case ElementType.I1:
				return TypeSystem.SByte;
			case ElementType.U1:
				return TypeSystem.Byte;
			case ElementType.I2:
				return TypeSystem.Int16;
			case ElementType.U2:
				return TypeSystem.UInt16;
			case ElementType.I4:
				return TypeSystem.Int32;
			case ElementType.U4:
				return TypeSystem.UInt32;
			case ElementType.I8:
				return TypeSystem.Int64;
			case ElementType.U8:
				return TypeSystem.UInt64;
			case ElementType.R4:
				return TypeSystem.Single;
			case ElementType.R8:
				return TypeSystem.Double;
			case ElementType.String:
				return TypeSystem.String;
			default:
				throw new NotImplementedException (etype.ToString ());
			}
		}

		TypeReference ReadCustomAttributeFieldOrPropType ()
		{
			var etype = (ElementType) ReadByte ();

			switch (etype) {
			case ElementType.Boxed:
				return TypeSystem.Object;
			case ElementType.SzArray:
				return new ArrayType (ReadCustomAttributeFieldOrPropType ());
			case ElementType.Enum:
				return ReadTypeReference ();
			case ElementType.Type:
				return TypeSystem.LookupType ("System", "Type");
			default:
				return GetPrimitiveType (etype);
			}
		}

		public TypeReference ReadTypeReference ()
		{
			return TypeParser.ParseType (reader.module, ReadUTF8String ());
		}

		object ReadCustomAttributeEnum (TypeReference enum_type)
		{
			var type = enum_type.CheckedResolve ();
			if (!type.IsEnum)
				throw new ArgumentException ();

			return ReadCustomAttributeElementValue (type.GetEnumUnderlyingType ());
		}

		public SecurityAttribute ReadSecurityAttribute ()
		{
			var attribute = new SecurityAttribute (ReadTypeReference ());

			ReadCompressedUInt32 ();

			ReadCustomAttributeNamedArguments (
				(ushort) ReadCompressedUInt32 (),
				ref attribute.fields,
				ref attribute.properties);

			return attribute;
		}

		public MarshalInfo ReadMarshalInfo ()
		{
			var native = ReadNativeType ();
			switch (native) {
			case NativeType.Array: {
				var array = new ArrayMarshalInfo ();
				if (CanReadMore ())
					array.element_type = ReadNativeType ();
				if (CanReadMore ())
					array.size_parameter_index = (int) ReadCompressedUInt32 ();
				if (CanReadMore ())
					array.size = (int) ReadCompressedUInt32 ();
				if (CanReadMore ())
					array.size_parameter_multiplier = (int) ReadCompressedUInt32 ();
				return array;
			}
			case NativeType.SafeArray: {
				var array = new SafeArrayMarshalInfo ();
				if (CanReadMore ())
					array.element_type = ReadVariantType ();
				return array;
			}
			case NativeType.FixedArray: {
				var array = new FixedArrayMarshalInfo ();
				if (CanReadMore ())
					array.size = (int) ReadCompressedUInt32 ();
				if (CanReadMore ())
					array.element_type = ReadNativeType ();
				return array;
			}
			case NativeType.FixedSysString: {
				var sys_string = new FixedSysStringMarshalInfo ();
				if (CanReadMore ())
					sys_string.size = (int) ReadCompressedUInt32 ();
				return sys_string;
			}
			case NativeType.CustomMarshaler: {
				var marshaler = new CustomMarshalInfo ();
				var guid_value = ReadUTF8String ();
				marshaler.guid = !string.IsNullOrEmpty (guid_value) ? new Guid (guid_value) : Guid.Empty;
				marshaler.unmanaged_type = ReadUTF8String ();
				marshaler.managed_type = ReadTypeReference ();
				marshaler.cookie = ReadUTF8String ();
				return marshaler;
			}
			default:
				return new MarshalInfo (native);
			}
		}

		NativeType ReadNativeType ()
		{
			return (NativeType) ReadByte ();
		}

		VariantType ReadVariantType ()
		{
			return (VariantType) ReadByte ();
		}

		string ReadUTF8String ()
		{
			if (buffer [position] == 0xff) {
				position++;
				return null;
			}

			var length = (int) ReadCompressedUInt32 ();
			if (length == 0)
				return string.Empty;

			var @string = Encoding.UTF8.GetString (buffer, position,
				buffer [position + length - 1] == 0 ? length - 1 : length);

			position += length;
			return @string;
		}

		public bool CanReadMore ()
		{
			return position - start < sig_length;
		}
	}
}
