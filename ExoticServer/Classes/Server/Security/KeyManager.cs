using System.Security.Cryptography;
namespace ExoticServer.Classes.Server.Security
{
    public class KeyManager
    {
        private RSAParameters _publicKey;
        private RSAParameters _privateKey;

        private RSAParameters _clientPublicKey;

        // Initialize RSA for key pair generation
        private RSA _rsa = new RSACng();

        public KeyManager()
        {
            _rsa.KeySize = 4096;
            GenerateKeyPair();
        }

        // Generate RSA public and private keys
        private void GenerateKeyPair()
        {
            _publicKey = _rsa.ExportParameters(false);  // false to get the public key
            _privateKey = _rsa.ExportParameters(true);  // true to get the private key
        }

        // Get the server's public key
        public RSAParameters GetPublicKey()
        {
            return _publicKey;
        }

        public RSAParameters GetClientPublicKey()
        {
            return _clientPublicKey;
        }

        public void SetClientPublicKey(RSAParameters clientPublicKey)
        {
            _clientPublicKey = clientPublicKey;
        }
    }
}
