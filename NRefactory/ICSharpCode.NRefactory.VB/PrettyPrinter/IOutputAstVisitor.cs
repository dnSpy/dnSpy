// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.PrettyPrinter
{
//	public interface IOutputDomVisitor : IAstVisitor
//	{
//		event Action<INode> BeforeNodeVisit;
//		event Action<INode> AfterNodeVisit;
//		
//		string Text {
//			get;
//		}
//		
//		Errors Errors {
//			get;
//		}
//		
//		AbstractPrettyPrintOptions Options {
//			get;
//		}
//		IOutputFormatter OutputFormatter {
//			get;
//		}
//	}
	
	public interface IOutputFormatter
	{
		int IndentationLevel {
			get;
			set;
		}
		string Text {
			get;
		}
		bool IsInMemberBody {
			get;
			set;
		}
		void NewLine();
		void Indent();
//		void PrintComment(Comment comment, bool forceWriteInPreviousBlock);
//		void PrintPreprocessingDirective(PreprocessingDirective directive, bool forceWriteInPreviousBlock);
		void PrintBlankLine(bool forceWriteInPreviousBlock);
	}
}
