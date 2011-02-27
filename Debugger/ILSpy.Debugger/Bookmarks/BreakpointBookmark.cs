// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using ILSpy.Debugger.AvalonEdit;
using ILSpy.Debugger.Services;

namespace ILSpy.Debugger.Bookmarks
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
		string tooltip;
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
		
		public string Tooltip {
			get { return tooltip; }
			set { tooltip = value; }
		}
		
		public BreakpointBookmark(string typeName, AstLocation location, BreakpointAction action, DecompiledLanguages language) : base(typeName, location)
		{
			this.action = action;
			this.tooltip = language.ToString();
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
			return marker;
		}
	}
}
