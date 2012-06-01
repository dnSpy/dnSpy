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
using System.Text;

public class PropertiesAndEvents
{
	public event EventHandler AutomaticEvent;
	
	[field: NonSerialized]
	public event EventHandler AutomaticEventWithInitializer = delegate(object sender, EventArgs e)
	{
	};
	
	public event EventHandler CustomEvent
	{
		add
		{
			this.AutomaticEvent += value;
		}
		remove
		{
			this.AutomaticEvent -= value;
		}
	}
	
	public int AutomaticProperty
	{
		get;
		set;
	}
	
	public int CustomProperty
	{
		get
		{
			return this.AutomaticProperty;
		}
		set
		{
			this.AutomaticProperty = value;
		}
	}
	
	public int Getter(StringBuilder b)
	{
		return b.Length;
	}
	
	public void Setter(StringBuilder b)
	{
		b.Capacity = 100;
	}
	
	public char IndexerGetter(StringBuilder b)
	{
		return b[50];
	}
	
	public void IndexerSetter(StringBuilder b)
	{
		b[42] = 'b';
	}
}
