using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

namespace SmartBepInMods.Tools
{
    /// <summary>
    /// Base Universal log delegate
    /// </summary>
    /// <param name="s"></param>
    public delegate void Log(object s);
    internal interface Serializable { }
    /// <summary>
    /// Utility functions
    /// </summary>
    internal static class Extensions
    {
        public static TAttribute GetAttributeValue<TAttribute>(this Type type) where TAttribute : Attribute
        {
            var att = type.GetCustomAttributes(
                typeof(TAttribute), true
            ).FirstOrDefault() as TAttribute;
            if (att == null) throw new InvalidOperationException("Invalid Attribute request!");
            return att;
        }
        /// <summary>
        /// Parses the object from JSON
        /// </summary>
        /// <typeparam name="T">Object</typeparam>
        /// <param name="data">JSON</param>
        /// <returns></returns>
        internal static T Deserialize<T>(this string data)
        {
            return JsonUtility.FromJson<T>(data);
        }
        /// <summary>
        /// Parses the object to JSON
        /// </summary>
        /// <param name="serializable"></param>
        /// <returns></returns>
        internal static string Serialize(this Serializable serializable)
        {
            return JsonUtility.ToJson(serializable);
        }
        /// <summary>
        /// Posts a message to Discord via a Webhook
        /// </summary>
        /// <param name="body">This.</param>
        /// <param name="endpoint">Webhook URL</param>
        public static string PostMessage(string endpoint, Serializable body = null)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(endpoint);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            if (body != null)
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(body.Serialize());
                }
            }

            using (var streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
            {
                return streamReader.ReadToEnd();
            }
        }
        /// <summary>
        /// Gets an item from discord.
        /// </summary>
        /// <param name="body">This.</param>
        /// <param name="endpoint">Webhook URL</param>
        public static string GetMessage(string endpoint, Serializable body = null)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(endpoint);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";

            if (body != null)
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(body.Serialize());
                }
            }

            using (var streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream()))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
    namespace Discord
    {
        /// <summary>
        /// Webhook object
        /// </summary>
        public struct Webhook : Serializable
        {
            public string id;
            public int type;
            public string guild_id;
            public string channel_id;
            public User user;
            public string name;
            public string avatar;
            public string token;
            public string application_id;

            /// <summary>
            /// Gets the webhook object for the given id
            /// </summary>
            /// <param name="id">YOUR ID</param>
            /// <param name="token">YOUR TOKEN</param>
            /// <returns></returns>
            public static Webhook GetWebhook(string id, string token)
            {
                return Extensions.GetMessage($"https://discord.com/api/webhooks/{id}/{token}").Deserialize<Webhook>();
            }
            /// <summary>
            /// Executes the webhook object!
            /// </summary>
            /// <param name="id"></param>
            /// <param name="token"></param>
            public static void Execute(string id, string token, ExecuteForm data)
            {
                Extensions.PostMessage($"https://discord.com/api/webhooks/{id}/{token}", data);
            }
            /// <summary>
            /// Form for Webhook.Execute()
            /// </summary>
            public struct ExecuteForm : Serializable
            {
                public string content;
                public string username;
                public string avatar_url;
                public bool tts;
                public Embed[] embeds;
                // Todo File & Mentions
            }
        }
        /// <summary>
        /// User object
        /// </summary>
        public struct User : Serializable
        {
            public string id;
            public string username;
            public string discriminator;
            public string avatar;
            public bool bot;
            public bool system;
            public bool mfa_enabled;
            public string locale;
            public bool verified;
            public string email;
            public int flags;
            public int premium_type;
            public int public_flags;
        }
        /// <summary>
        /// Embed object
        /// </summary>
        public struct Embed : Serializable
        {
            public string title;
            public string description;
            public string url;
            public int color;
            public Footer footer;
            public Image image;
            public Thumbnail thumbnail;
            public Video video;
            public Provider provider;
            public Author author;
            public Field[] fields;

            /// <summary>
            /// Embed Footer object
            /// </summary>
            public struct Footer : Serializable
            {
                public string text;
                public string icon_url;
                public string proxy_icon_url;
            }
            /// <summary>
            /// Embed Image object
            /// </summary>
            public struct Image : Serializable
            {
                public string url;
                public string proxy_url;
                public int height;
                public int width;
            }
            /// <summary>
            /// Embed Thumbnail object
            /// </summary>
            public struct Thumbnail : Serializable
            {
                public string url;
                public string proxy_url;
                public string height;
                public string width;
            }
            /// <summary>
            /// Embed Video object
            /// </summary>
            public struct Video : Serializable
            {
                public string url;
                public string proxy_url;
                public string height;
                public string width;
            }
            /// <summary>
            /// Embed Provider object
            /// </summary>
            public struct Provider : Serializable
            {
                public string name;
                public string ur;
            }
            /// <summary>
            /// Embed Author object
            /// </summary>
            public struct Author : Serializable
            {
                public string name;
                public string url;
                public string icon_url;
                public string proxy_icon_url;
            }
            /// <summary>
            /// Embed Field object
            /// </summary>
            public struct Field : Serializable
            {
                public string name;
                public string value;
                public bool inline;
            }
        }
    }

}
