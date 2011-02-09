// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.NRefactory.Parser
{
	public class SpecialTracker
	{
		List<ISpecial> currentSpecials = new List<ISpecial>();
		
		CommentType   currentCommentType;
		StringBuilder sb = new StringBuilder();
		Location         startPosition;
		
		public List<ISpecial> CurrentSpecials {
			get {
				return currentSpecials;
			}
		}
		
		public void InformToken(int kind)
		{
			
		}
		
		/// <summary>
		/// Gets the specials from the SpecialTracker and resets the lists.
		/// </summary>
		public List<ISpecial> RetrieveSpecials()
		{
			List<ISpecial> tmp = currentSpecials;
			currentSpecials = new List<ISpecial>();
			return tmp;
		}
		
		public void AddEndOfLine(Location point)
		{
			currentSpecials.Add(new BlankLine(point));
		}
		
		public void AddPreprocessingDirective(string cmd, string arg, Location start, Location end)
		{
			currentSpecials.Add(new PreprocessingDirective(cmd, arg, start, end));
		}
		
		// used for comment tracking
		public void StartComment(CommentType commentType, Location startPosition)
		{
			this.currentCommentType = commentType;
			this.startPosition      = startPosition;
			this.sb.Length          = 0;
		}
		
		public void AddChar(char c)
		{
			sb.Append(c);
		}
		
		public void AddString(string s)
		{
			sb.Append(s);
		}
		
		public void FinishComment(Location endPosition)
		{
			currentSpecials.Add(new Comment(currentCommentType, sb.ToString(), startPosition, endPosition));
		}
	}
}
