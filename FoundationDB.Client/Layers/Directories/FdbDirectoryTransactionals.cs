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
	using FoundationDB.Layers.Tuples;
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	public static class FdbDirectoryTransactionals
	{

		#region CreateOrOpen...

		/// <summary>Opens the directory with the given path.
		/// If the directory does not exist, it is created (creating parent directories if necessary).
		/// If prefix is specified, the directory is created with the given physical prefix; otherwise a prefix is allocated automatically.
		/// If layer is specified, it is checked against the layer of an existing directory or set as the layer of a new directory.
		/// </summary>
		public static Task<FdbDirectorySubspace> CreateOrOpenAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, IFdbTuple path, string layer = null, Slice prefix = default(Slice), bool allowCreate = true, bool allowOpen = true, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => directory.CreateOrOpenAsync(tr, path, layer, prefix, allowCreate, allowOpen), cancellationToken);
		}

		/// <summary>Opens the directory with the given path.
		/// If the directory does not exist, it is created (creating parent directories if necessary).
		/// If prefix is specified, the directory is created with the given physical prefix; otherwise a prefix is allocated automatically.
		/// If layer is specified, it is checked against the layer of an existing directory or set as the layer of a new directory.
		/// </summary>
		public static Task<FdbDirectorySubspace> CreateOrOpenAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, string[] path, string layer = null, Slice prefix = default(Slice), bool allowCreate = true, bool allowOpen = true, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => directory.CreateOrOpenAsync(tr, path, layer, prefix, allowCreate, allowOpen), cancellationToken);
		}

		/// <summary>Opens a subdirectory with the given path.
		/// If the subdirectory does not exist, it is created (creating intermediate subdirectories if necessary).
		/// If prefix is specified, the subdirectory is created with the given physical prefix; otherwise a prefix is allocated automatically.
		/// If layer is specified, it is checked against the layer of an existing subdirectory or set as the layer of a new subdirectory.
		/// </summary>
		public static Task<FdbDirectorySubspace> CreateOrOpenAsync(this FdbDirectorySubspace subspace, IFdbTransactional dbOrTrans, IFdbTuple path, string layer = null, Slice prefix = default(Slice), bool allowCreate = true, bool allowOpen = true, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => subspace.CreateOrOpenAsync(tr, path, layer, prefix, allowCreate, allowOpen), cancellationToken);
		}

		/// <summary>Opens a subdirectory with the given path.
		/// If the subdirectory does not exist, it is created (creating intermediate subdirectories if necessary).
		/// If prefix is specified, the subdirectory is created with the given physical prefix; otherwise a prefix is allocated automatically.
		/// If layer is specified, it is checked against the layer of an existing subdirectory or set as the layer of a new subdirectory.
		/// </summary>
		public static Task<FdbDirectorySubspace> CreateOrOpenAsync(this FdbDirectorySubspace subspace, IFdbTransactional dbOrTrans, string[] path, string layer = null, Slice prefix = default(Slice), bool allowCreate = true, bool allowOpen = true, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => subspace.CreateOrOpenAsync(tr, path, layer, prefix, allowCreate, allowOpen), cancellationToken);
		}

		#endregion

		#region Create...

		/// <summary>Creates a directory with the given <paramref name="path"/> (creating parent directories if necessary).
		/// An error is raised if the given directory already exists.
		/// If <paramref name="prefix"/> is specified, the directory is created with the given physical prefix; otherwise a prefix is allocated automatically.
		/// If <paramref name="layer"/> is specified, it is recorded with the directory and will be checked by future calls to open.
		/// </summary>
		public static Task<FdbDirectorySubspace> CreateAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, IFdbTuple path, string layer = null, Slice prefix = default(Slice), CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => directory.CreateAsync(tr, path, layer, prefix), cancellationToken);
		}

		/// <summary>Creates a directory with the given <paramref name="path"/> (creating parent directories if necessary).
		/// An error is raised if the given directory already exists.
		/// If <paramref name="prefix"/> is specified, the directory is created with the given physical prefix; otherwise a prefix is allocated automatically.
		/// If <paramref name="layer"/> is specified, it is recorded with the directory and will be checked by future calls to open.
		/// </summary>
		public static Task<FdbDirectorySubspace> CreateAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, string[] path, string layer = null, Slice prefix = default(Slice), CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => directory.CreateAsync(tr, path, layer, prefix), cancellationToken);
		}

		/// <summary>Creates a subdirectory with the given <paramref name="path"/> (creating intermediate subdirectories if necessary).
		/// An exception is thrown if the given subdirectory already exists.
		/// </summary>
		public static Task<FdbDirectorySubspace> CreateAsync(this FdbDirectorySubspace subspace, IFdbTransactional dbOrTrans, IFdbTuple path, string layer = null, Slice prefix = default(Slice), CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => subspace.CreateAsync(tr, path, layer, prefix), cancellationToken);
		}

		/// <summary>Creates a subdirectory with the given <paramref name="path"/> (creating intermediate subdirectories if necessary).
		/// An exception is thrown if the given subdirectory already exists.
		/// </summary>
		public static Task<FdbDirectorySubspace> CreateAsync(this FdbDirectorySubspace subspace, IFdbTransactional dbOrTrans, string[] path, string layer = null, Slice prefix = default(Slice), CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => subspace.CreateAsync(tr, path, layer, prefix), cancellationToken);
		}

		#endregion

		#region Open...

		/// <summary>Opens the directory with the given <paramref name="path"/>.
		/// An error is raised if the directory does not exist, or if a layer is specified and a different layer was specified when the directory was created.
		/// </summary>
		public static Task<FdbDirectorySubspace> OpenAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, IFdbTuple path, string layer = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			// note: we will not write to the transaction
			return dbOrTrans.ReadWriteAsync((tr) => directory.OpenAsync(tr, path, layer), cancellationToken);
		}

		/// <summary>Opens the directory with the given <paramref name="path"/>.
		/// An error is raised if the directory does not exist, or if a layer is specified and a different layer was specified when the directory was created.
		/// </summary>
		public static Task<FdbDirectorySubspace> OpenAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, string[] path, string layer = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			// note: we will not write to the transaction
			return dbOrTrans.ReadWriteAsync((tr) => directory.OpenAsync(tr, path, layer), cancellationToken);
		}

		/// <summary>Opens a subdirectory with the given <paramref name="path"/>.
		/// An exception is thrown if the directory does not exist, or if a layer is specified and a different layer was specified when the directory was created.
		/// </summary>
		public static Task<FdbDirectorySubspace> OpenAsync(this FdbDirectorySubspace subspace, IFdbTransactional dbOrTrans, IFdbTuple path, string layer = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			// note: this should only read from the transaction
			return dbOrTrans.ReadWriteAsync((tr) => subspace.OpenAsync(tr, path, layer), cancellationToken);
		}

		/// <summary>Opens a subdirectory with the given <paramref name="path"/>.
		/// An exception is thrown if the directory does not exist, or if a layer is specified and a different layer was specified when the directory was created.
		/// </summary>
		public static Task<FdbDirectorySubspace> OpenAsync(this FdbDirectorySubspace subspace, IFdbTransactional dbOrTrans, string[] path, string layer = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			// note: this should only read from the transaction
			return dbOrTrans.ReadWriteAsync((tr) => subspace.OpenAsync(tr, path, layer), cancellationToken);
		}

		#endregion

		#region Move...

		/// <summary>Moves the directory found at <paramref name="oldPath"/> to <paramref name="newPath"/>.
		/// There is no effect on the physical prefix of the given directory, or on clients that already have the directory open.
		/// An error is raised if the old directory does not exist, a directory already exists at `new_path`, or the parent directory of `new_path` does not exist.
		/// </summary>
		public static Task<FdbDirectorySubspace> MoveAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, IFdbTuple oldPath, IFdbTuple newPath, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => directory.MoveAsync(tr, oldPath, newPath), cancellationToken);
		}

		/// <summary>Moves the directory found at <paramref name="oldPath"/> to <paramref name="newPath"/>.
		/// There is no effect on the physical prefix of the given directory, or on clients that already have the directory open.
		/// An error is raised if the old directory does not exist, a directory already exists at `new_path`, or the parent directory of `new_path` does not exist.
		/// </summary>
		public static Task<FdbDirectorySubspace> MoveAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, string[] oldPath, string[] newPath, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => directory.MoveAsync(tr, oldPath, newPath), cancellationToken);
		}

		/// <summary>Moves the current directory to <paramref name="newPath"/>.
		/// There is no effect on the physical prefix of the given directory, or on clients that already have the directory open.
		/// An error is raised if a directory already exists at `new_path`, or if the new path points to a child of the current directory.
		/// </summary>
		public static Task<FdbDirectorySubspace> MoveAsync(this FdbDirectorySubspace subspace, IFdbTransactional dbOrTrans, IFdbTuple newPath, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => subspace.MoveAsync(tr, newPath), cancellationToken);
		}

		/// <summary>Moves the current directory to <paramref name="newPath"/>.
		/// There is no effect on the physical prefix of the given directory, or on clients that already have the directory open.
		/// An error is raised if a directory already exists at `new_path`, or if the new path points to a child of the current directory.
		/// </summary>
		public static Task<FdbDirectorySubspace> MoveAsync(this FdbDirectorySubspace subspace, IFdbTransactional dbOrTrans, string[] newPath, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => subspace.MoveAsync(tr, newPath), cancellationToken);
		}

		#endregion

		#region Remove...

		/// <summary>Removes the directory, its contents, and all subdirectories.
		/// Warning: Clients that have already opened the directory might still insert data into its contents after it is removed.
		/// </summary>
		public static Task<bool> RemoveAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, IFdbTuple path, CancellationToken cancellationToken = default(CancellationToken))
		{
			return dbOrTrans.ReadWriteAsync((tr) => directory.RemoveAsync(tr, path), cancellationToken);
		}

		/// <summary>Removes the directory, its contents, and all subdirectories.
		/// Warning: Clients that have already opened the directory might still insert data into its contents after it is removed.
		/// </summary>
		public static Task<bool> RemoveAsync(this FdbDirectoryLayer directory, IFdbTransactional dbOrTrans, string[] path, CancellationToken cancellationToken = default(CancellationToken))
		{
			return dbOrTrans.ReadWriteAsync((tr) => directory.RemoveAsync(tr, path), cancellationToken);
		}

		/// <summary>Removes the directory, its contents, and all subdirectories.
		/// Warning: Clients that have already opened the directory might still insert data into its contents after it is removed.
		/// </summary>
		public static Task RemoveAsync(this FdbDirectorySubspace subspace, IFdbTransactional dbOrTrans, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadWriteAsync((tr) => subspace.RemoveAsync(tr), cancellationToken);
		}

		#endregion

		#region List...

		/// <summary>Returns the list of subdirectories of directory at <paramref name="path"/></summary>
		public static Task<List<IFdbTuple>> ListAsync(this FdbDirectoryLayer directory, IFdbReadOnlyTransactional dbOrTrans, IFdbTuple path, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadAsync((tr) => directory.ListAsync(tr, path), cancellationToken);
		}

		/// <summary>Returns the list of subdirectories of directory at <paramref name="path"/></summary>
		public static Task<List<IFdbTuple>> ListAsync(this FdbDirectoryLayer directory, IFdbReadOnlyTransactional dbOrTrans, string[] path, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadAsync((tr) => directory.ListAsync(tr, path), cancellationToken);
		}

		/// <summary>Returns the list of all the subdirectories of the current directory.</summary>
		public static Task<List<IFdbTuple>> ListAsync(this FdbDirectorySubspace subspace, IFdbReadOnlyTransactional dbOrTrans, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (dbOrTrans == null) throw new ArgumentNullException("dbOrTrans");
			return dbOrTrans.ReadAsync((tr) => subspace.ListAsync(tr), cancellationToken);
		}

		#endregion

	}

}
