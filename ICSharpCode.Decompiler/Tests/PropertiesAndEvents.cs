// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Text;

public class PropertiesAndEvents
{
	public event EventHandler AutomaticEvent;
	
	[field: NonSerialized]
	public event EventHandler AutomaticEventWithInitializer = delegate
	{
	}
	;
	
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
