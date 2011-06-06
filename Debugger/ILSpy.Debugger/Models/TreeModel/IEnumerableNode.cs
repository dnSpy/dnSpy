// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using Debugger.MetaData;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.ILSpy.Debugger.Services.Debugger;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	/// <summary>
	/// IEnumerable node in the variable tree.
	/// </summary>
	internal class IEnumerableNode : TreeNode
	{
		Expression targetObject;
		Expression debugListExpression;
		
		public IEnumerableNode(Expression targetObject, DebugType itemType)
		{
			this.targetObject = targetObject;
			
			this.Name = "IEnumerable";
			this.Text = "Expanding will enumerate the IEnumerable";
			DebugType debugListType;
			this.debugListExpression = DebuggerHelpers.CreateDebugListExpression(targetObject, itemType, out debugListType);
			this.ChildNodes = Utils.LazyGetItemsOfIList(this.debugListExpression);
		}
	}
}
