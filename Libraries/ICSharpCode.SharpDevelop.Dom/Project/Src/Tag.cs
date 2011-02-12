// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	public sealed class TagComment : Immutable
	{
		string key;
		
		public string Key {
			get {
				return key;
			}
		}
		
		string commentString;
		DomRegion region;
		
		public string CommentString {
			get {
				return commentString;
			}
		}
		
		public DomRegion Region {
			get {
				return region;
			}
		}
		
		public TagComment(string key, DomRegion region)
		{
			this.key = key;
			this.region = region;
		}
		
		public TagComment(string key, DomRegion region, string commentString)
		{
			this.key = key;
			this.region = region;
			this.commentString = commentString;
		}
	}
}
