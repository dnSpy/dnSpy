// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Debugger;
using Debugger.MetaData;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.Debugger.Services.Debugger;
using Module = Debugger.Module;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	internal partial class Utils
	{
		public static IEnumerable<TreeNode> LazyGetChildNodesOfObject(Expression targetObject, DebugType shownType)
		{
			MemberInfo[] publicStatic      = shownType.GetFieldsAndNonIndexedProperties(BindingFlags.Public    | BindingFlags.Static   | BindingFlags.DeclaredOnly);
			MemberInfo[] publicInstance    = shownType.GetFieldsAndNonIndexedProperties(BindingFlags.Public    | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			MemberInfo[] nonPublicStatic   = shownType.GetFieldsAndNonIndexedProperties(BindingFlags.NonPublic | BindingFlags.Static   | BindingFlags.DeclaredOnly);
			MemberInfo[] nonPublicInstance = shownType.GetFieldsAndNonIndexedProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			
			DebugType baseType = (DebugType)shownType.BaseType;
			if (baseType != null) {
				yield return new TreeNode(
					ImageService.GetImage("Icons.16x16.Class"),
					"BaseClass",
					baseType.Name,
					baseType.FullName,
					baseType.FullName == "System.Object" ? null : Utils.LazyGetChildNodesOfObject(targetObject, baseType)
				);
			}
			
			if (nonPublicInstance.Length > 0) {
				yield return new TreeNode(
					null,
					"NonPublicMembers",
					string.Empty,
					string.Empty,
					Utils.LazyGetMembersOfObject(targetObject, nonPublicInstance)
				);
			}
			
			if (publicStatic.Length > 0 || nonPublicStatic.Length > 0) {
				IEnumerable<TreeNode> childs = Utils.LazyGetMembersOfObject(targetObject, publicStatic);
				if (nonPublicStatic.Length > 0) {
					TreeNode nonPublicStaticNode = new TreeNode(
						null,
						"NonPublicStaticMembers",
						string.Empty,
						string.Empty,
						Utils.LazyGetMembersOfObject(targetObject, nonPublicStatic)
					);
					childs = Utils.PrependNode(nonPublicStaticNode, childs);
				}
				yield return new TreeNode(
					null,
					"StaticMembers",
					string.Empty,
					string.Empty,
					childs
				);
			}
			
			DebugType iListType = (DebugType)shownType.GetInterface(typeof(IList).FullName);
			if (iListType != null) {
				yield return new IListNode(targetObject);
			} else {
				DebugType iEnumerableType, itemType;
				if (shownType.ResolveIEnumerableImplementation(out iEnumerableType, out itemType)) {
					yield return new IEnumerableNode(targetObject, itemType);
				}
			}
			
			foreach(TreeNode node in LazyGetMembersOfObject(targetObject, publicInstance)) {
				yield return node;
			}
		}
		
		public static IEnumerable<TreeNode> LazyGetMembersOfObject(Expression expression, MemberInfo[] members)
		{
			List<TreeNode> nodes = new List<TreeNode>();
			foreach(MemberInfo memberInfo in members) {
				string imageName;
				var image = ExpressionNode.GetImageForMember((IDebugMemberInfo)memberInfo, out imageName);
				var exp = new ExpressionNode(image, memberInfo.Name, expression.AppendMemberReference((IDebugMemberInfo)memberInfo));
				exp.ImageName = imageName;
				nodes.Add(exp);
			}
			
			return nodes;
		}

		public static IEnumerable<TreeNode> LazyGetItemsOfIList(Expression targetObject)
		{
			// This is needed for expanding IEnumerable<T>
			
			var type = new SimpleType() { Identifier = typeof(IList).FullName };
			type.AddAnnotation(typeof(IList));
			
			targetObject = new CastExpression() { Expression = targetObject.Clone(), Type = type };

			int count = 0;
			GetValueException error = null;
			try {
				count = GetIListCount(targetObject);
			} catch (GetValueException e) {
				// Cannot yield a value in the body of a catch clause (CS1631)
				error = e;
			}
			if (error != null) {
				yield return new TreeNode(null, "(error)", error.Message, null, null);
			} else if (count == 0) {
				yield return new TreeNode(null, "(empty)", null, null, null);
			} else {
				for(int i = 0; i < count; i++) {
					string imageName;
					var image = ExpressionNode.GetImageForArrayIndexer(out imageName);
					var expression = new ExpressionNode(image, "[" + i + "]", targetObject.AppendIndexer(i));
					expression.ImageName = imageName;
					yield return expression;
				}
			}
		}
		
		/// <summary>
		/// Evaluates System.Collections.ICollection.Count property on given object.
		/// </summary>
		/// <exception cref="GetValueException">Evaluating System.Collections.ICollection.Count on targetObject failed.</exception>
		public static int GetIListCount(Expression targetObject)
		{
			Value list = targetObject.Evaluate(WindowsDebugger.CurrentProcess);
			var iCollectionInterface = list.Type.GetInterface(typeof(ICollection).FullName);
			if (iCollectionInterface == null)
				throw new GetValueException(targetObject, targetObject.PrettyPrint() + " does not implement System.Collections.ICollection");
			PropertyInfo countProperty = iCollectionInterface.GetProperty("Count");
			// Do not get string representation since it can be printed in hex
			return (int)list.GetPropertyValue(countProperty).PrimitiveValue;
		}
		
		public static IEnumerable<TreeNode> PrependNode(TreeNode node, IEnumerable<TreeNode> rest)
		{
			yield return node;
			if (rest != null) {
				foreach(TreeNode absNode in rest) {
					yield return absNode;
				}
			}
		}
	}
}
