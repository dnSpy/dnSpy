//
// StructureReader.cs
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
	using System.IO;

	using Mono.Cecil.Binary;
	using Mono.Cecil.Metadata;

	internal sealed class StructureReader : BaseStructureVisitor {

		ImageReader m_ir;
		Image m_img;
		bool m_manifestOnly;
		AssemblyDefinition m_asmDef;
		ModuleDefinition m_module;
		MetadataStreamCollection m_streams;
		TablesHeap m_tHeap;
		MetadataTableReader m_tableReader;

		public bool ManifestOnly {
			get { return m_manifestOnly; }
		}

		public ImageReader ImageReader {
			get { return m_ir; }
		}

		public Image Image {
			get { return m_img; }
		}

		public StructureReader (ImageReader ir)
		{
			if (ir.Image.CLIHeader == null)
				throw new ImageFormatException ("The image is not a managed assembly");

			m_ir = ir;
			m_img = ir.Image;
			m_streams = m_img.MetadataRoot.Streams;
			m_tHeap = m_streams.TablesHeap;
			m_tableReader = ir.MetadataReader.TableReader;
		}

		public StructureReader (ImageReader ir, bool manifestOnly) : this (ir)
		{
			m_manifestOnly = manifestOnly;
		}

		byte [] ReadBlob (uint pointer)
		{
			if (pointer == 0)
				return new byte [0];

			return m_streams.BlobHeap.Read (pointer);
		}

		string ReadString (uint pointer)
		{
			return m_streams.StringsHeap [pointer];
		}

		public override void VisitAssemblyDefinition (AssemblyDefinition asm)
		{
			if (!m_tHeap.HasTable (AssemblyTable.RId))
				throw new ReflectionException ("No assembly manifest");

			asm.MetadataToken = new MetadataToken (TokenType.Assembly, 1);
			m_asmDef = asm;

			switch (m_img.MetadataRoot.Header.Version) {
			case "v1.0.3705" :
				asm.Runtime = TargetRuntime.NET_1_0;
				break;
			case "v1.1.4322" :
				asm.Runtime = TargetRuntime.NET_1_1;
				break;
			default :
				asm.Runtime = TargetRuntime.NET_2_0;
				break;
			}

			if ((m_img.PEFileHeader.Characteristics & ImageCharacteristics.Dll) != 0)
				asm.Kind = AssemblyKind.Dll;
			else if (m_img.PEOptionalHeader.NTSpecificFields.SubSystem == SubSystem.WindowsGui ||
				m_img.PEOptionalHeader.NTSpecificFields.SubSystem == SubSystem.WindowsCeGui)
				asm.Kind = AssemblyKind.Windows;
			else
				asm.Kind = AssemblyKind.Console;
		}

		public override void VisitAssemblyNameDefinition (AssemblyNameDefinition name)
		{
			AssemblyTable atable = m_tableReader.GetAssemblyTable ();
			AssemblyRow arow = atable [0];
			name.Name = ReadString (arow.Name);
			name.Flags = arow.Flags;
			name.PublicKey = ReadBlob (arow.PublicKey);

			name.Culture = ReadString (arow.Culture);
			name.Version = new Version (
				arow.MajorVersion, arow.MinorVersion,
				arow.BuildNumber, arow.RevisionNumber);
			name.HashAlgorithm = arow.HashAlgId;
			name.MetadataToken = new MetadataToken (TokenType.Assembly, 1);
		}

		public override void VisitAssemblyNameReferenceCollection (AssemblyNameReferenceCollection names)
		{
			if (!m_tHeap.HasTable (AssemblyRefTable.RId))
				return;

			AssemblyRefTable arTable = m_tableReader.GetAssemblyRefTable ();
			for (int i = 0; i < arTable.Rows.Count; i++) {
				AssemblyRefRow arRow = arTable [i];
				AssemblyNameReference aname = new AssemblyNameReference (
					ReadString (arRow.Name),
					ReadString (arRow.Culture),
					new Version (arRow.MajorVersion, arRow.MinorVersion,
								 arRow.BuildNumber, arRow.RevisionNumber));
				aname.PublicKeyToken = ReadBlob (arRow.PublicKeyOrToken);
				aname.Hash = ReadBlob (arRow.HashValue);
				aname.Flags = arRow.Flags;
				aname.MetadataToken = new MetadataToken (TokenType.AssemblyRef, (uint) i + 1);
				names.Add (aname);
			}
		}

		public override void VisitResourceCollection (ResourceCollection resources)
		{
			if (!m_tHeap.HasTable (ManifestResourceTable.RId))
				return;

			ManifestResourceTable mrTable = m_tableReader.GetManifestResourceTable ();
			FileTable fTable = m_tableReader.GetFileTable ();

			for (int i = 0; i < mrTable.Rows.Count; i++) {
				ManifestResourceRow mrRow = mrTable [i];
				if (mrRow.Implementation.RID == 0) {
					EmbeddedResource eres = new EmbeddedResource (
						ReadString (mrRow.Name), mrRow.Flags);

					BinaryReader br = m_ir.MetadataReader.GetDataReader (
						m_img.CLIHeader.Resources.VirtualAddress);
					br.BaseStream.Position += mrRow.Offset;

					eres.Data = br.ReadBytes (br.ReadInt32 ());

					resources.Add (eres);
					continue;
				}

				switch (mrRow.Implementation.TokenType) {
				case TokenType.File :
					FileRow fRow = fTable [(int) mrRow.Implementation.RID - 1];
					LinkedResource lres = new LinkedResource (
						ReadString (mrRow.Name), mrRow.Flags,
						ReadString (fRow.Name));
					lres.Hash = ReadBlob (fRow.HashValue);
					resources.Add (lres);
					break;
				case TokenType.AssemblyRef :
					AssemblyNameReference asm =
						m_module.AssemblyReferences [(int) mrRow.Implementation.RID - 1];
					AssemblyLinkedResource alr = new AssemblyLinkedResource (
						ReadString (mrRow.Name),
						mrRow.Flags, asm);
					resources.Add (alr);
					break;
				}
			}
		}

		public override void VisitModuleDefinitionCollection (ModuleDefinitionCollection modules)
		{
			ModuleTable mt = m_tableReader.GetModuleTable ();
			if (mt == null || mt.Rows.Count != 1)
				throw new ReflectionException ("Can not read main module");

			ModuleRow mr = mt [0];
			string name = ReadString (mr.Name);
			ModuleDefinition main = new ModuleDefinition (name, m_asmDef, this, true);
			main.Mvid = m_streams.GuidHeap [mr.Mvid];
			main.MetadataToken = new MetadataToken (TokenType.Module, 1);
			modules.Add (main);
			m_module = main;
			m_module.Accept (this);

			FileTable ftable = m_tableReader.GetFileTable ();
			if (ftable == null || ftable.Rows.Count == 0)
				return;

			foreach (FileRow frow in ftable.Rows) {
				if (frow.Flags != FileAttributes.ContainsMetaData)
					continue;

				name = ReadString (frow.Name);
				FileInfo location = new FileInfo (
					m_img.FileInformation != null ? Path.Combine (m_img.FileInformation.DirectoryName, name) : name);
				if (!File.Exists (location.FullName))
					throw new FileNotFoundException ("Module not found : " + name);

				try {
					ImageReader module = ImageReader.Read (location.FullName);
					mt = module.Image.MetadataRoot.Streams.TablesHeap [ModuleTable.RId] as ModuleTable;
					if (mt == null || mt.Rows.Count != 1)
						throw new ReflectionException ("Can not read module : " + name);

					mr = mt [0];
					ModuleDefinition modext = new ModuleDefinition (name, m_asmDef,
						new StructureReader (module, m_manifestOnly), false);
					modext.Mvid = module.Image.MetadataRoot.Streams.GuidHeap [mr.Mvid];

					modules.Add (modext);
					modext.Accept (this);
				} catch (ReflectionException) {
					throw;
				} catch (Exception e) {
					throw new ReflectionException ("Can not read module : " + name, e);
				}
			}
		}

		public override void VisitModuleReferenceCollection (ModuleReferenceCollection modules)
		{
			if (!m_tHeap.HasTable (ModuleRefTable.RId))
				return;

			ModuleRefTable mrTable = m_tableReader.GetModuleRefTable ();
			for (int i = 0; i < mrTable.Rows.Count; i++) {
				ModuleRefRow mrRow = mrTable [i];
				ModuleReference mod = new ModuleReference (ReadString (mrRow.Name));
				mod.MetadataToken = MetadataToken.FromMetadataRow (TokenType.ModuleRef, i);
				modules.Add (mod);
			}
		}

		public override void TerminateAssemblyDefinition (AssemblyDefinition asm)
		{
			if (m_manifestOnly)
				return;

			foreach (ModuleDefinition mod in asm.Modules)
				mod.Controller.Reader.VisitModuleDefinition (mod);
		}
	}
}
