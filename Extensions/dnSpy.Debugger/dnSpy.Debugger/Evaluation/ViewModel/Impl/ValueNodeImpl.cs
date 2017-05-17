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
using System.Windows.Input;
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
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
		public override ImageReference Icon => Context.ValueNodeImageReferenceService.GetImageReference(RawNode.ImageName);

		public override ICommand RefreshExpressionCommand => new RelayCommand(a => RefreshExpression(), a => IsInvalid);
		public override string RefreshExpressionToolTip => dnSpy_Debugger_Resources.RefreshExpressionButtonToolTip;

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
			RawNode.Format(p);
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
		public override string RootId => rootId;
		string rootId;

		public override bool IsInvalid {
			get => isInvalid;
			protected set {
				if (isInvalid == value)
					return;
				isInvalid = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInvalid)));
			}
		}
		bool isInvalid;

		internal bool IsRoot { get; }
		internal RawNode RawNode => __rawNode_DONT_USE;
		RawNode __rawNode_DONT_USE;
		bool disableHighlightingOnReuse;
		ulong? cachedChildCount;

		public override IEditValueProvider NameEditValueProvider => Context.NameEditValueProvider;
		public override IEditableValue NameEditableValue {
			get {
				if (nameEditableValue == null)
					nameEditableValue = new EditableValueImpl(() => GetNameExpression(), s => SaveNameExpression(s), () => CanEditNameExpression());
				return nameEditableValue;
			}
		}
		IEditableValue nameEditableValue;

		public override IEditValueProvider ValueEditValueProvider => Context.ValueEditValueProvider;
		public override IEditableValue ValueEditableValue {
			get {
				if (valueEditableValue == null)
					valueEditableValue = new EditableValueImpl(() => GetEditableValue(), s => SaveEditableValue(s), () => !RawNode.IsReadOnly && !Context.IsWindowReadOnly);
				return valueEditableValue;
			}
		}
		IEditableValue valueEditableValue;

		public static ValueNodeImpl CreateEditNode(IValueNodesContext context) => new ValueNodeImpl(context);

		ValueNodeImpl(IValueNodesContext context) {
			IsRoot = true;
			Context = context ?? throw new ArgumentNullException(nameof(context));
			rootId = null;
			if (!context.EditValueNodeExpression.SupportsEditExpression)
				throw new InvalidOperationException();
			__rawNode_DONT_USE = new EditRawNode();
		}

		public ValueNodeImpl(IValueNodesContext context, DbgValueNode rootValueNode, string rootId, string expression, string errorMessage) {
			IsRoot = true;
			Context = context ?? throw new ArgumentNullException(nameof(context));
			this.rootId = rootId;
			if (rootValueNode == null) {
				__rawNode_DONT_USE = new ErrorRawNode(expression, errorMessage);
				IsInvalid = true;
			}
			else
				__rawNode_DONT_USE = new DbgValueRawNode(Context.ValueNodeReader, rootValueNode);
		}

		public ValueNodeImpl(IValueNodesContext context, RawNode parent, uint childIndex) {
			IsRoot = false;
			Context = context ?? throw new ArgumentNullException(nameof(context));
			__rawNode_DONT_USE = parent.CreateChild(childIndex);
		}

		// We don't check RawRoot if it's a EditRawNode. We overwrite the RootId with the new expression id so
		// the node will get reused (the UI node will still have keyboard focus and be selected).
		internal bool IsEditNode => IsRoot && Context.EditValueNodeExpression.SupportsEditExpression && RootId == null;

		internal bool CanEditNameExpression() => IsRoot && Context.EditValueNodeExpression.SupportsEditExpression && !Context.IsWindowReadOnly;

		EditableValueTextInfo GetNameExpression() {
			if (!CanEditNameExpression())
				throw new InvalidOperationException();
			var text = Context.ExpressionToEdit;
			Context.ExpressionToEdit = null;
			if (text != null)
				return new EditableValueTextInfo(text, EditValueFlags.None);
			var output = new StringBuilderTextColorOutput();
			RawNode.FormatName(output);
			return new EditableValueTextInfo(output.ToString());
		}

		void SaveNameExpression(string expression) {
			if (!CanEditNameExpression())
				throw new InvalidOperationException();
			disableHighlightingOnReuse = true;
			if (IsEditNode) {
				// Use the new expression's id so the UI node gets re-used
				rootId = Context.EditValueNodeExpression.AddExpressions(new[] { expression })[0];
				if (rootId == null)
					throw new InvalidOperationException();
			}
			else
				Context.EditValueNodeExpression.EditExpression(RootId, expression);
		}

		string GetEditableValue() {
			var output = new StringBuilderTextColorOutput();
			var options = Context.ValueNodeFormatParameters.ValueFormatterOptions & ~DbgValueFormatterOptions.Display;
			RawNode.FormatValue(output, options);
			return output.ToString();
		}

		void SaveEditableValue(string expression) {
			if (RawNode.IsReadOnly)
				throw new InvalidOperationException();
			var evalOptions = DbgEvaluationOptions.Expression;
			var res = RawNode.Assign(expression, evalOptions);
			if (res.Error == null)
				oldCachedValue = cachedValue;
			ResetForReuse();
			if (res.Error != null)
				Context.ShowMessageBox(res.Error, ShowMessageBoxButtons.OK);
		}

		public override bool Activate() => IsEditingValue();
		public override void Initialize() => TreeNode.LazyLoading = RawNode.HasChildren != false;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			if (RawNode.HasChildren == false)
				yield break;

			if (IsInvalid) {
				TreeNode.LazyLoading = RawNode.HasChildren != false;
				yield break;
			}

			var childCountTmp = RawNode.ChildCount;
			cachedChildCount = childCountTmp;
			if (childCountTmp == null) {
				TreeNode.LazyLoading = RawNode.HasChildren != false;
				yield break;
			}

			var childCount = childCountTmp.Value;
			if (childCount > MAX_CHILDREN) {
				bool open = Context.ShowMessageBox(string.Format(dnSpy_Debugger_Resources.Locals_Ask_TooManyItems, MAX_CHILDREN), ShowMessageBoxButtons.YesNo);
				if (!open) {
					TreeNode.LazyLoading = RawNode.HasChildren != false;
					yield break;
				}
				childCount = MAX_CHILDREN;
			}
			Debug.Assert(MAX_CHILDREN <= uint.MaxValue);
			Debug.Assert(childCount <= uint.MaxValue);
			uint count = (uint)childCount;
			for (uint i = 0; i < count; i++)
				yield return new ValueNodeImpl(Context, RawNode, i);
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
		internal void SetDebuggerValueNodeForRoot(DbgValueNodeInfo info) {
			if (!IsRoot)
				throw new InvalidOperationException();
			SetDebuggerValueNode(info);
		}

		internal void SetDebuggerValueNode(DbgValueNodeInfo info) {
			if (info.Node == null) {
				var newNode = info.CausesSideEffects ? null : new ErrorRawNode(info.Expression, info.ErrorMessage);
				InvalidateNodes(newNode, recursionCounter: 0);
			}
			else
				SetDebuggerValueNode(new DbgValueRawNode(Context.ValueNodeReader, info.Node), recursionCounter: 0);
		}
		const int MAX_TREEVIEW_RECURSION = 30;

		RawNode CreateNewNode(RawNode newNode) {
			if (newNode != null)
				return newNode;
			if (RawNode is ErrorRawNode errorNode)
				return errorNode;
			if (RawNode is CachedRawNode cachedNode)
				return cachedNode;
			return new CachedRawNode(RawNode.Expression, RawNode.ImageName, RawNode.HasChildren, cachedChildCount, cachedName, cachedValue, cachedExpectedType, cachedActualType);
		}

		void InvalidateNodes(RawNode newNode, int recursionCounter) {
			__rawNode_DONT_USE = CreateNewNode(newNode);
			oldCachedValue = cachedValue;
			// Don't show the value as changed if it's an error message
			if (disableHighlightingOnReuse || __rawNode_DONT_USE is ErrorRawNode)
				oldCachedValue = default(ClassifiedTextCollection);
			disableHighlightingOnReuse = false;
			IsInvalid = true;

			if (recursionCounter >= MAX_TREEVIEW_RECURSION) {
				ResetForReuse();
				return;
			}

			if (TreeNode.Children.Count > 0) {
				var children = TreeNode.Children;
				int count = children.Count;
				for (int i = 0; i < count; i++) {
					var node = (ValueNodeImpl)children[i].Data;
					node.InvalidateNodes(null, recursionCounter + 1);
				}
			}
			RefreshAllColumns();
		}

		bool SetDebuggerValueNode(DebuggerValueRawNode newNode, int recursionCounter) {
			Debug.Assert(newNode != null);
			var oldNode = __rawNode_DONT_USE;
			__rawNode_DONT_USE = newNode;
			oldCachedValue = cachedValue;
			if (disableHighlightingOnReuse)
				oldCachedValue = default(ClassifiedTextCollection);
			disableHighlightingOnReuse = false;
			IsInvalid = false;

			if (recursionCounter >= MAX_TREEVIEW_RECURSION || !IsSame(oldNode, newNode)) {
				ResetForReuse();
				return false;
			}

			if (TreeNode.IsExpanded) {
				var children = TreeNode.Children;
				// This was checked in IsSame
				Debug.Assert(GetChildCount(oldNode) == GetChildCount(newNode));
				Debug.Assert((uint)children.Count <= GetChildCount(newNode));
				int count = children.Count;
				for (int i = 0; i < count; i++) {
					var node = (ValueNodeImpl)children[i].Data;
					var rawNode = node.RawNode as ChildDbgValueRawNode;
					if (rawNode != null && !rawNode.HasDebuggerValueNode) {
						rawNode.SetParent(newNode, null);
						continue;
					}

					var newChildValue = Context.ValueNodeReader.GetDebuggerNodeForReuse(newNode, (uint)i);
					DebuggerValueRawNode newChildNode;
					if (rawNode != null) {
						newChildNode = rawNode;
						rawNode.SetParent(newNode, newChildValue);
					}
					else
						newChildNode = new ChildDbgValueRawNode(newNode, (uint)i, Context.ValueNodeReader, newChildValue);
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

		bool IsSame(RawNode oldNode, DebuggerValueRawNode newNode) {
			// If any one of them is null (even if both are null), they're not the same
			if (oldNode == null || newNode == null)
				return false;

			if (oldNode.Expression != newNode.Expression)
				return false;
			if (oldNode.ImageName != newNode.ImageName)
				return false;
			if (oldNode.HasChildren != newNode.HasChildren)
				return false;

			if (TreeNode.IsExpanded) {
				var oldCount = GetChildCount(oldNode);
				var newCount = GetChildCount(newNode);
				// If any one of them is null, it's unknown and never matches the other one even if it's also null
				if (oldCount == null || newCount == null || oldCount != newCount)
					return false;
			}

			return true;
		}

		static ulong? GetChildCount(RawNode node) => node.HasChildren == false ? 0 : node.ChildCount;

		void RefreshExpression() {
			if (Context.IsWindowReadOnly)
				return;
			var res = Context.ValueNodeReader.Evaluate(RawNode.Expression);
			if (res.Error != null)
				SetDebuggerValueNode(new DbgValueNodeInfo(RootId ?? RawNode.Expression, RawNode.Expression, res.Error, res.CausesSideEffects));
			else
				SetDebuggerValueNode(new DbgValueNodeInfo(res.ValueNode));
		}

		void ResetForReuse() {
			ResetChildren();
			RefreshAllColumns();
		}

		void ResetChildren() {
			cachedChildCount = null;
			TreeNode.Children.Clear();
			TreeNode.LazyLoading = RawNode.HasChildren != false;
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

		bool IsEditingValue() => nameEditableValue?.IsEditingValue == true || valueEditableValue?.IsEditingValue == true;

		internal void ClearEditingValueProperties() {
			if (nameEditableValue != null)
				nameEditableValue.IsEditingValue = false;
			if (valueEditableValue != null)
				valueEditableValue.IsEditingValue = false;
		}
	}
}
