using Authing.ApiClient.Types;
using System;
using System.Collections.Generic;

namespace Kaiyuanshe.OpenHackathon.Server.Storage.Entities
{
    /// <summary>
    /// Entity for login user.
    /// PK: userId
    /// RK: string.Empty
    /// </summary>
    public class UserEntity : BaseTableEntity
    {
        public string Name { get; set; }

        public string GivenName { get; set; }

        public string FamilyName { get; set; }

        public string MiddleName { get; set; }

        public string Profile { get; set; }

        public string PreferredUsername { get; set; }

        public string Website { get; set; }

        public string Gender { get; set; }

        public string Birthdate { get; set; }

        public string Zoneinfo { get; set; }

        public string Company { get; set; }

        public string Locale { get; set; }

        public string Formatted { get; set; }

        public string StreetAddress { get; set; }

        public string Locality { get; set; }

        public string Region { get; set; }

        public string PostalCode { get; set; }

        public string City { get; set; }

        public string Province { get; set; }

        public string Country { get; set; }

        public string Address { get; set; }

        public string Browser { get; set; }

        public string Device { get; set; }

        public bool IsDeleted { get; set; }

        [IgnoreEntityProperty]
        public string UserId
        {
            get
            {
                return PartitionKey;
            }
        }

        public string Arn { get; set; }

        public string UserPoolId { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public bool EmailVerified { get; set; }

        public string Phone { get; set; }

        public bool PhoneVerified { get; set; }

        public string Unionid { get; set; }

        public string OpenId { get; set; }

        [ConvertableEntityProperty]
        public IEnumerable<Identity> Identities { get; set; }

        public string Nickname { get; set; }

        [ConvertableEntityProperty]
        public IEnumerable<string> RegisterSource { get; set; }

        public string Photo { get; set; }

        public string Password { get; set; }

        public string OAuth { get; set; }

        public string Token { get; set; }

        public DateTime TokenExpiredAt { get; set; }

        public int LoginsCount { get; set; }

        public string LastLogin { get; set; }

        public string LastIp { get; set; }

        public string SignedUp { get; set; }

        public bool Blocked { get; set; }
    }
}
