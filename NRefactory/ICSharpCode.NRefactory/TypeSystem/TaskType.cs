// Copyright (c) 2010-2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Helper class for dealing with System.Threading.Tasks.Task.
	/// </summary>
	public static class TaskType
	{
		/// <summary>
		/// Gets the T in Task&lt;T&gt;.
		/// Returns void for non-generic Task.
		/// Any other type is returned unmodified.
		/// </summary>
		public static IType UnpackTask(ICompilation compilation, IType type)
		{
			if (!IsTask(type))
				return type;
			if (type.TypeParameterCount == 0)
				return compilation.FindType(KnownTypeCode.Void);
			else
				return type.TypeArguments[0];
		}
		
		/// <summary>
		/// Gets whether the specified type is Task or Task&lt;T&gt;.
		/// </summary>
		public static bool IsTask(IType type)
		{
			ITypeDefinition def = type.GetDefinition();
			if (def != null) {
				if (def.KnownTypeCode == KnownTypeCode.Task)
					return true;
				if (def.KnownTypeCode == KnownTypeCode.TaskOfT)
					return type is ParameterizedType;
			}
			return false;
		}
		
		/// <summary>
		/// Creates a task type.
		/// </summary>
		public static IType Create(ICompilation compilation, IType elementType)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (elementType == null)
				throw new ArgumentNullException("elementType");
			
			if (elementType.Kind == TypeKind.Void)
				return compilation.FindType(KnownTypeCode.Task);
			IType taskType = compilation.FindType(KnownTypeCode.TaskOfT);
			ITypeDefinition taskTypeDef = taskType.GetDefinition();
			if (taskTypeDef != null)
				return new ParameterizedType(taskTypeDef, new [] { elementType });
			else
				return taskType;
		}
	}
}
