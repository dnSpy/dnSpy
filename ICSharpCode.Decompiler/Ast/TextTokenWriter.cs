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
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast
{
	public class TextTokenWriter : TokenWriter
	{
		readonly ITextOutput output;
		readonly DecompilerContext context;
		readonly Stack<AstNode> nodeStack = new Stack<AstNode>();
		int braceLevelWithinType = -1;
		bool inDocumentationComment = false;
		bool firstUsingDeclaration;
		bool lastUsingDeclaration;
		
		TextLocation? lastEndOfLine;
		
		public bool FoldBraces = false;
		
		public TextTokenWriter(ITextOutput output, DecompilerContext context)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			if (context == null)
				throw new ArgumentNullException("context");
			this.output = output;
			this.context = context;
		}
		
		public override void WriteIdentifier(Identifier identifier)
		{
			if (identifier.IsVerbatim || CSharpOutputVisitor.IsKeyword(identifier.Name, identifier)) {
				output.Write('@');
			}
			
			var definition = GetCurrentDefinition();
			if (definition != null) {
				output.WriteDefinition(identifier.Name, definition, false);
				return;
			}
			
			object memberRef = GetCurrentMemberReference();

			if (memberRef != null) {
				output.WriteReference(identifier.Name, memberRef);
				return;
			}

			definition = GetCurrentLocalDefinition();
			if (definition != null) {
				output.WriteDefinition(identifier.Name, definition);
				return;
			}

			memberRef = GetCurrentLocalReference();
			if (memberRef != null) {
				output.WriteReference(identifier.Name, memberRef, true);
				return;
			}

			if (firstUsingDeclaration) {
				output.MarkFoldStart(defaultCollapsed: true);
				firstUsingDeclaration = false;
			}

			output.Write(identifier.Name);
		}

		MemberReference GetCurrentMemberReference()
		{
			AstNode node = nodeStack.Peek();
			MemberReference memberRef = node.Annotation<MemberReference>();
			if (memberRef == null && node.Role == Roles.TargetExpression && (node.Parent is InvocationExpression || node.Parent is ObjectCreateExpression)) {
				memberRef = node.Parent.Annotation<MemberReference>();
			}
			if (node is IdentifierExpression && node.Role == Roles.TargetExpression && node.Parent is InvocationExpression && memberRef != null) {
				var declaringType = memberRef.DeclaringType.Resolve();
				if (declaringType != null && declaringType.IsDelegate())
					return null;
			}
			return FilterMemberReference(memberRef);
		}

		MemberReference FilterMemberReference(MemberReference memberRef)
		{
			if (memberRef == null)
				return null;

			if (context.Settings.AutomaticEvents && memberRef is FieldDefinition) {
				var field = (FieldDefinition)memberRef;
				return field.DeclaringType.Events.FirstOrDefault(ev => ev.Name == field.Name) ?? memberRef;
			}

			return memberRef;
		}

		object GetCurrentLocalReference()
		{
			AstNode node = nodeStack.Peek();
			ILVariable variable = node.Annotation<ILVariable>();
			if (variable != null) {
				if (variable.OriginalParameter != null)
					return variable.OriginalParameter;
				//if (variable.OriginalVariable != null)
				//    return variable.OriginalVariable;
				return variable;
			}

			var gotoStatement = node as GotoStatement;
			if (gotoStatement != null)
			{
				var method = nodeStack.Select(nd => nd.Annotation<MethodReference>()).FirstOrDefault(mr => mr != null);
				if (method != null)
					return method.ToString() + gotoStatement.Label;
			}

			return null;
		}

		object GetCurrentLocalDefinition()
		{
			AstNode node = nodeStack.Peek();
			if (node is Identifier && node.Parent != null)
				node = node.Parent;
			
			var parameterDef = node.Annotation<ParameterDefinition>();
			if (parameterDef != null)
				return parameterDef;

			if (node is VariableInitializer || node is CatchClause || node is ForeachStatement) {
				var variable = node.Annotation<ILVariable>();
				if (variable != null) {
					if (variable.OriginalParameter != null)
						return variable.OriginalParameter;
					//if (variable.OriginalVariable != null)
					//    return variable.OriginalVariable;
					return variable;
				}
			}

			var label = node as LabelStatement;
			if (label != null) {
				var method = nodeStack.Select(nd => nd.Annotation<MethodReference>()).FirstOrDefault(mr => mr != null);
				if (method != null)
					return method.ToString() + label.Label;
			}

			return null;
		}
		
		object GetCurrentDefinition()
		{
			if (nodeStack == null || nodeStack.Count == 0)
				return null;
			
			var node = nodeStack.Peek();
			if (node is Identifier)
				node = node.Parent;
			if (IsDefinition(node))
				return node.Annotation<MemberReference>();
			
			return null;
		}
		
		public override void WriteKeyword(Role role, string keyword)
		{
			output.Write(keyword);
		}
		
		public override void WriteToken(Role role, string token)
		{
			// Attach member reference to token only if there's no identifier in the current node.
			MemberReference memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();
			if (memberRef != null && node.GetChildByRole(Roles.Identifier).IsNull)
				output.WriteReference(token, memberRef);
			else
				output.Write(token);
		}
		
		public override void Space()
		{
			output.Write(' ');
		}
		
		public void OpenBrace(BraceStyle style)
		{
			if (braceLevelWithinType >= 0 || nodeStack.Peek() is TypeDeclaration)
				braceLevelWithinType++;
			if (nodeStack.OfType<BlockStatement>().Count() <= 1 || FoldBraces) {
				output.MarkFoldStart(defaultCollapsed: braceLevelWithinType == 1);
			}
			output.WriteLine();
			output.WriteLine("{");
			output.Indent();
		}
		
		public void CloseBrace(BraceStyle style)
		{
			output.Unindent();
			output.Write('}');
			if (nodeStack.OfType<BlockStatement>().Count() <= 1 || FoldBraces)
				output.MarkFoldEnd();
			if (braceLevelWithinType >= 0)
				braceLevelWithinType--;
		}
		
		public override void Indent()
		{
			output.Indent();
		}
		
		public override void Unindent()
		{
			output.Unindent();
		}
		
		public override void NewLine()
		{
			if (lastUsingDeclaration) {
				output.MarkFoldEnd();
				lastUsingDeclaration = false;
			}
			lastEndOfLine = output.Location;
			output.WriteLine();
		}
		
		public override void WriteComment(CommentType commentType, string content)
		{
			switch (commentType) {
				case CommentType.SingleLine:
					output.Write("//");
					output.WriteLine(content);
					break;
				case CommentType.MultiLine:
					output.Write("/*");
					output.Write(content);
					output.Write("*/");
					break;
				case CommentType.Documentation:
					bool isLastLine = !(nodeStack.Peek().NextSibling is Comment);
					if (!inDocumentationComment && !isLastLine) {
						inDocumentationComment = true;
						output.MarkFoldStart("///" + content, true);
					}
					output.Write("///");
					output.Write(content);
					if (inDocumentationComment && isLastLine) {
						inDocumentationComment = false;
						output.MarkFoldEnd();
					}
					output.WriteLine();
					break;
				default:
					output.Write(content);
					break;
			}
		}
		
		public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			// pre-processor directive must start on its own line
			output.Write('#');
			output.Write(type.ToString().ToLowerInvariant());
			if (!string.IsNullOrEmpty(argument)) {
				output.Write(' ');
				output.Write(argument);
			}
			output.WriteLine();
		}
		
		public override void WritePrimitiveValue(object value, string literalValue = null)
		{
			new TextWriterTokenWriter(new TextOutputWriter(output)).WritePrimitiveValue(value, literalValue);
		}
		
		public override void WritePrimitiveType(string type)
		{
			output.Write(type);
			if (type == "new") {
				output.Write("()");
			}
		}
		
		Stack<TextLocation> startLocations = new Stack<TextLocation>();
		Stack<MethodDebugSymbols> symbolsStack = new Stack<MethodDebugSymbols>();
		
		public override void StartNode(AstNode node)
		{
			if (nodeStack.Count == 0) {
				if (IsUsingDeclaration(node)) {
					firstUsingDeclaration = !IsUsingDeclaration(node.PrevSibling);
					lastUsingDeclaration = !IsUsingDeclaration(node.NextSibling);
				} else {
					firstUsingDeclaration = false;
					lastUsingDeclaration = false;
				}
			}
			nodeStack.Push(node);
			startLocations.Push(output.Location);
			
			if (node is EntityDeclaration && node.Annotation<MemberReference>() != null && node.GetChildByRole(Roles.Identifier).IsNull)
				output.WriteDefinition("", node.Annotation<MemberReference>(), false);

			if (node.Annotation<MethodDebugSymbols>() != null) {
				symbolsStack.Push(node.Annotation<MethodDebugSymbols>());
				symbolsStack.Peek().StartLocation = startLocations.Peek();
			}
		}
		
		private bool IsUsingDeclaration(AstNode node)
		{
			return node is UsingDeclaration || node is UsingAliasDeclaration;
		}

		public override void EndNode(AstNode node)
		{
			if (nodeStack.Pop() != node)
				throw new InvalidOperationException();
			
			var startLocation = startLocations.Pop();
			
			// code mappings
			var ranges = node.Annotation<List<ILRange>>();
			if (symbolsStack.Count > 0 && ranges != null && ranges.Count > 0) {
				// Ignore the newline which was printed at the end of the statement
				TextLocation endLocation = (node is Statement) ? (lastEndOfLine ?? output.Location) : output.Location;
				symbolsStack.Peek().SequencePoints.Add(
					new SequencePoint() {
						ILRanges = ILRange.OrderAndJoin(ranges).ToArray(),
						StartLocation = startLocation,
						EndLocation = endLocation
					});
			}
			
			if (node.Annotation<MethodDebugSymbols>() != null) {
				symbolsStack.Peek().EndLocation = output.Location;
				output.AddDebugSymbols(symbolsStack.Pop());
			}
		}
		
		private static bool IsDefinition(AstNode node)
		{
			return node is EntityDeclaration
				|| (node is VariableInitializer && node.Parent is FieldDeclaration)
				|| node is FixedVariableInitializer;
		}
	}
}
