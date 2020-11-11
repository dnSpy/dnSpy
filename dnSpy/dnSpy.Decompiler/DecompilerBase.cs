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
using System.Globalization;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.CSharp;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler {
	/// <summary>
	/// Base class for language-specific decompiler implementations.
	/// </summary>
	public abstract class DecompilerBase : IDecompiler {
		public abstract string ContentTypeString { get; }
		public abstract string GenericNameUI { get; }
		public abstract string UniqueNameUI { get; }
		public abstract double OrderUI { get; }
		public abstract Guid GenericGuid { get; }
		public abstract Guid UniqueGuid { get; }
		public abstract DecompilerSettingsBase Settings { get; }
		public abstract string FileExtension { get; }
		public virtual string? ProjectFileExtension => null;
		public virtual MetadataTextColorProvider MetadataTextColorProvider => CSharpMetadataTextColorProvider.Instance;

		public void WriteName(ITextColorWriter output, TypeDef type) =>
			FormatTypeName(TextColorWriterToDecompilerOutput.Create(output), type);
		public void WriteType(ITextColorWriter output, ITypeDefOrRef? type, bool includeNamespace, ParamDef? pd = null) =>
			TypeToString(TextColorWriterToDecompilerOutput.Create(output), type, includeNamespace, pd);
		public void WriteName(ITextColorWriter output, PropertyDef property, bool? isIndexer) =>
			FormatPropertyName(TextColorWriterToDecompilerOutput.Create(output), property, isIndexer);
		public virtual void Decompile(MethodDef method, IDecompilerOutput output, DecompilationContext ctx) =>
			this.WriteCommentLine(output, TypeToString(method.DeclaringType, true) + "." + method.Name);
		public virtual void Decompile(PropertyDef property, IDecompilerOutput output, DecompilationContext ctx) =>
			this.WriteCommentLine(output, TypeToString(property.DeclaringType, true) + "." + property.Name);
		public virtual void Decompile(FieldDef field, IDecompilerOutput output, DecompilationContext ctx) =>
			this.WriteCommentLine(output, TypeToString(field.DeclaringType, true) + "." + field.Name);
		public virtual void Decompile(EventDef ev, IDecompilerOutput output, DecompilationContext ctx) =>
			this.WriteCommentLine(output, TypeToString(ev.DeclaringType, true) + "." + ev.Name);
		public virtual void Decompile(TypeDef type, IDecompilerOutput output, DecompilationContext ctx) =>
			this.WriteCommentLine(output, TypeToString(type, true));

		public virtual void DecompileNamespace(string @namespace, IEnumerable<TypeDef> types, IDecompilerOutput output, DecompilationContext ctx) {
			this.WriteCommentLine(output, string.IsNullOrEmpty(@namespace) ? string.Empty : IdentifierEscaper.Escape(@namespace));
			this.WriteCommentLine(output, string.Empty);
			this.WriteCommentLine(output, dnSpy_Decompiler_Resources.Decompile_Namespace_Types);
			this.WriteCommentLine(output, string.Empty);
			foreach (var type in types) {
				WriteCommentBegin(output, true);
				output.Write(IdentifierEscaper.Escape(type.Name), type, DecompilerReferenceFlags.None, BoxedTextColor.Comment);
				WriteCommentEnd(output, true);
				output.WriteLine();
			}
		}

		static IPEImage? TryGetPEImage(ModuleDef mod) => (mod as ModuleDefMD)?.Metadata.PEImage;

		protected void WriteAssembly(AssemblyDef asm, IDecompilerOutput output, DecompilationContext ctx) {
			DecompileInternal(asm, output, ctx);
			output.WriteLine();
			PrintEntryPoint(asm.ManifestModule, output);
			var peImage = TryGetPEImage(asm.ManifestModule);
			if (peImage is not null)
				WriteTimestampComment(output, peImage);
			output.WriteLine();
		}

		void WriteTimestampComment(IDecompilerOutput output, IPEImage peImage) {
			WriteCommentBegin(output, true);
			output.Write(dnSpy_Decompiler_Resources.Decompile_Timestamp, BoxedTextColor.Comment);
			output.Write(" ", BoxedTextColor.Comment);
			uint ts = peImage.ImageNTHeaders.FileHeader.TimeDateStamp;
			if ((int)ts > 0) {
				var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ts).ToLocalTime();
				var dateString = date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat);
				output.Write($"{ts:X8} ({dateString})", BoxedTextColor.Comment);
			}
			else {
				output.Write(dnSpy_Decompiler_Resources.UnknownValue, BoxedTextColor.Comment);
				output.Write($" ({ts:X8})", BoxedTextColor.Comment);
			}
			WriteCommentEnd(output, true);
			output.WriteLine();
		}

		protected void WriteModule(ModuleDef mod, IDecompilerOutput output, DecompilationContext ctx) {
			DecompileInternal(mod, output, ctx);
			output.WriteLine();
			if (mod.Types.Count > 0) {
				WriteCommentBegin(output, true);
				output.Write(dnSpy_Decompiler_Resources.Decompile_GlobalType + " ", BoxedTextColor.Comment);
				output.Write(IdentifierEscaper.Escape(mod.GlobalType.FullName), mod.GlobalType, DecompilerReferenceFlags.None, BoxedTextColor.Comment);
				output.WriteLine();
			}
			PrintEntryPoint(mod, output);
			this.WriteCommentLine(output, dnSpy_Decompiler_Resources.Decompile_Architecture + " " + GetPlatformDisplayName(mod));
			if (!mod.IsILOnly) {
				this.WriteCommentLine(output, dnSpy_Decompiler_Resources.Decompile_ThisAssemblyContainsUnmanagedCode);
			}
			string? runtimeName = GetRuntimeDisplayName(mod);
			if (runtimeName is not null) {
				this.WriteCommentLine(output, dnSpy_Decompiler_Resources.Decompile_Runtime + " " + runtimeName);
			}
			var peImage = TryGetPEImage(mod);
			if (peImage is not null)
				WriteTimestampComment(output, peImage);
			output.WriteLine();
		}

		public virtual void Decompile(AssemblyDef asm, IDecompilerOutput output, DecompilationContext ctx) => DecompileInternal(asm, output, ctx);
		public virtual void Decompile(ModuleDef mod, IDecompilerOutput output, DecompilationContext ctx) => DecompileInternal(mod, output, ctx);

		void DecompileInternal(AssemblyDef asm, IDecompilerOutput output, DecompilationContext ctx) {
			this.WriteCommentLine(output, asm.ManifestModule.Location);
			if (asm.IsContentTypeWindowsRuntime)
				this.WriteCommentLine(output, asm.Name + " [WinRT]");
			else
				this.WriteCommentLine(output, asm.FullName);
		}

		void DecompileInternal(ModuleDef mod, IDecompilerOutput output, DecompilationContext ctx) {
			this.WriteCommentLine(output, mod.Location);
			this.WriteCommentLine(output, mod.Name);
		}

		protected void PrintEntryPoint(ModuleDef mod, IDecompilerOutput output) {
			var ep = GetEntryPoint(mod);
			if (ep is uint)
				this.WriteCommentLine(output, string.Format(dnSpy_Decompiler_Resources.Decompile_NativeEntryPoint, (uint)ep));
			else if (ep is MethodDef epMethod) {
				WriteCommentBegin(output, true);
				output.Write(dnSpy_Decompiler_Resources.Decompile_EntryPoint + " ", BoxedTextColor.Comment);
				if (epMethod.DeclaringType is not null) {
					output.Write(IdentifierEscaper.Escape(epMethod.DeclaringType.FullName), epMethod.DeclaringType, DecompilerReferenceFlags.None, BoxedTextColor.Comment);
					output.Write(".", BoxedTextColor.Comment);
				}
				output.Write(IdentifierEscaper.Escape(epMethod.Name), epMethod, DecompilerReferenceFlags.None, BoxedTextColor.Comment);
				WriteCommentEnd(output, true);
				output.WriteLine();
			}
		}

		object? GetEntryPoint(ModuleDef? module) {
			int maxIters = 1;
			for (int i = 0; module is not null && i < maxIters; i++) {
				var rva = module.NativeEntryPoint;
				if (rva != 0)
					return (uint)rva;

				var manEp = module.ManagedEntryPoint;
				if (manEp is MethodDef ep)
					return ep;

				var file = manEp as FileDef;
				if (file is null)
					return null;

				var asm = module.Assembly;
				if (asm is null)
					return null;
				maxIters = asm.Modules.Count;

				module = asm.Modules.FirstOrDefault(m => File.Exists(m.Location) && StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(m.Location), file.Name));
			}

			return null;
		}

		protected void WriteCommentLineDeclaringType(IDecompilerOutput output, IMemberDef member) {
			WriteCommentBegin(output, true);
			output.Write(TypeToString(member.DeclaringType, includeNamespace: true), member.DeclaringType, DecompilerReferenceFlags.None, BoxedTextColor.Comment);
			WriteCommentEnd(output, true);
			output.WriteLine();
		}

		public virtual void WriteCommentBegin(IDecompilerOutput output, bool addSpace) {
			if (addSpace)
				output.Write("// ", BoxedTextColor.Comment);
			else
				output.Write("//", BoxedTextColor.Comment);
		}

		public virtual void WriteCommentEnd(IDecompilerOutput output, bool addSpace) { }

		string TypeToString(ITypeDefOrRef? type, bool includeNamespace, IHasCustomAttribute? typeAttributes = null) {
			var output = new StringBuilderDecompilerOutput();
			TypeToString(output, type, includeNamespace, typeAttributes);
			return output.ToString();
		}

		protected virtual void TypeToString(IDecompilerOutput output, ITypeDefOrRef? type, bool includeNamespace, IHasCustomAttribute? typeAttributes = null) {
			if (type is null)
				return;
			if (includeNamespace)
				output.Write(IdentifierEscaper.Escape(type.FullName), MetadataTextColorProvider.GetColor(type));
			else
				output.Write(IdentifierEscaper.Escape(type.Name), MetadataTextColorProvider.GetColor(type));
		}

		protected const FormatterOptions DefaultFormatterOptions = FormatterOptions.Default | FormatterOptions.ShowParameterLiteralValues;
		public virtual void WriteToolTip(ITextColorWriter output, IMemberRef member, IHasCustomAttribute? typeAttributes) =>
			new CSharpFormatter(output, DefaultFormatterOptions, null).WriteToolTip(member);
		public virtual void WriteToolTip(ITextColorWriter output, ISourceVariable variable) =>
			new CSharpFormatter(output, DefaultFormatterOptions, null).WriteToolTip(variable);
		public virtual void WriteNamespaceToolTip(ITextColorWriter output, string? @namespace) =>
			new CSharpFormatter(output, DefaultFormatterOptions, null).WriteNamespaceToolTip(@namespace);
		public virtual void Write(ITextColorWriter output, IMemberRef member, FormatterOptions flags) =>
			new CSharpFormatter(output, flags, null).Write(member);

		protected virtual void FormatPropertyName(IDecompilerOutput output, PropertyDef property, bool? isIndexer = null) {
			if (property is null)
				throw new ArgumentNullException(nameof(property));
			output.Write(IdentifierEscaper.Escape(property.Name), MetadataTextColorProvider.GetColor(property));
		}

		protected virtual void FormatTypeName(IDecompilerOutput output, TypeDef type) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			output.Write(IdentifierEscaper.Escape(type.Name), MetadataTextColorProvider.GetColor(type));
		}

		public virtual bool ShowMember(IMemberRef member) => true;
		protected static string GetPlatformDisplayName(ModuleDef module) => TargetFrameworkUtils.GetArchString(module);
		protected static string? GetRuntimeDisplayName(ModuleDef module) => TargetFrameworkInfo.Create(module).ToString();
		public virtual bool CanDecompile(DecompilationType decompilationType) => false;

		public virtual void Decompile(DecompilationType decompilationType, object data) => throw new NotImplementedException();
	}
}
