// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// Interface for resolvers that can solve cross-definition references.
	/// </summary>
	public interface IHighlightingDefinitionReferenceResolver
	{
		/// <summary>
		/// Gets the highlighting definition by name, or null if it is not found.
		/// </summary>
		IHighlightingDefinition GetDefinition(string name);
	}
}
