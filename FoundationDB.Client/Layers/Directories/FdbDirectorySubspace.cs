﻿#region BSD Licence
/* Copyright (c) 2013, Doxense SARL
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
	* Redistributions of source code must retain the above copyright
	  notice, this list of conditions and the following disclaimer.
	* Redistributions in binary form must reproduce the above copyright
	  notice, this list of conditions and the following disclaimer in the
	  documentation and/or other materials provided with the distribution.
	* Neither the name of Doxense nor the
	  names of its contributors may be used to endorse or promote products
	  derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

namespace FoundationDB.Layers.Directories
{
	using FoundationDB.Client;
	using FoundationDB.Client.Utils;
	using FoundationDB.Layers.Tuples;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading.Tasks;

	[DebuggerDisplay("Path={Path}, Prefix={Key}, Layer={Layer}")]
	public class FdbDirectorySubspace : FdbSubspace
	{

		internal FdbDirectorySubspace(IFdbTuple path, Slice prefix, FdbDirectoryLayer directoryLayer, string layer)
			: base(prefix)
		{
			Contract.Requires(path != null);
			Contract.Requires(prefix != null);
			Contract.Requires(directoryLayer != null);

			this.Path = path.Memoize();
			this.DirectoryLayer = directoryLayer;
			this.Layer = layer;
		}

		public FdbMemoizedTuple Path { get; private set; }

		public FdbDirectoryLayer DirectoryLayer { get; private set; }

		public string Layer { get; private set; }

		public void CheckLayer(string layer)
		{
			if (!string.IsNullOrEmpty(layer) && !string.IsNullOrEmpty(this.Layer) && layer != this.Layer)
				throw new InvalidOperationException("The directory was created with an incompatible layer.");
		}

		/// <summary>Opens a subdirectory with the given path.
		/// If the subdirectory does not exist, it is created (creating intermediate subdirectories if necessary).
		/// If prefix is specified, the subdirectory is created with the given physical prefix; otherwise a prefix is allocated automatically.
		/// If layer is specified, it is checked against the layer of an existing subdirectory or set as the layer of a new subdirectory.
		/// </summary>
		public Task<FdbDirectorySubspace> CreateOrOpenAsync(IFdbTransaction tr, IFdbTuple subPath, string layer = null, Slice prefix = default(Slice), bool allowCreate = true, bool allowOpen = true)
		{
			return this.DirectoryLayer.CreateOrOpenAsync(tr, this.Path.Concat(subPath), layer, prefix, allowCreate, allowOpen);
		}

		/// <summary>Opens a subdirectory with the given path.
		/// If the subdirectory does not exist, it is created (creating intermediate subdirectories if necessary).
		/// If prefix is specified, the subdirectory is created with the given physical prefix; otherwise a prefix is allocated automatically.
		/// If layer is specified, it is checked against the layer of an existing subdirectory or set as the layer of a new subdirectory.
		/// </summary>
		public Task<FdbDirectorySubspace> CreateOrOpenAsync(IFdbTransaction tr, string[] subPath, string layer = null, Slice prefix = default(Slice), bool allowCreate = true, bool allowOpen = true)
		{
			if (subPath == null) throw new ArgumentNullException("subPath");
			return this.DirectoryLayer.CreateOrOpenAsync(tr, this.Path.Concat(FdbTuple.CreateRange<string>(subPath)), layer, prefix, allowCreate, allowOpen);
		}

		/// <summary>Opens a subdirectory with the given <paramref name="path"/>.
		/// An exception is thrown if the subdirectory does not exist, or if a layer is specified and a different layer was specified when the subdirectory was created.
		/// </summary>
		/// <param name="tr">Transaction to use for the operation</param>
		/// <param name="subPath">Relative path of the subdirectory to create</param>
		public Task<FdbDirectorySubspace> OpenAsync(IFdbTransaction tr, IFdbTuple subPath, string layer = null)
		{
			return this.DirectoryLayer.OpenAsync(tr, this.Path.Concat(subPath), layer);
		}

		/// <summary>Opens a subdirectory with the given <paramref name="path"/>.
		/// An exception is thrown if the subdirectory does not exist, or if a layer is specified and a different layer was specified when the subdirectory was created.
		/// </summary>
		/// <param name="tr">Transaction to use for the operation</param>
		/// <param name="subPath">Relative path of the subdirectory to create</param>
		public Task<FdbDirectorySubspace> OpenAsync(IFdbTransaction tr, string[] subPath, string layer = null)
		{
			return this.DirectoryLayer.OpenAsync(tr, this.Path.Concat(FdbTuple.CreateRange<string>(subPath)), layer);
		}

		/// <summary>Creates a subdirectory with the given <paramref name="path"/> (creating intermediate subdirectories if necessary).
		/// An exception is thrown if the given subdirectory already exists.
		/// </summary>
		/// <param name="tr">Transaction to use for the operation</param>
		/// <param name="subPath">Relative path of the subdirectory to create</param>
		/// <param name="layer">If <paramref name="layer"/> is specified, it is recorded with the subdirectory and will be checked by future calls to open.</param>
		/// <param name="prefix">If <paramref name="prefix"/> is specified, the subdirectory is created with the given physical prefix; otherwise a prefix is allocated automatically.</param>
		public Task<FdbDirectorySubspace> CreateAsync(IFdbTransaction tr, IFdbTuple subPath, string layer = null, Slice prefix = default(Slice))
		{
			return this.DirectoryLayer.CreateAsync(tr, this.Path.Concat(subPath), layer, prefix);
		}

		/// <summary>Creates a subdirectory with the given <paramref name="path"/> (creating intermediate subdirectories if necessary).
		/// An exception is thrown if the given subdirectory already exists.
		/// </summary>
		/// <param name="tr">Transaction to use for the operation</param>
		/// <param name="subPath">Relative path of the subdirectory to create</param>
		/// <param name="layer">If <paramref name="layer"/> is specified, it is recorded with the subdirectory and will be checked by future calls to open.</param>
		/// <param name="prefix">If <paramref name="prefix"/> is specified, the subdirectory is created with the given physical prefix; otherwise a prefix is allocated automatically.</param>
		public Task<FdbDirectorySubspace> CreateAsync(IFdbTransaction tr, string[] subPath, string layer = null, Slice prefix = default(Slice))
		{
			return this.DirectoryLayer.CreateAsync(tr, this.Path.Concat(FdbTuple.CreateRange<string>(subPath)), layer, prefix);
		}

		/// <summary>Moves the current directory to <paramref name="newPath"/>.
		/// There is no effect on the physical prefix of the given directory, or on clients that already have the directory open.
		/// An error is raised if a directory already exists at `new_path`, or if the new path points to a child of the current directory.
		/// </summary>
		/// <param name="newPath">Full path (from the root) where this directory will be moved</param>
		public Task<FdbDirectorySubspace> MoveAsync(IFdbTransaction tr, IFdbTuple newPath)
		{
			return this.DirectoryLayer.MoveAsync(tr, this.Path, newPath);
		}

		/// <summary>Moves the current directory to <paramref name="newPath"/>.
		/// There is no effect on the physical prefix of the given directory, or on clients that already have the directory open.
		/// An error is raised if a directory already exists at `new_path`, or if the new path points to a child of the current directory.
		/// </summary>
		/// <param name="tr">Transaction to use for the operation</param>
		/// <param name="newPath">Full path (from the root) where this directory will be moved</param>
		public Task<FdbDirectorySubspace> MoveAsync(IFdbTransaction tr, string[] newPath)
		{
			return this.DirectoryLayer.MoveAsync(tr, this.Path, FdbTuple.CreateRange<string>(newPath));
		}

		/// <summary>Removes the directory, its contents, and all subdirectories.
		/// Warning: Clients that have already opened the directory might still insert data into its contents after it is removed.
		/// </summary>
		/// <param name="tr">Transaction to use for the operation</param>
		public Task<bool> RemoveAsync(IFdbTransaction tr)
		{
			return this.DirectoryLayer.RemoveAsync(tr, this.Path);
		}

		/// <summary>Returns the list of all the subdirectories of the current directory.</summary>
		public Task<List<IFdbTuple>> ListAsync(IFdbReadOnlyTransaction tr)
		{
			return this.DirectoryLayer.ListAsync(tr, this.Path);
		}

		public override string ToString()
		{
			return "DirectorySubspace(" + this.Path.ToString() + ", " + this.Key.ToString() + ")";
		}

	}
}
