// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.TextView;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Bookmarks
{
	public abstract class MarkerBookmark : BookmarkBase
	{
		public MarkerBookmark(IMemberRef member, uint ilOffset, TextLocation location, TextLocation endLocation) : base(member, ilOffset, location, endLocation)
		{
			this.Markers = new Dictionary<object, ITextMarker>();
		}

		public Dictionary<object, ITextMarker> Markers { get; private set; }

		public abstract ITextMarker CreateMarker(ITextMarkerService markerService, DecompilerTextView textView);
	}
}
