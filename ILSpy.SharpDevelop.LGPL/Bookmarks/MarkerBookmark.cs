// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.ILSpy.AvalonEdit;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Bookmarks
{
	public abstract class MarkerBookmark : BookmarkBase
	{
		public MarkerBookmark(IMemberRef member, TextLocation location, TextLocation endLocation) : base(member, location, endLocation)
		{
		}
		
		public ITextMarker Marker { get; set; }
		
		public abstract ITextMarker CreateMarker(ITextMarkerService markerService, int offset, int length);
	}
}
