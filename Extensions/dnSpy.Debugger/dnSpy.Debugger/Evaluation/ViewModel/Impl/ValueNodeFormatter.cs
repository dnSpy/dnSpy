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

using System.Diagnostics;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	sealed class ValueNodeFormatter {
		public DbgObjectIdService ObjectIdService { get; set; }
		public DbgLanguage Language { get; set; }

		public void WriteExpander(ITextColorWriter output, ValueNode vm) {
			if (vm.TreeNode.LazyLoading)
				output.Write(BoxedTextColor.Text, "+");
			else if (vm.TreeNode.Children.Count == 0) {
				// VS prints nothing
			}
			else if (vm.TreeNode.IsExpanded)
				output.Write(BoxedTextColor.Text, "-");
			else
				output.Write(BoxedTextColor.Text, "+");
		}

		public void WriteName(ITextColorWriter output, ValueNode vm) => vm.CachedName.WriteTo(output);

		public void WriteValueAndObjectId(ITextColorWriter output, ValueNode vm, out bool textChanged) {
			WriteValue(output, vm, out textChanged);
			WriteObjectId(output, vm);
		}

		public void WriteValue(ITextColorWriter output, ValueNode vm, out bool textChanged) {
			vm.CachedValue.WriteTo(output);
			textChanged = !vm.OldCachedValue.IsDefault && !vm.OldCachedValue.Equals(vm.CachedValue);
		}

		public void WriteObjectId(ITextColorWriter output, ValueNode vm) {
			Debug.Assert(ObjectIdService != null);
			if (ObjectIdService == null)
				return;
			var vmImpl = (ValueNodeImpl)vm;
			if (vmImpl.RawNode is DebuggerValueRawNode rawNode) {
				Debug.Assert(Language != null);
				if (Language == null)
					return;
				var objectId = ObjectIdService.GetObjectId(rawNode.DebuggerValueNode.Value);
				if (objectId != null) {
					output.WriteSpace();
					output.Write(BoxedTextColor.Punctuation, "{");
					var evalContext = vmImpl.Context.EvaluationContext;
					Debug.Assert(evalContext != null);
					if (evalContext == null)
						output.Write(BoxedTextColor.Error, "???");
					else
						Language.ObjectIdFormatter.FormatName(evalContext, output, objectId);
					output.Write(BoxedTextColor.Punctuation, "}");
				}
			}
		}

		public void WriteType(ITextColorWriter output, ValueNode vm) {
			vm.CachedExpectedType.WriteTo(output);
			var cachedActualType = vm.CachedActualType_OrDefaultInstance;
			// If it's default, expected type == actual type
			if (!cachedActualType.IsDefault) {
				output.WriteSpace();
				output.Write(BoxedTextColor.Error, "{");
				cachedActualType.WriteTo(output);
				output.Write(BoxedTextColor.Error, "}");
			}
		}
	}
}
