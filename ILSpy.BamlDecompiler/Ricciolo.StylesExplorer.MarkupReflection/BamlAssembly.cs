// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	public class BamlAssembly : MarshalByRefObject
	{
		private readonly string _filePath;
		private Assembly _assembly;
		private BamlFileList _bamlFile;

		public BamlAssembly(Assembly assembly)
		{
			_assembly = assembly;
			_filePath = assembly.CodeBase;

			ReadBaml();
		}

		public BamlAssembly(string filePath)
		{
			this._filePath = Path.GetFullPath(filePath);
			this._assembly = Assembly.LoadFile(this.FilePath);
			if (String.Compare(this.Assembly.CodeBase, this.FilePath, true) != 0)
				throw new ArgumentException("Cannot load filePath because Assembly is already loaded", "filePath");

			ReadBaml();
		}

		private void ReadBaml()
		{
			// Get available names
			string[] resources = this.Assembly.GetManifestResourceNames();
			foreach (string res in resources)
			{
				// Solo le risorse
				if (String.Compare(Path.GetExtension(res), ".resources", true) != 0) continue;

				// Get stream
				using (Stream stream = this.Assembly.GetManifestResourceStream(res))
				{
					try
					{
						ResourceReader reader = new ResourceReader(stream);
						foreach (DictionaryEntry entry in reader)
						{
							if (String.Compare(Path.GetExtension(entry.Key.ToString()), ".baml", true) == 0 && entry.Value is Stream)
							{
								BamlFile bm = new BamlFile(GetAssemblyResourceUri(entry.Key.ToString()), (Stream)entry.Value);
								this.BamlFiles.Add(bm);
							}
						}
					}
					catch (ArgumentException)
					{}
				}
			}
		}

		private Uri GetAssemblyResourceUri(string resourceName)
		{
			AssemblyName asm = this.Assembly.GetName();
			byte[] data = asm.GetPublicKeyToken();
			StringBuilder token = new StringBuilder(data.Length * 2);
			for (int x = 0; x < data.Length; x++)
			{
				token.Append(data[x].ToString("x", System.Globalization.CultureInfo.InvariantCulture));
			}

			return new Uri(String.Format(@"{0};V{1};{2};component\{3}", asm.Name, asm.Version, token, Path.ChangeExtension(resourceName, ".xaml")), UriKind.RelativeOrAbsolute);
		}

		public string FilePath
		{
			get { return _filePath; }
		}

		public Assembly Assembly
		{
			get { return _assembly; }
		}

		public BamlFileList BamlFiles
		{
			get
			{
				if (_bamlFile == null)
					_bamlFile = new BamlFileList();
				return _bamlFile;
			}
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}

	[Serializable()]
	public class BamlFileList : Collection<BamlFile>
	{}

}
