﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using System;
using System.ComponentModel;

namespace Azure.Storage.Sas
{
    // <summary>
    /// <see cref="AccountSasBuilder"/> is used to generate an account level
    /// Shared Access Signature (SAS) for Azure Storage services.
    /// For more information, see
    /// <see href="https://docs.microsoft.com/rest/api/storageservices/constructing-an-account-sas" />.
    /// </summary>
    public struct AccountSasBuilder : IEquatable<AccountSasBuilder>
    {
        /// <summary>
        /// The storage service version to use to authenticate requests made 
        /// with this shared access signature, and the service version to use 
        /// when handling requests made with this shared access signature.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The optional signed protocol field specifies the protocol
        /// permitted for a request made with the SAS.  Possible values are
        /// <see cref="SasProtocol.HttpsAndHttp"/>,
        /// <see cref="SasProtocol.Https"/>, and
        /// <see cref="SasProtocol.None"/>.
        /// </summary>
        public SasProtocol Protocol { get; set; }

        /// <summary>
        /// Optionally specify the time at which the shared access signature
        /// becomes valid.  If omitted when DateTimeOffset.MinValue is used,
        /// start time for this call is assumed to be the time when the
        /// storage service receives the request.
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// The time at which the shared access signature becomes invalid.
        /// This field must be omitted if it has been specified in an 
        /// associated stored access policy.
        /// </summary>
        public DateTimeOffset ExpiryTime { get; set; }

        /// <summary>
        /// The permissions associated with the shared access signature. The
        /// user is restricted to operations allowed by the permissions. The
        /// <see cref="AccountSasPermissions"/> type can be used to create the
        /// permissions string.
        /// </summary>
        public string Permissions { get; set; }

        /// <summary>
        /// Specifies an IP address or a range of IP addresses from which to
        /// accept requests. If the IP address from which the request 
        /// originates does not match the IP address or address range 
        /// specified on the SAS token, the request is not authenticated.
        /// When specifying a range of IP addresses, note that the range is 
        /// inclusive.
        /// </summary>
        public IPRange IPRange { get; set; }

        /// <summary>
        /// The services associated with the shared access signature. The
        /// user is restricted to operations with the specified services. The
        /// <see cref="AccountSasServices"/> type can be used to create the
        /// services string.
        /// </summary>
        public string Services { get; set; }

        /// <summary>
        /// The resource types associated with the shared access signature. The
        /// user is restricted to operations on the specified resources. The
        /// <see cref="AccountSasResourceTypes"/> type can be used to create
        /// the resource types string.
        /// </summary>
        public string ResourceTypes { get; set; }

        /// <summary>
        /// Use an account's <see cref="StorageSharedKeyCredential"/> to sign this 
        /// shared access signature values to produce the proper SAS query
        /// parameters for authenticating requests.
        /// </summary>
        /// <param name="sharedKeyCredential">
        /// The storage account's <see cref="StorageSharedKeyCredential"/>.
        /// </param>
        /// <returns>
        /// The <see cref="SasQueryParameters"/> used for authenticating
        /// requests.
        /// </returns>
        public SasQueryParameters ToSasQueryParameters(StorageSharedKeyCredential sharedKeyCredential)
        {
            // https://docs.microsoft.com/en-us/rest/api/storageservices/Constructing-an-Account-SAS
            sharedKeyCredential = sharedKeyCredential ?? throw Errors.ArgumentNull(nameof(sharedKeyCredential));

            if (this.ExpiryTime == default || String.IsNullOrEmpty(this.Permissions) || String.IsNullOrEmpty(this.ResourceTypes) || String.IsNullOrEmpty(this.Services))
            {
                throw Errors.AccountSasMissingData();
            }
            if (String.IsNullOrEmpty(this.Version))
            {
                this.Version = SasQueryParameters.DefaultSasVersion;
            }
            // Make sure the permission characters are in the correct order
            this.Permissions = AccountSasPermissions.Parse(this.Permissions).ToString();
            var startTime = SasQueryParameters.FormatTimesForSasSigning(this.StartTime);
            var expiryTime = SasQueryParameters.FormatTimesForSasSigning(this.ExpiryTime);

            // String to sign: http://msdn.microsoft.com/en-us/library/azure/dn140255.aspx
            var stringToSign = String.Join("\n",
                sharedKeyCredential.AccountName,
                this.Permissions,
                this.Services,
                this.ResourceTypes,
                startTime,
                expiryTime,
                this.IPRange.ToString(),
                this.Protocol.ToString(),
                this.Version,
                "");  // That's right, the account SAS requires a terminating extra newline

            var signature = sharedKeyCredential.ComputeHMACSHA256(stringToSign);
            var p = new SasQueryParameters(
                this.Version,
                this.Services,
                this.ResourceTypes,
                this.Protocol,
                this.StartTime,
                this.ExpiryTime,
                this.IPRange,
                null, // Identifier
                null, // Resource
                this.Permissions,
                signature);
            return p;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() =>
            base.ToString();

        /// <summary>
        /// Check if two <see cref="AccountSasBuilder"/> instances are equal.
        /// </summary>
        /// <param name="obj">The instance to compare to.</param>
        /// <returns>True if they're equal, false otherwise.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) =>
            obj is AccountSasBuilder other &&
            this.Equals(other);

        /// <summary>
        /// Get a hash code for the <see cref="AccountSasBuilder"/>.
        /// </summary>
        /// <returns>Hash code for the <see cref="AccountSasBuilder"/>.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() =>
            this.ExpiryTime.GetHashCode() ^
            this.IPRange.GetHashCode() ^
            (this.Permissions?.GetHashCode() ?? 0) ^
            this.Protocol.GetHashCode() ^
            (this.ResourceTypes?.GetHashCode() ?? 0) ^
            (this.Services?.GetHashCode() ?? 0) ^
            this.StartTime.GetHashCode() ^
            (this.Version?.GetHashCode() ?? 0);

        /// <summary>
        /// Check if two <see cref="AccountSasBuilder"/> instances are equal.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns>True if they're equal, false otherwise.</returns>
        public static bool operator ==(AccountSasBuilder left, AccountSasBuilder right) =>
            left.Equals(right);

        /// <summary>
        /// Check if two <see cref="AccountSasBuilder"/> instances are not
        /// equal.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns>True if they're not equal, false otherwise.</returns>
        public static bool operator !=(AccountSasBuilder left, AccountSasBuilder right) =>
            !(left == right);

        /// <summary>
        /// Check if two <see cref="AccountSasBuilder"/> instances are equal.
        /// </summary>
        /// <param name="other">The instance to compare to.</param>
        /// <returns>True if they're equal, false otherwise.</returns>
        public bool Equals(AccountSasBuilder other) =>
            this.ExpiryTime == other.ExpiryTime &&
            this.IPRange == other.IPRange &&
            this.Permissions == other.Permissions &&
            this.Protocol == other.Protocol &&
            this.ResourceTypes == other.ResourceTypes &&
            this.Services == other.Services &&
            this.StartTime == other.StartTime &&
            this.Version == other.Version;
    }
}
