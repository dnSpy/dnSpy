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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Decompiler.XmlDoc;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.IL;
using dnSpy.Decompiler.ILSpy.Core.Settings;
using dnSpy.Decompiler.ILSpy.Core.Text;
using dnSpy.Decompiler.ILSpy.Core.XmlDoc;
using ICSharpCode.Decompiler.Disassembler;

namespace dnSpy.Decompiler.ILSpy.Core.IL {
	sealed class DecompilerProvider : IDecompilerProvider {
		readonly DecompilerSettingsService decompilerSettingsService;

		// Keep the default ctor. It's used by dnSpy.Console.exe
		public DecompilerProvider()
			: this(DecompilerSettingsService.__Instance_DONT_USE) {
		}

		public DecompilerProvider(DecompilerSettingsService decompilerSettingsService) {
			Debug.Assert(decompilerSettingsService != null);
			this.decompilerSettingsService = decompilerSettingsService ?? throw new ArgumentNullException(nameof(decompilerSettingsService));
		}

		public IEnumerable<IDecompiler> Create() {
			yield return new ILDecompiler(decompilerSettingsService.ILDecompilerSettings);
		}
	}

	/// <summary>
	/// IL language support.
	/// </summary>
	/// <remarks>
	/// Currently comes in two versions:
	/// flat IL (detectControlStructure=false) and structured IL (detectControlStructure=true).
	/// </remarks>
	sealed class ILDecompiler : DecompilerBase {
		readonly bool detectControlStructure;

		public override DecompilerSettingsBase Settings => langSettings;
		readonly ILDecompilerSettings langSettings;

		public ILDecompiler(ILDecompilerSettings langSettings)
			: this(langSettings, true) {
		}

		public ILDecompiler(ILDecompilerSettings langSettings, bool detectControlStructure) {
			this.langSettings = langSettings;
			this.detectControlStructure = detectControlStructure;
		}

		public override double OrderUI => DecompilerConstants.IL_ILSPY_ORDERUI;
		public override string ContentTypeString => ContentTypesInternal.ILILSpy;
		public override string GenericNameUI => DecompilerConstants.GENERIC_NAMEUI_IL;
		public override string UniqueNameUI => "IL";
		public override Guid GenericGuid => DecompilerConstants.LANGUAGE_IL;
		public override Guid UniqueGuid => DecompilerConstants.LANGUAGE_IL_ILSPY;
		public override string FileExtension => ".il";

		ReflectionDisassembler CreateReflectionDisassembler(IDecompilerOutput output, DecompilationContext ctx, IMemberDef member) =>
			CreateReflectionDisassembler(output, ctx, member.Module);

		ReflectionDisassembler CreateReflectionDisassembler(IDecompilerOutput output, DecompilationContext ctx, ModuleDef ownerModule) {
			var disOpts = new DisassemblerOptions(langSettings.Settings.SettingsVersion, ctx.CancellationToken, ownerModule);
			if (langSettings.Settings.ShowILComments)
				disOpts.GetOpCodeDocumentation = ILLanguageHelper.GetOpCodeDocumentation;
			var sb = new StringBuilder();
			if (langSettings.Settings.ShowXmlDocumentation)
				disOpts.GetXmlDocComments = a => GetXmlDocComments(a, sb);
			disOpts.CreateInstructionBytesReader = m => InstructionBytesReader.Create(m, ctx.IsBodyModified != null && ctx.IsBodyModified(m));
			disOpts.ShowTokenAndRvaComments = langSettings.Settings.ShowTokenAndRvaComments;
			disOpts.ShowILBytes = langSettings.Settings.ShowILBytes;
			disOpts.SortMembers = langSettings.Settings.SortMembers;
			disOpts.ShowPdbInfo = langSettings.Settings.ShowPdbInfo;
			disOpts.MaxStringLength = langSettings.Settings.MaxStringLength;
			return new ReflectionDisassembler(output, detectControlStructure, disOpts);
		}

		static IEnumerable<string> GetXmlDocComments(IMemberRef mr, StringBuilder sb) {
			if (mr == null || mr.Module == null)
				yield break;
			var xmldoc = XmlDocLoader.LoadDocumentation(mr.Module);
			if (xmldoc == null)
				yield break;
			string doc = xmldoc.GetDocumentation(XmlDocKeyProvider.GetKey(mr, sb));
			if (string.IsNullOrEmpty(doc))
				yield break;

			foreach (var info in new XmlDocLine(doc)) {
				sb.Clear();
				if (info != null) {
					sb.Append(' ');
					info.Value.WriteTo(sb);
				}
				yield return sb.ToString();
			}
		}

		public override void Decompile(MethodDef method, IDecompilerOutput output, DecompilationContext ctx) {
			var dis = CreateReflectionDisassembler(output, ctx, method);
			dis.DisassembleMethod(method, true);
		}

		public override void Decompile(FieldDef field, IDecompilerOutput output, DecompilationContext ctx) {
			var dis = CreateReflectionDisassembler(output, ctx, field);
			dis.DisassembleField(field, false);
		}

		public override void Decompile(PropertyDef property, IDecompilerOutput output, DecompilationContext ctx) {
			ReflectionDisassembler rd = CreateReflectionDisassembler(output, ctx, property);
			rd.DisassembleProperty(property, addLineSep: true);
			if (property.GetMethod != null) {
				output.WriteLine();
				rd.DisassembleMethod(property.GetMethod, true);
			}
			if (property.SetMethod != null) {
				output.WriteLine();
				rd.DisassembleMethod(property.SetMethod, true);
			}
			foreach (var m in property.OtherMethods) {
				output.WriteLine();
				rd.DisassembleMethod(m, true);
			}
		}

		public override void Decompile(EventDef ev, IDecompilerOutput output, DecompilationContext ctx) {
			ReflectionDisassembler rd = CreateReflectionDisassembler(output, ctx, ev);
			rd.DisassembleEvent(ev, addLineSep: true);
			if (ev.AddMethod != null) {
				output.WriteLine();
				rd.DisassembleMethod(ev.AddMethod, true);
			}
			if (ev.RemoveMethod != null) {
				output.WriteLine();
				rd.DisassembleMethod(ev.RemoveMethod, true);
			}
			foreach (var m in ev.OtherMethods) {
				output.WriteLine();
				rd.DisassembleMethod(m, true);
			}
		}

		public override void Decompile(TypeDef type, IDecompilerOutput output, DecompilationContext ctx) {
			var dis = CreateReflectionDisassembler(output, ctx, type);
			dis.DisassembleType(type, true);
		}

		public override void Decompile(AssemblyDef asm, IDecompilerOutput output, DecompilationContext ctx) {
			output.WriteLine("// " + asm.ManifestModule.Location, BoxedTextColor.Comment);
			PrintEntryPoint(asm.ManifestModule, output);
			output.WriteLine();

			ReflectionDisassembler rd = CreateReflectionDisassembler(output, ctx, asm.ManifestModule);
			rd.WriteAssemblyHeader(asm);
		}

		public override void Decompile(ModuleDef mod, IDecompilerOutput output, DecompilationContext ctx) {
			output.WriteLine("// " + mod.Location, BoxedTextColor.Comment);
			PrintEntryPoint(mod, output);
			output.WriteLine();

			ReflectionDisassembler rd = CreateReflectionDisassembler(output, ctx, mod);
			output.WriteLine();
			rd.WriteModuleHeader(mod);
		}

		protected override void TypeToString(IDecompilerOutput output, ITypeDefOrRef t, bool includeNamespace, IHasCustomAttribute attributeProvider = null) =>
			t.WriteTo(output, includeNamespace ? ILNameSyntax.TypeName : ILNameSyntax.ShortTypeName);

		public override void WriteToolTip(ITextColorWriter output, IMemberRef member, IHasCustomAttribute typeAttributes) {
			if (!(member is ITypeDefOrRef) && ILDecompilerUtils.Write(TextColorWriterToDecompilerOutput.Create(output), member))
				return;

			base.WriteToolTip(output, member, typeAttributes);
		}
	}
}
