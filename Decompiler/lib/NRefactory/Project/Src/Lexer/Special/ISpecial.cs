// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.NRefactory
{
	/// <summary>
	/// Interface for all specials.
	/// </summary>
	public interface ISpecial
	{
		Location StartPosition { get; }
		Location EndPosition { get; }
		
		object AcceptVisitor(ISpecialVisitor visitor, object data);
	}
	
	public interface ISpecialVisitor
	{
		object Visit(ISpecial special, object data);
		object Visit(BlankLine special, object data);
		object Visit(Comment special, object data);
		object Visit(PreprocessingDirective special, object data);
	}
	
	public abstract class AbstractSpecial : ISpecial
	{
		public abstract object AcceptVisitor(ISpecialVisitor visitor, object data);
		
		Location startPosition, endPosition;
		
		protected AbstractSpecial(Location position)
		{
			this.startPosition = position;
			this.endPosition = position;
		}
		
		protected AbstractSpecial(Location startPosition, Location endPosition)
		{
			this.startPosition = startPosition;
			this.endPosition = endPosition;
		}
		
		public Location StartPosition {
			get {
				return startPosition;
			}
			set {
				startPosition = value;
			}
		}
		
		public Location EndPosition {
			get {
				return endPosition;
			}
			set {
				endPosition = value;
			}
		}
		
		public override string ToString()
		{
			return String.Format("[{0}: Start = {1}, End = {2}]",
			                     GetType().Name, StartPosition, EndPosition);
		}
	}
}
