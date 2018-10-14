using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Nancy;
using NantCom.NancyBlack.Modules.DatabaseSystem;
using NantCom.NancyBlack.Modules.MembershipSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace NantCom.NancyBlack.Modules.GoogleAccountSystem
{
    public class GoogleAccountModule : BaseModule
    {
        /// <summary>
        /// Client ID
        /// </summary>
        private string ClientID
        {
            get
            {
                if (this.CurrentSite.google == null || this.CurrentSite.google.ClientID == null)
                {
                    throw new InvalidOperationException("Google OAuth was not initialized");
                }

                return this.CurrentSite.google.ClientID;
            }
        }

        /// <summary>
        /// Client Secret
        /// </summary>
        private string ClientSecret
        {
            get
            {
                if (this.CurrentSite.google == null || this.CurrentSite.google.ClientSecret == null)
                {
                    throw new InvalidOperationException("Google OAuth was not initialized");
                }

                return this.CurrentSite.google.ClientSecret;
            }
        }


        /// <summary>
        /// Module which handles google oauth flow
        /// </summary>
        public GoogleAccountModule()
        {
            Get["/__googleoauth"] = this.HandleRequest((arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return 401;
                }

                if (this.Request.Query.scope == null)
                {
                    return 400;
                }

                // Generates state and PKCE values.
                string state = randomDataBase64url(32);
                string code_verifier = randomDataBase64url(32);
                string code_challenge = base64urlencodeNoPadding(sha256(code_verifier));

                const string code_challenge_method = "S256";

                string redirectURI = this.Request.Url.ToString().Replace(this.Request.Url.Query, "") + "/receive";

                // Creates the OAuth 2.0 authorization request.
                string authorizationRequest = string.Format("{0}?response_type=code&scope=openid%20profile{6}&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}&access_type=offline&prompt=consent",
                    "https://accounts.google.com/o/oauth2/v2/auth",
                    Uri.EscapeDataString(redirectURI),
                    this.ClientID,
                    state,
                    code_challenge,
                    code_challenge_method,
                    "%20" + Uri.EscapeDataString((string)this.Request.Query.scope)
                );

                GlobalVar.Default["GoogleOAuthState-" + this.CurrentUser.Id] = JsonConvert.SerializeObject(new
                {
                    state = state,
                    code_verifier = code_verifier,
                    code_challenge = code_challenge
                });

                this.SiteDatabase.UpsertRecord(this.CurrentUser);

                return this.Response.AsRedirect(authorizationRequest);
            });

            Get["/__googleoauth/receive"] = (arg) =>
            {
                if (this.CurrentUser.IsAnonymous)
                {
                    return 400;
                }

                // Checks for errors.
                if (this.Request.Query.error != null)
                {
                    return this.Request.Query.error;
                }

                if (this.Request.Query.code == null
                    || this.Request.Query.state == null)
                {
                    return 400;
                }

                // extracts the code
                var code = this.Request.Query.code;
                var incoming_state = this.Request.Query.state;

                dynamic userState = JObject.Parse(GlobalVar.Default["GoogleOAuthState-" + this.CurrentUser.Id]);

                // Compares the receieved state to the expected value, to ensure that
                // this app made the request which resulted in authorization.
                if (incoming_state != (string)userState.state)
                {
                    return 400;
                }

                string redirectURI = this.Request.Url.ToString().Replace( this.Request.Url.Query, "" );

                // Gets the token
                {
                    var client = new RestClient("https://www.googleapis.com/");
                    var req = new RestRequest("/oauth2/v4/token");
                    req.Method = Method.POST;
                    req.AddParameter("code", code);
                    req.AddParameter("redirect_uri", redirectURI);
                    req.AddParameter("client_id", this.ClientID);
                    req.AddParameter("code_verifier", (string)userState.code_verifier);
                    req.AddParameter("client_secret", this.ClientSecret);
                    req.AddParameter("scope", "");
                    req.AddParameter("grant_type", "authorization_code");

                    var response = client.Execute(req);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        return response.Content;
                    }

                    GlobalVar.Default["GoogleOAuthState-" + this.CurrentUser.Id] = null;
                    this.CurrentUser.GoogleOAuthToken = JObject.Parse(response.Content);
                    this.CurrentUser.GoogleOAuthToken.Expiry = DateTime.Now.AddMinutes( (int)this.CurrentUser.GoogleOAuthToken.expires_in );
                    this.SiteDatabase.UpsertRecord(this.CurrentUser);
                }

                {
                    var client = new RestClient("https://www.googleapis.com/");
                    var req = new RestRequest("/oauth2/v3/userinfo");
                    req.Method = Method.GET;
                    req.AddHeader("Authorization", "Bearer " + (string)this.CurrentUser.GoogleOAuthToken.access_token);

                    var response = client.Execute(req);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        return response.Content;
                    }

                    this.CurrentUser.GoogleUserInfo = JObject.Parse(response.Content);
                    this.SiteDatabase.UpsertRecord(this.CurrentUser);
                }

                return "Token Received";
            };
        }


        /// <summary>
        /// Refresh access token for given user id
        /// </summary>
        public static void RefreshTokenIfRequired(NancyBlackDatabase db, dynamic siteSettings, int userId)
        {
            var user = db.GetById<NcbUser>(userId);
            if (user == null)
            {
                throw new ArgumentException("User is not valid");
            }

            GoogleAccountModule.RefreshTokenIfRequired(db, siteSettings, user);
        }

        /// <summary>
        /// Refresh access token for given user
        /// </summary>
        public static void RefreshTokenIfRequired( NancyBlackDatabase db, dynamic siteSettings, NcbUser user )
        {
            if (user.GoogleOAuthToken == null ||
                user.GoogleOAuthToken.refresh_token == null)
            {
                throw new ArgumentException("User was never authenticated with google or does not have refresh_token");
            }

            // No need to refresh token
            if (((DateTime)user.GoogleOAuthToken.Expiry).Subtract(DateTime.Now).TotalMinutes > 2)
            {
                return;
            }

            // Gets the token
            {
                var client = new RestClient("https://www.googleapis.com/");
                var req = new RestRequest("/oauth2/v4/token");
                req.Method = Method.POST;
                req.AddParameter("client_id", siteSettings.google.ClientID);
                req.AddParameter("client_secret", siteSettings.google.ClientSecret);
                req.AddParameter("refresh_token", user.GoogleOAuthToken.refresh_token);
                req.AddParameter("grant_type", "refresh_token");

                var response = client.Execute(req);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new InvalidOperationException(response.Content);
                }

                dynamic result = JObject.Parse(response.Content);

                user.GoogleOAuthToken.access_token = result.access_token;
                user.GoogleOAuthToken.expires_in = result.expires_in;
                user.GoogleOAuthToken.Expiry = DateTime.Now.AddSeconds((int)user.GoogleOAuthToken.expires_in);

                db.UpsertRecord(user);

            }
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        private static string randomDataBase64url(uint length)
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
        private static byte[] sha256(string inputStirng)
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
        private static string base64urlencodeNoPadding(byte[] buffer)
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