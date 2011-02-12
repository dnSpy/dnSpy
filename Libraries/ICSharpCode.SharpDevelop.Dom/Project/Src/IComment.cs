// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

namespace ICSharpCode.SharpDevelop.Dom
{
	public interface IComment
	{
		bool IsBlockComment {
			get;
		}
		
		string CommentTag {
			get;
		}
		
		string CommentText {
			get;
		}
		
		DomRegion Region {
			get;
		}
	}
}
