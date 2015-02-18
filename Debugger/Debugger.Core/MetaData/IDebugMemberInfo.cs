// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace Debugger.MetaData
{
	public interface IDebugMemberInfo
	{
		Type DeclaringType { get; }
		Module DebugModule { get; }
		string Name { get; }
		int MetadataToken { get; }
		bool IsStatic { get; }
		bool IsPublic { get; }
		bool IsAssembly { get; }
		bool IsFamily { get; }
		bool IsPrivate { get; }
		DebugType MemberType { get; }
	}
}
