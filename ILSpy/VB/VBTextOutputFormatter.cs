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
using ICSharpCode.NRefactory.VB;
using ICSharpCode.NRefactory.VB.Ast;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.VB
{
	/// <summary>
	/// Description of VBTextOutputFormatter.
	/// </summary>
	public class VBTextOutputFormatter : IOutputFormatter
	{
		readonly ITextOutput output;
		readonly Stack<AstNode> nodeStack = new Stack<AstNode>();
		
		public VBTextOutputFormatter(ITextOutput output)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			this.output = output;
		}
		
		MemberMapping currentMemberMapping;
		Stack<MemberMapping> parentMemberMappings = new Stack<MemberMapping>();
		List<Tuple<MemberMapping, List<ILRange>>> multiMappings;
		
		public void StartNode(AstNode node)
		{
//			var ranges = node.Annotation<List<ILRange>>();
//			if (ranges != null && ranges.Count > 0)
//			{
//				// find the ancestor that has method mapping as annotation
//				if (node.Ancestors != null && node.Ancestors.Count() > 0)
//				{
//					var n = node.Ancestors.FirstOrDefault(a => a.Annotation<MemberMapping>() != null);
//					if (n != null) {
//						MemberMapping mapping = n.Annotation<MemberMapping>();
//
//						// add all ranges
//						foreach (var range in ranges) {
//							mapping.MemberCodeMappings.Add(new SourceCodeMapping {
//							                               	ILInstructionOffset = range,
//							                               	SourceCodeLine = output.CurrentLine,
//							                               	MemberMapping = mapping
//							                               });
//						}
//					}
//				}
//			}
			
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
		
		public void EndNode(AstNode node)
		{
			if (nodeStack.Pop() != node)
				throw new InvalidOperationException();
			
			if (node.Annotation<MemberMapping>() != null) {
				output.AddDebuggerMemberMapping(currentMemberMapping);
				currentMemberMapping = parentMemberMappings.Pop();
			}
			var mms = node.Annotation<List<Tuple<MemberMapping, List<ILRange>>>>();
			if (mms != null) {
				Debug.Assert(mms == multiMappings);
				if (mms == multiMappings) {
					foreach (var mm in mms)
						output.AddDebuggerMemberMapping(mm.Item1);
					multiMappings = null;
				}
			}
		}
		
		public void WriteIdentifier(string identifier, TextTokenType tokenType)
		{
			var definition = GetCurrentDefinition();
			if (definition != null) {
				output.WriteDefinition(identifier, definition, tokenType);
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

			output.Write(identifier, tokenType);
		}

		IMemberRef GetCurrentMemberReference()
		{
			AstNode node = nodeStack.Peek();
			IMemberRef memberRef = node.Annotation<IMemberRef>();
			if (memberRef == null && node.Role == AstNode.Roles.TargetExpression && (node.Parent is InvocationExpression || node.Parent is ObjectCreationExpression)) {
				memberRef = node.Parent.Annotation<IMemberRef>();
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
			return null;
		}

		object GetCurrentLocalDefinition()
		{
			AstNode node = nodeStack.Peek();
			var parameterDef = node.Annotation<Parameter>();
			if (parameterDef != null)
				return parameterDef;

			if (node is VariableInitializer || node is CatchBlock || node is ForEachStatement) {
				var variable = node.Annotation<ILVariable>();
				if (variable != null) {
					if (variable.OriginalParameter != null)
						return variable.OriginalParameter;
					//if (variable.OriginalVariable != null)
					//    return variable.OriginalVariable;
					return variable;
				} else {

				}
			}

			return null;
		}
		
		object GetCurrentDefinition()
		{
			if (nodeStack == null || nodeStack.Count == 0)
				return null;
			
			var node = nodeStack.Peek();			
			if (IsDefinition(node))
				return node.Annotation<IMemberRef>();
			
			node = node.Parent;
			if (IsDefinition(node))
				return node.Annotation<IMemberRef>();

			return null;
		}
		
		public void WriteKeyword(string keyword)
		{
			IMemberRef memberRef = GetCurrentMemberReference();
			var node = nodeStack.Peek();
			if (memberRef != null && node is PrimitiveType)
				output.WriteReference(keyword, memberRef, TextTokenType.Keyword);
			else
				output.Write(keyword, TextTokenType.Keyword);
		}
		
		public void WriteToken(string token, TextTokenType tokenType)
		{
			// Attach member reference to token only if there's no identifier in the current node.
			IMemberRef memberRef = GetCurrentMemberReference();
			if (memberRef != null && nodeStack.Peek().GetChildByRole(AstNode.Roles.Identifier).IsNull)
				output.WriteReference(token, memberRef, tokenType);
			else
				output.Write(token, tokenType);
		}
		
		public void Space()
		{
			output.WriteSpace();
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
			output.WriteLine();
		}
		
		public void WriteComment(bool isDocumentation, string content)
		{
			if (isDocumentation) {
				output.Write("'''", TextTokenType.XmlDocTag);
				output.WriteXmlDoc(content);
				output.WriteLine();
			}
			else
				output.WriteLine("'" + content, TextTokenType.Comment);
		}
		
		public void MarkFoldStart()
		{
			output.MarkFoldStart();
		}
		
		public void MarkFoldEnd()
		{
			output.MarkFoldEnd();
		}
		
		private static bool IsDefinition(AstNode node)
		{
			return
				node is FieldDeclaration ||
				node is ConstructorDeclaration ||
				node is EventDeclaration ||
				node is DelegateDeclaration ||
				node is OperatorDeclaration||
				node is MemberDeclaration ||
				node is TypeDeclaration;
		}

		class DebugState
		{
			public List<AstNode> Nodes = new List<AstNode>();
			public List<ILRange> ExtraILRanges = new List<ILRange>();
			public TextLocation StartLocation;
		}
		readonly Stack<DebugState> debugStack = new Stack<DebugState>();
		public void DebugStart(AstNode node)
		{
			debugStack.Push(new DebugState { StartLocation = output.Location });
		}

		public void DebugHidden(object hiddenILRanges)
		{
			var list = hiddenILRanges as IList<ILRange>;
			if (list != null) {
				if (debugStack.Count > 0)
					debugStack.Peek().ExtraILRanges.AddRange(list);
			}
		}

		public void DebugExpression(AstNode node)
		{
			if (debugStack.Count > 0)
				debugStack.Peek().Nodes.Add(node);
		}

		static readonly IEnumerable<ILRange> emptyILRange = new ILRange[0];
		public void DebugEnd(AstNode node)
		{
			var state = debugStack.Pop();
			if (currentMemberMapping != null) {
				foreach (var range in ILRange.OrderAndJoint(GetILRanges(state))) {
					currentMemberMapping.MemberCodeMappings.Add(
						new SourceCodeMapping {
							ILInstructionOffset = range,
							StartLocation = state.StartLocation,
							EndLocation = output.Location,
							MemberMapping = currentMemberMapping
						});
				}
			}
			else if (multiMappings != null) {
				foreach (var mm in multiMappings) {
					foreach (var range in ILRange.OrderAndJoint(mm.Item2)) {
						mm.Item1.MemberCodeMappings.Add(
							new SourceCodeMapping {
								ILInstructionOffset = range,
								StartLocation = state.StartLocation,
								EndLocation = output.Location,
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
			foreach (var range in state.ExtraILRanges)
				yield return range;
		}
	}
}
