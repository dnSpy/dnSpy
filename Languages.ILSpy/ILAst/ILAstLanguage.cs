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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;
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

	[Export(typeof(ILanguageCreator))]
	sealed class MyLanguageCreator : ILanguageCreator {
		readonly LanguageSettingsManager languageSettingsManager;

		[ImportingConstructor]
		MyLanguageCreator(LanguageSettingsManager languageSettingsManager) {
			this.languageSettingsManager = languageSettingsManager;
		}

		public IEnumerable<ILanguage> Create() {
			return new LanguageProvider(languageSettingsManager).Languages;
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

		public override IDecompilerSettings Settings {
			get { return langSettings; }
		}
		readonly IDecompilerSettings langSettings;

		ILAstLanguage(ILAstLanguageDecompilerSettings langSettings, double orderUI) {
			this.langSettings = langSettings;
			this.orderUI = orderUI;
		}

		public override double OrderUI {
			get { return orderUI; }
		}
		readonly double orderUI;

		public override string GenericNameUI {
			get { return "ILAst"; }
		}

		public override string UniqueNameUI {
			get { return uniqueNameUI; }
		}

		public override Guid GenericGuid {
			get { return LanguageConstants.LANGUAGE_ILAST_ILSPY; }
		}

		public override Guid UniqueGuid {
			get { return uniqueGuid; }
		}

		public override void Decompile(MethodDef method, ITextOutput output, DecompilationContext ctx) {
			WriteCommentBegin(output, true);
			output.Write("Method: ", TextTokenKind.Comment);
			output.WriteDefinition(IdentifierEscaper.Escape(method.FullName), method, TextTokenKind.Comment, false);
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
				output.Write("async", TextTokenKind.Keyword);
				output.Write("/", TextTokenKind.Operator);
				output.WriteLine("await", TextTokenKind.Keyword);
			}

			var allVariables = ilMethod.GetSelfAndChildrenRecursive<ILExpression>().Select(e => e.Operand as ILVariable)
				.Where(v => v != null && !v.IsParameter).Distinct();
			foreach (ILVariable v in allVariables) {
				output.WriteDefinition(IdentifierEscaper.Escape(v.Name), v, v.IsParameter ? TextTokenKind.Parameter : TextTokenKind.Local);
				if (v.Type != null) {
					output.WriteSpace();
					output.Write(":", TextTokenKind.Operator);
					output.WriteSpace();
					if (v.IsPinned) {
						output.Write("pinned", TextTokenKind.Keyword);
						output.WriteSpace();
					}
					v.Type.WriteTo(output, ILNameSyntax.ShortTypeName);
				}
				if (v.GeneratedByDecompiler) {
					output.WriteSpace();
					output.Write("[", TextTokenKind.Operator);
					output.Write("generated", TextTokenKind.Keyword);
					output.Write("]", TextTokenKind.Operator);
				}
				output.WriteLine();
			}

			var memberMapping = new MemberMapping(method);
			foreach (ILNode node in ilMethod.Body) {
				node.WriteTo(output, memberMapping);
				if (!node.WritesNewLine)
					output.WriteLine();
			}
			output.AddDebugSymbols(memberMapping);
			EndKeywordBlock(output);
		}

		void StartKeywordBlock(ITextOutput output, string keyword, IMemberDef member) {
			output.Write(keyword, TextTokenKind.Keyword);
			output.WriteSpace();
			output.WriteDefinition(IdentifierEscaper.Escape(member.Name), member, TextTokenKindUtils.GetTextTokenType(member), false);
			output.WriteSpace();
			output.WriteLeftBrace();
			output.WriteLine();
			output.Indent();
		}

		void EndKeywordBlock(ITextOutput output) {
			output.Unindent();
			output.WriteRightBrace();
			output.WriteLine();
		}

		public override void Decompile(EventDef ev, ITextOutput output, DecompilationContext ctx) {
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

		public override void Decompile(FieldDef field, ITextOutput output, DecompilationContext ctx) {
			output.WriteReference(IdentifierEscaper.Escape(field.FieldType.GetFullName()), field.FieldType.ToTypeDefOrRef(), TextTokenKindUtils.GetTextTokenType(field.FieldType));
			output.WriteSpace();
			output.WriteDefinition(IdentifierEscaper.Escape(field.Name), field, TextTokenKindUtils.GetTextTokenType(field), false);
			var c = field.Constant;
			if (c != null) {
				output.WriteSpace();
				output.Write("=", TextTokenKind.Operator);
				output.WriteSpace();
				if (c.Value == null)
					output.Write("null", TextTokenKind.Keyword);
				else {
					switch (c.Type) {
					case ElementType.Boolean:
						if (c.Value is bool)
							output.Write((bool)c.Value ? "true" : "false", TextTokenKind.Keyword);
						else
							goto default;
						break;

					case ElementType.Char:
						output.Write(string.Format("'{0}'", c.Value), TextTokenKind.Char);
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
						output.Write(string.Format("{0}", c.Value), TextTokenKind.Number);
						break;

					case ElementType.String:
						output.Write(string.Format("{0}", c.Value), TextTokenKind.String);
						break;

					default:
						output.Write(string.Format("{0}", c.Value), TextTokenKind.Text);
						break;
					}
				}
			}
		}

		public override void Decompile(PropertyDef property, ITextOutput output, DecompilationContext ctx) {
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

		public override void Decompile(TypeDef type, ITextOutput output, DecompilationContext ctx) {
			this.WriteCommentLine(output, string.Format("Type: {0}", type.FullName));
			if (type.BaseType != null) {
				WriteCommentBegin(output, true);
				output.Write("Base type: ", TextTokenKind.Comment);
				output.WriteReference(IdentifierEscaper.Escape(type.BaseType.FullName), type.BaseType, TextTokenKind.Comment);
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
				uniqueGuid = new Guid(string.Format("CB470049-6AFB-4BDB-93DC-1BB9{0:X8}", id++)),
				inlineVariables = false
			};
			string nextName = "ILAst (variable splitting)";
			foreach (ILAstOptimizationStep step in Enum.GetValues(typeof(ILAstOptimizationStep))) {
				yield return new ILAstLanguage(languageSettingsManager.ILAstLanguageDecompilerSettings, orderUI++) {
					uniqueNameUI = nextName,
					uniqueGuid = new Guid(string.Format("CB470049-6AFB-4BDB-93DC-1BB9{0:X8}", id++)),
					abortBeforeStep = step
				};
				nextName = "ILAst (after " + step + ")";
			}
		}

		public override string FileExtension {
			get {
				return ".il";
			}
		}

		protected override void TypeToString(ITextOutput output, ITypeDefOrRef t, bool includeNamespace, IHasCustomAttribute attributeProvider = null) {
			t.WriteTo(output, includeNamespace ? ILNameSyntax.TypeName : ILNameSyntax.ShortTypeName);
		}
	}
#endif
}
