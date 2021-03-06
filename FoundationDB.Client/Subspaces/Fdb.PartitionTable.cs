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

namespace FoundationDB.Client
{
	using FoundationDB.Client.Native;
	using FoundationDB.Client.Utils;
	using FoundationDB.Layers.Directories;
	using FoundationDB.Layers.Tuples;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;

	public static partial class Fdb
	{

		public static class PartitionTable
		{
			internal const string PartitionLayerId = "partition";

			/// <summary>Open the root partition of the default cluster</summary>
			/// <param name="cancellationToken"></param>
			/// <returns></returns>
			public static Task<FdbDatabasePartition> OpenRootAsync(CancellationToken cancellationToken = default(CancellationToken))
			{
				return OpenPartitionAsync(clusterFile: null, dbName: null, globalSpace: FdbSubspace.Empty, cancellationToken: cancellationToken);
			}

			/// <summary>Open the root partition of a cluster</summary>
			public static Task<FdbDatabasePartition> OpenRootAsync(string clusterFile, string dbName, CancellationToken cancellationToken = default(CancellationToken))
			{
				return OpenPartitionAsync(clusterFile: clusterFile, dbName: dbName, globalSpace: FdbSubspace.Empty, cancellationToken: cancellationToken);
			}

			/// <summary>Open a specific partition of the default cluster</summary>
			/// <param name="globalSpace"></param>
			/// <param name="cancellationToken"></param>
			/// <returns></returns>
			public static Task<FdbDatabasePartition> OpenPartitionAsync(FdbSubspace globalSpace, CancellationToken cancellationToken = default(CancellationToken))
			{
				return OpenPartitionAsync(clusterFile: null, dbName: null, globalSpace: globalSpace, cancellationToken: cancellationToken);
			}

			/// <summary>Open a specific partition of a cluster</summary>
			/// <param name="clusterFile"></param>
			/// <param name="dbName"></param>
			/// <param name="globalSpace"></param>
			/// <param name="cancellationToken"></param>
			/// <returns></returns>
			public static async Task<FdbDatabasePartition> OpenPartitionAsync(string clusterFile, string dbName, FdbSubspace globalSpace, CancellationToken cancellationToken = default(CancellationToken))
			{
				FdbDatabase db = null;
				try
				{
					db = await Fdb.OpenAsync(clusterFile, dbName, globalSpace).ConfigureAwait(false);
					return new FdbDatabasePartition(db, nodes: null, contents: null, ownsDatabase: true);
				}
				catch(Exception)
				{
					if (db != null) db.Dispose();
					throw;
				}
			}

			/// <summary>Open a named partition of the default cluster, using the root DirectoryLayer to discover the partition's prefix</summary>
			/// <param name="partitionPath"></param>
			/// <param name="cancellationToken"></param>
			/// <returns></returns>
			public static Task<FdbDatabasePartition> OpenNamedPartitionAsync(IFdbTuple partitionPath, CancellationToken cancellationToken = default(CancellationToken))
			{
				return OpenNamedPartitionAsync(clusterFile: null, dbName: null, partitionPath: partitionPath, cancellationToken: cancellationToken);
			}

			/// <summary>Open a named partition of a cluster, using its root DirectoryLayer to discover the partition's prefix</summary>
			/// <param name="clusterFile"></param>
			/// <param name="dbName"></param>
			/// <param name="partitionPath"></param>
			/// <param name="cancellationToken"></param>
			/// <returns></returns>
			public static async Task<FdbDatabasePartition> OpenNamedPartitionAsync(string clusterFile, string dbName, IFdbTuple partitionPath, CancellationToken cancellationToken = default(CancellationToken))
			{
				if (partitionPath == null) throw new ArgumentNullException("partitionPath");
				if (partitionPath.Count == 0) throw new ArgumentException("The path to the named partition cannot be empty", "partionPath");

				// looks at the global partition table for the specified named partition

				// By convention, all named databases will be under the "/Databases" folder

				FdbDatabase db = null;
				FdbSubspace rootSpace = FdbSubspace.Empty;
				try
				{
					db = await Fdb.OpenAsync(clusterFile, dbName, rootSpace).ConfigureAwait(false);
					var rootLayer = new FdbDirectoryLayer(rootSpace[FdbKey.Directory], rootSpace);
					if (Logging.On) Logging.Verbose(typeof(Fdb.PartitionTable), "OpenNamedPartitionAsync", String.Format("Opened root layer of database {0} using cluster file '{1}'", db.Name, db.Cluster.Path));

					// look up in the root layer for the named partition
					var descriptor = await rootLayer.CreateOrOpenAsync(db, partitionPath, layer: PartitionLayerId).ConfigureAwait(false);
					if (Logging.On) Logging.Verbose(typeof(Fdb.PartitionTable), "OpenNamedPartitionAsync", String.Format("Found named partition '{0}' at prefix {1}", descriptor.Path.ToString(), descriptor.ToString()));

					// switch the global space of the database to the new prefix
					// note: we make sure to copy the descriptor to be isolated from any changes to the key slices by the caller.
					db.ChangeGlobalSpace(descriptor.Copy());

					var partition = new FdbDatabasePartition(db, db.GlobalSpace[FdbKey.Directory], db.GlobalSpace, ownsDatabase: true);
					if (Logging.On) Logging.Info(typeof(Fdb.PartitionTable), "OpenNamedPartitionAsync", String.Format("Opened partition {0} at {1}, using directory layer at {2}", descriptor.Path.ToString(), db.GlobalSpace.ToString(), partition.Root.NodeSubspace.ToString()));

					return partition;
				}
				catch(Exception e)
				{
					if (db != null) db.Dispose();
					if (Logging.On) Logging.Exception(typeof(Fdb.PartitionTable), "OpenNamedPartitionAsync", e);
					throw;
				}
			}

		}

	}

}
