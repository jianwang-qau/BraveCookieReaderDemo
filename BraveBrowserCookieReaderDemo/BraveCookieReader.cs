using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace BraveBrowserCookieReaderDemo
{
    public class BraveCookieReader
    {
        public IEnumerable<Tuple<string, string>> ReadCookies(string hostName)
        {
            if (hostName == null) throw new ArgumentNullException("hostName");

            using var context = new BraveCookieDbContext();

            var cookies = context
                .Cookies
                .Where(c => c.HostKey.Equals(hostName))
                .AsNoTracking();

            // Big thanks to https://stackoverflow.com/a/60611673/6481581 for answering how Chrome 80 and up changed the way cookies are encrypted.

            string encKey = File.ReadAllText(System.Environment.GetEnvironmentVariable("LOCALAPPDATA") + @"\BraveSoftware\Brave-Browser\User Data\Local State");
            encKey = JObject.Parse(encKey)["os_crypt"]["encrypted_key"].ToString();
            var decodedKey = System.Security.Cryptography.ProtectedData.Unprotect(Convert.FromBase64String(encKey).Skip(5).ToArray(), null, System.Security.Cryptography.DataProtectionScope.LocalMachine);

            foreach (var cookie in cookies)
            {

                var data = cookie.EncryptedValue;

                var decodedValue = _decryptWithKey(data, decodedKey, 3);


                yield return Tuple.Create(cookie.Name, decodedValue);
            }
        }


        private string _decryptWithKey(byte[] message, byte[] key, int nonSecretPayloadLength)
        {
            const int KEY_BIT_SIZE = 256;
            const int MAC_BIT_SIZE = 128;
            const int NONCE_BIT_SIZE = 96;

            if (key == null || key.Length != KEY_BIT_SIZE / 8)
                throw new ArgumentException(String.Format("Key needs to be {0} bit!", KEY_BIT_SIZE), "key");
            if (message == null || message.Length == 0)
                throw new ArgumentException("Message required!", "message");

            using (var cipherStream = new MemoryStream(message))
            using (var cipherReader = new BinaryReader(cipherStream))
            {
                var nonSecretPayload = cipherReader.ReadBytes(nonSecretPayloadLength);
                var nonce = cipherReader.ReadBytes(NONCE_BIT_SIZE / 8);
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(new KeyParameter(key), MAC_BIT_SIZE, nonce);
                cipher.Init(false, parameters);
                var cipherText = cipherReader.ReadBytes(message.Length);
                var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];
                try
                {
                    var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);
                    cipher.DoFinal(plainText, len);
                }
                catch (InvalidCipherTextException)
                {
                    return null;
                }
                return Encoding.Default.GetString(plainText);
            }
        }
    }
}
