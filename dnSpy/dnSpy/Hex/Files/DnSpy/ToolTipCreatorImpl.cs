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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Hex.Files.DnSpy;
using dnSpy.Contracts.Hex.Files.ToolTips;

namespace dnSpy.Hex.Files.DnSpy {
	[Export(typeof(ToolTipCreatorFactory))]
	sealed class ToolTipCreatorFactoryImpl : ToolTipCreatorFactory {
		readonly ToolTipObjectFactory toolTipObjectFactory;
		readonly HexToolTipContentCreatorFactory hexToolTipContentCreatorFactory;

		[ImportingConstructor]
		ToolTipCreatorFactoryImpl(ToolTipObjectFactory toolTipObjectFactory, HexToolTipContentCreatorFactory hexToolTipContentCreatorFactory) {
			this.toolTipObjectFactory = toolTipObjectFactory;
			this.hexToolTipContentCreatorFactory = hexToolTipContentCreatorFactory;
		}

		public override ToolTipCreator Create() =>
			new ToolTipCreatorImpl(toolTipObjectFactory, hexToolTipContentCreatorFactory.Create());
	}

	sealed class ToolTipCreatorImpl : ToolTipCreator {
		public override HexToolTipContentCreator ToolTipContentCreator { get; }
		readonly ToolTipObjectFactory toolTipObjectFactory;

		public ToolTipCreatorImpl(ToolTipObjectFactory toolTipObjectFactory, HexToolTipContentCreator hexToolTipContentCreator) {
			this.toolTipObjectFactory = toolTipObjectFactory ?? throw new ArgumentNullException(nameof(toolTipObjectFactory));
			ToolTipContentCreator = hexToolTipContentCreator ?? throw new ArgumentNullException(nameof(hexToolTipContentCreator));
		}

		public override object Create() => toolTipObjectFactory.Create(ToolTipContentCreator.Create());
	}
}
