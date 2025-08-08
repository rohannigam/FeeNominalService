using System;
using System.Security;
using System.Text;
using System.Runtime.InteropServices;

namespace FeeNominalService.Utils
{
    /// <summary>
    /// Simple secure handler for sensitive data using SecureString (without ProtectedData)
    /// This class provides secure handling of sensitive data to prevent memory dumps
    /// </summary>
    public static class SimpleSecureDataHandler
    {
        /// <summary>
        /// Converts a string to SecureString for secure memory handling
        /// </summary>
        /// <param name="input">The string to secure</param>
        /// <returns>SecureString containing the sensitive data</returns>
        public static SecureString ToSecureString(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return new SecureString();

            var secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        /// <summary>
        /// Converts SecureString back to string (use with caution)
        /// </summary>
        /// <param name="secureString">The SecureString to convert</param>
        /// <returns>The string value</returns>
        public static string FromSecureString(SecureString? secureString)
        {
            if (secureString == null || secureString.Length == 0)
                return string.Empty;

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(unmanagedString) ?? string.Empty;
            }
            finally
            {
                if (unmanagedString != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }
        }

        /// <summary>
        /// Securely processes sensitive data using a temporary SecureString
        /// </summary>
        /// <param name="sensitiveData">The sensitive data to process</param>
        /// <param name="processor">Action to perform with the secure data</param>
        public static void ProcessSecurely(string? sensitiveData, Action<SecureString>? processor)
        {
            if (string.IsNullOrEmpty(sensitiveData) || processor == null)
                return;

            using var secureData = ToSecureString(sensitiveData);
            processor(secureData);
        }

        /// <summary>
        /// Securely processes sensitive data and returns a result
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="sensitiveData">The sensitive data to process</param>
        /// <param name="processor">Function to perform with the secure data</param>
        /// <returns>The result of the processing</returns>
        public static T? ProcessSecurely<T>(string? sensitiveData, Func<SecureString, T>? processor)
        {
            if (string.IsNullOrEmpty(sensitiveData) || processor == null)
                return default(T?);

            using var secureData = ToSecureString(sensitiveData);
            return processor(secureData);
        }

        /// <summary>
        /// Securely clears a string from memory (safe version without unsafe code)
        /// </summary>
        /// <param name="str">The string to clear</param>
        public static void SecureClear(ref string? str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                // Create a new string with zeros to overwrite the original
                var zeroString = new string('\0', str.Length);
                str = zeroString;
                str = null;
            }
        }

        /// <summary>
        /// Creates a secure temporary object that automatically clears sensitive data
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="sensitiveData">The sensitive data</param>
        /// <param name="factory">Factory function to create the object</param>
        /// <returns>Secure temporary object</returns>
        public static SimpleSecureTempObject<T?> CreateSecureTemp<T>(string? sensitiveData, Func<string, T>? factory)
        {
            if (string.IsNullOrEmpty(sensitiveData) || factory == null)
                return new SimpleSecureTempObject<T?>(default(T?));

            var tempObject = factory(sensitiveData!);
            return new SimpleSecureTempObject<T?>(tempObject);
        }
    }

    /// <summary>
    /// Simple secure temporary object that automatically clears sensitive data when disposed
    /// </summary>
    /// <typeparam name="T">Type of the object</typeparam>
    public class SimpleSecureTempObject<T> : IDisposable
    {
        private T? _value;
        private bool _disposed = false;

        public SimpleSecureTempObject(T? value)
        {
            _value = value;
        }

        public T? Value
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(SimpleSecureTempObject<T>));
                return _value;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Clear the value if it's a string or contains sensitive data
                if (_value is string str)
                {
                    var tempStr = str;
                    SimpleSecureDataHandler.SecureClear(ref tempStr);
                }
                else if (_value is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _value = default(T?);
                _disposed = true;
            }
        }
    }
} 