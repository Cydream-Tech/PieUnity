using System;
using System.Security.Cryptography;
using System.Text;

namespace Pie
{
    // Preserved by Runtime/link.xml (Pie.Runtime preserve=all) and included in
    // the static-binding preset. BCL only: no Unity, logging or thread APIs.
    public sealed class PieUtf8Sha256 : IDisposable
    {
        private readonly SHA256 hash = SHA256.Create();
        private bool finalized;
        private bool disposed;

        // Each fragment must contain complete surrogate pairs. The JSON emitter
        // escapes lone surrogates before calling this boundary.
        public void UpdateUtf8(string text)
        {
            CheckActive();
            var bytes = Encoding.UTF8.GetBytes(text);
            if (bytes.Length != 0)
                hash.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
        }

        public string DigestHex()
        {
            CheckActive();
            finalized = true;
            hash.TransformFinalBlock(new byte[0], 0, 0);
            var digest = hash.Hash;
            var hex = new StringBuilder(64);
            foreach (var b in digest) hex.Append(b.ToString("x2"));
            return hex.ToString();
        }

        private void CheckActive()
        {
            if (disposed) throw new ObjectDisposedException(nameof(PieUtf8Sha256));
            if (finalized) throw new InvalidOperationException("Hash already finalized");
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            hash.Dispose();
        }
    }
}
