// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Parser
{
	/// <summary>
	/// Lexer interface
	/// </summary>
	public interface ILexer : IDisposable
	{
		/// <summary>
		/// Sets the start line/column number. This method can be called only before the first token is read.
		/// </summary>
		void SetInitialLocation(Location location);
		
		/// <summary>
		/// Sets the context of the lexer.
		/// </summary>
		void SetInitialContext(SnippetType context);
		
		Errors Errors {
			get;
		}
		
		/// <summary>
		/// The current Token. <seealso cref="ICSharpCode.NRefactory.Parser.Token"/>
		/// </summary>
		Token Token {
			get;
		}
		
		/// <summary>
		/// The next Token (The <see cref="Token"/> after <see cref="NextToken"/> call) . <seealso cref="ICSharpCode.NRefactory.Parser.Token"/>
		/// </summary>
		Token LookAhead {
			get;
		}
		
		/// <summary>
		/// Special comment tags are tags like TODO, HACK or UNDONE which are read by the lexer and stored in <see cref="TagComments"/>.
		/// </summary>
		string[] SpecialCommentTags {
			get;
			set;
		}
		
		/// <summary>
		/// Gets/Sets if the lexer should skip adding comments to the special tracker. Set this
		/// property to true to improve lexing performance.
		/// </summary>
		bool SkipAllComments {
			get;
			set;
		}
		
		/// <summary>
		/// Gets/Sets if the lexer should evaluate conditional compilation symbols.
		/// </summary>
		bool EvaluateConditionalCompilation { get; set; }
		
		/// <summary>
		/// The dictionary with the conditional compilation symbols.
		/// C# ignores the value (you can use null), it just cares whether a symbol is defined.
		/// </summary>
		IDictionary<string, object> ConditionalCompilationSymbols { get; }
		
		/// <summary>
		/// Sets the conditional compilation symbols. 
		/// </summary>
		/// <param name="symbols">
		/// A <see cref="System.String"/> containing the symbols. The symbols are separated by ';'.
		/// </param>
		void SetConditionalCompilationSymbols (string symbols);
		
		/// <summary>
		/// Returns the comments that had been read and containing tag key words.
		/// </summary>
		List<TagComment> TagComments {
			get;
		}
		
		SpecialTracker SpecialTracker {
			get;
		}
		
		void StartPeek();
		
		/// <summary>
		/// Gives back the next token. A second call to Peek() gives the next token after the last call for Peek() and so on.
		/// </summary>
		/// <returns>An <see cref="Token"/> object.</returns>
		Token Peek();
		
		/// <summary>
		/// Reads the next token and gives it back.
		/// </summary>
		/// <returns>An <see cref="Token"/> object.</returns>
		Token NextToken();
		
		/// <summary>
		/// Skips to the end of the current code block.
		/// For this, the lexer must have read the next token AFTER the token opening the
		/// block (so that Lexer.Token is the block-opening token, not Lexer.LookAhead).
		/// After the call, Lexer.LookAhead will be the block-closing token.
		/// </summary>
		void SkipCurrentBlock(int targetToken);
		
		/// <summary>
		/// Used to export the current state of the lexer. The exported state should be
		/// complete, so that it is possible to reset the lexer to a previous state completely.
		/// </summary>
		LexerMemento Export();
		
		/// <summary>
		/// Is fired by the lexer as soon as a savepoint is reached.
		/// The Export-method can be used to retrieve the current state.
		/// </summary>
		event EventHandler<SavepointEventArgs> SavepointReached;
	}
}
