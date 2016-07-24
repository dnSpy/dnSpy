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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Languages.ILSpy.Core.Text;
using dnSpy.Languages.ILSpy.Settings;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.ILAst;

namespace dnSpy.Languages.ILSpy.ILAst {
	sealed class LanguageProvider : ILanguageProvider {
		readonly LanguageSettingsManager languageSettingsManager;

		// Keep the default ctor. It's used by dnSpy.Console.exe
		public LanguageProvider()
			: this(LanguageSettingsManager.__Instance_DONT_USE) {
		}

		public LanguageProvider(LanguageSettingsManager languageSettingsManager) {
			Debug.Assert(languageSettingsManager != null);
			if (languageSettingsManager == null)
				throw new ArgumentNullException();
			this.languageSettingsManager = languageSettingsManager;
		}

		public IEnumerable<ILanguage> Languages {
			get {
#if DEBUG
				foreach (var l in ILAstLanguage.GetDebugLanguages(languageSettingsManager))
					yield return l;
#endif
				yield break;
			}
		}
	}

#if DEBUG
	/// <summary>
	/// Represents the ILAst "language" used for debugging purposes.
	/// </summary>
	sealed class ILAstLanguage : Language {
		string uniqueNameUI;
		Guid uniqueGuid;
		bool inlineVariables = true;
		ILAstOptimizationStep? abortBeforeStep;

		public override DecompilerSettingsBase Settings { get; }

		ILAstLanguage(ILAstLanguageDecompilerSettings langSettings, double orderUI) {
			this.Settings = langSettings;
			this.OrderUI = orderUI;
		}

		public override double OrderUI { get; }
		public override string ContentTypeString => ContentTypesInternal.ILAstILSpy;
		public override string GenericNameUI => "ILAst";
		public override string UniqueNameUI => uniqueNameUI;
		public override Guid GenericGuid => LanguageConstants.LANGUAGE_ILAST_ILSPY;
		public override Guid UniqueGuid => uniqueGuid;

		public override void Decompile(MethodDef method, IDecompilerOutput output, DecompilationContext ctx) {
			WriteCommentBegin(output, true);
			output.Write("Method: ", BoxedOutputColor.Comment);
			output.Write(IdentifierEscaper.Escape(method.FullName), method, DecompilerReferenceFlags.Definition, BoxedOutputColor.Comment);
			WriteCommentEnd(output, true);
			output.WriteLine();

			if (!method.HasBody) {
				return;
			}

			StartKeywordBlock(output, ".body", method);

			ILAstBuilder astBuilder = new ILAstBuilder();
			ILBlock ilMethod = new ILBlock();
			DecompilerContext context = new DecompilerContext(method.Module) { CurrentType = method.DeclaringType, CurrentMethod = method };
			ilMethod.Body = astBuilder.Build(method, inlineVariables, context);

			if (abortBeforeStep != null) {
				new ILAstOptimizer().Optimize(context, ilMethod, abortBeforeStep.Value);
			}

			if (context.CurrentMethodIsAsync) {
				output.Write("async", BoxedOutputColor.Keyword);
				output.Write("/", BoxedOutputColor.Punctuation);
				output.WriteLine("await", BoxedOutputColor.Keyword);
			}

			var allVariables = ilMethod.GetSelfAndChildrenRecursive<ILExpression>().Select(e => e.Operand as ILVariable)
				.Where(v => v != null && !v.IsParameter).Distinct();
			foreach (ILVariable v in allVariables) {
				output.Write(IdentifierEscaper.Escape(v.Name), v, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, v.IsParameter ? BoxedOutputColor.Parameter : BoxedOutputColor.Local);
				if (v.Type != null) {
					output.Write(" ", BoxedOutputColor.Text);
					output.Write(":", BoxedOutputColor.Punctuation);
					output.Write(" ", BoxedOutputColor.Text);
					if (v.IsPinned) {
						output.Write("pinned", BoxedOutputColor.Keyword);
						output.Write(" ", BoxedOutputColor.Text);
					}
					v.Type.WriteTo(output, ILNameSyntax.ShortTypeName);
				}
				if (v.GeneratedByDecompiler) {
					output.Write(" ", BoxedOutputColor.Text);
					output.Write("[", BoxedOutputColor.Punctuation);
					output.Write("generated", BoxedOutputColor.Keyword);
					output.Write("]", BoxedOutputColor.Punctuation);
				}
				output.WriteLine();
			}

			var builder = new MethodDebugInfoBuilder(method);
			foreach (ILNode node in ilMethod.Body) {
				node.WriteTo(output, builder);
				if (!node.WritesNewLine)
					output.WriteLine();
			}
			output.AddDebugInfo(builder.Create());
			EndKeywordBlock(output);
		}

		void StartKeywordBlock(IDecompilerOutput output, string keyword, IMemberDef member) {
			output.Write(keyword, BoxedOutputColor.Keyword);
			output.Write(" ", BoxedOutputColor.Text);
			output.Write(IdentifierEscaper.Escape(member.Name), member, DecompilerReferenceFlags.Definition, OutputColorHelper.GetColor(member));
			output.Write(" ", BoxedOutputColor.Text);
			output.Write("{", BoxedOutputColor.Punctuation);
			output.WriteLine();
			output.Indent();
		}

		void EndKeywordBlock(IDecompilerOutput output) {
			output.Unindent();
			output.Write("}", BoxedOutputColor.Punctuation);
			output.WriteLine();
		}

		public override void Decompile(EventDef ev, IDecompilerOutput output, DecompilationContext ctx) {
			StartKeywordBlock(output, ".event", ev);

			if (ev.AddMethod != null) {
				StartKeywordBlock(output, ".add", ev.AddMethod);
				EndKeywordBlock(output);
			}

			if (ev.InvokeMethod != null) {
				StartKeywordBlock(output, ".invoke", ev.InvokeMethod);
				EndKeywordBlock(output);
			}

			if (ev.RemoveMethod != null) {
				StartKeywordBlock(output, ".remove", ev.RemoveMethod);
				EndKeywordBlock(output);
			}

			EndKeywordBlock(output);
		}

		public override void Decompile(FieldDef field, IDecompilerOutput output, DecompilationContext ctx) {
			output.Write(IdentifierEscaper.Escape(field.FieldType.GetFullName()), field.FieldType.ToTypeDefOrRef(), DecompilerReferenceFlags.None, OutputColorHelper.GetColor(field.FieldType));
			output.Write(" ", BoxedOutputColor.Text);
			output.Write(IdentifierEscaper.Escape(field.Name), field, DecompilerReferenceFlags.Definition, OutputColorHelper.GetColor(field));
			var c = field.Constant;
			if (c != null) {
				output.Write(" ", BoxedOutputColor.Text);
				output.Write("=", BoxedOutputColor.Operator);
				output.Write(" ", BoxedOutputColor.Text);
				if (c.Value == null)
					output.Write("null", BoxedOutputColor.Keyword);
				else {
					switch (c.Type) {
					case ElementType.Boolean:
						if (c.Value is bool)
							output.Write((bool)c.Value ? "true" : "false", BoxedOutputColor.Keyword);
						else
							goto default;
						break;

					case ElementType.Char:
						output.Write($"'{c.Value}'", BoxedOutputColor.Char);
						break;

					case ElementType.I1:
					case ElementType.U1:
					case ElementType.I2:
					case ElementType.U2:
					case ElementType.I4:
					case ElementType.U4:
					case ElementType.I8:
					case ElementType.U8:
					case ElementType.R4:
					case ElementType.R8:
					case ElementType.I:
					case ElementType.U:
						output.Write($"{c.Value}", BoxedOutputColor.Number);
						break;

					case ElementType.String:
						output.Write($"{c.Value}", BoxedOutputColor.String);
						break;

					default:
						output.Write($"{c.Value}", BoxedOutputColor.Text);
						break;
					}
				}
			}
		}

		public override void Decompile(PropertyDef property, IDecompilerOutput output, DecompilationContext ctx) {
			StartKeywordBlock(output, ".property", property);

			foreach (var getter in property.GetMethods) {
				StartKeywordBlock(output, ".get", getter);
				EndKeywordBlock(output);
			}

			foreach (var setter in property.SetMethods) {
				StartKeywordBlock(output, ".set", setter);
				EndKeywordBlock(output);
			}

			foreach (var other in property.OtherMethods) {
				StartKeywordBlock(output, ".other", other);
				EndKeywordBlock(output);
			}

			EndKeywordBlock(output);
		}

		public override void Decompile(TypeDef type, IDecompilerOutput output, DecompilationContext ctx) {
			this.WriteCommentLine(output, $"Type: {type.FullName}");
			if (type.BaseType != null) {
				WriteCommentBegin(output, true);
				output.Write("Base type: ", BoxedOutputColor.Comment);
				output.Write(IdentifierEscaper.Escape(type.BaseType.FullName), type.BaseType, DecompilerReferenceFlags.None, BoxedOutputColor.Comment);
				WriteCommentEnd(output, true);
				output.WriteLine();
			}
			foreach (var nested in type.NestedTypes) {
				Decompile(nested, output, ctx);
				output.WriteLine();
			}

			foreach (var field in type.Fields) {
				Decompile(field, output, ctx);
				output.WriteLine();
			}

			foreach (var property in type.Properties) {
				Decompile(property, output, ctx);
				output.WriteLine();
			}

			foreach (var @event in type.Events) {
				Decompile(@event, output, ctx);
				output.WriteLine();
			}

			foreach (var method in type.Methods) {
				Decompile(method, output, ctx);
				output.WriteLine();
			}
		}

		internal static IEnumerable<ILAstLanguage> GetDebugLanguages(LanguageSettingsManager languageSettingsManager) {
			double orderUI = LanguageConstants.ILAST_ILSPY_DEBUG_ORDERUI;
			uint id = 0x64A926A5;
			yield return new ILAstLanguage(languageSettingsManager.ILAstLanguageDecompilerSettings, orderUI++) {
				uniqueNameUI = "ILAst (unoptimized)",
				uniqueGuid = new Guid($"CB470049-6AFB-4BDB-93DC-1BB9{id++:X8}"),
				inlineVariables = false
			};
			string nextName = "ILAst (variable splitting)";
			foreach (ILAstOptimizationStep step in Enum.GetValues(typeof(ILAstOptimizationStep))) {
				yield return new ILAstLanguage(languageSettingsManager.ILAstLanguageDecompilerSettings, orderUI++) {
					uniqueNameUI = nextName,
					uniqueGuid = new Guid($"CB470049-6AFB-4BDB-93DC-1BB9{id++:X8}"),
					abortBeforeStep = step
				};
				nextName = "ILAst (after " + step + ")";
			}
		}

		public override string FileExtension => ".il";

		protected override void TypeToString(IDecompilerOutput output, ITypeDefOrRef t, bool includeNamespace, IHasCustomAttribute attributeProvider = null) =>
			t.WriteTo(output, includeNamespace ? ILNameSyntax.TypeName : ILNameSyntax.ShortTypeName);
	}
#endif
}
