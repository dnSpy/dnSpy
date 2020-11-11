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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
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

		public event PropertyChangedEventHandler? PropertyChanged;
		public override ImageReference Icon => Context.ValueNodeImageReferenceService.GetImageReference(RawNode.ImageName);

		public override ICommand RefreshExpressionCommand => new RelayCommand(a => RefreshExpression(), a => CanRefreshExpression);
		public override string RefreshExpressionToolTip => dnSpy_Debugger_Resources.RefreshExpressionButtonToolTip;

		public override FormatterObject<ValueNode> NameObject => new FormatterObject<ValueNode>(this, Context.NameColumnName);
		public override FormatterObject<ValueNode> ValueObject => new FormatterObject<ValueNode>(this, Context.ValueColumnName);
		public override FormatterObject<ValueNode> TypeObject => new FormatterObject<ValueNode>(this, Context.TypeColumnName);

		public override ref readonly ClassifiedTextCollection CachedName {
			get {
				if (cachedName.IsDefault)
					InitializeCachedText();
				return ref cachedName;
			}
		}

		public override ref readonly ClassifiedTextCollection CachedValue {
			get {
				if (cachedValue.IsDefault)
					InitializeCachedText();
				return ref cachedValue;
			}
		}

		public override ref readonly ClassifiedTextCollection CachedExpectedType {
			get {
				if (cachedExpectedType.IsDefault)
					InitializeCachedText();
				return ref cachedExpectedType;
			}
		}

		/// <summary>
		/// It's default if it's identical to <see cref="CachedExpectedType"/>
		/// </summary>
		public override ref readonly ClassifiedTextCollection CachedActualType_OrDefaultInstance {
			get {
				// Check cachedExpectedType and not cachedActualType since cachedActualType
				// is default if it equals cachedExpectedType
				if (cachedExpectedType.IsDefault)
					InitializeCachedText();
				return ref cachedActualType;
			}
		}

		public override ref readonly ClassifiedTextCollection OldCachedValue => ref oldCachedValue;

		void InitializeCachedText() {
			var p = Context.ValueNodeFormatParameters;
			p.Initialize(cachedName.IsDefault, cachedValue.IsDefault, cachedExpectedType.IsDefault);
			RawNode.Format(Context.EvaluationInfo, p, Context.FormatCulture);
			if (cachedName.IsDefault)
				cachedName = p.NameOutput.GetClassifiedText();
			if (cachedValue.IsDefault)
				cachedValue = p.ValueOutput.GetClassifiedText();
			if (cachedExpectedType.IsDefault) {
				cachedExpectedType = p.ExpectedTypeOutput.GetClassifiedText();
				if (p.ActualTypeOutput.Equals(cachedExpectedType)) {
					p.ActualTypeOutput.Clear();
					cachedActualType = default;
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
		public override string? RootId => rootId;
		string? rootId;

		public bool IsDisabled => IsInvalid || Context.IsWindowReadOnly;

		public override bool IsInvalid {
			get => isInvalid;
			protected set {
				if (isInvalid == value)
					return;
				bool oldIsDisabled = IsDisabled;
				isInvalid = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInvalid)));
				if (oldIsDisabled != IsDisabled)
					RefreshAllColumns();
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
				if (nameEditableValue is null)
					nameEditableValue = new EditableValueImpl(() => GetNameExpression(), s => SaveNameExpression(s), () => CanEditNameExpression(), IsEditNode ? EditableValueOptions.SingleClick : EditableValueOptions.None);
				return nameEditableValue;
			}
		}
		IEditableValue? nameEditableValue;

		public override IEditValueProvider ValueEditValueProvider => Context.ValueEditValueProvider;
		public override IEditableValue ValueEditableValue {
			get {
				if (valueEditableValue is null)
					valueEditableValue = new EditableValueImpl(() => GetEditableValue(), s => SaveEditableValue(s), () => CanEditValue());
				return valueEditableValue;
			}
		}
		IEditableValue? valueEditableValue;

		public static ValueNodeImpl CreateEditNode(IValueNodesContext context) => new ValueNodeImpl(context);

		ValueNodeImpl(IValueNodesContext context) {
			IsRoot = true;
			Context = context ?? throw new ArgumentNullException(nameof(context));
			rootId = null;
			if (!context.EditValueNodeExpression.SupportsEditExpression)
				throw new InvalidOperationException();
			__rawNode_DONT_USE = new EditRawNode();
		}

		public ValueNodeImpl(IValueNodesContext context, DbgValueNode? rootValueNode, string? rootId, string? expression, string? errorMessage) {
			IsRoot = true;
			Context = context ?? throw new ArgumentNullException(nameof(context));
			this.rootId = rootId;
			if (rootValueNode is null) {
				__rawNode_DONT_USE = new ErrorRawNode(expression!, errorMessage!);
				IsInvalid = true;
			}
			else {
				__rawNode_DONT_USE = new DbgValueRawNode(Context.ValueNodeReader, rootValueNode);
				IsInvalid = rootValueNode.HasError;
			}
		}

		public void Reuse(DbgValueNode? rootValueNode, string? rootId, string? expression, string? errorMessage) {
			this.rootId = rootId;
			oldCachedValue = default;
			disableHighlightingOnReuse = false;
			if (rootValueNode is null) {
				__rawNode_DONT_USE = new ErrorRawNode(expression!, errorMessage!);
				IsInvalid = true;
			}
			else {
				__rawNode_DONT_USE = new DbgValueRawNode(Context.ValueNodeReader, rootValueNode);
				IsInvalid = rootValueNode.HasError;
			}
			ResetForReuse();
		}

		public ValueNodeImpl(IValueNodesContext context, RawNode parent, uint childIndex) {
			IsRoot = false;
			Context = context ?? throw new ArgumentNullException(nameof(context));
			__rawNode_DONT_USE = parent.CreateChild(debuggerValueNodeChanged, this, childIndex);
		}

		internal bool IsEditNode => IsRoot && Context.EditValueNodeExpression.SupportsEditExpression && RootId is null;

		internal bool CanEditNameExpression() => IsRoot && Context.EditValueNodeExpression.SupportsEditExpression && !Context.IsWindowReadOnly && Context.EvaluationInfo is not null;

		EditableValueTextInfo GetNameExpression() {
			if (!CanEditNameExpression())
				throw new InvalidOperationException();
			var text = Context.ExpressionToEdit;
			Context.ExpressionToEdit = null;
			if (text is not null)
				return new EditableValueTextInfo(text, EditValueFlags.None);
			// Always use the expression since the Name column could've been replaced with any random
			// text if DebuggerDisplayAttribute.Name property isn't null.
			return new EditableValueTextInfo(RawNode.Expression, EditValueFlags.SelectText);
		}

		void SaveNameExpression(string? expression) {
			if (!CanEditNameExpression())
				throw new InvalidOperationException();
			if (expression is null)
				expression = string.Empty;
			if (GetNameExpression().Text == expression)
				return;
			disableHighlightingOnReuse = true;
			if (IsEditNode)
				Context.EditValueNodeExpression.AddExpressions(new[] { expression });
			else
				Context.EditValueNodeExpression.EditExpression(RootId, expression);
		}

		internal bool CanEditValue() => !RawNode.IsReadOnly && !Context.IsWindowReadOnly && Context.EvaluationInfo is not null;

		EditableValueTextInfo GetEditableValue() {
			if (!CanEditValue())
				throw new InvalidOperationException();
			Debug2.Assert(Context.EvaluationInfo is not null);
			var text = Context.ExpressionToEdit;
			Context.ExpressionToEdit = null;
			if (text is not null)
				return new EditableValueTextInfo(text, EditValueFlags.None);
			var output = new DbgStringBuilderTextWriter();
			var options = Context.ValueNodeFormatParameters.ValueFormatterOptions | DbgValueFormatterOptions.Edit;
			RawNode.FormatValue(Context.EvaluationInfo, output, options, Context.FormatCulture);
			return new EditableValueTextInfo(output.ToString());
		}

		void SaveEditableValue(string? expression) {
			if (!CanEditValue())
				throw new InvalidOperationException();
			if (expression is null)
				expression = string.Empty;
			Debug2.Assert(Context.EvaluationInfo is not null);
			if (GetEditableValue().Text == expression)
				return;
			var res = RawNode.Assign(Context.EvaluationInfo, expression, Context.EvaluationOptions);
			if (res.Error is null)
				oldCachedValue = cachedValue;
			bool retry = res.Error is not null &&
				(res.Flags & DbgEEAssignmentResultFlags.CompilerError) != 0 &&
				(res.Flags & DbgEEAssignmentResultFlags.ExecutedCode) == 0 &&
				CanEditValue();
			Context.OnValueNodeAssigned(res.Error, retry);
			if (retry) {
				Context.ExpressionToEdit = expression;
				ValueEditableValue.IsEditingValue = true;
			}
		}

		public override bool Activate() => IsEditingValue();
		public override void Initialize() => ResetLazyLoading();

		void ResetLazyLoading() {
			if (RawNode.HasInitializedUnderlyingData)
				TreeNode.LazyLoading = RawNode.HasChildren != false;
			else {
				// The underlying DbgValueNode hasn't been read yet and we must not read it now (eg. it
				// could be a child of a large array or a class with lots of properties/fields).
				// Whenever it has read its value, it will notify us and we'll update LazyLoading.
				TreeNode.LazyLoading = true;
			}
		}

		// To minimize allocations we use a static delegate and pass in 'this' to the delegate
		static readonly Action<ChildDbgValueRawNode, object> debuggerValueNodeChanged = OnDebuggerValueNodeChanged;
		static void OnDebuggerValueNodeChanged(ChildDbgValueRawNode rawNode, object obj) {
			if (!rawNode.HasInitializedUnderlyingData)
				throw new InvalidOperationException();
			var self = (ValueNodeImpl)obj;
			if (rawNode != self.RawNode)
				throw new InvalidOperationException();
			self.ResetLazyLoading();
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			if (RawNode.HasChildren == false)
				yield break;
			Debug.Assert(RawNode.HasInitializedUnderlyingData);

			if (IsInvalid) {
				ResetLazyLoading();
				yield break;
			}

			Debug2.Assert(Context.EvaluationInfo is not null);
			var childCountTmp = RawNode.GetChildCount(Context.EvaluationInfo);
			cachedChildCount = childCountTmp;
			if (childCountTmp is null) {
				ResetLazyLoading();
				yield break;
			}

			var childCount = childCountTmp.Value;
			if (childCount > MAX_CHILDREN) {
				bool open = Context.ShowMessageBox(string.Format(dnSpy_Debugger_Resources.Locals_Ask_TooManyItems, MAX_CHILDREN), ShowMessageBoxButtons.YesNo);
				if (!open) {
					ResetLazyLoading();
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
				cachedName = default;
			if ((options & RefreshNodeOptions.RefreshValue) != 0)
				cachedValue = default;
			if ((options & RefreshNodeOptions.RefreshType) != 0) {
				cachedExpectedType = default;
				cachedActualType = default;
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
		internal void SetDebuggerValueNodeForRoot(in DbgValueNodeInfo info) {
			if (!IsRoot)
				throw new InvalidOperationException();
			rootId = info.Id;
			SetDebuggerValueNode(info);
		}

		internal void SetDebuggerValueNode(in DbgValueNodeInfo info) {
			if (info.Node is null) {
				var newNode = info.CausesSideEffects ? null : new ErrorRawNode(info.Expression!, info.ErrorMessage!);
				InvalidateNodes(newNode, recursionCounter: 0);
			}
			else if (info.Node.HasError) {
				var newNode = info.CausesSideEffects ? null : new DbgValueRawNode(Context.ValueNodeReader, info.Node);
				InvalidateNodes(newNode, recursionCounter: 0);
			}
			else
				SetDebuggerValueNode(new DbgValueRawNode(Context.ValueNodeReader, info.Node), recursionCounter: 0);
		}
		const int MAX_TREEVIEW_RECURSION = 30;

		RawNode CreateNewNode(RawNode? newNode) {
			if (newNode is not null)
				return newNode;
			if (RawNode is ErrorRawNode errorNode)
				return errorNode;
			if (RawNode is CachedRawNodeBase cachedNode)
				return cachedNode;
			if (!RawNode.HasInitializedUnderlyingData)
				return EmptyCachedRawNode.Instance;
			return new CachedRawNode(RawNode.CanEvaluateExpression, RawNode.Expression, RawNode.ImageName, RawNode.HasChildren, cachedChildCount, cachedName, cachedValue, cachedExpectedType, cachedActualType);
		}

		void InvalidateNodes(RawNode? newNode, int recursionCounter) {
			__rawNode_DONT_USE = CreateNewNode(newNode);
			oldCachedValue = cachedValue;
			// Don't show the value as changed if it's an error message
			if (disableHighlightingOnReuse || __rawNode_DONT_USE is ErrorRawNode || (__rawNode_DONT_USE is DebuggerValueRawNode valueNode && valueNode.HasInitializedUnderlyingData && valueNode.DebuggerValueNode.HasError))
				oldCachedValue = default;
			disableHighlightingOnReuse = false;
			IsInvalid = true;

			if (recursionCounter >= MAX_TREEVIEW_RECURSION) {
				ResetForReuse();
				return;
			}

			if (!(__rawNode_DONT_USE is ErrorRawNode) && TreeNode.Children.Count > 0) {
				var children = TreeNode.Children;
				int count = children.Count;
				for (int i = 0; i < count; i++) {
					var node = (ValueNodeImpl)children[i].Data;
					node.InvalidateNodes(null, recursionCounter + 1);
				}
			}
			else
				ResetChildren();

			RefreshAllColumns();
		}

		bool SetDebuggerValueNode(DebuggerValueRawNode newNode, int recursionCounter) {
			Debug2.Assert(newNode is not null);
			var oldNode = __rawNode_DONT_USE;
			__rawNode_DONT_USE = newNode;
			oldCachedValue = cachedValue;
			if (disableHighlightingOnReuse)
				oldCachedValue = default;
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
					var childNode = (ValueNodeImpl)children[i].Data;
					var childRawNode = childNode.RawNode as ChildDbgValueRawNode;
					if (childRawNode is not null && !childRawNode.HasInitializedUnderlyingData) {
						childRawNode.SetParent(newNode, null);
						continue;
					}

					// Check if we must read the value. If its treenode is expanded, it must be read now, else it can be delayed
					if (childRawNode is not null && childNode.TreeNode.IsExpanded) {
						var newChildValue = Context.ValueNodeReader.GetDebuggerNodeForReuse(newNode, (uint)i);
						// We have to create a new one here and can't reuse the existing ChildDbgValueRawNode by
						// calling its SetParent() method. Otherwise IsSame() above will compare the same
						// reference against itself.
						var newChildRawNode = new ChildDbgValueRawNode(debuggerValueNodeChanged, childNode, newNode, childRawNode.DbgValueNodeChildIndex, Context.ValueNodeReader, newChildValue);
						if (!childNode.SetDebuggerValueNode(newChildRawNode, recursionCounter + 1)) {
							ResetForReuse();
							return false;
						}
					}
					else {
						// It's safe to read the underlying data lazily
						if (childRawNode is not null)
							childRawNode.SetParent(newNode, null);
						else
							childNode.__rawNode_DONT_USE = new ChildDbgValueRawNode(debuggerValueNodeChanged, childNode, newNode, (uint)i, Context.ValueNodeReader);
						childNode.oldCachedValue = childNode.cachedValue;
						if (childNode.disableHighlightingOnReuse)
							childNode.oldCachedValue = default;
						childNode.disableHighlightingOnReuse = false;
						childNode.IsInvalid = false;
						childNode.ResetForReuse();
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
			if (oldNode is null || newNode is null)
				return false;

			Debug.Assert(oldNode.HasInitializedUnderlyingData);
			Debug.Assert(newNode.HasInitializedUnderlyingData);

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
				if (oldCount is null || newCount is null || oldCount != newCount)
					return false;
			}

			return true;
		}

		ulong? GetChildCount(RawNode node) => node.HasChildren == false ? 0 : node.GetChildCount(Context.EvaluationInfo!);

		// Don't allow refreshing the value if it's an EmptyCachedRawNode since it doesn't have the original expression
		bool CanRefreshExpression => IsInvalid && RawNode.CanEvaluateExpression && !string.IsNullOrEmpty(RawNode.Expression);

		void RefreshExpression() {
			if (!CanRefreshExpression)
				throw new InvalidOperationException();
			if (Context.IsWindowReadOnly)
				return;
			var res = Context.ValueNodeReader.Evaluate(RawNode.Expression);
			if (res.Node is null)
				res = new DbgValueNodeInfo(RootId ?? res.Expression!, res.Expression!, res.ErrorMessage!, res.CausesSideEffects);
			SetDebuggerValueNode(res);
		}

		void ResetForReuse() {
			ResetChildren();
			RefreshAllColumns();
		}

		void ResetChildren() {
			cachedChildCount = null;
			TreeNode.Children.Clear();
			ResetLazyLoading();
		}

		void RefreshAllColumns() {
			// We can't check PropertyChanged if it's null since it can be non-null even if this
			// node isn't visible on the screen.
			if (cachedName.IsDefault && cachedValue.IsDefault && cachedExpectedType.IsDefault) {
				// All cached values are default so there's no reason to clear them
				return;
			}

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
			if (nameEditableValue is not null)
				nameEditableValue.IsEditingValue = false;
			if (valueEditableValue is not null)
				valueEditableValue.IsEditingValue = false;
		}
	}
}
