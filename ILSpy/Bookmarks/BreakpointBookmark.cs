// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Windows.Media;
using dnlib.DotNet;
using dnSpy.Bookmarks;
using dnSpy.Images;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks {
	public class BreakpointBookmark : MarkerBookmark
	{
		public static HighlightingColor HighlightingColor = new HighlightingColor {
			Background = new SimpleHighlightingBrush(Color.FromRgb(0xB4, 0x26, 0x26)),
			Foreground = new SimpleHighlightingBrush(Colors.White),
		};
		public static HighlightingColor DisabledHighlightingColor = HighlightingColor;
		bool isEnabled = true;
		
		/// <summary>
		/// Gets the function/method where the breakpoint is set.
		/// <remarks>
		/// In case of methods, it is the same as the MemberReference metadata token.<br/>
		/// In case of properties and events, it's the GetMethod/SetMethod|AddMethod/RemoveMethod token.
		/// </remarks>
		/// </summary>
		public MethodKey MethodKey { get; private set; }
		
		public ILRange ILRange { get; private set; }
		
		public virtual bool IsEnabled {
			get {
				return isEnabled;
			}
			set {
				if (isEnabled != value) {
					isEnabled = value;
					if (IsEnabledChanged != null)
						IsEnabledChanged(this, EventArgs.Empty);
					if (ImageChanged != null)
					    ImageChanged(this, EventArgs.Empty); // Image property reflects IsEnabled property
					Modified();
					Redraw();
				}
			}
		}
		
		public event EventHandler IsEnabledChanged;
		
		public BreakpointBookmark(IMemberRef member, TextLocation location, TextLocation endLocation, ILRange range, bool isEnabled = true)
			: base(member, range.From, location, endLocation)
		{
			var key = MethodKey.Create(member);
			Debug.Assert(key != null, "Caller must verify that MethodKey.Create() won't fail");
			this.MethodKey = key.Value;
			this.ILRange = range;
			this.isEnabled = isEnabled;
		}

		public override int ZOrder {
			get { return (int)TextMarkerZOrder.Breakpoint; }
		}

		public override bool HasImage {
			get { return true; }
		}
		
		public override ImageSource GetImage(Color bgColor)
		{
			return IsEnabled ?
				ImageCache.Instance.GetImage("Breakpoint", bgColor) :
				ImageCache.Instance.GetImage("DisabledBreakpoint", bgColor);
		}
		
		public event EventHandler ImageChanged;

		public override bool IsVisible(DecompilerTextView textView)
		{
			var key = MethodKey.Create(MemberReference);
			uint ilOffset = ILRange.From;
			TextLocation location, endLocation;
			var cm = textView == null ? null : textView.CodeMappings;
			if (cm == null || key == null || !cm.ContainsKey(key.Value))
				return false;
			if (!cm[key.Value].GetInstructionByTokenAndOffset((uint)ilOffset, out location, out endLocation))
				return false;

			return true;
		}
		
		public override ITextMarker CreateMarker(ITextMarkerService markerService, DecompilerTextView textView)
		{
			ITextMarker marker = CreateMarkerInternal(markerService, textView);
			var cm = textView == null ? null : textView.CodeMappings;
			marker.ZOrder = ZOrder;
			marker.HighlightingColor = () => IsEnabled ? HighlightingColor : DisabledHighlightingColor;
			marker.IsVisible = b => cm != null && b is BreakpointBookmark && cm.ContainsKey(((BreakpointBookmark)b).MethodKey);
			marker.Bookmark = this;
			return marker;
		}

		protected override void Redraw()
		{
			base.Redraw();
			foreach (var marker in Markers.Values) {
				if (marker != null)
					marker.Redraw();
			}
		}
	}
}
