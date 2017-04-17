using System;
using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Debugger.Locals {
	class ValueComparer : BaseVMComparer<ValueVM> {
		readonly TextClassifierTextColorWriter writerX = new TextClassifierTextColorWriter();
		readonly TextClassifierTextColorWriter writerY = new TextClassifierTextColorWriter();

		string GetCachedOutputRepresentation(TextClassifierTextColorWriter writer, CachedOutput co) {
			var conv = new OutputConverter(writer);
			foreach (var t in co.data)
				conv.Write(t.Item1, t.Item2);
			return writer.Text;
		}

		protected override int CompareByProperty(ValueVM x, ValueVM y, string propertyName) {
			try {
				switch (propertyName) {
					case nameof(ValueVM.NameObject):
						x.WriteName(writerX);
						y.WriteName(writerY);
						return StringComparer.OrdinalIgnoreCase.Compare(writerX.Text, writerY.Text);

					case nameof(ValueVM.ValueObject):
						var xValue = GetCachedOutputRepresentation(writerX, x.CachedOutputValue);
						var yValue = GetCachedOutputRepresentation(writerY, y.CachedOutputValue);
						return StringComparer.Ordinal.Compare(xValue, yValue);

					case nameof(ValueVM.TypeObject):
						var xType = GetCachedOutputRepresentation(writerX, x.CachedOutputType);
						var yType = GetCachedOutputRepresentation(writerY, y.CachedOutputType);
						return StringComparer.OrdinalIgnoreCase.Compare(xType, yType);
				}
			}
			finally {
				writerX.Clear();
				writerY.Clear();
			}
			return 0;
		}
	}
}
