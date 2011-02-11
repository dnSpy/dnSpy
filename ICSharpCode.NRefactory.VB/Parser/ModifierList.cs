// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.NRefactory.VB.Dom;

namespace ICSharpCode.NRefactory.VB.Parser
{
	internal class ModifierList
	{
		Modifiers cur;
		Location location = new Location(-1, -1);
		
		public Modifiers Modifier {
			get {
				return cur;
			}
		}
		
		public Location GetDeclarationLocation(Location keywordLocation)
		{
			if(location.IsEmpty) {
				return keywordLocation;
			}
			return location;
		}
		
//		public Location Location {
//			get {
//				return location;
//			}
//			set {
//				location = value;
//			}
//		}
		
		public bool isNone { get { return cur == Modifiers.None; } }
		
		public bool Contains(Modifiers m)
		{
			return ((cur & m) != 0);
		}
		
		public void Add(Modifiers m, Location tokenLocation) 
		{
			if(location.IsEmpty) {
				location = tokenLocation;
			}
			
			if ((cur & m) == 0) {
				cur |= m;
			} else {
//				parser.Error("modifier " + m + " already defined");
			}
		}
		
//		public void Add(Modifiers m)
//		{
//			Add(m.cur, m.Location);
//		}
		
		public void Check(Modifiers allowed)
		{
			Modifiers wrong = cur & ~allowed;
			if (wrong != Modifiers.None) {
//				parser.Error("modifier(s) " + wrong + " not allowed here");
			}
		}
	}
}
