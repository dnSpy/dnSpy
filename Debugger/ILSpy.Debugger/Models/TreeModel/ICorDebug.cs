// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System.Collections.Generic;
using Debugger.Interop.CorDebug;
using Debugger.MetaData;
using Debugger;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	internal class ICorDebug
	{
		public class InfoNode: TreeNode
		{
			List<TreeNode> children;
			
			public InfoNode(string name, string text): this(name, text, null)
			{
				
			}
			
			public InfoNode(string name, string text, List<TreeNode> children)
			{
				this.Name = name;
				this.Text = text;
				this.ChildNodes = children;
				this.children = children;
			}
			
			public void AddChild(string name, string text)
			{
				if (children == null) {
					children = new List<TreeNode>();
					this.ChildNodes = children;
				}
				children.Add(new InfoNode(name, text));
			}
			
			public void AddChild(string name, string text, List<TreeNode> subChildren)
			{
				if (children == null) {
					children = new List<TreeNode>();
					this.ChildNodes = children;
				}
				children.Add(new InfoNode(name, text, subChildren));
			}
		}
		
		public static InfoNode GetDebugInfoRoot(AppDomain appDomain, ICorDebugValue corValue)
		{
			return new InfoNode("ICorDebug", "", GetDebugInfo(appDomain, corValue));
		}
		
		public static List<TreeNode> GetDebugInfo(AppDomain appDomain, ICorDebugValue corValue)
		{
			List<TreeNode> items = new List<TreeNode>();
			
			if (corValue is ICorDebugValue) {
				InfoNode info = new InfoNode("ICorDebugValue", "");
				info.AddChild("Address", corValue.GetAddress().ToString("X8"));
				info.AddChild("Type", ((CorElementType)corValue.GetTheType()).ToString());
				info.AddChild("Size", corValue.GetSize().ToString());
				items.Add(info);
			}
			if (corValue is ICorDebugValue2) {
				InfoNode info = new InfoNode("ICorDebugValue2", "");
				ICorDebugValue2 corValue2 = (ICorDebugValue2)corValue;
				string fullname;
				try {
					fullname = DebugType.CreateFromCorType(appDomain, corValue2.GetExactType()).FullName;
				} catch (DebuggerException e) {
					fullname = e.Message;
				}
				info.AddChild("ExactType", fullname);
				items.Add(info);
			}
			if (corValue is ICorDebugGenericValue) {
				InfoNode info = new InfoNode("ICorDebugGenericValue", "");
				try {
					byte[] bytes = ((ICorDebugGenericValue)corValue).GetRawValue();
					for(int i = 0; i < bytes.Length; i += 8) {
						string val = "";
						for(int j = i; j < bytes.Length && j < i + 8; j++) {
							val += bytes[j].ToString("X2") + " ";
						}
						info.AddChild("Value" + i.ToString("X2"), val);
					}
				} catch (System.ArgumentException) {
					info.AddChild("Value", "N/A");
				}
				items.Add(info);
			}
			if (corValue is ICorDebugReferenceValue) {
				InfoNode info = new InfoNode("ICorDebugReferenceValue", "");
				ICorDebugReferenceValue refValue = (ICorDebugReferenceValue)corValue;
				info.AddChild("IsNull", (refValue.IsNull() != 0).ToString());
				if (refValue.IsNull() == 0) {
					info.AddChild("Value", refValue.GetValue().ToString("X8"));
					if (refValue.Dereference() != null) {
						info.AddChild("Dereference", "", GetDebugInfo(appDomain, refValue.Dereference()));
					} else {
						info.AddChild("Dereference", "N/A");
					}
				}
				items.Add(info);
			}
			if (corValue is ICorDebugHeapValue) {
				InfoNode info = new InfoNode("ICorDebugHeapValue", "");
				items.Add(info);
			}
			if (corValue is ICorDebugHeapValue2) {
				InfoNode info = new InfoNode("ICorDebugHeapValue2", "");
				items.Add(info);
			}
			if (corValue is ICorDebugObjectValue) {
				InfoNode info = new InfoNode("ICorDebugObjectValue", "");
				ICorDebugObjectValue objValue = (ICorDebugObjectValue)corValue;
				info.AddChild("Class", objValue.GetClass().GetToken().ToString("X8"));
				info.AddChild("IsValueClass", (objValue.IsValueClass() != 0).ToString());
				items.Add(info);
			}
			if (corValue is ICorDebugObjectValue2) {
				InfoNode info = new InfoNode("ICorDebugObjectValue2", "");
				items.Add(info);
			}
			if (corValue is ICorDebugBoxValue) {
				InfoNode info = new InfoNode("ICorDebugBoxValue", "");
				ICorDebugBoxValue boxValue = (ICorDebugBoxValue)corValue;
				info.AddChild("Object", "", GetDebugInfo(appDomain, boxValue.GetObject()));
				items.Add(info);
			}
			if (corValue is ICorDebugStringValue) {
				InfoNode info = new InfoNode("ICorDebugStringValue", "");
				ICorDebugStringValue stringValue = (ICorDebugStringValue)corValue;
				info.AddChild("Length", stringValue.GetLength().ToString());
				info.AddChild("String", stringValue.GetString());
				items.Add(info);
			}
			if (corValue is ICorDebugArrayValue) {
				InfoNode info = new InfoNode("ICorDebugArrayValue", "");
				info.AddChild("...", "...");
				items.Add(info);
			}
			if (corValue is ICorDebugHandleValue) {
				InfoNode info = new InfoNode("ICorDebugHandleValue", "");
				ICorDebugHandleValue handleValue = (ICorDebugHandleValue)corValue;
				info.AddChild("HandleType", handleValue.GetHandleType().ToString());
				items.Add(info);
			}
			
			return items;
		}
	}
}
