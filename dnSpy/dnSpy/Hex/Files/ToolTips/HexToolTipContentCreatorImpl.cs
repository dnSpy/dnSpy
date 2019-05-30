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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.ToolTips;
using dnSpy.Contracts.Hex.Text;

namespace dnSpy.Hex.Files.ToolTips {
	[Export(typeof(HexToolTipContentCreatorFactory))]
	sealed class HexToolTipContentCreatorFactoryImpl : HexToolTipContentCreatorFactory {
		readonly HexFieldFormatterFactory hexFieldFormatterFactory;

		[ImportingConstructor]
		HexToolTipContentCreatorFactoryImpl(HexFieldFormatterFactory hexFieldFormatterFactory) => this.hexFieldFormatterFactory = hexFieldFormatterFactory;

		public override HexToolTipContentCreator Create() =>
			new HexToolTipContentCreatorImpl(hexFieldFormatterFactory);
	}

	sealed class HexToolTipContentCreatorImpl : HexToolTipContentCreator {
		public override object? Image { get; set; }

		readonly struct WriterState {
			public HexTextWriterImpl Writer { get; }
			public HexFieldFormatter Formatter { get; }
			public WriterState(HexTextWriterImpl writer, HexFieldFormatter formatter) {
				Writer = writer;
				Formatter = formatter;
			}
		}

		public override HexFieldFormatter Writer => writerStateList[writerStateList.Count - 1].Formatter;

		readonly HexFieldFormatterFactory hexFieldFormatterFactory;
		readonly List<WriterState> writerStateList;

		public HexToolTipContentCreatorImpl(HexFieldFormatterFactory hexFieldFormatterFactory) {
			this.hexFieldFormatterFactory = hexFieldFormatterFactory ?? throw new ArgumentNullException(nameof(hexFieldFormatterFactory));
			writerStateList = new List<WriterState>();
			CreateNewWriter();
		}

		public override HexFieldFormatter CreateNewWriter() {
			var writer = new HexTextWriterImpl();
			var formatter = hexFieldFormatterFactory.Create(writer);
			writerStateList.Add(new WriterState(writer, formatter));
			return formatter;
		}

		public override HexToolTipContent Create() {
			var list = new List<HexClassifiedTextCollection>(writerStateList.Count);
			foreach (var state in writerStateList)
				list.Add(new HexClassifiedTextCollection(state.Writer.AllText.ToArray()));
			return new HexToolTipContent(list.ToArray(), Image);
		}

		sealed class HexTextWriterImpl : HexTextWriter {
			public List<HexClassifiedText> AllText { get; } = new List<HexClassifiedText>();
			public override void Write(string text, string tag) => AllText.Add(new HexClassifiedText(text, tag));
		}
	}
}
