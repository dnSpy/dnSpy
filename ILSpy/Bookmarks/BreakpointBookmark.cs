// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
{
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
					Redraw();
				}
			}
		}
		
		public event EventHandler IsEnabledChanged;
		
		public BreakpointBookmark(IMemberRef member, TextLocation location, TextLocation endLocation, ILRange range)
			: base(member, location, endLocation)
		{
			this.MethodKey = new MethodKey(member);
			this.ILRange = range;
		}
		
		public override ImageSource Image {
			get {
		    return IsEnabled ? Images.Breakpoint : Images.DisabledBreakpoint;
			}
		}
		
		public event EventHandler ImageChanged;
		
		public override ITextMarker CreateMarker(ITextMarkerService markerService)
		{
			ITextMarker marker = CreateMarkerInternal(markerService);
			marker.ZOrder = ZOrder;
			marker.HighlightingColor = () => IsEnabled ? HighlightingColor : DisabledHighlightingColor;
			marker.IsVisible = b => {
				var cm = DebugInformation.CodeMappings;
				return cm != null && b is BreakpointBookmark && cm.ContainsKey(((BreakpointBookmark)b).MethodKey);
			};
			marker.Bookmark = this;
			this.Marker = marker;
			return marker;
		}

		protected override void Redraw()
		{
			base.Redraw();
			var marker = this.Marker;
			if (marker != null)
				marker.Redraw();
		}
	}
}
