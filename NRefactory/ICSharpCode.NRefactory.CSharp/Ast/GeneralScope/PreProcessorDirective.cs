// 
// PreProcessorDirective.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	public enum PreProcessorDirectiveType : byte
	{
		Invalid = 0,
		Region = 1,
		Endregion = 2,
		
		If = 3,
		Endif = 4,
		Elif = 5,
		Else = 6,
		
		Define = 7,
		Undef = 8,
		Error = 9,
		Warning = 10,
		Pragma = 11,
		Line = 12
	}

	public class LinePreprocessorDirective : PreProcessorDirective
	{
		public int LineNumber {
			get;
			set;
		}

		public string FileName {
			get;
			set;
		}

		public LinePreprocessorDirective(TextLocation startLocation, TextLocation endLocation) : base (PreProcessorDirectiveType.Line, startLocation, endLocation)
		{
		}

		public LinePreprocessorDirective(string argument = null) : base (PreProcessorDirectiveType.Line, argument)
		{
		}
	}

	public class PragmaWarningPreprocessorDirective : PreProcessorDirective
	{
		public static readonly Role<PrimitiveExpression>  WarningRole = new Role<PrimitiveExpression> ("Warning");

		public static readonly TokenRole PragmaKeywordRole = new TokenRole ("#pragma");
		public static readonly TokenRole WarningKeywordRole = new TokenRole ("warning");
		public static readonly TokenRole DisableKeywordRole = new TokenRole ("disable");
		public static readonly TokenRole RestoreKeywordRole = new TokenRole ("restore");

		public bool Disable {
			get {
				return !DisableToken.IsNull;
			}
		}

		public CSharpTokenNode PragmaToken {
			get { return GetChildByRole (PragmaKeywordRole); }
		}

		public CSharpTokenNode WarningToken {
			get { return GetChildByRole (WarningKeywordRole); }
		}

		public CSharpTokenNode DisableToken {
			get { return GetChildByRole (DisableKeywordRole); }
		}

		public CSharpTokenNode RestoreToken {
			get { return GetChildByRole (RestoreKeywordRole); }
		}

		public AstNodeCollection<PrimitiveExpression> Warnings {
			get { return GetChildrenByRole(WarningRole); }
		}

		public override TextLocation EndLocation {
			get {
				var child = LastChild;
				if (child == null)
					return base.EndLocation;
				return child.EndLocation;
			}
		}

		public PragmaWarningPreprocessorDirective(TextLocation startLocation, TextLocation endLocation) : base (PreProcessorDirectiveType.Pragma, startLocation, endLocation)
		{
		}

		public PragmaWarningPreprocessorDirective(string argument = null) : base (PreProcessorDirectiveType.Pragma, argument)
		{
		}

		public bool IsDefined(int pragmaWarning)
		{
			return Warnings.Select(w => (int)w.Value).Any(n => n == pragmaWarning);
		}
	}
	
	public class PreProcessorDirective : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Whitespace;
			}
		}
		
		public PreProcessorDirectiveType Type {
			get;
			set;
		}
		
		public string Argument {
			get;
			set;
		}
		
		/// <summary>
		/// For an '#if' directive, specifies whether the condition evaluated to true.
		/// </summary>
		public bool Take {
			get;
			set;
		}
		
		TextLocation startLocation;
		public override TextLocation StartLocation {
			get { 
				return startLocation;
			}
		}
		
		TextLocation endLocation;
		public override TextLocation EndLocation {
			get {
				return endLocation;
			}
		}
		
		public PreProcessorDirective(PreProcessorDirectiveType type, TextLocation startLocation, TextLocation endLocation)
		{
			this.Type = type;
			this.startLocation = startLocation;
			this.endLocation = endLocation;
		}

		public PreProcessorDirective(PreProcessorDirectiveType type, string argument = null)
		{
			this.Type = type;
			this.Argument = argument;
		}

		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitPreProcessorDirective (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitPreProcessorDirective (this);
		}

		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitPreProcessorDirective (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			PreProcessorDirective o = other as PreProcessorDirective;
			return o != null && Type == o.Type && MatchString(Argument, o.Argument);
		}
	}
}

