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
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.Ast
{
	public class TextOutputFormatter : IOutputFormatter
	{
		readonly ITextOutput output;
		readonly DecompilerContext context;
		readonly Stack<AstNode> nodeStack = new Stack<AstNode>();
		int braceLevelWithinType = -1;
		bool inDocumentationComment = false;
		bool firstUsingDeclaration;
		bool lastUsingDeclaration;
		
		public bool FoldBraces = false;
		
		public TextOutputFormatter(ITextOutput output, DecompilerContext context)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			if (context == null)
				throw new ArgumentNullException("context");
			this.output = output;
			this.context = context;
		}
		
		public void WriteIdentifier(string identifier, TextTokenType tokenType)
		{
			var definition = GetCurrentDefinition();
			if (definition != null) {
				output.WriteDefinition(identifier, definition, tokenType, false);
				return;
			}
			
			object memberRef = GetCurrentMemberReference();

			if (memberRef != null) {
				output.WriteReference(identifier, memberRef, tokenType);
				return;
			}

			definition = GetCurrentLocalDefinition();
			if (definition != null) {
				output.WriteDefinition(identifier, definition, tokenType);
				return;
			}

			memberRef = GetCurrentLocalReference();
			if (memberRef != null) {
				output.WriteReference(identifier, memberRef, tokenType, true);
				return;
			}

			if (firstUsingDeclaration) {
				output.MarkFoldStart(defaultCollapsed: true);
				firstUsingDeclaration = false;
			}

			output.Write(identifier, tokenType);
		}

		IMemberRef GetCurrentMemberReference()
		{
			AstNode node = nodeStack.Peek();
			IMemberRef memberRef = node.Annotation<IMemberRef>();
			if ((node.Role == Roles.Type && node.Parent is ObjectCreateExpression) ||
				(memberRef == null && node.Role == Roles.TargetExpression && (node.Parent is InvocationExpression || node.Parent is ObjectCreateExpression)))
			{
				memberRef = node.Parent.Annotation<IMemberRef>();
			}
			return FilterMemberReference(memberRef);
		}

		IMemberRef FilterMemberReference(IMemberRef memberRef)
		{
			if (memberRef == null)
				return null;

			if (context.Settings.AutomaticEvents && memberRef is FieldDef) {
				var field = (FieldDef)memberRef;
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
				var method = nodeStack.Select(nd => nd.Annotation<IMethod>()).FirstOrDefault(mr => mr != null && mr.IsMethod);
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
			
			var parameterDef = node.Annotation<Parameter>();
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
				var method = nodeStack.Select(nd => nd.Annotation<IMethod>()).FirstOrDefault(mr => mr != null && mr.IsMethod);
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
				return node.Annotation<IMemberRef>();
			
			return null;
		}
		
		public void WriteKeyword(string keyword)
		{
			IMemberRef memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();
			if (memberRef != null && (node is ConstructorInitializer || node is PrimitiveType))
				output.WriteReference(keyword, memberRef, TextTokenType.Keyword);
			else
				output.Write(keyword, TextTokenType.Keyword);
		}
		
		public void WriteToken(string token, TextTokenType tokenType)
		{
			// Attach member reference to token only if there's no identifier in the current node.
			IMemberRef memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();
			if (memberRef != null && node.GetChildByRole(Roles.Identifier).IsNull)
				output.WriteReference(token, memberRef, tokenType);
			else
				output.Write(token, tokenType);
		}
		
		public void Space()
		{
			output.WriteSpace();
		}
		
		public void OpenBrace(BraceStyle style, out TextLocation? start, out TextLocation? end)
		{
			if (braceLevelWithinType >= 0 || nodeStack.Peek() is TypeDeclaration)
				braceLevelWithinType++;
			if (nodeStack.OfType<BlockStatement>().Count() <= 1 || FoldBraces) {
				output.MarkFoldStart(defaultCollapsed: braceLevelWithinType == 1);
			}
			output.WriteLine();
			start = output.Location;
			output.WriteLeftBrace();
			end = output.Location;
			output.WriteLine();
			output.Indent();
		}
		
		public void CloseBrace(BraceStyle style, out TextLocation? start, out TextLocation? end)
		{
			output.Unindent();
			start = output.Location;
			output.WriteRightBrace();
			end = output.Location;
			if (nodeStack.OfType<BlockStatement>().Count() <= 1 || FoldBraces)
				output.MarkFoldEnd();
			if (braceLevelWithinType >= 0)
				braceLevelWithinType--;
		}
		
		public void Indent()
		{
			output.Indent();
		}
		
		public void Unindent()
		{
			output.Unindent();
		}
		
		public void NewLine()
		{
			if (lastUsingDeclaration) {
				output.MarkFoldEnd();
				lastUsingDeclaration = false;
			}
			output.WriteLine();
		}
		
		public void WriteComment(CommentType commentType, string content)
		{
			switch (commentType) {
				case CommentType.SingleLine:
					output.Write("//", TextTokenType.Comment);
					output.WriteLine(content, TextTokenType.Comment);
					break;
				case CommentType.MultiLine:
					output.Write("/*", TextTokenType.Comment);
					output.Write(content, TextTokenType.Comment);
					output.Write("*/", TextTokenType.Comment);
					break;
				case CommentType.Documentation:
					bool isLastLine = !(nodeStack.Peek().NextSibling is Comment);
					if (!inDocumentationComment && !isLastLine) {
						inDocumentationComment = true;
						output.MarkFoldStart("///" + content, true);
					}
					output.Write("///", TextTokenType.XmlDocTag);
					output.WriteXmlDoc(content);
					if (inDocumentationComment && isLastLine) {
						inDocumentationComment = false;
						output.MarkFoldEnd();
					}
					output.WriteLine();
					break;
				default:
					output.Write(content, TextTokenType.Comment);
					break;
			}
		}
		
		public void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			// pre-processor directive must start on its own line
			output.Write('#', TextTokenType.Text);
			output.Write(type.ToString().ToLowerInvariant(), TextTokenType.Text);
			if (!string.IsNullOrEmpty(argument)) {
				output.WriteSpace();
				output.Write(argument, TextTokenType.Text);
			}
			output.WriteLine();
		}
		
		MemberMapping currentMemberMapping;
		Stack<MemberMapping> parentMemberMappings = new Stack<MemberMapping>();
		
		public void StartNode(AstNode node)
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
			
			if (node is EntityDeclaration && node.Annotation<IMemberRef>() != null && node.GetChildByRole(Roles.Identifier).IsNull)
				output.WriteDefinition("", node.Annotation<IMemberRef>(), TextTokenType.Text, false);
			
			MemberMapping mapping = node.Annotation<MemberMapping>();
			if (mapping != null) {
				parentMemberMappings.Push(currentMemberMapping);
				currentMemberMapping = mapping;
			}
		}
		
		private bool IsUsingDeclaration(AstNode node)
		{
			return node is UsingDeclaration || node is UsingAliasDeclaration;
		}

		public void EndNode(AstNode node)
		{
			if (nodeStack.Pop() != node)
				throw new InvalidOperationException();
			
			if (node.Annotation<MemberMapping>() != null) {
				output.AddDebuggerMemberMapping(currentMemberMapping);
				currentMemberMapping = parentMemberMappings.Pop();
			}
		}
		
		private static bool IsDefinition(AstNode node)
		{
			return node is EntityDeclaration
				|| (node is VariableInitializer && node.Parent is FieldDeclaration)
				|| node is FixedVariableInitializer;
		}

		class DebugState
		{
			public List<AstNode> Nodes = new List<AstNode>();
			public TextLocation StartLocation;
		}
		readonly Stack<DebugState> debugStack = new Stack<DebugState>();
		public void DebugStart(AstNode node, TextLocation? start)
		{
			debugStack.Push(new DebugState { StartLocation = start ?? output.Location });
		}

		public void DebugHidden(AstNode hiddenNode)
		{
			if (hiddenNode == null || hiddenNode.IsNull)
				return;
			if (debugStack.Count > 0)
				debugStack.Peek().Nodes.AddRange(hiddenNode.DescendantsAndSelf);
		}

		public void DebugExpression(AstNode node)
		{
			if (debugStack.Count > 0)
				debugStack.Peek().Nodes.Add(node);
		}

		static readonly IEnumerable<ILRange> emptyILRange = new ILRange[0];
		public void DebugEnd(AstNode node, TextLocation? end)
		{
			var state = debugStack.Pop();
			if (currentMemberMapping == null)
				return;
			// add all ranges
			foreach (var range in ILRange.OrderAndJoint(GetILRanges(state))) {
				currentMemberMapping.MemberCodeMappings.Add(
					new SourceCodeMapping {
						ILInstructionOffset = range,
						StartLocation = state.StartLocation,
						EndLocation = end ?? output.Location,
						MemberMapping = currentMemberMapping
					});
			}
		}

		static IEnumerable<ILRange> GetILRanges(DebugState state)
		{
			foreach (var node in state.Nodes) {
				foreach (var ann in node.Annotations) {
					var list = ann as IList<ILRange>;
					if (list == null)
						continue;
					foreach (var range in list)
						yield return range;
				}
			}
		}
	}
}
