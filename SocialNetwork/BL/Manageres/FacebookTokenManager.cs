﻿using Authetication.Db;
using Authetication.Models;
using BL.Interfaces;
using BL.Models;
using Jose;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Manageres
{
    public class FacebookTokenManager/* : ITokenMng, IToken*///TODO use interface
    {
        private readonly TokenService _TokenService;
        private readonly string _secretKey = "G0xanO_eGRj8mS8xRVAPb3TXAWLP416mrn1XOiqwvcnFZV4AUd97F978NoDsFKYthzRsKsM3dILakTJRlUC9336wOe-x2xotyDyerEMdSIUmoWxXyjbxTCCZ-pEqaM3CH32Q7G6k_Ys_SVgrHDGAO5ueTIJ2lko9rYBnnF6lYnlEONVk-7dzFrVCRRZMrI-iM4-EH_gbdxP93S-xf_6-R5iuS8lzoBwljPcEv90iQ5PLKQ9OXU1I1RLjOAXQKKzCY1oQKkti9NMyGyfk0Sci56D5HnCo-Mk91AbzbuHIZx9Ng97buh7vaGT6FQZg1KfzYzB9Vg4uDtpHRXYLbnx9yQ";

        public FacebookTokenManager()
        {
            _TokenService = new TokenService();
        }

        public string CreateAndSave(FacebookUser user)
        {
            string token = GenerateJWT(user);

            Token tmp = new Token
            {
                UserId = user.UserId,
                token = token,
                TimeStamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds,
                State = State.active
            };

            Save(tmp);
            //add to neo4j
            return tmp.token;
        }

        private Token Save(Token token)
        {
            _TokenService.Add(token);
            return token;
        }

        public string GenerateJWT(FacebookUser user)
        {
            Byte[] secretKey = Encoding.ASCII.GetBytes(_secretKey);

            long iat = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            long exp = (long)(DateTime.UtcNow.AddMinutes(15) - new DateTime(1970, 1, 1)).TotalSeconds;

            var paylode = new Dictionary<string, object>()
            {
                ["sub"] = user.UserId,
                ["exp"] = exp,
                ["iat"] = iat,
                ["facebookId"] = user.FacebookId
            };

            return JWT.Encode(paylode, secretKey, JwsAlgorithm.HS256);
        }

        private string Decrypt(string token)
        {
            // string secretKey = "G0xanO_eGRj8mS8xRVAPb3TXAWLP416mrn1XOiqwvcnFZV4AUd97F978NoDsFKYthzRsKsM3dILakTJRlUC9336wOe-x2xotyDyerEMdSIUmoWxXyjbxTCCZ-pEqaM3CH32Q7G6k_Ys_SVgrHDGAO5ueTIJ2lko9rYBnnF6lYnlEONVk-7dzFrVCRRZMrI-iM4-EH_gbdxP93S-xf_6-R5iuS8lzoBwljPcEv90iQ5PLKQ9OXU1I1RLjOAXQKKzCY1oQKkti9NMyGyfk0Sci56D5HnCo-Mk91AbzbuHIZx9Ng97buh7vaGT6FQZg1KfzYzB9Vg4uDtpHRXYLbnx9yQ";

            try
            {
                string paylode = JWT.Decode(token, Encoding.ASCII.GetBytes(_secretKey));
                dynamic data = JObject.Parse(paylode);

                long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                if (data.exp < now || data.iat > now)
                    return null;
                return data.sub;
            }
            catch (InvalidAlgorithmException)
            {
                return null;
            }
            catch (IntegrityException)
            {
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        private string DecryptFacebookId(string token)
        {
            // string secretKey = "G0xanO_eGRj8mS8xRVAPb3TXAWLP416mrn1XOiqwvcnFZV4AUd97F978NoDsFKYthzRsKsM3dILakTJRlUC9336wOe-x2xotyDyerEMdSIUmoWxXyjbxTCCZ-pEqaM3CH32Q7G6k_Ys_SVgrHDGAO5ueTIJ2lko9rYBnnF6lYnlEONVk-7dzFrVCRRZMrI-iM4-EH_gbdxP93S-xf_6-R5iuS8lzoBwljPcEv90iQ5PLKQ9OXU1I1RLjOAXQKKzCY1oQKkti9NMyGyfk0Sci56D5HnCo-Mk91AbzbuHIZx9Ng97buh7vaGT6FQZg1KfzYzB9Vg4uDtpHRXYLbnx9yQ";

            try
            {
                string paylode = JWT.Decode(token, Encoding.ASCII.GetBytes(_secretKey));
                dynamic data = JObject.Parse(paylode);

                long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                if (data.exp < now || data.iat > now)
                    return null;
                return data.facebookId;
            }
            catch (InvalidAlgorithmException)
            {
                return null;
            }
            catch (IntegrityException)
            {
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }


        public bool Verify(string token)
        {
            string paylode = Decrypt(token);
            if (paylode == null)
                return false;
            return true;
        }

        public string DecodeFacebookId(string token)
        {
            return DecryptFacebookId(token);
        }
        public string DecodeUserId(string token)
        {
            return Decrypt(token);
        }

        public string RefreshToken(string token)
        {

            try
            {
                string paylode = JWT.Decode(token, Encoding.ASCII.GetBytes(_secretKey));
                dynamic data = JObject.Parse(paylode);

                long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                if (data.exp < now || data.iat > now)
                    return null;

                return GenerateJWT(new FacebookUser { UserId = data.sub, FacebookId = data.facebookId });
            }
            //catch (InvalidAlgorithmException)
            //{
            //    return null;
            //}
            //catch (IntegrityException)
            //{
            //    return null;
            //}
            catch (Exception e)
            {
                return null;
            }
        }

    }
}