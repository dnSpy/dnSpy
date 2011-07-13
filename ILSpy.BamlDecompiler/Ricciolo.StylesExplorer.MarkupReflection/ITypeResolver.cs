// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	public interface ITypeResolver
	{
		string RuntimeVersion { get; }
		bool IsLocalAssembly(string name);
		IType GetTypeByAssemblyQualifiedName(string name);
		IDependencyPropertyDescriptor GetDependencyPropertyDescriptor(string name, IType ownerType, IType targetType);
	}
}
