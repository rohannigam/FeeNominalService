# FeeNominalService Documentation

## Overview
FeeNominalService is a microservice designed to handle surcharge calculations, merchant onboarding, and transaction processing for payment systems. The service provides a secure API for managing merchant accounts, API keys, calculating surcharges, and processing various types of transactions.

## Supported Endpoints

### Transaction Endpoints
```
/api/v1/sales/process
/api/v1/sales/process-batch
/api/v1/refunds/process
/api/v1/refunds/process-batch
/api/v1/cancel
/api/v1/cancel/batch
```

### Surcharge Endpoints (DEPRECATED - New endpoints coming soon)

**DEPRECATED ENDPOINTS:**
```
/api/v1/surchargefee/calculate
/api/v1/surchargefee/calculate-batch
```

**NEW ENDPOINTS (Coming Soon):**
```
/api/v1/surcharge/auth
/api/v1/surcharge/refund
/api/v1/surcharge/void
/api/v1/surcharge/split-shipment
/api/v1/surcharge/transactions/{id}
/api/v1/surcharge/transactions
```

**Note:** The old surcharge fee endpoints have been deprecated and will be replaced with workflow-based endpoints that integrate directly with surcharge providers (like Interpayments) and store transaction records in the database.

### Surcharge Provider Endpoints
```
/api/v1/merchants/{merchantId}/surcharge-providers
/api/v1/merchants/{merchantId}/surcharge-providers/{id}
/api/v1/merchants/{merchantId}/surcharge-providers/{id}/restore
```

### Onboarding Endpoints
```
/api/v1/onboarding/merchants
/api/v1/onboarding/merchants/{id}
/api/v1/onboarding/merchants/{id}/status
/api/v1/onboarding/merchants/{merchantId}/api-keys
/api/v1/onboarding/merchants/{merchantId}/audit-trail
/api/v1/onboarding/merchants/external/{externalMerchantId}
/api/v1/onboarding/merchants/external-guid/{externalMerchantGuid}
```

### API Key Management Endpoints (v1)
```
/api/v1/onboarding/apikey/initial-generate
/api/v1/onboarding/apikey/generate
/api/v1/onboarding/apikey/list
/api/v1/onboarding/apikey/update
/api/v1/onboarding/apikey/revoke
/api/v1/onboarding/apikey/rotate
```

### Health Check Endpoints
```
/api/v1/ping
/api/ping
```

Note: When creating an API key, you can use wildcards in the paths (e.g., `/api/v1/sales/*`) to allow all endpoints under a specific path.

## Architecture

### Core Components

1. **Controllers**
   - `OnboardingController`: Handles merchant onboarding and API key management
   - `SurchargeFeeController`: Manages surcharge fee calculations
   - `SurchargeProviderController`: Manages surcharge provider configurations
   - `SalesController`: Processes sale transactions
   - `RefundsController`: Handles refund operations
   - `CancelController`: Manages transaction cancellations
   - `PingController`: Health check endpoint

2. **Services**
   - `ApiKeyService`: Manages API key lifecycle
   - `AwsSecretsManagerService`: Handles secure storage of sensitive data
   - `RequestSigningService`: Implements request signing for security
   - `MerchantService`: Manages merchant information
   - `AuditService`: Handles audit logging
   - `SurchargeFeeService`: Calculates surcharge fees
   - `SurchargeProviderService`: Manages surcharge provider configurations
   - `SaleService`: Processes sales
   - `RefundService`: Handles refunds
   - `CancelService`: Manages cancellations

3. **Models**
   - Transaction-related models (Transaction, BatchTransaction)
   - API Key models (ApiKey, ApiKeyUsage)
   - Merchant models (Merchant, MerchantStatus)
   - Surcharge Provider models (SurchargeProvider, ProviderCredentials)
   - Request/Response models for each operation
   - Audit and logging models

### Design Patterns

The service implements several design patterns to ensure maintainability, scalability, and testability:

1. **Repository Pattern**
   - Purpose: Abstracts data access logic
   - Implementation:
     ```csharp
     public interface IApiKeyRepository
     {
         Task<ApiKey?> GetByKeyAsync(string key);
         Task<List<ApiKey>> GetByMerchantIdAsync(Guid merchantId);
         Task<ApiKey> CreateAsync(ApiKey apiKey);
         Task<ApiKey> UpdateAsync(ApiKey apiKey);
     }
     ```
   - Benefits:
     - Separates data access from business logic
     - Enables unit testing through mocking
     - Provides consistent data access interface

2. **Dependency Injection Pattern**
   - Purpose: Manages component dependencies
   - Implementation:
     ```csharp
     public class ExampleApiKeyService
     {
         private readonly IAwsSecretsManagerService _secretsManager;
         private readonly IApiKeyRepository _apiKeyRepository;
         private readonly IMerchantRepository _merchantRepository;
         private readonly ILogger<ExampleApiKeyService> _logger;
         private readonly ApiKeyConfiguration _settings;
     }
     ```
   - Benefits:
     - Loose coupling between components
     - Easier testing and maintenance
     - Flexible configuration

3. **Service Layer Pattern**
   - Purpose: Encapsulates business logic
   - Implementation:
     ```csharp
     public interface ISurchargeFeeService
     {
         Task<string> CalculateSurchargeAsync(SurchargeRequest request);
         Task<List<string>> CalculateBatchSurchargesAsync(List<SurchargeRequest> requests);
     }
     ```
   - Benefits:
     - Separation of concerns
     - Reusable business logic
     - Centralized transaction management

4. **Unit of Work Pattern**
   - Purpose: Manages database transactions
   - Implementation:
     ```csharp
     public class ApplicationDbContext : DbContext
     {
         public DbSet<Merchant> Merchants { get; set; }
         public DbSet<ApiKey> ApiKeys { get; set; }
         public DbSet<Transaction> Transactions { get; set; }
     }
     ```
   - Benefits:
     - Transaction management
     - Data consistency
     - Atomic operations

5. **Factory Pattern**
   - Purpose: Creates complex objects
   - Implementation:
     ```csharp
     public class MockAwsSecretsManagerService : IAwsSecretsManagerService
     {
         private readonly Dictionary<string, string> _mockSecrets;
         // Factory methods for creating test data
     }
     ```
   - Benefits:
     - Encapsulates object creation
     - Provides flexibility in implementation
     - Supports testing

6. **Strategy Pattern**
   - Purpose: Implements interchangeable algorithms
   - Implementation:
     ```csharp
     public interface IRequestSigningService
     {
         Task<bool> ValidateRequestAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature);
         Task<string> GenerateSignatureAsync(string merchantId, string apiKey, string timestamp, string nonce);
     }
     ```
   - Benefits:
     - Runtime flexibility
     - Easy to add new strategies
     - Clean separation of concerns

7. **Observer Pattern**
   - Purpose: Handles event notifications
   - Implementation:
     ```csharp
     public interface IAuditService
     {
         Task LogAuditAsync(string entityType, Guid entityId, string action, JsonDocument? oldValues, JsonDocument? newValues, string performedBy);
         Task<AuditLog[]> GetAuditLogsAsync(string? entityType, Guid? entityId, string? action);
     }
     ```
   - Benefits:
     - Loose coupling
     - Event-driven architecture
     - Extensible notification system

8. **Decorator Pattern**
   - Purpose: Adds functionality dynamically
   - Implementation:
     ```csharp
     public class AwsSecretsManagerService : IAwsSecretsManagerService
     {
         private readonly ILogger<AwsSecretsManagerService> _logger;
         // Logging decorator for operations
     }
     ```
   - Benefits:
     - Dynamic behavior modification
     - Single responsibility principle
     - Open/closed principle

9. **Singleton Pattern**
   - Purpose: Manages shared resources
   - Implementation:
     ```csharp
     public class ApiKeyConfiguration
     {
         public string SecretName { get; set; }
         public int DefaultRateLimit { get; set; }
     }
     ```
   - Benefits:
     - Resource sharing
     - State management
     - Global access point

10. **Command Pattern**
    - Purpose: Encapsulates requests
    - Implementation:
      ```csharp
      public interface ICancelService
      {
          Task<string> ProcessCancelAsync(CancelRequest request);
          Task<List<string>> ProcessBatchCancellationsAsync(List<CancelRequest> requests);
      }
      ```
    - Benefits:
      - Encapsulates requests
      - Supports undo operations
      - Queues and logs operations

11. **Facade Pattern**
    - Purpose: Simplifies complex subsystems
    - Implementation:
      ```csharp
      public class MerchantService
      {
          private readonly ApplicationDbContext _context;
          // Simplified interface for merchant operations
      }
      ```
    - Benefits:
      - Simplified interface
      - Reduced coupling
      - Better organization

12. **Chain of Responsibility**
    - Purpose: Processes requests in sequence
    - Implementation:
      ```csharp
      public class RequestSigningService
      {
          public async Task<bool> ValidateRequestAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature)
          {
              // Sequential validation steps
          }
      }
      ```
    - Benefits:
      - Flexible request handling
      - Separation of concerns
      - Dynamic processing chain

### Pattern Interactions

The service implements a sophisticated interaction between multiple design patterns to achieve a secure, maintainable, and scalable architecture. Here's how these patterns work together:

1. **Authentication Flow**
   - **Chain of Responsibility + Strategy Pattern**
     ```csharp
     // ApiKeyAuthHandler implements the chain
     public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
     {
         private readonly IRequestSigningService _requestSigningService;
         
         // Request validation follows a chain of checks
         protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
         {
             // 1. Check required headers
             // 2. Validate merchant
             // 3. Validate API key
             // 4. Validate signature
         }
     }
     ```
   - **Repository + Unit of Work Pattern**
     ```csharp
     // ApiKeyService coordinates between patterns
     public class ExampleApiKeyService
     {
         private readonly IApiKeyRepository _apiKeyRepository;
         private readonly ApplicationDbContext _context;
         
         public async Task<ApiKeyInfo> GenerateApiKeyAsync(GenerateApiKeyRequest request)
         {
             // Uses Unit of Work for transaction management
             using var transaction = await _context.Database.BeginTransactionAsync();
             try
             {
                 // Repository pattern for data access
                 var apiKey = await _apiKeyRepository.CreateAsync(newApiKey);
                 // Commit transaction
             }
         }
     }
     ```

2. **API Key Management**
   - **Factory + Singleton Pattern**
     ```csharp
     // Factory pattern for creating API keys
     public class ExampleApiKeyService
     {
         private string GenerateSecureRandomString(int length)
         {
             // Factory method for key generation
         }
     }
     
     // Singleton pattern for configuration
     public class ApiKeyConfiguration
     {
         public string SecretName { get; set; }
         public int DefaultRateLimit { get; set; }
     }
     ```
   - **Observer + Decorator Pattern**
     ```csharp
     // Observer pattern for audit logging
     public class AuditService : IAuditService
     {
         public async Task LogAuditAsync(string entityType, Guid entityId, string action, JsonDocument? oldValues, JsonDocument? newValues, string performedBy)
         {
             // Logs changes to API keys
         }
     }
     
     // Decorator pattern for AWS Secrets Manager
     public class AwsSecretsManagerService : IAwsSecretsManagerService
     {
         private readonly ILogger<AwsSecretsManagerService> _logger;
         
         public async Task StoreSecretAsync(string secretName, string secretValue)
         {
             // Decorates secret storage with logging
         }
     }
     ```

3. **Request Processing**
   - **Command + Service Layer Pattern**
     ```csharp
     // Command pattern for request handling
     public interface ISurchargeFeeService
     {
         Task<string> CalculateSurchargeAsync(SurchargeRequest request);
         Task<List<string>> CalculateBatchSurchargesAsync(List<SurchargeRequest> requests);
     }
     
     // Service layer for business logic
     public class SurchargeFeeService : ISurchargeFeeService
     {
         private readonly IApiKeyService _apiKeyService;
         private readonly IAuditService _auditService;
         
         public async Task<string> CalculateSurchargeAsync(SurchargeRequest request)
         {
             // Coordinates between services
         }
     }
     ```

4. **Data Access Layer**
   - **Repository + Unit of Work Pattern**
     ```csharp
     // Repository pattern for data access
     public interface IApiKeyRepository
     {
         Task<ApiKey?> GetByKeyAsync(string key);
         Task<List<ApiKey>> GetByMerchantIdAsync(Guid merchantId);
     }
     
     // Unit of Work pattern for transaction management
     public class ApplicationDbContext : DbContext
     {
         public DbSet<ApiKey> ApiKeys { get; set; }
         public DbSet<Merchant> Merchants { get; set; }
     }
     ```

5. **Security Implementation**
   - **Strategy + Chain of Responsibility Pattern**
     ```csharp
     // Strategy pattern for request signing
     public interface IRequestSigningService
     {
         Task<bool> ValidateRequestAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature);
         Task<string> GenerateSignatureAsync(string merchantId, string apiKey, string timestamp, string nonce);
     }
     
     // Chain of Responsibility for validation
     public class RequestSigningService : IRequestSigningService
     {
         public async Task<bool> ValidateRequestAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature)
         {
             // Sequential validation steps
         }
     }
     ```

6. **Service Integration**
   - **Facade + Dependency Injection Pattern**
     ```csharp
     // Facade pattern for service coordination
     public class MerchantService
     {
         private readonly IApiKeyService _apiKeyService;
         private readonly IAuditService _auditService;
         private readonly IRequestSigningService _requestSigningService;
         
         // Simplifies complex operations
         public async Task<MerchantInfo> CreateMerchantAsync(CreateMerchantRequest request)
         {
             // Coordinates between multiple services
         }
     }
     
     // Dependency Injection in Program.cs
     builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
     builder.Services.AddScoped<IRequestSigningService, RequestSigningService>();
     ```

7. **Error Handling**
   - **Chain of Responsibility + Observer Pattern**
     ```csharp
     // Chain of Responsibility for error handling
     public class ApiKeyAuthHandler
     {
         protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
         {
             try
             {
                 // Sequential error checks
             }
             catch (Exception ex)
             {
                 // Observer pattern for error logging
                 _logger.LogError(ex, "Authentication failed");
             }
         }
     }
     ```

8. **Configuration Management**
   - **Singleton + Factory Pattern**
     ```csharp
     // Singleton for configuration
     public class ApiKeyConfiguration
     {
         public string SecretName { get; set; }
         public int DefaultRateLimit { get; set; }
     }
     
     // Factory for AWS service creation
     builder.Services.AddScoped<IAwsSecretsManagerService>(sp =>
     {
         var env = sp.GetRequiredService<IHostEnvironment>();
         return env.IsDevelopment() 
             ? new MockAwsSecretsManagerService(...)
             : new AwsSecretsManagerService(...);
     });
     ```

These pattern interactions provide:
1. **Security**: Layered authentication and authorization
2. **Maintainability**: Clear separation of concerns
3. **Scalability**: Modular and extensible design
4. **Testability**: Easy to mock and test components
5. **Reliability**: Robust error handling and logging
6. **Flexibility**: Easy to modify or extend functionality

### SOLID Principles

The codebase adheres to SOLID principles:

1. **Single Responsibility Principle**
   - Each class has a single purpose
   - Example: `ApiKeyService` handles only API key operations

2. **Open/Closed Principle**
   - Classes are open for extension but closed for modification
   - Example: Service interfaces allow new implementations

3. **Liskov Substitution Principle**
   - Derived classes can substitute base classes
   - Example: Repository implementations are interchangeable

4. **Interface Segregation Principle**
   - Interfaces are focused and specific
   - Example: `IApiKeyRepository` has only API key-related methods

5. **Dependency Inversion Principle**
   - High-level modules don't depend on low-level modules
   - Example: Services depend on interfaces, not concrete implementations

### External Services Integration

1. **AWS Services**
   - AWS Secrets Manager: Secure storage of API keys and secrets
   - AWS S3: File storage (if configured)
   - AWS SQS: Message queuing (if configured)

2. **Database**
   - PostgreSQL: Primary database for storing merchant data, transactions, and audit logs

3. **Authentication**
   - Custom API key authentication
   - HMAC-based request signing
   - JWT for internal authentication (if configured)

## Database Schema

The service uses PostgreSQL with a dedicated schema `fee_nominal`. The database is designed to handle merchant management, API key management, transaction processing, and audit logging.

### Core Tables

1. **merchant_statuses**
   - Purpose: Defines possible states for merchant accounts
   - Key Fields:
     - `code`: Status identifier (e.g., 'ACTIVE', 'SUSPENDED')
     - `name`: Display name
     - `description`: Detailed status description
     - `is_active`: Whether the status is currently in use
   - Usage: Used to track merchant account lifecycle

2. **merchants**
   - Purpose: Stores merchant information
   - Key Fields:
     - `external_id`: Unique merchant identifier
     - `name`: Merchant name
     - `status_id`: Reference to merchant_statuses
     - `created_by`: User who created the merchant
   - Usage: Core merchant data storage

3. **api_keys**
   - Purpose: Manages API keys for merchant authentication
   - Key Fields:
     - `id`: Unique identifier (GUID)
     - `key`: Unique API key value
     - `merchant_id`: Reference to merchants
     - `name`: Name of the API key
     - `description`: Optional description of the API key
     - `rate_limit`: Request rate limit
     - `allowed_endpoints`: Array of permitted endpoints
     - `status`: Key status (ACTIVE, REVOKED, etc.)
     - `expires_at`: Key expiration timestamp
     - `last_used_at`: Last usage timestamp
     - `last_rotated_at`: Last rotation timestamp
     - `created_at`: Creation timestamp
     - `created_by`: User who created the key
     - `onboarding_reference`: Reference to onboarding process
     - `onboarding_timestamp`: Timestamp of onboarding
     - `purpose`: Purpose of the API key
   - Usage: API key lifecycle management

4. **api_key_usage**
   - Purpose: Tracks API key usage for rate limiting and monitoring
   - Key Fields:
     - `api_key_usage_id`: Unique identifier (GUID)
     - `api_key_id`: Reference to api_keys
     - `endpoint`: API endpoint accessed
     - `ip_address`: IP address of the request
     - `request_count`: Number of requests
     - `window_start`: Rate limit window start
     - `window_end`: Rate limit window end
     - `created_at`: Creation timestamp
     - `timestamp`: Request timestamp
     - `http_method`: HTTP method used
     - `status_code`: Response status code
     - `response_time_ms`: Response time in milliseconds
   - Usage: Rate limiting, usage monitoring, and analytics

5. **audit_logs**
   - Purpose: Comprehensive audit trail
   - Key Fields:
     - `entity_type`: Type of entity changed
     - `entity_id`: ID of changed entity
     - `action`: Type of action performed
     - `old_values`: Previous state (JSONB)
     - `new_values`: New state (JSONB)
     - `updated_by`: User who made the change
     - `ip_address`: Requester's IP
     - `user_agent`: Requester's user agent
   - Usage: Change tracking and compliance

6. **transactions**
   - Purpose: Records individual transactions
   - Key Fields:
     - `merchant_id`: Reference to merchants
     - `amount`: Transaction amount
     - `currency`: Transaction currency
     - `surcharge_amount`: Calculated surcharge
     - `total_amount`: Total with surcharge
     - `status`: Transaction status
   - Usage: Transaction processing and tracking

7. **batch_transactions**
   - Purpose: Manages batch processing
   - Key Fields:
     - `merchant_id`: Reference to merchants
     - `batch_reference`: Unique batch identifier
     - `status`: Batch processing status
     - `total_transactions`: Total transactions in batch
     - `successful_transactions`: Successful count
     - `failed_transactions`: Failed count
     - `completed_at`: Batch completion timestamp
   - Usage: Batch processing management

8. **authentication_attempts**
   - Purpose: Tracks authentication attempts
   - Key Fields:
     - `api_key_id`: Reference to api_keys
     - `ip_address`: Attempt source IP
     - `success`: Whether attempt succeeded
     - `timestamp`: Attempt timestamp
   - Usage: Security monitoring and threat detection

### Additional Tables

9. **surcharge_providers**
   - Purpose: Stores surcharge provider configurations
   - Key Fields:
     - `id`: Unique provider identifier
     - `name`: Provider name
     - `code`: Provider code
     - `description`: Provider description
     - `base_url`: Provider API base URL
     - `authentication_type`: Authentication method
     - `credentials_schema`: JSON schema for credentials
     - `status`: Provider status
     - `created_at`: Creation timestamp
     - `updated_at`: Last update timestamp
   - Usage: Surcharge provider management

10. **provider_credentials**
    - Purpose: Stores encrypted provider credentials
    - Key Fields:
      - `provider_id`: Reference to surcharge_providers
      - `credentials`: Encrypted credentials (JSONB)
      - `created_at`: Creation timestamp
      - `updated_at`: Last update timestamp
    - Usage: Secure credential storage

### Database Features

1. **Indexes**
   - Optimized for common queries
   - Covers foreign keys and frequently searched fields
   - Supports efficient rate limiting and audit queries

2. **Triggers**
   - Automatic `updated_at` timestamp management
   - Ensures data consistency
   - Maintains audit trail

3. **Constraints**
   - Foreign key relationships
   - Unique constraints on critical fields
   - Not null constraints on required fields

4. **Data Types**
   - UUID for primary keys
   - TIMESTAMP WITH TIME ZONE for dates
   - JSONB for flexible data storage
   - DECIMAL(19,4) for monetary values

### Development Data

The database includes test data for development:
- Default merchant statuses
- Test merchant (DEV001)
- Test API key with limited permissions

### Best Practices

1. **Data Integrity**
   - Use transactions for related operations
   - Maintain referential integrity
   - Validate data before insertion

2. **Performance**
   - Use appropriate indexes
   - Monitor query performance
   - Regular maintenance

3. **Security**
   - Encrypt sensitive data
   - Use parameterized queries
   - Implement proper access controls

4. **Backup and Recovery**
   - Regular backups
   - Point-in-time recovery
   - Disaster recovery plan

### Example Queries

1. **Merchant Management**
   ```sql
   -- Get active merchants with their status
   SELECT m.external_id, m.name, ms.code as status
   FROM merchants m
   JOIN merchant_statuses ms ON m.status_id = ms.id
   WHERE ms.code = 'ACTIVE';

   -- Get merchant's API keys
   SELECT ak.key, ak.description, ak.rate_limit, ak.allowed_endpoints
   FROM api_keys ak
   JOIN merchants m ON ak.merchant_id = m.id
   WHERE m.external_id = 'DEV001'
   AND ak.status = 'ACTIVE';
   ```

2. **API Key Usage and Rate Limiting**
   ```sql
   -- Check API key usage within rate limit window
   SELECT 
       ak.key,
       SUM(aku.request_count) as total_requests,
       ak.rate_limit
   FROM api_keys ak
   JOIN api_key_usage aku ON ak.id = aku.api_key_id
   WHERE ak.key = 'test_api_key'
   AND aku.window_start >= CURRENT_TIMESTAMP - INTERVAL '1 hour'
   GROUP BY ak.key, ak.rate_limit;

   -- Get failed authentication attempts
   SELECT 
       aa.ip_address,
       COUNT(*) as attempt_count
   FROM authentication_attempts aa
   WHERE aa.success = false
   AND aa.timestamp >= CURRENT_TIMESTAMP - INTERVAL '24 hours'
   GROUP BY aa.ip_address
   HAVING COUNT(*) > 5;
   ```

3. **Transaction Processing**
   ```sql
   -- Get transaction summary for a merchant
   SELECT 
       DATE_TRUNC('day', t.created_at) as transaction_date,
       COUNT(*) as total_transactions,
       SUM(t.amount) as total_amount,
       SUM(t.surcharge_amount) as total_surcharge
   FROM transactions t
   JOIN merchants m ON t.merchant_id = m.id
   WHERE m.external_id = 'DEV001'
   GROUP BY DATE_TRUNC('day', t.created_at)
   ORDER BY transaction_date DESC;

   -- Get batch transaction status
   SELECT 
       bt.batch_reference,
       bt.status,
       bt.total_transactions,
       bt.successful_transactions,
       bt.failed_transactions,
       bt.completed_at
   FROM batch_transactions bt
   JOIN merchants m ON bt.merchant_id = m.id
   WHERE m.external_id = 'DEV001'
   AND bt.created_at >= CURRENT_TIMESTAMP - INTERVAL '7 days';
   ```

4. **Audit and Compliance**
   ```sql
   -- Get recent changes to API keys
   SELECT 
       al.performed_at,
       al.action,
       al.old_values,
       al.new_values,
       al.updated_by,
       al.ip_address
   FROM audit_logs al
   WHERE al.entity_type = 'API_KEY'
   AND al.performed_at >= CURRENT_TIMESTAMP - INTERVAL '24 hours'
   ORDER BY al.performed_at DESC;

   -- Get merchant status changes
   SELECT 
       m.external_id,
       al.action,
       al.old_values->>'status' as old_status,
       al.new_values->>'status' as new_status,
       al.performed_at
   FROM audit_logs al
   JOIN merchants m ON al.entity_id = m.id
   WHERE al.entity_type = 'MERCHANT'
   AND al.action = 'UPDATE'
   ORDER BY al.performed_at DESC;
   ```

5. **Surcharge Provider Management**
   ```sql
   -- Get active surcharge providers
   SELECT 
       sp.name,
       sp.code,
       sp.base_url,
       sp.authentication_type,
       sp.status
   FROM surcharge_providers sp
   WHERE sp.status = 'active';

   -- Get provider credentials (admin only)
   SELECT 
       sp.name,
       pc.credentials,
       pc.updated_at
   FROM surcharge_providers sp
   JOIN provider_credentials pc ON sp.id = pc.provider_id
   WHERE sp.code = 'INTERPAY';
   ```

### AWS RDS Deployment

The database will be deployed to AWS RDS PostgreSQL instance. Here's the deployment process:

1. **Infrastructure Setup**
   - Create AWS RDS instance
     ```bash
     # Example AWS CLI command
     aws rds create-db-instance \
         --db-instance-identifier fee-nominal-db \
         --db-instance-class db.t3.micro \
         --engine postgres \
         --master-username admin \
         --master-user-password <password> \
         --allocated-storage 20
     ```
   - Configure VPC and security groups
   - Set up parameter groups for PostgreSQL optimization

2. **Jenkins Pipeline Setup**
   ```groovy
   pipeline {
       agent any
       environment {
           DB_HOST = credentials('DB_HOST')
           DB_NAME = credentials('DB_NAME')
           DB_USER = credentials('DB_USER')
           DB_PASSWORD = credentials('DB_PASSWORD')
       }
       stages {
           stage('Database Migration') {
               steps {
                   sh '''
                       # Run database migrations
                       dotnet ef database update
                   '''
               }
           }
           stage('Data Seeding') {
               steps {
                   sh '''
                       # Seed initial data
                       psql -h $DB_HOST -U $DB_USER -d $DB_NAME -f init.sql
                   '''
               }
           }
       }
   }
   ```

3. **OpenShift Configuration**
   ```yaml
   # Database deployment configuration
   apiVersion: apps.openshift.io/v1
   kind: DeploymentConfig
   metadata:
     name: fee-nominal-db
   spec:
     template:
       spec:
         containers:
         - name: postgres
           image: postgres:latest
           env:
           - name: POSTGRES_DB
             value: fee_nominal
           - name: POSTGRES_USER
             valueFrom:
               secretKeyRef:
                 name: db-credentials
                 key: username
           - name: POSTGRES_PASSWORD
             valueFrom:
               secretKeyRef:
                 name: db-credentials
                 key: password
   ```

4. **Deployment Steps**
   1. **Preparation**
      - Create AWS RDS instance
      - Configure network security
      - Set up monitoring and alerts
      - Create backup strategy

   2. **Database Migration**
      - Export existing schema
      - Create migration scripts
      - Test migrations in staging
      - Plan rollback procedures

   3. **Jenkins Pipeline**
      - Set up CI/CD pipeline
      - Configure environment variables
      - Add database migration steps
      - Implement rollback procedures

   4. **OpenShift Deployment**
      - Create deployment configuration
      - Set up secrets for credentials
      - Configure persistent storage
      - Set up health checks

   5. **Post-Deployment**
      - Verify database connectivity
      - Run data integrity checks
      - Monitor performance
      - Set up automated backups

5. **Monitoring and Maintenance**
   - Set up CloudWatch metrics
   - Configure RDS performance insights
   - Implement automated backup schedule
   - Set up alerting for critical issues

6. **Security Considerations**
   - Encrypt data at rest
   - Enable SSL connections
   - Implement network security
   - Regular security audits

7. **Backup and Recovery**
   - Automated daily backups
   - Point-in-time recovery
   - Cross-region replication
   - Disaster recovery testing

## API Endpoints

### Onboarding Endpoints (v1)

#### 1. Generate Initial API Key
- **Endpoint**: `POST /api/v1/onboarding/apikey/initial-generate`
- **Description**: Generates the first API key for a merchant
- **Authentication**: None (initial key generation)
- **Request Body**:
  ```json
  {
    "merchantId": "string",
    "description": "string",          // Optional
    "rateLimit": "integer",
    "allowedEndpoints": ["string"],
    "purpose": "string",
    "merchantName": "string",
    "onboardingMetadata": {           // Required
      "adminUserId": "string",        // Required
      "onboardingReference": "string", // Required
      "onboardingTimestamp": "datetime" // Optional, defaults to UTC now
    }
  }
  ```
- **Response**:
  ```json
  {
    "apiKey": "string",
    "secret": "string",
    "expiresAt": "datetime",
    "rateLimit": "integer",
    "allowedEndpoints": ["string"],
    "purpose": "string",
    "description": "string",
    "onboardingMetadata": {
      "adminUserId": "string",
      "onboardingReference": "string",
      "onboardingTimestamp": "datetime"
    }
  }
  ```

#### 2. Generate Additional API Key
- **Endpoint**: `POST /api/v1/onboarding/apikey/generate`
- **Description**: Generates additional API keys for a merchant
- **Authentication**: Required (X-API-Key header)
- **Request Body**:
  ```json
  {
    "merchantId": "string",
    "description": "string",          // Optional
    "rateLimit": "integer",
    "allowedEndpoints": ["string"],
    "purpose": "string",
    "onboardingMetadata": {           // Required
      "adminUserId": "string",        // Required
      "onboardingReference": "string", // Required
      "onboardingTimestamp": "datetime" // Optional, defaults to UTC now
    }
  }
  ```
- **Response**: Same as Initial API Key generation

#### 3. List API Keys
- **Endpoint**: `GET /api/v1/onboarding/apikey/list`
- **Description**: Retrieves all API keys for a merchant
- **Authentication**: Required (X-API-Key header)
- **Query Parameters**:
  - `merchantId`: string (required)
- **Response**:
  ```json
  [
    {
      "apiKey": "string",
      "description": "string",        // Optional, defaults to empty string
      "rateLimit": "integer",
      "allowedEndpoints": ["string"],
      "status": "string",
      "createdAt": "datetime",
      "lastRotatedAt": "datetime",
      "lastUsedAt": "datetime",
      "revokedAt": "datetime",
      "expiresAt": "datetime",
      "isRevoked": "boolean",
      "isExpired": "boolean",
      "usageCount": "integer",
      "onboardingMetadata": {
        "adminUserId": "string",
        "onboardingReference": "string",
        "onboardingTimestamp": "datetime"
      }
    }
  ]
  ```

#### 4. Update API Key
- **Endpoint**: `POST /api/v1/onboarding/apikey/update`
- **Description**: Updates an existing API key's properties
- **Authentication**: Required (X-API-Key header)
- **Request Body**:
  ```json
  {
    "merchantId": "string",
    "apiKey": "string",
    "description": "string",          // Optional
    "rateLimit": "integer",
    "allowedEndpoints": ["string"],
    "onboardingMetadata": {           // Required
      "adminUserId": "string",        // Required
      "onboardingReference": "string", // Required
      "onboardingTimestamp": "datetime" // Optional, defaults to UTC now
    }
  }
  ```
- **Response**: Same as List API Keys response format

#### 5. Revoke API Key
- **Endpoint**: `POST /api/v1/onboarding/apikey/revoke`
- **Description**: Revokes an existing API key
- **Authentication**: Required (X-API-Key header)
- **Request Body**:
  ```json
  {
    "merchantId": "string",
    "apiKey": "string"
  }
  ```
- **Response**:
  ```json
  {
    "success": "boolean",
    "message": "string"
  }
  ```

#### 6. Rotate API Key
- **Endpoint**: `POST /api/v1/onboarding/apikey/rotate`
- **Description**: Rotates (replaces) an existing API key with a new one while maintaining the same secret
- **Authentication**: Required (X-API-Key header)
- **Request Body**:
  ```json
  {
    "merchantId": "string",
    "apiKey": "string",
    "onboardingMetadata": {           // Required
      "adminUserId": "string",        // Required
      "onboardingReference": "string", // Required
      "onboardingTimestamp": "datetime" // Optional, defaults to UTC now
    }
  }
  ```
- **Response**: Same as List API Keys response format

### Merchant Management Endpoints (v1)

#### 1. Create Merchant
- **Endpoint**: `POST /api/v1/onboarding/merchants`
- **Description**: Creates a new merchant
- **Authentication**: Required (X-API-Key header)
- **Request Body**: `Merchant` object
- **Response**: Created `Merchant` object

#### 2. Get Merchant
- **Endpoint**: `GET /api/v1/onboarding/merchants/{id}`
- **Description**: Retrieves merchant information
- **Authentication**: Required (X-API-Key header)
- **Path Parameters**:
  - `id`: Guid (required)
- **Response**: `Merchant` object

#### 3. Update Merchant Status
- **Endpoint**: `POST /api/v1/onboarding/merchants/{id}/status`
- **Description**: Updates a merchant's status
- **Authentication**: Required (X-API-Key header)
- **Path Parameters**:
  - `id`: Guid (required)
- **Request Body**:
  ```json
  {
    "statusId": "guid"
  }
  ```
- **Response**: Updated `Merchant` object

### Surcharge Fee Endpoints (v1)

#### 1. Calculate Surcharge Fee
- **Endpoint**: `POST /api/v1/surchargefee/calculate`
- **Description**: Calculates surcharge fee for a transaction
- **Authentication**: Required (X-API-Key header)
- **Request Body**:
  ```json
  {
    "amount": "decimal",
    "sTxId": "string",
    "mTxId": "string",
    "country": "string",
    "region": "string"
  }
  ```
- **Response**:
  ```json
  {
    "surchargeAmount": "decimal",
    "totalAmount": "decimal",
    "sTxId": "string",
    "mTxId": "string",
    "provider": "string",
    "calculatedAt": "datetime"
  }
  ```

#### 2. Calculate Batch Surcharge Fee
- **Endpoint**: `POST /api/v1/surchargefee/calculate-batch`
- **Description**: Calculates surcharge fees for multiple transactions
- **Authentication**: Required (X-API-Key header)
- **Request Body**: Array of surcharge fee requests
- **Response**: Array of surcharge fee responses

### Sales Endpoints (v1)

#### 1. Process Sale
- **Endpoint**: `POST /api/v1/sales/process`
- **Description**: Processes a sale transaction
- **Authentication**: Required (X-API-Key header)
- **Request Body**:
  ```json
  {
    "sTxId": "string",
    "amount": "decimal",
    "currency": "string",
    "mTxId": "string"
  }
  ```
- **Response**:
  ```json
  {
    "sTxId": "string",
    "mTxId": "string",
    "amount": "decimal",
    "currency": "string",
    "status": "string",
    "processedAt": "datetime"
  }
  ```

#### 2. Process Batch Sales
- **Endpoint**: `POST /api/v1/sales/process-batch`
- **Description**: Processes multiple sale transactions
- **Authentication**: Required (X-API-Key header)
- **Request Body**: Array of sale requests
- **Response**: Array of sale responses

### Refund Endpoints (v1)

#### 1. Process Refund
- **Endpoint**: `POST /api/v1/refunds/process`
- **Description**: Processes a refund transaction
- **Authentication**: Required (X-API-Key header)
- **Request Body**:
  ```json
  {
    "sTxId": "string",
    "amount": "decimal",
    "mTxId": "string",
    "cardToken": "string"
  }
  ```
- **Response**:
  ```json
  {
    "sTxId": "string",
    "mTxId": "string",
    "amount": "decimal",
    "status": "string",
    "processedAt": "datetime"
  }
  ```

#### 2. Process Batch Refunds
- **Endpoint**: `POST /api/v1/refunds/process-batch`
- **Description**: Processes multiple refund transactions
- **Authentication**: Required (X-API-Key header)
- **Request Body**: Array of refund requests
- **Response**: Array of refund responses

### Cancellation Endpoints (v1)

#### 1. Process Cancel
- **Endpoint**: `POST /api/v1/cancel`
- **Description**: Processes a cancellation transaction
- **Authentication**: Required (X-API-Key header)
- **Request Body**:
  ```json
  {
    "sTxId": "string",
    "sAmount": "string",
    "sCurrency": "string",
    "sDescription": "string",
    "sReference": "string"
  }
  ```

#### 2. Process Batch Cancellations
- **Endpoint**: `POST /api/v1/cancel/batch`
- **Description**: Processes multiple cancellation transactions
- **Authentication**: Required (X-API-Key header)
- **Request Body**: Array of cancellation requests

### Health Check Endpoints (v1)

#### 1. Ping
- **Endpoint**: `GET /api/v1/ping`
- **Description**: Checks if the API is up and running
- **Authentication**: None

### Surcharge Provider Endpoints (v1)

#### 1. Create Surcharge Provider
- **Endpoint**: `POST /api/v1/merchants/{merchantId}/surcharge-providers`
- **Description**: Creates a new surcharge provider configuration
- **Authentication**: Required (X-API-Key header + X-Merchant-ID header)
- **Path Parameters**:
  - `merchantId`: string (required)
- **Required Headers**:
  - `X-Merchant-ID`: Merchant ID (must match URL parameter)
  - `X-Timestamp`: Current UTC timestamp
  - `X-Nonce`: Unique string for request
  - `X-API-Key`: Valid API key for authentication
  - `X-Signature`: HMAC-SHA256 signature of the request
- **Request Body**:
  ```json
  {
    "name": "string",                // Required, max 100 chars
    "code": "string",                // Required, max 50 chars, unique per merchant
    "description": "string",         // Optional, max 500 chars
    "baseUrl": "string",            // Required, max 200 chars
    "authenticationType": "string",  // Required, max 50 chars
    "credentialsSchema": {           // Required
        "required_fields": [
            {
                "name": "string",
                "type": "string",
                "description": "string"
            }
        ],
        "optional_fields": [
            {
                "name": "string",
                "type": "string",
                "description": "string"
            }
        ]
    },
    "statusCode": "string",         // Optional, defaults to "ACTIVE"
    "configuration": {              // Optional
        "ConfigName": "string",     // Required if configuration provided
        "credentials": {            // Required if configuration provided
            "field_name": "value"
        },
        "timeout": 30,              // Optional, 1-300 seconds
        "retryCount": 3,            // Optional, 0-10
        "retryDelay": 5,            // Optional, 1-60 seconds
        "rateLimit": 1000,          // Optional, 1-10000
        "rateLimitPeriod": 3600,    // Optional, 1-3600 seconds
        "metadata": {}              // Optional, JSON object
    }
  }
  ```
- **Response**:
  ```json
  {
    "id": "guid",
    "name": "string",
    "code": "string",
    "description": "string",
    "baseUrl": "string",
    "authenticationType": "string",
    "credentialsSchema": {
        "required_fields": [...],
        "optional_fields": [...]
    },
    "status": "string",
    "createdAt": "datetime",
    "updatedAt": "datetime",
    "createdBy": "string",
    "updatedBy": "string",
    "configuration": {
        "id": "guid",
        "configName": "string",
        "isActive": true,
        "isPrimary": true,
        "credentials": {
            "field_name": "value"
        },
        "timeout": 30,
        "retryCount": 3,
        "retryDelay": 5,
        "rateLimit": 1000,
        "rateLimitPeriod": 3600,
        "metadata": {},
        "createdAt": "datetime",
        "updatedAt": "datetime",
        "lastUsedAt": "datetime",
        "lastSuccessAt": "datetime",
        "lastErrorAt": "datetime",
        "lastErrorMessage": "string",
        "successCount": 0,
        "errorCount": 0,
        "averageResponseTime": 150.5
    }
  }
  ```

#### 2. Get All Surcharge Providers
- **Endpoint**: `GET /api/v1/merchants/{merchantId}/surcharge-providers`
- **Description**: Retrieves all surcharge providers created by the specified merchant
- **Authentication**: Required (X-API-Key header + X-Merchant-ID header)
- **Path Parameters**:
  - `merchantId`: string (required)
- **Required Headers**:
  - `X-Merchant-ID`: Merchant ID (must match URL parameter)
  - `X-Timestamp`: Current UTC timestamp
  - `X-Nonce`: Unique string for request
  - `X-API-Key`: Valid API key for authentication
  - `X-Signature`: HMAC-SHA256 signature of the request
- **Response**: Array of surcharge provider objects with configurations

#### 3. Get Surcharge Provider by ID
- **Endpoint**: `GET /api/v1/merchants/{merchantId}/surcharge-providers/{id}`
- **Description**: Retrieves a specific surcharge provider by ID. Only returns providers created by the specified merchant.
- **Authentication**: Required (X-API-Key header + X-Merchant-ID header)
- **Path Parameters**:
  - `merchantId`: string (required)
  - `id`: Guid (required)
- **Required Headers**:
  - `X-Merchant-ID`: Merchant ID (must match URL parameter)
  - `X-Timestamp`: Current UTC timestamp
  - `X-Nonce`: Unique string for request
  - `X-API-Key`: Valid API key for authentication
  - `X-Signature`: HMAC-SHA256 signature of the request
- **Response**: Surcharge provider object with configuration

#### 4. Update Surcharge Provider
- **Endpoint**: `PUT /api/v1/merchants/{merchantId}/surcharge-providers/{id}`
- **Description**: Updates an existing surcharge provider. Only allows updates to providers created by the specified merchant.
- **Authentication**: Required (X-API-Key header + X-Merchant-ID header)
- **Path Parameters**:
  - `merchantId`: string (required)
  - `id`: Guid (required)
- **Required Headers**:
  - `X-Merchant-ID`: Merchant ID (must match URL parameter)
  - `X-Timestamp`: Current UTC timestamp
  - `X-Nonce`: Unique string for request
  - `X-API-Key`: Valid API key for authentication
  - `X-Signature`: HMAC-SHA256 signature of the request
- **Request Body**: Same as Create Surcharge Provider
- **Response**: Updated surcharge provider object with configuration

#### 5. Delete Surcharge Provider (Soft Delete)
- **Endpoint**: `DELETE /api/v1/merchants/{merchantId}/surcharge-providers/{id}`
- **Description**: Soft deletes a surcharge provider by setting its status to "DELETED". Only allows deletion of providers created by the specified merchant.
- **Authentication**: Required (X-API-Key header + X-Merchant-ID header)
- **Path Parameters**:
  - `merchantId`: string (required)
  - `id`: Guid (required)
- **Required Headers**:
  - `X-Merchant-ID`: Merchant ID (must match URL parameter)
  - `X-Timestamp`: Current UTC timestamp
  - `X-Nonce`: Unique string for request
  - `X-API-Key`: Valid API key for authentication
  - `X-Signature`: HMAC-SHA256 signature of the request
- **Response**:
  ```json
  {
    "id": "guid",
    "name": "string",
    "code": "string",
    "description": "string",
    "baseUrl": "string",
    "authenticationType": "string",
    "credentialsSchema": {...},
    "status": "DELETED",
    "createdAt": "datetime",
    "updatedAt": "datetime",
    "createdBy": "string",
    "updatedBy": "string",
    "configuration": {...}
  }
  ```

#### 6. Restore Surcharge Provider
- **Endpoint**: `POST /api/v1/merchants/{merchantId}/surcharge-providers/{id}/restore`
- **Description**: Restores a soft-deleted surcharge provider by setting its status back to "ACTIVE". Only allows restoration of providers created by the specified merchant.
- **Authentication**: Required (X-API-Key header + X-Merchant-ID header)
- **Path Parameters**:
  - `merchantId`: string (required)
  - `id`: Guid (required)
- **Required Headers**:
  - `X-Merchant-ID`: Merchant ID (must match URL parameter)
  - `X-Timestamp`: Current UTC timestamp
  - `X-Nonce`: Unique string for request
  - `X-API-Key`: Valid API key for authentication
  - `X-Signature`: HMAC-SHA256 signature of the request
- **Response**:
  ```json
  {
    "id": "guid",
    "name": "string",
    "code": "string",
    "description": "string",
    "baseUrl": "string",
    "authenticationType": "string",
    "credentialsSchema": {...},
    "status": "ACTIVE",
    "createdAt": "datetime",
    "updatedAt": "datetime",
    "createdBy": "string",
    "updatedBy": "string",
    "configuration": {...}
  }
  ```

### Example: Interpayments Provider Configuration

```json
{
    "name": "Interpayments",
    "code": "INTERPAY",
    "description": "Interpayments Surcharge Provider for payment processing",
    "baseUrl": "https://api.interpayments.com/v1",
    "authenticationType": "JWT",
    "credentialsSchema": {
        "required_fields": [
            {
                "name": "client_id",
                "type": "string",
                "description": "Interpayments Client ID for OAuth2 authentication"
            },
            {
                "name": "client_secret",
                "type": "string",
                "description": "Interpayments Client Secret for OAuth2 authentication"
            },
            {
                "name": "audience",
                "type": "string",
                "description": "JWT Audience claim for token validation"
            },
            {
                "name": "issuer",
                "type": "string",
                "description": "JWT Issuer claim for token validation"
            }
        ],
        "optional_fields": [
            {
                "name": "scope",
                "type": "string",
                "description": "OAuth2 scope for API access"
            },
            {
                "name": "token_endpoint",
                "type": "string",
                "description": "Custom OAuth2 token endpoint URL"
            }
        ]
    },
    "configuration": {
        "ConfigName": "Interpayments Production",
        "credentials": {
            "client_id": "your-client-id",
            "client_secret": "your-client-secret",
            "audience": "https://api.interpayments.com",
            "issuer": "https://interpayments.auth0.com",
            "scope": "read:transactions write:transactions"
        },
        "timeout": 30,
        "retryCount": 3,
        "retryDelay": 5,
        "rateLimit": 1000,
        "rateLimitPeriod": 3600,
        "metadata": {
            "environment": "production",
            "version": "1.0"
        }
    }
}
```

### Credentials Schema Validation

The service includes comprehensive validation for credentials schema and configuration:

1. **Field Validation**
   - Required fields must be present in configuration
   - Field types are validated (string, email, url, jwt, api_key, etc.)
   - Field length limits are enforced
   - Custom validation rules for specific field types

2. **Configuration Validation**
   - Timeout: 1-300 seconds
   - Retry count: 0-10 attempts
   - Retry delay: 1-60 seconds
   - Rate limit: 1-10,000 requests
   - Rate limit period: 1-3,600 seconds

3. **Supported Authentication Types**
   - JWT: JSON Web Token authentication
   - API_KEY: API key-based authentication
   - OAUTH2: OAuth 2.0 authentication
   - BASIC_AUTH: Basic authentication
   - CUSTOM: Custom authentication schemes

4. **Field Type Validation**
   - JWT: Validates JWT token format
   - API_KEY: Validates API key format
   - EMAIL: Validates email address format
   - URL: Validates URL format
   - CERTIFICATE: Validates certificate format
   - BASE64: Validates base64 encoding
   - JSON: Validates JSON format

### Provider Code Uniqueness

Provider codes are unique per merchant, allowing different merchants to use the same provider code (e.g., "INTERPAY") without conflicts. The system enforces this through a composite unique constraint on (code, created_by).

## Security Features

1. **API Key Management**
   - Secure key generation using `IApiKeyGenerator`
   - Key rotation support with `RotateApiKeyAsync`
   - Rate limiting per API key with configurable limits
   - Endpoint restrictions via `AllowedEndpoints`
   - Key expiration with `ExpiresAt`
   - Key revocation with audit trail
   - Secret storage in AWS Secrets Manager
   - Usage tracking and monitoring
   - Onboarding metadata tracking
   - Optional description field for better key management
   - Last used timestamp tracking
   - Response time monitoring
   - IP address tracking for security analysis

2. **Request Signing**
   - HMAC-based request signing using SHA-256
   - Required headers:
     - X-Merchant-ID: Merchant identifier (must match URL parameter for merchant-specific endpoints)
     - X-API-Key: API key value
     - X-Timestamp: Current UTC timestamp
     - X-Nonce: Unique request identifier
     - X-Signature: HMAC signature
   - Signature calculation:
     - Data format: `timestamp|nonce|merchantId|apiKey`
     - HMAC-SHA256 with secret key
     - Base64 encoded result
   - Timestamp validation to prevent replay attacks
   - Nonce tracking to prevent request duplication

3. **Authentication & Authorization**
   - API key authentication via `ApiKeyAuthHandler`
   - Merchant-specific access control via X-Merchant-ID header
   - Role-based access control
   - Endpoint-level authorization
   - Merchant ownership validation for surcharge providers
   - Admin role requirements for sensitive operations
   - Usage count tracking for billing and monitoring
   - Response time monitoring for performance analysis
   - IP address tracking for security monitoring

4. **Audit Logging**
   - Comprehensive change tracking
   - JSON-based value storage
   - User action tracking
   - IP address logging
   - Request/response logging middleware
   - Detailed error logging
   - API key usage tracking
   - Response time monitoring
   - Onboarding metadata tracking
   - Key rotation history
   - Usage patterns analysis
   - Merchant-specific audit trails

5. **Data Protection**
   - Soft delete for surcharge providers (status-based deletion)
   - Merchant isolation for provider data
   - Encrypted credential storage
   - Secure configuration management
   - Validation of all input data
   - Protection against SQL injection
   - XSS prevention through proper encoding

## Integration Guide

### 1. Initial Setup

1. **Generate Initial API Key**
   ```http
   POST /api/v1/onboarding/apikey/initial-generate
   Content-Type: application/json

   {
     "merchantId": "your-merchant-id",
     "description": "Initial API key",
     "rateLimit": 1000,
     "allowedEndpoints": ["surchargefee/calculate", "surchargefee/calculate-batch"],
     "onboardingMetadata": {
       "adminUserId": "admin-user-id",
       "onboardingReference": "onboarding-ref",
       "onboardingTimestamp": "2024-03-14T12:00:00Z"
     }
   }
   ```

2. **Store Credentials**
   - Save the returned API key and secret securely
   - Never expose the secret in client-side code
   - Use secure storage for credentials
   - Implement key rotation policy

### 2. Making Authenticated Requests

1. **Generate Request Headers**
   ```csharp
   // Generate timestamp and nonce
   var timestamp = DateTime.UtcNow.ToString("O");
   var nonce = Guid.NewGuid().ToString();

   // Read request body
   var requestBody = JsonSerializer.Serialize(request);

   // Generate signature
   var dataToSign = $"{timestamp}{nonce}{requestBody}";
   using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
   var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign)));

   // Set headers
   request.Headers.Add("X-Merchant-ID", merchantId);
   request.Headers.Add("X-API-Key", apiKey);
   request.Headers.Add("X-Timestamp", timestamp);
   request.Headers.Add("X-Nonce", nonce);
   request.Headers.Add("X-Signature", signature);
   ```

2. **Handle Rate Limiting**
   - Monitor rate limit headers
   - Implement exponential backoff
   - Handle 429 Too Many Requests responses
   - Cache responses when appropriate

3. **Error Handling**
   ```csharp
   try
   {
       var response = await client.SendAsync(request);
       if (!response.IsSuccessStatusCode)
       {
           var error = await response.Content.ReadFromJsonAsync<ApiResponse>();
           switch (response.StatusCode)
           {
               case HttpStatusCode.BadRequest:
                   // Handle validation errors
                   break;
               case HttpStatusCode.Unauthorized:
                   // Handle authentication errors
                   break;
               case HttpStatusCode.Forbidden:
                   // Handle authorization errors
                   break;
               case HttpStatusCode.TooManyRequests:
                   // Handle rate limiting
                   break;
               default:
                   // Handle other errors
                   break;
           }
       }
   }
   catch (Exception ex)
   {
       // Handle network or other errors
   }
   ```

### 3. Best Practices

1. **Security**
   - Use HTTPS for all requests
   - Implement proper key rotation
   - Monitor API key usage
   - Implement request signing
   - Validate all responses
   - Handle errors gracefully

2. **Performance**
   - Implement request caching
   - Use batch endpoints when possible
   - Monitor rate limits
   - Implement retry logic
   - Use connection pooling

3. **Monitoring**
   - Track API key usage
   - Monitor error rates
   - Log all requests
   - Set up alerts
   - Track response times

4. **Development**
   - Use environment variables
   - Implement proper logging
   - Write unit tests
   - Use mock services
   - Follow security guidelines

## Development Environment

1. **Local Setup**
   - Use Docker for PostgreSQL
   - Configure mock AWS services
   - Set environment to Development

2. **Testing**
   - Use Postman collection for API testing
   - Environment variables for different scenarios
   - Pre-request scripts for authentication

## Production Environment

1. **Deployment**
   - Containerized deployment
   - AWS infrastructure
   - Load balancing
   - Auto-scaling

2. **Monitoring**
   - Health checks
   - Performance metrics
   - Error tracking
   - Usage statistics

## Best Practices

1. **API Usage**
   - Implement proper error handling
   - Use appropriate rate limits
   - Monitor API key usage
   - Rotate keys regularly

2. **Security**
   - Use HTTPS
   - Implement request signing
   - Validate timestamps
   - Track authentication attempts

3. **Development**
   - Use mock services in development
   - Follow security guidelines
   - Implement proper error handling
   - Maintain audit logs

# Request Signing
The service implements a secure request signing mechanism for all API requests (except initial API key generation). The signature is calculated using the following components:

1. **Timestamp**: Current UTC time in ISO 8601 format
2. **Nonce**: A unique random string for each request
3. **MerchantId**: Your merchant ID (must match X-Merchant-ID header and URL parameter for merchant-specific endpoints)
4. **ApiKey**: Your API key
5. **Secret**: The API key secret (not the API key itself)

## Signature Calculation
1. Concatenate the following fields using pipe separator: `timestamp|nonce|merchantId|apiKey`
2. Create HMAC-SHA256 hash using the secret key
3. Base64 encode the hash

## Required Headers
- `X-Merchant-ID`: Your merchant ID (required for merchant-specific endpoints)
- `X-API-Key`: Your API key
- `X-Timestamp`: Current UTC time in ISO 8601 format
- `X-Nonce`: Unique random string
- `X-Signature`: The calculated signature

## Example
```javascript
// Generate timestamp and nonce
const timestamp = new Date().toISOString();
const nonce = Math.random().toString(36).substring(2, 15);

// Create data string to sign
const dataToSign = `${timestamp}|${nonce}|${merchantId}|${apiKey}`;

// Generate HMAC-SHA256 signature
const signature = CryptoJS.HmacSHA256(dataToSign, secret).toString(CryptoJS.enc.Base64);

// Set headers
headers.add('X-Merchant-ID', merchantId);
headers.add('X-API-Key', apiKey);
headers.add('X-Timestamp', timestamp);
headers.add('X-Nonce', nonce);
headers.add('X-Signature', signature);
```

## Security Features
1. **Timestamp Validation**: Requests must be within a configurable time window
2. **Nonce Tracking**: Prevents request replay attacks
3. **HMAC-SHA256**: Strong cryptographic signing
4. **Base64 Encoding**: Standard encoding for signature transmission
5. **Merchant ID Validation**: X-Merchant-ID header must match URL parameter for merchant-specific endpoints
6. **Request Body Validation**: Signature includes request body for POST/PUT requests