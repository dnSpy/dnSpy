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
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.ILSpy.Core.Settings;
using dnSpy.Decompiler.ILSpy.Core.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.ILAst;

namespace dnSpy.Decompiler.ILSpy.Core.ILAst {
	sealed class DecompilerProvider : IDecompilerProvider {
		readonly DecompilerSettingsService decompilerSettingsService;

		// Keep the default ctor. It's used by dnSpy.Console.exe
		public DecompilerProvider()
			: this(DecompilerSettingsService.__Instance_DONT_USE) {
		}

		public DecompilerProvider(DecompilerSettingsService decompilerSettingsService) {
			Debug2.Assert(decompilerSettingsService is not null);
			this.decompilerSettingsService = decompilerSettingsService ?? throw new ArgumentNullException(nameof(decompilerSettingsService));
		}

		public IEnumerable<IDecompiler> Create() {
#if DEBUG
			foreach (var l in ILAstDecompiler.GetDebugDecompilers(decompilerSettingsService))
				yield return l;
#endif
			yield break;
		}
	}

#if DEBUG
	/// <summary>
	/// Represents the ILAst "language" used for debugging purposes.
	/// </summary>
	sealed class ILAstDecompiler : DecompilerBase {
		readonly string uniqueNameUI;
		Guid uniqueGuid;
		bool inlineVariables = true;
		ILAstOptimizationStep? abortBeforeStep;

		public override DecompilerSettingsBase Settings { get; }
		const int settingsVersion = 1;

		ILAstDecompiler(ILAstDecompilerSettings langSettings, double orderUI, string uniqueNameUI) {
			Settings = langSettings;
			OrderUI = orderUI;
			this.uniqueNameUI = uniqueNameUI;
		}

		public override double OrderUI { get; }
		public override string ContentTypeString => ContentTypesInternal.ILAstILSpy;
		public override string GenericNameUI => "ILAst";
		public override string UniqueNameUI => uniqueNameUI;
		public override Guid GenericGuid => DecompilerConstants.LANGUAGE_ILAST_ILSPY;
		public override Guid UniqueGuid => uniqueGuid;

		public override void Decompile(MethodDef method, IDecompilerOutput output, DecompilationContext ctx) {
			WriteCommentBegin(output, true);
			output.Write("Method: ", BoxedTextColor.Comment);
			output.Write(IdentifierEscaper.Escape(method.FullName), method, DecompilerReferenceFlags.Definition, BoxedTextColor.Comment);
			WriteCommentEnd(output, true);
			output.WriteLine();

			if (!method.HasBody) {
				return;
			}

			var bodyInfo = StartKeywordBlock(output, ".body", method);

			ILAstBuilder astBuilder = new ILAstBuilder();
			ILBlock ilMethod = new ILBlock(CodeBracesRangeFlags.MethodBraces);
			DecompilerContext context = new DecompilerContext(settingsVersion, method.Module, MetadataTextColorProvider) {
				CurrentType = method.DeclaringType,
				CurrentMethod = method,
				CalculateILSpans = ctx.CalculateILSpans,
			};
			ilMethod.Body = astBuilder.Build(method, inlineVariables, context);

			var stateMachineKind = StateMachineKind.None;
			MethodDef? inlinedMethod = null;
			AsyncMethodDebugInfo? asyncInfo = null;
			string? compilerName = null;
			if (abortBeforeStep is not null) {
				var optimizer = new ILAstOptimizer();
				optimizer.Optimize(context, ilMethod, out stateMachineKind, out inlinedMethod, out asyncInfo, abortBeforeStep.Value);
				compilerName = optimizer.CompilerName;
			}

			if (context.CurrentMethodIsYieldReturn) {
				output.Write("yield", BoxedTextColor.Keyword);
				output.Write(" ", BoxedTextColor.Text);
				output.WriteLine("return", BoxedTextColor.Keyword);
			}
			if (context.CurrentMethodIsAsync) {
				output.Write("async", BoxedTextColor.Keyword);
				output.Write("/", BoxedTextColor.Punctuation);
				output.WriteLine("await", BoxedTextColor.Keyword);
			}

			var allVariables = ilMethod.GetSelfAndChildrenRecursive<ILExpression>().Select(e => e.Operand as ILVariable)
				.Where(v => v is not null && !v.IsParameter).Distinct();
			foreach (var v in allVariables) {
				Debug2.Assert(v is not null);
				output.Write(IdentifierEscaper.Escape(v.Name), v.GetTextReferenceObject(), DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, v.IsParameter ? BoxedTextColor.Parameter : BoxedTextColor.Local);
				if (v.Type is not null) {
					output.Write(" ", BoxedTextColor.Text);
					output.Write(":", BoxedTextColor.Punctuation);
					output.Write(" ", BoxedTextColor.Text);
					if (v.IsPinned) {
						output.Write("pinned", BoxedTextColor.Keyword);
						output.Write(" ", BoxedTextColor.Text);
					}
					v.Type.WriteTo(output, ILNameSyntax.ShortTypeName);
				}
				if (v.GeneratedByDecompiler) {
					output.Write(" ", BoxedTextColor.Text);
					var start = output.NextPosition;
					output.Write("[", BoxedTextColor.Punctuation);
					output.Write("generated", BoxedTextColor.Keyword);
					var end = output.NextPosition;
					output.Write("]", BoxedTextColor.Punctuation);
					output.AddBracePair(new TextSpan(start, 1), new TextSpan(end, 1), CodeBracesRangeFlags.SquareBrackets);
				}
				output.WriteLine();
			}

			var localVariables = new HashSet<ILVariable>(GetVariables(ilMethod));
			var builder = new MethodDebugInfoBuilder(settingsVersion, stateMachineKind, inlinedMethod ?? method, inlinedMethod is not null ? method : null, CreateSourceLocals(localVariables), CreateSourceParameters(localVariables), asyncInfo);
			builder.CompilerName = compilerName;
			foreach (ILNode node in ilMethod.Body) {
				node.WriteTo(output, builder);
				if (!node.WritesNewLine)
					output.WriteLine();
			}
			output.AddDebugInfo(builder.Create());
			EndKeywordBlock(output, bodyInfo, CodeBracesRangeFlags.MethodBraces, addLineSeparator: true);
		}

		IEnumerable<ILVariable> GetVariables(ILBlock ilMethod) {
			foreach (var n in ilMethod.GetSelfAndChildrenRecursive(new List<ILNode>())) {
				var expr = n as ILExpression;
				if (expr is not null) {
					var v = expr.Operand as ILVariable;
					if (v is not null)
						yield return v;
					continue;
				}
				var cb = n as ILTryCatchBlock.CatchBlockBase;
				if (cb is not null && cb.ExceptionVariable is not null)
					yield return cb.ExceptionVariable;
			}
		}

		readonly List<SourceLocal> sourceLocalsList = new List<SourceLocal>();
		SourceLocal[] CreateSourceLocals(HashSet<ILVariable> variables) {
			foreach (var v in variables) {
				if (v.IsParameter)
					continue;
				sourceLocalsList.Add(v.GetSourceLocal());
			}
			var array = sourceLocalsList.ToArray();
			sourceLocalsList.Clear();
			return array;
		}

		readonly List<SourceParameter> sourceParametersList = new List<SourceParameter>();
		SourceParameter[] CreateSourceParameters(HashSet<ILVariable> variables) {
			foreach (var v in variables) {
				if (!v.IsParameter)
					continue;
				sourceParametersList.Add(v.GetSourceParameter());
			}
			var array = sourceParametersList.ToArray();
			sourceParametersList.Clear();
			return array;
		}

		struct BraceInfo {
			public int Start { get; }
			public BraceInfo(int start) => Start = start;
		}

		BraceInfo StartKeywordBlock(IDecompilerOutput output, string keyword, IMemberDef member) {
			output.Write(keyword, BoxedTextColor.Keyword);
			output.Write(" ", BoxedTextColor.Text);
			output.Write(IdentifierEscaper.Escape(member.Name), member, DecompilerReferenceFlags.Definition, MetadataTextColorProvider.GetColor(member));
			output.Write(" ", BoxedTextColor.Text);
			var start = output.NextPosition;
			output.Write("{", BoxedTextColor.Punctuation);
			output.WriteLine();
			output.IncreaseIndent();
			return new BraceInfo(start);
		}

		void EndKeywordBlock(IDecompilerOutput output, BraceInfo info, CodeBracesRangeFlags flags, bool addLineSeparator = false) {
			output.DecreaseIndent();
			var end = output.NextPosition;
			output.Write("}", BoxedTextColor.Punctuation);
			output.AddBracePair(new TextSpan(info.Start, 1), new TextSpan(end, 1), flags);
			if (addLineSeparator)
				output.AddLineSeparator(end);
			output.WriteLine();
		}

		public override void Decompile(EventDef ev, IDecompilerOutput output, DecompilationContext ctx) {
			var eventInfo = StartKeywordBlock(output, ".event", ev);

			if (ev.AddMethod is not null) {
				var info = StartKeywordBlock(output, ".add", ev.AddMethod);
				EndKeywordBlock(output, info, CodeBracesRangeFlags.AccessorBraces);
			}

			if (ev.InvokeMethod is not null) {
				var info = StartKeywordBlock(output, ".invoke", ev.InvokeMethod);
				EndKeywordBlock(output, info, CodeBracesRangeFlags.AccessorBraces);
			}

			if (ev.RemoveMethod is not null) {
				var info = StartKeywordBlock(output, ".remove", ev.RemoveMethod);
				EndKeywordBlock(output, info, CodeBracesRangeFlags.AccessorBraces);
			}

			EndKeywordBlock(output, eventInfo, CodeBracesRangeFlags.EventBraces, addLineSeparator: true);
		}

		public override void Decompile(FieldDef field, IDecompilerOutput output, DecompilationContext ctx) {
			output.Write(IdentifierEscaper.Escape(field.FieldType.GetFullName()), field.FieldType.ToTypeDefOrRef(), DecompilerReferenceFlags.None, MetadataTextColorProvider.GetColor(field.FieldType));
			output.Write(" ", BoxedTextColor.Text);
			output.Write(IdentifierEscaper.Escape(field.Name), field, DecompilerReferenceFlags.Definition, MetadataTextColorProvider.GetColor(field));
			var c = field.Constant;
			if (c is not null) {
				output.Write(" ", BoxedTextColor.Text);
				output.Write("=", BoxedTextColor.Operator);
				output.Write(" ", BoxedTextColor.Text);
				if (c.Value is null)
					output.Write("null", BoxedTextColor.Keyword);
				else {
					switch (c.Type) {
					case ElementType.Boolean:
						if (c.Value is bool)
							output.Write((bool)c.Value ? "true" : "false", BoxedTextColor.Keyword);
						else
							goto default;
						break;

					case ElementType.Char:
						output.Write($"'{c.Value}'", BoxedTextColor.Char);
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
						output.Write($"{c.Value}", BoxedTextColor.Number);
						break;

					case ElementType.String:
						output.Write($"{c.Value}", BoxedTextColor.String);
						break;

					default:
						output.Write($"{c.Value}", BoxedTextColor.Text);
						break;
					}
				}
			}
		}

		public override void Decompile(PropertyDef property, IDecompilerOutput output, DecompilationContext ctx) {
			var propInfo = StartKeywordBlock(output, ".property", property);

			foreach (var getter in property.GetMethods) {
				var info = StartKeywordBlock(output, ".get", getter);
				EndKeywordBlock(output, info, CodeBracesRangeFlags.AccessorBraces);
			}

			foreach (var setter in property.SetMethods) {
				var info = StartKeywordBlock(output, ".set", setter);
				EndKeywordBlock(output, info, CodeBracesRangeFlags.AccessorBraces);
			}

			foreach (var other in property.OtherMethods) {
				var info = StartKeywordBlock(output, ".other", other);
				EndKeywordBlock(output, info, CodeBracesRangeFlags.AccessorBraces);
			}

			EndKeywordBlock(output, propInfo, CodeBracesRangeFlags.PropertyBraces, addLineSeparator: true);
		}

		public override void Decompile(TypeDef type, IDecompilerOutput output, DecompilationContext ctx) {
			this.WriteCommentLine(output, $"Type: {type.FullName}");
			if (type.BaseType is not null) {
				WriteCommentBegin(output, true);
				output.Write("Base type: ", BoxedTextColor.Comment);
				output.Write(IdentifierEscaper.Escape(type.BaseType.FullName), type.BaseType, DecompilerReferenceFlags.None, BoxedTextColor.Comment);
				WriteCommentEnd(output, true);
				output.WriteLine();
			}
			foreach (var nested in type.NestedTypes) {
				Decompile(nested, output, ctx);
				output.WriteLine();
			}

			int lastFieldPos = -1;
			foreach (var field in type.Fields) {
				Decompile(field, output, ctx);
				lastFieldPos = output.NextPosition;
				output.WriteLine();
			}
			if (lastFieldPos >= 0) {
				output.AddLineSeparator(lastFieldPos);
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

		internal static IEnumerable<ILAstDecompiler> GetDebugDecompilers(DecompilerSettingsService decompilerSettingsService) {
			double orderUI = DecompilerConstants.ILAST_ILSPY_DEBUG_ORDERUI;
			uint id = 0x64A926A5;
			yield return new ILAstDecompiler(decompilerSettingsService.ILAstDecompilerSettings, orderUI++, "ILAst (unoptimized)") {
				uniqueGuid = new Guid($"CB470049-6AFB-4BDB-93DC-1BB9{id++:X8}"),
				inlineVariables = false
			};
			string nextName = "ILAst (variable splitting)";
			foreach (ILAstOptimizationStep step in (ILAstOptimizationStep[])Enum.GetValues(typeof(ILAstOptimizationStep))) {
				yield return new ILAstDecompiler(decompilerSettingsService.ILAstDecompilerSettings, orderUI++, nextName) {
					uniqueGuid = new Guid($"CB470049-6AFB-4BDB-93DC-1BB9{id++:X8}"),
					abortBeforeStep = step
				};
				nextName = "ILAst (after " + step + ")";
			}
		}

		public override string FileExtension => ".il";

		protected override void TypeToString(IDecompilerOutput output, ITypeDefOrRef? t, bool includeNamespace, IHasCustomAttribute? attributeProvider = null) =>
			t.WriteTo(output, includeNamespace ? ILNameSyntax.TypeName : ILNameSyntax.ShortTypeName);
	}
#endif
}
