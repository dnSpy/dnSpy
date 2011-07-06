// 
// NodeOutputChange.cs
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// This is the node that should be outputted at a position.
	/// </summary>
	public class NodeOutput
	{
		public readonly Dictionary<AstNode, Segment> NodeSegments = new Dictionary<AstNode, Segment> ();
			
		public string Text {
			get;
			set;
		}

		public void Trim ()
		{
			for (int i = 0; i < Text.Length; i++) {
				char ch = Text [i];
				if (ch != ' ' && ch != '\t') {
					if (i > 0) {
						Text = Text.Substring (i);
						foreach (var seg in NodeSegments.Values) {
							seg.Offset -= i;
						}
					}
					break;
				}
			}
		}
		
		public class Segment : ISegment
		{
			public int Offset {
				get;
				set;
			}

			public int Length {
				get {
					return EndOffset - Offset;
				}
			}

			public int EndOffset {
				get;
				set;
			}
			
			public Segment (int offset)
			{
				this.Offset = offset;
			}
		}
	}
	
		
	/// <summary>
	/// Outputs an Ast node at a specific offset.
	/// </summary>
	public abstract class NodeOutputAction : TextReplaceAction
	{
		public NodeOutput NodeOutput {
			get;
			private set;
		}
		
		public override string InsertedText {
			get {
				return NodeOutput.Text;
			}
			set {
				throw new NotSupportedException ("Changing text with this propery is not supported on NodeOutputChange.");
			}
		}
		
		public NodeOutputAction (int offset, int removedChars, NodeOutput output) : base (offset, removedChars)
		{
			if (output == null)
				throw new ArgumentNullException ("output");
			this.NodeOutput = output;
		}
	}
	
	
	/// <summary>
	/// An (Offset,Length)-pair.
	/// </summary>
	public interface ISegment
	{
		/// <summary>
		/// Gets the start offset of the segment.
		/// </summary>
		int Offset { get; }
		
		/// <summary>
		/// Gets the length of the segment.
		/// </summary>
		/// <remarks>Must not be negative.</remarks>
		int Length { get; }
		
		/// <summary>
		/// Gets the end offset of the segment.
		/// </summary>
		/// <remarks>EndOffset = Offset + Length;</remarks>
		int EndOffset { get; }
	}
}
