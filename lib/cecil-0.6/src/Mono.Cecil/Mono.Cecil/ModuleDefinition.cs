//
// ModuleDefinition.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

namespace Mono.Cecil {

	using System;
	using SR = System.Reflection;
	using SS = System.Security;
	using SSP = System.Security.Permissions;
	using System.Text;

	using Mono.Cecil.Cil;
	using Mono.Cecil.Binary;
	using Mono.Cecil.Metadata;

	public sealed class ModuleDefinition : ModuleReference, ICustomAttributeProvider, IMetadataScope,
		IReflectionStructureVisitable, IReflectionVisitable {

		Guid m_mvid;
		bool m_main;
		bool m_manifestOnly;

		AssemblyNameReferenceCollection m_asmRefs;
		ModuleReferenceCollection m_modRefs;
		ResourceCollection m_res;
		TypeDefinitionCollection m_types;
		TypeReferenceCollection m_refs;
		ExternTypeCollection m_externs;
		MemberReferenceCollection m_members;
		CustomAttributeCollection m_customAttrs;

		AssemblyDefinition m_asm;
		Image m_image;

		ImageReader m_imgReader;
		ReflectionController m_controller;
		SecurityDeclarationReader m_secReader;

		public Guid Mvid {
			get { return m_mvid; }
			set { m_mvid = value; }
		}

		public bool Main {
			get { return m_main; }
			set { m_main = value; }
		}

		public AssemblyNameReferenceCollection AssemblyReferences {
			get { return m_asmRefs; }
		}

		public ModuleReferenceCollection ModuleReferences {
			get { return m_modRefs; }
		}

		public ResourceCollection Resources {
			get { return m_res; }
		}

		public TypeDefinitionCollection Types {
			get { return m_types; }
		}

		public TypeReferenceCollection TypeReferences {
			get { return m_refs; }
		}

		public MemberReferenceCollection MemberReferences {
			get { return m_members; }
		}

		public ExternTypeCollection ExternTypes {
			get {
				if (m_externs == null)
					m_externs = new ExternTypeCollection (this);

				return m_externs;
			}
		}

		public CustomAttributeCollection CustomAttributes {
			get {
				if (m_customAttrs == null)
					m_customAttrs = new CustomAttributeCollection (this);

				return m_customAttrs;
			}
		}

		public AssemblyDefinition Assembly {
			get { return m_asm; }
		}

		internal ReflectionController Controller {
			get { return m_controller; }
		}

		internal ImageReader ImageReader {
			get { return m_imgReader; }
		}

		public Image Image {
			get { return m_image; }
			set {
				m_image = value;
				m_secReader = null;
			}
		}

		public ModuleDefinition (string name, AssemblyDefinition asm) :
			this (name, asm, null, false)
		{
		}

		public ModuleDefinition (string name, AssemblyDefinition asm, bool main) :
			this (name, asm, null, main)
		{
		}

		internal ModuleDefinition (string name, AssemblyDefinition asm, StructureReader reader) :
			this (name, asm, reader, false)
		{
		}

		internal ModuleDefinition (string name, AssemblyDefinition asm, StructureReader reader, bool main) : base (name)
		{
			if (asm == null)
				throw new ArgumentNullException ("asm");
			if (name == null || name.Length == 0)
				throw new ArgumentNullException ("name");

			m_asm = asm;
			m_main = main;
#if !CF_1_0
			m_mvid = Guid.NewGuid ();
#endif
			if (reader != null) {
				m_image = reader.Image;
				m_imgReader = reader.ImageReader;
				m_manifestOnly = reader.ManifestOnly;
			} else
				m_image = Image.CreateImage ();

			m_modRefs = new ModuleReferenceCollection (this);
			m_asmRefs = new AssemblyNameReferenceCollection (this);
			m_res = new ResourceCollection (this);
			m_types = new TypeDefinitionCollection (this);
			m_refs = new TypeReferenceCollection (this);
			m_members = new MemberReferenceCollection (this);

			m_controller = new ReflectionController (this);
		}

		public IMetadataTokenProvider LookupByToken (MetadataToken token)
		{
			return m_controller.Reader.LookupByToken (token);
		}

		public IMetadataTokenProvider LookupByToken (TokenType table, int rid)
		{
			return LookupByToken (new MetadataToken (table, (uint) rid));
		}

		void CheckContext (TypeDefinition context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			if (context.Module != this)
				throw new ArgumentException ("The context parameter does not belongs to this module");
			if (context.GenericParameters.Count == 0)
				throw new ArgumentException ("The context parameter is not a generic type");
		}

		ImportContext GetContext ()
		{
			return new ImportContext (m_controller.Importer);
		}

		static ImportContext GetContext (IImporter importer)
		{
			return new ImportContext (importer);
		}

		ImportContext GetContext (TypeDefinition context)
		{
			return new ImportContext (m_controller.Importer, context);
		}

		static ImportContext GetContext (IImporter importer, TypeDefinition context)
		{
			return new ImportContext (importer, context);
		}

		public TypeReference Import (Type type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");

			return m_controller.Helper.ImportSystemType (type, GetContext ());
		}

		public TypeReference Import (Type type, TypeDefinition context)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			CheckContext (context);

			return m_controller.Helper.ImportSystemType (type, GetContext (context));
		}

		public MethodReference Import (SR.MethodBase meth)
		{
			if (meth == null)
				throw new ArgumentNullException ("meth");

			if (meth is SR.ConstructorInfo)
				return m_controller.Helper.ImportConstructorInfo (
					meth as SR.ConstructorInfo, GetContext ());
			else
				return m_controller.Helper.ImportMethodInfo (
					meth as SR.MethodInfo, GetContext ());
		}

		public MethodReference Import (SR.MethodBase meth, TypeDefinition context)
		{
			if (meth == null)
				throw new ArgumentNullException ("meth");
			CheckContext (context);

			if (meth is SR.ConstructorInfo)
				return m_controller.Helper.ImportConstructorInfo (
					meth as SR.ConstructorInfo, GetContext (context));
			else
				return m_controller.Helper.ImportMethodInfo (
					meth as SR.MethodInfo, GetContext (context));
		}

		public FieldReference Import (SR.FieldInfo field)
		{
			if (field == null)
				throw new ArgumentNullException ("field");

			return m_controller.Helper.ImportFieldInfo (field, GetContext ());
		}

		public FieldReference Import (SR.FieldInfo field, TypeDefinition context)
		{
			if (field == null)
				throw new ArgumentNullException ("field");
			CheckContext (context);

			return m_controller.Helper.ImportFieldInfo (field, GetContext (context));
		}

		public TypeReference Import (TypeReference type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");

			return m_controller.Importer.ImportTypeReference (type, GetContext ());
		}

		public TypeReference Import (TypeReference type, TypeDefinition context)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			CheckContext (context);

			return m_controller.Importer.ImportTypeReference (type, GetContext (context));
		}

		public MethodReference Import (MethodReference meth)
		{
			if (meth == null)
				throw new ArgumentNullException ("meth");

			return m_controller.Importer.ImportMethodReference (meth, GetContext ());
		}

		public MethodReference Import (MethodReference meth, TypeDefinition context)
		{
			if (meth == null)
				throw new ArgumentNullException ("meth");
			CheckContext (context);

			return m_controller.Importer.ImportMethodReference (meth, GetContext (context));
		}

		public FieldReference Import (FieldReference field)
		{
			if (field == null)
				throw new ArgumentNullException ("field");

			return m_controller.Importer.ImportFieldReference (field, GetContext ());
		}

		public FieldReference Import (FieldReference field, TypeDefinition context)
		{
			if (field == null)
				throw new ArgumentNullException ("field");
			CheckContext (context);

			return m_controller.Importer.ImportFieldReference (field, GetContext (context));
		}

		static FieldDefinition ImportFieldDefinition (FieldDefinition field, ImportContext context)
		{
			return FieldDefinition.Clone (field, context);
		}

		static MethodDefinition ImportMethodDefinition (MethodDefinition meth, ImportContext context)
		{
			return MethodDefinition.Clone (meth, context);
		}

		static TypeDefinition ImportTypeDefinition (TypeDefinition type, ImportContext context)
		{
			return TypeDefinition.Clone (type, context);
		}

		public TypeDefinition Inject (TypeDefinition type)
		{
			return Inject (type, m_controller.Importer);
		}

		public TypeDefinition Inject (TypeDefinition type, IImporter importer)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			if (importer == null)
				throw new ArgumentNullException ("importer");

			TypeDefinition definition = ImportTypeDefinition (type, GetContext (importer));
			this.Types.Add (definition);
			return definition;
		}

		public TypeDefinition Inject (TypeDefinition type, TypeDefinition context)
		{
			return Inject (type, context, m_controller.Importer);
		}

		public TypeDefinition Inject (TypeDefinition type, TypeDefinition context, IImporter importer)
		{
			Check (type, context, importer);

			TypeDefinition definition = ImportTypeDefinition (type, GetContext (importer, context));
			context.NestedTypes.Add (definition);
			return definition;
		}

		public MethodDefinition Inject (MethodDefinition meth, TypeDefinition context)
		{
			return Inject (meth, context, m_controller.Importer);
		}

		void Check (IMemberDefinition definition, TypeDefinition context, IImporter importer)
		{
			if (definition == null)
				throw new ArgumentNullException ("definition");
			if (context == null)
				throw new ArgumentNullException ("context");
			if (importer == null)
				throw new ArgumentNullException ("importer");
			if (context.Module != this)
				throw new ArgumentException ("The context parameter does not belongs to this module");
		}

		public MethodDefinition Inject (MethodDefinition meth, TypeDefinition context, IImporter importer)
		{
			Check (meth, context, importer);

			MethodDefinition definition = ImportMethodDefinition (meth, GetContext (importer, context));
			context.Methods.Add (definition);
			return definition;
		}

		public FieldDefinition Inject (FieldDefinition field, TypeDefinition context, IImporter importer)
		{
			Check (field, context, importer);

			FieldDefinition definition = ImportFieldDefinition (field, GetContext (importer, context));
			context.Fields.Add (definition);
			return definition;
		}

		public void FullLoad ()
		{
			if (m_manifestOnly)
				m_controller.Reader.VisitModuleDefinition (this);

			foreach (TypeDefinition type in this.Types) {
				foreach (MethodDefinition meth in type.Methods)
					meth.LoadBody ();
				foreach (MethodDefinition ctor in type.Constructors)
					ctor.LoadBody ();
			}

			if (m_controller.Reader.SymbolReader == null)
				return;

			m_controller.Reader.SymbolReader.Dispose ();
			m_controller.Reader.SymbolReader = null;
		}

		public void LoadSymbols ()
		{
			m_controller.Reader.SymbolReader = SymbolStoreHelper.GetReader (this);
		}

		public void LoadSymbols (ISymbolReader reader)
		{
			m_controller.Reader.SymbolReader = reader;
		}

		public void SaveSymbols ()
		{
			m_controller.Writer.SaveSymbols = true;
		}

		public void SaveSymbols (ISymbolWriter writer)
		{
			SaveSymbols ();
			m_controller.Writer.SymbolWriter = writer;
		}

		public void SaveSymbols (string outputDirectory)
		{
			SaveSymbols ();
			m_controller.Writer.OutputFile = outputDirectory;
		}

		public void SaveSymbols (string outputDirectory, ISymbolWriter writer)
		{
			SaveSymbols (outputDirectory);
			m_controller.Writer.SymbolWriter = writer;
		}

		public byte [] GetAsByteArray (CustomAttribute ca)
		{
			CustomAttribute customAttr = ca;
			if (!ca.Resolved)
				if (customAttr.Blob != null)
					return customAttr.Blob;
				else
					return new byte [0];

			return m_controller.Writer.SignatureWriter.CompressCustomAttribute (
				ReflectionWriter.GetCustomAttributeSig (ca), ca.Constructor);
		}

		public byte [] GetAsByteArray (SecurityDeclaration dec)
		{
			// TODO - add support for 2.0 format
			// note: the 1.x format is still supported in 2.0 so this isn't an immediate problem
			if (!dec.Resolved)
				return dec.Blob;

#if !CF_1_0 && !CF_2_0
			if (dec.PermissionSet != null)
				return Encoding.Unicode.GetBytes (dec.PermissionSet.ToXml ().ToString ());
#endif

			return new byte [0];
		}

		public CustomAttribute FromByteArray (MethodReference ctor, byte [] data)
		{
			return m_controller.Reader.GetCustomAttribute (ctor, data);
		}

		public SecurityDeclaration FromByteArray (SecurityAction action, byte [] declaration)
		{
			if (m_secReader == null)
				m_secReader = new SecurityDeclarationReader (Image.MetadataRoot, m_controller.Reader);
			return m_secReader.FromByteArray (action, declaration);
		}

		public override void Accept (IReflectionStructureVisitor visitor)
		{
			visitor.VisitModuleDefinition (this);

			this.AssemblyReferences.Accept (visitor);
			this.ModuleReferences.Accept (visitor);
			this.Resources.Accept (visitor);
		}

		public void Accept (IReflectionVisitor visitor)
		{
			visitor.VisitModuleDefinition (this);

			this.Types.Accept (visitor);
			this.TypeReferences.Accept (visitor);
		}

		public override string ToString ()
		{
			string s = (m_main ? "(main), Mvid=" : "Mvid=");
			return s + m_mvid;
		}
	}
}
