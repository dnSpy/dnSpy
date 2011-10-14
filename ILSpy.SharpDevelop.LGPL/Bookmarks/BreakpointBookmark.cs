// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Media;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.SharpDevelop;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
{
	public enum BreakpointAction
	{
		Break,
		Trace,
		Condition
	}
	
	public class BreakpointBookmark : MarkerBookmark
	{
		bool isHealthy = true;
		bool isEnabled = true;
		BreakpointAction action = BreakpointAction.Break;
		
		public BreakpointAction Action {
			get {
				return action;
			}
			set {
				if (action != value) {
					action = value;
					Redraw();
				}
			}
		}
		
		/// <summary>
		/// Gets the function/method where the breakpoint is set.
		/// <remarks>
		/// In case of methods, it is the same as the MemberReference metadata token.<br/>
		/// In case of properties and events, it's the GetMethod/SetMethod|AddMethod/RemoveMethod token.
		/// </remarks>
		/// </summary>
		public int FunctionToken { get; private set; }
		
		public ILRange ILRange { get; private set; }
		
		public virtual bool IsHealthy {
			get {
				return isHealthy;
			}
			set {
				if (isHealthy != value) {
					isHealthy = value;
					Redraw();
				}
			}
		}
		
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
		
		public string Tooltip { get; private set; }
		
		public BreakpointBookmark(MemberReference member, TextLocation location, int functionToken, ILRange range, BreakpointAction action)
			: base(member, location)
		{
			this.action = action;
			this.FunctionToken = functionToken;
			this.ILRange = range;
			this.Tooltip = string.Format("Line:{0}, IL range:{1}-{2}", location.Line, range.From, range.To);
		}
		
		public override ImageSource Image {
			get {
		    return IsEnabled ? Images.Breakpoint : Images.DisabledBreakpoint;
			}
		}
		
		public event EventHandler ImageChanged;
		
		public override ITextMarker CreateMarker(ITextMarkerService markerService, int offset, int length)
		{
			ITextMarker marker = markerService.Create(offset, length);
			marker.BackgroundColor = Color.FromRgb(180, 38, 38);
			marker.ForegroundColor = Colors.White;
			marker.IsVisible = b => b is BreakpointBookmark && DebugInformation.CodeMappings != null &&
				DebugInformation.CodeMappings.ContainsKey(((BreakpointBookmark)b).FunctionToken);
			marker.Bookmark = this;
			this.Marker = marker;
			
			return marker;
		}
	}
}
