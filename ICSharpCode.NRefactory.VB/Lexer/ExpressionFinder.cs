// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.VB.Parser
{
	public partial class ExpressionFinder
	{
		Stack<Block> stack = new Stack<Block>();
		StringBuilder output = new StringBuilder();
		
		void PopContext()
		{
			if (stack.Any()) {
				string indent = new string('\t', stack.Count - 1);
				var item = stack.Pop();
				item.isClosed = true;
				Print(indent + "exit " + item.context);
			} else {
				Print("empty stack");
			}
		}
		
		void PushContext(Context context, Token la, Token t)
		{
			string indent = new string('\t', stack.Count);
			TextLocation l = la == null ? (t == null ? TextLocation.Empty : t.EndLocation) : la.Location;
			
			stack.Push(new Block() { context = context, lastExpressionStart = l });
			Print(indent + "enter " + context);
		}
		
		public ExpressionFinder(ExpressionFinderState state)
		{
			wasQualifierTokenAtStart = state.WasQualifierTokenAtStart;
			nextTokenIsPotentialStartOfExpression = state.NextTokenIsPotentialStartOfExpression;
			nextTokenIsStartOfImportsOrAccessExpression = state.NextTokenIsStartOfImportsOrAccessExpression;
			readXmlIdentifier = state.ReadXmlIdentifier;
			identifierExpected = state.IdentifierExpected;
			stateStack = new Stack<int>(state.StateStack.Reverse());
			stack = new Stack<Block>(state.BlockStack.Select(x => (Block)x.Clone()).Reverse());
			currentState = state.CurrentState;
			output = new StringBuilder();
		}
		
		void Print(string text)
		{
			//Console.WriteLine(text);
			output.AppendLine(text);
		}
		
		public void SetContext(SnippetType type)
		{
			switch (type) {
				case SnippetType.Expression:
					currentState = startOfExpression;
					break;
			}
			
			Advance();
		}
		
		public string Output {
			get { return output.ToString(); }
		}
		
		public string Stacktrace {
			get {
				string text = "";
				
				foreach (Block b in stack) {
					text += b.ToString() + "\n";
				}
				
				return text;
			}
		}
		
		public Block CurrentBlock {
			get { return stack.Any() ? stack.Peek() : Block.Default; }
		}
		
		public bool IsIdentifierExpected {
			get { return identifierExpected; }
		}
		
		void SetIdentifierExpected(Token la)
		{
			identifierExpected = true;
			if (la != null)
				CurrentBlock.lastExpressionStart = la.Location;
			else if (t != null)
				CurrentBlock.lastExpressionStart = t.EndLocation;
		}
		
		public bool InContext(Context expected)
		{
			return stack
				.SkipWhile(f => f.context == Context.Expression)
				.IsElement(fx => fx.context == expected);
		}
		
		public bool NextTokenIsPotentialStartOfExpression {
			get { return nextTokenIsPotentialStartOfExpression; }
		}
		
		public bool ReadXmlIdentifier {
			get { return readXmlIdentifier; }
			set { readXmlIdentifier = value; }
		}
		
		public bool NextTokenIsStartOfImportsOrAccessExpression {
			get { return nextTokenIsStartOfImportsOrAccessExpression; }
		}
		
		public bool WasQualifierTokenAtStart {
			get { return wasQualifierTokenAtStart; }
		}
		
		public bool IsMissingModifier {
			get { return isMissingModifier; }
		}
		
		public bool WasNormalAttribute {
			get { return wasNormalAttribute; }
		}
		
		public int ActiveArgument {
			get { return activeArgument; }
		}
		
		public List<Token> Errors {
			get { return errors; }
		}
		
		public ExpressionFinderState Export()
		{
			return new ExpressionFinderState() {
				WasQualifierTokenAtStart = wasQualifierTokenAtStart,
				NextTokenIsPotentialStartOfExpression = nextTokenIsPotentialStartOfExpression,
				NextTokenIsStartOfImportsOrAccessExpression = nextTokenIsStartOfImportsOrAccessExpression,
				ReadXmlIdentifier = readXmlIdentifier,
				IdentifierExpected = identifierExpected,
				StateStack = new Stack<int>(stateStack.Reverse()),
				BlockStack = new Stack<Block>(stack.Select(x => (Block)x.Clone()).Reverse()),
				CurrentState = currentState
			};
		}
	}
}
