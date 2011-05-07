//
// cs-tokenizer.cs: The Tokenizer for the C# compiler
//                  This also implements the preprocessor
//
// Author: Miguel de Icaza (miguel@gnu.org)
//         Marek Safar (marek.safar@seznam.cz)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
//
//
using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;

namespace Mono.CSharp
{
	/// <summary>
	///    Tokenizer for C# source code. 
	/// </summary>
	public class Tokenizer : yyParser.yyInput
	{
		class KeywordEntry<T>
		{
			public readonly T Token;
			public KeywordEntry<T> Next;
			public readonly char[] Value;

			public KeywordEntry (string value,T token)
			{
				this.Value = value.ToCharArray ();
				this.Token = token;
			}
		}

		sealed class IdentifiersComparer : IEqualityComparer<char[]>
		{
			readonly int length;

			public IdentifiersComparer (int length)
			{
				this.length = length;
			}

			public bool Equals (char[] x, char[] y)
			{
				for (int i = 0; i < length; ++i)
					if (x [i] != y [i])
						return false;

				return true;
			}

			public int GetHashCode (char[] obj)
			{
				int h = 0;
				for (int i = 0; i < length; ++i)
					h = (h << 5) - h + obj [i];

				return h;
			}
		}

		//
		// This class has to be used in the parser only, it reuses token
		// details after each parse
		//
		public class LocatedToken
		{
			int row, column;
			string value;
			static LocatedToken[] buffer;
			static int pos;

			private LocatedToken ()
			{
			}

			public static LocatedToken Create (int row, int column)
			{
				return Create (null, row, column);
			}

			public static LocatedToken Create (string value, int row, int column)
			{
				//
				// TODO: I am not very happy about the logic but it's the best
				// what I could come up with for now.
				// Ideally we should be using just tiny buffer (256 elements) which
				// is enough to hold all details for currect stack and recycle elements
				// poped from the stack but there is a trick needed to recycle
				// them properly.
				//
				LocatedToken entry;
				if (pos >= buffer.Length) {
					entry = new LocatedToken ();
				} else {
					entry = buffer [pos];
					if (entry == null) {
						entry = new LocatedToken ();
						buffer [pos] = entry;
					}

					++pos;
				}
				entry.value = value;
				entry.row = row;
				entry.column = column;
				return entry;
			}

			//
			// Used for token not required by expression evaluator
			//
			[Conditional ("FULL_AST")]
			public static void CreateOptional (int row, int col, ref object token)
			{
				token = Create (row, col);
			}

			public static void Initialize ()
			{
				if (buffer == null)
					buffer = new LocatedToken [10000];
				pos = 0;
			}

			public Location Location {
				get { return new Location (row, column); }
			}

			public string Value {
				get { return value; }
			}
		}

		public enum PreprocessorDirective
		{
			Invalid = 0,

			Region = 1,
			Endregion = 2,
			If = 3 | RequiresArgument,
			Endif = 4,
			Elif = 5 | RequiresArgument,
			Else = 6,
			Define = 7 | RequiresArgument,
			Undef = 8 | RequiresArgument,
			Error = 9,
			Warning = 10,
			Pragma = 11 | CustomArgumentsParsing,
			Line = 12,

			CustomArgumentsParsing = 1 << 10,
			RequiresArgument = 1 << 11
		}

		SeekableStreamReader reader;
		SourceFile ref_name;
		CompilationSourceFile file_name;
		CompilerContext context;
		bool hidden = false;
		int ref_line = 1;
		int line = 1;
		int col = 0;
		int previous_col;
		int current_token;
		int tab_size;
		bool handle_get_set = false;
		bool handle_remove_add = false;
		bool handle_where = false;
		bool handle_typeof = false;
		bool lambda_arguments_parsing;
		List<Location> escaped_identifiers;
		int parsing_generic_less_than;
		readonly bool doc_processing;
		
		//
		// Used mainly for parser optimizations. Some expressions for instance
		// can appear only in block (including initializer, base initializer)
		// scope only
		//
		public int parsing_block;
		internal bool query_parsing;
		
		// 
		// When parsing type only, useful for ambiguous nullable types
		//
		public int parsing_type;
		
		//
		// Set when parsing generic declaration (type or method header)
		//
		public bool parsing_generic_declaration;
		public bool parsing_generic_declaration_doc;
		
		//
		// The value indicates that we have not reach any declaration or
		// namespace yet
		//
		public int parsing_declaration;
		public bool parsing_attribute_section;

		//
		// The special characters to inject on streams to run the unit parser
		// in the special expression mode. Using private characters from
		// Plane Sixteen (U+100000 to U+10FFFD)
		//
		// This character is only tested just before the tokenizer is about to report
		// an error;   So on the regular operation mode, this addition will have no
		// impact on the tokenizer's performance.
		//
		
		public const int EvalStatementParserCharacter = 0x100000;
		public const int EvalCompilationUnitParserCharacter = 0x100001;
		public const int EvalUsingDeclarationsParserCharacter = 0x100002;
		public const int DocumentationXref = 0x100003;
		
		//
		// XML documentation buffer. The save point is used to divide
		// comments on types and comments on members.
		//
		StringBuilder xml_comment_buffer;

		//
		// See comment on XmlCommentState enumeration.
		//
		XmlCommentState xml_doc_state = XmlCommentState.Allowed;

		//
		// Whether tokens have been seen on this line
		//
		bool tokens_seen = false;

		//
		// Set to true once the GENERATE_COMPLETION token has bee
		// returned.   This helps produce one GENERATE_COMPLETION,
		// as many COMPLETE_COMPLETION as necessary to complete the
		// AST tree and one final EOF.
		//
		bool generated;
		
		//
		// Whether a token has been seen on the file
		// This is needed because `define' is not allowed to be used
		// after a token has been seen.
		//
		bool any_token_seen;

		//
		// Class variables
		// 
		static readonly KeywordEntry<int>[][] keywords;
		static readonly KeywordEntry<PreprocessorDirective>[][] keywords_preprocessor;
		static readonly Dictionary<string, object> keyword_strings; 		// TODO: HashSet
		static readonly NumberStyles styles;
		static readonly NumberFormatInfo csharp_format_info;

		// Pragma arguments
		static readonly char[] pragma_warning = "warning".ToCharArray ();
		static readonly char[] pragma_warning_disable = "disable".ToCharArray ();
		static readonly char[] pragma_warning_restore = "restore".ToCharArray ();
		static readonly char[] pragma_checksum = "checksum".ToCharArray ();
		static readonly char[] simple_whitespaces = new char[] { ' ', '\t' };
		bool startsLine = true;
		internal SpecialsBag sbag;

		public bool PropertyParsing {
			get { return handle_get_set; }
			set { handle_get_set = value; }
		}

		public bool EventParsing {
			get { return handle_remove_add; }
			set { handle_remove_add = value; }
		}

		public bool ConstraintsParsing {
			get { return handle_where; }
			set { handle_where = value; }
		}

		public bool TypeOfParsing {
			get { return handle_typeof; }
			set { handle_typeof = value; }
		}

		public int TabSize {
			get { return tab_size; }
			set { tab_size = value; }
		}

		public XmlCommentState doc_state {
			get { return xml_doc_state; }
			set {
				if (value == XmlCommentState.Allowed) {
					check_incorrect_doc_comment ();
					reset_doc_comment ();
				}
				xml_doc_state = value;
			}
		}

		//
		// This is used to trigger completion generation on the parser
		public bool CompleteOnEOF;

		void AddEscapedIdentifier (Location loc)
		{
			if (escaped_identifiers == null)
				escaped_identifiers = new List<Location> ();

			escaped_identifiers.Add (loc);
		}

		public bool IsEscapedIdentifier (MemberName name)
		{
			return escaped_identifiers != null && escaped_identifiers.Contains (name.Location);
		}

		//
		// Values for the associated token returned
		//
		internal int putback_char; 	// Used by repl only
		object val;

		//
		// Pre-processor
		//
		const int TAKING        = 1;
		const int ELSE_SEEN = 4;
		const int PARENT_TAKING = 8;
		const int REGION = 16;		

		//
		// pre-processor if stack state:
		//
		Stack<int> ifstack;

		static System.Text.StringBuilder string_builder;
		const int max_id_size = 512;
		static readonly char[] id_builder = new char [max_id_size];
		public static Dictionary<char[], string>[] identifiers = new Dictionary<char[], string>[max_id_size + 1];
		const int max_number_size = 512;
		static char[] number_builder = new char [max_number_size];
		static int number_pos;
		static char[] value_builder = new char[256];

		public int Line {
			get {
				return ref_line;
			}
		}

		//
		// This is used when the tokenizer needs to save
		// the current position as it needs to do some parsing
		// on its own to deamiguate a token in behalf of the
		// parser.
		//
		Stack<Position> position_stack = new Stack<Position> (2);

		class Position
		{
			public int position;
			public int line;
			public int ref_line;
			public int col;
			public bool hidden;
			public int putback_char;
			public int previous_col;
			public Stack<int> ifstack;
			public int parsing_generic_less_than;
			public int current_token;
			public object val;

			public Position (Tokenizer t)
			{
				position = t.reader.Position;
				line = t.line;
				ref_line = t.ref_line;
				col = t.col;
				hidden = t.hidden;
				putback_char = t.putback_char;
				previous_col = t.previous_col;
				if (t.ifstack != null && t.ifstack.Count != 0) {
					// There is no simple way to clone Stack<T> all
					// methods reverse the order
					var clone = t.ifstack.ToArray ();
					Array.Reverse (clone);
					ifstack = new Stack<int> (clone);
				}
				parsing_generic_less_than = t.parsing_generic_less_than;
				current_token = t.current_token;
				val = t.val;
			}
		}

		public Tokenizer (SeekableStreamReader input, CompilationSourceFile file, CompilerContext ctx)
		{
			this.ref_name = file;
			this.file_name = file;
			this.context = ctx;
			reader = input;

			putback_char = -1;

			xml_comment_buffer = new StringBuilder ();
			doc_processing = ctx.Settings.DocumentationFile != null;

			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				tab_size = 4;
			else
				tab_size = 8;

			//
			// FIXME: This could be `Location.Push' but we have to
			// find out why the MS compiler allows this
			//
			Mono.CSharp.Location.Push (file, file);
		}
		
		public void PushPosition ()
		{
			position_stack.Push (new Position (this));
		}

		public void PopPosition ()
		{
			Position p = position_stack.Pop ();

			reader.Position = p.position;
			ref_line = p.ref_line;
			line = p.line;
			col = p.col;
			hidden = p.hidden;
			putback_char = p.putback_char;
			previous_col = p.previous_col;
			ifstack = p.ifstack;
			parsing_generic_less_than = p.parsing_generic_less_than;
			current_token = p.current_token;
			val = p.val;
		}

		// Do not reset the position, ignore it.
		public void DiscardPosition ()
		{
			position_stack.Pop ();
	}
		
		static void AddKeyword (string kw, int token)
		{
			keyword_strings.Add (kw, null);

			AddKeyword (keywords, kw, token);
}

		static void AddPreprocessorKeyword (string kw, PreprocessorDirective directive)
		{
			AddKeyword (keywords_preprocessor, kw, directive);
		}

		static void AddKeyword<T> (KeywordEntry<T>[][] keywords, string kw, T token)
		{
			int length = kw.Length;
			if (keywords[length] == null) {
				keywords[length] = new KeywordEntry<T>['z' - '_' + 1];
			}

			int char_index = kw[0] - '_';
			var kwe = keywords[length][char_index];
			if (kwe == null) {
				keywords[length][char_index] = new KeywordEntry<T> (kw, token);
				return;
			}

			while (kwe.Next != null) {
				kwe = kwe.Next;
			}

			kwe.Next = new KeywordEntry<T> (kw, token);
		}

		//
		// Class initializer
		// 
		static Tokenizer ()
		{
			keyword_strings = new Dictionary<string, object> ();

			// 11 is the length of the longest keyword for now
			keywords = new KeywordEntry<int>[11][];

			AddKeyword ("__arglist", Token.ARGLIST);
			AddKeyword ("__makeref", Token.MAKEREF);
			AddKeyword ("__reftype", Token.REFTYPE);
			AddKeyword ("__refvalue", Token.REFVALUE);
			AddKeyword ("abstract", Token.ABSTRACT);
			AddKeyword ("as", Token.AS);
			AddKeyword ("add", Token.ADD);
			AddKeyword ("base", Token.BASE);
			AddKeyword ("bool", Token.BOOL);
			AddKeyword ("break", Token.BREAK);
			AddKeyword ("byte", Token.BYTE);
			AddKeyword ("case", Token.CASE);
			AddKeyword ("catch", Token.CATCH);
			AddKeyword ("char", Token.CHAR);
			AddKeyword ("checked", Token.CHECKED);
			AddKeyword ("class", Token.CLASS);
			AddKeyword ("const", Token.CONST);
			AddKeyword ("continue", Token.CONTINUE);
			AddKeyword ("decimal", Token.DECIMAL);
			AddKeyword ("default", Token.DEFAULT);
			AddKeyword ("delegate", Token.DELEGATE);
			AddKeyword ("do", Token.DO);
			AddKeyword ("double", Token.DOUBLE);
			AddKeyword ("else", Token.ELSE);
			AddKeyword ("enum", Token.ENUM);
			AddKeyword ("event", Token.EVENT);
			AddKeyword ("explicit", Token.EXPLICIT);
			AddKeyword ("extern", Token.EXTERN);
			AddKeyword ("false", Token.FALSE);
			AddKeyword ("finally", Token.FINALLY);
			AddKeyword ("fixed", Token.FIXED);
			AddKeyword ("float", Token.FLOAT);
			AddKeyword ("for", Token.FOR);
			AddKeyword ("foreach", Token.FOREACH);
			AddKeyword ("goto", Token.GOTO);
			AddKeyword ("get", Token.GET);
			AddKeyword ("if", Token.IF);
			AddKeyword ("implicit", Token.IMPLICIT);
			AddKeyword ("in", Token.IN);
			AddKeyword ("int", Token.INT);
			AddKeyword ("interface", Token.INTERFACE);
			AddKeyword ("internal", Token.INTERNAL);
			AddKeyword ("is", Token.IS);
			AddKeyword ("lock", Token.LOCK);
			AddKeyword ("long", Token.LONG);
			AddKeyword ("namespace", Token.NAMESPACE);
			AddKeyword ("new", Token.NEW);
			AddKeyword ("null", Token.NULL);
			AddKeyword ("object", Token.OBJECT);
			AddKeyword ("operator", Token.OPERATOR);
			AddKeyword ("out", Token.OUT);
			AddKeyword ("override", Token.OVERRIDE);
			AddKeyword ("params", Token.PARAMS);
			AddKeyword ("private", Token.PRIVATE);
			AddKeyword ("protected", Token.PROTECTED);
			AddKeyword ("public", Token.PUBLIC);
			AddKeyword ("readonly", Token.READONLY);
			AddKeyword ("ref", Token.REF);
			AddKeyword ("remove", Token.REMOVE);
			AddKeyword ("return", Token.RETURN);
			AddKeyword ("sbyte", Token.SBYTE);
			AddKeyword ("sealed", Token.SEALED);
			AddKeyword ("set", Token.SET);
			AddKeyword ("short", Token.SHORT);
			AddKeyword ("sizeof", Token.SIZEOF);
			AddKeyword ("stackalloc", Token.STACKALLOC);
			AddKeyword ("static", Token.STATIC);
			AddKeyword ("string", Token.STRING);
			AddKeyword ("struct", Token.STRUCT);
			AddKeyword ("switch", Token.SWITCH);
			AddKeyword ("this", Token.THIS);
			AddKeyword ("throw", Token.THROW);
			AddKeyword ("true", Token.TRUE);
			AddKeyword ("try", Token.TRY);
			AddKeyword ("typeof", Token.TYPEOF);
			AddKeyword ("uint", Token.UINT);
			AddKeyword ("ulong", Token.ULONG);
			AddKeyword ("unchecked", Token.UNCHECKED);
			AddKeyword ("unsafe", Token.UNSAFE);
			AddKeyword ("ushort", Token.USHORT);
			AddKeyword ("using", Token.USING);
			AddKeyword ("virtual", Token.VIRTUAL);
			AddKeyword ("void", Token.VOID);
			AddKeyword ("volatile", Token.VOLATILE);
			AddKeyword ("while", Token.WHILE);
			AddKeyword ("partial", Token.PARTIAL);
			AddKeyword ("where", Token.WHERE);

			// LINQ keywords
			AddKeyword ("from", Token.FROM);
			AddKeyword ("join", Token.JOIN);
			AddKeyword ("on", Token.ON);
			AddKeyword ("equals", Token.EQUALS);
			AddKeyword ("select", Token.SELECT);
			AddKeyword ("group", Token.GROUP);
			AddKeyword ("by", Token.BY);
			AddKeyword ("let", Token.LET);
			AddKeyword ("orderby", Token.ORDERBY);
			AddKeyword ("ascending", Token.ASCENDING);
			AddKeyword ("descending", Token.DESCENDING);
			AddKeyword ("into", Token.INTO);

			// Contextual async keywords
			AddKeyword ("async", Token.ASYNC);
			AddKeyword ("await", Token.AWAIT);

			keywords_preprocessor = new KeywordEntry<PreprocessorDirective>[10][];

			AddPreprocessorKeyword ("region", PreprocessorDirective.Region);
			AddPreprocessorKeyword ("endregion", PreprocessorDirective.Endregion);
			AddPreprocessorKeyword ("if", PreprocessorDirective.If);
			AddPreprocessorKeyword ("endif", PreprocessorDirective.Endif);
			AddPreprocessorKeyword ("elif", PreprocessorDirective.Elif);
			AddPreprocessorKeyword ("else", PreprocessorDirective.Else);
			AddPreprocessorKeyword ("define", PreprocessorDirective.Define);
			AddPreprocessorKeyword ("undef", PreprocessorDirective.Undef);
			AddPreprocessorKeyword ("error", PreprocessorDirective.Error);
			AddPreprocessorKeyword ("warning", PreprocessorDirective.Warning);
			AddPreprocessorKeyword ("pragma", PreprocessorDirective.Pragma);
			AddPreprocessorKeyword ("line", PreprocessorDirective.Line);

			csharp_format_info = NumberFormatInfo.InvariantInfo;
			styles = NumberStyles.Float;

			string_builder = new System.Text.StringBuilder ();
		}

		int GetKeyword (char[] id, int id_len)
		{
			//
			// Keywords are stored in an array of arrays grouped by their
			// length and then by the first character
			//
			if (id_len >= keywords.Length || keywords [id_len] == null)
				return -1;

			int first_index = id [0] - '_';
			if (first_index > 'z' - '_')
				return -1;

			var kwe = keywords [id_len] [first_index];
			if (kwe == null)
				return -1;

			int res;
			do {
				res = kwe.Token;
				for (int i = 1; i < id_len; ++i) {
					if (id [i] != kwe.Value [i]) {
						res = 0;
						kwe = kwe.Next;
						break;
					}
				}
			} while (res == 0 && kwe != null);

			if (res == 0)
				return -1;

			int next_token;
			switch (res) {
			case Token.GET:
			case Token.SET:
				if (!handle_get_set)
					res = -1;
				break;
			case Token.REMOVE:
			case Token.ADD:
				if (!handle_remove_add)
					res = -1;
				break;
			case Token.EXTERN:
				if (parsing_declaration == 0)
					res = Token.EXTERN_ALIAS;
				break;
			case Token.DEFAULT:
				if (peek_token () == Token.COLON) {
					token ();
					res = Token.DEFAULT_COLON;
				}
				break;
			case Token.WHERE:
				if (!handle_where && !query_parsing)
					res = -1;
				break;
			case Token.FROM:
				//
				// A query expression is any expression that starts with `from identifier'
				// followed by any token except ; , =
				// 
				if (!query_parsing) {
					if (lambda_arguments_parsing) {
						res = -1;
						break;
					}

					PushPosition ();
					// HACK: to disable generics micro-parser, because PushPosition does not
					// store identifiers array
					parsing_generic_less_than = 1;
					switch (xtoken ()) {
					case Token.IDENTIFIER:
					case Token.INT:
					case Token.BOOL:
					case Token.BYTE:
					case Token.CHAR:
					case Token.DECIMAL:
					case Token.FLOAT:
					case Token.LONG:
					case Token.OBJECT:
					case Token.STRING:
					case Token.UINT:
					case Token.ULONG:
						next_token = xtoken ();
						if (next_token == Token.SEMICOLON || next_token == Token.COMMA || next_token == Token.EQUALS)
							goto default;
						
						res = Token.FROM_FIRST;
						query_parsing = true;
						if (context.Settings.Version <= LanguageVersion.ISO_2)
							Report.FeatureIsNotAvailable (context, Location, "query expressions");
						break;
					case Token.VOID:
						Expression.Error_VoidInvalidInTheContext (Location, Report);
						break;
					default:
						PopPosition ();
						// HACK: A token is not a keyword so we need to restore identifiers buffer
						// which has been overwritten before we grabbed the identifier
						id_builder [0] = 'f'; id_builder [1] = 'r'; id_builder [2] = 'o'; id_builder [3] = 'm';
						return -1;
					}
					PopPosition ();
				}
				break;
			case Token.JOIN:
			case Token.ON:
			case Token.EQUALS:
			case Token.SELECT:
			case Token.GROUP:
			case Token.BY:
			case Token.LET:
			case Token.ORDERBY:
			case Token.ASCENDING:
			case Token.DESCENDING:
			case Token.INTO:
				if (!query_parsing)
					res = -1;
				break;
				
			case Token.USING:
			case Token.NAMESPACE:
				// TODO: some explanation needed
				check_incorrect_doc_comment ();
				break;
				
			case Token.PARTIAL:
				if (parsing_block > 0) {
					res = -1;
					break;
				}

				// Save current position and parse next token.
				PushPosition ();

				next_token = token ();
				bool ok = (next_token == Token.CLASS) ||
					(next_token == Token.STRUCT) ||
					(next_token == Token.INTERFACE) ||
					(next_token == Token.VOID);

				PopPosition ();

				if (ok) {
					if (next_token == Token.VOID) {
						if (context.Settings.Version <= LanguageVersion.ISO_2)
							Report.FeatureIsNotAvailable (context, Location, "partial methods");
					} else if (context.Settings.Version == LanguageVersion.ISO_1)
						Report.FeatureIsNotAvailable (context, Location, "partial types");

					return res;
				}

				if (next_token < Token.LAST_KEYWORD) {
					Report.Error (267, Location,
						"The `partial' modifier can be used only immediately before `class', `struct', `interface', or `void' keyword");
					return token ();
				}					

				res = -1;
				break;

			// TODO: async, it's modifiers context only
			case Token.ASYNC:
				if (parsing_block > 0 || context.Settings.Version != LanguageVersion.Future) {
					res = -1;
				}
				break;

			// TODO: async, it's async block context only
			case Token.AWAIT:
				if (context.Settings.Version != LanguageVersion.Future) {
					res = -1;
				}

				break;
			}

			return res;
			return res;
		}

		static PreprocessorDirective GetPreprocessorDirective (char[] id, int id_len)
		{
			//
			// Keywords are stored in an array of arrays grouped by their
			// length and then by the first character
			//
			if (id_len >= keywords_preprocessor.Length || keywords_preprocessor[id_len] == null)
				return PreprocessorDirective.Invalid;

			int first_index = id[0] - '_';
			if (first_index > 'z' - '_')
				return PreprocessorDirective.Invalid;

			var kwe = keywords_preprocessor[id_len][first_index];
			if (kwe == null)
				return PreprocessorDirective.Invalid;

			PreprocessorDirective res = PreprocessorDirective.Invalid;
			do {
				res = kwe.Token;
				for (int i = 1; i < id_len; ++i) {
					if (id[i] != kwe.Value[i]) {
						res = 0;
						kwe = kwe.Next;
						break;
					}
				}
			} while (res == PreprocessorDirective.Invalid && kwe != null);

			return res;
		}

		public Location Location {
			get {
				return new Location (ref_line, hidden ? -1 : col);
			}
		}

		static bool is_identifier_start_character (int c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || Char.IsLetter ((char)c);
		}

		static bool is_identifier_part_character (char c)
		{
			if (c >= 'a' && c <= 'z')
				return true;

			if (c >= 'A' && c <= 'Z')
				return true;

			if (c == '_' || (c >= '0' && c <= '9'))
				return true;

			if (c < 0x80)
				return false;

			return Char.IsLetter (c) || Char.GetUnicodeCategory (c) == UnicodeCategory.ConnectorPunctuation;
		}

		public static bool IsKeyword (string s)
		{
			return keyword_strings.ContainsKey (s);
		}

		//
		// Open parens micro parser. Detects both lambda and cast ambiguity.
		//	
		int TokenizeOpenParens ()
		{
			int ptoken;
			current_token = -1;

			int bracket_level = 0;
			bool is_type = false;
			bool can_be_type = false;
			
			while (true) {
				ptoken = current_token;
				token ();

				switch (current_token) {
				case Token.CLOSE_PARENS:
					token ();
					
					//
					// Expression inside parens is lambda, (int i) => 
					//
					if (current_token == Token.ARROW)
						return Token.OPEN_PARENS_LAMBDA;

					//
					// Expression inside parens is single type, (int[])
					//
					if (is_type)
						return Token.OPEN_PARENS_CAST;

					//
					// Expression is possible cast, look at next token, (T)null
					//
					if (can_be_type) {
						switch (current_token) {
						case Token.OPEN_PARENS:
						case Token.BANG:
						case Token.TILDE:
						case Token.IDENTIFIER:
						case Token.LITERAL:
						case Token.BASE:
						case Token.CHECKED:
						case Token.DELEGATE:
						case Token.FALSE:
						case Token.FIXED:
						case Token.NEW:
						case Token.NULL:
						case Token.SIZEOF:
						case Token.THIS:
						case Token.THROW:
						case Token.TRUE:
						case Token.TYPEOF:
						case Token.UNCHECKED:
						case Token.UNSAFE:
						case Token.DEFAULT:

						//
						// These can be part of a member access
						//
						case Token.INT:
						case Token.UINT:
						case Token.SHORT:
						case Token.USHORT:
						case Token.LONG:
						case Token.ULONG:
						case Token.DOUBLE:
						case Token.FLOAT:
						case Token.CHAR:
						case Token.BYTE:
						case Token.DECIMAL:
						case Token.BOOL:
							return Token.OPEN_PARENS_CAST;
						}
					}
					return Token.OPEN_PARENS;
					
				case Token.DOT:
				case Token.DOUBLE_COLON:
					if (ptoken != Token.IDENTIFIER && ptoken != Token.OP_GENERICS_GT)
						goto default;

					continue;

				case Token.IDENTIFIER:
					switch (ptoken) {
					case Token.DOT:
						if (bracket_level == 0) {
							is_type = false;
							can_be_type = true;
						}

						continue;
					case Token.OP_GENERICS_LT:
					case Token.COMMA:
					case Token.DOUBLE_COLON:
					case -1:
						if (bracket_level == 0)
							can_be_type = true;
						continue;
					default:
						can_be_type = is_type = false;
						continue;
					}

				case Token.OBJECT:
				case Token.STRING:
				case Token.BOOL:
				case Token.DECIMAL:
				case Token.FLOAT:
				case Token.DOUBLE:
				case Token.SBYTE:
				case Token.BYTE:
				case Token.SHORT:
				case Token.USHORT:
				case Token.INT:
				case Token.UINT:
				case Token.LONG:
				case Token.ULONG:
				case Token.CHAR:
				case Token.VOID:
					if (bracket_level == 0)
						is_type = true;
					continue;

				case Token.COMMA:
					if (bracket_level == 0) {
						bracket_level = 100;
						can_be_type = is_type = false;
					}
					continue;

				case Token.OP_GENERICS_LT:
				case Token.OPEN_BRACKET:
					if (bracket_level++ == 0)
						is_type = true;
					continue;

				case Token.OP_GENERICS_GT:
				case Token.CLOSE_BRACKET:
					--bracket_level;
					continue;

				case Token.INTERR_NULLABLE:
				case Token.STAR:
					if (bracket_level == 0)
						is_type = true;
					continue;

				case Token.REF:
				case Token.OUT:
					can_be_type = is_type = false;
					continue;

				default:
					return Token.OPEN_PARENS;
				}
			}
		}

		public static bool IsValidIdentifier (string s)
		{
			if (s == null || s.Length == 0)
				return false;

			if (!is_identifier_start_character (s [0]))
				return false;
			
			for (int i = 1; i < s.Length; i ++)
				if (! is_identifier_part_character (s [i]))
					return false;
			
			return true;
		}

		bool parse_less_than ()
		{
		start:
			int the_token = token ();
			if (the_token == Token.OPEN_BRACKET) {
				do {
					the_token = token ();
				} while (the_token != Token.CLOSE_BRACKET);
				the_token = token ();
			} else if (the_token == Token.IN || the_token == Token.OUT) {
				the_token = token ();
			}
			switch (the_token) {
			case Token.IDENTIFIER:
			case Token.OBJECT:
			case Token.STRING:
			case Token.BOOL:
			case Token.DECIMAL:
			case Token.FLOAT:
			case Token.DOUBLE:
			case Token.SBYTE:
			case Token.BYTE:
			case Token.SHORT:
			case Token.USHORT:
			case Token.INT:
			case Token.UINT:
			case Token.LONG:
			case Token.ULONG:
			case Token.CHAR:
			case Token.VOID:
				break;
			case Token.OP_GENERICS_GT:
				return true;

			default:
				return false;
			}
		again:
			the_token = token ();

			if (the_token == Token.OP_GENERICS_GT)
				return true;
			else if (the_token == Token.COMMA || the_token == Token.DOT || the_token == Token.DOUBLE_COLON)
				goto start;
			else if (the_token == Token.INTERR_NULLABLE || the_token == Token.STAR)
				goto again;
			else if (the_token == Token.OP_GENERICS_LT) {
				if (!parse_less_than ())
					return false;
				goto again;
			} else if (the_token == Token.OPEN_BRACKET) {
			rank_specifiers:
				the_token = token ();
				if (the_token == Token.CLOSE_BRACKET)
					goto again;
				else if (the_token == Token.COMMA)
					goto rank_specifiers;
				return false;
			}

			return false;
		}

		bool parse_generic_dimension (out int dimension)
		{
			dimension = 1;

		again:
			int the_token = token ();
			if (the_token == Token.OP_GENERICS_GT)
				return true;
			else if (the_token == Token.COMMA) {
				dimension++;
				goto again;
			}

			return false;
		}
		
		public int peek_token ()
		{
			int the_token;

			PushPosition ();
			the_token = token ();
			PopPosition ();
			
			return the_token;
		}
					
		//
		// Tonizes `?' using custom disambiguous rules to return one
		// of following tokens: INTERR_NULLABLE, OP_COALESCING, INTERR
		//
		// Tricky expression look like:
		//
		// Foo ? a = x ? b : c;
		//
		int TokenizePossibleNullableType ()
		{
			if (parsing_block == 0 || parsing_type > 0)
				return Token.INTERR_NULLABLE;

			int d = peek_char ();
			if (d == '?') {
				get_char ();
				return Token.OP_COALESCING;
			}

			switch (current_token) {
			case Token.CLOSE_PARENS:
			case Token.TRUE:
			case Token.FALSE:
			case Token.NULL:
			case Token.LITERAL:
				return Token.INTERR;
			}

			if (d != ' ') {
				if (d == ',' || d == ';' || d == '>')
					return Token.INTERR_NULLABLE;
				if (d == '*' || (d >= '0' && d <= '9'))
					return Token.INTERR;
			}

			PushPosition ();
			current_token = Token.NONE;
			int next_token;
			switch (xtoken ()) {
			case Token.LITERAL:
			case Token.TRUE:
			case Token.FALSE:
			case Token.NULL:
			case Token.THIS:
			case Token.NEW:
				next_token = Token.INTERR;
				break;
				
			case Token.SEMICOLON:
			case Token.COMMA:
			case Token.CLOSE_PARENS:
			case Token.OPEN_BRACKET:
			case Token.OP_GENERICS_GT:
			case Token.INTERR:
				next_token = Token.INTERR_NULLABLE;
				break;
				
			default:
				next_token = -1;
				break;
			}

			if (next_token == -1) {
				switch (xtoken ()) {
				case Token.COMMA:
				case Token.SEMICOLON:
				case Token.OPEN_BRACE:
				case Token.CLOSE_PARENS:
				case Token.IN:
					next_token = Token.INTERR_NULLABLE;
					break;
					
				case Token.COLON:
					next_token = Token.INTERR;
					break;							
					
				default:
					int ntoken;
					int interrs = 1;
					int colons = 0;
					//
					// All shorcuts failed, do it hard way
					//
					while ((ntoken = xtoken ()) != Token.EOF) {
						if (ntoken == Token.SEMICOLON)
							break;
						
						if (ntoken == Token.COLON) {
							if (++colons == interrs)
								break;
							continue;
						}
						
						if (ntoken == Token.INTERR) {
							++interrs;
							continue;
						}
					}
					
					next_token = colons != interrs ? Token.INTERR_NULLABLE : Token.INTERR;
					break;
				}
			}
			
			PopPosition ();
			return next_token;
		}

		bool decimal_digits (int c)
		{
			int d;
			bool seen_digits = false;
			
			if (c != -1){
				if (number_pos == max_number_size)
					Error_NumericConstantTooLong ();
				number_builder [number_pos++] = (char) c;
			}
			
			//
			// We use peek_char2, because decimal_digits needs to do a 
			// 2-character look-ahead (5.ToString for example).
			//
			while ((d = peek_char2 ()) != -1){
				if (d >= '0' && d <= '9'){
					if (number_pos == max_number_size)
						Error_NumericConstantTooLong ();
					number_builder [number_pos++] = (char) d;
					get_char ();
					seen_digits = true;
				} else
					break;
			}
			
			return seen_digits;
		}

		static bool is_hex (int e)
		{
			return (e >= '0' && e <= '9') || (e >= 'A' && e <= 'F') || (e >= 'a' && e <= 'f');
		}

		static TypeCode real_type_suffix (int c)
		{
			switch (c){
			case 'F': case 'f':
				return TypeCode.Single;
			case 'D': case 'd':
				return TypeCode.Double;
			case 'M': case 'm':
				return TypeCode.Decimal;
			default:
				return TypeCode.Empty;
			}
		}

		int integer_type_suffix (ulong ul, int c)
		{
			bool is_unsigned = false;
			bool is_long = false;

			if (c != -1){
				bool scanning = true;
				do {
					switch (c){
					case 'U': case 'u':
						if (is_unsigned)
							scanning = false;
						is_unsigned = true;
						get_char ();
						break;

					case 'l':
						if (!is_unsigned){
							//
							// if we have not seen anything in between
							// report this error
							//
							Report.Warning (78, 4, Location, "The 'l' suffix is easily confused with the digit '1' (use 'L' for clarity)");
						}

						goto case 'L';

					case 'L': 
						if (is_long)
							scanning = false;
						is_long = true;
						get_char ();
						break;
						
					default:
						scanning = false;
						break;
					}
					c = peek_char ();
				} while (scanning);
			}

			if (is_long && is_unsigned){
				val = new ULongLiteral (context.BuiltinTypes, ul, Location);
				return Token.LITERAL;
			}
			
			if (is_unsigned){
				// uint if possible, or ulong else.

				if ((ul & 0xffffffff00000000) == 0)
					val = new UIntLiteral (context.BuiltinTypes, (uint) ul, Location);
				else
					val = new ULongLiteral (context.BuiltinTypes, ul, Location);
			} else if (is_long){
				// long if possible, ulong otherwise
				if ((ul & 0x8000000000000000) != 0)
					val = new ULongLiteral (context.BuiltinTypes, ul, Location);
				else
					val = new LongLiteral (context.BuiltinTypes, (long) ul, Location);
			} else {
				// int, uint, long or ulong in that order
				if ((ul & 0xffffffff00000000) == 0){
					uint ui = (uint) ul;
					
					if ((ui & 0x80000000) != 0)
						val = new UIntLiteral (context.BuiltinTypes, ui, Location);
					else
						val = new IntLiteral (context.BuiltinTypes, (int) ui, Location);
				} else {
					if ((ul & 0x8000000000000000) != 0)
						val = new ULongLiteral (context.BuiltinTypes, ul, Location);
					else
						val = new LongLiteral (context.BuiltinTypes, (long) ul, Location);
				}
			}
			return Token.LITERAL;
		}
				
		//
		// given `c' as the next char in the input decide whether
		// we need to convert to a special type, and then choose
		// the best representation for the integer
		//
		int adjust_int (int c)
		{
			try {
				if (number_pos > 9){
					ulong ul = (uint) (number_builder [0] - '0');

					for (int i = 1; i < number_pos; i++){
						ul = checked ((ul * 10) + ((uint)(number_builder [i] - '0')));
					}
					return integer_type_suffix (ul, c);
				} else {
					uint ui = (uint) (number_builder [0] - '0');

					for (int i = 1; i < number_pos; i++){
						ui = checked ((ui * 10) + ((uint)(number_builder [i] - '0')));
					}
					return integer_type_suffix (ui, c);
				}
			} catch (OverflowException) {
				Error_NumericConstantTooLong ();
				val = new IntLiteral (context.BuiltinTypes, 0, Location);
				return Token.LITERAL;
			}
			catch (FormatException) {
				Report.Error (1013, Location, "Invalid number");
				val = new IntLiteral (context.BuiltinTypes, 0, Location);
				return Token.LITERAL;
			}
		}
		
		int adjust_real (TypeCode t)
		{
			string s = new String (number_builder, 0, number_pos);
			const string error_details = "Floating-point constant is outside the range of type `{0}'";

			switch (t){
			case TypeCode.Decimal:
				try {
					val = new DecimalLiteral (context.BuiltinTypes, decimal.Parse (s, styles, csharp_format_info), Location);
				} catch (OverflowException) {
					val = new DecimalLiteral (context.BuiltinTypes, 0, Location);
					Report.Error (594, Location, error_details, "decimal");
				}
				break;
			case TypeCode.Single:
				try {
					val = new FloatLiteral (context.BuiltinTypes, float.Parse (s, styles, csharp_format_info), Location);
				} catch (OverflowException) {
					val = new FloatLiteral (context.BuiltinTypes, 0, Location);
					Report.Error (594, Location, error_details, "float");
				}
				break;
			default:
				try {
					val = new DoubleLiteral (context.BuiltinTypes, double.Parse (s, styles, csharp_format_info), Location);
				} catch (OverflowException) {
					val = new DoubleLiteral (context.BuiltinTypes, 0, Location);
					Report.Error (594, Location, error_details, "double");
				}
				break;
			}

			return Token.LITERAL;
		}

		int handle_hex ()
		{
			int d;
			ulong ul;
			
			get_char ();
			while ((d = peek_char ()) != -1){
				if (is_hex (d)){
					number_builder [number_pos++] = (char) d;
					get_char ();
				} else
					break;
			}
			
			string s = new String (number_builder, 0, number_pos);
			try {
				if (number_pos <= 8)
					ul = System.UInt32.Parse (s, NumberStyles.HexNumber);
				else
					ul = System.UInt64.Parse (s, NumberStyles.HexNumber);
			} catch (OverflowException){
				Error_NumericConstantTooLong ();
				val = new IntLiteral (context.BuiltinTypes, 0, Location);
				return Token.LITERAL;
			}
			catch (FormatException) {
				Report.Error (1013, Location, "Invalid number");
				val = new IntLiteral (context.BuiltinTypes, 0, Location);
				return Token.LITERAL;
			}
			
			return integer_type_suffix (ul, peek_char ());
		}

		//
		// Invoked if we know we have .digits or digits
		//
		int is_number (int c)
		{
			bool is_real = false;

			number_pos = 0;

			if (c >= '0' && c <= '9'){
				if (c == '0'){
					int peek = peek_char ();

					if (peek == 'x' || peek == 'X')
						return handle_hex ();
				}
				decimal_digits (c);
				c = get_char ();
			}

			//
			// We need to handle the case of
			// "1.1" vs "1.string" (LITERAL_FLOAT vs NUMBER DOT IDENTIFIER)
			//
			if (c == '.'){
				if (decimal_digits ('.')){
					is_real = true;
					c = get_char ();
				} else {
					putback ('.');
					number_pos--;
					return adjust_int (-1);
				}
			}
			
			if (c == 'e' || c == 'E'){
				is_real = true;
				if (number_pos == max_number_size)
					Error_NumericConstantTooLong ();
				number_builder [number_pos++] = 'e';
				c = get_char ();
				
				if (c == '+'){
					if (number_pos == max_number_size)
						Error_NumericConstantTooLong ();
					number_builder [number_pos++] = '+';
					c = -1;
				} else if (c == '-') {
					if (number_pos == max_number_size)
						Error_NumericConstantTooLong ();
					number_builder [number_pos++] = '-';
					c = -1;
				} else {
					if (number_pos == max_number_size)
						Error_NumericConstantTooLong ();
					number_builder [number_pos++] = '+';
				}
					
				decimal_digits (c);
				c = get_char ();
			}

			var type = real_type_suffix (c);
			if (type == TypeCode.Empty && !is_real){
				putback (c);
				return adjust_int (c);
			}

			is_real = true;

			if (type == TypeCode.Empty){
				putback (c);
			}
			
			if (is_real)
				return adjust_real (type);

			throw new Exception ("Is Number should never reach this point");
		}

		//
		// Accepts exactly count (4 or 8) hex, no more no less
		//
		int getHex (int count, out int surrogate, out bool error)
		{
			int i;
			int total = 0;
			int c;
			int top = count != -1 ? count : 4;
			
			get_char ();
			error = false;
			surrogate = 0;
			for (i = 0; i < top; i++){
				c = get_char ();

				if (c >= '0' && c <= '9')
					c = (int) c - (int) '0';
				else if (c >= 'A' && c <= 'F')
					c = (int) c - (int) 'A' + 10;
				else if (c >= 'a' && c <= 'f')
					c = (int) c - (int) 'a' + 10;
				else {
					error = true;
					return 0;
				}
				
				total = (total * 16) + c;
				if (count == -1){
					int p = peek_char ();
					if (p == -1)
						break;
					if (!is_hex ((char)p))
						break;
				}
			}

			if (top == 8) {
				if (total > 0x0010FFFF) {
					error = true;
					return 0;
				}

				if (total >= 0x00010000) {
					surrogate = ((total - 0x00010000) % 0x0400 + 0xDC00);					
					total = ((total - 0x00010000) / 0x0400 + 0xD800);
				}
			}

			return total;
		}

		int escape (int c, out int surrogate)
		{
			bool error;
			int d;
			int v;

			d = peek_char ();
			if (c != '\\') {
				surrogate = 0;
				return c;
			}
			
			switch (d){
			case 'a':
				v = '\a'; break;
			case 'b':
				v = '\b'; break;
			case 'n':
				v = '\n'; break;
			case 't':
				v = '\t'; break;
			case 'v':
				v = '\v'; break;
			case 'r':
				v = '\r'; break;
			case '\\':
				v = '\\'; break;
			case 'f':
				v = '\f'; break;
			case '0':
				v = 0; break;
			case '"':
				v = '"'; break;
			case '\'':
				v = '\''; break;
			case 'x':
				v = getHex (-1, out surrogate, out error);
				if (error)
					goto default;
				return v;
			case 'u':
			case 'U':
				return EscapeUnicode (d, out surrogate);
			default:
				surrogate = 0;
				Report.Error (1009, Location, "Unrecognized escape sequence `\\{0}'", ((char)d).ToString ());
				return d;
			}

			get_char ();
			surrogate = 0;
			return v;
		}

		int EscapeUnicode (int ch, out int surrogate)
		{
			bool error;
			if (ch == 'U') {
				ch = getHex (8, out surrogate, out error);
			} else {
				ch = getHex (4, out surrogate, out error);
			}

			if (error)
				Report.Error (1009, Location, "Unrecognized escape sequence");

			return ch;
		}

		int get_char ()
		{
			int x;
			if (putback_char != -1) {
				x = putback_char;
				putback_char = -1;
			} else {
				x = reader.Read ();
			}
			
			if (x == '\r') {
				if (peek_char () == '\n') {
					putback_char = -1;
				}

				x = '\n';
				advance_line ();
			} else if (x == '\n') {
				advance_line ();
			} else {
				col++;
			}
			return x;
		}

		void advance_line ()
		{
			line++;
			ref_line++;
			previous_col = col;
			col = 0;
			startsLine = true;
		}

		int peek_char ()
		{
			if (putback_char == -1)
				putback_char = reader.Read ();
			return putback_char;
		}

		int peek_char2 ()
		{
			if (putback_char != -1)
				return putback_char;
			return reader.Peek ();
		}
		
		void putback (int c)
		{
			if (putback_char != -1){
				Console.WriteLine ("Col: " + col);
				Console.WriteLine ("Row: " + line);
				Console.WriteLine ("Name: " + ref_name.Name);
				Console.WriteLine ("Current [{0}] putting back [{1}]  ", putback_char, c);
				throw new Exception ("This should not happen putback on putback");
			}
			if (c == '\n' || col == 0) {
				// It won't happen though.
				line--;
				ref_line--;
				col = previous_col;
			}
			else
				col--;
			putback_char = c;
		}

		public bool advance ()
		{
			return peek_char () != -1 || CompleteOnEOF;
		}

		public Object Value {
			get {
				return val;
			}
		}

		public Object value ()
		{
			return val;
		}

		public int token ()
		{
			current_token = xtoken ();
			return current_token;
		}

		int TokenizePreprocessorIdentifier (out int c)
		{
			int endLine, endCol;
			return TokenizePreprocessorIdentifier (out c, out endLine, out endCol);
		}

		int TokenizePreprocessorIdentifier (out int c, out int endLine, out int endCol)
		{
			// skip over white space
			do {
				c = get_char ();
			} while (c == ' ' || c == '\t');

			endLine = line;
			endCol = col;
			int pos = 0;
			while (c != -1 && c >= 'a' && c <= 'z') {
				id_builder[pos++] = (char) c;
				endCol = col + 1;
				c = get_char ();
				if (c == '\\') {
					int peek = peek_char ();
					if (peek == 'U' || peek == 'u') {
						int surrogate;
						c = EscapeUnicode (c, out surrogate);
						if (surrogate != 0) {
							if (is_identifier_part_character ((char) c)) {
								id_builder[pos++] = (char) c;
							}
							c = surrogate;
						}
					}
				}
			}

			return pos;
		}

		PreprocessorDirective get_cmd_arg (out string arg)
		{
			int c;
			int startLine = line, startCol = col;

			tokens_seen = false;
			arg = "";
			
			int endLine, endCol;
			var cmd = GetPreprocessorDirective (id_builder, TokenizePreprocessorIdentifier (out c, out endLine, out endCol));
			
			if ((cmd & PreprocessorDirective.CustomArgumentsParsing) != 0) {
				sbag.AddPreProcessorDirective (startLine, startCol, line, col, cmd, null);
				return cmd;
			}
			

			// skip over white space
			while (c == ' ' || c == '\t')
				c = get_char ();

			int has_identifier_argument = (int)(cmd & PreprocessorDirective.RequiresArgument);

			int pos = 0;

			while (c != -1 && c != '\n' && c != '\r') {
				if (c == '\\' && has_identifier_argument >= 0) {
					if (has_identifier_argument != 0) {
						has_identifier_argument = 1;

						int peek = peek_char ();
						if (peek == 'U' || peek == 'u') {
							int surrogate;
							c = EscapeUnicode (c, out surrogate);
							if (surrogate != 0) {
								if (is_identifier_part_character ((char)c)) {
									if (pos == value_builder.Length)
										Array.Resize (ref value_builder, pos * 2);

									value_builder [pos++] = (char)c;
								}
								c = surrogate;
							}
						}
					} else {
						has_identifier_argument = -1;
					}
				} else if (c == '/' && peek_char () == '/') {
					//
					// Eat single-line comments
					//
					get_char ();
					do {
						c = get_char ();
					} while (c != -1 && c != '\n');

					break;
				}

				endLine = line;
				endCol = col;
				
				if (pos == value_builder.Length)
					Array.Resize (ref value_builder, pos * 2);

				value_builder[pos++] = (char) c;
				c = get_char ();
			}

			if (pos != 0) {
				if (pos > max_id_size)
					arg = new string (value_builder, 0, pos);
				else
					arg = InternIdentifier (value_builder, pos);

				// Eat any trailing whitespaces
				arg = arg.Trim (simple_whitespaces);
			}
			sbag.AddPreProcessorDirective (startLine, startCol, endLine, endCol, cmd, arg);

			return cmd;
		}

		//
		// Handles the #line directive
		//
		bool PreProcessLine (string arg)
		{
			if (arg.Length == 0)
				return false;

			if (arg == "default"){
				ref_line = line;
				ref_name = file_name;
				hidden = false;
				Location.Push (file_name, ref_name);
				return true;
			} else if (arg == "hidden"){
				hidden = true;
				return true;
			}
			
			try {
				int pos;

				if ((pos = arg.IndexOf (' ')) != -1 && pos != 0){
					ref_line = System.Int32.Parse (arg.Substring (0, pos));
					pos++;
					
					char [] quotes = { '\"' };
					
					string name = arg.Substring (pos). Trim (quotes);
					ref_name = context.LookupFile (file_name, name);
					file_name.AddIncludeFile (ref_name);
					hidden = false;
					Location.Push (file_name, ref_name);
				} else {
					ref_line = System.Int32.Parse (arg);
					hidden = false;
				}
			} catch {
				return false;
			}
			
			return true;
		}

		//
		// Handles #define and #undef
		//
		void PreProcessDefinition (bool is_define, string ident, bool caller_is_taking)
		{
			if (ident.Length == 0 || ident == "true" || ident == "false"){
				Report.Error (1001, Location, "Missing identifier to pre-processor directive");
				return;
			}

			if (ident.IndexOfAny (simple_whitespaces) != -1){
				Error_EndLineExpected ();
				return;
			}

			if (!is_identifier_start_character (ident [0]))
				Report.Error (1001, Location, "Identifier expected: {0}", ident);
			
			foreach (char c in ident.Substring (1)){
				if (!is_identifier_part_character (c)){
					Report.Error (1001, Location, "Identifier expected: {0}",  ident);
					return;
				}
			}

			if (!caller_is_taking)
				return;

			if (is_define) {
				//
				// #define ident
				//
				if (context.Settings.IsConditionalSymbolDefined (ident))
					return;

				file_name.AddDefine (ident);
			} else {
				//
				// #undef ident
				//
				file_name.AddUndefine (ident);
			}
		}

		byte read_hex (out bool error)
		{
			int total;
			int c = get_char ();

			if ((c >= '0') && (c <= '9'))
				total = (int) c - (int) '0';
			else if ((c >= 'A') && (c <= 'F'))
				total = (int) c - (int) 'A' + 10;
			else if ((c >= 'a') && (c <= 'f'))
				total = (int) c - (int) 'a' + 10;
			else {
				error = true;
				return 0;
			}

			total *= 16;
			c = get_char ();

			if ((c >= '0') && (c <= '9'))
				total += (int) c - (int) '0';
			else if ((c >= 'A') && (c <= 'F'))
				total += (int) c - (int) 'A' + 10;
			else if ((c >= 'a') && (c <= 'f'))
				total += (int) c - (int) 'a' + 10;
			else {
				error = true;
				return 0;
			}

			error = false;
			return (byte) total;
		}

		//
		// Parses #pragma checksum
		//
		bool ParsePragmaChecksum ()
		{
			//
			// The syntax is ` "foo.txt" "{guid}" "hash"'
			//
			int c = get_char ();

			if (c != '"')
				return false;

			string_builder.Length = 0;
			while (c != -1 && c != '\n') {
				c = get_char ();
				if (c == '"') {
					c = get_char ();
					break;
				}

				string_builder.Append ((char) c);
			}

			if (string_builder.Length == 0) {
				Report.Warning (1709, 1, Location, "Filename specified for preprocessor directive is empty");
			}

			// TODO: Any white-spaces count
			if (c != ' ')
				return false;

			SourceFile file = context.LookupFile (file_name, string_builder.ToString ());

			if (get_char () != '"' || get_char () != '{')
				return false;

			bool error;
			byte[] guid_bytes = new byte [16];
			int i = 0;

			for (; i < 4; i++) {
				guid_bytes [i] = read_hex (out error);
				if (error)
					return false;
			}

			if (get_char () != '-')
				return false;

			for (; i < 10; i++) {
				guid_bytes [i] = read_hex (out error);
				if (error)
					return false;

				guid_bytes [i++] = read_hex (out error);
				if (error)
					return false;

				if (get_char () != '-')
					return false;
			}

			for (; i < 16; i++) {
				guid_bytes [i] = read_hex (out error);
				if (error)
					return false;
			}

			if (get_char () != '}' || get_char () != '"')
				return false;

			// TODO: Any white-spaces count
			c = get_char ();
			if (c != ' ')
				return false;

			if (get_char () != '"')
				return false;

			// Any length of checksum
			List<byte> checksum_bytes = new List<byte> (16);

			c = peek_char ();
			while (c != '"' && c != -1) {
				checksum_bytes.Add (read_hex (out error));
				if (error)
					return false;

				c = peek_char ();
			}

			if (c == '/') {
				ReadSingleLineComment ();
			} else if (get_char () != '"') {
				return false;
			}

			file.SetChecksum (guid_bytes, checksum_bytes.ToArray ());
			ref_name.AutoGenerated = true;
			return true;
		}

		bool IsTokenIdentifierEqual (char[] identifier)
		{
			for (int i = 0; i < identifier.Length; ++i) {
				if (identifier[i] != id_builder[i])
					return false;
			}

			return true;
		}

		int TokenizePragmaNumber (ref int c)
		{
			number_pos = 0;

			int number;

			if (c >= '0' && c <= '9') {
				decimal_digits (c);
				uint ui = (uint) (number_builder[0] - '0');

				try {
					for (int i = 1; i < number_pos; i++) {
						ui = checked ((ui * 10) + ((uint) (number_builder[i] - '0')));
					}

					number = (int) ui;
				} catch (OverflowException) {
					Error_NumericConstantTooLong ();
					number = -1;
				}


				c = get_char ();

				// skip over white space
				while (c == ' ' || c == '\t')
					c = get_char ();

				if (c == ',') {
					c = get_char ();
				}

				// skip over white space
				while (c == ' ' || c == '\t')
					c = get_char ();
			} else {
				number = -1;
				if (c == '/') {
					ReadSingleLineComment ();
				} else {
					Report.Warning (1692, 1, Location, "Invalid number");

					// Read everything till the end of the line or file
					do {
						c = get_char ();
					} while (c != -1 && c != '\n');
				}
			}

			return number;
		}
		
		
		void ReadSingleLineComment ()
		{
			if (peek_char () != '/')
				Report.Warning (1696, 1, Location, "Single-line comment or end-of-line expected");
			sbag.StartComment (SpecialsBag.CommentType.Single, startsLine, line, col - 1);
			// Read everything till the end of the line or file
			int c;
			do {
				c = get_char ();
				sbag.PushCommentChar (c);
				var pc = peek_char ();
				if (pc == '\n' || pc == -1) 
					sbag.EndComment (line, col + 1);
			} while (c != -1 && c != '\n');
		}

		/// <summary>
		/// Handles #pragma directive
		/// </summary>
		void ParsePragmaDirective (string arg)
		{
			int c;
			int length = TokenizePreprocessorIdentifier (out c);
			if (length == pragma_warning.Length && IsTokenIdentifierEqual (pragma_warning)) {
				length = TokenizePreprocessorIdentifier (out c);

				//
				// #pragma warning disable
				// #pragma warning restore
				//
				if (length == pragma_warning_disable.Length) {
					bool disable = IsTokenIdentifierEqual (pragma_warning_disable);
					if (disable || IsTokenIdentifierEqual (pragma_warning_restore)) {
						// skip over white space
						while (c == ' ' || c == '\t')
							c = get_char ();

						var loc = Location;

						if (c == '\n' || c == '/') {
							if (c == '/')
								ReadSingleLineComment ();

							//
							// Disable/Restore all warnings
							//
							if (disable) {
								Report.RegisterWarningRegion (loc).WarningDisable (loc.Row);
							} else {
								Report.RegisterWarningRegion (loc).WarningEnable (loc.Row);
							}
						} else {
							//
							// Disable/Restore a warning or group of warnings
							//
							int code;
							do {
								code = TokenizePragmaNumber (ref c);
								if (code > 0) {
									if (disable) {
										Report.RegisterWarningRegion (loc).WarningDisable (loc, code, Report);
									} else {
										Report.RegisterWarningRegion (loc).WarningEnable (loc, code, Report);
									}
								}
							} while (code >= 0 && c != '\n' && c != -1);
						}

						return;
					}
				}

				Report.Warning (1634, 1, Location, "Expected disable or restore");
				return;
			}

			//
			// #pragma checksum
			//
			if (length == pragma_checksum.Length && IsTokenIdentifierEqual (pragma_checksum)) {
				if (c != ' ' || !ParsePragmaChecksum ()) {
					Report.Warning (1695, 1, Location,
						"Invalid #pragma checksum syntax. Expected \"filename\" \"{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}\" \"XXXX...\"");
				}

				return;
			}

			Report.Warning (1633, 1, Location, "Unrecognized #pragma directive");
		}

		bool eval_val (string s)
		{
			if (s == "true")
				return true;
			if (s == "false")
				return false;

			return file_name.IsConditionalDefined (context, s);
		}

		bool pp_primary (ref string s)
		{
			s = s.Trim ();
			int len = s.Length;

			if (len > 0){
				char c = s [0];
				
				if (c == '('){
					s = s.Substring (1);
					bool val = pp_expr (ref s, false);
					if (s.Length > 0 && s [0] == ')'){
						s = s.Substring (1);
						return val;
					}
					Error_InvalidDirective ();
					return false;
				}
				
				if (is_identifier_start_character (c)){
					int j = 1;

					while (j < len){
						c = s [j];
						
						if (is_identifier_part_character (c)){
							j++;
							continue;
						}
						bool v = eval_val (s.Substring (0, j));
						s = s.Substring (j);
						return v;
					}
					bool vv = eval_val (s);
					s = "";
					return vv;
				}
			}
			Error_InvalidDirective ();
			return false;
		}
		
		bool pp_unary (ref string s)
		{
			s = s.Trim ();
			int len = s.Length;

			if (len > 0){
				if (s [0] == '!'){
					if (len > 1 && s [1] == '='){
						Error_InvalidDirective ();
						return false;
					}
					s = s.Substring (1);
					return ! pp_primary (ref s);
				} else
					return pp_primary (ref s);
			} else {
				Error_InvalidDirective ();
				return false;
			}
		}
		
		bool pp_eq (ref string s)
		{
			bool va = pp_unary (ref s);

			s = s.Trim ();
			int len = s.Length;
			if (len > 0){
				if (s [0] == '='){
					if (len > 2 && s [1] == '='){
						s = s.Substring (2);
						return va == pp_unary (ref s);
					} else {
						Error_InvalidDirective ();
						return false;
					}
				} else if (s [0] == '!' && len > 1 && s [1] == '='){
					s = s.Substring (2);

					return va != pp_unary (ref s);

				} 
			}

			return va;
				
		}
		
		bool pp_and (ref string s)
		{
			bool va = pp_eq (ref s);

			s = s.Trim ();
			int len = s.Length;
			if (len > 0){
				if (s [0] == '&'){
					if (len > 2 && s [1] == '&'){
						s = s.Substring (2);
						return (va & pp_and (ref s));
					} else {
						Error_InvalidDirective ();
						return false;
					}
				} 
			}
			return va;
		}
		
		//
		// Evaluates an expression for `#if' or `#elif'
		//
		bool pp_expr (ref string s, bool isTerm)
		{
			bool va = pp_and (ref s);
			s = s.Trim ();
			int len = s.Length;
			if (len > 0){
				char c = s [0];
				
				if (c == '|'){
					if (len > 2 && s [1] == '|'){
						s = s.Substring (2);
						return va | pp_expr (ref s, isTerm);
					} else {
						Error_InvalidDirective ();
						return false;
					}
				}
				if (isTerm) {
					Error_EndLineExpected ();
					return false;
				}
			}
			
			return va;
		}

		bool eval (string s)
		{
			bool v = pp_expr (ref s, true);
			s = s.Trim ();
			if (s.Length != 0){
				return false;
			}

			return v;
		}

		void Error_NumericConstantTooLong ()
		{
			Report.Error (1021, Location, "Integral constant is too large");			
		}
		
		void Error_InvalidDirective ()
		{
			Report.Error (1517, Location, "Invalid preprocessor directive");
		}

		void Error_UnexpectedDirective (string extra)
		{
			Report.Error (
				1028, Location,
				"Unexpected processor directive ({0})", extra);
		}

		void Error_TokensSeen ()
		{
			Report.Error (1032, Location,
				"Cannot define or undefine preprocessor symbols after first token in file");
		}

		void Eror_WrongPreprocessorLocation ()
		{
			Report.Error (1040, Location,
				"Preprocessor directives must appear as the first non-whitespace character on a line");
		}

		void Error_EndLineExpected ()
		{
			Report.Error (1025, Location, "Single-line comment or end-of-line expected");
		}

		//
		// Raises a warning when tokenizer found documentation comment
		// on unexpected place
		//
		void WarningMisplacedComment (Location loc)
		{
			if (doc_state != XmlCommentState.Error) {
				doc_state = XmlCommentState.Error;
				Report.Warning (1587, 2, loc, "XML comment is not placed on a valid language element");
			}
		}
		
		//
		// if true, then the code continues processing the code
		// if false, the code stays in a loop until another directive is
		// reached.
		// When caller_is_taking is false we ignore all directives except the ones
		// which can help us to identify where the #if block ends
		bool ParsePreprocessingDirective (bool caller_is_taking)
		{
			string arg;
			bool region_directive = false;

			var directive = get_cmd_arg (out arg);

			//
			// The first group of pre-processing instructions is always processed
			//
			switch (directive) {
			case PreprocessorDirective.Region:
				region_directive = true;
				arg = "true";
				goto case PreprocessorDirective.If;

			case PreprocessorDirective.Endregion:
				if (ifstack == null || ifstack.Count == 0){
					Error_UnexpectedDirective ("no #region for this #endregion");
					return true;
				}
				int pop = ifstack.Pop ();
					
				if ((pop & REGION) == 0)
					Report.Error (1027, Location, "Expected `#endif' directive");
					
				return caller_is_taking;
				
			case PreprocessorDirective.If:
				if (ifstack == null)
					ifstack = new Stack<int> (2);

				int flags = region_directive ? REGION : 0;
				if (ifstack.Count == 0){
					flags |= PARENT_TAKING;
				} else {
					int state = ifstack.Peek ();
					if ((state & TAKING) != 0) {
						flags |= PARENT_TAKING;
					}
				}

				if (eval (arg) && caller_is_taking) {
					ifstack.Push (flags | TAKING);
					return true;
				}
				ifstack.Push (flags);
				return false;

			case PreprocessorDirective.Endif:
				if (ifstack == null || ifstack.Count == 0){
					Error_UnexpectedDirective ("no #if for this #endif");
					return true;
				} else {
					pop = ifstack.Pop ();
					
					if ((pop & REGION) != 0)
						Report.Error (1038, Location, "#endregion directive expected");
					
					if (arg.Length != 0) {
						Error_EndLineExpected ();
					}
					
					if (ifstack.Count == 0)
						return true;

					int state = ifstack.Peek ();
					return (state & TAKING) != 0;
				}

			case PreprocessorDirective.Elif:
				if (ifstack == null || ifstack.Count == 0){
					Error_UnexpectedDirective ("no #if for this #elif");
					return true;
				} else {
					int state = ifstack.Pop ();

					if ((state & REGION) != 0) {
						Report.Error (1038, Location, "#endregion directive expected");
						return true;
					}

					if ((state & ELSE_SEEN) != 0){
						Error_UnexpectedDirective ("#elif not valid after #else");
						return true;
					}

					if ((state & TAKING) != 0) {
						ifstack.Push (0);
						return false;
					}

					if (eval (arg) && ((state & PARENT_TAKING) != 0)){
						ifstack.Push (state | TAKING);
						return true;
					}

					ifstack.Push (state);
					return false;
				}

			case PreprocessorDirective.Else:
				if (ifstack == null || ifstack.Count == 0){
					Error_UnexpectedDirective ("no #if for this #else");
					return true;
				} else {
					int state = ifstack.Peek ();

					if ((state & REGION) != 0) {
						Report.Error (1038, Location, "#endregion directive expected");
						return true;
					}

					if ((state & ELSE_SEEN) != 0){
						Error_UnexpectedDirective ("#else within #else");
						return true;
					}

					ifstack.Pop ();

					if (arg.Length != 0) {
						Error_EndLineExpected ();
						return true;
					}

					bool ret = false;
					if ((state & PARENT_TAKING) != 0) {
						ret = (state & TAKING) == 0;
					
						if (ret)
							state |= TAKING;
						else
							state &= ~TAKING;
					}
	
					ifstack.Push (state | ELSE_SEEN);
					
					return ret;
				}
			case PreprocessorDirective.Define:
				if (any_token_seen){
					Error_TokensSeen ();
					return caller_is_taking;
				}
				PreProcessDefinition (true, arg, caller_is_taking);
				return caller_is_taking;

			case PreprocessorDirective.Undef:
				if (any_token_seen){
					Error_TokensSeen ();
					return caller_is_taking;
				}
				PreProcessDefinition (false, arg, caller_is_taking);
				return caller_is_taking;

			case PreprocessorDirective.Invalid:
				Report.Error (1024, Location, "Wrong preprocessor directive");
				return true;
			}

			//
			// These are only processed if we are in a `taking' block
			//
			if (!caller_is_taking)
				return false;
					
			switch (directive){
			case PreprocessorDirective.Error:
				Report.Error (1029, Location, "#error: '{0}'", arg);
				return true;

			case PreprocessorDirective.Warning:
				Report.Warning (1030, 1, Location, "#warning: `{0}'", arg);
				return true;

			case PreprocessorDirective.Pragma:
				if (context.Settings.Version == LanguageVersion.ISO_1) {
					Report.FeatureIsNotAvailable (context, Location, "#pragma");
				}

				ParsePragmaDirective (arg);
				return true;

			case PreprocessorDirective.Line:
				if (!PreProcessLine (arg))
					Report.Error (
						1576, Location,
						"The line number specified for #line directive is missing or invalid");
				return caller_is_taking;
			}

			throw new NotImplementedException (directive.ToString ());
		}

		private int consume_string (bool quoted)
		{
			int c;
			int pos = 0;
			Location start_location = Location;
			if (quoted)
				start_location = start_location - 1;

			while (true){
				c = get_char ();
				if (c == '"') {
					if (quoted && peek_char () == '"') {
						if (pos == value_builder.Length)
							Array.Resize (ref value_builder, pos * 2);

						value_builder[pos++] = (char) c;
						get_char ();
						continue;
					}

					string s;
					if (pos == 0)
						s = string.Empty;
					else if (pos <= 4)
						s = InternIdentifier (value_builder, pos);
					else
						s = new string (value_builder, 0, pos);

					val = new StringLiteral (context.BuiltinTypes, s, start_location);
					return Token.LITERAL;
				}

				if (c == '\n') {
					if (!quoted)
						Report.Error (1010, Location, "Newline in constant");
				} else if (c == '\\' && !quoted) {
					int surrogate;
					c = escape (c, out surrogate);
					if (c == -1)
						return Token.ERROR;
					if (surrogate != 0) {
						if (pos == value_builder.Length)
							Array.Resize (ref value_builder, pos * 2);

						value_builder[pos++] = (char) c;
						c = surrogate;
					}
				} else if (c == -1) {
					Report.Error (1039, Location, "Unterminated string literal");
					return Token.EOF;
				}

				if (pos == value_builder.Length)
					Array.Resize (ref value_builder, pos * 2);

				value_builder[pos++] = (char) c;
			}
		}

		private int consume_identifier (int s)
		{
			int res = consume_identifier (s, false);

			if (doc_state == XmlCommentState.Allowed)
				doc_state = XmlCommentState.NotAllowed;
			startsLine = false;
			return res;
		}

		int consume_identifier (int c, bool quoted) 
		{
			//
			// This method is very performance sensitive. It accounts
			// for approximately 25% of all parser time
			//

			int pos = 0;
			int column = col;
			if (quoted)
				--column;

			if (c == '\\') {
				int surrogate;
				c = escape (c, out surrogate);
				if (surrogate != 0) {
					id_builder [pos++] = (char) c;
					c = surrogate;
				}
			}

			id_builder [pos++] = (char) c;

			try {
				while (true) {
					c = reader.Read ();

					if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || (c >= '0' && c <= '9')) {
						id_builder [pos++] = (char) c;
						continue;
					}

					if (c < 0x80) {
						if (c == '\\') {
							int surrogate;
							c = escape (c, out surrogate);
							if (is_identifier_part_character ((char) c))
								id_builder[pos++] = (char) c;

							if (surrogate != 0) {
								c = surrogate;
							}

							continue;
						}
					} else if (Char.IsLetter ((char) c) || Char.GetUnicodeCategory ((char) c) == UnicodeCategory.ConnectorPunctuation) {
						id_builder [pos++] = (char) c;
						continue;
					}

					putback_char = c;
					break;
				}
			} catch (IndexOutOfRangeException) {
				Report.Error (645, Location, "Identifier too long (limit is 512 chars)");
				--pos;
				col += pos;
			}

			col += pos - 1;

			//
			// Optimization: avoids doing the keyword lookup
			// on uppercase letters
			//
			if (id_builder [0] >= '_' && !quoted) {
				int keyword = GetKeyword (id_builder, pos);
				if (keyword != -1) {
					val = LocatedToken.Create (null, ref_line, column);
					return keyword;
				}
			}

			string s = InternIdentifier (id_builder, pos);
#if FULL_AST
			if (quoted) {
				val = LocatedToken.Create ("@" + s, ref_line, column - 1);
			} else {
				val = LocatedToken.Create (s, ref_line, column);
			}
#else
			val = LocatedToken.Create (s, ref_line, column);
#endif
			if (quoted && parsing_attribute_section)
				AddEscapedIdentifier (((LocatedToken) val).Location);

			return Token.IDENTIFIER;
		}

		static string InternIdentifier (char[] charBuffer, int length)
		{
			//
			// Keep identifiers in an array of hashtables to avoid needless
			// allocations
			//
			var identifiers_group = identifiers [length];
			string s;
			if (identifiers_group != null) {
				if (identifiers_group.TryGetValue (charBuffer, out s)) {
					return s;
				}
			} else {
				// TODO: this should be number of files dependant
				// corlib compilation peaks at 1000 and System.Core at 150
				int capacity = length > 20 ? 10 : 100;
				identifiers_group = new Dictionary<char[], string> (capacity, new IdentifiersComparer (length));
				identifiers [length] = identifiers_group;
			}

			char[] chars = new char[length];
			Array.Copy (charBuffer, chars, length);

			s = new string (charBuffer, 0, length);
			identifiers_group.Add (chars, s);

			return s;
		}
		
		public int xtoken ()
		{
			int d, c;

			// Whether we have seen comments on the current line
			bool comments_seen = false;
			while ((c = get_char ()) != -1) {
				switch (c) {
				case '\t':
					col = ((col - 1 + tab_size) / tab_size) * tab_size;
					continue;

				case ' ':
				case '\f':
				case '\v':
				case 0xa0:
				case 0:
				case 0xFEFF:	// Ignore BOM anywhere in the file
					continue;

/*				This is required for compatibility with .NET
				case 0xEF:
					if (peek_char () == 0xBB) {
						PushPosition ();
						get_char ();
						if (get_char () == 0xBF)
							continue;
						PopPosition ();
					}
					break;
*/
				case '\\':
					tokens_seen = true;
					return consume_identifier (c);

				case '{':
					val = LocatedToken.Create (ref_line, col);
					return Token.OPEN_BRACE;
				case '}':
					val = LocatedToken.Create (ref_line, col);
					return Token.CLOSE_BRACE;
				case '[':
					// To block doccomment inside attribute declaration.
					if (doc_state == XmlCommentState.Allowed)
						doc_state = XmlCommentState.NotAllowed;

					val = LocatedToken.Create (ref_line, col);

					if (parsing_block == 0 || lambda_arguments_parsing)
						return Token.OPEN_BRACKET;

					int next = peek_char ();
					switch (next) {
					case ']':
					case ',':
						return Token.OPEN_BRACKET;

					case ' ':
					case '\f':
					case '\v':
					case '\r':
					case '\n':
					case '/':
						next = peek_token ();
						if (next == Token.COMMA || next == Token.CLOSE_BRACKET)
							return Token.OPEN_BRACKET;

						return Token.OPEN_BRACKET_EXPR;
					default:
						return Token.OPEN_BRACKET_EXPR;
					}
				case ']':
					LocatedToken.CreateOptional (ref_line, col, ref val);
					return Token.CLOSE_BRACKET;
				case '(':
					val = LocatedToken.Create (ref_line, col);
					//
					// An expression versions of parens can appear in block context only
					//
					if (parsing_block != 0 && !lambda_arguments_parsing) {
						
						//
						// Optmize most common case where we know that parens
						// is not special
						//
						switch (current_token) {
						case Token.IDENTIFIER:
						case Token.IF:
						case Token.FOR:
						case Token.FOREACH:
						case Token.TYPEOF:
						case Token.WHILE:
						case Token.USING:
						case Token.DEFAULT:
						case Token.DELEGATE:
						case Token.OP_GENERICS_GT:
							return Token.OPEN_PARENS;
						}

						// Optimize using peek
						int xx = peek_char ();
						switch (xx) {
						case '(':
						case '\'':
						case '"':
						case '0':
						case '1':
							return Token.OPEN_PARENS;
						}

						lambda_arguments_parsing = true;
						PushPosition ();
						d = TokenizeOpenParens ();
						PopPosition ();
						lambda_arguments_parsing = false;
						return d;
					}

					return Token.OPEN_PARENS;
				case ')':
					LocatedToken.CreateOptional (ref_line, col, ref val);
					return Token.CLOSE_PARENS;
				case ',':
					LocatedToken.CreateOptional (ref_line, col, ref val);
					return Token.COMMA;
				case ';':
					LocatedToken.CreateOptional (ref_line, col, ref val);
					return Token.SEMICOLON;
				case '~':
					val = LocatedToken.Create (ref_line, col);
					return Token.TILDE;
				case '?':
					val = LocatedToken.Create (ref_line, col);
					return TokenizePossibleNullableType ();
				case '<':
					val = LocatedToken.Create (ref_line, col);
					if (parsing_generic_less_than++ > 0)
						return Token.OP_GENERICS_LT;

					return TokenizeLessThan ();

				case '>':
					val = LocatedToken.Create (ref_line, col);
					d = peek_char ();

					if (d == '=') {
						get_char ();
						return Token.OP_GE;
					}

					if (parsing_generic_less_than > 1 || (parsing_generic_less_than == 1 && d != '>')) {
						parsing_generic_less_than--;
						return Token.OP_GENERICS_GT;
					}

					if (d == '>') {
						get_char ();
						d = peek_char ();

						if (d == '=') {
							get_char ();
							return Token.OP_SHIFT_RIGHT_ASSIGN;
						}
						return Token.OP_SHIFT_RIGHT;
					}

					return Token.OP_GT;

				case '+':
					val = LocatedToken.Create (ref_line, col);
					d = peek_char ();
					if (d == '+') {
						d = Token.OP_INC;
					} else if (d == '=') {
						d = Token.OP_ADD_ASSIGN;
					} else {
						return Token.PLUS;
					}
					get_char ();
					return d;

				case '-':
					val = LocatedToken.Create (ref_line, col);
					d = peek_char ();
					if (d == '-') {
						d = Token.OP_DEC;
					} else if (d == '=')
						d = Token.OP_SUB_ASSIGN;
					else if (d == '>')
						d = Token.OP_PTR;
					else {
						return Token.MINUS;
					}
					get_char ();
					return d;

				case '!':
					val = LocatedToken.Create (ref_line, col);
					if (peek_char () == '=') {
						get_char ();
						return Token.OP_NE;
					}
					return Token.BANG;

				case '=':
					val = LocatedToken.Create (ref_line, col);
					d = peek_char ();
					if (d == '=') {
						get_char ();
						return Token.OP_EQ;
					}
					if (d == '>') {
						get_char ();
						return Token.ARROW;
					}

					return Token.ASSIGN;

				case '&':
					val = LocatedToken.Create (ref_line, col);
					d = peek_char ();
					if (d == '&') {
						get_char ();
						return Token.OP_AND;
					}
					if (d == '=') {
						get_char ();
						return Token.OP_AND_ASSIGN;
					}
					return Token.BITWISE_AND;

				case '|':
					val = LocatedToken.Create (ref_line, col);
					d = peek_char ();
					if (d == '|') {
						get_char ();
						return Token.OP_OR;
					}
					if (d == '=') {
						get_char ();
						return Token.OP_OR_ASSIGN;
					}
					return Token.BITWISE_OR;

				case '*':
					val = LocatedToken.Create (ref_line, col);
					if (peek_char () == '=') {
						get_char ();
						return Token.OP_MULT_ASSIGN;
					}
					return Token.STAR;

				case '/':
					d = peek_char ();
					if (d == '=') {
						val = LocatedToken.Create (ref_line, col);
						get_char ();
						return Token.OP_DIV_ASSIGN;
					}

					// Handle double-slash comments.
					if (d == '/') {
						get_char ();
						if (doc_processing) {
							if (peek_char () == '/') {
								sbag.StartComment (SpecialsBag.CommentType.Documentation, startsLine, line, col - 1);
								get_char ();
								// Don't allow ////.
								if ((d = peek_char ()) != '/') {
									sbag.PushCommentChar (d);
									if (doc_state == XmlCommentState.Allowed)
										handle_one_line_xml_comment ();
									else if (doc_state == XmlCommentState.NotAllowed)
										WarningMisplacedComment (Location - 3);
								}
							} else {
								if (xml_comment_buffer.Length > 0)
									doc_state = XmlCommentState.NotAllowed;
							}
						} else {
							bool isDoc = peek_char () == '/';
							sbag.StartComment (isDoc ? SpecialsBag.CommentType.Documentation : SpecialsBag.CommentType.Single, startsLine, line, col - 1);
							if (isDoc)
								get_char ();
						}
						
						d = peek_char ();
						if (d == '\n' || d == '\r')
							sbag.EndComment (line, col + 1);
						
						while ((d = get_char ()) != -1 && (d != '\n') && d != '\r') {
							sbag.PushCommentChar (d);
							var pc = peek_char ();
							if (pc == -1 || pc == '\n' || pc == '\r') {
								sbag.EndComment (line, col + 1);
							}
						}
						
						any_token_seen |= tokens_seen;
						tokens_seen = false;
						comments_seen = false;
						continue;
					} else if (d == '*'){
						sbag.StartComment (SpecialsBag.CommentType.Multi, startsLine, line, col);
						get_char ();
						bool docAppend = false;
						if (doc_processing && peek_char () == '*') {
							int ch = get_char ();
							sbag.PushCommentChar (ch);
							// But when it is /**/, just do nothing.
							if (peek_char () == '/') {
								ch = get_char ();
								sbag.PushCommentChar (ch);
								sbag.EndComment (line, col + 1);
								continue;
							}
							if (doc_state == XmlCommentState.Allowed)
								docAppend = true;
							else if (doc_state == XmlCommentState.NotAllowed) {
								WarningMisplacedComment (Location - 2);
							}
						}

						int current_comment_start = 0;
						if (docAppend) {
							current_comment_start = xml_comment_buffer.Length;
							xml_comment_buffer.Append (Environment.NewLine);
						}

						while ((d = get_char ()) != -1){
							sbag.PushCommentChar (d);
							if (d == '*' && peek_char () == '/'){
								sbag.PushCommentChar ('/');
								get_char ();
								sbag.EndComment (line, col + 1);
								comments_seen = true;
								break;
							}
							if (docAppend)
								xml_comment_buffer.Append ((char) d);
							
							if (d == '\n'){
								any_token_seen |= tokens_seen;
								tokens_seen = false;
								// 
								// Reset 'comments_seen' just to be consistent.
								// It doesn't matter either way, here.
								//
								comments_seen = false;
							}
						}
						
						if (!comments_seen)
							Report.Error (1035, Location, "End-of-file found, '*/' expected");

						if (docAppend)
							update_formatted_doc_comment (current_comment_start);
						continue;
					}
					val = LocatedToken.Create (ref_line, col);
					return Token.DIV;

				case '%':
					val = LocatedToken.Create (ref_line, col);
					if (peek_char () == '='){
						get_char ();
						return Token.OP_MOD_ASSIGN;
					}
					return Token.PERCENT;

				case '^':
					val = LocatedToken.Create (ref_line, col);
					if (peek_char () == '='){
						get_char ();
						return Token.OP_XOR_ASSIGN;
					}
					return Token.CARRET;

				case ':':
					val = LocatedToken.Create (ref_line, col);
					if (peek_char () == ':') {
						get_char ();
						return Token.DOUBLE_COLON;
					}
					return Token.COLON;

				case '0': case '1': case '2': case '3': case '4':
				case '5': case '6': case '7': case '8': case '9':
					tokens_seen = true;
					return is_number (c);

				case '\n': // white space
					any_token_seen |= tokens_seen;
					tokens_seen = false;
					comments_seen = false;
					continue;

				case '.':
					tokens_seen = true;
					d = peek_char ();
					if (d >= '0' && d <= '9')
						return is_number (c);

					LocatedToken.CreateOptional (ref_line, col, ref val);
					return Token.DOT;
				
				case '#':
					if (tokens_seen || comments_seen) {
						Eror_WrongPreprocessorLocation ();
						return Token.ERROR;
					}
					
					if (ParsePreprocessingDirective (true))
						continue;

					bool directive_expected = false;
					while ((c = get_char ()) != -1) {
						if (col == 1) {
							directive_expected = true;
						} else if (!directive_expected) {
							// TODO: Implement comment support for disabled code and uncomment this code
//							if (c == '#') {
//								Eror_WrongPreprocessorLocation ();
//								return Token.ERROR;
//							}
							continue;
						}

						if (c == ' ' || c == '\t' || c == '\n' || c == '\f' || c == '\v' )
							continue;

						if (c == '#') {
							if (ParsePreprocessingDirective (false))
								break;
						}
						directive_expected = false;
					}

					if (c != -1) {
						tokens_seen = false;
						continue;
					}

					return Token.EOF;
				
				case '"':
					return consume_string (false);

				case '\'':
					return TokenizeBackslash ();
				
				case '@':
					c = get_char ();
					if (c == '"') {
						tokens_seen = true;
						return consume_string (true);
					}

					if (is_identifier_start_character (c)){
						return consume_identifier (c, true);
					}

					Report.Error (1646, Location, "Keyword, identifier, or string expected after verbatim specifier: @");
					return Token.ERROR;

				case EvalStatementParserCharacter:
					return Token.EVAL_STATEMENT_PARSER;
				case EvalCompilationUnitParserCharacter:
					return Token.EVAL_COMPILATION_UNIT_PARSER;
				case EvalUsingDeclarationsParserCharacter:
					return Token.EVAL_USING_DECLARATIONS_UNIT_PARSER;
				case DocumentationXref:
					return Token.DOC_SEE;
				}

				if (is_identifier_start_character (c)) {
					tokens_seen = true;
					return consume_identifier (c);
				}

				if (char.IsWhiteSpace ((char) c))
					continue;

				Report.Error (1056, Location, "Unexpected character `{0}'", ((char) c).ToString ());
			}

			if (CompleteOnEOF){
				if (generated)
					return Token.COMPLETE_COMPLETION;
				
				generated = true;
				return Token.GENERATE_COMPLETION;
			}
			

			return Token.EOF;
		}

		int TokenizeBackslash ()
		{
			int c = get_char ();
			tokens_seen = true;
			if (c == '\'') {
				val = new CharLiteral (context.BuiltinTypes, (char) c, Location);
				Report.Error (1011, Location, "Empty character literal");
				return Token.LITERAL;
			}

			if (c == '\n') {
				Report.Error (1010, Location, "Newline in constant");
				return Token.ERROR;
			}

			int d;
			c = escape (c, out d);
			if (c == -1)
				return Token.ERROR;
			if (d != 0)
				throw new NotImplementedException ();

			val = new CharLiteral (context.BuiltinTypes, (char) c, Location);
			c = get_char ();

			if (c != '\'') {
				Report.Error (1012, Location, "Too many characters in character literal");

				// Try to recover, read until newline or next "'"
				while ((c = get_char ()) != -1) {
					if (c == '\n' || c == '\'')
						break;
				}
			}

			return Token.LITERAL;
		}

		int TokenizeLessThan ()
		{
			int d;
			if (handle_typeof) {
				PushPosition ();
				if (parse_generic_dimension (out d)) {
					val = d;
					DiscardPosition ();
					return Token.GENERIC_DIMENSION;
				}
				PopPosition ();
			}

			// Save current position and parse next token.
			PushPosition ();
			if (parse_less_than ()) {
				if (parsing_generic_declaration && (parsing_generic_declaration_doc || token () != Token.DOT)) {
					d = Token.OP_GENERICS_LT_DECL;
				} else {
					d = Token.OP_GENERICS_LT;
				}
				PopPosition ();
				return d;
			}

			PopPosition ();
			parsing_generic_less_than = 0;

			d = peek_char ();
			if (d == '<') {
				get_char ();
				d = peek_char ();

				if (d == '=') {
					get_char ();
					return Token.OP_SHIFT_LEFT_ASSIGN;
				}
				return Token.OP_SHIFT_LEFT;
			}

			if (d == '=') {
				get_char ();
				return Token.OP_LE;
			}
			return Token.OP_LT;
		}

		//
		// Handles one line xml comment
		//
		private void handle_one_line_xml_comment ()
		{
			int c;
			while ((c = peek_char ()) == ' ') {
				sbag.PushCommentChar (c);
				get_char (); // skip heading whitespaces.
			}
			while ((c = peek_char ()) != -1 && c != '\n' && c != '\r') {
				sbag.PushCommentChar (c);
				xml_comment_buffer.Append ((char) get_char ());
			}
			if (c == '\r' || c == '\n')
				xml_comment_buffer.Append (Environment.NewLine);
		}

		//
		// Remove heading "*" in Javadoc-like xml documentation.
		//
		private void update_formatted_doc_comment (int current_comment_start)
		{
			int length = xml_comment_buffer.Length - current_comment_start;
			string [] lines = xml_comment_buffer.ToString (
				current_comment_start,
				length).Replace ("\r", "").Split ('\n');
			
			// The first line starts with /**, thus it is not target
			// for the format check.
			for (int i = 1; i < lines.Length; i++) {
				string s = lines [i];
				int idx = s.IndexOf ('*');
				string head = null;
				if (idx < 0) {
					if (i < lines.Length - 1)
						return;
					head = s;
				} else
					head = s.Substring (0, idx);
				foreach (char c in head)
					if (c != ' ')
						return;
				lines [i] = s.Substring (idx + 1);
			}
			xml_comment_buffer.Remove (current_comment_start, length);
			xml_comment_buffer.Insert (current_comment_start, String.Join (Environment.NewLine, lines));
		}

		//
		// Checks if there was incorrect doc comments and raise
		// warnings.
		//
		public void check_incorrect_doc_comment ()
		{
			if (xml_comment_buffer.Length > 0)
				WarningMisplacedComment (Location);
		}

		//
		// Consumes the saved xml comment lines (if any)
		// as for current target member or type.
		//
		public string consume_doc_comment ()
		{
			if (xml_comment_buffer.Length > 0) {
				string ret = xml_comment_buffer.ToString ();
				reset_doc_comment ();
				return ret;
			}
			return null;
		}

		Report Report {
			get { return context.Report; }
		}

		void reset_doc_comment ()
		{
			xml_comment_buffer.Length = 0;
		}

		public void cleanup ()
		{
			if (ifstack != null && ifstack.Count >= 1) {
				int state = ifstack.Pop ();
				if ((state & REGION) != 0)
					Report.Error (1038, Location, "#endregion directive expected");
				else 
					Report.Error (1027, Location, "Expected `#endif' directive");
			}
		}
	}

	//
	// Indicates whether it accepts XML documentation or not.
	//
	public enum XmlCommentState {
		// comment is allowed in this state.
		Allowed,
		// comment is not allowed in this state.
		NotAllowed,
		// once comments appeared when it is NotAllowed, then the
		// state is changed to it, until the state is changed to
		// .Allowed.
		Error
	}
}

