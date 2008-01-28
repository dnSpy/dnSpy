// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Parser
{
	public abstract class AbstractParser : IParser
	{
		protected const int    MinErrDist   = 2;
		protected const string ErrMsgFormat = "-- line {0} col {1}: {2}";  // 0=line, 1=column, 2=text
		
		
		private Errors errors;
		private ILexer lexer;
		
		protected int    errDist = MinErrDist;
		
		[CLSCompliant(false)]
		protected CompilationUnit compilationUnit;
		
		bool parseMethodContents = true;
		
		public bool ParseMethodBodies {
			get {
				return parseMethodContents;
			}
			set {
				parseMethodContents = value;
			}
		}
		
		public ILexer Lexer {
			get {
				return lexer;
			}
		}
		
		public Errors Errors {
			get {
				return errors;
			}
		}
		
		public CompilationUnit CompilationUnit {
			get {
				return compilationUnit;
			}
		}
		
		internal AbstractParser(ILexer lexer)
		{
			this.errors = lexer.Errors;
			this.lexer  = lexer;
			errors.SynErr = new ErrorCodeProc(SynErr);
		}
		
		public abstract void Parse();
		
		public abstract Expression ParseExpression();
		public abstract BlockStatement ParseBlock();
		public abstract List<INode> ParseTypeMembers();
		
		protected abstract void SynErr(int line, int col, int errorNumber);
		
		protected void SynErr(int n)
		{
			if (errDist >= MinErrDist) {
				errors.SynErr(lexer.LookAhead.line, lexer.LookAhead.col, n);
			}
			errDist = 0;
		}
		
		protected void SemErr(string msg)
		{
			if (errDist >= MinErrDist) {
				errors.Error(lexer.Token.line, lexer.Token.col, msg);
			}
			errDist = 0;
		}
		
		protected void Expect(int n)
		{
			if (lexer.LookAhead.kind == n) {
				lexer.NextToken();
			} else {
				SynErr(n);
			}
		}
		
		#region System.IDisposable interface implementation
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
		public void Dispose()
		{
			errors = null;
			if (lexer != null) {
				lexer.Dispose();
			}
			lexer = null;
		}
		#endregion
	}
}
