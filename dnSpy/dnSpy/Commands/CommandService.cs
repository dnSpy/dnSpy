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
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Command;

namespace dnSpy.Commands {
	[Export(typeof(ICommandService))]
	sealed class CommandService : ICommandService {
		readonly Lazy<ICommandInfoProvider, ICommandInfoProviderMetadata>[] commandInfoProviders;
		readonly Lazy<ICommandTargetFilterProvider, ICommandTargetFilterProviderMetadata>[] commandTargetFilterProviders;

		[ImportingConstructor]
		CommandService([ImportMany] IEnumerable<Lazy<ICommandInfoProvider, ICommandInfoProviderMetadata>> commandInfoProviders, [ImportMany] IEnumerable<Lazy<ICommandTargetFilterProvider, ICommandTargetFilterProviderMetadata>> commandTargetFilterProviders) {
			this.commandInfoProviders = commandInfoProviders.OrderBy(a => a.Metadata.Order).ToArray();
			this.commandTargetFilterProviders = commandTargetFilterProviders.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public IRegisteredCommandElement Register(UIElement sourceElement, object target) {
			if (sourceElement == null)
				throw new ArgumentNullException(nameof(sourceElement));
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			var coll = new KeyShortcutCollection();
			foreach (var provider in commandInfoProviders)
				coll.Add(provider.Value, target);

			var cmdElem = new RegisteredCommandElement(this, sourceElement, coll, target);
			foreach (var c in commandTargetFilterProviders) {
				var filter = c.Value.Create(target);
				if (filter == null)
					continue;
				cmdElem.AddFilter(filter, c.Metadata.Order);
			}
			return cmdElem;
		}

		public CommandInfo? CreateCommandInfo(object target, string text) {
			foreach (var c in commandInfoProviders) {
				var c2 = c.Value as ICommandInfoProvider2;
				if (c2 == null)
					continue;
				var cmd = c2.CreateFromTextInput(target, text);
				if (cmd != null)
					return cmd;
			}
			return null;
		}
	}
}
