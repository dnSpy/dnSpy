// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy.Debugger.AvalonEdit;
using ICSharpCode.ILSpy.Debugger.Services;
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
		
		public DecompiledLanguages Language { get; private set; }
		
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
					Redraw();
				}
			}
		}
		
		public event EventHandler IsEnabledChanged;
		
		public string Tooltip { get; private set; }
		
		public BreakpointBookmark(MemberReference member, AstLocation location, ILRange range, BreakpointAction action, DecompiledLanguages language) : base(member, location)
		{
			this.action = action;
			this.ILRange = range;
			this.Tooltip = string.Format("Language:{0}, Line:{1}, IL range:{2}-{3}", language.ToString(), location.Line, range.From, range.To);
			this.Language = language;			
		}
		
		public override ImageSource Image {
			get {
				return ImageService.Breakpoint;
			}
		}
		
		public override ITextMarker CreateMarker(ITextMarkerService markerService, int offset, int length)
		{
			ITextMarker marker = markerService.Create(offset, length);
			marker.BackgroundColor = Color.FromRgb(180, 38, 38);
			marker.ForegroundColor = Colors.White;
			marker.IsVisible = b => b is MarkerBookmark && DebugData.DecompiledMemberReferences != null &&
				DebugData.DecompiledMemberReferences.ContainsKey(((MarkerBookmark)b).MemberReference.MetadataToken.ToInt32());
			marker.Bookmark = this;
			this.Marker = marker;
			
			return marker;
		}
	}
}
