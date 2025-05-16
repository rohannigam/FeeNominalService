using System;

namespace FeeNominalService.Models.Common
{
    public class ApiResponse
    {
        public required string Message { get; set; }
        public bool Success { get; set; }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public required T Data { get; set; }
    }
} 