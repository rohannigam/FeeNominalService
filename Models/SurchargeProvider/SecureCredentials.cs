using System;
using System.Security;
using System.Text.Json;
using FeeNominalService.Utils;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Secure wrapper for Credentials using SecureString to prevent memory dumps
    /// Checkmarx: Privacy Violation - This class uses SecureString for secure handling of credentials data
    /// Enhanced security: Uses SecureString and proper disposal to prevent memory dumps and exposure
    /// </summary>
    public class SecureCredentials : IDisposable
    {
        private SecureString? _secureCredentials;
        private bool _disposed = false;

        /// <summary>
        /// Sets the credentials securely using SecureString
        /// </summary>
        /// <param name="credentialsJson">The JSON string representation of the credentials</param>
        public void SetCredentials(string credentialsJson)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureCredentials));

            _secureCredentials?.Dispose();
            _secureCredentials = new SecureString();
            
            foreach (char c in credentialsJson)
            {
                _secureCredentials.AppendChar(c);
            }
            _secureCredentials.MakeReadOnly();
        }

        /// <summary>
        /// Processes the secure credentials data within a secure context
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="processor">Function to process the credentials data</param>
        /// <returns>Result of the processing function</returns>
        public T? ProcessCredentialsSecurely<T>(Func<SecureString, T> processor)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureCredentials));

            if (_secureCredentials == null)
                return default(T);

            var credentialsString = SimpleSecureDataHandler.FromSecureString(_secureCredentials);
            return SimpleSecureDataHandler.ProcessSecurely(credentialsString, secureCredentials => processor(secureCredentials));
        }

        /// <summary>
        /// Gets the credentials as a JsonDocument for processing
        /// </summary>
        /// <returns>JsonDocument representation of the credentials</returns>
        public JsonDocument? GetCredentials()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureCredentials));

            if (_secureCredentials == null)
                return null;

            var credentialsString = SimpleSecureDataHandler.FromSecureString(_secureCredentials);
            return JsonDocument.Parse(credentialsString);
        }

        /// <summary>
        /// Gets the credentials as a string (use with caution)
        /// </summary>
        /// <returns>String representation of the credentials</returns>
        public string GetCredentialsString()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureCredentials));

            if (_secureCredentials == null)
                return string.Empty;

            return SimpleSecureDataHandler.FromSecureString(_secureCredentials);
        }

        /// <summary>
        /// Creates a SecureCredentials from a JsonDocument
        /// </summary>
        /// <param name="credentialsDoc">The JsonDocument</param>
        /// <returns>SecureCredentials wrapper</returns>
        public static SecureCredentials FromJsonDocument(JsonDocument credentialsDoc)
        {
            var credentialsJson = credentialsDoc.RootElement.GetRawText();
            var secure = new SecureCredentials();
            secure.SetCredentials(credentialsJson);
            return secure;
        }

        /// <summary>
        /// Creates a SecureCredentials from a JSON string
        /// </summary>
        /// <param name="credentialsJson">The JSON string</param>
        /// <returns>SecureCredentials wrapper</returns>
        public static SecureCredentials FromJsonString(string credentialsJson)
        {
            var secure = new SecureCredentials();
            secure.SetCredentials(credentialsJson);
            return secure;
        }

        /// <summary>
        /// Creates a SecureCredentials from an object
        /// </summary>
        /// <param name="credentials">The credentials object</param>
        /// <returns>SecureCredentials wrapper</returns>
        public static SecureCredentials FromObject(object credentials)
        {
            var credentialsJson = JsonSerializer.Serialize(credentials);
            var secure = new SecureCredentials();
            secure.SetCredentials(credentialsJson);
            return secure;
        }

        /// <summary>
        /// Disposes the SecureString to clear it from memory
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _secureCredentials?.Dispose();
                _secureCredentials = null;
                _disposed = true;
            }
        }
    }
} 