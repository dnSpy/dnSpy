// 
// IMemberProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Completion
{
	public interface ICompletionContextProvider
	{
		IList<string> ConditionalSymbols {
			get;
		}

		void GetCurrentMembers (int offset, out IUnresolvedTypeDefinition currentType, out IUnresolvedMember currentMember);

		Tuple<string, TextLocation> GetMemberTextToCaret(int caretOffset, IUnresolvedTypeDefinition currentType, IUnresolvedMember currentMember);

		CSharpAstResolver GetResolver (CSharpResolver resolver, AstNode rootNode);
	}

	public class DefaultCompletionContextProvider : ICompletionContextProvider
	{
		readonly IDocument document;
		readonly CSharpUnresolvedFile unresolvedFile;
		readonly List<string> symbols = new List<string> ();

		public IList<string> ConditionalSymbols {
			get {
				return symbols;
			}
		}

		public DefaultCompletionContextProvider (IDocument document, CSharpUnresolvedFile unresolvedFile)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			if (unresolvedFile == null)
				throw new ArgumentNullException("unresolvedFile");
			this.document = document;
			this.unresolvedFile = unresolvedFile;
		}

		public void AddSymbol (string sym)
		{
			symbols.Add (sym);
		}
		public void GetCurrentMembers(int offset, out IUnresolvedTypeDefinition currentType, out IUnresolvedMember currentMember)
		{
			//var document = engine.document;
			var location = document.GetLocation(offset);
			
			currentType = null;
			
			foreach (var type in unresolvedFile.TopLevelTypeDefinitions) {
				if (type.Region.Begin < location)
					currentType = type;
			}
			currentType = FindInnerType (currentType, location);
			
			// location is beyond last reported end region, now we need to check, if the end region changed
			if (currentType != null && currentType.Region.End < location) {
				if (!IsInsideType (currentType, location))
					currentType = null;
			}
			currentMember = null;
			if (currentType != null) {
				foreach (var member in currentType.Members) {
					if (member.Region.Begin < location && (currentMember == null || currentMember.Region.Begin < member.Region.Begin))
						currentMember = member;
				}
			}
			
			// location is beyond last reported end region, now we need to check, if the end region changed
			// NOTE: Enums are a special case, there the "last" field needs to be treated as current member
			if (currentMember != null && currentMember.Region.End < location && currentType.Kind != TypeKind.Enum) {
				if (!IsInsideType (currentMember, location))
					currentMember = null;
			}/*
			var stack = GetBracketStack (engine.GetMemberTextToCaret ().Item1);
			if (stack.Count == 0)
				currentMember = null;*/
		}

		IUnresolvedTypeDefinition FindInnerType (IUnresolvedTypeDefinition parent, TextLocation location)
		{
			if (parent == null)
				return null;
			var currentType = parent;
			foreach (var type in parent.NestedTypes) {
				if (type.Region.Begin < location  && location < type.Region.End)
					currentType = FindInnerType (type, location);
			}
			
			return currentType;
		}
		
		bool IsInsideType (IUnresolvedEntity currentType, TextLocation location)
		{
			int startOffset = document.GetOffset (currentType.Region.Begin);
			int endOffset = document.GetOffset (location);
			//bool foundEndBracket = false;
		
			var bracketStack = new Stack<char> ();
		
			bool isInString = false, isInChar = false;
			bool isInLineComment = false, isInBlockComment = false;
			
			for (int i = startOffset; i < endOffset; i++) {
				char ch = document.GetCharAt (i);
				switch (ch) {
					case '(':
					case '[':
					case '{':
						if (!isInString && !isInChar && !isInLineComment && !isInBlockComment)
							bracketStack.Push (ch);
						break;
					case ')':
					case ']':
					case '}':
						if (!isInString && !isInChar && !isInLineComment && !isInBlockComment)
						if (bracketStack.Count > 0)
							bracketStack.Pop ();
						break;
					case '\r':
					case '\n':
						isInLineComment = false;
						break;
					case '/':
						if (isInBlockComment) {
							if (i > 0 && document.GetCharAt (i - 1) == '*') 
								isInBlockComment = false;
						} else if (!isInString && !isInChar && i + 1 < document.TextLength) {
							char nextChar = document.GetCharAt (i + 1);
							if (nextChar == '/')
								isInLineComment = true;
							if (!isInLineComment && nextChar == '*')
								isInBlockComment = true;
						}
						break;
					case '"':
						if (!(isInChar || isInLineComment || isInBlockComment)) 
							isInString = !isInString;
						break;
					case '\'':
						if (!(isInString || isInLineComment || isInBlockComment)) 
							isInChar = !isInChar;
						break;
					default :
						break;
					}
				}
			return bracketStack.Any (t => t == '{');
		}
	
		public Tuple<string, TextLocation> GetMemberTextToCaret(int caretOffset, IUnresolvedTypeDefinition currentType, IUnresolvedMember currentMember)
		{
			int startOffset;
			if (currentMember != null && currentType != null && currentType.Kind != TypeKind.Enum) {
				startOffset = document.GetOffset(currentMember.Region.Begin);
			} else if (currentType != null) {
				startOffset = document.GetOffset(currentType.Region.Begin);
			} else {
				startOffset = 0;
			}
			while (startOffset > 0) {
				char ch = document.GetCharAt(startOffset - 1);
				if (ch != ' ' && ch != '\t') {
					break;
				}
				--startOffset;
			}

			return Tuple.Create (document.GetText (startOffset, caretOffset - startOffset), document.GetLocation (startOffset));
		}


		public CSharpAstResolver GetResolver (CSharpResolver resolver, AstNode rootNode)
		{
			return new CSharpAstResolver (resolver, rootNode, unresolvedFile);
		}


	}
}

