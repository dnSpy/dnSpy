// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.ComponentModel;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Represents a collapsed line section.
	/// Use the Uncollapse() method to uncollapse the section.
	/// </summary>
	public sealed class CollapsedLineSection
	{
		DocumentLine start, end;
		HeightTree heightTree;
		
		#if DEBUG
		internal string ID;
		static int nextId;
		#else
		const string ID = "";
		#endif
		
		internal CollapsedLineSection(HeightTree heightTree, DocumentLine start, DocumentLine end)
		{
			this.heightTree = heightTree;
			this.start = start;
			this.end = end;
			#if DEBUG
			unchecked {
				this.ID = " #" + (nextId++);
			}
			#endif
		}
		
		/// <summary>
		/// Gets if the document line is collapsed.
		/// This property initially is true and turns to false when uncollapsing the section.
		/// </summary>
		public bool IsCollapsed {
			get { return start != null; }
		}
		
		/// <summary>
		/// Gets the start line of the section.
		/// When the section is uncollapsed or the text containing it is deleted,
		/// this property returns null.
		/// </summary>
		public DocumentLine Start {
			get { return start; }
			internal set { start = value; }
		}
		
		/// <summary>
		/// Gets the end line of the section.
		/// When the section is uncollapsed or the text containing it is deleted,
		/// this property returns null.
		/// </summary>
		public DocumentLine End {
			get { return end; }
			internal set { end = value; }
		}
		
		/// <summary>
		/// Uncollapses the section.
		/// This causes the Start and End properties to be set to null!
		/// Does nothing if the section is already uncollapsed.
		/// </summary>
		public void Uncollapse()
		{
			if (start == null)
				return;
			
			heightTree.Uncollapse(this);
			#if DEBUG
			heightTree.CheckProperties();
			#endif
			
			start = null;
			end = null;
		}
		
		/// <summary>
		/// Gets a string representation of the collapsed section.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
		public override string ToString()
		{
			return "[CollapsedSection" + ID + " Start=" + (start != null ? start.LineNumber.ToString() : "null")
				+ " End=" + (end != null ? end.LineNumber.ToString() : "null") + "]";
		}
	}
}
