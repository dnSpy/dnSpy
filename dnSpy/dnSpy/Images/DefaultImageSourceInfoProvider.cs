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
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Images;

namespace dnSpy.Images {
	sealed class DefaultImageSourceInfoProvider : IImageSourceInfoProvider {
		readonly Assembly assembly;
		Dictionary<string, ImageSourceInfo[]>? nameToInfosDict;

		public DefaultImageSourceInfoProvider(Assembly assembly) => this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));

		public ImageSourceInfo[]? GetImageSourceInfos(string name) {
			if (nameToInfosDict is null)
				InitializeResources();
			Debug2.Assert(!(nameToInfosDict is null));
			if (nameToInfosDict.TryGetValue(name, out var infos))
				return infos;
			return null;
		}

		void InitializeResources() {
			if (!(nameToInfosDict is null))
				return;
			var dict = new Dictionary<string, List<ImageSourceInfo>>(StringComparer.InvariantCultureIgnoreCase);

			var asmName = assembly.GetName();
			var rsrcName = asmName.Name + ".g.resources";
			try {
				var baseUri = "/" + asmName.Name + ";v" + asmName.Version + ";component/";
				var options = new ModuleCreationOptions();
				options.TryToLoadPdbFromDisk = false;
				using (var mod = ModuleDefMD.Load(assembly.ManifestModule, options)) {
					var rsrc = mod.Resources.Find(rsrcName) as EmbeddedResource;
					Debug2.Assert(!(rsrc is null));
					if (!(rsrc is null)) {
						var set = ResourceReader.Read(mod, rsrc.CreateReader());
						foreach (var elem in set.ResourceElements) {
							const string imagesPrefix = "images/";
							if (!(elem.Name is null) && elem.Name.StartsWith(imagesPrefix, StringComparison.OrdinalIgnoreCase)) {
								var imageName = elem.Name.Substring(imagesPrefix.Length);
								var nameNoExt = RemoveExtension(imageName);
								string? nameKey = null;
								ImageSourceInfo? info = null;
								if (imageName.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) || imageName.EndsWith(".baml", StringComparison.OrdinalIgnoreCase)) {
									nameKey = nameNoExt;
									info = new ImageSourceInfo {
										Uri = baseUri + RemoveExtension(elem.Name) + ".xaml",
										Size = ImageSourceInfo.AnySize,
									};
								}
								else if (imageName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) {
									info = new ImageSourceInfo {
										Uri = baseUri + elem.Name,
										Size = GetImageSize(nameNoExt, out nameKey) ?? new Size(16, 16),
									};
								}
								if (!(info is null) && !(nameKey is null)) {
									if (!dict.TryGetValue(nameKey, out var list))
										dict.Add(nameKey, list = new List<ImageSourceInfo>());
									list.Add(info.Value);
								}
							}
						}
					}
				}
			}
			catch {
				Debug.Fail($"Failed to load resources from {assembly}");
			}

			var dict2 = new Dictionary<string, ImageSourceInfo[]>(dict.Count, StringComparer.InvariantCultureIgnoreCase);
			foreach (var kv in dict)
				dict2[kv.Key] = kv.Value.ToArray();
			nameToInfosDict = dict2;
		}

		static string RemoveExtension(string name) {
			int index = name.LastIndexOf('.');
			return index < 0 ? name : name.Substring(0, index);
		}

		static Size? GetImageSize(string name, out string nameKey) {
			nameKey = name;
			var match = sizeRegex.Match(name);
			if (!match.Success)
				return null;
			if (match.Groups.Count != 3)
				return null;
			if (!int.TryParse(match.Groups[1].Value, out int width))
				return null;
			if (!int.TryParse(match.Groups[2].Value, out int height))
				return null;
			nameKey = name.Substring(0, match.Index);
			return new Size(width, height);
		}
		static readonly Regex sizeRegex = new Regex(@"\.(\d+)\.(\d+)$");
	}
}
