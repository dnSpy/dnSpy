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
using System.Globalization;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.Text;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	/// <summary>
	/// Base class of nodes containing the data shown in the UI. Some of them contain real debugger data,
	/// others contain cached data or error messages.
	/// </summary>
	abstract class RawNode {
		/// <summary>
		/// Some nodes can delay reading the underlying debugger <see cref="DbgValueNode"/>. If the real
		/// data hasn't been read yet, this property returns false. This is used to prevent reading the
		/// underlying data when re-using nodes.
		/// </summary>
		public virtual bool HasInitializedUnderlyingData => true;
		public abstract bool CanEvaluateExpression { get; }
		public abstract string Expression { get; }
		public abstract string ImageName { get; }
		public abstract bool IsReadOnly { get; }
		public abstract bool? HasChildren { get; }
		public abstract ulong? GetChildCount(DbgEvaluationInfo evalInfo);
		public virtual RawNode CreateChild(Action<ChildDbgValueRawNode, object> debuggerValueNodeChanged, object debuggerValueNodeChangedData, uint index) => throw new NotSupportedException();
		public abstract void Format(DbgEvaluationInfo? evalInfo, IDbgValueNodeFormatParameters options, CultureInfo? cultureInfo);
		public abstract void FormatName(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo);
		public abstract void FormatValue(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo);
		public abstract DbgValueNodeAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options);
	}

	sealed class EditRawNode : RawNode {
		public override bool CanEvaluateExpression => false;
		public override string Expression => string.Empty;
		public override string ImageName => PredefinedDbgValueNodeImageNames.Edit;
		public override bool IsReadOnly => true;
		public override bool? HasChildren => false;
		public override ulong? GetChildCount(DbgEvaluationInfo evalInfo) => 0;
		public override void Format(DbgEvaluationInfo? evalInfo, IDbgValueNodeFormatParameters options, CultureInfo? cultureInfo) { }
		public override void FormatName(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo) { }
		public override void FormatValue(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo) { }
		public override DbgValueNodeAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options) => throw new NotSupportedException();
	}

	sealed class ErrorRawNode : RawNode {
		public override bool CanEvaluateExpression => true;
		public string ErrorMessage => errorMessage;
		public override string Expression => expression;
		public override string ImageName => PredefinedDbgValueNodeImageNames.Error;
		public override bool IsReadOnly => true;
		public override bool? HasChildren => false;
		public override ulong? GetChildCount(DbgEvaluationInfo evalInfo) => 0;

		readonly string expression;
		string errorMessage;

		public ErrorRawNode(string expression, string errorMessage) {
			this.expression = expression ?? throw new ArgumentNullException(nameof(expression));
			this.errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
		}

		internal void SetErrorMessage(string errorMessage) => this.errorMessage = errorMessage;

		public override void Format(DbgEvaluationInfo? evalInfo, IDbgValueNodeFormatParameters options, CultureInfo? cultureInfo) {
			if (!(options.NameOutput is null))
				FormatName(evalInfo, options.NameOutput, options.NameFormatterOptions, cultureInfo);
			if (!(options.ValueOutput is null))
				FormatValue(evalInfo, options.ValueOutput, options.ValueFormatterOptions, cultureInfo);
		}

		public override void FormatName(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo) => output.Write(DbgTextColor.Text, expression);
		public override void FormatValue(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo) => output.Write(DbgTextColor.Error, errorMessage);
		public override DbgValueNodeAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options) => throw new NotSupportedException();
	}

	abstract class CachedRawNodeBase : RawNode {
		public sealed override bool IsReadOnly => true;
		public sealed override DbgValueNodeAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options) => throw new NotImplementedException();

		protected abstract ref readonly ClassifiedTextCollection CachedName { get; }
		protected abstract ref readonly ClassifiedTextCollection CachedValue { get; }
		protected abstract ref readonly ClassifiedTextCollection CachedExpectedType { get; }
		protected abstract ref readonly ClassifiedTextCollection CachedActualType { get; }

		public sealed override void Format(DbgEvaluationInfo? evalInfo, IDbgValueNodeFormatParameters options, CultureInfo? cultureInfo) {
			if (!(options.NameOutput is null))
				FormatName(evalInfo, options.NameOutput, options.NameFormatterOptions, cultureInfo);
			if (!(options.ValueOutput is null))
				FormatValue(evalInfo, options.ValueOutput, options.ValueFormatterOptions, cultureInfo);
			if (!(options.ExpectedTypeOutput is null))
				WriteTo(options.ExpectedTypeOutput, CachedExpectedType);
			if (!(options.ActualTypeOutput is null))
				WriteTo(options.ActualTypeOutput, CachedActualType);
		}

		public sealed override void FormatName(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo) => WriteTo(output, CachedName, string.IsNullOrEmpty(Expression) ? UNKNOWN : Expression);
		public sealed override void FormatValue(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo) => WriteTo(output, CachedValue);

		const string UNKNOWN = "???";
		static void WriteTo(IDbgTextWriter output, in ClassifiedTextCollection coll, string unknownText = UNKNOWN) {
			if (coll.IsDefault) {
				output.Write(DbgTextColor.Error, unknownText);
				return;
			}

			coll.WriteTo(output);
		}
	}

	sealed class EmptyCachedRawNode : CachedRawNodeBase {
		public static readonly EmptyCachedRawNode Instance = new EmptyCachedRawNode();
		EmptyCachedRawNode() { }

		public override bool CanEvaluateExpression => false;
		public override string Expression => string.Empty;
		public override string ImageName => PredefinedDbgValueNodeImageNames.Error;
		public override bool? HasChildren => false;
		public override ulong? GetChildCount(DbgEvaluationInfo evalInfo) => 0;

		protected override ref readonly ClassifiedTextCollection CachedName => ref ClassifiedTextCollection.Empty;
		protected override ref readonly ClassifiedTextCollection CachedValue => ref ClassifiedTextCollection.Empty;
		protected override ref readonly ClassifiedTextCollection CachedExpectedType => ref ClassifiedTextCollection.Empty;
		protected override ref readonly ClassifiedTextCollection CachedActualType => ref ClassifiedTextCollection.Empty;
	}

	sealed class CachedRawNode : CachedRawNodeBase {
		public override bool CanEvaluateExpression { get; }
		public override string Expression { get; }
		public override string ImageName { get; }
		public override bool? HasChildren { get; }
		public override ulong? GetChildCount(DbgEvaluationInfo evalInfo) => childCount;
		readonly ulong? childCount;

		protected override ref readonly ClassifiedTextCollection CachedName => ref cachedName;
		protected override ref readonly ClassifiedTextCollection CachedValue => ref cachedValue;
		protected override ref readonly ClassifiedTextCollection CachedExpectedType => ref cachedExpectedType;
		protected override ref readonly ClassifiedTextCollection CachedActualType => ref cachedActualType;

		readonly ClassifiedTextCollection cachedName;
		readonly ClassifiedTextCollection cachedValue;
		readonly ClassifiedTextCollection cachedExpectedType;
		readonly ClassifiedTextCollection cachedActualType;

		public CachedRawNode(bool canEvaluateExpression, string expression, string imageName, bool? hasChildren, ulong? childCount, in ClassifiedTextCollection cachedName, in ClassifiedTextCollection cachedValue, in ClassifiedTextCollection cachedExpectedType, in ClassifiedTextCollection cachedActualType) {
			CanEvaluateExpression = canEvaluateExpression;
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			ImageName = imageName ?? throw new ArgumentNullException(nameof(imageName));
			HasChildren = hasChildren;
			this.childCount = childCount;
			this.cachedName = cachedName;
			this.cachedValue = cachedValue;
			this.cachedExpectedType = cachedExpectedType;
			this.cachedActualType = cachedActualType.IsDefault ? ref cachedExpectedType : ref cachedActualType;
		}
	}

	/// <summary>
	/// Base class of nodes containing actual debugger data (a <see cref="DbgValueNode"/>)
	/// </summary>
	abstract class DebuggerValueRawNode : RawNode {
		public override bool CanEvaluateExpression => DebuggerValueNode.CanEvaluateExpression;
		public override string Expression => DebuggerValueNode.Expression;
		public override string ImageName => DebuggerValueNode.ImageName;
		public override bool IsReadOnly => DebuggerValueNode.IsReadOnly;
		public override bool? HasChildren => DebuggerValueNode.HasChildren;
		public override ulong? GetChildCount(DbgEvaluationInfo evalInfo) => DebuggerValueNode.GetChildCount(evalInfo);

		internal abstract DbgValueNode DebuggerValueNode { get; }

		protected DbgValueNodeReader reader;

		protected DebuggerValueRawNode(DbgValueNodeReader reader) => this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

		public override RawNode CreateChild(Action<ChildDbgValueRawNode, object> debuggerValueNodeChanged, object debuggerValueNodeChangedData, uint index) =>
			new ChildDbgValueRawNode(debuggerValueNodeChanged, debuggerValueNodeChangedData, this, index, reader);

		public override void Format(DbgEvaluationInfo? evalInfo, IDbgValueNodeFormatParameters options, CultureInfo? cultureInfo) {
			if (!(evalInfo is null))
				DebuggerValueNode.Format(evalInfo, options, cultureInfo);
			else {
				if (!(options.NameOutput is null))
					FormatName(evalInfo, options.NameOutput, options.NameFormatterOptions, cultureInfo);
				if (!(options.ValueOutput is null))
					FormatValue(evalInfo, options.ValueOutput, options.ValueFormatterOptions, cultureInfo);
			}
		}

		public override void FormatName(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo) {
			if (!(evalInfo is null))
				DebuggerValueNode.FormatName(evalInfo, output, options, cultureInfo);
			else
				output.Write(DbgTextColor.Error, "???");
		}

		public override void FormatValue(DbgEvaluationInfo? evalInfo, IDbgTextWriter output, DbgValueFormatterOptions options, CultureInfo? cultureInfo) {
			if (!(evalInfo is null))
				DebuggerValueNode.FormatValue(evalInfo, output, options, cultureInfo);
			else
				output.Write(DbgTextColor.Error, "???");
		}

		public override DbgValueNodeAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options) =>
			DebuggerValueNode.Assign(evalInfo, expression, options);
	}

	/// <summary>
	/// This is usually the root node shown in the variables windows, but it could also be a child node if
	/// it got refreshed (user pressed the Refresh icon).
	/// </summary>
	sealed class DbgValueRawNode : DebuggerValueRawNode {
		internal override DbgValueNode DebuggerValueNode { get; }
		public DbgValueRawNode(DbgValueNodeReader reader, DbgValueNode valueNode)
			: base(reader) => DebuggerValueNode = valueNode ?? throw new ArgumentNullException(nameof(valueNode));
	}

	sealed class ChildDbgValueRawNode : DebuggerValueRawNode {
		public override bool HasInitializedUnderlyingData => !(__dbgValueNode_DONT_USE is null);
		internal DebuggerValueRawNode Parent => parent;
		internal uint DbgValueNodeChildIndex { get; }

		internal override DbgValueNode DebuggerValueNode {
			get {
				var dbgNode = __dbgValueNode_DONT_USE;
				if (dbgNode is null) {
					__dbgValueNode_DONT_USE = dbgNode = reader.GetDebuggerNode(this);
					debuggerValueNodeChanged(this, debuggerValueNodeChangedData);
				}
				return dbgNode;
			}
		}
		DbgValueNode? __dbgValueNode_DONT_USE;

		DebuggerValueRawNode parent;
		readonly Action<ChildDbgValueRawNode, object> debuggerValueNodeChanged;
		readonly object debuggerValueNodeChangedData;

		public ChildDbgValueRawNode(Action<ChildDbgValueRawNode, object> debuggerValueNodeChanged, object debuggerValueNodeChangedData, DebuggerValueRawNode parent, uint dbgValueNodeChildIndex, DbgValueNodeReader reader)
			: base(reader) {
			this.debuggerValueNodeChanged = debuggerValueNodeChanged ?? throw new ArgumentNullException(nameof(debuggerValueNodeChanged));
			this.debuggerValueNodeChangedData = debuggerValueNodeChangedData;
			this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
			DbgValueNodeChildIndex = dbgValueNodeChildIndex;
		}

		public ChildDbgValueRawNode(Action<ChildDbgValueRawNode, object> debuggerValueNodeChanged, object debuggerValueNodeChangedData, DebuggerValueRawNode parent, uint dbgValueNodeChildIndex, DbgValueNodeReader reader, DbgValueNode value)
			: base(reader) {
			this.debuggerValueNodeChanged = debuggerValueNodeChanged ?? throw new ArgumentNullException(nameof(debuggerValueNodeChanged));
			this.debuggerValueNodeChangedData = debuggerValueNodeChangedData;
			this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
			DbgValueNodeChildIndex = dbgValueNodeChildIndex;
			__dbgValueNode_DONT_USE = value ?? throw new ArgumentNullException(nameof(value));
		}

		internal void SetParent(DebuggerValueRawNode parent, DbgValueNode? newValue) {
			this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
			__dbgValueNode_DONT_USE = newValue;
		}
	}
}
