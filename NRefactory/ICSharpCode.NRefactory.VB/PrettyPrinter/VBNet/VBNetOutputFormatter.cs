// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Parser;

namespace ICSharpCode.NRefactory.VB.PrettyPrinter
{
	/// <summary>
	/// Description of VBNetOutputFormatter.
	/// </summary>
	public sealed class VBNetOutputFormatter : AbstractOutputFormatter
	{
		public VBNetOutputFormatter(VBNetPrettyPrintOptions prettyPrintOptions) : base(prettyPrintOptions)
		{
		}
		
		public override void PrintToken(int token)
		{
			PrintText(Tokens.GetTokenString(token));
		}
		
		public override void PrintIdentifier(string identifier)
		{
			if (Keywords.IsNonIdentifierKeyword(identifier)) {
				PrintText("[");
				PrintText(identifier);
				PrintText("]");
			} else {
				PrintText(identifier);
			}
		}
		
//		public override void PrintComment(Comment comment, bool forceWriteInPreviousBlock)
//		{
//			switch (comment.CommentType) {
//				case CommentType.Block:
//					WriteLineInPreviousLine("'" + comment.CommentText.Replace("\n", "\n'"), forceWriteInPreviousBlock);
//					break;
//				case CommentType.Documentation:
//					WriteLineInPreviousLine("'''" + comment.CommentText, forceWriteInPreviousBlock);
//					break;
//				default:
//					WriteLineInPreviousLine("'" + comment.CommentText, forceWriteInPreviousBlock);
//					break;
//			}
//		}
		
//		public override void PrintPreprocessingDirective(PreprocessingDirective directive, bool forceWriteInPreviousBlock)
//		{
//			if (IsInMemberBody
//			    && (string.Equals(directive.Cmd, "#Region", StringComparison.InvariantCultureIgnoreCase)
//			        || string.Equals(directive.Cmd, "#End", StringComparison.InvariantCultureIgnoreCase)
//			        && directive.Arg.StartsWith("Region", StringComparison.InvariantCultureIgnoreCase)))
//			{
//				WriteLineInPreviousLine("'" + directive.Cmd + " " + directive.Arg, forceWriteInPreviousBlock);
//			} else if (!directive.Expression.IsNull) {
//				VBNetOutputVisitor visitor = new VBNetOutputVisitor();
//				directive.Expression.AcceptVisitor(visitor, null);
//				WriteLineInPreviousLine(directive.Cmd + " " + visitor.Text + " Then", forceWriteInPreviousBlock);
//			} else {
//				base.PrintPreprocessingDirective(directive, forceWriteInPreviousBlock);
//			}
//		}
		
		public void PrintLineContinuation()
		{
			if (!LastCharacterIsWhiteSpace)
				Space();
			PrintText("_" + Environment.NewLine);
		}
	}
}
