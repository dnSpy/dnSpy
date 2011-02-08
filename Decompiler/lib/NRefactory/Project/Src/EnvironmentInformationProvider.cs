// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.NRefactory
{
	public interface IEnvironmentInformationProvider
	{
		bool HasField(string fullTypeName, int typeParameterCount, string fieldName);
	}
	
	sealed class DummyEnvironmentInformationProvider : IEnvironmentInformationProvider
	{
		internal static readonly IEnvironmentInformationProvider Instance = new DummyEnvironmentInformationProvider();
		
		public bool HasField(string fullTypeName, int typeParameterCount, string fieldName)
		{
			return false;
		}
	}
}
