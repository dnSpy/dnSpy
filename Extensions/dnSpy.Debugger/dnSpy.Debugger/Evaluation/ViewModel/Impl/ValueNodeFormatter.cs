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

using System.Diagnostics;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	sealed class ValueNodeFormatter {
		public DbgObjectIdService? ObjectIdService { get; set; }
		public DbgLanguage? Language { get; set; }

		public void WriteExpander(IDbgTextWriter output, ValueNode vm) {
			if (vm.TreeNode.LazyLoading)
				output.Write(DbgTextColor.Text, "+");
			else if (vm.TreeNode.Children.Count == 0) {
				// VS prints nothing
			}
			else if (vm.TreeNode.IsExpanded)
				output.Write(DbgTextColor.Text, "-");
			else
				output.Write(DbgTextColor.Text, "+");
		}

		public void WriteName(IDbgTextWriter output, ValueNode vm) => vm.CachedName.WriteTo(output);

		public void WriteValueAndObjectId(IDbgTextWriter output, ValueNode vm, out bool textChanged) {
			WriteValue(output, vm, out textChanged);
			WriteObjectId(output, vm);
		}

		public void WriteValue(IDbgTextWriter output, ValueNode vm, out bool textChanged) {
			vm.CachedValue.WriteTo(output);
			textChanged = !vm.OldCachedValue.IsDefault && !vm.OldCachedValue.Equals(vm.CachedValue);
		}

		public void WriteObjectId(IDbgTextWriter output, ValueNode vm) {
			Debug2.Assert(ObjectIdService is not null);
			if (ObjectIdService is null)
				return;
			var vmImpl = (ValueNodeImpl)vm;
			if (vmImpl.RawNode is DebuggerValueRawNode rawNode) {
				var language = Language;
				Debug2.Assert(language is not null);
				if (language is null)
					return;
				var value = rawNode.DebuggerValueNode.Value;
				if (value is null)
					return;
				var objectId = ObjectIdService.GetObjectId(value);
				if (objectId is not null) {
					output.Write(DbgTextColor.Text, " ");
					output.Write(DbgTextColor.Punctuation, "{");
					var evalInfo = vmImpl.Context.EvaluationInfo;
					Debug2.Assert(evalInfo is not null);
					if (evalInfo is null)
						output.Write(DbgTextColor.Error, "???");
					else
						language.Formatter.FormatObjectIdName(evalInfo.Context, output, objectId.Id);
					output.Write(DbgTextColor.Punctuation, "}");
				}
			}
		}

		public void WriteType(IDbgTextWriter output, ValueNode vm) {
			vm.CachedExpectedType.WriteTo(output);
			var cachedActualType = vm.CachedActualType_OrDefaultInstance;
			// If it's default, expected type == actual type
			if (!cachedActualType.IsDefault) {
				output.Write(DbgTextColor.Text, " ");
				output.Write(DbgTextColor.Error, "{");
				cachedActualType.WriteTo(output);
				output.Write(DbgTextColor.Error, "}");
			}
		}
	}
}
