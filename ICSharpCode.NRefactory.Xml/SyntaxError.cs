/*
 * Created by SharpDevelop.
 * User: Daniel
 * Date: 2/20/2012
 * Time: 17:46
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// A syntax error.
	/// </summary>
	public class SyntaxError : ISegment
	{
		readonly int startOffset;
		readonly int endOffset;
		readonly string description;
		
		/// <summary>
		/// Creates a new syntax error.
		/// </summary>
		public SyntaxError(int startOffset, int endOffset, string description)
		{
			if (description == null)
				throw new ArgumentNullException("description");
			this.startOffset = startOffset;
			this.endOffset = endOffset;
			this.description = description;
		}
		
		/// <summary>
		/// Gets a description of the syntax error.
		/// </summary>
		public string Description {
			get { return description; }
		}
		
		/// <summary>
		/// Gets the start offset of the segment.
		/// </summary>
		public int StartOffset {
			get { return startOffset; }
		}
		
		int ISegment.Offset {
			get { return startOffset; }
		}
		
		/// <inheritdoc/>
		public int Length {
			get { return endOffset - startOffset; }
		}
		
		/// <inheritdoc/>
		public int EndOffset {
			get { return endOffset; }
		}
	}
}
