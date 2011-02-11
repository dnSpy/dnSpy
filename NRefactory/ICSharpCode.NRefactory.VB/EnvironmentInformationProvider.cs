// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB
{
	public interface IEnvironmentInformationProvider
	{
		bool HasField(string reflectionTypeName, int typeParameterCount, string fieldName);
	}
	
	sealed class DummyEnvironmentInformationProvider : IEnvironmentInformationProvider
	{
		internal static readonly IEnvironmentInformationProvider Instance = new DummyEnvironmentInformationProvider();
		
		public bool HasField(string reflectionTypeName, int typeParameterCount, string fieldName)
		{
			return false;
		}
	}
}
