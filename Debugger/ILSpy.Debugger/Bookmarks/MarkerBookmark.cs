// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.CSharp;
using ILSpy.Debugger.AvalonEdit;

namespace ILSpy.Debugger.Bookmarks
{
	public abstract class MarkerBookmark : BookmarkBase
	{
		public MarkerBookmark(string typeName, AstLocation location) : base(typeName, location)
		{
		}
		
		public ITextMarker Marker { get; set; }
		
		public abstract ITextMarker CreateMarker(ITextMarkerService markerService, int offset, int length);
	}
}
