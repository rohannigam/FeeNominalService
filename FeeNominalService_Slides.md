# FeeNominalService - Comprehensive Overview
## Enterprise Payment Processing Microservice Architecture

---

## Slide 1: Executive Summary
### **FeeNominalService Overview**
- **Purpose**: Enterprise-grade payment processing microservice for surcharge calculations, merchant management, and provider integrations
- **Technology Stack**: .NET 8.0, PostgreSQL, AWS Services, Entity Framework Core, Serilog, Swagger/OpenAPI
- **Architecture**: Clean Architecture with Repository Pattern, Dependency Injection
- **Security**: API Key Authentication, HMAC Request Signing, AWS Secrets Manager
- **Integration**: InterPayments API, Extensible Provider Adapter System, SOAP Web Services

---

## Slide 2: System Architecture Overview
### **Multi-Tier Architecture with External Integrations**

```mermaid
graph TB
    subgraph "External Systems"
        OP[Onboarding Portal]
        SW[SOAP Web Services]
        IP[InterPayments API]
    end
    
    subgraph "FeeNominalService"
        API[API Gateway Layer]
        AUTH[Authentication & Authorization]
        BL[Business Logic Layer]
        DAL[Data Access Layer]
    end
    
    subgraph "Infrastructure"
        DB[(PostgreSQL)]
        AWS[AWS Services]
        LOG[Logging & Monitoring]
    end
    
    OP -->|Merchant Registration| API
    SW -->|Surcharge Transactions| API
    IP -->|Provider Integration| BL
    
    API --> AUTH
    AUTH --> BL
    BL --> DAL
    DAL --> DB
    
    BL --> AWS
    BL --> LOG
```

---

## Slide 3: Technical Architecture - Internal System Components
### **Detailed Internal Component Architecture**

```mermaid
graph TB
    subgraph "API Gateway Layer"
        OC[OnboardingController]
        SC[SurchargeController]
        SPC[SurchargeProviderController]
        HC[HealthController]
    end
    
    subgraph "Authentication & Authorization"
        AKH[ApiKeyAuthHandler]
        RS[RequestSigningService]
        CVS[CredentialValidationService]
        PVS[ProviderValidationService]
    end
    
    subgraph "Business Logic Layer"
        MS[MerchantService]
        AKS[ApiKeyService]
        STS[SurchargeTransactionService]
        SPS[SurchargeProviderService]
        SPC[SurchargeProviderConfigService]
        AS[AuditService]
        TS[TokenService]
    end
    
    subgraph "Data Access Layer"
        MR[MerchantRepository]
        AKR[ApiKeyRepository]
        STR[SurchargeTransactionRepository]
        SPR[SurchargeProviderRepository]
        SPC[SurchargeProviderConfigRepository]
        MAR[MerchantAuditTrailRepository]
    end
    
    subgraph "Provider Adapter System"
        IAF[ISurchargeProviderAdapterFactory]
        IPA[InterPaymentsAdapter]
        FPA[Future Provider Adapters]
    end
    
    subgraph "AWS Integration"
        ASM[AwsSecretsManagerService]
        LAS[LocalApiKeySecretService]
    end
    
    subgraph "Infrastructure"
        DB[(PostgreSQL Database)]
        LOG[Serilog Logging]
        SWG[Swagger/OpenAPI]
        MID[RequestResponseLoggingMiddleware]
    end
    
    OC --> MS
    OC --> AKS
    SC --> STS
    SPC --> SPS
    SPC --> SPC
    
    MS --> MR
    AKS --> AKR
    STS --> STR
    SPS --> SPR
    SPC --> SPC
    
    STS --> IAF
    IAF --> IPA
    IAF --> FPA
    
    AKS --> ASM
    AKS --> LAS
    
    MR --> DB
    AKR --> DB
    STR --> DB
    SPR --> DB
    SPC --> DB
    MAR --> DB
    
    OC --> LOG
    SC --> LOG
    SPC --> LOG
    MID --> LOG
```

---

## Slide 3: Core Business Capabilities
### **Primary Functions**

#### **1. Merchant Onboarding & Management**
- **Initial API Key Generation**: One-time setup with merchant registration
- **Merchant Lifecycle Management**: Create, update, status management
- **Audit Trail & Compliance**: Complete audit logging for all operations
- **External System Integration**: Support for onboarding portal integration

#### **2. Surcharge Processing**
- **Real-time Transaction Processing**: Auth, sale, refund, cancel operations
- **Multi-provider Support**: InterPayments primary, extensible adapter system
- **SOAP Web Service Integration**: Legacy system support
- **Transaction Correlation**: Provider transaction ID tracking

#### **3. Provider Management**
- **Dynamic Configuration**: Runtime provider configuration updates
- **Credential Schema Validation**: Type-safe credential management
- **Primary Configuration Logic**: Single primary config per merchant/provider
- **Soft Delete Support**: Audit-friendly deletion with restore capability

---

## Slide 4: API Endpoints & Integration Points
### **Comprehensive RESTful API Structure**

```mermaid
graph LR
    subgraph "Onboarding API Service"
        OI[Initial API Key Generation]
        MG[Merchant Management]
        AK[API Key Management]
        AT[Audit Trail]
    end
    
    subgraph "Surcharge Processing"
        SA[Surcharge Auth]
        SS[Surcharge Sale]
        SR[Surcharge Refund]
        SC[Surcharge Cancel]
    end
    
    subgraph "Provider Management"
        PC[Provider Creation]
        PU[Provider Updates]
        PD[Provider Deletion]
        PR[Provider Restoration]
    end
    
    subgraph "SOAP Web Services"
        SWA[SOAP Auth]
        SWS[SOAP Sale]
        SWR[SOAP Refund]
        SWC[SOAP Cancel]
    end
    
    OI --> MG
    MG --> AK
    AK --> AT
    
    SA --> SS
    SS --> SR
    SR --> SC
    
    PC --> PU
    PU --> PD
    PD --> PR
    
    SWA --> SA
    SWS --> SS
    SWR --> SR
    SWC --> SC
```

---

## Slide 5: Onboarding API Service Integration
### **Merchant Onboarding Workflow**

```mermaid
sequenceDiagram
    participant OP as Onboarding Portal
    participant FNS as FeeNominalService
    participant AWS as AWS Secrets Manager
    participant DB as PostgreSQL
    
    OP->>FNS: POST /api/v1/onboarding/apikey/initial-generate
    Note over FNS: Creates merchant + initial API key
    FNS->>DB: Store merchant record
    FNS->>AWS: Store API key secret
    FNS->>DB: Store API key metadata
    FNS->>DB: Create audit trail entry
    FNS-->>OP: Return merchant + API key details
    
    OP->>FNS: POST /api/v1/onboarding/apikey/generate
    Note over FNS: Generate additional API keys
    FNS->>DB: Validate existing API key
    FNS->>AWS: Store new secret
    FNS-->>OP: Return new API key
    
    OP->>FNS: POST /api/v1/onboarding/apikey/rotate
    Note over FNS: Rotate API key secret
    FNS->>AWS: Update secret
    FNS->>DB: Update metadata
    FNS-->>OP: Return new secret
```

**Cool Capabilities:**
- ✅ **One-time Initial Setup**: Complete merchant + API key creation in single call
- ✅ **AWS Secrets Manager Integration**: Secure secret storage
- ✅ **Audit Trail**: Complete logging of all operations
- ✅ **API Key Rotation**: Secure key rotation with audit trail
- ✅ **External System Support**: Onboarding portal integration

---

## Slide 6: SOAP Web Service Integration
### **Legacy System Integration**

```mermaid
sequenceDiagram
    participant SW as SOAP Web Service
    participant FNS as FeeNominalService
    participant IP as InterPayments
    participant DB as PostgreSQL
    
    SW->>FNS: POST /api/v1/surcharge/auth
    Note over FNS: Validate merchant + API key
    FNS->>DB: Check provider configuration
    FNS->>IP: Forward auth request
    FNS->>DB: Store transaction record
    FNS-->>SW: Return auth response
    
    SW->>FNS: POST /api/v1/surcharge/sale
    Note over FNS: Process sale with correlation
    FNS->>DB: Match provider transaction ID
    FNS->>IP: Process sale
    FNS->>DB: Update transaction status
    FNS-->>SW: Return sale response
    
    SW->>FNS: POST /api/v1/surcharge/refund
    Note over FNS: Process refund
    FNS->>DB: Validate original transaction
    FNS->>IP: Process refund
    FNS->>DB: Create refund record
    FNS-->>SW: Return refund response
```

**Cool Capabilities:**
- ✅ **Legacy System Support**: SOAP web service integration
- ✅ **Transaction Correlation**: Provider transaction ID tracking
- ✅ **Follow-up Operations**: Auth → Sale → Refund/Cancel workflow
- ✅ **Merchant Isolation**: Strict data segregation
- ✅ **Provider Configuration Matching**: Dynamic provider selection

---

## Slide 7: Surcharge Provider Management
### **Dynamic Provider Configuration System**

```mermaid
graph TB
    subgraph "Provider Management"
        PC[Create Provider]
        PU[Update Provider]
        PD[Delete Provider]
        PR[Restore Provider]
        PL[List Providers]
    end
    
    subgraph "Configuration Features"
        CS[Credential Schema Validation]
        PCON[Primary Configuration Logic]
        SD[Soft Delete Support]
        AD[Audit Trail]
    end
    
    subgraph "Integration Points"
        IP[InterPayments Adapter]
        FA[Future Adapters]
        VA[Validation Service]
    end
    
    PC --> CS
    PU --> CS
    PD --> SD
    PR --> SD
    PL --> AD
    
    CS --> VA
    PCON --> IP
    IP --> FA
```

**Cool Capabilities:**
- ✅ **Credential Schema Validation**: Type-safe credential management
- ✅ **Primary Configuration Logic**: Single primary per merchant/provider
- ✅ **Soft Delete with Restore**: Audit-friendly deletion
- ✅ **Include Deleted Option**: `?includeDeleted=true` for audit
- ✅ **Extensible Adapter System**: Easy addition of new providers

---

## Slide 8: Security Implementation
### **Multi-Layer Security Architecture**

```mermaid
graph TB
    subgraph "Authentication Layer"
        AK[API Key Validation]
        HMAC[HMAC-SHA256 Signing]
        TS[Timestamp & Nonce]
        RP[Replay Protection]
    end
    
    subgraph "Authorization Layer"
        MI[Merchant Isolation]
        EP[Endpoint Permissions]
        RL[Rate Limiting]
        AT[Audit Trail]
    end
    
    subgraph "Data Security"
        AWS[AWS Secrets Manager]
        ENC[Encryption at Rest]
        VPC[VPC Endpoint Security]
        SSL[TLS Encryption]
    end
    
    AK --> MI
    HMAC --> EP
    TS --> RL
    RP --> AT
    
    MI --> AWS
    EP --> ENC
    RL --> VPC
    AT --> SSL
```

**Cool Capabilities:**
- ✅ **HMAC Request Signing**: `(timestamp|nonce|merchantId|apiKey)` signature
- ✅ **Merchant Isolation**: Strict data segregation
- ✅ **AWS Secrets Manager**: Secure credential storage
- ✅ **Rate Limiting**: Per API key usage tracking
- ✅ **Audit Trail**: Complete operation logging

---

## Slide 9: Database Design & Data Flow
### **PostgreSQL Schema with Audit Trail**

```mermaid
erDiagram
    merchants ||--o{ api_keys : "has"
    merchants ||--o{ surcharge_providers : "owns"
    merchants ||--o{ merchant_audit_trail : "logs"
    
    surcharge_providers ||--o{ surcharge_provider_configs : "configures"
    surcharge_providers ||--o{ surcharge_trans : "processes"
    
    api_keys ||--o{ api_key_usage : "tracks"
    
    merchants {
        uuid id PK
        string external_id UK
        string name
        string status
        timestamp created_at
        timestamp updated_at
        string created_by
    }
    
    api_keys {
        uuid id PK
        uuid merchant_id FK
        string key UK
        string description
        int rate_limit
        string[] allowed_endpoints
        string status
        timestamp created_at
        timestamp last_rotated_at
        timestamp revoked_at
    }
    
    surcharge_providers {
        uuid id PK
        string name
        string code UK
        string description
        string base_url
        string authentication_type
        jsonb credentials_schema
        string status
        timestamp created_at
        timestamp updated_at
        string created_by
    }
    
    surcharge_provider_configs {
        uuid id PK
        uuid merchant_id FK
        uuid provider_id FK
        string config_name
        boolean is_primary
        boolean is_active
        jsonb credentials
        int timeout
        int retry_count
        int retry_delay
        int rate_limit
        int rate_limit_period
        jsonb metadata
    }
    
    surcharge_trans {
        uuid id PK
        uuid merchant_id FK
        uuid provider_id FK
        string correlation_id
        string provider_transaction_id
        string operation_type
        string status
        decimal amount
        jsonb request_data
        jsonb response_data
        timestamp created_at
        timestamp updated_at
    }
    
    merchant_audit_trail {
        uuid id PK
        uuid merchant_id FK
        string action
        string entity_type
        string old_value
        string new_value
        string performed_by
        timestamp created_at
    }
```

---

## Slide 10: API Endpoints Summary
### **Comprehensive Endpoint Coverage**

#### **Onboarding Endpoints** (`/api/v1/onboarding`)
- `POST /apikey/initial-generate` - **One-time merchant + API key creation**
- `POST /apikey/generate` - **Additional API key generation**
- `POST /apikey/rotate` - **Secure API key rotation**
- `POST /apikey/revoke` - **API key revocation**
- `GET /apikey/list` - **List merchant API keys**
- `POST /merchants` - **Merchant creation**
- `GET /merchants/{id}` - **Get merchant details**
- `PUT /merchants/{id}` - **Update merchant**
- `GET /merchants/external/{externalId}` - **Get by external ID**
- `GET /merchants/{merchantId}/audit-trail` - **Audit trail retrieval**

#### **Surcharge Processing** (`/api/v1/surcharge`)
- `POST /auth` - **Initial authorization**
- `POST /sale` - **Sale processing**
- `POST /refund` - **Refund processing**
- `POST /cancel` - **Cancellation processing**
- `GET /transactions/{id}` - **Get transaction details**
- `GET /transactions` - **List transactions with pagination**

#### **Provider Management** (`/api/v1/merchants/{merchantId}/surcharge-providers`)
- `POST /` - **Create provider with configuration**
- `GET /` - **List providers (with `?includeDeleted=true` option)**
- `GET /{id}` - **Get provider details**
- `PUT /{id}` - **Update provider**
- `DELETE /{id}` - **Soft delete provider**
- `POST /{id}/restore` - **Restore deleted provider**

---

## Slide 11: Key Technical Achievements
### **Enterprise-Grade Features**

#### **🔐 Security & Compliance**
- **Multi-layer Authentication**: API key + HMAC signing + timestamp/nonce
- **Merchant Isolation**: Strict data segregation between tenants
- **AWS Secrets Manager**: Secure credential storage
- **Complete Audit Trail**: All operations logged for compliance

#### **🔄 Integration Capabilities**
- **Onboarding Portal Integration**: Complete merchant lifecycle management
- **SOAP Web Service Support**: Legacy system integration
- **Provider Adapter System**: Extensible provider integration
- **Transaction Correlation**: Provider transaction ID tracking

#### **⚡ Performance & Scalability**
- **Stateless Design**: Horizontal scaling support
- **Connection Pooling**: Database performance optimization
- **Rate Limiting**: Per API key usage tracking
- **Caching Strategies**: Response optimization

#### **🛠️ Developer Experience**
- **Comprehensive Documentation**: Swagger/OpenAPI integration
- **Error Handling**: Structured error responses
- **Validation**: Request/response validation
- **Monitoring**: Health checks and metrics

---

## **Summary: Key Achievements**
✅ **Enterprise Security**: Multi-layer authentication and encryption  
✅ **Scalable Architecture**: Clean separation of concerns and dependency injection  
✅ **Provider Flexibility**: Extensible adapter system for multiple payment providers  
✅ **Comprehensive Auditing**: Full audit trail and compliance tracking  
✅ **Legacy Integration**: SOAP web service support  
✅ **Onboarding Support**: Complete merchant lifecycle management  
✅ **Production Ready**: Monitoring, logging, and deployment automation  
✅ **API-First Design**: RESTful endpoints with comprehensive documentation  

**Technology Stack**: .NET 8.0, PostgreSQL, AWS Services, Entity Framework Core, Serilog, Swagger/OpenAPI 