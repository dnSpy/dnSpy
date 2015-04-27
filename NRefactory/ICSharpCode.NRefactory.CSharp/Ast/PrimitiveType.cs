// 
// FullTypeName.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp
{
	public class PrimitiveType : AstType
	{
		TextLocation location;
		string keyword = string.Empty;
		
		public string Keyword {
			get { return keyword; }
			set { 
				if (value == null)
					throw new ArgumentNullException();
				ThrowIfFrozen();
				keyword = value; 
			}
		}
		
		public KnownTypeCode KnownTypeCode {
			get { return GetTypeCodeForPrimitiveType(this.Keyword); }
		}
		
		public PrimitiveType()
		{
		}
		
		public PrimitiveType(string keyword)
		{
			this.Keyword = keyword;
		}
		
		public PrimitiveType(string keyword, TextLocation location)
		{
			this.Keyword = keyword;
			this.location = location;
		}
		
		public override TextLocation StartLocation {
			get {
				return location;
			}
		}
		
		internal void SetStartLocation(TextLocation value)
		{
			ThrowIfFrozen();
			this.location = value;
		}
		
		public override TextLocation EndLocation {
			get {
				return new TextLocation (location.Line, location.Column + keyword.Length);
			}
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitPrimitiveType (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitPrimitiveType (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitPrimitiveType (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			PrimitiveType o = other as PrimitiveType;
			return o != null && MatchString(this.Keyword, o.Keyword);
		}

		public override string ToString(CSharpFormattingOptions formattingOptions)
		{
			return Keyword;
		}
		
		public override ITypeReference ToTypeReference(NameLookupMode lookupMode, InterningProvider interningProvider = null)
		{
			KnownTypeCode typeCode = GetTypeCodeForPrimitiveType(this.Keyword);
			if (typeCode == KnownTypeCode.None)
				return new UnknownType(null, this.Keyword);
			else
				return KnownTypeReference.Get(typeCode);
		}
		
		public static KnownTypeCode GetTypeCodeForPrimitiveType(string keyword)
		{
			switch (keyword) {
				case "string":
					return KnownTypeCode.String;
				case "int":
					return KnownTypeCode.Int32;
				case "uint":
					return KnownTypeCode.UInt32;
				case "object":
					return KnownTypeCode.Object;
				case "bool":
					return KnownTypeCode.Boolean;
				case "sbyte":
					return KnownTypeCode.SByte;
				case "byte":
					return KnownTypeCode.Byte;
				case "short":
					return KnownTypeCode.Int16;
				case "ushort":
					return KnownTypeCode.UInt16;
				case "long":
					return KnownTypeCode.Int64;
				case "ulong":
					return KnownTypeCode.UInt64;
				case "float":
					return KnownTypeCode.Single;
				case "double":
					return KnownTypeCode.Double;
				case "decimal":
					return KnownTypeCode.Decimal;
				case "char":
					return KnownTypeCode.Char;
				case "void":
					return KnownTypeCode.Void;
				default:
					return KnownTypeCode.None;
			}
		}
	}
}

