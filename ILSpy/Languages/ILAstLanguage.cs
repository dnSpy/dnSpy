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
using System.Linq;
using dnlib.DotNet;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.ILAst;

namespace ICSharpCode.ILSpy {
#if DEBUG
	/// <summary>
	/// Represents the ILAst "language" used for debugging purposes.
	/// </summary>
	sealed class ILAstLanguage : Language
	{
		string name;
		bool inlineVariables = true;
		ILAstOptimizationStep? abortBeforeStep;
		
		public override string Name {
			get {
				return name;
			}
		}
		
		public override void DecompileMethod(MethodDef method, ITextOutput output, DecompilationOptions options)
		{
			WriteComment(output, "Method: ");
			output.WriteDefinition(IdentifierEscaper.Escape(method.FullName), method, TextTokenType.Comment, false);
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
				output.Write("async", TextTokenType.Keyword);
				output.Write('/', TextTokenType.Operator);
				output.WriteLine("await", TextTokenType.Keyword);
			}
			
			var allVariables = ilMethod.GetSelfAndChildrenRecursive<ILExpression>().Select(e => e.Operand as ILVariable)
				.Where(v => v != null && !v.IsParameter).Distinct();
			foreach (ILVariable v in allVariables) {
				output.WriteDefinition(IdentifierEscaper.Escape(v.Name), v, v.IsParameter ? TextTokenType.Parameter : TextTokenType.Local);
				if (v.Type != null) {
					output.WriteSpace();
					output.Write(':', TextTokenType.Operator);
					output.WriteSpace();
					if (v.IsPinned) {
						output.Write("pinned", TextTokenType.Keyword);
						output.WriteSpace();
					}
					v.Type.WriteTo(output, ILNameSyntax.ShortTypeName);
				}
				if (v.IsGenerated) {
					output.WriteSpace();
					output.Write('[', TextTokenType.Operator);
					output.Write("generated", TextTokenType.Keyword);
					output.Write(']', TextTokenType.Operator);
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

		void StartKeywordBlock(ITextOutput output, string keyword, IMemberDef member)
		{
			output.Write(keyword, TextTokenType.Keyword);
			output.WriteSpace();
			output.WriteDefinition(IdentifierEscaper.Escape(member.Name), member, TextTokenHelper.GetTextTokenType(member), false);
			output.WriteSpace();
			output.WriteLeftBrace();
			output.WriteLine();
			output.Indent();
		}

		void EndKeywordBlock(ITextOutput output)
		{
			output.Unindent();
			output.WriteRightBrace();
			output.WriteLine();
		}

		public override void DecompileEvent(EventDef ev, ITextOutput output, DecompilationOptions options)
		{
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

		public override void DecompileField(FieldDef field, ITextOutput output, DecompilationOptions options)
		{
			output.WriteReference(IdentifierEscaper.Escape(field.FieldType.GetFullName()), field.FieldType.ToTypeDefOrRef(), TextTokenHelper.GetTextTokenType(field.FieldType));
			output.WriteSpace();
			output.WriteDefinition(IdentifierEscaper.Escape(field.Name), field, TextTokenHelper.GetTextTokenType(field), false);
			var c = field.Constant;
			if (c != null) {
				output.WriteSpace();
				output.Write('=', TextTokenType.Operator);
				output.WriteSpace();
				if (c.Value == null)
					output.Write("null", TextTokenType.Keyword);
				else {
					switch (c.Type) {
					case ElementType.Boolean:
						if (c.Value is bool)
							output.Write((bool)c.Value ? "true" : "false", TextTokenType.Keyword);
						else
							goto default;
						break;

					case ElementType.Char:
						output.Write(string.Format("'{0}'", c.Value), TextTokenType.Char);
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
						output.Write(string.Format("{0}", c.Value), TextTokenType.Number);
						break;

					case ElementType.String:
						output.Write(string.Format("{0}", c.Value), TextTokenType.String);
						break;

					default:
						output.Write(string.Format("{0}", c.Value), TextTokenType.Text);
						break;
					}
				}
			}
		}

		public override void DecompileProperty(PropertyDef property, ITextOutput output, DecompilationOptions options)
		{
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

		public override void DecompileType(TypeDef type, ITextOutput output, DecompilationOptions options)
		{
			WriteCommentLine(output, string.Format("Type: {0}", type.FullName));
			if (type.BaseType != null) {
				WriteComment(output, string.Format("Base type: "));
				output.WriteReference(IdentifierEscaper.Escape(type.BaseType.FullName), type.BaseType, TextTokenType.Comment);
				output.WriteLine();
			}
			foreach (var nested in type.NestedTypes) {
				DecompileType(nested, output, options);
				output.WriteLine();
			}

			foreach (var field in type.Fields) {
				DecompileField(field, output, options);
				output.WriteLine();
			}

			foreach (var property in type.Properties) {
				DecompileProperty(property, output, options);
				output.WriteLine();
			}

			foreach (var @event in type.Events) {
				DecompileEvent(@event, output, options);
				output.WriteLine();
			}

			foreach (var method in type.Methods) {
				DecompileMethod(method, output, options);
				output.WriteLine();
			}
		}
		
		internal static IEnumerable<ILAstLanguage> GetDebugLanguages()
		{
			yield return new ILAstLanguage { name = "ILAst (unoptimized)", inlineVariables = false };
			string nextName = "ILAst (variable splitting)";
			foreach (ILAstOptimizationStep step in Enum.GetValues(typeof(ILAstOptimizationStep))) {
				yield return new ILAstLanguage { name = nextName, abortBeforeStep = step };
				nextName = "ILAst (after " + step + ")";
				
			}
		}
		
		public override string FileExtension {
			get {
				return ".il";
			}
		}

		public override void TypeToString(ITextOutput output, ITypeDefOrRef t, bool includeNamespace, IHasCustomAttribute attributeProvider = null)
		{
			t.WriteTo(output, includeNamespace ? ILNameSyntax.TypeName : ILNameSyntax.ShortTypeName);
		}
	}
	#endif
}
