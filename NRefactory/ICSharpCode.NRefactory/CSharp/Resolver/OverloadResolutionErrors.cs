// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[Flags]
	public enum OverloadResolutionErrors
	{
		None = 0,
		/// <summary>
		/// Too many positional arguments (some could not be mapped to any parameter).
		/// </summary>
		TooManyPositionalArguments = 0x0001,
		/// <summary>
		/// A named argument could not be mapped to any parameter
		/// </summary>
		NoParameterFoundForNamedArgument = 0x0002,
		/// <summary>
		/// Type inference failed for a generic method.
		/// </summary>
		TypeInferenceFailed = 0x0004,
		/// <summary>
		/// Type arguments were explicitly specified, but did not match the number of type parameters.
		/// </summary>
		WrongNumberOfTypeArguments = 0x0008,
		/// <summary>
		/// After substituting type parameters with the inferred types; a constructed type within the formal parameters
		/// does not satisfy its constraint.
		/// </summary>
		ConstructedTypeDoesNotSatisfyConstraint = 0x0010,
		/// <summary>
		/// No argument was mapped to a non-optional parameter
		/// </summary>
		MissingArgumentForRequiredParameter = 0x0020,
		/// <summary>
		/// Several arguments were mapped to a single (non-params-array) parameter
		/// </summary>
		MultipleArgumentsForSingleParameter = 0x0040,
		/// <summary>
		/// 'ref'/'out' passing mode doesn't match for at least 1 parameter
		/// </summary>
		ParameterPassingModeMismatch = 0x0080,
		/// <summary>
		/// Argument type cannot be converted to parameter type
		/// </summary>
		ArgumentTypeMismatch = 0x0100,
		/// <summary>
		/// There is no unique best overload
		/// </summary>
		AmbiguousMatch = 0x0200
	}
}
