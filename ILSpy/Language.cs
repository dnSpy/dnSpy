// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Description of ILanguage.
	/// </summary>
	public abstract class Language
	{
		public static readonly Language Current = Languages.IL;
		
		public virtual string TypeToString(TypeReference t)
		{
			return t.Name;
		}
	}
	
	public static class Languages
	{
		public static readonly Language IL = new ILLanguage();
		
		class ILLanguage : Language
		{
		}
	}
}
