// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

public static class Switch
{
	public static string ShortSwitchOverString(string text)
	{
		switch (text) {
			case "First case":
				return "Text";
			default:
				return "Default";
		}
	}
	
	public static string SwitchOverString1(string text)
	{
		switch (text)
		{
			case "First case":
				return "Text1";
			case "Second case":
			case "2nd case":
				return "Text2";
			case "Third case":
				return "Text3";
			case "Fourth case":
				return "Text4";
			case "Fifth case":
				return "Text5";
			case "Sixth case":
				return "Text6";
			case null:
				return null;
			default:
				return "Default";
		}
	}
	
	public static string SwitchOverString2()
	{
		switch (Environment.UserName)
		{
			case "First case":
				return "Text1";
			case "Second case":
				return "Text2";
			case "Third case":
				return "Text3";
			case "Fourth case":
				return "Text4";
			case "Fifth case":
				return "Text5";
			case "Sixth case":
				return "Text6";
			default:
				return "Default";
		}
	}
}
