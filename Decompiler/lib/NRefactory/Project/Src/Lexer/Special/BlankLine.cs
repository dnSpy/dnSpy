// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.NRefactory
{
	public class BlankLine : AbstractSpecial
	{
		public BlankLine(Location point) : base(point)
		{
		}
		
		public override object AcceptVisitor(ISpecialVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
