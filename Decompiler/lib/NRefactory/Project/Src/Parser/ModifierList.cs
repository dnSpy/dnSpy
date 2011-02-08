// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Parser
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
			if(location.X == -1 && location.Y == -1) {
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
			if(location.X == -1 && location.Y == -1) {
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
