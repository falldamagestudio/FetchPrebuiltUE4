using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DistributionTools
{
    public static class GoogleOAuthFlow
    {
        public struct ApplicationDefaultCredentialsFile
        {
            [JsonProperty]
            private string ApplicationDefaultCredentialsFile_;

            public ApplicationDefaultCredentialsFile(string applicationDefaultCredentialsFile)
            {
                ApplicationDefaultCredentialsFile_ = applicationDefaultCredentialsFile;
            }

            public static explicit operator string(ApplicationDefaultCredentialsFile applicationDefaultCredentialsFile)
            {
                return applicationDefaultCredentialsFile.ApplicationDefaultCredentialsFile_;
            }

            public override string ToString()
            {
                return ApplicationDefaultCredentialsFile_;
            }
        }

        public struct ApplicationOAuthConfiguration
        {
            public OAuth.ClientID ClientID;
            public OAuth.ClientSecret ClientSecret;
            public ApplicationDefaultCredentialsFile ApplicationDefaultCredentialsFile;
        }

        // client configuration
        const string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        const string tokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";
        const string userInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";

        private struct CodeVerifier
        {
            [JsonProperty]
            private string Verifier;

            public CodeVerifier(string verifier)
            {
                Verifier = verifier;
            }

            public static explicit operator string(CodeVerifier codeVerifier)
            {
                return codeVerifier.Verifier;
            }

            public override string ToString()
            {
                return Verifier;
            }
        }

        private struct State
        {
            [JsonProperty]
            private string State_;

            public State(string state)
            {
                State_ = state;
            }

            public static explicit operator string(State state)
            {
                return state.State_;
            }

            public override string ToString()
            {
                return State_;
            }
        }

        private struct RedirectURI
        {
            [JsonProperty]
            private string URI;

            public RedirectURI(string uri)
            {
                URI = uri;
            }

            public static explicit operator string(RedirectURI redirectURI)
            {
                return redirectURI.URI;
            }

            public override string ToString()
            {
                return URI;
            }
        }

        private struct AuthorizationCode
        {
            [JsonProperty]
            private string Code;

            public AuthorizationCode(string code)
            {
                Code = code;
            }

            public static explicit operator string(AuthorizationCode authorizationCode)
            {
                return authorizationCode.Code;
            }

            public override string ToString()
            {
                return Code;
            }
        }

        public struct AuthorizedUserApplicationDefaultCredentials
        {
            [JsonProperty(PropertyName = "client_id")]
            public OAuth.ClientID ClientId;
            [JsonProperty(PropertyName = "client_secret")]
            public OAuth.ClientSecret ClientSecret;
            [JsonProperty(PropertyName = "refresh_token")]
            public OAuth.RefreshToken RefreshToken;
            [JsonProperty(PropertyName = "type")]
            private readonly string Type;

            private const string AuthorizedUser = "authorized_user";

            public AuthorizedUserApplicationDefaultCredentials(OAuth.ClientID clientId, OAuth.ClientSecret clientSecret, OAuth.RefreshToken refreshToken)
            {
                ClientId = clientId;
                ClientSecret = clientSecret;
                RefreshToken = refreshToken;
                Type = AuthorizedUser;
            }

            public AuthorizedUserApplicationDefaultCredentials(ApplicationDefaultCredentials applicationDefaultCredentials)
            {
                ClientId = new OAuth.ClientID(applicationDefaultCredentials.Content["client_id"]);
                ClientSecret = new OAuth.ClientSecret(applicationDefaultCredentials.Content["client_secret"]);
                RefreshToken = new OAuth.RefreshToken(applicationDefaultCredentials.Content["refresh_token"]);
                Type = AuthorizedUser;
            }

            public override string ToString()
            {
                return $"ClientId: \"{ClientId}\", ClientSecret: \"{ClientSecret}\", RefreshToken: \"{RefreshToken}\", Type: \"{Type}\"";
            }
        }

        public class ApplicationDefaultCredentialsJsonConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                ApplicationDefaultCredentials applicationDefaultCredentials = (ApplicationDefaultCredentials)value;

                JObject jo = JObject.FromObject(applicationDefaultCredentials.Content);
                writer.WriteToken(jo.CreateReader());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                Dictionary<string, string> content = new Dictionary<string, string>();
                serializer.Populate(jo.CreateReader(), content);
                return new ApplicationDefaultCredentials(content);
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(ApplicationDefaultCredentials);
            }
        }

        [JsonConverter(typeof(ApplicationDefaultCredentialsJsonConverter))]
        public struct ApplicationDefaultCredentials
        {
            public readonly Dictionary<string, string> Content;

            private const string AuthorizedUser = "authorized_user";

            public ApplicationDefaultCredentials(Dictionary<string, string> content)
            {
                Content = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> entry in content)
                    Content[entry.Key] = entry.Value;
            }

            public ApplicationDefaultCredentials(AuthorizedUserApplicationDefaultCredentials authorizedUserApplicationDefaultCredentials)
            {
                Content = new Dictionary<string, string>();
                Content["type"] = AuthorizedUser;
                Content["client_id"] = (string) authorizedUserApplicationDefaultCredentials.ClientId;
                Content["client_secret"] = (string) authorizedUserApplicationDefaultCredentials.ClientSecret;
                Content["refresh_token"] = (string) authorizedUserApplicationDefaultCredentials.RefreshToken;
            }

            public bool IsAuthorizedUser()
            {
                if (Content != null)
                {
                    string value;
                    Content.TryGetValue("type", out value);
                    return value == AuthorizedUser;
                }
                else
                    return false;
            }

            public override string ToString()
            {
                List<string> entries = new List<string>();
                if (Content != null)
                {
                    foreach (KeyValuePair<string, string> entry in Content)
                    {
                        entries.Add($"{entry.Key}: \"{entry.Value}\"");
                    }
                }

                return string.Join(", ", entries.ToArray());
            }
        }


        public static async Task<AuthorizedUserApplicationDefaultCredentials> DoEndUserOAuthFlow(ApplicationOAuthConfiguration applicationOAuthConfiguration)
        {
            OAuth.RefreshToken refreshToken = await RefreshLoginInteractive(applicationOAuthConfiguration);
            AuthorizedUserApplicationDefaultCredentials authorizedUserApplicationDefaultCredentials = new AuthorizedUserApplicationDefaultCredentials(applicationOAuthConfiguration.ClientID, applicationOAuthConfiguration.ClientSecret, refreshToken);
            return authorizedUserApplicationDefaultCredentials;
        }

        private static ApplicationDefaultCredentials ReadApplicationDefaultCredentials(ApplicationDefaultCredentialsFile applicationDefaultCredentialsFile)
        {
            JsonSerializer jsonSerializer = new JsonSerializer();

            try
            {
                using (StreamReader streamReader = new StreamReader((string)applicationDefaultCredentialsFile))
                using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                {
                    return jsonSerializer.Deserialize<ApplicationDefaultCredentials>(jsonTextReader);
                }
            }
            catch (JsonSerializationException)
            {
                // Credentials file does not parse correctly
                return default;
            }
            catch (System.IO.FileNotFoundException)
            {
                // credentials file does not exist
                return default;
            }
        }

        private static void WriteApplicationDefaultCredentials(ApplicationDefaultCredentials applicationDefaultCredentials, ApplicationDefaultCredentialsFile applicationDefaultCredentialsFile)
        {
            JsonSerializer jsonSerializer = new JsonSerializer();

            using (StreamWriter streamWriter = new StreamWriter((string)applicationDefaultCredentialsFile))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(streamWriter))
            {
                jsonSerializer.Serialize(jsonTextWriter, applicationDefaultCredentials);
            }
        }

        public static void RemoveApplicationDefaultCredentials(ApplicationDefaultCredentialsFile applicationDefaultCredentialsFile)
        {
            try
            {
                File.Delete((string)applicationDefaultCredentialsFile);
            }
            catch (FileNotFoundException) { }
        }

        public static async Task CreateUserApplicationDefaultCredentials(ApplicationOAuthConfiguration applicationOAuthConfiguration)
        {
            RemoveApplicationDefaultCredentials(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile);

            AuthorizedUserApplicationDefaultCredentials authorizedUserApplicationDefaultCredentials = await DoEndUserOAuthFlow(applicationOAuthConfiguration);
            ApplicationDefaultCredentials applicationDefaultCredentials = new ApplicationDefaultCredentials(authorizedUserApplicationDefaultCredentials);
            WriteApplicationDefaultCredentials(applicationDefaultCredentials, applicationOAuthConfiguration.ApplicationDefaultCredentialsFile);
        }

        public static async Task<bool> RefreshUserApplicationDefaultCredentials(ApplicationOAuthConfiguration applicationOAuthConfiguration)
        {
            ApplicationDefaultCredentials applicationDefaultCredentials = ReadApplicationDefaultCredentials(applicationOAuthConfiguration.ApplicationDefaultCredentialsFile);
            if (applicationDefaultCredentials.Content == null)
            {
                AuthorizedUserApplicationDefaultCredentials newAuthorizedUserApplicationDefaultCredentials = await DoEndUserOAuthFlow(applicationOAuthConfiguration);
                ApplicationDefaultCredentials newApplicationDefaultCredentials = new ApplicationDefaultCredentials(newAuthorizedUserApplicationDefaultCredentials);
                WriteApplicationDefaultCredentials(newApplicationDefaultCredentials, applicationOAuthConfiguration.ApplicationDefaultCredentialsFile);
                return !string.IsNullOrEmpty((string)newAuthorizedUserApplicationDefaultCredentials.RefreshToken);
            }

            if (applicationDefaultCredentials.IsAuthorizedUser())
            {
                AuthorizedUserApplicationDefaultCredentials authorizedUserApplicationDefaultCredentials = new AuthorizedUserApplicationDefaultCredentials(applicationDefaultCredentials);
                if (string.IsNullOrEmpty((string)authorizedUserApplicationDefaultCredentials.RefreshToken)
                    || string.IsNullOrEmpty((string)await RefreshAccessToken(authorizedUserApplicationDefaultCredentials.RefreshToken, applicationOAuthConfiguration)))
                {
                    AuthorizedUserApplicationDefaultCredentials newAuthorizedUserApplicationDefaultCredentials = await DoEndUserOAuthFlow(applicationOAuthConfiguration);
                    ApplicationDefaultCredentials newApplicationDefaultCredentials = new ApplicationDefaultCredentials(newAuthorizedUserApplicationDefaultCredentials);
                    WriteApplicationDefaultCredentials(newApplicationDefaultCredentials, applicationOAuthConfiguration.ApplicationDefaultCredentialsFile);
                    return !string.IsNullOrEmpty((string)newAuthorizedUserApplicationDefaultCredentials.RefreshToken);
                }

                return !string.IsNullOrEmpty((string)authorizedUserApplicationDefaultCredentials.RefreshToken);
            }
            return true;
        }


        // ref http://stackoverflow.com/a/3978040
        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static async Task<OAuth.AccessToken> RefreshAccessToken(OAuth.RefreshToken refreshToken, ApplicationOAuthConfiguration applicationOAuthConfiguration)
        {
            output("Refreshing access token...");

            // builds the  request
            string tokenRequestURI = "https://oauth2.googleapis.com/token";
            string tokenRequestBody = string.Format("client_id={0}&client_secret={1}&grant_type=refresh_token&refresh_token={2}",
                (string)applicationOAuthConfiguration.ClientID,
                (string)applicationOAuthConfiguration.ClientSecret,
                (string)refreshToken
                );

            // sends the request
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(tokenRequestURI);
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            //tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            byte[] _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
            tokenRequest.ContentLength = _byteVersion.Length;
            Stream stream = tokenRequest.GetRequestStream();
            await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            try
            {
                // gets the response
                WebResponse tokenResponse = await tokenRequest.GetResponseAsync();
                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();

                    // converts to dictionary
                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);

                    string access_token = tokenEndpointDecoded["access_token"];
                    return new OAuth.AccessToken(access_token);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        output("HTTP: " + response.StatusCode);
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            // reads response body
                            string responseText = await reader.ReadToEndAsync();
                            output(responseText);
                        }
                    }

                }
            }

            return default(OAuth.AccessToken);
        }

        private static async Task<OAuth.RefreshToken> RefreshLoginInteractive(ApplicationOAuthConfiguration applicationOAuthConfiguration)
        {
            string[] scopes = new string[] { "openid", "profile", "https://www.googleapis.com/auth/devstorage.read_write" };

            RedirectURI redirectURI;
            HttpListener http;
            CreateHTTPListenerForInteractiveLogin(out redirectURI, out http);

            State state;
            CodeVerifier codeVerifier;
            InitiateInteractiveAuthorizationRequest(applicationOAuthConfiguration.ClientID, redirectURI, scopes, out state, out codeVerifier);

            HttpListenerContext context = await WaitForAuthorizationResponse(http);

            SendResponseToBrowser(http, context);

            AuthorizationCode authorizationCode = ParseAuthorizationResponse(http, context, state);

            RefreshTokenAndAccessToken refreshTokenAndAccessToken = await PerformCodeExchange(applicationOAuthConfiguration, redirectURI, authorizationCode, codeVerifier);

            return refreshTokenAndAccessToken.RefreshToken;
        }

        private static void CreateHTTPListenerForInteractiveLogin(out RedirectURI redirectURI, out HttpListener http)
        {
            // Creates a redirect URI using an available port on the loopback address.
            redirectURI = new RedirectURI(string.Format("http://{0}:{1}/", IPAddress.Loopback, GetRandomUnusedPort()));
            output("redirect URI: " + (string)redirectURI);

            // Creates an HttpListener to listen for requests on that redirect URI.
            http = new HttpListener();
            http.Prefixes.Add((string)redirectURI);
            output("Listening..");
            http.Start();
        }

        private static string CreateAuthorizationRequest(OAuth.ClientID clientID, State state, string code_challenge, string code_challenge_method, RedirectURI redirectURI, string[] scopes)
        {
            string scopesUrlEncoded = string.Join("%20", scopes);
            return string.Format("{0}?response_type=code&scope={1}&redirect_uri={2}&client_id={3}&state={4}&code_challenge={5}&code_challenge_method={6}",
                authorizationEndpoint,
                scopesUrlEncoded,
                System.Uri.EscapeDataString((string)redirectURI),
                (string)clientID,
                (string)state,
                code_challenge,
                code_challenge_method);
        }

        private static void InitiateInteractiveAuthorizationRequest(OAuth.ClientID clientID, RedirectURI redirectURI, string[] scopes, out State state, out CodeVerifier codeVerifier)
        {
            // Generates state and PKCE values.
            state = new State(randomDataBase64url(32));
            codeVerifier = new CodeVerifier(randomDataBase64url(32));
            string code_challenge = base64urlencodeNoPadding(sha256((string)codeVerifier));
            const string code_challenge_method = "S256";

            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = CreateAuthorizationRequest(clientID, state, code_challenge, code_challenge_method, redirectURI, scopes);

            // Opens request in the browser.
            System.Diagnostics.Process.Start(authorizationRequest);
        }

        private static async Task<HttpListenerContext> WaitForAuthorizationResponse(HttpListener http)
        {
            // Waits for the OAuth authorization response.
            return await http.GetContextAsync();
        }

        private static void SendResponseToBrowser(HttpListener http, HttpListenerContext context)
        {
            // Sends an HTTP response to the browser.
            var response = context.Response;
            string responseString = string.Format("<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>");
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
            {
                responseOutput.Close();
                http.Stop();
                Console.WriteLine("HTTP server stopped.");
            });
        }

        private static AuthorizationCode ParseAuthorizationResponse(HttpListener http, HttpListenerContext context, State state)
        {
            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                output(String.Format("OAuth authorization error: {0}.", context.Request.QueryString.Get("error")));
                return default(AuthorizationCode);
            }
            if (context.Request.QueryString.Get("code") == null
                || context.Request.QueryString.Get("state") == null)
            {
                output("Malformed authorization response. " + context.Request.QueryString);
                return default(AuthorizationCode);
            }

            // extracts the code
            var code = context.Request.QueryString.Get("code");
            var incoming_state = context.Request.QueryString.Get("state");

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incoming_state != (string)state)
            {
                output(String.Format("Received request with invalid state ({0})", incoming_state));
                return default(AuthorizationCode);
            }
            output("Authorization code: " + code);

            return new AuthorizationCode(code);
        }

        private struct RefreshTokenAndAccessToken
        {
            public OAuth.RefreshToken RefreshToken;
            public OAuth.AccessToken AccessToken;
        }

        private static async Task<RefreshTokenAndAccessToken> PerformCodeExchange(ApplicationOAuthConfiguration applicationOAuthConfiguration, RedirectURI redirectURI, AuthorizationCode authorizationCode, CodeVerifier codeVerifier)
        {
            output("Exchanging code for tokens...");

            // builds the  request
            string tokenRequestURI = "https://www.googleapis.com/oauth2/v4/token";
            string tokenRequestBody = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&client_secret={4}&scope=&grant_type=authorization_code",
                (string)authorizationCode,
                System.Uri.EscapeDataString((string)redirectURI),
                (string)applicationOAuthConfiguration.ClientID,
                (string)codeVerifier,
                (string)applicationOAuthConfiguration.ClientSecret
                );

            // sends the request
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(tokenRequestURI);
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            byte[] _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
            tokenRequest.ContentLength = _byteVersion.Length;
            Stream stream = tokenRequest.GetRequestStream();
            await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            try
            {
                // gets the response
                WebResponse tokenResponse = await tokenRequest.GetResponseAsync();
                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();

                    // converts to dictionary
                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);

                    string refresh_token = tokenEndpointDecoded["refresh_token"];
                    string access_token = tokenEndpointDecoded["access_token"];

                    return new RefreshTokenAndAccessToken { RefreshToken = new OAuth.RefreshToken(refresh_token), AccessToken = new OAuth.AccessToken(access_token) };
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        output("HTTP: " + response.StatusCode);
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            // reads response body
                            string responseText = await reader.ReadToEndAsync();
                            output(responseText);
                        }
                    }

                }
            }

            return default;
        }

        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        /// <param name="output">string to be appended</param>
        public static void output(string output)
        {
            Console.WriteLine(output);
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        public static string randomDataBase64url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return base64urlencodeNoPadding(bytes);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        public static byte[] sha256(string inputStirng)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string base64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }
    }
}
