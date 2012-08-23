//
// CompletionExtensionMethods.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;

namespace ICSharpCode.NRefactory.Completion
{
	public static class CompletionExtensionMethods
	{
		/// <summary>
		/// Gets the EditorBrowsableState of an entity.
		/// </summary>
		/// <returns>
		/// The editor browsable state.
		/// </returns>
		/// <param name='entity'>
		/// Entity.
		/// </param>
		public static System.ComponentModel.EditorBrowsableState GetEditorBrowsableState(this IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException ("entity");

			var browsableState = entity.Attributes.FirstOrDefault(attr => attr.AttributeType.Name == "EditorBrowsableAttribute" && attr.AttributeType.Namespace == "System.ComponentModel");
			if (browsableState != null && browsableState.PositionalArguments.Count == 1) {
				try {
					return (System.ComponentModel.EditorBrowsableState)browsableState.PositionalArguments [0].ConstantValue;
				} catch (Exception) {}
			}
			return System.ComponentModel.EditorBrowsableState.Always;
		}
		
		/// <summary>
		/// Determines if an entity should be shown in the code completion window. This is the same as:
		/// <c>GetEditorBrowsableState (entity) != System.ComponentModel.EditorBrowsableState.Never</c>
		/// </summary>
		/// <returns>
		/// <c>true</c> if the entity should be shown; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='entity'>
		/// The entity.
		/// </param>
		public static bool IsBrowsable(this IEntity entity)
		{
			return GetEditorBrowsableState (entity) != System.ComponentModel.EditorBrowsableState.Never;
		}
	}
}

