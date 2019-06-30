/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	// Make sure it's identical to Microsoft.CodeAnalysis.ExpressionEvaluator.ExpressionCompilerConstants
	static class ExpressionCompilerConstants {
		public const string IntrinsicAssemblyNamespace = "dnSpy.Roslyn.ExpressionEvaluator";
		public const string IntrinsicAssemblyTypeName = "IntrinsicMethods";
		public const string IntrinsicAssemblyTypeMetadataName = IntrinsicAssemblyNamespace + "." + IntrinsicAssemblyTypeName;
		public const string GetExceptionMethodName = "GetException";
		public const string GetStowedExceptionMethodName = "GetStowedException";
		public const string GetObjectAtAddressMethodName = "GetObjectAtAddress";
		public const string GetReturnValueMethodName = "GetReturnValue";
		public const string CreateVariableMethodName = "CreateVariable";
		public const string GetVariableValueMethodName = "GetObjectByAlias";
		public const string GetVariableAddressMethodName = "GetVariableAddress";
	}
}
