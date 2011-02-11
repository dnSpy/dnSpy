// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.Parser.VB
{
	public class XmlModeInfo : ICloneable
	{
		public bool inXmlTag, inXmlCloseTag, isDocumentStart;
		public int level;
		
		public XmlModeInfo(bool isSpecial)
		{
			level = isSpecial ? -1 : 0;
			inXmlTag = inXmlCloseTag = isDocumentStart = false;
		}
		
		public object Clone()
		{
			return new XmlModeInfo(false) {
				inXmlCloseTag = this.inXmlCloseTag,
				inXmlTag = this.inXmlTag,
				isDocumentStart = this.isDocumentStart,
				level = this.level
			};
		}
	}
}
