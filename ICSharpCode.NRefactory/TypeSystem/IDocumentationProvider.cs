// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Provides XML documentation for members.
	/// </summary>
	public interface IDocumentationProvider
	{
		/// <summary>
		/// Gets the XML documentation for the specified entity.
		/// </summary>
		string GetDocumentation(IEntity entity);
	}
}
