using Newtonsoft.Json;
using static DistributionTools.JsonHelpers;

namespace DistributionTools
{
    public static class OAuth
    {
        [JsonConverter(typeof(ObjectToStringConverter<ClientID>))]
        public struct ClientID
        {
            [JsonProperty]
            private string ID;

            public ClientID(string id)
            {
                ID = id;
            }

            public static explicit operator string(ClientID clientID)
            {
                return clientID.ID;
            }

            public override string ToString()
            {
                return ID;
            }
        }

        [JsonConverter(typeof(ObjectToStringConverter<ClientSecret>))]
        public struct ClientSecret
        {
            [JsonProperty]
            private string Secret;

            public ClientSecret(string secret)
            {
                Secret = secret;
            }

            public static explicit operator string(ClientSecret clientSecret)
            {
                return clientSecret.Secret;
            }

            public override string ToString()
            {
                return Secret;
            }
        }

        [JsonConverter(typeof(ObjectToStringConverter<RefreshToken>))]
        public struct RefreshToken
        {
            [JsonProperty]
            private string Token;

            public RefreshToken(string token)
            {
                Token = token;
            }

            public static explicit operator string(RefreshToken refreshToken)
            {
                return refreshToken.Token;
            }

            public override string ToString()
            {
                return Token;
            }
        }

        [JsonConverter(typeof(ObjectToStringConverter<AccessToken>))]
        public struct AccessToken
        {
            [JsonProperty]
            private string Token;

            public AccessToken(string token)
            {
                Token = token;
            }

            public static explicit operator string(AccessToken accessToken)
            {
                return accessToken.Token;
            }

            public override string ToString()
            {
                return Token;
            }
        }
    }
}
