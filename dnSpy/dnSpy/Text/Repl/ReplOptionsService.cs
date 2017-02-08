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
using dnSpy.Contracts.Settings.Groups;
using dnSpy.Contracts.Settings.Repl;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Repl {
	interface IReplOptionsService {
		IReplOptions[] Options { get; }
	}

	[Export(typeof(IReplOptionsService))]
	sealed class ReplOptionsService : IReplOptionsService {
		public IReplOptions[] Options { get; }

		[ImportingConstructor]
		ReplOptionsService(ITextViewOptionsGroupService textViewOptionsGroupService, IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<ReplOptionsDefinition, IReplOptionsDefinitionMetadata>> replOptionsDefinitions) {
			var group = textViewOptionsGroupService.GetGroup(PredefinedTextViewGroupNames.REPL);
			Options = replOptionsDefinitions.Select(a => ReplOptions.TryCreate(group, contentTypeRegistryService, a.Metadata)).Where(a => a != null).ToArray();
		}
	}
}
