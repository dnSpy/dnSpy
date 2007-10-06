//
// StructureWriter.cs
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

	internal sealed class StructureWriter : BaseStructureVisitor {

		MetadataWriter m_mdWriter;
		MetadataTableWriter m_tableWriter;
		MetadataRowWriter m_rowWriter;

		AssemblyDefinition m_asm;
		BinaryWriter m_binaryWriter;

		public AssemblyDefinition Assembly {
			get { return m_asm; }
		}

		static void ResetImage (ModuleDefinition mod)
		{
			Image ni = Image.CreateImage ();
			ni.Accept (new CopyImageVisitor (mod.Image));
			mod.Image = ni;
		}

		public StructureWriter (AssemblyDefinition asm, BinaryWriter writer)
		{
			m_asm = asm;
			m_binaryWriter = writer;
		}

		public BinaryWriter GetWriter ()
		{
			return m_binaryWriter;
		}

		public override void VisitAssemblyDefinition (AssemblyDefinition asm)
		{
			if (asm.Kind != AssemblyKind.Dll && asm.EntryPoint == null)
				throw new ReflectionException ("Assembly does not have an entry point defined");

			if ((asm.MainModule.Image.CLIHeader.Flags & RuntimeImage.ILOnly) == 0)
				throw new NotSupportedException ("Can not write a mixed mode assembly");

			foreach (ModuleDefinition module in asm.Modules)
				if (module.Image.CLIHeader.Metadata.VirtualAddress != RVA.Zero)
					ResetImage (module);

			ReflectionWriter rw = asm.MainModule.Controller.Writer;
			rw.StructureWriter = this;

			m_mdWriter = rw.MetadataWriter;
			m_tableWriter = rw.MetadataTableWriter;
			m_rowWriter = rw.MetadataRowWriter;

			if (!rw.SaveSymbols)
				return;

			FileStream fs = m_binaryWriter.BaseStream as FileStream;
			if (fs != null)
				rw.OutputFile = fs.Name;
		}

		public override void VisitAssemblyNameDefinition (AssemblyNameDefinition name)
		{
			AssemblyTable asmTable = m_tableWriter.GetAssemblyTable ();

			if (name.PublicKey != null && name.PublicKey.Length > 0)
				name.Flags |= AssemblyFlags.PublicKey;

			AssemblyRow asmRow = m_rowWriter.CreateAssemblyRow (
				name.HashAlgorithm,
				(ushort) name.Version.Major,
				(ushort) name.Version.Minor,
				(ushort) name.Version.Build,
				(ushort) name.Version.Revision,
				name.Flags,
				m_mdWriter.AddBlob (name.PublicKey),
				m_mdWriter.AddString (name.Name),
				m_mdWriter.AddString (name.Culture));

			asmTable.Rows.Add (asmRow);
		}

		public override void VisitAssemblyNameReferenceCollection (AssemblyNameReferenceCollection references)
		{
			foreach (AssemblyNameReference name in references)
				VisitAssemblyNameReference (name);
		}

		public override void VisitAssemblyNameReference (AssemblyNameReference name)
		{
			byte [] pkortoken;
			if (name.PublicKey != null && name.PublicKey.Length > 0)
				pkortoken = name.PublicKey;
			else if (name.PublicKeyToken != null && name.PublicKeyToken.Length > 0)
				pkortoken = name.PublicKeyToken;
			else
				pkortoken = new byte [0];

			AssemblyRefTable arTable = m_tableWriter.GetAssemblyRefTable ();
			AssemblyRefRow arRow = m_rowWriter.CreateAssemblyRefRow (
				(ushort) name.Version.Major,
				(ushort) name.Version.Minor,
				(ushort) name.Version.Build,
				(ushort) name.Version.Revision,
				name.Flags,
				m_mdWriter.AddBlob (pkortoken),
				m_mdWriter.AddString (name.Name),
				m_mdWriter.AddString (name.Culture),
				m_mdWriter.AddBlob (name.Hash));

			arTable.Rows.Add (arRow);
		}

		public override void VisitResourceCollection (ResourceCollection resources)
		{
			VisitCollection (resources);
		}

		public override void VisitEmbeddedResource (EmbeddedResource res)
		{
			AddManifestResource (
				m_mdWriter.AddResource (res.Data),
				res.Name, res.Flags,
				new MetadataToken (TokenType.ManifestResource, 0));
		}

		public override void VisitLinkedResource (LinkedResource res)
		{
			FileTable fTable = m_tableWriter.GetFileTable ();
			FileRow fRow = m_rowWriter.CreateFileRow (
				Mono.Cecil.FileAttributes.ContainsNoMetaData,
				m_mdWriter.AddString (res.File),
				m_mdWriter.AddBlob (res.Hash));

			fTable.Rows.Add (fRow);

			AddManifestResource (
				0, res.Name, res.Flags,
				new MetadataToken (TokenType.File, (uint) fTable.Rows.IndexOf (fRow) + 1));
		}

		public override void VisitAssemblyLinkedResource (AssemblyLinkedResource res)
		{
			MetadataToken impl = new MetadataToken (TokenType.AssemblyRef,
				(uint) m_asm.MainModule.AssemblyReferences.IndexOf (res.Assembly) + 1);

			AddManifestResource (0, res.Name, res.Flags, impl);
		}

		void AddManifestResource (uint offset, string name, ManifestResourceAttributes flags, MetadataToken impl)
		{
			ManifestResourceTable mrTable = m_tableWriter.GetManifestResourceTable ();
			ManifestResourceRow mrRow = m_rowWriter.CreateManifestResourceRow (
				offset,
				flags,
				m_mdWriter.AddString (name),
				impl);

			mrTable.Rows.Add (mrRow);
		}

		public override void VisitModuleDefinitionCollection (ModuleDefinitionCollection modules)
		{
			VisitCollection (modules);
		}

		public override void VisitModuleDefinition (ModuleDefinition module)
		{
			if (module.Main) {
				ModuleTable modTable = m_tableWriter.GetModuleTable ();
				ModuleRow modRow = m_rowWriter.CreateModuleRow (
					0,
					m_mdWriter.AddString (module.Name),
					m_mdWriter.AddGuid (module.Mvid),
					0,
					0);

				modTable.Rows.Add (modRow);
			} else {
				// multiple module assemblies
				throw new NotImplementedException ();
			}
		}

		public override void VisitModuleReferenceCollection (ModuleReferenceCollection modules)
		{
			VisitCollection (modules);
		}

		public override void VisitModuleReference (ModuleReference module)
		{
			ModuleRefTable mrTable = m_tableWriter.GetModuleRefTable ();
			ModuleRefRow mrRow = m_rowWriter.CreateModuleRefRow (
				m_mdWriter.AddString (module.Name));

			mrTable.Rows.Add (mrRow);
		}

		public override void TerminateAssemblyDefinition (AssemblyDefinition asm)
		{
			foreach (ModuleDefinition mod in asm.Modules) {
				ReflectionWriter writer = mod.Controller.Writer;
				writer.VisitModuleDefinition (mod);
				writer.VisitTypeReferenceCollection (mod.TypeReferences);
				writer.VisitTypeDefinitionCollection (mod.Types);
				writer.VisitMemberReferenceCollection (mod.MemberReferences);
				writer.CompleteTypeDefinitions ();

				writer.TerminateModuleDefinition (mod);
			}
		}
	}
}
