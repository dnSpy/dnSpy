// 
// Indent.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	public enum IndentType
	{
		Block,
		DoubleBlock,
		Continuation,
		Alignment,
		Label,
		Empty
	}

	public class Indent
	{
		readonly CloneableStack<IndentType> indentStack = new CloneableStack<IndentType>();
		readonly TextEditorOptions options;
		int curIndent;
		int extraSpaces;
		string indentString;

		public int CurIndent {
			get {
				return curIndent;
			}
		}

		public Indent(TextEditorOptions options)
		{
			this.options = options;
			Reset();
		}

		Indent(Indent engine)
		{
			this.indentStack = engine.indentStack.Clone();
			this.options = engine.options;
			this.curIndent = engine.curIndent;
			this.extraSpaces = engine.extraSpaces;
			this.indentString = engine.indentString;
		}

		public Indent Clone()
		{
			return new Indent(this);
		}

		public void Reset()
		{
			curIndent = 0;
			indentString = "";
			indentStack.Clear();
		}

		public void Push(IndentType type)
		{
			indentStack.Push(type);
			curIndent += GetIndent(type);
			Update();
		}

		public void Push(Indent indent)
		{
			foreach (var i in indent.indentStack)
				Push(i);
		}

		public void Pop()
		{
			curIndent -= GetIndent(indentStack.Pop());
			Update();
		}

		public bool PopIf(IndentType type)
		{
			if (Count > 0 && Peek() == type)
			{
				Pop();
				return true;
			}

			return false;
		}

		public void PopWhile(IndentType type)
		{
			while (Count > 0 && Peek() == type)
			{
				Pop();
			}
		}

		public bool PopTry()
		{
			if (Count > 0)
			{
				Pop();
				return true;
			}

			return false;
		}

		public int Count {
			get {
				return indentStack.Count;
			}
		}

		public IndentType Peek()
		{
			return indentStack.Peek();
		}

		int GetIndent(IndentType indentType)
		{
			switch (indentType) {
				case IndentType.Block:
					return options.IndentSize;
				case IndentType.DoubleBlock:
					return options.IndentSize * 2;
				case IndentType.Alignment:
				case IndentType.Continuation:
					return options.ContinuationIndent;
				case IndentType.Label:
					return options.LabelIndent;
				case IndentType.Empty:
					return 0;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void Update()
		{
			if (options.TabsToSpaces) {
				indentString = new string(' ', curIndent + ExtraSpaces);
				return;
			}
			indentString = new string('\t', curIndent / options.TabSize) + new string(' ', curIndent % options.TabSize) + new string(' ', ExtraSpaces);
		}

		public int ExtraSpaces {
			get {
				return extraSpaces;
			}
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException("ExtraSpaces >= 0 but was " + value);
				extraSpaces = value;
				Update();
			}
		}


		public string IndentString {
			get {
				return indentString;
			}
		}

		public override string ToString()
		{
			return string.Format("[Indent: curIndent={0}]", curIndent);
		}

		public Indent GetIndentWithoutSpace ()
		{
			var result = new Indent(options);
			foreach (var i in indentStack)
					result.Push(i);
			return result;
		}

		public static Indent ConvertFrom(string indentString, Indent correctIndent, TextEditorOptions options = null)
		{
			options = options ?? TextEditorOptions.Default;
			var result = new Indent(options);

			var indent = string.Concat(indentString.Where(c => c == ' ' || c == '\t'));
			var indentTypes = new Stack<IndentType>(correctIndent.indentStack);

			foreach (var _ in indent.TakeWhile(c => c == '\t'))
			{
				if (indentTypes.Count > 0)
					result.Push(indentTypes.Pop());
				else
					result.Push(IndentType.Continuation);
			}

			result.ExtraSpaces = indent
				.SkipWhile(c => c == '\t')
				.TakeWhile(c => c == ' ')
				.Count();

			return result;
		}

		public void RemoveAlignment()
		{
			ExtraSpaces = 0;
			if (Count > 0 && Peek() == IndentType.Alignment)
				Pop();
		}

		public void SetAlignment(int i, bool forceSpaces = false)
		{
			var alignChars = Math.Max(0, i);
			if (forceSpaces) {
				ExtraSpaces = alignChars;
				return;
			}
			RemoveAlignment();
			Push(IndentType.Alignment);
		}

	}
}
