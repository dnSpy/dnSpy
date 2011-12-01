// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IParameter"/>.
	/// </summary>
	public sealed class DefaultParameter : IParameter
	{
		readonly IType type;
		readonly string name;
		readonly DomRegion region;
		readonly IList<IAttribute> attributes;
		readonly bool isRef, isOut, isParams, isOptional;
		readonly object defaultValue;
		
		public DefaultParameter(IType type, string name)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (name == null)
				throw new ArgumentNullException("name");
			this.type = type;
			this.name = name;
		}
		
		public DefaultParameter(IType type, string name, DomRegion region = default(DomRegion), IList<IAttribute> attributes = null,
		                        bool isRef = false, bool isOut = false, bool isParams = false, bool isOptional = false, object defaultValue = null)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (name == null)
				throw new ArgumentNullException("name");
			this.type = type;
			this.name = name;
			this.region = region;
			this.attributes = attributes;
			this.isRef = isRef;
			this.isOut = isOut;
			this.isParams = isParams;
			this.isOptional = isOptional;
			this.defaultValue = defaultValue;
		}
		
		public IList<IAttribute> Attributes {
			get { return attributes; }
		}
		
		public bool IsRef {
			get { return isRef; }
		}
		
		public bool IsOut {
			get { return isOut; }
		}
		
		public bool IsParams {
			get { return isParams; }
		}
		
		public bool IsOptional {
			get { return isOptional; }
		}
		
		public string Name {
			get { return name; }
		}
		
		public DomRegion Region {
			get { return region; }
		}
		
		public IType Type {
			get { return type; }
		}
		
		bool IVariable.IsConst {
			get { return false; }
		}
		
		public object ConstantValue {
			get { return defaultValue; }
		}
		
		public override string ToString()
		{
			return ToString(this);
		}
		
		public static string ToString(IParameter p)
		{
			StringBuilder b = new StringBuilder();
			if (p.IsRef)
				b.Append("ref ");
			if (p.IsOut)
				b.Append("out ");
			if (p.IsParams)
				b.Append("params ");
			b.Append(p.Name);
			b.Append(':');
			b.Append(p.Type.ToString());
			if (p.IsOptional) {
				b.Append(" = ");
				if (p.ConstantValue != null)
					b.Append(p.ConstantValue.ToString());
				else
					b.Append("null");
			}
			return b.ToString();
		}
	}
}
