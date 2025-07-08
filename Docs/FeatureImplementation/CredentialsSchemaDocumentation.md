# Credentials Schema Documentation (Updated as of 2025-06-27)

## Overview
The credentials schema is a required component for all surcharge providers. It defines the structure and validation rules for authentication credentials, ensuring providers cannot be created without proper credential definitions and validation.

## What's New
- **Configurable Validation:** All validation rules (field length, type, value, etc.) are now environment-specific and configurable.
- **Single-Call Creation:** Providers and configurations can be created atomically in a single API call.
- **Detailed Error Handling:** Validation errors return detailed, structured error responses (see examples below).
- **New Field Types:** Support for JWT, API key, certificate, base64, JSON, and more.
- **Security/Compliance:** All sensitive fields are encrypted at rest; validation prevents credential stuffing and malformed data.
- **Internal Audience Note:** This doc is the reference for onboarding, QA, and internal devs. All schema/validation changes are reflected here first.

## Schema Structure (Summary)
- Top-level: `name`, `description`, `version`, `required_fields` (array)
- Optional: `optional_fields`, `documentation_url`, `metadata`
- Each field: `name`, `type`, `description`, plus optional constraints (see below)

## Field Types (Supported)
- Basic: string, number, integer, boolean
- Auth: email, url, password, jwt, api_key, client_id, client_secret, access_token, refresh_token, username
- Security: certificate, private_key, public_key, base64, json

## Validation Rules (Summary)
- All required fields must have valid names, types, and descriptions
- Field names max 100 chars, descriptions max 500 chars
- Field types must be from the supported list
- Length, pattern, allowedValues, etc. are enforced if present
- At least one required field is mandatory
- All validation is environment-configurable (see SurchargeProviderValidation section in appsettings)

## Single-Call Provider+Config Creation
- You can create a provider and its configuration in a single API call (see API docs for request/response format)
- Validation is performed on both schema and credential values atomically

## Error Handling & Examples
- All validation errors return HTTP 400 with a structured error response:

```json
{
  "message": "Invalid credentials schema",
  "errors": [
    "Schema name cannot exceed 100 characters",
    "required_fields[0].type 'invalid_type' is not a valid field type"
  ]
}
```

- Credential value errors:
```json
{
  "message": "Invalid credentials",
  "errors": [
    "Credential value 'jwt_token' length (15000) exceeds maximum (10000)",
    "Invalid JWT token format"
  ]
}
```

## Security & Compliance
- Sensitive fields (`sensitive: true`) are encrypted at rest
- All credential and schema validation is enforced before provider creation
- All error messages are secure and do not leak sensitive data
- All validation rules are environment-specific (dev/prod)

## For Onboarding/QA/Internal Users
- Use this doc as the reference for all provider credential schema and validation logic
- All new features, field types, and validation rules will be reflected here first
- If you encounter a validation error, refer to the error examples above for troubleshooting 