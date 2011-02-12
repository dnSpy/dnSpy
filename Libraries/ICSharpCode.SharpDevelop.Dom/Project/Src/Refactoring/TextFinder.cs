// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom.Refactoring
{
	public struct TextFinderMatch
	{
		public readonly int Position;
		public readonly int Length;
		
		public readonly int ResolvePosition;
		
		public static readonly TextFinderMatch Empty = new TextFinderMatch(-1, 0);
		
		public TextFinderMatch(int position, int length)
		{
			this.Position = position;
			this.Length = length;
			this.ResolvePosition = position;
		}
		
		public TextFinderMatch(int position, int length, int resolvePosition)
		{
			this.Position = position;
			this.Length = length;
			this.ResolvePosition = resolvePosition;
		}
	}
	
	public abstract class TextFinder
	{
		public virtual string PrepareInputText(string inputText)
		{
			return inputText;
		}
		
		public abstract TextFinderMatch Find(string inputText, int startPosition);
	}
}
