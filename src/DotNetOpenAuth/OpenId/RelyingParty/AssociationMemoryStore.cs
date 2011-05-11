//-----------------------------------------------------------------------
// <copyright file="AssociationMemoryStore.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.OpenId.RelyingParty {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Manages a set of associations in memory only (no database).
	/// </summary>
	/// <remarks>
	/// This class should be used for low-to-medium traffic relying party sites that can afford to lose associations
	/// if the app pool was ever restarted.  High traffic relying parties and providers should write their own
	/// implementation of <see cref="IRelyingPartyAssociationStore"/> that works against their own database schema
	/// to allow for persistance and recall of associations across servers in a web farm and server restarts.
	/// </remarks>
	internal class AssociationMemoryStore : IRelyingPartyAssociationStore {
		/// <summary>
		/// How many association store requests should occur between each spring cleaning.
		/// </summary>
		private const int PeriodicCleaningFrequency = 10;

		/// <summary>
		/// For Relying Parties, this maps OP Endpoints to a set of associations with that endpoint.
		/// For Providers, this keeps smart and dumb associations in two distinct pools.
		/// </summary>
		private Dictionary<Uri, Associations> serverAssocsTable = new Dictionary<Uri, Associations>();

		/// <summary>
		/// A counter to track how close we are to an expired association cleaning run.
		/// </summary>
		private int periodicCleaning;

		/// <summary>
		/// Stores a given association for later recall.
		/// </summary>
		/// <param name="providerEndpoint">The OP Endpoint with which the association is established.</param>
		/// <param name="association">The association to store.</param>
		public void StoreAssociation(Uri providerEndpoint, Association association) {
			lock (this) {
				if (!this.serverAssocsTable.ContainsKey(providerEndpoint)) {
					this.serverAssocsTable.Add(providerEndpoint, new Associations());
				}
				Associations server_assocs = this.serverAssocsTable[providerEndpoint];

				server_assocs.Set(association);

				unchecked {
					this.periodicCleaning++;
				}
				if (this.periodicCleaning % PeriodicCleaningFrequency == 0) {
					this.ClearExpiredAssociations();
				}
			}
		}

		/// <summary>
		/// Gets the best association (the one with the longest remaining life) for a given key.
		/// </summary>
		/// <param name="providerEndpoint">The Uri (for relying parties) or Smart/Dumb (for Providers).</param>
		/// <param name="securitySettings">The security settings.</param>
		/// <returns>
		/// The requested association, or null if no unexpired <see cref="Association"/>s exist for the given key.
		/// </returns>
		public Association GetAssociation(Uri providerEndpoint, SecuritySettings securitySettings) {
			lock (this) {
				return this.GetServerAssociations(providerEndpoint).Best.FirstOrDefault(assoc => securitySettings.IsAssociationInPermittedRange(assoc));
			}
		}

		/// <summary>
		/// Gets the association for a given key and handle.
		/// </summary>
		/// <param name="providerEndpoint">The Uri (for relying parties) or Smart/Dumb (for Providers).</param>
		/// <param name="handle">The handle of the specific association that must be recalled.</param>
		/// <returns>
		/// The requested association, or null if no unexpired <see cref="Association"/>s exist for the given key and handle.
		/// </returns>
		public Association GetAssociation(Uri providerEndpoint, string handle) {
			lock (this) {
				return this.GetServerAssociations(providerEndpoint).Get(handle);
			}
		}

		/// <summary>
		/// Removes a specified handle that may exist in the store.
		/// </summary>
		/// <param name="providerEndpoint">The Uri (for relying parties) or Smart/Dumb (for Providers).</param>
		/// <param name="handle">The handle of the specific association that must be deleted.</param>
		/// <returns>
		/// True if the association existed in this store previous to this call.
		/// </returns>
		/// <remarks>
		/// No exception should be thrown if the association does not exist in the store
		/// before this call.
		/// </remarks>
		public bool RemoveAssociation(Uri providerEndpoint, string handle) {
			lock (this) {
				return this.GetServerAssociations(providerEndpoint).Remove(handle);
			}
		}

		/// <summary>
		/// Gets the server associations for a given OP Endpoint or dumb/smart mode.
		/// </summary>
		/// <param name="providerEndpoint">The OP Endpoint with which the association is established.</param>
		/// <returns>The collection of associations that fit the <paramref name="providerEndpoint"/>.</returns>
		internal Associations GetServerAssociations(Uri providerEndpoint) {
			lock (this) {
				if (!this.serverAssocsTable.ContainsKey(providerEndpoint)) {
					this.serverAssocsTable.Add(providerEndpoint, new Associations());
				}

				return this.serverAssocsTable[providerEndpoint];
			}
		}

		/// <summary>
		/// Clears all expired associations from the store.
		/// </summary>
		private void ClearExpiredAssociations() {
			lock (this) {
				foreach (Associations assocs in this.serverAssocsTable.Values) {
					assocs.ClearExpired();
				}
			}
		}
	}
}
