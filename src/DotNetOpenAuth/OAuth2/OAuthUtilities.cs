﻿//-----------------------------------------------------------------------
// <copyright file="OAuthUtilities.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.OAuth2 {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.Contracts;
	using System.Globalization;
	using System.Linq;
	using System.Net;
	using System.Text;
	using DotNetOpenAuth.Messaging;

	/// <summary>
	/// Some common utility methods for OAuth 2.0.
	/// </summary>
	internal static class OAuthUtilities {
		/// <summary>
		/// Authorizes an HTTP request using an OAuth 2.0 access token in an HTTP Authorization header.
		/// </summary>
		/// <param name="request">The request to authorize.</param>
		/// <param name="accessToken">The access token previously obtained from the Authorization Server.</param>
		internal static void AuthorizeWithOAuthWrap(this HttpWebRequest request, string accessToken) {
			Contract.Requires<ArgumentNullException>(request != null);
			Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(accessToken));
			request.Headers[HttpRequestHeader.Authorization] = string.Format(
				CultureInfo.InvariantCulture,
				Protocol.HttpAuthorizationHeaderFormat,
				Uri.EscapeDataString(accessToken));
		}

		/// <summary>
		/// Gets information about the client with a given identifier.
		/// </summary>
		/// <param name="authorizationServer">The authorization server.</param>
		/// <param name="clientIdentifier">The client identifier.</param>
		/// <returns>The client information.  Never null.</returns>
		internal static IConsumerDescription GetClientOrThrow(this IAuthorizationServer authorizationServer, string clientIdentifier) {
			Contract.Requires<ArgumentException>(!String.IsNullOrEmpty(clientIdentifier));
			Contract.Ensures(Contract.Result<IConsumerDescription>() != null);

			try {
				return authorizationServer.GetClient(clientIdentifier);
			} catch (KeyNotFoundException ex) {
				throw ErrorUtilities.Wrap(ex, OAuth.OAuthStrings.ConsumerOrTokenSecretNotFound);
			} catch (ArgumentException ex) {
				throw ErrorUtilities.Wrap(ex, OAuth.OAuthStrings.ConsumerOrTokenSecretNotFound);
			}
		}
	}
}