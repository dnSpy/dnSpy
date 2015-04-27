// 
// Script.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Mono.CSharp;
using ITypeDefinition = ICSharpCode.NRefactory.TypeSystem.ITypeDefinition;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Class for creating change scripts.
	/// 'Original document' = document without the change script applied.
	/// 'Current document' = document with the change script (as far as it is already created) applies.
	/// </summary>
	public abstract class Script : IDisposable
	{
		internal struct Segment : ISegment
		{
			readonly int offset;
			readonly int length;
			
			public int Offset {
				get { return offset; }
			}
			
			public int Length {
				get { return length; }
			}
			
			public int EndOffset {
				get { return Offset + Length; }
			}
			
			public Segment (int offset, int length)
			{
				this.offset = offset;
				this.length = length;
			}
			
			public override string ToString ()
			{
				return string.Format ("[Script.Segment: Offset={0}, Length={1}, EndOffset={2}]", Offset, Length, EndOffset);
			}
		}
		
		readonly CSharpFormattingOptions formattingOptions;
		readonly TextEditorOptions options;
		readonly Dictionary<AstNode, ISegment> segmentsForInsertedNodes = new Dictionary<AstNode, ISegment>();
		
		protected Script(CSharpFormattingOptions formattingOptions, TextEditorOptions options)
		{
			if (formattingOptions == null)
				throw new ArgumentNullException("formattingOptions");
			if (options == null)
				throw new ArgumentNullException("options");
			this.formattingOptions = formattingOptions;
			this.options = options;
		}
		
		/// <summary>
		/// Given an offset in the original document (at the start of script execution),
		/// returns the offset in the current document.
		/// </summary>
		public abstract int GetCurrentOffset(int originalDocumentOffset);
		
		/// <summary>
		/// Given an offset in the original document (at the start of script execution),
		/// returns the offset in the current document.
		/// </summary>
		public abstract int GetCurrentOffset(TextLocation originalDocumentLocation);
		
		/// <summary>
		/// Creates a tracked segment for the specified (offset,length)-segment.
		/// Offset is interpreted to be an offset in the current document.
		/// </summary>
		/// <returns>
		/// A segment that initially has the specified values, and updates
		/// on every <see cref="Replace(int,int,string)"/> call.
		/// </returns>
		protected abstract ISegment CreateTrackedSegment(int offset, int length);

		/// <summary>
		/// Gets the current text segment of the specified AstNode.
		/// </summary>
		/// <param name="node">The node to get the segment for.</param>
		public ISegment GetSegment(AstNode node)
		{
			ISegment segment;
			if (segmentsForInsertedNodes.TryGetValue(node, out segment))
				return segment;
			if (node.StartLocation.IsEmpty || node.EndLocation.IsEmpty) {
				throw new InvalidOperationException("Trying to get the position of a node that is not part of the original document and was not inserted");
			}
			int startOffset = GetCurrentOffset(node.StartLocation);
			int endOffset = GetCurrentOffset(node.EndLocation);
			return new Segment(startOffset, endOffset - startOffset);
		}
		
		/// <summary>
		/// Replaces text.
		/// </summary>
		/// <param name="offset">The starting offset of the text to be replaced.</param>
		/// <param name="length">The length of the text to be replaced.</param>
		/// <param name="newText">The new text.</param>
		public abstract void Replace (int offset, int length, string newText);
		
		public void InsertText(int offset, string newText)
		{
			Replace(offset, 0, newText);
		}
		
		public void RemoveText(int offset, int length)
		{
			Replace(offset, length, "");
		}
		
		public CSharpFormattingOptions FormattingOptions {
			get { return formattingOptions; }
		}
		
		public TextEditorOptions Options {
			get { return options; }
		}
		
		public void InsertBefore(AstNode node, AstNode newNode)
		{
			var startOffset = GetCurrentOffset(new TextLocation(node.StartLocation.Line, 1));
			var output = OutputNode (GetIndentLevelAt (startOffset), newNode);
			string text = output.Text;
			if (!(newNode is Expression || newNode is AstType))
				text += Options.EolMarker;
			InsertText(startOffset, text);
			output.RegisterTrackedSegments(this, startOffset);
			CorrectFormatting (node, newNode);
		}

		public void InsertAfter(AstNode node, AstNode newNode)
		{
            var indentLevel = IndentLevelFor(node);
            var output = OutputNode(indentLevel, newNode);
            string text =  PrefixFor(node, newNode) + output.Text;

            var insertOffset = GetCurrentOffset(node.EndLocation);
            InsertText(insertOffset, text);
            output.RegisterTrackedSegments(this, insertOffset);
            CorrectFormatting (node, newNode);
		}

	    private int IndentLevelFor(AstNode node)
	    {
            if (!DoesInsertingAfterRequireNewline(node))
	            return 0;
	        
            return GetIndentLevelAt(GetCurrentOffset(new TextLocation(node.StartLocation.Line, 1)));
	    }

	    bool DoesInsertingAfterRequireNewline(AstNode node)
	    {
            if (node is Expression)
                return false;

            if (node is AstType)
                return false;

	        if (node is ParameterDeclaration)
	            return false;

	        var token = node as CSharpTokenNode;
	        if (token != null && token.Role == Roles.LPar)
	            return false;
	        
	        return true;
	    }

	    private string PrefixFor(AstNode node, AstNode newNode)
	    {
	        if (DoesInsertingAfterRequireNewline(node))
	            return Options.EolMarker;

	        if (newNode is ParameterDeclaration && node is ParameterDeclaration)
	            //todo: worry about adding characters to the document without matching AstNode's. 
	            return ", ";

	        return String.Empty;
	    }

	    public void AddTo(BlockStatement bodyStatement, AstNode newNode)
		{
			var startOffset = GetCurrentOffset(bodyStatement.LBraceToken.EndLocation);
			var output = OutputNode(1 + GetIndentLevelAt(startOffset), newNode, true);
			InsertText(startOffset, output.Text);
			output.RegisterTrackedSegments(this, startOffset);
			CorrectFormatting (null, newNode);
		}
	
		public void AddTo(TypeDeclaration typeDecl, EntityDeclaration entityDecl)
		{
			var startOffset = GetCurrentOffset(typeDecl.LBraceToken.EndLocation);
			var output = OutputNode(1 + GetIndentLevelAt(startOffset), entityDecl, true);
			InsertText(startOffset, output.Text);
			output.RegisterTrackedSegments(this, startOffset);
			CorrectFormatting (null, entityDecl);
		}
		
		/// <summary>
		/// Changes the modifier of a given entity declaration.
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <param name="modifiers">The new modifiers.</param>
		public void ChangeModifier(EntityDeclaration entity, Modifiers modifiers)
		{
			var dummyEntity = new MethodDeclaration ();
			dummyEntity.Modifiers = modifiers;

			int offset;
			int endOffset;

			if (entity.ModifierTokens.Any ()) {
				offset = GetCurrentOffset(entity.ModifierTokens.First ().StartLocation);
				endOffset = GetCurrentOffset(entity.ModifierTokens.Last ().GetNextSibling (s => s.Role != Roles.NewLine && s.Role != Roles.Whitespace).StartLocation);
			} else {
				var child = entity.FirstChild;
				while (child.NodeType == NodeType.Whitespace ||
				       child.Role == EntityDeclaration.AttributeRole ||
				       child.Role == Roles.NewLine) {
					child = child.NextSibling;
				}
				offset = endOffset = GetCurrentOffset(child.StartLocation);
			}

			var sb = new StringBuilder();
			foreach (var modifier in dummyEntity.ModifierTokens) {
				sb.Append(modifier.ToString());
				sb.Append(' ');
			}

			Replace(offset, endOffset - offset, sb.ToString());
		}

		public void ChangeModifier(ParameterDeclaration param, ParameterModifier modifier)
		{
			var child = param.FirstChild;
			Func<AstNode, bool> pred = s => s.Role == ParameterDeclaration.RefModifierRole || s.Role == ParameterDeclaration.OutModifierRole || s.Role == ParameterDeclaration.ParamsModifierRole || s.Role == ParameterDeclaration.ThisModifierRole;
			if (!pred(child))
				child = child.GetNextSibling(pred); 

			int offset;
			int endOffset;

			if (child != null) {
				offset = GetCurrentOffset(child.StartLocation);
				endOffset = GetCurrentOffset(child.GetNextSibling (s => s.Role != Roles.NewLine && s.Role != Roles.Whitespace).StartLocation);
			} else {
				offset = endOffset = GetCurrentOffset(param.Type.StartLocation);
			}
			string modString;
			switch (modifier) {
				case ParameterModifier.None:
					modString = "";
					break;
				case ParameterModifier.Ref:
					modString = "ref ";
					break;
				case ParameterModifier.Out:
					modString = "out ";
					break;
				case ParameterModifier.Params:
					modString = "params ";
					break;
				case ParameterModifier.This:
					modString = "this ";
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			Replace(offset, endOffset - offset, modString);
		}

		/// <summary>
		/// Changes the base types of a type declaration.
		/// </summary>
		/// <param name="type">The type declaration to modify.</param>
		/// <param name="baseTypes">The new base types.</param>
		public void ChangeBaseTypes(TypeDeclaration type, IEnumerable<AstType> baseTypes)
		{
			var dummyType = new TypeDeclaration();
			dummyType.BaseTypes.AddRange(baseTypes);

			int offset;
			int endOffset;
			var sb = new StringBuilder();

			if (type.BaseTypes.Any ()) {
				offset = GetCurrentOffset(type.ColonToken.StartLocation);
				endOffset = GetCurrentOffset(type.BaseTypes.Last ().EndLocation);
			} else {
				sb.Append(' ');
				if (type.TypeParameters.Any()) {
					offset = endOffset = GetCurrentOffset(type.RChevronToken.EndLocation);
				} else {
					offset = endOffset = GetCurrentOffset(type.NameToken.EndLocation);
				}
			}

			if (dummyType.BaseTypes.Any()) {
				sb.Append(": ");
				sb.Append(string.Join(", ", dummyType.BaseTypes));
			}

			Replace(offset, endOffset - offset, sb.ToString());
			FormatText(type);
		}


		/// <summary>
		/// Adds an attribute section to a given entity.
		/// </summary>
		/// <param name="entity">The entity to add the attribute to.</param>
		/// <param name="attr">The attribute to add.</param>
		public void AddAttribute(EntityDeclaration entity, AttributeSection attr)
		{
			var node = entity.FirstChild;
			while (node.NodeType == NodeType.Whitespace || node.Role == Roles.Attribute) {
				node = node.NextSibling;
			}
			InsertBefore(node, attr);
		}

		public virtual Task Link (params AstNode[] nodes)
		{
			// Default implementation: do nothing
			// Derived classes are supposed to enter the text editor's linked state.
			
			// Immediately signal the task as completed:
			var tcs = new TaskCompletionSource<object>();
			tcs.SetResult(null);
			return tcs.Task;
		}

		public virtual Task Link (IEnumerable<AstNode> nodes)
		{
			return Link(nodes.ToArray());
		}
		
		public void Replace (AstNode node, AstNode replaceWith)
		{
			var segment = GetSegment (node);
			int startOffset = segment.Offset;
			int level = 0;
			if (!(replaceWith is Expression) && !(replaceWith is AstType))
				level = GetIndentLevelAt (startOffset);
			NodeOutput output = OutputNode (level, replaceWith);
			output.TrimStart ();
			Replace (startOffset, segment.Length, output.Text);
			output.RegisterTrackedSegments(this, startOffset);
			CorrectFormatting (node, node);
		}

		List<AstNode> nodesToFormat = new List<AstNode> ();

		void CorrectFormatting(AstNode node, AstNode newNode)
		{
			if (node is Identifier || node is IdentifierExpression || node is CSharpTokenNode || node is AstType)
				return;
			if (node == null || node.Parent is BlockStatement) {
				nodesToFormat.Add (newNode); 
			} else {
				nodesToFormat.Add ((node.Parent != null && (node.Parent is Statement || node.Parent is Expression || node.Parent is VariableInitializer)) ? node.Parent : newNode); 
			}
		}
		
		public abstract void Remove (AstNode node, bool removeEmptyLine = true);

		/// <summary>
		/// Safely removes an attribue from it's section (removes empty sections).
		/// </summary>
		/// <param name="attr">The attribute to be removed.</param>
		public void RemoveAttribute(Attribute attr)
		{
			AttributeSection section = (AttributeSection)attr.Parent;
			if (section.Attributes.Count == 1) {
				Remove(section);
				return;
			}

			var newSection = (AttributeSection)section.Clone();
			int i = 0;
			foreach (var a in section.Attributes) {
				if (a == attr)
					break;
				i++;
			}
			newSection.Attributes.Remove (newSection.Attributes.ElementAt (i));
			Replace(section, newSection);
		}
		
		public abstract void FormatText (IEnumerable<AstNode> nodes);

		public void FormatText (params AstNode[] nodes)
		{
			FormatText ((IEnumerable<AstNode>)nodes);
		}

		public virtual void Select (AstNode node)
		{
			// default implementation: do nothing
			// Derived classes are supposed to set the text editor's selection
		}

		public virtual void Select (TextLocation start, TextLocation end)
		{
			// default implementation: do nothing
			// Derived classes are supposed to set the text editor's selection
		}

		public virtual void Select (int startOffset, int endOffset)
		{
			// default implementation: do nothing
			// Derived classes are supposed to set the text editor's selection
		}

		
		public enum InsertPosition
		{
			Start,
			Before,
			After,
			End
		}
		
		public virtual Task<Script> InsertWithCursor(string operation, InsertPosition defaultPosition, IList<AstNode> nodes)
		{
			throw new NotImplementedException();
		}
		
		public virtual Task<Script> InsertWithCursor(string operation, ITypeDefinition parentType, Func<Script, RefactoringContext, IList<AstNode>> nodeCallback)
		{
			throw new NotImplementedException();
		}
		
		public Task<Script> InsertWithCursor(string operation, InsertPosition defaultPosition, params AstNode[] nodes)
		{
			return InsertWithCursor(operation, defaultPosition, (IList<AstNode>)nodes);
		}

		public Task<Script> InsertWithCursor(string operation, ITypeDefinition parentType, Func<Script, RefactoringContext, AstNode> nodeCallback)
		{
			return InsertWithCursor(operation, parentType, (Func<Script, RefactoringContext, IList<AstNode>>)delegate (Script s, RefactoringContext ctx) {
				return new AstNode[] { nodeCallback(s, ctx) };
			});
		}
		
		protected virtual int GetIndentLevelAt (int offset)
		{
			return 0;
		}
		
		sealed class SegmentTrackingTokenWriter : TextWriterTokenWriter
		{
			internal List<KeyValuePair<AstNode, Segment>> NewSegments = new List<KeyValuePair<AstNode, Segment>>();
			readonly Stack<int> startOffsets = new Stack<int>();
			readonly StringWriter stringWriter;
			
			public SegmentTrackingTokenWriter(StringWriter stringWriter)
				: base(stringWriter)
			{
				this.stringWriter = stringWriter;
			}
			
			public override void WriteIdentifier (Identifier identifier)
			{
				int startOffset = stringWriter.GetStringBuilder ().Length;
				int endOffset = startOffset + (identifier.Name ?? "").Length + (identifier.IsVerbatim ? 1 : 0);
				NewSegments.Add(new KeyValuePair<AstNode, Segment>(identifier, new Segment(startOffset, endOffset - startOffset)));
				base.WriteIdentifier (identifier);
			}
			
			public override void StartNode (AstNode node)
			{
				base.StartNode (node);
				startOffsets.Push(stringWriter.GetStringBuilder ().Length);
			}
			
			public override void EndNode (AstNode node)
			{
				int startOffset = startOffsets.Pop();
				int endOffset = stringWriter.GetStringBuilder ().Length;
				NewSegments.Add(new KeyValuePair<AstNode, Segment>(node, new Segment(startOffset, endOffset - startOffset)));
				base.EndNode (node);
			}
		}
		
		protected NodeOutput OutputNode(int indentLevel, AstNode node, bool startWithNewLine = false)
		{
			var stringWriter = new StringWriter ();
			var formatter = new SegmentTrackingTokenWriter(stringWriter);
			formatter.Indentation = indentLevel;
			formatter.IndentationString = Options.TabsToSpaces ? new string (' ', Options.IndentSize) : "\t";
			stringWriter.NewLine = Options.EolMarker;
			if (startWithNewLine)
				formatter.NewLine ();
			var visitor = new CSharpOutputVisitor (formatter, formattingOptions);
			node.AcceptVisitor (visitor);
			string text = stringWriter.ToString().TrimEnd();
			return new NodeOutput(text, formatter.NewSegments);
		}
		
		protected class NodeOutput
		{
			string text;
			readonly List<KeyValuePair<AstNode, Segment>> newSegments;
			int trimmedLength;
			
			internal NodeOutput(string text, List<KeyValuePair<AstNode, Segment>> newSegments)
			{
				this.text = text;
				this.newSegments = newSegments;
			}
			
			public string Text {
				get { return text; }
			}
			
			public void TrimStart()
			{
				for (int i = 0; i < text.Length; i++) {
					char ch = text [i];
					if (ch != ' ' && ch != '\t') {
						if (i > 0) {
							text = text.Substring (i);
							trimmedLength = i;
						}
						break;
					}
				}
			}
			
			public void RegisterTrackedSegments(Script script, int insertionOffset)
			{
				foreach (var pair in newSegments) {
					int offset = insertionOffset + pair.Value.Offset - trimmedLength;
					ISegment trackedSegment = script.CreateTrackedSegment(offset, pair.Value.Length);
					script.segmentsForInsertedNodes.Add(pair.Key, trackedSegment);
				}
			}
		}
		
		/// <summary>
		/// Renames the specified symbol.
		/// </summary>
		/// <param name='symbol'>
		/// The symbol to rename
		/// </param>
		/// <param name='name'>
		/// The new name, if null the user is prompted for a new name.
		/// </param>
		public virtual void Rename(ISymbol symbol, string name = null)
		{
		}
		
		public virtual void DoGlobalOperationOn(IEnumerable<IEntity> entities, Action<RefactoringContext, Script, IEnumerable<AstNode>> callback, string operationDescription = null)
		{
		}

		public virtual void Dispose()
		{
			FormatText (nodesToFormat);
		}
		
		public enum NewTypeContext {
			/// <summary>
			/// The class should be placed in a new file to the current namespace.
			/// </summary>
			CurrentNamespace,
			
			/// <summary>
			/// The class should be placed in the unit tests. (not implemented atm.)
			/// </summary>
			UnitTests
		}
		
		/// <summary>
		/// Creates a new file containing the type, namespace and correct usings.
		/// (Note: Should take care of IDE specific things, file headers, add to project, correct name).
		/// </summary>
		/// <param name='newType'>
		/// New type to be created.
		/// </param>
		/// <param name='context'>
		/// The Context in which the new type should be created.
		/// </param>
		public virtual void CreateNewType(AstNode newType, NewTypeContext context = NewTypeContext.CurrentNamespace)
		{
		}
	}

	public static class ExtMethods
	{
		public static void ContinueScript (this Task task, Action act)
		{
			if (task.IsCompleted) {
				act();
			} else {
				task.ContinueWith(delegate {
					act();
				}, TaskScheduler.FromCurrentSynchronizationContext());
			}
		}

		public static void ContinueScript (this Task<Script> task, Action<Script> act)
		{
			if (task.IsCompleted) {
				act(task.Result);
			} else {
				task.ContinueWith(delegate {
					act(task.Result);
				}, TaskScheduler.FromCurrentSynchronizationContext());
			}
		}
	}
}
