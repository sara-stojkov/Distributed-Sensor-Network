using Microsoft.AspNetCore.Mvc;

namespace IngestionService.Controllers
{
    [ApiController]
    [Route("api/keys")]
    public class KeysController : ControllerBase
    {
        private readonly ServerKeyProvider _keyProvider;

        public KeysController(ServerKeyProvider keyProvider)
            => _keyProvider = keyProvider;

        /// <summary>
        /// GET /api/keys/server-public-key
        /// Returns the server's RSA public key in PEM format.
        /// Sensors call this on startup to encrypt their AES session keys.
        /// </summary>
        [HttpGet("server-public-key")]
        public IActionResult GetPublicKey()
            => Content(_keyProvider.PublicKeyPem, "text/plain");
    }

}
