// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Debugger;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Bookmarks
{
	/// <summary>
	/// A bookmark that can be attached to an AvalonEdit TextDocument.
	/// </summary>
	public abstract class BookmarkBase : IBookmark
	{
		TextLocation location;
		TextLocation endLocation;

		public static int GetLineNumber(IBookmark b, DecompilerTextView textView)
		{
			var bm = b as BookmarkBase;
			if (bm != null)
				return bm.GetLineNumber(textView);
			return b.LineNumber;
		}

		protected void Modified()
		{
			if (OnModified != null)
				OnModified(this, EventArgs.Empty);
		}
		public event EventHandler OnModified;
		
		/// <summary>
		/// Start location. DON'T USE this if you need an exact location. This is valid in ONE of
		/// the active text views. Use <see cref="GetLocation"/> instead.
		/// </summary>
		public TextLocation Location {
			get { return location; }
			set {
				if (location != value) {
					location = value;
					Modified();
				}
			}
		}
		
		/// <summary>
		/// End location. DON'T USE this if you need an exact location. This is valid in ONE of
		/// the active text views. Use <see cref="GetLocation"/> instead.
		/// </summary>
		public TextLocation EndLocation {
			get { return endLocation; }
			set {
				if (endLocation != value) {
					endLocation = value;
					Modified();
				}
			}
		}
		
		protected virtual void Redraw()
		{
		}
		
		public IMemberRef MemberReference { get; private set; }
		
		/// <summary>
		/// Line number. DON'T USE this if you need an exact location. This is valid in ONE of
		/// the active text views. Use <see cref="GetLineNumber"/> instead.
		/// </summary>
		public int LineNumber {
			get { return location.Line; }
		}

		public abstract int ZOrder { get; }

		/// <summary>
		/// true if the bookmark is visible in the displayed document
		/// </summary>
		public virtual bool IsVisible(DecompilerTextView textView) {
			return true;
		}
		
		/// <summary>
		/// Gets if the bookmark can be toggled off using the 'set/unset bookmark' command.
		/// </summary>
		public virtual bool CanToggle {
			get {
				return true;
			}
		}

		public uint ILOffset {
			get { return ilOffset; }
		}
		readonly uint ilOffset;
		
		public BookmarkBase(IMemberRef member, uint ilOffset, TextLocation location, TextLocation endLocation)
		{
			this.MemberReference = member;
			this.ilOffset = ilOffset;
			this.Location = location;
			this.EndLocation = endLocation;
		}

		public virtual bool HasImage {
			get { return false; }
		}
		
		public virtual ImageSource GetImage(Color bgColor)
		{
			return null;
		}
		
		public virtual void MouseDown(MouseButtonEventArgs e)
		{
		}
		
		public virtual void MouseUp(MouseButtonEventArgs e)
		{
		}

		/// <summary>
		/// Gets line number. Returns -1 if it's unknown
		/// </summary>
		/// <param name="textView"></param>
		/// <returns></returns>
		public int GetLineNumber(DecompilerTextView textView)
		{
			TextLocation location, endLocation;
			if (GetLocation(textView, out location, out endLocation))
				return location.Line;
			return -1;
		}

		public TextLocation GetLocation(DecompilerTextView textView)
		{
			TextLocation location, endLocation;
			if (GetLocation(textView, out location, out endLocation))
				return location;
			return new TextLocation();
		}

		public bool GetLocation(DecompilerTextView textView, out TextLocation location, out TextLocation endLocation)
		{
			var cm = textView == null ? null : textView.CodeMappings;
			MemberMapping mapping;
			var key = MethodKey.Create(MemberReference);
			if (cm == null || key == null || !cm.TryGetValue(key.Value, out mapping)) {
				location = endLocation = new TextLocation();
				return false;
			}

			bool isMatch;
			SourceCodeMapping map = mapping.GetInstructionByOffset(ilOffset, out isMatch);

			location = map.StartLocation;
			endLocation = map.EndLocation;
			return true;
		}

		protected ITextMarker CreateMarkerInternal(ITextMarkerService markerService, DecompilerTextView textView)
		{
			TextLocation location, endLocation;
			if (!GetLocation(textView, out location, out endLocation))
				throw new InvalidOperationException();

			var line = markerService.TextView.Document.GetLineByNumber(location.Line);
			var endLine = markerService.TextView.Document.GetLineByNumber(endLocation.Line);
			int startOffset = line.Offset + location.Column - 1;
			int endOffset = endLine.Offset + endLocation.Column - 1;

			return markerService.Create(startOffset, endOffset - startOffset);
		}

		public void UpdateLocation(TextLocation location, TextLocation endLocation)
		{
			if (this.location == location && this.endLocation == endLocation)
				return;
			this.location = location;
			this.endLocation = endLocation;
			Modified();
		}
	}
}
