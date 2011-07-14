// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB
{
//	/// <summary>
//	/// Interface for all specials.
//	/// </summary>
//	public interface ISpecial
//	{
//		Location StartPosition { get; }
//		Location EndPosition { get; }
//		
//		object AcceptVisitor(ISpecialVisitor visitor, object data);
//	}
//	
//	public interface ISpecialVisitor
//	{
//		object Visit(ISpecial special, object data);
//		object Visit(BlankLine special, object data);
//		object Visit(Comment special, object data);
//		object Visit(PreprocessingDirective special, object data);
//	}
//	
//	public abstract class AbstractSpecial : ISpecial
//	{
//		public abstract object AcceptVisitor(ISpecialVisitor visitor, object data);
//		
//		protected AbstractSpecial(Location position)
//		{
//			this.StartPosition = position;
//			this.EndPosition = position;
//		}
//		
//		protected AbstractSpecial(Location startPosition, Location endPosition)
//		{
//			this.StartPosition = startPosition;
//			this.EndPosition = endPosition;
//		}
//		
//		public Location StartPosition { get; set; }
//		public Location EndPosition { get; set; }
//		
//		public override string ToString()
//		{
//			return String.Format("[{0}: Start = {1}, End = {2}]",
//			                     GetType().Name, StartPosition, EndPosition);
//		}
//	}
}
