// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.CSharp;

namespace ICSharpCode.SharpDevelop.Dom.CSharp
{
	/// <summary>
	/// Supports getting the expression including context from the cursor position.
	/// </summary>
	public class CSharpExpressionFinder : IExpressionFinder
	{
		ParseInformation parseInformation;
		IProjectContent projectContent;
		
		public CSharpExpressionFinder(ParseInformation parseInformation)
		{
			this.parseInformation = parseInformation;
			if (parseInformation != null && parseInformation.CompilationUnit != null) {
				projectContent = parseInformation.CompilationUnit.ProjectContent;
			} else {
				projectContent = DefaultProjectContent.DummyProjectContent;
			}
		}
		
		ILexer lexer;
		Location targetPosition;
		List<int> lineOffsets;
		
		int LocationToOffset(Location location)
		{
			if (location.Line <= 0) return -1;
			return lineOffsets[location.Line - 1] + location.Column - 1;
		}
		
		Location OffsetToLocation(int offset)
		{
			int lineNumber = lineOffsets.BinarySearch(offset);
			if (lineNumber < 0) {
				lineNumber = (~lineNumber) - 1;
			}
			return new Location(offset - lineOffsets[lineNumber] + 1, lineNumber + 1);
		}
		
		enum FrameType
		{
			Global,
			/// <summary>
			/// "class C { * }"
			/// </summary>
			TypeDecl,
			/// <summary>
			/// "interface I { * }"
			/// </summary>
			Interface,
			/// <summary>
			/// "enum E { * }"
			/// </summary>
			Enum,
			/// <summary>
			/// "void Method(*) {}"
			/// </summary>
			ParameterList,
			/// <summary>
			/// "public string Property { * }"
			/// </summary>
			Property,
			/// <summary>
			/// "public event EventHandler SomethingChanged { * }"
			/// </summary>
			Event,
			/// <summary>
			/// "void Method() { * }"
			/// </summary>
			Statements,
			/// <summary>
			/// "if (*) {}"
			/// </summary>
			Expression,
			/// <summary>
			/// "new T { * }"
			/// </summary>
			ObjectInitializer,
			/// <summary>
			/// "[*] class ..."
			/// </summary>
			AttributeSection,
			/// <summary>
			/// "[Obsolete(*)] class ..."
			/// </summary>
			AttributeArguments,
			/// <summary>
			/// Type reference frame "typeof(*)"
			/// </summary>
			TypeReference,
			/// <summary>
			/// Type parameter declaration, "class C&lt;*gt;"
			/// </summary>
			TypeParameterDecl,
			/// <summary>
			/// The Frame is no longer active.
			/// </summary>
			Popped
		}
		
		/// <summary>
		/// Used to support a frame-type specific state machine. Used in TrackCurrentContext
		/// </summary>
		enum FrameState
		{
			/// <summary>
			/// the default state (all frame types)
			/// </summary>
			Normal,
			/// <summary>
			/// parsing an inheritance list (Global+TypeDecl)
			/// </summary>
			InheritanceList,
			/// <summary>
			/// parsing an event declaration (Interface+TypeDecl)
			/// </summary>
			EventDecl,
			/// <summary>
			/// parsing a field declaration (Interface+TypeDecl).
			/// Could also be a method or property declaration.
			/// </summary>
			FieldDecl,
			/// <summary>
			/// parsing a field declaration, the field name was already specified (Interface+TypeDecl).
			/// Could also be a method or property declaration.
			/// </summary>
			FieldDeclAfterIdentifier,
			/// <summary>
			/// parsing a method declaration (Interface+TypeDecl) or a delegate declaration (Global+TypeDecl)
			/// </summary>
			MethodDecl,
			/// <summary>
			/// parsing a field initializer (TypeDecl)
			/// </summary>
			Initializer,
			/// <summary>
			/// Between class/struct/enum keyword and body of the type declaration
			/// </summary>
			TypeDecl,
			/// <summary>
			/// Between "where" and start of the generic method/class
			/// </summary>
			Constraints,
			/// <summary>
			/// Between "new" and "(" / "{".
			/// </summary>
			ObjectCreation,
			/// <summary>
			/// In object initializer, in the value part (after "=")
			/// </summary>
			ObjectInitializerValue,
			/// <summary>
			/// After a keyword like "if","while","using" etc., but before the embedded statement.
			/// </summary>
			StatementWithEmbeddedStatement,
			/// <summary>
			/// After "using" in global context, but before "=" or ";".
			/// </summary>
			UsingNamespace
		}
		
		/// <summary>
		/// When parsing the code, each block starting with one of the brackets "(", "[", "{" or "&lt;" (for generics)
		/// gets an instance of Frame.
		/// </summary>
		sealed class Frame
		{
			internal Frame parent;
			internal FrameType type;
			internal FrameType parenthesisChildType;
			internal FrameType curlyChildType;
			internal FrameType squareBracketChildType;
			internal FrameState state;
			internal char bracketType;
			internal ExpressionContext context;
			internal IReturnType expectedType;
			
			internal bool InExpressionMode {
				get {
					return type == FrameType.Statements
						|| type == FrameType.Expression
						|| type == FrameType.AttributeArguments
						|| type == FrameType.ObjectInitializer
						|| state == FrameState.Initializer
						|| state == FrameState.ObjectCreation;
				}
			}
			
			internal void SetContext(ExpressionContext context)
			{
				this.context = context;
				this.expectedType = null;
			}
			internal void SetExpectedType(IReturnType expectedType)
			{
				this.expectedType = expectedType;
				this.context = ExpressionContext.Default;
			}
			internal void SetDefaultContext()
			{
				if (state == FrameState.InheritanceList) {
					if (curlyChildType == FrameType.Enum) {
						SetContext(ExpressionContext.EnumBaseType);
					} else if (curlyChildType == FrameType.Interface) {
						SetContext(ExpressionContext.Interface);
					} else {
						SetContext(ExpressionContext.InheritableType);
					}
				} else if (state == FrameState.Constraints) {
					SetContext(CSharpExpressionContext.Constraints);
				} else if (state == FrameState.UsingNamespace) {
					SetContext(ExpressionContext.Namespace);
				} else {
					switch (type) {
						case FrameType.Global:
							SetContext(ExpressionContext.Global);
							break;
						case FrameType.TypeDecl:
							SetContext(ExpressionContext.TypeDeclaration);
							break;
						case FrameType.Enum:
						case FrameType.TypeParameterDecl:
							SetContext(ExpressionContext.IdentifierExpected);
							break;
						case FrameType.Interface:
							SetContext(CSharpExpressionContext.InterfaceDeclaration);
							break;
						case FrameType.Event:
							SetContext(CSharpExpressionContext.EventDeclaration);
							break;
						case FrameType.Property:
							if (parent != null && parent.type == FrameType.Interface) {
								SetContext(CSharpExpressionContext.InterfacePropertyDeclaration);
							} else {
								SetContext(CSharpExpressionContext.PropertyDeclaration);
							}
							break;
						case FrameType.Statements:
							SetContext(ExpressionContext.MethodBody);
							break;
						case FrameType.ParameterList:
							SetContext(CSharpExpressionContext.ParameterType);
							break;
						case FrameType.ObjectInitializer:
							if (state == FrameState.ObjectInitializerValue) {
								SetContext(ExpressionContext.Default);
							} else {
								SetContext(CSharpExpressionContext.ObjectInitializer);
							}
							break;
						case FrameType.AttributeSection:
							SetContext(ExpressionContext.Attribute);
							break;
						case FrameType.TypeReference:
							SetContext(ExpressionContext.Type);
							break;
						default:
							SetContext(ExpressionContext.Default);
							break;
					}
				}
			}
			
			/// <summary>start of the expression currently being tracked</summary>
			internal Location lastExpressionStart;
			
			/// <summary>Position of the last "new" keyword</summary>
			internal Location lastNewTokenStart;
			
			public Frame() : this(null, '\0') {}
			
			public Frame(Frame parent, char bracketType)
			{
				this.parent = parent;
				this.bracketType = bracketType;
				if (parent != null) {
					if (bracketType == '{') {
						this.type = parent.curlyChildType;
					} else if (bracketType == '(') {
						this.type = parent.parenthesisChildType;
					} else if (bracketType == '[') {
						this.type = parent.squareBracketChildType;
					} else {
						this.type = parent.type;
					}
				}
				ResetCurlyChildType();
				ResetParenthesisChildType();
				ResetSquareBracketChildType();
				SetDefaultContext();
			}
			
			public void ResetCurlyChildType()
			{
				if (state == FrameState.Initializer) {
					this.curlyChildType = FrameType.Expression;
				} else {
					switch (this.type) {
						case FrameType.Property:
						case FrameType.Event:
							this.curlyChildType = FrameType.Statements;
							break;
						default:
							this.curlyChildType = this.type;
							break;
					}
				}
			}
			
			public void ResetParenthesisChildType()
			{
				if (this.InExpressionMode) {
					this.parenthesisChildType = FrameType.Expression;
				} else if (type == FrameType.AttributeSection) {
					this.parenthesisChildType = FrameType.AttributeArguments;
				} else {
					this.parenthesisChildType = this.type;
				}
			}
			
			public void ResetSquareBracketChildType()
			{
				if (InExpressionMode)
					this.squareBracketChildType = FrameType.Expression;
				else
					this.squareBracketChildType = FrameType.AttributeSection;
			}
		}
		
		void Init(string text, int offset)
		{
			if (offset < 0 || offset > text.Length)
				throw new ArgumentOutOfRangeException("offset", offset, "offset must be between 0 and " + text.Length);
			lexer = ParserFactory.CreateLexer(SupportedLanguage.CSharp, new StringReader(text));
			lexer.SkipAllComments = true;
			lineOffsets = new List<int>();
			lineOffsets.Add(0);
			for (int i = 0; i < text.Length; i++) {
				if (i == offset) {
					targetPosition = new Location(offset - lineOffsets[lineOffsets.Count - 1] + 1, lineOffsets.Count);
				}
				if (text[i] == '\n') {
					lineOffsets.Add(i + 1);
				} else if (text[i] == '\r') {
					if (i + 1 < text.Length && text[i + 1] != '\n') {
						lineOffsets.Add(i + 1);
					}
				}
			}
			if (offset == text.Length) {
				targetPosition = new Location(offset - lineOffsets[lineOffsets.Count - 1] + 1, lineOffsets.Count);
			}
			
			frame = new Frame();
			lastToken = Tokens.EOF;
		}
		
		Frame frame;
		int lastToken;
		
		public ExpressionResult FindExpression(string text, int offset)
		{
			Init(text, offset);
			Token token;
			Location lastError = Location.Empty;
			lexer.Errors.Error = delegate (int errorLine, int errorCol, string errorMsg) {
				lastError = new Location(errorCol, errorLine);
			};
			while ((token = lexer.NextToken()) != null) {
				if (token.Kind == Tokens.EOF) break;
				
				if (targetPosition <= token.Location) {
					break;
				}
				ApplyToken(token);
				if (targetPosition <= token.EndLocation) {
					if (token.Kind == Tokens.Literal) {
						// do not return string literal as expression if offset was inside the literal,
						// or if it was at the end of the literal when the literal was not terminated correctly.
						if (targetPosition < token.EndLocation || lastError == token.Location) {
							frame.lastExpressionStart = Location.Empty;
						}
					}
					break;
				}
				lastToken = token.Kind;
			}
			
			int tokenOffset;
			if (token == null || token.Kind == Tokens.EOF)
				tokenOffset = text.Length;
			else
				tokenOffset = LocationToOffset(token.Location);
			int lastExpressionStartOffset = LocationToOffset(frame.lastExpressionStart);
			if (lastExpressionStartOffset >= 0) {
				if (offset < tokenOffset) {
					// offset is in front of this token
					return MakeResult(text, lastExpressionStartOffset, tokenOffset, frame.context);
				} else {
					// offset is IN this token
					return MakeResult(text, lastExpressionStartOffset, offset, frame.context);
				}
			} else {
				return new ExpressionResult(null, frame.context);
			}
		}
		
		void ApplyToken(Token token)
		{
			TrackCurrentFrameAndExpression(token);
			TrackCurrentContext(token);
		}
		
		void TrackCurrentFrameAndExpression(Token token)
		{
			while (frame.bracketType == '<' && !Tokens.ValidInsideTypeName[token.Kind]) {
				frame.type = FrameType.Popped;
				frame = frame.parent;
			}
			switch (token.Kind) {
				case Tokens.OpenCurlyBrace:
					frame.lastExpressionStart = Location.Empty;
					frame = new Frame(frame, '{');
					frame.parent.ResetCurlyChildType();
					break;
				case Tokens.CloseCurlyBrace:
					while (frame.parent != null) {
						if (frame.bracketType == '{') {
							frame.type = FrameType.Popped;
							frame = frame.parent;
							break;
						} else {
							frame.type = FrameType.Popped;
							frame = frame.parent;
						}
					}
					break;
				case Tokens.OpenParenthesis:
					if (frame.lastExpressionStart.IsEmpty)
						frame.lastExpressionStart = token.Location;
					frame = new Frame(frame, '(');
					frame.parent.ResetParenthesisChildType();
					break;
				case Tokens.OpenSquareBracket:
					frame = new Frame(frame, '[');
					frame.parent.ResetSquareBracketChildType();
					break;
				case Tokens.CloseParenthesis:
				case Tokens.CloseSquareBracket:
					if (frame.parent != null && (frame.bracketType == '(' || frame.bracketType == '[')) {
						frame.type = FrameType.Popped;
						frame = frame.parent;
					}
					break;
				case Tokens.LessThan:
					if (Tokens.ValidInsideTypeName[lastToken]) {
						frame = new Frame(frame, '<');
						if (frame.parent.InExpressionMode) {
							frame.SetContext(ExpressionContext.Default);
						} else if ((frame.parent.state == FrameState.TypeDecl
						            || frame.parent.state == FrameState.MethodDecl
						            || frame.parent.state == FrameState.FieldDeclAfterIdentifier)
						           && frame.parent.context == ExpressionContext.IdentifierExpected)
						{
							frame.type = FrameType.TypeParameterDecl;
							frame.SetContext(ExpressionContext.IdentifierExpected);
							frame.parent.SetContext(CSharpExpressionContext.ConstraintsStart);
						} else {
							frame.SetContext(ExpressionContext.Type);
						}
					}
					break;
				case Tokens.GreaterThan:
					if (frame.parent != null && frame.bracketType == '<') {
						frame.type = FrameType.Popped;
						frame = frame.parent;
					} else {
						frame.lastExpressionStart = Location.Empty;
						frame.SetDefaultContext();
					}
					break;
				case Tokens.Question:
					// do not reset context - TrackCurrentContext will take care of this
					frame.lastExpressionStart = Location.Empty;
					break;
				case Tokens.Pointer:
				case Tokens.Dot:
				case Tokens.DoubleColon:
					// let the current expression continue
					break;
				default:
					if (Tokens.IdentifierTokens[token.Kind]) {
						if (lastToken != Tokens.Dot && lastToken != Tokens.DoubleColon && lastToken != Tokens.Pointer) {
							if (Tokens.ValidInsideTypeName[lastToken]) {
								frame.SetDefaultContext();
							}
							frame.lastExpressionStart = token.Location;
						}
					} else if (Tokens.SimpleTypeName[token.Kind] || Tokens.ExpressionStart[token.Kind] || token.Kind == Tokens.Literal) {
						frame.lastExpressionStart = token.Location;
					} else {
						frame.lastExpressionStart = Location.Empty;
						frame.SetDefaultContext();
					}
					break;
			}
		}
		
		void TrackCurrentContext(Token token)
		{
			if (frame.state == FrameState.ObjectCreation) {
				if (token.Kind == Tokens.CloseParenthesis) {
					if (frame.context.IsObjectCreation) {
						frame.context = ExpressionContext.Default;
						frame.lastExpressionStart = frame.lastNewTokenStart;
					}
					// keep frame.state
				} else if (token.Kind == Tokens.GreaterThan
				           || token.Kind == Tokens.DoubleColon || token.Kind == Tokens.Dot
				           || Tokens.SimpleTypeName[token.Kind])
				{
					// keep frame.state == FrameState.ObjectCreationInType
				} else {
					frame.state = FrameState.Normal;
					frame.ResetCurlyChildType();
				}
			} else if (frame.state == FrameState.UsingNamespace) {
				if (token.Kind != Tokens.Identifier && token.Kind != Tokens.Dot && token.Kind != Tokens.DoubleColon) {
					frame.state = FrameState.Normal;
					frame.SetDefaultContext();
				}
			}
			
			switch (token.Kind) {
				case Tokens.Using:
					if (frame.type == FrameType.Global) {
						frame.state = FrameState.UsingNamespace;
						frame.SetDefaultContext();
						break;
					} else {
						goto case Tokens.For;
					}
				case Tokens.For:
				case Tokens.Foreach:
				case Tokens.Fixed:
				case Tokens.Catch:
					if (frame.type == FrameType.Statements) {
						frame.parenthesisChildType = FrameType.Statements;
						frame.state = FrameState.StatementWithEmbeddedStatement;
					}
					break;
				case Tokens.If:
				case Tokens.While:
				case Tokens.Switch:
				case Tokens.Lock:
					if (frame.type == FrameType.Statements) {
						frame.state = FrameState.StatementWithEmbeddedStatement;
					}
					break;
				case Tokens.Throw:
					frame.SetExpectedType(projectContent.SystemTypes.Exception);
					break;
				case Tokens.New:
					if (frame.InExpressionMode) {
						frame.SetContext(ExpressionContext.TypeDerivingFrom(frame.expectedType, true));
						frame.state = FrameState.ObjectCreation;
						frame.curlyChildType = FrameType.ObjectInitializer;
						frame.lastNewTokenStart = token.Location;
					}
					break;
				case Tokens.Namespace:
					frame.SetContext(ExpressionContext.IdentifierExpected);
					break;
				case Tokens.Assign:
					if (frame.type == FrameType.Global) {
						frame.SetContext(CSharpExpressionContext.FullyQualifiedType);
						break;
					} else if (frame.type == FrameType.Enum) {
						frame.SetContext(ExpressionContext.Default);
						break;
					} else if (frame.type == FrameType.TypeDecl) {
						frame.SetContext(ExpressionContext.Default);
						frame.state = FrameState.Initializer;
						frame.ResetParenthesisChildType();
						frame.ResetSquareBracketChildType();
						frame.ResetCurlyChildType();
						break;
					} else if (frame.type == FrameType.ObjectInitializer) {
						frame.state = FrameState.ObjectInitializerValue;
						frame.SetDefaultContext();
						break;
					} else {
						goto default;
					}
				case Tokens.Colon:
					if (frame.state == FrameState.MethodDecl && lastToken == Tokens.CloseParenthesis) {
						frame.SetContext(CSharpExpressionContext.BaseConstructorCall);
						frame.parenthesisChildType = FrameType.Expression;
					} else {
						if (frame.curlyChildType == FrameType.TypeDecl || frame.curlyChildType == FrameType.Interface || frame.curlyChildType == FrameType.Enum) {
							if (frame.state != FrameState.Constraints) {
								frame.state = FrameState.InheritanceList;
								frame.SetDefaultContext();
							}
						}
					}
					break;
				case Tokens.Class:
				case Tokens.Struct:
					if (frame.type == FrameType.Global || frame.type == FrameType.TypeDecl) {
						if (frame.state != FrameState.Constraints) {
							frame.state = FrameState.TypeDecl;
							frame.curlyChildType = FrameType.TypeDecl;
							frame.SetContext(ExpressionContext.IdentifierExpected);
						}
					}
					break;
				case Tokens.Interface:
					if (frame.type == FrameType.Global || frame.type == FrameType.TypeDecl) {
						frame.state = FrameState.TypeDecl;
						frame.curlyChildType = FrameType.Interface;
						frame.SetContext(ExpressionContext.IdentifierExpected);
					}
					break;
				case Tokens.Enum:
					if (frame.type == FrameType.Global || frame.type == FrameType.TypeDecl) {
						frame.state = FrameState.TypeDecl;
						frame.curlyChildType = FrameType.Enum;
						frame.SetContext(ExpressionContext.IdentifierExpected);
					}
					break;
				case Tokens.Delegate:
					if (frame.InExpressionMode) {
						frame.parenthesisChildType = FrameType.ParameterList;
						frame.curlyChildType = FrameType.Statements;
					} else if (frame.type == FrameType.Global || frame.type == FrameType.TypeDecl) {
						frame.parenthesisChildType = FrameType.ParameterList;
						frame.state = FrameState.MethodDecl;
						frame.SetContext(ExpressionContext.Type);
					}
					break;
				case Tokens.LambdaArrow:
					frame.curlyChildType = FrameType.Statements;
					break;
				case Tokens.Event:
					frame.SetContext(ExpressionContext.DelegateType);
					frame.curlyChildType = FrameType.Event;
					frame.state = FrameState.EventDecl;
					break;
				case Tokens.Comma:
					if (frame.state == FrameState.FieldDecl
					    || frame.state == FrameState.FieldDeclAfterIdentifier
					    || frame.state == FrameState.Initializer)
					{
						frame.state = FrameState.FieldDecl;
						frame.SetContext(ExpressionContext.IdentifierExpected);
					} else if (frame.state == FrameState.ObjectInitializerValue) {
						frame.state = FrameState.Normal;
						frame.SetDefaultContext();
					} else if (frame.type == FrameType.Statements) {
						frame.SetContext(ExpressionContext.IdentifierExpected);
					}
					break;
				case Tokens.Where:
					if (!frame.InExpressionMode && (frame.type == FrameType.Global || frame.type == FrameType.TypeDecl)) {
						frame.state = FrameState.Constraints;
						frame.SetDefaultContext();
					}
					break;
				case Tokens.CloseCurlyBrace:
				case Tokens.Semicolon:
					frame.state = FrameState.Normal;
					frame.SetDefaultContext();
					break;
				case Tokens.OpenParenthesis:
					if (frame.parent != null && (frame.parent.state == FrameState.FieldDeclAfterIdentifier
					                             || frame.parent.state == FrameState.FieldDecl))
					{
						frame.type = FrameType.ParameterList;
						frame.SetContext(CSharpExpressionContext.FirstParameterType);
						frame.parent.state = FrameState.MethodDecl;
						frame.parent.curlyChildType = FrameType.Statements;
					}
					break;
				case Tokens.CloseParenthesis:
					if (frame.state == FrameState.StatementWithEmbeddedStatement) {
						frame.state = FrameState.Normal;
						frame.lastExpressionStart = token.EndLocation;
					}
					break;
				case Tokens.Question:
					// IdentifierExpected = this is after a type name = the ? was a nullable marker
					if (frame.context != ExpressionContext.IdentifierExpected) {
						frame.SetDefaultContext();
					}
					break;
				case Tokens.This:
					if (frame.state == FrameState.FieldDecl) {
						// this is an indexer declaration
						frame.squareBracketChildType = FrameType.ParameterList;
						frame.state = FrameState.FieldDeclAfterIdentifier;
					}
					break;
				case Tokens.Goto:
					frame.SetContext(ExpressionContext.IdentifierExpected);
					break;
				case Tokens.As:
				case Tokens.Is:
					frame.SetContext(ExpressionContext.Type);
					break;
				case Tokens.Typeof:
					frame.parenthesisChildType = FrameType.TypeReference;
					break;
				default:
					if (Tokens.SimpleTypeName[token.Kind]) {
						if (frame.type == FrameType.Interface || frame.type == FrameType.TypeDecl) {
							if (frame.state == FrameState.Normal) {
								frame.state = FrameState.FieldDecl;
								frame.curlyChildType = FrameType.Property;
							} else if (frame.state == FrameState.FieldDecl && Tokens.IdentifierTokens[token.Kind]) {
								frame.state = FrameState.FieldDeclAfterIdentifier;
							}
							if (frame.state != FrameState.ObjectCreation) {
								frame.SetContext(ExpressionContext.IdentifierExpected);
							}
						} else if (frame.type == FrameType.ParameterList
						           || frame.type == FrameType.Statements
						           || frame.type == FrameType.Global)
						{
							if (!frame.context.IsObjectCreation && frame.state != FrameState.UsingNamespace) {
								frame.SetContext(ExpressionContext.IdentifierExpected);
							}
						}
					}
					break;
			}
		}
		
		public ExpressionResult FindFullExpression(string text, int offset)
		{
			return FindFullExpression(text, offset, null);
		}
		
		/// <summary>
		/// Like FindFullExpression, but text is a code snippet inside a type declaration.
		/// </summary>
		public ExpressionResult FindFullExpressionInTypeDeclaration(string text, int offset)
		{
			Frame root = new Frame();
			root.curlyChildType = FrameType.TypeDecl;
			Frame typeDecl = new Frame(root, '{');
			return FindFullExpression(text, offset, typeDecl);
		}
		
		
		/// <summary>
		/// Like FindFullExpression, but text is a code snippet inside a method body.
		/// </summary>
		public ExpressionResult FindFullExpressionInMethod(string text, int offset)
		{
			Frame root = new Frame();
			root.curlyChildType = FrameType.TypeDecl;
			Frame typeDecl = new Frame(root, '{');
			typeDecl.curlyChildType = FrameType.Statements;
			Frame methodBody = new Frame(typeDecl, '{');
			return FindFullExpression(text, offset, methodBody);
		}
		
		ExpressionResult FindFullExpression(string text, int offset, Frame initialFrame)
		{
			Init(text, offset);
			
			if (initialFrame != null) {
				frame = initialFrame;
			}
			
			const int SEARCHING_OFFSET = 0;
			const int SEARCHING_END = 1;
			int state = SEARCHING_OFFSET;
			Frame resultFrame = frame;
			int resultStartOffset = -1;
			int alternateResultStartOffset = -1;
			int resultEndOffset = -1;
			ExpressionContext prevContext = ExpressionContext.Default;
			ExpressionContext resultContext = ExpressionContext.Default;
			
			Token token;
			while ((token = lexer.NextToken()) != null) {
				if (token.Kind == Tokens.EOF) break;
				
				if (state == SEARCHING_OFFSET) {
					if (targetPosition < token.Location) {
						resultFrame = frame;
						resultContext = frame.context;
						resultStartOffset = LocationToOffset(frame.lastExpressionStart);
						alternateResultStartOffset = LocationToOffset(frame.lastNewTokenStart);
						if (resultStartOffset < 0)
							break;
						resultEndOffset = LocationToOffset(token.Location);
						state = SEARCHING_END;
					}
				}
				prevContext = frame.context;
				ApplyToken(token);
				if (state == SEARCHING_OFFSET) {
					if (targetPosition < token.EndLocation) {
						resultFrame = frame;
						resultContext = prevContext;
						resultStartOffset = LocationToOffset(frame.lastExpressionStart);
						alternateResultStartOffset = LocationToOffset(frame.lastNewTokenStart);
						resultEndOffset = LocationToOffset(token.EndLocation);
						if (resultStartOffset < 0)
							break;
						state = SEARCHING_END;
					}
				} else if (state == SEARCHING_END) {
					int lastExpressionStartOffset = LocationToOffset(resultFrame.lastExpressionStart);
					if (lastExpressionStartOffset == alternateResultStartOffset && alternateResultStartOffset >= 0)
						resultStartOffset = lastExpressionStartOffset;
					if (resultFrame.type == FrameType.Popped ||
					    lastExpressionStartOffset != resultStartOffset ||
					    token.Kind == Tokens.Dot || token.Kind == Tokens.DoubleColon || token.Kind == Tokens.Pointer)
					{
						
						// now we can change the context based on the next token
						if (frame == resultFrame && Tokens.IdentifierTokens[token.Kind]) {
							// the expression got aborted because of an identifier. This means the
							// expression was a type reference
							resultContext = ExpressionContext.Type;
						} else if (resultFrame.bracketType == '<' && token.Kind == Tokens.GreaterThan) {
							// expression was a type argument
							resultContext = ExpressionContext.Type;
							return MakeResult(text, resultStartOffset, resultEndOffset, resultContext);
						}
						if (frame == resultFrame || resultFrame.type == FrameType.Popped) {
							return MakeResult(text, resultStartOffset, resultEndOffset, resultContext);
						}
					} else {
						if (frame.bracketType != '<') {
							resultEndOffset = LocationToOffset(token.EndLocation);
						}
					}
				}
				lastToken = token.Kind;
			}
			// offset is behind all tokens -> cannot find any expression
			return new ExpressionResult(null, frame.context);
		}
		
		ExpressionResult MakeResult(string text, int startOffset, int endOffset, ExpressionContext context)
		{
			return new ExpressionResult(text.Substring(startOffset, endOffset - startOffset),
			                            DomRegion.FromLocation(OffsetToLocation(startOffset), OffsetToLocation(endOffset)),
			                            context, null);
		}
		
		public string RemoveLastPart(string expression)
		{
			Init(expression, expression.Length - 1);
			int lastValidPos = 0;
			Token token;
			while ((token = lexer.NextToken()) != null) {
				if (token.Kind == Tokens.EOF) break;
				
				if (frame.parent == null) {
					if (token.Kind == Tokens.Dot || token.Kind == Tokens.DoubleColon || token.Kind == Tokens.Pointer
					    || token.Kind == Tokens.OpenParenthesis || token.Kind == Tokens.OpenSquareBracket)
					{
						lastValidPos = LocationToOffset(token.Location);
					}
				}
				ApplyToken(token);
				
				lastToken = token.Kind;
			}
			return expression.Substring(0, lastValidPos);
		}
		
		#region Comment Filter and 'inside string watcher'
		
		// NOTE: FilterComments is not used anymore inside the ExpressionFinder, it should be moved
		// into another class / or removed completely if it is not required anymore.
		
		int initialOffset;
		public string FilterComments(string text, ref int offset)
		{
			if (text.Length <= offset)
				return null;
			this.initialOffset = offset;
			StringBuilder outText = new StringBuilder();
			int curOffset = 0;
			
			while (curOffset <= initialOffset) {
				char ch = text[curOffset];
				
				switch (ch) {
					case '@':
						if (curOffset + 1 < text.Length && text[curOffset + 1] == '"') {
							outText.Append(text[curOffset++]); // @
							outText.Append(text[curOffset++]); // "
							if (!ReadVerbatimString(outText, text, ref curOffset)) {
								return null;
							}
						}else{
							outText.Append(ch);
							++curOffset;
						}
						break;
					case '\'':
						outText.Append(ch);
						curOffset++;
						if(! ReadChar(outText, text, ref curOffset)) {
							return null;
						}
						break;
					case '"':
						outText.Append(ch);
						curOffset++;
						if (!ReadString(outText, text, ref curOffset)) {
							return null;
						}
						break;
					case '/':
						if (curOffset + 1 < text.Length && text[curOffset + 1] == '/') {
							offset    -= 2;
							curOffset += 2;
							if (!ReadToEOL(text, ref curOffset, ref offset)) {
								return null;
							}
						} else if (curOffset + 1 < text.Length && text[curOffset + 1] == '*') {
							offset    -= 2;
							curOffset += 2;
							if (!ReadMultiLineComment(text, ref curOffset, ref offset)) {
								return null;
							}
						} else {
							goto default;
						}
						break;
					case '#':
						if (!ReadToEOL(text, ref curOffset, ref offset)) {
							return null;
						}
						break;
					default:
						outText.Append(ch);
						++curOffset;
						break;
				}



			}
			
			return outText.ToString();

		}
		
		bool ReadToEOL(string text, ref int curOffset, ref int offset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				--offset;
				if (ch == '\n') {
					return true;
				}
			}
			return false;
		}
		
		bool ReadChar(StringBuilder outText, string text, ref int curOffset)
		{
			if (curOffset > initialOffset)
				return false;
			char first = text[curOffset++];
			outText.Append(first);
			if (curOffset > initialOffset)
				return false;
			char second = text[curOffset++];
			outText.Append(second);
			if (first == '\\') {
				// character is escape sequence, so read one char more
				char next;
				do {
					if (curOffset > initialOffset)
						return false;
					next = text[curOffset++];
					outText.Append(next);
					// unicode or hexadecimal character literals can have more content characters
				} while((second == 'u' || second == 'x') && char.IsLetterOrDigit(next));
			}
			return text[curOffset - 1] == '\'';
		}
		
		bool ReadString(StringBuilder outText, string text, ref int curOffset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				outText.Append(ch);
				if (ch == '"') {
					return true;
				} else if (ch == '\\') {
					if (curOffset <= initialOffset)
						outText.Append(text[curOffset++]);
				}
			}
			return false;
		}
		
		bool ReadVerbatimString(StringBuilder outText, string text, ref int curOffset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				outText.Append(ch);
				if (ch == '"') {
					if (curOffset < text.Length && text[curOffset] == '"') {
						outText.Append(text[curOffset++]);
					} else {
						return true;
					}
				}


			}
			return false;

		}
		
		bool ReadMultiLineComment(string text, ref int curOffset, ref int offset)
		{
			while (curOffset <= initialOffset) {
				char ch = text[curOffset++];
				--offset;
				if (ch == '*') {
					if (curOffset < text.Length && text[curOffset] == '/') {
						++curOffset;
						--offset;
						return true;
					}
				}
			}
			return false;
		}
		#endregion
	}
}
