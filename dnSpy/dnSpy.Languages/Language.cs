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
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Languages.CSharp;
using dnSpy.Languages.Properties;

namespace dnSpy.Languages {
	/// <summary>
	/// Base class for language-specific decompiler implementations.
	/// </summary>
	public abstract class Language : ILanguage {
		public abstract string ContentTypeString { get; }
		public abstract string GenericNameUI { get; }
		public abstract string UniqueNameUI { get; }
		public abstract double OrderUI { get; }
		public abstract Guid GenericGuid { get; }
		public abstract Guid UniqueGuid { get; }
		public abstract DecompilerSettingsBase Settings { get; }
		public abstract string FileExtension { get; }
		public virtual string ProjectFileExtension => null;

		public void WriteName(IOutputColorWriter output, TypeDef type) =>
			FormatTypeName(OutputColorWriterToDecompilerOutput.Create(output), type);
		public void WriteType(IOutputColorWriter output, ITypeDefOrRef type, bool includeNamespace, ParamDef pd = null) =>
			TypeToString(OutputColorWriterToDecompilerOutput.Create(output), type, includeNamespace, pd);
		public void WriteName(IOutputColorWriter output, PropertyDef property, bool? isIndexer) =>
			FormatPropertyName(OutputColorWriterToDecompilerOutput.Create(output), property, isIndexer);
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
			this.WriteCommentLine(output, dnSpy_Languages_Resources.Decompile_Namespace_Types);
			this.WriteCommentLine(output, string.Empty);
			foreach (var type in types) {
				this.WriteCommentBegin(output, true);
				output.Write(IdentifierEscaper.Escape(type.Name), type, DecompilerReferenceFlags.None, BoxedOutputColor.Comment);
				this.WriteCommentEnd(output, true);
				output.WriteLine();
			}
		}

		static IPEImage TryGetPEImage(ModuleDef mod) {
			var m = mod as ModuleDefMD;
			return m == null ? null : m.MetaData.PEImage;
		}

		protected void WriteAssembly(AssemblyDef asm, IDecompilerOutput output, DecompilationContext ctx) {
			DecompileInternal(asm, output, ctx);
			output.WriteLine();
			this.PrintEntryPoint(asm.ManifestModule, output);
			var peImage = TryGetPEImage(asm.ManifestModule);
			if (peImage != null) {
				this.WriteCommentBegin(output, true);
				uint ts = peImage.ImageNTHeaders.FileHeader.TimeDateStamp;
				var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ts);
				var dateString = date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat);
				output.Write(string.Format(dnSpy_Languages_Resources.Decompile_Timestamp, ts, dateString), BoxedOutputColor.Comment);
				this.WriteCommentEnd(output, true);
				output.WriteLine();
			}
			output.WriteLine();
		}

		protected void WriteModule(ModuleDef mod, IDecompilerOutput output, DecompilationContext ctx) {
			DecompileInternal(mod, output, ctx);
			output.WriteLine();
			if (mod.Types.Count > 0) {
				this.WriteCommentBegin(output, true);
				output.Write(dnSpy_Languages_Resources.Decompile_GlobalType + " ", BoxedOutputColor.Comment);
				output.Write(IdentifierEscaper.Escape(mod.GlobalType.FullName), mod.GlobalType, DecompilerReferenceFlags.None, BoxedOutputColor.Comment);
				output.WriteLine();
			}
			this.PrintEntryPoint(mod, output);
			this.WriteCommentLine(output, dnSpy_Languages_Resources.Decompile_Architecture + " " + GetPlatformDisplayName(mod));
			if (!mod.IsILOnly) {
				this.WriteCommentLine(output, dnSpy_Languages_Resources.Decompile_ThisAssemblyContainsUnmanagedCode);
			}
			string runtimeName = GetRuntimeDisplayName(mod);
			if (runtimeName != null) {
				this.WriteCommentLine(output, dnSpy_Languages_Resources.Decompile_Runtime + " " + runtimeName);
			}
			var peImage = TryGetPEImage(mod);
			if (peImage != null) {
				this.WriteCommentBegin(output, true);
				uint ts = peImage.ImageNTHeaders.FileHeader.TimeDateStamp;
				var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ts);
				var dateString = date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat);
				output.Write(string.Format(dnSpy_Languages_Resources.Decompile_Timestamp, ts, dateString), BoxedOutputColor.Comment);
				this.WriteCommentEnd(output, true);
				output.WriteLine();
			}
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
				this.WriteCommentLine(output, string.Format(dnSpy_Languages_Resources.Decompile_NativeEntryPoint, (uint)ep));
			else if (ep is MethodDef) {
				var epMethod = (MethodDef)ep;
				WriteCommentBegin(output, true);
				output.Write(dnSpy_Languages_Resources.Decompile_EntryPoint + " ", BoxedOutputColor.Comment);
				if (epMethod.DeclaringType != null) {
					output.Write(IdentifierEscaper.Escape(epMethod.DeclaringType.FullName), epMethod.DeclaringType, DecompilerReferenceFlags.None, BoxedOutputColor.Comment);
					output.Write(".", BoxedOutputColor.Comment);
				}
				output.Write(IdentifierEscaper.Escape(epMethod.Name), epMethod, DecompilerReferenceFlags.None, BoxedOutputColor.Comment);
				WriteCommentEnd(output, true);
				output.WriteLine();
			}
		}

		object GetEntryPoint(ModuleDef module) {
			int maxIters = 1;
			for (int i = 0; module != null && i < maxIters; i++) {
				var rva = module.NativeEntryPoint;
				if (rva != 0)
					return (uint)rva;

				var manEp = module.ManagedEntryPoint;
				var ep = manEp as MethodDef;
				if (ep != null)
					return ep;

				var file = manEp as FileDef;
				if (file == null)
					return null;

				var asm = module.Assembly;
				if (asm == null)
					return null;
				maxIters = asm.Modules.Count;

				module = asm.Modules.FirstOrDefault(m => File.Exists(m.Location) && StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(m.Location), file.Name));
			}

			return null;
		}

		protected void WriteCommentLineDeclaringType(IDecompilerOutput output, IMemberDef member) {
			WriteCommentBegin(output, true);
			output.Write(TypeToString(member.DeclaringType, includeNamespace: true), member.DeclaringType, DecompilerReferenceFlags.None, BoxedOutputColor.Comment);
			WriteCommentEnd(output, true);
			output.WriteLine();
		}

		public virtual void WriteCommentBegin(IDecompilerOutput output, bool addSpace) {
			if (addSpace)
				output.Write("// ", BoxedOutputColor.Comment);
			else
				output.Write("//", BoxedOutputColor.Comment);
		}

		public virtual void WriteCommentEnd(IDecompilerOutput output, bool addSpace) { }

		string TypeToString(ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null) {
			var output = new StringBuilderDecompilerOutput();
			TypeToString(output, type, includeNamespace, typeAttributes);
			return output.ToString();
		}

		protected virtual void TypeToString(IDecompilerOutput output, ITypeDefOrRef type, bool includeNamespace, IHasCustomAttribute typeAttributes = null) {
			if (type == null)
				return;
			if (includeNamespace)
				output.Write(IdentifierEscaper.Escape(type.FullName), OutputColorHelper.GetColor(type));
			else
				output.Write(IdentifierEscaper.Escape(type.Name), OutputColorHelper.GetColor(type));
		}

		public virtual void WriteToolTip(IOutputColorWriter output, IMemberRef member, IHasCustomAttribute typeAttributes) =>
			new SimpleCSharpPrinter(output, SimplePrinterFlags.Default).WriteToolTip(member);
		public virtual void WriteToolTip(IOutputColorWriter output, IVariable variable, string name) =>
			new SimpleCSharpPrinter(output, SimplePrinterFlags.Default).WriteToolTip(variable, name);
		public virtual void Write(IOutputColorWriter output, IMemberRef member, SimplePrinterFlags flags) =>
			new SimpleCSharpPrinter(output, flags).Write(member);

		protected static string GetName(IVariable variable, string name) {
			if (!string.IsNullOrWhiteSpace(name))
				return name;
			var n = variable.Name;
			if (!string.IsNullOrWhiteSpace(n))
				return n;
			return $"#{variable.Index}";
		}

		protected virtual void FormatPropertyName(IDecompilerOutput output, PropertyDef property, bool? isIndexer = null) {
			if (property == null)
				throw new ArgumentNullException(nameof(property));
			output.Write(IdentifierEscaper.Escape(property.Name), OutputColorHelper.GetColor(property));
		}

		protected virtual void FormatTypeName(IDecompilerOutput output, TypeDef type) {
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			output.Write(IdentifierEscaper.Escape(type.Name), OutputColorHelper.GetColor(type));
		}

		public virtual bool ShowMember(IMemberRef member) => true;
		protected static string GetPlatformDisplayName(ModuleDef module) => TargetFrameworkUtils.GetArchString(module);
		protected static string GetRuntimeDisplayName(ModuleDef module) => TargetFrameworkInfo.Create(module).ToString();
		public virtual bool CanDecompile(DecompilationType decompilationType) => false;

		public virtual void Decompile(DecompilationType decompilationType, object data) {
			throw new NotImplementedException();
		}
	}
}
