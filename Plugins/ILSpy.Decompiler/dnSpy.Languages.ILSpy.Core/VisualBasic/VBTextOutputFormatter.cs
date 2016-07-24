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
using dnSpy.Decompiler.Shared;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Ast;

namespace dnSpy.Languages.ILSpy.VisualBasic {
	sealed class VBTextOutputFormatter : IOutputFormatter {
		readonly IDecompilerOutput output;
		readonly Stack<AstNode> nodeStack = new Stack<AstNode>();

		public VBTextOutputFormatter(IDecompilerOutput output) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			this.output = output;
		}

		MethodDebugInfoBuilder currentMethodDebugInfoBuilder;
		Stack<MethodDebugInfoBuilder> parentMethodDebugInfoBuilder = new Stack<MethodDebugInfoBuilder>();
		List<Tuple<MethodDebugInfoBuilder, List<BinSpan>>> multiMappings;

		public void StartNode(AstNode node) {
			nodeStack.Push(node);

			MethodDebugInfoBuilder mapping = node.Annotation<MethodDebugInfoBuilder>();
			if (mapping != null) {
				parentMethodDebugInfoBuilder.Push(currentMethodDebugInfoBuilder);
				currentMethodDebugInfoBuilder = mapping;
			}
			// For ctor/cctor field initializers
			var mms = node.Annotation<List<Tuple<MethodDebugInfoBuilder, List<BinSpan>>>>();
			if (mms != null) {
				Debug.Assert(multiMappings == null);
				multiMappings = mms;
			}
		}

		public void EndNode(AstNode node) {
			if (nodeStack.Pop() != node)
				throw new InvalidOperationException();

			if (node.Annotation<MethodDebugInfoBuilder>() != null) {
				output.AddDebugInfo(currentMethodDebugInfoBuilder.Create());
				currentMethodDebugInfoBuilder = parentMethodDebugInfoBuilder.Pop();
			}
			var mms = node.Annotation<List<Tuple<MethodDebugInfoBuilder, List<BinSpan>>>>();
			if (mms != null) {
				Debug.Assert(mms == multiMappings);
				if (mms == multiMappings) {
					foreach (var mm in mms)
						output.AddDebugInfo(mm.Item1.Create());
					multiMappings = null;
				}
			}
		}

		public void WriteIdentifier(string identifier, object data) {
			var definition = GetCurrentDefinition();
			if (definition != null) {
				output.Write(IdentifierEscaper.Escape(identifier), definition, DecompilerReferenceFlags.Definition, data);
				return;
			}

			object memberRef = GetCurrentMemberReference();
			if (memberRef != null) {
				output.Write(IdentifierEscaper.Escape(identifier), memberRef, DecompilerReferenceFlags.None, data);
				return;
			}

			definition = GetCurrentLocalDefinition();
			if (definition != null) {
				output.Write(IdentifierEscaper.Escape(identifier), definition, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, data);
				return;
			}

			memberRef = GetCurrentLocalReference();
			if (memberRef != null) {
				output.Write(IdentifierEscaper.Escape(identifier), memberRef, DecompilerReferenceFlags.Local, data);
				return;
			}

			output.Write(IdentifierEscaper.Escape(identifier), data);
		}

		IMemberRef GetCurrentMemberReference() {
			AstNode node = nodeStack.Peek();
			if (node.Annotation<ILVariable>() != null)
				return null;
			var memberRef = node.Annotation<IMemberRef>();
			if (node.Parent is ObjectCreationExpression)
				memberRef = node.Parent.Annotation<IMethod>();
			if (memberRef == null && node is Identifier) {
				node = node.Parent ?? node;
				memberRef = node.Annotation<IMemberRef>();
			}
			if (memberRef == null && node.Role == AstNode.Roles.TargetExpression && (node.Parent is InvocationExpression || node.Parent is ObjectCreationExpression)) {
				memberRef = node.Parent.Annotation<IMemberRef>();
			}
			return memberRef;
		}

		object GetCurrentLocalReference() {
			AstNode node = nodeStack.Peek();
			ILVariable variable = node.Annotation<ILVariable>();
			if (variable == null && node.Parent is IdentifierExpression)
				variable = node.Parent.Annotation<ILVariable>();
			if (variable != null) {
				if (variable.OriginalParameter != null)
					return variable.OriginalParameter;
				//if (variable.OriginalVariable != null)
				//    return variable.OriginalVariable;
				return variable;
			}
			return null;
		}

		object GetCurrentLocalDefinition() {
			AstNode node = nodeStack.Peek();
			var parameterDef = node.Annotation<Parameter>();
			if (parameterDef != null)
				return parameterDef;

			if (node is VariableIdentifier)
				node = node.Parent ?? node;
			if (node is VariableDeclaratorWithTypeAndInitializer || node is CatchBlock || node is ForEachStatement) {
				var variable = node.Annotation<ILVariable>();
				if (variable != null) {
					if (variable.OriginalParameter != null)
						return variable.OriginalParameter;
					//if (variable.OriginalVariable != null)
					//    return variable.OriginalVariable;
					return variable;
				}
			}

			var label = node as LabelDeclarationStatement;
			if (label != null) {
				var method = nodeStack.Select(nd => nd.Annotation<IMethod>()).FirstOrDefault(mr => mr != null && mr.IsMethod);
				if (method != null)
					return method.ToString() + label.Label;
			}


			return null;
		}

		object GetCurrentDefinition() {
			if (nodeStack == null || nodeStack.Count == 0)
				return null;

			var node = nodeStack.Peek();
			if (node is ParameterDeclaration)
				return null;
			if (node is VariableIdentifier)
				return ((VariableIdentifier)node).Name.Annotation<IMemberDef>();
			if (IsDefinition(node))
				return node.Annotation<IMemberRef>();

			if (node is Identifier) {
				node = node.Parent;
				if (IsDefinition(node))
					return node.Annotation<IMemberRef>();
			}

			return null;
		}

		public void WriteKeyword(string keyword) {
			IMemberRef memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();
			if (memberRef != null && (node is PrimitiveType || node is InstanceExpression))
				output.Write(keyword, memberRef, DecompilerReferenceFlags.None, BoxedTextTokenKind.Keyword);
			else if (memberRef != null && (node is ConstructorDeclaration && keyword == "New"))
				output.Write(keyword, memberRef, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextTokenKind.Keyword);
			else if (memberRef != null && (node is Accessor && (keyword == "Get" || keyword == "Set" || keyword == "AddHandler" || keyword == "RemoveHandler" || keyword == "RaiseEvent"))) {
				if (canPrintAccessor)
					output.Write(keyword, memberRef, DecompilerReferenceFlags.Local | DecompilerReferenceFlags.Definition, BoxedTextTokenKind.Keyword);
				else
					output.Write(keyword, BoxedTextTokenKind.Keyword);
				canPrintAccessor = !canPrintAccessor;
			}
			else
				output.Write(keyword, BoxedTextTokenKind.Keyword);
		}
		bool canPrintAccessor = true;

		public void WriteToken(string token, object data) {
			IMemberRef memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();

			bool addRef = memberRef != null &&
					(node is BinaryOperatorExpression ||
					node is UnaryOperatorExpression ||
					node is AssignmentExpression);

			// Add a ref to the method if it's a delegate call
			if (!addRef && node is InvocationExpression && memberRef is IMethod) {
				var md = Resolve(memberRef as IMethod);
				if (md != null && md.DeclaringType != null && md.DeclaringType.IsDelegate)
					addRef = true;
			}

			if (addRef)
				output.Write(token, memberRef, DecompilerReferenceFlags.None, data);
			else
				output.Write(token, data);
		}

		static MethodDef Resolve(IMethod method) {
			if (method is MethodSpec)
				method = ((MethodSpec)method).Method;
			if (method is MemberRef)
				return ((MemberRef)method).ResolveMethod();
			else
				return (MethodDef)method;
		}

		public void Space() => output.Write(" ", BoxedTextTokenKind.Text);
		public void Indent() => output.Indent();
		public void Unindent() => output.Unindent();
		public void NewLine() => output.WriteLine();

		public void WriteComment(bool isDocumentation, string content) {
			if (isDocumentation) {
				output.Write("'''", BoxedTextTokenKind.XmlDocCommentDelimiter);
				output.WriteXmlDoc(content);
				output.WriteLine();
			}
			else
				output.WriteLine("'" + content, BoxedTextTokenKind.Comment);
		}

		static bool IsDefinition(AstNode node) =>
			node is FieldDeclaration ||
			node is ConstructorDeclaration ||
			node is EventDeclaration ||
			node is DelegateDeclaration ||
			node is OperatorDeclaration ||
			node is MemberDeclaration ||
			node is TypeDeclaration ||
			node is EnumDeclaration ||
			node is EnumMemberDeclaration ||
			node is TypeParameterDeclaration;

		class DebugState {
			public List<AstNode> Nodes = new List<AstNode>();
			public List<BinSpan> ExtraBinSpans = new List<BinSpan>();
			public int StartLocation;
		}
		readonly Stack<DebugState> debugStack = new Stack<DebugState>();
		public void DebugStart(AstNode node) => debugStack.Push(new DebugState { StartLocation = output.NextPosition });

		public void DebugHidden(object hiddenBinSpans) {
			var list = hiddenBinSpans as IList<BinSpan>;
			if (list != null) {
				if (debugStack.Count > 0)
					debugStack.Peek().ExtraBinSpans.AddRange(list);
			}
		}

		public void DebugExpression(AstNode node) {
			if (debugStack.Count > 0)
				debugStack.Peek().Nodes.Add(node);
		}

		public void DebugEnd(AstNode node) {
			var state = debugStack.Pop();
			if (currentMethodDebugInfoBuilder != null) {
				foreach (var binSpan in BinSpan.OrderAndCompact(GetBinSpans(state)))
					currentMethodDebugInfoBuilder.Add(new SourceStatement(binSpan, new TextSpan(state.StartLocation, output.NextPosition - state.StartLocation)));
			}
			else if (multiMappings != null) {
				foreach (var mm in multiMappings) {
					foreach (var binSpan in BinSpan.OrderAndCompact(mm.Item2))
						mm.Item1.Add(new SourceStatement(binSpan, new TextSpan(state.StartLocation, output.NextPosition - state.StartLocation)));
				}
			}
		}

		static IEnumerable<BinSpan> GetBinSpans(DebugState state) {
			foreach (var node in state.Nodes) {
				foreach (var ann in node.Annotations) {
					var list = ann as IList<BinSpan>;
					if (list == null)
						continue;
					foreach (var binSpan in list)
						yield return binSpan;
				}
			}
			foreach (var binSpan in state.ExtraBinSpans)
				yield return binSpan;
		}
	}
}
