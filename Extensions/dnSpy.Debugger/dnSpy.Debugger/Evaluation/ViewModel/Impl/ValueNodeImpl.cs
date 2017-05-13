/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.Text;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	[Flags]
	enum RefreshNodeOptions {
		RefreshName					= 0x00000001,
		RefreshNameControl			= 0x00000002,
		RefreshValue				= 0x00000004,
		RefreshValueControl			= 0x00000008,
		RefreshType					= 0x00000010,
		RefreshTypeControl			= 0x00000020,
	}

	sealed class ValueNodeImpl : ValueNode, INotifyPropertyChanged {
		// We have to use a small number here because the treeview derives from listview which uses a
		// ListCollectionView that only supports adding/removing one item at a time. It's incredibly slow.
		// TODO: use your own ICollectionView (collection impls ICollectionView or ICollectionViewFactory)
		const uint MAX_CHILDREN = 10000;

		public event PropertyChangedEventHandler PropertyChanged;
		public override ImageReference Icon => Context.ValueNodeImageReferenceService.GetImageReference(DebuggerValueNode.ImageName);

		public override FormatterObject<ValueNode> NameObject => new FormatterObject<ValueNode>(this, Context.NameColumnName);
		public override FormatterObject<ValueNode> ValueObject => new FormatterObject<ValueNode>(this, Context.ValueColumnName);
		public override FormatterObject<ValueNode> TypeObject => new FormatterObject<ValueNode>(this, Context.TypeColumnName);

		public override ClassifiedTextCollection CachedName {
			get {
				if (cachedName.IsDefault)
					InitializeCachedText();
				return cachedName;
			}
		}
		public override ClassifiedTextCollection CachedValue {
			get {
				if (cachedValue.IsDefault)
					InitializeCachedText();
				return cachedValue;
			}
		}
		public override ClassifiedTextCollection CachedExpectedType {
			get {
				if (cachedExpectedType.IsDefault)
					InitializeCachedText();
				return cachedExpectedType;
			}
		}
		/// <summary>
		/// It's default if it's identical to <see cref="CachedExpectedType"/>
		/// </summary>
		public override ClassifiedTextCollection CachedActualType_OrDefaultInstance {
			get {
				// Check cachedExpectedType and not cachedActualType since cachedActualType
				// is default if it equals cachedExpectedType
				if (cachedExpectedType.IsDefault)
					InitializeCachedText();
				return cachedActualType;
			}
		}
		public override ClassifiedTextCollection OldCachedValue => oldCachedValue;

		void InitializeCachedText() {
			var p = Context.ValueNodeFormatParameters;
			p.Initialize(cachedName.IsDefault, cachedValue.IsDefault, cachedExpectedType.IsDefault);
			DebuggerValueNode.Format(p);
			if (cachedName.IsDefault)
				cachedName = p.NameOutput.GetClassifiedText();
			if (cachedValue.IsDefault)
				cachedValue = p.ValueOutput.GetClassifiedText();
			if (cachedExpectedType.IsDefault) {
				cachedExpectedType = p.ExpectedTypeOutput.GetClassifiedText();
				if (p.ActualTypeOutput.Equals(cachedExpectedType)) {
					p.ActualTypeOutput.Clear();
					cachedActualType = default(ClassifiedTextCollection);
				}
				else
					cachedActualType = p.ActualTypeOutput.GetClassifiedText();
			}
		}
		ClassifiedTextCollection cachedName;
		ClassifiedTextCollection cachedValue;
		ClassifiedTextCollection cachedExpectedType;
		ClassifiedTextCollection cachedActualType;
		ClassifiedTextCollection oldCachedValue;

		public IValueNodesContext Context { get; }

		internal uint DbgValueNodeChildIndex { get; }
		internal ValueNodeImpl Parent { get; }
		internal string RootId { get; }
		bool HasDebuggerValueNode => __dbgValueNode_DONT_USE != null;
		internal DbgValueNode DebuggerValueNode {
			get {
				var dbgNode = __dbgValueNode_DONT_USE;
				if (dbgNode == null)
					__dbgValueNode_DONT_USE = dbgNode = Context.ValueNodeReader.GetDebuggerNode(this);
				return dbgNode;
			}
		}
		DbgValueNode __dbgValueNode_DONT_USE;

		public override IEditValueProvider ValueEditValueProvider => Context.ValueEditValueProvider;

		public override IEditableValue ValueEditableValue {
			get {
				if (valueEditableValue == null)
					valueEditableValue = new EditableValueImpl(() => GetEditableValue(), s => SaveEditableValue(s), () => !DebuggerValueNode.IsReadOnly);
				return valueEditableValue;
			}
		}
		IEditableValue valueEditableValue;

		public ValueNodeImpl(IValueNodesContext context, DbgValueNode dbgValueNode, string rootId) {
			Context = context ?? throw new ArgumentNullException(nameof(context));
			RootId = rootId;
			__dbgValueNode_DONT_USE = dbgValueNode ?? throw new ArgumentNullException(nameof(dbgValueNode));
		}

		// parent is needed since we need it before TreeNodeData.Parent is available
		public ValueNodeImpl(IValueNodesContext context, ValueNodeImpl parent, uint childIndex) {
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Parent = parent ?? throw new ArgumentNullException(nameof(parent));
			DbgValueNodeChildIndex = childIndex;
		}

		string GetEditableValue() {
			var output = new StringBuilderTextColorOutput();
			var options = Context.ValueNodeFormatParameters.ValueFormatterOptions & ~DbgValueFormatterOptions.Display;
			DebuggerValueNode.FormatValue(output, options);
			return output.ToString();
		}

		void SaveEditableValue(string expression) {
			if (DebuggerValueNode.IsReadOnly)
				throw new InvalidOperationException();
			var evalOptions = DbgEvaluationOptions.Expression;
			var res = DebuggerValueNode.Assign(expression, evalOptions);
			if (res.Error == null)
				oldCachedValue = cachedValue;
			ResetForReuse();
			if (res.Error != null)
				Context.ShowMessageBox(res.Error, ShowMessageBoxButtons.OK);
		}

		public override bool Activate() => valueEditableValue?.IsEditingValue == true;
		public override void Initialize() => TreeNode.LazyLoading = DebuggerValueNode.HasChildren != false;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			if (DebuggerValueNode.HasChildren == false)
				yield break;

			ulong childCount = DebuggerValueNode.ChildCount;
			if (childCount > MAX_CHILDREN) {
				bool open = Context.ShowMessageBox(string.Format(dnSpy_Debugger_Resources.Locals_Ask_TooManyItems, MAX_CHILDREN), ShowMessageBoxButtons.YesNo);
				if (!open) {
					TreeNode.LazyLoading = DebuggerValueNode.HasChildren != false;
					yield break;
				}
				childCount = MAX_CHILDREN;
			}
			uint count = (uint)childCount;
			for (uint i = 0; i < count; i++)
				yield return new ValueNodeImpl(Context, this, i);
		}

		void RefreshControls(RefreshNodeOptions options) {
			if ((options & RefreshNodeOptions.RefreshName) != 0)
				cachedName = default(ClassifiedTextCollection);
			if ((options & RefreshNodeOptions.RefreshValue) != 0)
				cachedValue = default(ClassifiedTextCollection);
			if ((options & RefreshNodeOptions.RefreshType) != 0) {
				cachedExpectedType = default(ClassifiedTextCollection);
				cachedActualType = default(ClassifiedTextCollection);
			}
			if ((options & RefreshNodeOptions.RefreshNameControl) != 0)
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameObject)));
			if ((options & RefreshNodeOptions.RefreshValueControl) != 0)
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueObject)));
			if ((options & RefreshNodeOptions.RefreshTypeControl) != 0)
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeObject)));
		}

		public override void OnRefreshUI() => RefreshControls(Context.RefreshNodeOptions);

		// If it's a new value, then reset everything, else try to re-use it so eg. expanded arrays
		// or classes don't get collapsed again.
		internal void SetDebuggerValueNodeForRoot(DbgValueNode newNode) {
			if (Parent != null)
				throw new InvalidOperationException();
			SetDebuggerValueNode(newNode, recursionCounter: 0);
		}

		bool SetDebuggerValueNode(DbgValueNode newNode, int recursionCounter) {
			const int MAX_RECURSION = 30;
			Debug.Assert(newNode != null);
			var oldNode = __dbgValueNode_DONT_USE;
			__dbgValueNode_DONT_USE = newNode;
			oldCachedValue = cachedValue;

			if (recursionCounter >= MAX_RECURSION || !IsSame(oldNode, newNode)) {
				ResetForReuse();
				return false;
			}

			if (TreeNode.IsExpanded) {
				var children = TreeNode.Children;
				// This was checked in IsSame
				Debug.Assert(oldNode.ChildCount == newNode.ChildCount);
				Debug.Assert((uint)children.Count <= newNode.ChildCount);
				int count = children.Count;
				for (int i = 0; i < count; i++) {
					var node = (ValueNodeImpl)children[i].Data;
					if (!node.HasDebuggerValueNode)
						continue;
					var newChildNode = Context.ValueNodeReader.GetDebuggerNodeForReuse(node);
					if (!node.SetDebuggerValueNode(newChildNode, recursionCounter + 1)) {
						ResetForReuse();
						return false;
					}
				}
			}
			else
				ResetChildren();

			RefreshAllColumns();
			return true;
		}

		bool IsSame(DbgValueNode oldNode, DbgValueNode newNode) {
			// If any one of them is null (even if both are null), they're not the same
			if (oldNode == null || newNode == null)
				return false;

			if (oldNode.Expression != newNode.Expression)
				return false;
			if (oldNode.ImageName != newNode.ImageName)
				return false;
			if (oldNode.IsReadOnly != newNode.IsReadOnly)
				return false;
			if (oldNode.HasChildren != newNode.HasChildren)
				return false;

			if (TreeNode.IsExpanded) {
				if (oldNode.ChildCount != newNode.ChildCount)
					return false;
			}

			return true;
		}

		void ResetForReuse() {
			ResetChildren();
			RefreshAllColumns();
		}

		void ResetChildren() {
			TreeNode.Children.Clear();
			Debug.Assert(HasDebuggerValueNode);
			TreeNode.LazyLoading = DebuggerValueNode.HasChildren != false;
		}

		void RefreshAllColumns() {
			Context.RefreshNodeOptions =
				RefreshNodeOptions.RefreshName | RefreshNodeOptions.RefreshNameControl |
				RefreshNodeOptions.RefreshValue | RefreshNodeOptions.RefreshValueControl |
				RefreshNodeOptions.RefreshType | RefreshNodeOptions.RefreshTypeControl;
			// Need to call it to refresh the icon (could've changed). It will call OnRefreshUI()
			// which uses the options init'd above
			TreeNode.RefreshUI();
		}
	}
}
