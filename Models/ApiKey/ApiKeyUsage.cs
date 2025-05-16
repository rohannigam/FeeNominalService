using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models.ApiKey
{
    /// <summary>
    /// Represents the usage tracking for an API key
    /// </summary>
    public class ApiKeyUsage
    {
        /// <summary>
        /// Unique identifier for the usage record
        /// </summary>
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Reference to the API key
        /// </summary>
        [Required]
        [Column("api_key_id")]
        public Guid ApiKeyId { get; set; }

        /// <summary>
        /// The endpoint that was accessed
        /// </summary>
        [Required]
        [StringLength(255)]
        [Column("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// The IP address of the client that made the request
        /// </summary>
        [Required]
        [Column(TypeName = "varchar(45)")]  // IPv6 addresses can be up to 45 characters
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Number of requests made in the current window
        /// </summary>
        [Required]
        [Column("request_count")]
        public int RequestCount { get; set; } = 1;

        /// <summary>
        /// Start of the rate limiting window
        /// </summary>
        [Required]
        [Column("window_start")]
        public DateTime WindowStart { get; set; }

        /// <summary>
        /// End of the rate limiting window
        /// </summary>
        [Required]
        [Column("window_end")]
        public DateTime WindowEnd { get; set; }

        /// <summary>
        /// When this usage record was created
        /// </summary>
        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property for the API key
        /// </summary>
        public virtual ApiKey ApiKey { get; set; } = null!;

        /// <summary>
        /// When the API key was used
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The HTTP method used in the request
        /// </summary>
        [Required]
        [Column(TypeName = "varchar(10)")]
        public string HttpMethod { get; set; } = string.Empty;

        /// <summary>
        /// The HTTP status code of the response
        /// </summary>
        [Required]
        public int StatusCode { get; set; }

        /// <summary>
        /// The response time in milliseconds
        /// </summary>
        [Required]
        public int ResponseTimeMs { get; set; }
    }
} 