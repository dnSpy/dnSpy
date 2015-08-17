// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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

using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.ILSpy.XmlDoc;

namespace ICSharpCode.ILSpy {
	/// <summary>
	/// IL language support.
	/// </summary>
	/// <remarks>
	/// Currently comes in two versions:
	/// flat IL (detectControlStructure=false) and structured IL (detectControlStructure=true).
	/// </remarks>
	public class ILLanguage : Language
	{
		private readonly bool detectControlStructure;
		
		public ILLanguage(bool detectControlStructure)
		{
			this.detectControlStructure = detectControlStructure;
		}
		
		public override string Name {
			get { return "IL"; }
		}
		
		public override string FileExtension {
			get { return ".il"; }
		}

		ReflectionDisassembler CreateReflectionDisassembler(ITextOutput output, DecompilationOptions options, IMemberDef member)
		{
			return CreateReflectionDisassembler(output, options, member.Module);
		}

		ReflectionDisassembler CreateReflectionDisassembler(ITextOutput output, DecompilationOptions options, ModuleDef ownerModule)
		{
			var disOpts = new DisassemblerOptions(options.CancellationToken, ownerModule);
			if (options.DecompilerSettings.ShowILComments)
				disOpts.GetOpCodeDocumentation = GetOpCodeDocumentation;
			if (options.DecompilerSettings.ShowXmlDocumentation)
				disOpts.GetXmlDocComments = GetXmlDocComments;
			disOpts.CreateInstructionBytesReader = InstructionBytesReader.Create;
			disOpts.ShowTokenAndRvaComments = options.DecompilerSettings.ShowTokenAndRvaComments;
			disOpts.ShowILBytes = options.DecompilerSettings.ShowILBytes;
			disOpts.SortMembers = options.DecompilerSettings.SortMembers;
			return new ReflectionDisassembler(output, detectControlStructure, disOpts);
		}

		static readonly string[] cachedOpCodeDocs = new string[0x200];
		public static string GetOpCodeDocumentation(OpCode code)
		{
			int index = (int)code.Code;
			int hi = index >> 8;
			if (hi == 0xFE)
				index -= 0xFD00;
			else if (hi != 0)
				return null;
			var s = cachedOpCodeDocs[index];
			if (s != null)
				return s;

			var docProvider = XmlDocLoader.MscorlibDocumentation;
			if (docProvider != null) {
				string docXml = docProvider.GetDocumentation("F:System.Reflection.Emit.OpCodes." + code.Code.ToString());
				if (docXml != null) {
					XmlDocRenderer renderer = new XmlDocRenderer();
					renderer.AddXmlDocumentation(docXml);
					return cachedOpCodeDocs[index] = renderer.ToString();
				}
			}

			return null;
		}

		static IEnumerable<string> GetXmlDocComments(IMemberRef mr)
		{
			if (mr == null || mr.Module == null)
				yield break;
			var xmldoc = XmlDocLoader.LoadDocumentation(mr.Module);
			if (xmldoc == null)
				yield break;
			string doc = xmldoc.GetDocumentation(XmlDocKeyProvider.GetKey(mr));
			if (doc == null)
				yield break;

			foreach (var line in AddXmlDocTransform.GetXmlDocLines(new StringReader(doc)))
				yield return line;
		}
		
		public override void DecompileMethod(MethodDef method, ITextOutput output, DecompilationOptions options)
		{
			var dis = CreateReflectionDisassembler(output, options, method);
			dis.DisassembleMethod(method);
		}
		
		public override void DecompileField(FieldDef field, ITextOutput output, DecompilationOptions options)
		{
			var dis = CreateReflectionDisassembler(output, options, field);
			dis.DisassembleField(field);
		}
		
		public override void DecompileProperty(PropertyDef property, ITextOutput output, DecompilationOptions options)
		{
			ReflectionDisassembler rd = CreateReflectionDisassembler(output, options, property);
			rd.DisassembleProperty(property);
			if (property.GetMethod != null) {
				output.WriteLine();
				rd.DisassembleMethod(property.GetMethod);
			}
			if (property.SetMethod != null) {
				output.WriteLine();
				rd.DisassembleMethod(property.SetMethod);
			}
			foreach (var m in property.OtherMethods) {
				output.WriteLine();
				rd.DisassembleMethod(m);
			}
		}
		
		public override void DecompileEvent(EventDef ev, ITextOutput output, DecompilationOptions options)
		{
			ReflectionDisassembler rd = CreateReflectionDisassembler(output, options, ev);
			rd.DisassembleEvent(ev);
			if (ev.AddMethod != null) {
				output.WriteLine();
				rd.DisassembleMethod(ev.AddMethod);
			}
			if (ev.RemoveMethod != null) {
				output.WriteLine();
				rd.DisassembleMethod(ev.RemoveMethod);
			}
			foreach (var m in ev.OtherMethods) {
				output.WriteLine();
				rd.DisassembleMethod(m);
			}
		}
		
		public override void DecompileType(TypeDef type, ITextOutput output, DecompilationOptions options)
		{
			var dis = CreateReflectionDisassembler(output, options, type);
			dis.DisassembleType(type);
		}
		
		public override void DecompileAssembly(LoadedAssembly assembly, ITextOutput output, DecompilationOptions options, DecompileAssemblyFlags flags = DecompileAssemblyFlags.AssemblyAndModule)
		{
			bool decompileAsm = (flags & DecompileAssemblyFlags.Assembly) != 0;
			bool decompileMod = (flags & DecompileAssemblyFlags.Module) != 0;
			output.WriteLine("// " + assembly.FileName, TextTokenType.Comment);
			if (decompileMod || decompileAsm)
				PrintEntryPoint(assembly, output);
			output.WriteLine();
			
			ReflectionDisassembler rd = CreateReflectionDisassembler(output, options, assembly.ModuleDefinition);
			if (decompileMod && options.FullDecompilation)
				rd.WriteAssemblyReferences(assembly.ModuleDefinition as ModuleDefMD);
			if (decompileAsm && assembly.AssemblyDefinition != null)
				rd.WriteAssemblyHeader(assembly.AssemblyDefinition);
			if (decompileMod) {
				output.WriteLine();
				rd.WriteModuleHeader(assembly.ModuleDefinition);
				if (options.FullDecompilation) {
					output.WriteLine();
					output.WriteLine();
					rd.WriteModuleContents(assembly.ModuleDefinition);
				}
			}
		}

		public override void TypeToString(ITextOutput output, ITypeDefOrRef t, bool includeNamespace, IHasCustomAttribute attributeProvider = null)
		{
			t.WriteTo(output, includeNamespace ? ILNameSyntax.TypeName : ILNameSyntax.ShortTypeName);
		}

		public override void WriteToolTip(ITextOutput output, IMemberRef member, IHasCustomAttribute typeAttributes)
		{
			if (!(member is ITypeDefOrRef) && Write(output, member))
				return;

			base.WriteToolTip(output, member, typeAttributes);
		}

		public static bool Write(ITextOutput output, IMemberRef member)
		{
			var method = member as IMethod;
			if (method != null && method.IsMethod) {
				method.WriteMethodTo(output);
				return true;
			}

			var field = member as IField;
			if (field != null && field.IsField) {
				field.WriteFieldTo(output);
				return true;
			}

			var prop = member as PropertyDef;
			if (prop != null) {
				var dis = new ReflectionDisassembler(output, false, new DisassemblerOptions(new System.Threading.CancellationToken(), null));
				dis.DisassembleProperty(prop, false);
				return true;
			}

			var evt = member as EventDef;
			if (evt != null) {
				var dis = new ReflectionDisassembler(output, false, new DisassemblerOptions(new System.Threading.CancellationToken(), null));
				dis.DisassembleEvent(evt, false);
				return true;
			}

			var type = member as ITypeDefOrRef;
			if (type != null) {
				type.WriteTo(output, ILNameSyntax.TypeName);
				return true;
			}

			return false;
		}
	}
}
