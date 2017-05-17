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
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.Text;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	/// <summary>
	/// Base class of nodes containing the data shown in the UI. Some of them contain real debugger data,
	/// others contain cached data or error messages.
	/// </summary>
	abstract class RawNode {
		public abstract string Expression { get; }
		public abstract string ImageName { get; }
		public abstract bool IsReadOnly { get; }
		public abstract bool? HasChildren { get; }
		public abstract ulong? ChildCount { get; }
		public virtual RawNode CreateChild(uint index) => throw new NotSupportedException();
		public abstract void Format(IDbgValueNodeFormatParameters options);
		public abstract void FormatName(ITextColorWriter output);
		public abstract void FormatValue(ITextColorWriter output, DbgValueFormatterOptions options);
		public abstract DbgValueNodeAssignmentResult Assign(string expression, DbgEvaluationOptions options);
	}

	sealed class EditRawNode : RawNode {
		public override string Expression => string.Empty;
		public override string ImageName => PredefinedDbgValueNodeImageNames.Edit;
		public override bool IsReadOnly => true;
		public override bool? HasChildren => false;
		public override ulong? ChildCount => 0;
		public override void Format(IDbgValueNodeFormatParameters options) { }
		public override void FormatName(ITextColorWriter output) { }
		public override void FormatValue(ITextColorWriter output, DbgValueFormatterOptions options) { }
		public override DbgValueNodeAssignmentResult Assign(string expression, DbgEvaluationOptions options) => throw new NotSupportedException();
	}

	sealed class ErrorRawNode : RawNode {
		public string ErrorMessage => errorMessage;
		public override string Expression => expression;
		public override string ImageName => PredefinedDbgValueNodeImageNames.Error;
		public override bool IsReadOnly => true;
		public override bool? HasChildren => false;
		public override ulong? ChildCount => 0;

		readonly string expression;
		string errorMessage;

		public ErrorRawNode(string expression, string errorMessage) {
			this.expression = expression ?? throw new ArgumentNullException(nameof(expression));
			this.errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
		}

		internal void SetErrorMessage(string errorMessage) => this.errorMessage = errorMessage;

		public override void Format(IDbgValueNodeFormatParameters options) {
			if (options.NameOutput != null)
				FormatName(options.NameOutput);
			if (options.ValueOutput != null)
				FormatValue(options.ValueOutput, options.ValueFormatterOptions);
		}

		public override void FormatName(ITextColorWriter output) => output.Write(BoxedTextColor.Text, expression);
		public override void FormatValue(ITextColorWriter output, DbgValueFormatterOptions options) => output.Write(BoxedTextColor.Error, errorMessage);
		public override DbgValueNodeAssignmentResult Assign(string expression, DbgEvaluationOptions options) => throw new NotSupportedException();
	}

	sealed class CachedRawNode : RawNode {
		public override string Expression { get; }
		public override string ImageName { get; }
		public override bool IsReadOnly => true;
		public override bool? HasChildren { get; }
		public override ulong? ChildCount { get; }

		/*readonly*/ ClassifiedTextCollection cachedName;
		/*readonly*/ ClassifiedTextCollection cachedValue;
		/*readonly*/ ClassifiedTextCollection cachedExpectedType;
		/*readonly*/ ClassifiedTextCollection cachedActualType;

		public CachedRawNode(string expression, string imageName, bool? hasChildren, ulong? childCount, ClassifiedTextCollection cachedName, ClassifiedTextCollection cachedValue, ClassifiedTextCollection cachedExpectedType, ClassifiedTextCollection cachedActualType) {
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			ImageName = imageName ?? throw new ArgumentNullException(nameof(imageName));
			HasChildren = hasChildren;
			ChildCount = childCount;
			this.cachedName = cachedName;
			this.cachedValue = cachedValue;
			this.cachedExpectedType = cachedExpectedType;
			this.cachedActualType = cachedActualType.IsDefault ? cachedExpectedType : cachedActualType;
		}

		public override void Format(IDbgValueNodeFormatParameters options) {
			if (options.NameOutput != null)
				FormatName(options.NameOutput);
			if (options.ValueOutput != null)
				FormatValue(options.ValueOutput, options.ValueFormatterOptions);
			if (options.ExpectedTypeOutput != null)
				WriteTo(options.ExpectedTypeOutput, cachedExpectedType);
			if (options.ActualTypeOutput != null)
				WriteTo(options.ActualTypeOutput, cachedActualType);
		}

		public override void FormatName(ITextColorWriter output) => WriteTo(output, cachedName, Expression);
		public override void FormatValue(ITextColorWriter output, DbgValueFormatterOptions options) => WriteTo(output, cachedValue);

		static void WriteTo(ITextColorWriter output, ClassifiedTextCollection coll, string unknownText = "???") {
			if (coll.IsDefault) {
				output.Write(BoxedTextColor.Error, unknownText);
				return;
			}

			foreach (var info in coll.Result)
				output.Write(info.Color, info.Text);
		}

		public override DbgValueNodeAssignmentResult Assign(string expression, DbgEvaluationOptions options) => throw new NotImplementedException();
	}

	/// <summary>
	/// Base class of nodes containing actual debugger data (a <see cref="DbgValueNode"/>)
	/// </summary>
	abstract class DebuggerValueRawNode : RawNode {
		public override string Expression => DebuggerValueNode.Expression;
		public override string ImageName => DebuggerValueNode.ImageName;
		public override bool IsReadOnly => DebuggerValueNode.IsReadOnly;
		public override bool? HasChildren => DebuggerValueNode.HasChildren;
		public override ulong? ChildCount => DebuggerValueNode.ChildCount;

		internal abstract DbgValueNode DebuggerValueNode { get; }

		protected DbgValueNodeReader reader;

		protected DebuggerValueRawNode(DbgValueNodeReader reader) => this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

		public override RawNode CreateChild(uint index) => new ChildDbgValueRawNode(this, index, reader);
		public override void Format(IDbgValueNodeFormatParameters options) => DebuggerValueNode.Format(options);
		public override void FormatName(ITextColorWriter output) => DebuggerValueNode.FormatName(output);
		public override void FormatValue(ITextColorWriter output, DbgValueFormatterOptions options) => DebuggerValueNode.FormatValue(output, options);
		public override DbgValueNodeAssignmentResult Assign(string expression, DbgEvaluationOptions options) => DebuggerValueNode.Assign(expression, options);
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
		internal DebuggerValueRawNode Parent => parent;
		internal uint DbgValueNodeChildIndex { get; }

		internal bool HasDebuggerValueNode => __dbgValueNode_DONT_USE != null;
		internal override DbgValueNode DebuggerValueNode {
			get {
				var dbgNode = __dbgValueNode_DONT_USE;
				if (dbgNode == null)
					__dbgValueNode_DONT_USE = dbgNode = reader.GetDebuggerNode(this);
				return dbgNode;
			}
		}
		DbgValueNode __dbgValueNode_DONT_USE;

		DebuggerValueRawNode parent;

		public ChildDbgValueRawNode(DebuggerValueRawNode parent, uint dbgValueNodeChildIndex, DbgValueNodeReader reader)
			: base(reader) {
			this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
			DbgValueNodeChildIndex = dbgValueNodeChildIndex;
		}

		public ChildDbgValueRawNode(DebuggerValueRawNode parent, uint dbgValueNodeChildIndex, DbgValueNodeReader reader, DbgValueNode value)
			: base(reader) {
			this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
			DbgValueNodeChildIndex = dbgValueNodeChildIndex;
			__dbgValueNode_DONT_USE = value ?? throw new ArgumentNullException(nameof(value));
		}

		internal void SetParent(DebuggerValueRawNode parent, DbgValueNode newValue) {
			this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
			__dbgValueNode_DONT_USE = newValue;
		}
	}
}
