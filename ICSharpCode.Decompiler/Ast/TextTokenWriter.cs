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
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using dnlib.DotNet;

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
		
		public override void WriteIdentifier(Identifier identifier, TextTokenType tokenType)
		{
			if (tokenType == TextTokenType.Text)
				tokenType = TextTokenHelper.GetTextTokenType(identifier.AnnotationVT<TextTokenType>() ?? identifier.Annotation<object>());

			var definition = GetCurrentDefinition(identifier);
			if (definition != null) {
				output.WriteDefinition(identifier.Name, definition, tokenType, false);
				return;
			}
			
			object memberRef = GetCurrentMemberReference();

			if (memberRef != null) {
				output.WriteReference(identifier.Name, memberRef, tokenType);
				return;
			}

			definition = GetCurrentLocalDefinition();
			if (definition != null) {
				output.WriteDefinition(identifier.Name, definition, tokenType);
				return;
			}

			memberRef = GetCurrentLocalReference();
			if (memberRef != null) {
				output.WriteReference(identifier.Name, memberRef, tokenType, true);
				return;
			}

			if (firstUsingDeclaration) {
				output.MarkFoldStart(defaultCollapsed: true);
				firstUsingDeclaration = false;
			}

			output.Write(identifier.Name, tokenType);
		}

		IMemberRef GetCurrentMemberReference()
		{
			AstNode node = nodeStack.Peek();
			IMemberRef memberRef = node.Annotation<IMemberRef>();
			if (node is IndexerDeclaration)
				memberRef = null;
			if ((node is SimpleType || node is MemberType) && node.Parent is ObjectCreateExpression)
				memberRef = node.Parent.Annotation<IMemberRef>() ?? memberRef;
			if (memberRef == null && node.Role == Roles.TargetExpression && (node.Parent is InvocationExpression || node.Parent is ObjectCreateExpression)) {
				memberRef = node.Parent.Annotation<IMemberRef>();
			}
			if (node is IdentifierExpression && node.Role == Roles.TargetExpression && node.Parent is InvocationExpression && memberRef != null) {
				var declaringType = memberRef.DeclaringType.Resolve();
				if (declaringType != null && declaringType.IsDelegate())
					return null;
			}
			return FilterMemberReference(memberRef);
		}

		IMemberRef FilterMemberReference(IMemberRef memberRef)
		{
			if (memberRef == null)
				return null;

			if (context.Settings.AutomaticEvents && memberRef is FieldDef) {
				var field = (FieldDef)memberRef;
				return field.DeclaringType.FindEvent(field.Name) ?? memberRef;
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
		
		object GetCurrentDefinition(Identifier identifier)
		{
			if (nodeStack != null && nodeStack.Count != 0) {
				var data = GetDefinition(nodeStack.Peek());
				if (data != null)
					return data;
			}
			return GetDefinition(identifier);
		}

		object GetDefinition(AstNode node)
		{
			if (node is Identifier) {
				node = node.Parent;
				if (node is VariableInitializer)
					node = node.Parent;		// get FieldDeclaration / EventDeclaration
			}
			if (IsDefinition(node))
				return node.Annotation<IMemberRef>();
			
			return null;
		}
		
		public override void WriteKeyword(Role role, string keyword)
		{
			WriteKeyword(keyword);
		}

		void WriteKeyword(string keyword)
		{
			IMemberRef memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();
			if (node is IndexerDeclaration)
				memberRef = node.Annotation<PropertyDef>();
			if (memberRef != null && (node is PrimitiveType || node is ConstructorInitializer || node is BaseReferenceExpression || node is ThisReferenceExpression))
				output.WriteReference(keyword, memberRef, TextTokenType.Keyword);
			else if (memberRef != null && node is IndexerDeclaration && keyword == "this")
				output.WriteDefinition(keyword, memberRef, TextTokenType.Keyword, false);
			else
				output.Write(keyword, TextTokenType.Keyword);
		}
		
		public override void WriteToken(Role role, string token, TextTokenType tokenType)
		{
			// Attach member reference to token only if there's no identifier in the current node.
			IMemberRef memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();
			if (token != ":" && memberRef != null && node.GetChildByRole(Roles.Identifier).IsNull)
				output.WriteReference(token, memberRef, tokenType);
			else
				output.Write(token, tokenType);
		}
		
		public override void Space()
		{
			output.WriteSpace();
		}
		
		public void OpenBrace(BraceStyle style, out TextLocation? start, out TextLocation? end)
		{
			if (braceLevelWithinType >= 0 || nodeStack.Peek() is TypeDeclaration)
				braceLevelWithinType++;
			if (nodeStack.OfType<BlockStatement>().Count() <= 1) {
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
			if (nodeStack.OfType<BlockStatement>().Count() <= 1)
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
		
		public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
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
		
		public override void WritePrimitiveValue(object value, TextTokenType? tokenType = null, string literalValue = null)
		{
			int column = 0;
			TextWriterTokenWriter.WritePrimitiveValue(value, tokenType, literalValue, ref column, (a, b) => output.Write(a, b), (a, b, c) => WriteToken(a, b, c));
		}
		
		public override void WritePrimitiveType(string type)
		{
			WriteKeyword(type);
			if (type == "new") {
				output.Write('(', TextTokenType.Operator);
				output.Write(')', TextTokenType.Operator);
			}
		}
		
		MemberMapping currentMemberMapping;
		Stack<MemberMapping> parentMemberMappings = new Stack<MemberMapping>();
		List<Tuple<MemberMapping, List<ILRange>>> multiMappings;
		
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
			
			MemberMapping mapping = node.Annotation<MemberMapping>();
			if (mapping != null) {
				parentMemberMappings.Push(currentMemberMapping);
				currentMemberMapping = mapping;
			}
			// For ctor/cctor field initializers
			var mms = node.Annotation<List<Tuple<MemberMapping, List<ILRange>>>>();
			if (mms != null) {
				Debug.Assert(multiMappings == null);
				multiMappings = mms;
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
			
			if (node.Annotation<MemberMapping>() != null) {
				output.AddDebugSymbols(currentMemberMapping);
				currentMemberMapping = parentMemberMappings.Pop();
			}
			var mms = node.Annotation<List<Tuple<MemberMapping, List<ILRange>>>>();
			if (mms != null) {
				Debug.Assert(mms == multiMappings);
				if (mms == multiMappings) {
					foreach (var mm in mms)
						output.AddDebugSymbols(mm.Item1);
					multiMappings = null;
				}
			}
		}
		
		private static bool IsDefinition(AstNode node)
		{
			return node is EntityDeclaration
				|| (node is VariableInitializer && node.Parent is FieldDeclaration)
				|| node is FixedVariableInitializer
				|| node is TypeParameterDeclaration;
		}

		class DebugState
		{
			public List<AstNode> Nodes = new List<AstNode>();
			public TextLocation StartLocation;
		}
		readonly Stack<DebugState> debugStack = new Stack<DebugState>();
		public override void DebugStart(AstNode node, TextLocation? start)
		{
			debugStack.Push(new DebugState { StartLocation = start ?? output.Location });
		}

		public override void DebugHidden(AstNode hiddenNode)
		{
			if (hiddenNode == null || hiddenNode.IsNull)
				return;
			if (debugStack.Count > 0)
				debugStack.Peek().Nodes.AddRange(hiddenNode.DescendantsAndSelf);
		}

		public override void DebugExpression(AstNode node)
		{
			if (debugStack.Count > 0)
				debugStack.Peek().Nodes.Add(node);
		}

		static readonly IEnumerable<ILRange> emptyILRange = new ILRange[0];
		public override void DebugEnd(AstNode node, TextLocation? end)
		{
			var state = debugStack.Pop();
			if (currentMemberMapping != null) {
				foreach (var range in ILRange.OrderAndJoin(GetILRanges(state))) {
					currentMemberMapping.MemberCodeMappings.Add(
						new SourceCodeMapping {
							ILInstructionOffset = range,
							StartLocation = state.StartLocation,
							EndLocation = end ?? output.Location,
							MemberMapping = currentMemberMapping
						});
				}
			}
			else if (multiMappings != null) {
				foreach (var mm in multiMappings) {
					foreach (var range in ILRange.OrderAndJoin(mm.Item2)) {
						mm.Item1.MemberCodeMappings.Add(
							new SourceCodeMapping {
								ILInstructionOffset = range,
								StartLocation = state.StartLocation,
								EndLocation = end ?? output.Location,
								MemberMapping = mm.Item1
							});
					}
				}
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

		public override TextLocation? GetLocation()
		{
			return output.Location;
		}
	}
}
