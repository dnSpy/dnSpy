// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

public class CallOverloadedMethod
{
	public void OverloadedMethod(object a)
	{
	}
	
	public void OverloadedMethod(int? a)
	{
	}
	
	public void OverloadedMethod(string a)
	{
	}
	
	public void Call()
	{
		this.OverloadedMethod("(string)");
		this.OverloadedMethod((object)"(object)");
		this.OverloadedMethod(5);
		this.OverloadedMethod((object)5);
		this.OverloadedMethod(5L);
		this.OverloadedMethod((object)null);
		this.OverloadedMethod((string)null);
		this.OverloadedMethod((int?)null);
	}
	
	public void CallMethodUsingInterface(List<int> list)
	{
		((ICollection<int>)list).Clear();
	}
}
