//
// IndentState.cs
//
// Author:
//       Matej Miklečić <matej.miklecic@gmail.com>
//
// Copyright (c) 2013 Matej Miklečić (matej.miklecic@gmail.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp
{
	#region IndentState

	/// <summary>
	///     The base class for all indentation states. 
	///     Each state defines the logic for indentation based on chars that
	///     are pushed to it.
	/// </summary>
	public abstract class IndentState : ICloneable
	{
		#region Properties

		/// <summary>
		///     The indentation engine using this state.
		/// </summary>
		public CSharpIndentEngine Engine;

		/// <summary>
		///     The parent state. 
		///     This state can use the indentation levels of its parent.
		///     When this state exits, the engine returns to the parent.
		/// </summary>
		public IndentState Parent;

		/// <summary>
		///     The indentation of the current line.
		///     This is set when the state is created and will be changed to
		///     <see cref="NextLineIndent"/> when the <see cref="CSharpIndentEngine.newLineChar"/> 
		///     is pushed.
		/// </summary>
		public Indent ThisLineIndent;

		/// <summary>
		///     The indentation of the next line.
		///     This is set when the state is created and can change depending
		///     on the pushed chars.
		/// </summary>
		public Indent NextLineIndent;

		#endregion

		#region Constructors

		protected IndentState()
		{
		}

		/// <summary>
		///     Creates a new indentation state that is a copy of the given
		///     prototype.
		/// </summary>
		/// <param name="prototype">
		///     The prototype state.
		/// </param>
		/// <param name="engine">
		///     The engine of the new state.
		/// </param>
		protected IndentState(IndentState prototype, CSharpIndentEngine engine)
		{
			Engine = engine;
			Parent = prototype.Parent != null ? prototype.Parent.Clone(engine) : null;

			ThisLineIndent = prototype.ThisLineIndent.Clone();
			NextLineIndent = prototype.NextLineIndent.Clone();
		}

		#endregion

		#region IClonable

		object ICloneable.Clone()
		{
			return Clone(Engine);
		}

		public abstract IndentState Clone(CSharpIndentEngine engine);

		#endregion

		#region Methods

		internal void Initialize (CSharpIndentEngine engine, IndentState parent = null)
		{
			Parent = parent;
			Engine = engine;

			InitializeState();
		}

		/// <summary>
		///     Initializes the state:
		///       - sets the default indentation levels.
		/// </summary>
		/// <remarks>
		///     Each state can override this method if it needs a different
		///     logic for setting up the default indentations.
		/// </remarks>
		public virtual void InitializeState()
		{
			ThisLineIndent = new Indent(Engine.textEditorOptions);
			NextLineIndent = ThisLineIndent.Clone();
		}

		/// <summary>
		///     Actions performed when this state exits.
		/// </summary>
		public virtual void OnExit()
		{
			if (Parent != null)
			{
				// if a state exits on the newline character, it has to push
				// it back to its parent (and so on recursively if the parent 
				// state also exits). Otherwise, the parent state wouldn't
				// know that the engine isn't on the same line anymore.
				if (Engine.currentChar == Engine.newLineChar)
				{
					Parent.Push(Engine.newLineChar);
				}

				// when a state exits the engine stays on the same line and this
				// state has to override the Parent.ThisLineIndent.
				Parent.ThisLineIndent = ThisLineIndent.Clone();
			}
		}

		/// <summary>
		///     Changes the current state of the <see cref="CSharpIndentEngine"/> using the current
		///     state as the parent for the new one.
		/// </summary>
		/// <typeparam name="T">
		///     The type of the new state. Must be assignable from <see cref="IndentState"/>.
		/// </typeparam>
		public void ChangeState<T>()
			where T : IndentState, new ()
		{
			var t = new T();
			t.Initialize(Engine, Engine.currentState);
			Engine.currentState = t;
		}

		/// <summary>
		///     Exits this state by setting the current state of the
		///     <see cref="CSharpIndentEngine"/> to this state's parent.
		/// </summary>
		public void ExitState()
		{
			OnExit();
			Engine.currentState = Engine.currentState.Parent ?? new GlobalBodyState(Engine);
		}

		/// <summary>
		///     Common logic behind the push method.
		///     Each state can override this method and implement its own logic.
		/// </summary>
		/// <param name="ch">
		///     The current character that's being pushed.
		/// </param>
		public virtual void Push(char ch)
		{
			// replace ThisLineIndent with NextLineIndent if the newLineChar is pushed
			if (ch == Engine.newLineChar)
			{
				var delta = Engine.textEditorOptions.ContinuationIndent;
				while (NextLineIndent.CurIndent - ThisLineIndent.CurIndent > delta &&
					   NextLineIndent.PopIf(IndentType.Continuation)) ;
				ThisLineIndent = NextLineIndent.Clone();
			}
		}

		/// <summary>
		///     When derived, checks if the given sequence of chars form
		///     a valid keyword or variable name, depending on the state.
		/// </summary>
		/// <param name="keyword">
		///     A possible keyword.
		/// </param>
		public virtual void CheckKeyword(string keyword)
		{ }

		/// <summary>
		///     When derived, checks if the given sequence of chars form
		///     a valid keyword or variable name, depending on the state.
		/// </summary>
		/// <param name="keyword">
		///     A possible keyword.
		/// </param>
		/// <remarks>
		///     This method should be called from <see cref="Push(char)"/>.
		///     It is left to derived classes to call this method because of
		///     performance issues.
		/// </remarks>
		public virtual void CheckKeywordOnPush(string keyword)
		{ }

		#endregion
	}

	#endregion

	#region Null state

	/// <summary>
	///     Null state.
	/// </summary>
	/// <remarks>
	///     Doesn't define any transitions to new states.
	/// </remarks>
	public class NullState : IndentState
	{
		public NullState()
		{ }

		public NullState(NullState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override void Push(char ch)
		{ }

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new NullState(this, engine);
		}
	}

	#endregion

	#region Brackets body states

	#region Brackets body base

	/// <summary>
	///     The base for all brackets body states.
	/// </summary>
	/// <remarks>
	///     Represents a block of code between a pair of brackets.
	/// </remarks>
	public abstract class BracketsBodyBaseState : IndentState
	{
	
		/// <summary>
		///     When derived in a concrete bracket body state, represents
		///     the closed bracket character pair.
		/// </summary>
		public abstract char ClosedBracket { get; }

		protected BracketsBodyBaseState()
		{ }

		protected BracketsBodyBaseState(BracketsBodyBaseState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override void Push(char ch)
		{
			base.Push(ch);
			switch (ch) {
				case '#':
					if (Engine.isLineStart)
						ChangeState<PreProcessorState>();
					break;
				case '/':
					if (Engine.previousChar == '/')
						ChangeState<LineCommentState>();
					break;
				case '*':
					if (Engine.previousChar == '/')
						ChangeState<MultiLineCommentState>();
					break;
				case '"':
					if (Engine.previousChar == '@')
					{
						ChangeState<VerbatimStringState>();
					}
					else
					{
						ChangeState<StringLiteralState>();
					}
					break;
				case '\'':
					ChangeState<CharacterState>();
					break;
				case '{':
					ChangeState<BracesBodyState>();
					break;
				case '(':
					ChangeState<ParenthesesBodyState>();
					break;
				case '[':
					ChangeState<SquareBracketsBodyState>();
					break;
				default:
					if (ch == ClosedBracket)
						ExitState();
					break;
			}
		}
	}

	#endregion

	#region Braces body state

	/// <summary>
	///     Braces body state.
	/// </summary>
	/// <remarks>
	///     Represents a block of code between { and }.
	/// </remarks>
	public class BracesBodyState : BracketsBodyBaseState
	{
		/// <summary>
		///     Type of the current block body.
		/// </summary>
		public Body CurrentBody;

		/// <summary>
		///     Type of the next block body.
		///     Same as <see cref="CurrentBody"/> if none of the
		///     <see cref="Body"/> keywords have been read.
		/// </summary>
		public Body NextBody;

		/// <summary>
		///     Type of the current statement.
		/// </summary>
		public Statement CurrentStatement
		{
			get
			{
				return currentStatement;
			}
			set
			{
				// clear NestedIfStatementLevels if this statement breaks the sequence
				if (currentStatement == Statement.None && value != Statement.Else)
				{
					NestedIfStatementLevels.Clear();
				}

				currentStatement = value;
			}
		}
		Statement currentStatement;

		/// <summary>
		///    Contains indent levels of nested if statements.
		/// </summary>
		internal CloneableStack<Indent> NestedIfStatementLevels = new CloneableStack<Indent>();

		/// <summary>
		///    Contains the indent level of the last statement or body keyword.
		/// </summary>
		public Indent LastBlockIndent;

		/// <summary>
		///     True if the engine is on the right side of the equal operator '='.
		/// </summary>
		public bool IsRightHandExpression;

		/// <summary>
		///     True if the '=' char has been pushed and it's not
		///     a part of a relational operator (&gt;=, &lt;=, !=, ==).
		/// </summary>
		public bool IsEqualCharPushed;

		/// <summary>
		///     The indentation of the previous line.
		/// </summary>
		public int PreviousLineIndent;

		/// <summary>
		///     True if the dot member (e.g. method invocation) indentation has
		///     been handled in the current statement.
		/// </summary>
		public bool IsMemberReferenceDotHandled;

		public override char ClosedBracket
		{
			get { return '}'; }
		}

		public BracesBodyState()
		{
		}

		public BracesBodyState(BracesBodyState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			CurrentBody = prototype.CurrentBody;
			NextBody = prototype.NextBody;
			CurrentStatement = prototype.CurrentStatement;
			NestedIfStatementLevels = prototype.NestedIfStatementLevels.Clone();
			IsRightHandExpression = prototype.IsRightHandExpression;
			IsEqualCharPushed = prototype.IsEqualCharPushed;
			IsMemberReferenceDotHandled = prototype.IsMemberReferenceDotHandled;
			LastBlockIndent = prototype.LastBlockIndent;
			PreviousLineIndent = prototype.PreviousLineIndent;
		}

		public override void Push(char ch)
		{
			// handle IsRightHandExpression property
			if (IsEqualCharPushed)
			{
				if (IsRightHandExpression)
				{
					if (ch == Engine.newLineChar)
					{
						NextLineIndent.RemoveAlignment();
						NextLineIndent.Push(IndentType.Continuation);
					}
				}
				// ignore "==" and "=>" operators
				else if (ch != '=' && ch != '>')
				{
					IsRightHandExpression = true;

					if (ch == Engine.newLineChar)
					{
						NextLineIndent.Push(IndentType.Continuation);
					}
					else
					{
						NextLineIndent.SetAlignment(Engine.column - NextLineIndent.CurIndent);
					}
				}

				IsEqualCharPushed = ch == ' ' || ch == '\t';
			}

			if (ch == ';' || (ch == ',' && IsRightHandExpression))
			{
				OnStatementExit();
			}
			else if (ch == '=' && !(Engine.previousChar == '=' || Engine.previousChar == '<' || Engine.previousChar == '>' || Engine.previousChar == '!'))
			{
				IsEqualCharPushed = true;
			}
			else if (ch == '.' && !IsMemberReferenceDotHandled)
			{
				// OPTION: CSharpFormattingOptions.AlignToMemberReferenceDot
				if (Engine.formattingOptions.AlignToMemberReferenceDot && !Engine.isLineStart)
				{
					IsMemberReferenceDotHandled = true;
					NextLineIndent.RemoveAlignment();
					NextLineIndent.SetAlignment(Engine.column - NextLineIndent.CurIndent - 1, true);
				}
				else if (Engine.isLineStart)
				{
					IsMemberReferenceDotHandled = true;

					ThisLineIndent.RemoveAlignment();
					while (ThisLineIndent.CurIndent > PreviousLineIndent && 
					       ThisLineIndent.PopIf(IndentType.Continuation)) ;
					ThisLineIndent.Push(IndentType.Continuation);
					NextLineIndent = ThisLineIndent.Clone();
				}
			}
			else if (ch == ':' && Engine.isLineStart && !IsRightHandExpression)
			{
				// try to capture ': base(...)', ': this(...)' and inherit statements when they are on a new line
				ThisLineIndent.Push(IndentType.Continuation);
			}
			else if (ch == Engine.newLineChar)
			{
				PreviousLineIndent = ThisLineIndent.CurIndent;
			}

			if (Engine.wordToken.ToString() == "else")
			{
				CheckKeywordOnPush("else");
			}

			base.Push(ch);
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();

			// OPTION: IDocumentIndentEngine.EnableCustomIndentLevels
			var parent = Parent as BracesBodyState;
			if (parent == null || parent.LastBlockIndent == null || !Engine.EnableCustomIndentLevels)
			{
				NextLineIndent.RemoveAlignment();
				NextLineIndent.PopIf(IndentType.Continuation);
			}
			else
			{
				NextLineIndent = parent.LastBlockIndent.Clone();
			}

			if (Engine.isLineStart)
			{
				ThisLineIndent = NextLineIndent.Clone();
			}

			CurrentBody = extractBody(Parent);
			NextBody = Body.None;
			CurrentStatement = Statement.None;

			AddIndentation(CurrentBody);
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new BracesBodyState(this, engine);
		}

		public override void OnExit()
		{
			if (Parent is BracesBodyState && !((BracesBodyState)Parent).IsRightHandExpression)
			{
				((BracesBodyState)Parent).OnStatementExit();
			}

			if (Engine.isLineStart)
			{
				ThisLineIndent.RemoveAlignment();
				ThisLineIndent.PopTry();
				BraceStyle style;
				if (TryGetBraceStyle(this.CurrentBody, out style)) {
					if (style == BraceStyle.NextLineShifted ||
						style == BraceStyle.NextLineShifted2||
						style == BraceStyle.BannerStyle) {
						ThisLineIndent.Push(IndentType.Block);
					}
				}
			}

			base.OnExit();
		}

		/// <summary>
		///     Actions performed when the current statement exits.
		/// </summary>
		public virtual void OnStatementExit()
		{
			IsRightHandExpression = false;
			IsMemberReferenceDotHandled = false;

			NextLineIndent.RemoveAlignment();
			NextLineIndent.PopWhile(IndentType.Continuation);

			CurrentStatement = Statement.None;
			NextBody = Body.None;
			LastBlockIndent = null;
		}

		#region Helpers

		/// <summary>
		///     Types of braces bodies.
		/// </summary>
		public enum Body
		{
			None,
			Namespace,
			Class,
			Struct,
			Interface,
			Enum,
			Switch,
			Case,
			Try,
			Catch,
			Finally
		}

		/// <summary>
		///     Types of statements.
		/// </summary>
		public enum Statement
		{
			None,
			If,
			Else,
			Do,
			While,
			For,
			Foreach,
			Lock,
			Using,
			Return
		}

		static readonly Dictionary<string, Body> bodies = new Dictionary<string, Body>
		{
			{ "namespace", Body.Namespace },
			{ "class", Body.Class },
			{ "struct", Body.Struct },
			{ "interface", Body.Interface },
			{ "enum", Body.Enum },
			{ "switch", Body.Switch },
			{ "try", Body.Try },
			{ "catch", Body.Catch },
			{ "finally", Body.Finally },
		};

		static readonly Dictionary<string, Statement> statements = new Dictionary<string, Statement>
		{
			{ "if", Statement.If },
			// { "else", Statement.Else }, // should be handled in CheckKeywordAtPush
			{ "do", Statement.Do },
			{ "while", Statement.While },
			{ "for", Statement.For },
			{ "foreach", Statement.Foreach },
			{ "lock", Statement.Lock },
			{ "using", Statement.Using },
			{ "return", Statement.Return },
		};

		static readonly HashSet<string> blocks = new HashSet<string>
		{
			"namespace",
			"class",
			"struct",
			"interface",
			"enum",
			"switch",
			"try",
			"catch",
			"finally",
			"if",
			"else",
			"do",
			"while",
			"for",
			"foreach",
			"lock",
			"using",
		};

		readonly string[] caseDefaultKeywords = {
			"case",
			"default"
		};

		readonly string[] classStructKeywords = {
			"class",
			"struct"
		};

		/// <summary>
		///     Checks if the given string is a keyword and sets the
		///     <see cref="NextBody"/> and the <see cref="CurrentStatement"/>
		///     variables appropriately.
		/// </summary>
		/// <param name="keyword">
		///     A possible keyword.
		/// </param>
		/// <remarks>
		///     This method is called from <see cref="Push(char)"/>
		/// </remarks>
		public override void CheckKeywordOnPush(string keyword)
		{
			if (keyword == "else")
			{
				CurrentStatement = Statement.Else;

				// OPTION: CSharpFormattingOptions.AlignElseInIfStatements
				if (!Engine.formattingOptions.AlignElseInIfStatements && NestedIfStatementLevels.Count > 0)
				{
					ThisLineIndent = NestedIfStatementLevels.Pop().Clone();
					NextLineIndent = ThisLineIndent.Clone();
				}

				NextLineIndent.Push(IndentType.Continuation);
			}

			if (blocks.Contains(keyword) && Engine.NeedsReindent)
			{
				LastBlockIndent = Indent.ConvertFrom(Engine.CurrentIndent, ThisLineIndent, Engine.textEditorOptions);
			}
		}

		/// <summary>
		///     Checks if the given string is a keyword and sets the
		///     <see cref="NextBody"/> and the <see cref="CurrentStatement"/>
		///     variables appropriately.
		/// </summary>
		/// <param name="keyword">
		///     A possible keyword.
		/// </param>
		public override void CheckKeyword(string keyword)
		{
			if (bodies.ContainsKey(keyword))
			{
				var isKeywordTemplateConstraint =
					classStructKeywords.Contains(keyword) &&
					(NextBody == Body.Class || NextBody == Body.Struct || NextBody == Body.Interface);

				if (!isKeywordTemplateConstraint)
				{
					NextBody = bodies[keyword];
				}
			}
			else if (caseDefaultKeywords.Contains(keyword) && CurrentBody == Body.Switch && Engine.isLineStartBeforeWordToken)
			{
				ChangeState<SwitchCaseState>();
			}
			else if (keyword == "where" && Engine.isLineStartBeforeWordToken)
			{
				// try to capture where (generic type constraint)
				ThisLineIndent.Push(IndentType.Continuation);
			}
			else if (statements.ContainsKey(keyword))
			{
				Statement previousStatement = CurrentStatement;
				CurrentStatement = statements[keyword];

				// return if this is a using declaration or alias
				if (CurrentStatement == Statement.Using &&
				   (this is GlobalBodyState || CurrentBody == Body.Namespace))
				{
					return;
				}

				// OPTION: CSharpFormattingOptions.AlignEmbeddedIfStatements
				if (Engine.formattingOptions.AlignEmbeddedStatements &&
					previousStatement == Statement.If &&
					CurrentStatement == Statement.If)
				{
					ThisLineIndent.PopIf(IndentType.Continuation);
					NextLineIndent.PopIf(IndentType.Continuation);
				}

				// OPTION: CSharpFormattingOptions.AlignEmbeddedStatements
				if (Engine.formattingOptions.AlignEmbeddedStatements &&
					previousStatement == Statement.Lock &&
					CurrentStatement == Statement.Lock)
				{
					ThisLineIndent.PopIf(IndentType.Continuation);
					NextLineIndent.PopIf(IndentType.Continuation);
				}

				// OPTION: CSharpFormattingOptions.AlignEmbeddedUsingStatements
				if (Engine.formattingOptions.AlignEmbeddedStatements &&
					previousStatement == Statement.Using &&
					CurrentStatement == Statement.Using)
				{
					ThisLineIndent.PopIf(IndentType.Continuation);
					NextLineIndent.PopIf(IndentType.Continuation);
				}

				// only add continuation for 'else' in 'else if' statement.
				if (!(CurrentStatement == Statement.If && previousStatement == Statement.Else && !Engine.isLineStartBeforeWordToken))
				{
					NextLineIndent.Push(IndentType.Continuation);
				}

				if (CurrentStatement == Statement.If)
				{
					NestedIfStatementLevels.Push(ThisLineIndent);
				}
			}

			if (blocks.Contains(keyword) && Engine.NeedsReindent)
			{
				LastBlockIndent = Indent.ConvertFrom(Engine.CurrentIndent, ThisLineIndent, Engine.textEditorOptions);
			}
		}

		/// <summary>
		///     Pushes a new level of indentation depending on the given
		///     <paramref name="braceStyle"/>.
		/// </summary>
		void AddIndentation(BraceStyle braceStyle)
		{
			switch (braceStyle)
			{
				case BraceStyle.NextLineShifted:
					ThisLineIndent.Push(IndentType.Block);
					NextLineIndent.Push(IndentType.Block);
					break;
				case BraceStyle.DoNotChange:
				case BraceStyle.EndOfLine:
				case BraceStyle.EndOfLineWithoutSpace:
				case BraceStyle.NextLine:
				case BraceStyle.BannerStyle:
					NextLineIndent.Push(IndentType.Block);
					break;
				case BraceStyle.NextLineShifted2:
					ThisLineIndent.Push(IndentType.Block);
					NextLineIndent.Push(IndentType.DoubleBlock);
					break;
			}
		}

		bool TryGetBraceStyle (Body body, out BraceStyle style)
		{
			style = BraceStyle.DoNotChange;
			switch (body)
			{
				case Body.None:
					if (!Engine.formattingOptions.IndentBlocks)
						return false;
					style = Engine.formattingOptions.StatementBraceStyle;
					return true;
				case Body.Namespace:
					if (!Engine.formattingOptions.IndentNamespaceBody)
						return false;
					style = Engine.formattingOptions.NamespaceBraceStyle;
					return true;
				case Body.Class:
					if (!Engine.formattingOptions.IndentClassBody)
						return false;
					style = Engine.formattingOptions.ClassBraceStyle;
					return true;
				case Body.Struct:
					if (!Engine.formattingOptions.IndentStructBody)
						return false;
					style = Engine.formattingOptions.StructBraceStyle;
					return true;
				case Body.Interface:
					if (!Engine.formattingOptions.IndentInterfaceBody)
						return false;
					style = Engine.formattingOptions.InterfaceBraceStyle;
					return true;
				case Body.Enum:
					if (!Engine.formattingOptions.IndentEnumBody)
						return false;
					style = Engine.formattingOptions.EnumBraceStyle;
					return true;
				case Body.Switch:
					if (!Engine.formattingOptions.IndentSwitchBody)
						return false;
					style = Engine.formattingOptions.StatementBraceStyle;
					return true;
				case Body.Try:
				case Body.Catch:
				case Body.Finally:
					style = Engine.formattingOptions.StatementBraceStyle;
					return true;
			}
			return false;
		}

		/// <summary>
		///     Pushes a new level of indentation depending on the given
		///     <paramref name="body"/>.
		/// </summary>
		void AddIndentation(Body body)
		{
			var isExpression = Parent is ParenthesesBodyState || Parent is SquareBracketsBodyState ||
				(Parent is BracesBodyState && ((BracesBodyState)Parent).IsRightHandExpression);
			if (isExpression && Engine.formattingOptions.IndentBlocksInsideExpressions && Engine.isLineStart)
			{
				AddIndentation(BraceStyle.NextLineShifted);
			}

			BraceStyle style;
			if (TryGetBraceStyle(body, out style))
			{
				AddIndentation(style);
			} else {
				NextLineIndent.Push(IndentType.Empty);
			}
		}

		/// <summary>
		///     Extracts the <see cref="CurrentBody"/> from the given state.
		/// </summary>
		/// <returns>
		///     The correct <see cref="Body"/> type for this state.
		/// </returns>
		static Body extractBody(IndentState state)
		{
			if (state != null && state is BracesBodyState)
			{
				return ((BracesBodyState)state).NextBody;
			}

			return Body.None;
		}

		#endregion
	}

	#endregion

	#region Global body state

	/// <summary>
	///     Global body state.
	/// </summary>
	/// <remarks>
	///     Represents the global space of the program.
	/// </remarks>
	public class GlobalBodyState : BracesBodyState
	{
		public override char ClosedBracket
		{
			get { return '\0'; }
		}

		public GlobalBodyState()
		{ }


		public GlobalBodyState(CSharpIndentEngine engine)
		{
			Initialize (engine, null);
		}

		public GlobalBodyState(GlobalBodyState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new GlobalBodyState(this, engine);
		}

		public override void InitializeState()
		{
			ThisLineIndent = new Indent(Engine.textEditorOptions);
			NextLineIndent = ThisLineIndent.Clone();
		}
	}

	#endregion

	#region Switch-case body state

	/// <summary>
	///     Switch-case statement state.
	/// </summary>
	/// <remarks>
	///     Represents the block of code in one switch case (including default).
	/// </remarks>
	public class SwitchCaseState : BracesBodyState
	{
		public SwitchCaseState()
		{ }

		public SwitchCaseState(SwitchCaseState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override void Push(char ch)
		{
			// on ClosedBracket both this state (a case or a default statement)
			// and also the whole switch block (handled in the base class) must exit.
			if (ch == ClosedBracket)
			{
				ExitState();
				if (Parent is BracesBodyState)
					Parent.OnExit();
			}

			base.Push(ch);
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = ThisLineIndent.Clone();

			// remove all continuations and extra spaces
			ThisLineIndent.RemoveAlignment(); 
			ThisLineIndent.PopWhile(IndentType.Continuation);

			NextLineIndent.RemoveAlignment();
			NextLineIndent.PopWhile(IndentType.Continuation);

			if (Engine.formattingOptions.IndentCaseBody)
			{
				NextLineIndent.Push(IndentType.Block);
			}
			else
			{
				NextLineIndent.Push(IndentType.Empty);
			}
		}

		static readonly string[] caseDefaultKeywords = {
			"case",
			"default"
		};

		static readonly string[] breakContinueReturnGotoKeywords = {
			"break",
			"continue",
			"return",
			"goto"
		};

		public override void CheckKeyword(string keyword)
		{
			if (caseDefaultKeywords.Contains(keyword) && Engine.isLineStartBeforeWordToken)
			{
				ExitState();
				ChangeState<SwitchCaseState>();
			}
			else if (breakContinueReturnGotoKeywords.Contains(keyword) && Engine.isLineStartBeforeWordToken)
			{
				// OPTION: Engine.formattingOptions.IndentBreakStatements
				if (!Engine.formattingOptions.IndentBreakStatements)
				{
					ThisLineIndent = Parent.ThisLineIndent.Clone();
				}
			}

			base.CheckKeyword(keyword);
		}


		public override void OnExit()
		{
			//Parent.OnExit();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new SwitchCaseState(this, engine);
		}
	}

	#endregion

	#region Parentheses body state

	/// <summary>
	///     Parentheses body state.
	/// </summary>
	/// <remarks>
	///     Represents a block of code between ( and ).
	/// </remarks>
	public class ParenthesesBodyState : BracketsBodyBaseState
	{
		/// <summary>
		///     True if any char has been pushed.
		/// </summary>
		public bool IsSomethingPushed;

		public override char ClosedBracket
		{
			get { return ')'; }
		}

		public ParenthesesBodyState()
		{ }

		public ParenthesesBodyState(ParenthesesBodyState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsSomethingPushed = prototype.IsSomethingPushed;
		}

		public override void Push(char ch)
		{
			if (ch == Engine.newLineChar)
			{
				if (Engine.formattingOptions.AnonymousMethodBraceStyle == BraceStyle.EndOfLine || 
					Engine.formattingOptions.AnonymousMethodBraceStyle == BraceStyle.EndOfLineWithoutSpace) {
					if (NextLineIndent.PopIf(IndentType.Continuation)) {
						NextLineIndent.Push(IndentType.Block);
					}
				}
			}
			else if (!IsSomethingPushed)
			{
				// OPTION: CSharpFormattingOptions.AlignToFirstMethodCallArgument
				if (Engine.formattingOptions.AlignToFirstMethodCallArgument)
				{
					NextLineIndent.PopTry();
					// align the next line at the beginning of the open bracket
					NextLineIndent.ExtraSpaces = Math.Max(0, Engine.column - NextLineIndent.CurIndent - 1);
				}
			}

			base.Push(ch);
			IsSomethingPushed = true;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = ThisLineIndent.Clone();
			NextLineIndent.Push(IndentType.Continuation);
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new ParenthesesBodyState(this, engine);
		}

		public override void OnExit()
		{
			if (Engine.isLineStart)
			{
				if (ThisLineIndent.ExtraSpaces > 0)
				{
					ThisLineIndent.ExtraSpaces--;
				}
				else
				{
					ThisLineIndent.PopTry();
				}
			}

			base.OnExit();
		}
	}

	#endregion

	#region Square brackets body state

	/// <summary>
	///     Square brackets body state.
	/// </summary>
	/// <remarks>
	///     Represents a block of code between [ and ].
	/// </remarks>
	public class SquareBracketsBodyState : BracketsBodyBaseState
	{
		/// <summary>
		///     True if any char has been pushed.
		/// </summary>
		public bool IsSomethingPushed;

		public override char ClosedBracket
		{
			get { return ']'; }
		}

		public SquareBracketsBodyState()
		{ }

		public SquareBracketsBodyState(SquareBracketsBodyState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsSomethingPushed = prototype.IsSomethingPushed;
		}

		public override void Push(char ch)
		{
			if (ch == Engine.newLineChar)
			{
				if (NextLineIndent.PopIf(IndentType.Continuation))
				{
					NextLineIndent.Push(IndentType.Block);
				}
			}
			else if (!IsSomethingPushed)
			{
				// OPTION: CSharpFormattingOptions.AlignToFirstIndexerArgument
				if (Engine.formattingOptions.AlignToFirstIndexerArgument)
				{
					NextLineIndent.PopTry();
					// align the next line at the beginning of the open bracket
					NextLineIndent.ExtraSpaces = Math.Max(0, Engine.column - NextLineIndent.CurIndent - 1);
				}
			}

			base.Push(ch);
			IsSomethingPushed = true;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = ThisLineIndent.Clone();
			NextLineIndent.Push(IndentType.Continuation);
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new SquareBracketsBodyState(this, engine);
		}

		public override void OnExit()
		{
			if (Engine.isLineStart)
			{
				if (ThisLineIndent.ExtraSpaces > 0)
				{
					ThisLineIndent.ExtraSpaces--;
				}
				else
				{
					ThisLineIndent.PopTry();
				}
			}

			base.OnExit();
		}
	}

	#endregion

	#endregion

	#region PreProcessor state

	/// <summary>
	///     PreProcessor directive state.
	/// </summary>
	/// <remarks>
	///     Activated when the '#' char is pushed and the 
	///     <see cref="CSharpIndentEngine.isLineStart"/> is true.
	/// </remarks>
	public class PreProcessorState : IndentState
	{
		/// <summary>
		///     The type of the preprocessor directive.
		/// </summary>
		public PreProcessorDirective DirectiveType;

		/// <summary>
		///     If <see cref="DirectiveType"/> is set (not equal to 'None'), this
		///     stores the expression of the directive.
		/// </summary>
		public StringBuilder DirectiveStatement;

		public PreProcessorState()
		{
			DirectiveType = PreProcessorDirective.None;
			DirectiveStatement = new StringBuilder();
		}

		public PreProcessorState(PreProcessorState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			DirectiveType = prototype.DirectiveType;
			DirectiveStatement = new StringBuilder(prototype.DirectiveStatement.ToString());
		}

		public override void Push(char ch)
		{
			// HACK: if this change would be left for the CheckKeyword method, we will lose
			//       it if the next pushed char is newLineChar since ThisLineIndent will be 
			//       immediately replaced with NextLineIndent. As this most likely will
			//       happen, we check for "endregion" on every push.
			if (Engine.wordToken.ToString() == "endregion")
			{
				CheckKeywordOnPush("endregion");
			}

			base.Push(ch);

			if (DirectiveType != PreProcessorDirective.None)
			{
				DirectiveStatement.Append(ch);
			}

			if (ch == Engine.newLineChar)
			{
				ExitState();
				switch (DirectiveType)
				{
					case PreProcessorDirective.If:
						Engine.ifDirectiveEvalResults.Push(eval(DirectiveStatement.ToString()));
						if (Engine.ifDirectiveEvalResults.Peek())
						{
							// the if/elif directive is true -> continue with the previous state
						}
						else
						{
							// the if/elif directive is false -> change to a state that will 
							// ignore any chars until #endif or #elif
							ChangeState<PreProcessorCommentState>();
						}
						break;
					case PreProcessorDirective.Elif:
						if (Engine.ifDirectiveEvalResults.Count > 0)
						{
							if (!Engine.ifDirectiveEvalResults.Peek())
							{
								ExitState();
								Engine.ifDirectiveEvalResults.Pop();
								goto case PreProcessorDirective.If;
							}
						}
						// previous if was true -> comment
						ChangeState<PreProcessorCommentState>();
						break;
					case PreProcessorDirective.Else:
						if (Engine.ifDirectiveEvalResults.Count > 0 && Engine.ifDirectiveEvalResults.Peek())
						{
							// some if/elif directive was true -> change to a state that will 
							// ignore any chars until #endif
							ChangeState<PreProcessorCommentState>();
						}
						else
						{
							// none if/elif directives were true -> exit comment state.
							if (Engine.currentState is PreProcessorCommentState)
								ExitState();
						}
						break;
					case PreProcessorDirective.Define:
						var defineSymbol = DirectiveStatement.ToString().Trim();
						if (!Engine.conditionalSymbols.Contains(defineSymbol))
						{
							Engine.conditionalSymbols.Add(defineSymbol);
						}
						break;
					case PreProcessorDirective.Undef:
						var undefineSymbol = DirectiveStatement.ToString().Trim();
						if (Engine.conditionalSymbols.Contains(undefineSymbol))
						{
							Engine.conditionalSymbols.Remove(undefineSymbol);
						}
						break;
					case PreProcessorDirective.Endif:
						// marks the end of this block
						if (Engine.currentState is PreProcessorCommentState)
							ExitState();

						Engine.ifDirectiveEvalResults.Pop();
						Engine.ifDirectiveIndents.Pop();
						break;
					case PreProcessorDirective.Region:
					case PreProcessorDirective.Pragma:
					case PreProcessorDirective.Warning:
					case PreProcessorDirective.Error:
					case PreProcessorDirective.Line:
						// continue with the previous state
						break;
				}
			}
		}

		public override void InitializeState()
		{
			// OPTION: IndentPreprocessorStatements
			if (Engine.formattingOptions.IndentPreprocessorDirectives)
			{
				if (Engine.ifDirectiveIndents.Count > 0)
				{
					ThisLineIndent = Engine.ifDirectiveIndents.Peek().Clone();
				}
				else
				{
					ThisLineIndent = Parent.ThisLineIndent.Clone();
				}
			}
			else
			{
				ThisLineIndent = new Indent(Engine.textEditorOptions);
			}

			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		static readonly Dictionary<string, PreProcessorDirective> preProcessorDirectives = new Dictionary<string, PreProcessorDirective>
		{
			{ "if", PreProcessorDirective.If },
			{ "elif", PreProcessorDirective.Elif },
			{ "else", PreProcessorDirective.Else },
			{ "endif", PreProcessorDirective.Endif },
			{ "region", PreProcessorDirective.Region },
			{ "endregion", PreProcessorDirective.Endregion },
			{ "pragma", PreProcessorDirective.Pragma },
			{ "warning", PreProcessorDirective.Warning },
			{ "error", PreProcessorDirective.Error },
			{ "line", PreProcessorDirective.Line },
			{ "define", PreProcessorDirective.Define },
			{ "undef", PreProcessorDirective.Undef }
		};

		public override void CheckKeywordOnPush(string keyword)
		{
			if (keyword == "endregion")
			{
				DirectiveType = PreProcessorDirective.Endregion;
				ThisLineIndent = Parent.NextLineIndent.Clone();
			}
		}

		public override void CheckKeyword(string keyword)
		{
			// check if the directive type has already been set
			if (DirectiveType != PreProcessorDirective.None)
			{
				return;
			}

			if (preProcessorDirectives.ContainsKey(keyword))
			{
				DirectiveType = preProcessorDirectives[keyword];

				// adjust the indentation for the region directive
				if (DirectiveType == PreProcessorDirective.Region)
				{
					ThisLineIndent = Parent.NextLineIndent.Clone();
				}
				else if (DirectiveType == PreProcessorDirective.If)
				{
					Engine.ifDirectiveIndents.Push(ThisLineIndent.Clone());
				}
			}
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new PreProcessorState(this, engine);
		}

		/// <summary>
		///     Types of preprocessor directives.
		/// </summary>
		public enum PreProcessorDirective
		{
			None,
			If,
			Elif,
			Else,
			Endif,
			Region,
			Endregion,
			Pragma,
			Warning,
			Error,
			Line,
			Define,
			Undef
		}

		#region Pre processor evaluation (from cs-tokenizer.cs)

		static bool is_identifier_start_character(int c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || Char.IsLetter((char)c);
		}

		static bool is_identifier_part_character(char c)
		{
			if (c >= 'a' && c <= 'z')
				return true;

			if (c >= 'A' && c <= 'Z')
				return true;

			if (c == '_' || (c >= '0' && c <= '9'))
				return true;

			if (c < 0x80)
				return false;

			return Char.IsLetter(c) || Char.GetUnicodeCategory(c) == UnicodeCategory.ConnectorPunctuation;
		}

		bool eval_val(string s)
		{
			if (s == "true")
				return true;
			if (s == "false")
				return false;

			return Engine.conditionalSymbols != null && Engine.conditionalSymbols.Contains(s) ||
				Engine.customConditionalSymbols != null && Engine.customConditionalSymbols.Contains(s);
		}

		bool pp_primary(ref string s)
		{
			s = s.Trim();
			int len = s.Length;

			if (len > 0)
			{
				char c = s[0];

				if (c == '(')
				{
					s = s.Substring(1);
					bool val = pp_expr(ref s, false);
					if (s.Length > 0 && s[0] == ')')
					{
						s = s.Substring(1);
						return val;
					}
					return false;
				}

				if (is_identifier_start_character(c))
				{
					int j = 1;

					while (j < len)
					{
						c = s[j];

						if (is_identifier_part_character(c))
						{
							j++;
							continue;
						}
						bool v = eval_val(s.Substring(0, j));
						s = s.Substring(j);
						return v;
					}
					bool vv = eval_val(s);
					s = "";
					return vv;
				}
			}
			return false;
		}

		bool pp_unary(ref string s)
		{
			s = s.Trim();
			int len = s.Length;

			if (len > 0)
			{
				if (s[0] == '!')
				{
					if (len > 1 && s[1] == '=')
					{
						return false;
					}
					s = s.Substring(1);
					return !pp_primary(ref s);
				}
				else
					return pp_primary(ref s);
			}
			else
			{
				return false;
			}
		}

		bool pp_eq(ref string s)
		{
			bool va = pp_unary(ref s);

			s = s.Trim();
			int len = s.Length;
			if (len > 0)
			{
				if (s[0] == '=')
				{
					if (len > 2 && s[1] == '=')
					{
						s = s.Substring(2);
						return va == pp_unary(ref s);
					}
					else
					{
						return false;
					}
				}
				else if (s[0] == '!' && len > 1 && s[1] == '=')
				{
					s = s.Substring(2);

					return va != pp_unary(ref s);

				}
			}

			return va;

		}

		bool pp_and(ref string s)
		{
			bool va = pp_eq(ref s);

			s = s.Trim();
			int len = s.Length;
			if (len > 0)
			{
				if (s[0] == '&')
				{
					if (len > 2 && s[1] == '&')
					{
						s = s.Substring(2);
						return (va & pp_and(ref s));
					}
					else
					{
						return false;
					}
				}
			}
			return va;
		}

		//
		// Evaluates an expression for `#if' or `#elif'
		//
		bool pp_expr(ref string s, bool isTerm)
		{
			bool va = pp_and(ref s);
			s = s.Trim();
			int len = s.Length;
			if (len > 0)
			{
				char c = s[0];

				if (c == '|')
				{
					if (len > 2 && s[1] == '|')
					{
						s = s.Substring(2);
						return va | pp_expr(ref s, isTerm);
					}
					else
					{

						return false;
					}
				}
				if (isTerm)
				{
					return false;
				}
			}

			return va;
		}

		bool eval(string s)
		{
			bool v = pp_expr(ref s, true);
			s = s.Trim();
			if (s.Length != 0)
			{
				return false;
			}

			return v;
		}

		#endregion
	}

	#endregion

	#region PreProcessorComment state

	/// <summary>
	///     PreProcessor comment state.
	/// </summary>
	/// <remarks>
	///     Activates when the #if or #elif directive is false and ignores
	///     all pushed chars until the next '#'.
	/// </remarks>
	public class PreProcessorCommentState : IndentState
	{
		public PreProcessorCommentState()
		{ }

		public PreProcessorCommentState(PreProcessorCommentState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == '#' && Engine.isLineStart)
			{
				ChangeState<PreProcessorState>();
			}
		}

		public override void InitializeState()
		{
			if (Engine.formattingOptions.IndentPreprocessorDirectives &&
			    Engine.ifDirectiveIndents.Count > 0)
			{
				ThisLineIndent = Engine.ifDirectiveIndents.Peek().Clone();
				NextLineIndent = ThisLineIndent.Clone();
			}
			else
			{
				ThisLineIndent = Parent.NextLineIndent.Clone();
				NextLineIndent = ThisLineIndent.Clone();
			}
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new PreProcessorCommentState(this, engine);
		}
	}

	#endregion

	#region LineComment state

	/// <summary>
	///     Single-line comment state.
	/// </summary>
	public class LineCommentState : IndentState
	{
		/// <summary>
		///     It's possible that this should be the DocComment state:
		///         check if the first next pushed char is equal to '/'.
		/// </summary>
		public bool CheckForDocComment = true;

		public LineCommentState()
		{
			/*			if (engine.formattingOptions.KeepCommentsAtFirstColumn && engine.column == 2)
				ThisLineIndent.Reset();*/
		}

		public LineCommentState(LineCommentState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			CheckForDocComment = prototype.CheckForDocComment;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == Engine.newLineChar)
			{
				// to handle cases like //\n/*
				// Otherwise line 2 would be treated as line comment.
				Engine.previousChar = '\0';
				ExitState();
			}
			else if (ch == '/' && CheckForDocComment)
			{
				// wrong state, should be DocComment.
				ExitState();
				ChangeState<DocCommentState>();
			}

			CheckForDocComment = false;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new LineCommentState(this, engine);
		}
	}

	#endregion

	#region DocComment state

	/// <summary>
	///     XML documentation comment state.
	/// </summary>
	public class DocCommentState : IndentState
	{
		public DocCommentState()
		{ }

		public DocCommentState(DocCommentState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{ }

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == Engine.newLineChar)
			{
				ExitState();
			}
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new DocCommentState(this, engine);
		}
	}

	#endregion

	#region MultiLineComment state

	/// <summary>
	///     Multi-line comment state.
	/// </summary>
	public class MultiLineCommentState : IndentState
	{
		/// <summary>
		///     True if any char has been pushed to this state.
		/// </summary>
		/// <remarks>
		///     Needed to resolve an issue when the first pushed char is '/'.
		///     The state would falsely exit on this sequence of chars '/*/',
		///     since it only checks if the last two chars are '/' and '*'.
		/// </remarks>
		public bool IsAnyCharPushed;

		public MultiLineCommentState()
		{ }

		public MultiLineCommentState(MultiLineCommentState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsAnyCharPushed = prototype.IsAnyCharPushed;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == '/' && Engine.previousChar == '*' && IsAnyCharPushed)
			{
				ExitState();
			}

			IsAnyCharPushed = true;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = ThisLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new MultiLineCommentState(this, engine);
		}
	}

	#endregion

	#region StringLiteral state

	/// <summary>
	///     StringLiteral state.
	/// </summary>
	public class StringLiteralState : IndentState
	{
		/// <summary>
		///     True if the next char is escaped with '\'.
		/// </summary>
		public bool IsEscaped;

		public StringLiteralState()
		{ }

		public StringLiteralState(StringLiteralState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsEscaped = prototype.IsEscaped;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == Engine.newLineChar || (!IsEscaped && ch == '"')) {
				ExitState();
			} else {
				IsEscaped = ch == '\\' && !IsEscaped;
			}
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new StringLiteralState(this, engine);
		}
	}

	#endregion

	#region Verbatim string state

	/// <summary>
	///     Verbatim string state.
	/// </summary>
	public class VerbatimStringState : IndentState
	{
		/// <summary>
		///     True if there is an odd number of '"' in a row.
		/// </summary>
		public bool IsEscaped;

		public VerbatimStringState()
		{ }

		public VerbatimStringState(VerbatimStringState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsEscaped = prototype.IsEscaped;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (IsEscaped && ch != '"')
			{
				ExitState();
				// the char has been pushed to the wrong state, push it back
				Engine.currentState.Push(ch);
			}

			IsEscaped = ch == '"' && !IsEscaped;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = new Indent(Engine.textEditorOptions);
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new VerbatimStringState(this, engine);
		}
	}

	#endregion

	#region Character state

	/// <summary>
	///     Character state.
	/// </summary>
	public class CharacterState : IndentState
	{
		/// <summary>
		///     True if the next char is escaped with '\'.
		/// </summary>
		public bool IsEscaped;

		public CharacterState()
		{ }

		public CharacterState(CharacterState prototype, CSharpIndentEngine engine)
			: base(prototype, engine)
		{
			IsEscaped = prototype.IsEscaped;
		}

		public override void Push(char ch)
		{
			base.Push(ch);

			if (ch == Engine.newLineChar)
			{
				ExitState();
			}
			else if (!IsEscaped && ch == '\'')
			{
				ExitState();
			}

			IsEscaped = ch == '\\' && !IsEscaped;
		}

		public override void InitializeState()
		{
			ThisLineIndent = Parent.ThisLineIndent.Clone();
			NextLineIndent = Parent.NextLineIndent.Clone();
		}

		public override IndentState Clone(CSharpIndentEngine engine)
		{
			return new CharacterState(this, engine);
		}
	}

	#endregion
}
