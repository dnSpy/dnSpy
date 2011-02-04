// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// A &lt;SyntaxDefinition&gt; element.
	/// </summary>
	[Serializable]
	public class XshdSyntaxDefinition
	{
		/// <summary>
		/// Creates a new XshdSyntaxDefinition object.
		/// </summary>
		public XshdSyntaxDefinition()
		{
			this.Elements = new NullSafeCollection<XshdElement>();
			this.Extensions = new NullSafeCollection<string>();
		}
		
		/// <summary>
		/// Gets/sets the definition name
		/// </summary>
		public string Name { get; set; }
		
		/// <summary>
		/// Gets the associated extensions.
		/// </summary>
		public IList<string> Extensions { get; private set; }
		
		/// <summary>
		/// Gets the collection of elements.
		/// </summary>
		public IList<XshdElement> Elements { get; private set; }
		
		/// <summary>
		/// Applies the visitor to all elements.
		/// </summary>
		public void AcceptElements(IXshdVisitor visitor)
		{
			foreach (XshdElement element in Elements) {
				element.AcceptVisitor(visitor);
			}
		}
	}
}
