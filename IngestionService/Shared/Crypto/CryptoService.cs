using IngestionService.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IngestionService.Services
{
    public class CryptoService
    {
        private readonly RSA _sensorPrivateKey;
        private readonly RSA _serverPublicKey;

        public string SensorPublicKeyBase64 { get; }

        public CryptoService(RSA sensorPrivateKey, RSA serverPublicKey)
        {
            _sensorPrivateKey = sensorPrivateKey;
            _serverPublicKey = serverPublicKey;
            
            SensorPublicKeyBase64 = Convert.ToBase64String(
                sensorPrivateKey.ExportSubjectPublicKeyInfo());
        }


        public SecureMessage Encrypt(SensorReading reading)
        {
            string json = JsonSerializer.Serialize(reading);
            byte[] plaintext = Encoding.UTF8.GetBytes(json);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateKey();
            aes.GenerateIV();

            byte[] encrypted;
            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plaintext, 0, plaintext.Length);
                cs.FlushFinalBlock();
                encrypted = ms.ToArray();
            }

            byte[] wrappedKey = _serverPublicKey.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

            byte[] signature = _sensorPrivateKey.SignData(
                encrypted, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return new SecureMessage
            {
                EncryptedPayload = Convert.ToBase64String(encrypted),
                EncryptedAesKey = Convert.ToBase64String(wrappedKey),
                IV = Convert.ToBase64String(aes.IV),
                Signature = Convert.ToBase64String(signature),
                SenderPublicKey = SensorPublicKeyBase64,
                SentAt = reading.Timestamp.ToString("O"),
                MessageId = reading.MessageId,
                SensorId = reading.SensorId
            };
        }

        public static SensorReading? Decrypt(
            SecureMessage msg,
            RSA serverPrivateKey)
        {
            try
            {
                byte[] ciphertext = Convert.FromBase64String(msg.EncryptedPayload);
                byte[] wrappedKey = Convert.FromBase64String(msg.EncryptedAesKey);
                byte[] iv = Convert.FromBase64String(msg.IV);
                byte[] signature = Convert.FromBase64String(msg.Signature);
                byte[] senderPubDer = Convert.FromBase64String(msg.SenderPublicKey);

                using var senderRsa = RSA.Create();
                senderRsa.ImportSubjectPublicKeyInfo(senderPubDer, out _);
                bool valid = senderRsa.VerifyData(
                    ciphertext, signature,
                    HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                if (!valid) return null;

                byte[] aesKey = serverPrivateKey.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256);

                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = aesKey;
                aes.IV = iv;

                byte[] plaintext;
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(ciphertext))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var result = new MemoryStream())
                {
                    cs.CopyTo(result);
                    plaintext = result.ToArray();
                }

                return JsonSerializer.Deserialize<SensorReading>(Encoding.UTF8.GetString(plaintext));
            }
            catch
            {
                return null;
            }
        }

        public static RSA GenerateRsaKeyPair() => RSA.Create(2048);

        public static string ExportPrivateKeyPem(RSA rsa)
            => rsa.ExportRSAPrivateKeyPem();

        public static string ExportPublicKeyPem(RSA rsa)
            => rsa.ExportSubjectPublicKeyInfoPem();

        public static RSA LoadPrivateKeyPem(string pem)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }

        public static RSA LoadPublicKeyPem(string pem)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }
    }
}
