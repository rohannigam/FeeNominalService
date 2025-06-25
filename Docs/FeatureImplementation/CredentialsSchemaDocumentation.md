# Credentials Schema Documentation

## Overview

The credentials schema is a **required** component for all surcharge providers that defines the structure and validation rules for authentication credentials. This ensures that providers cannot be created without proper credential definitions, and that all credential fields have proper validation.

## Schema Structure

### Required Top-Level Properties

Every credentials schema must include:

1. **`name`** (string, required): A descriptive name for the authentication method
2. **`description`** (string, required): Detailed description of the authentication method
3. **`version`** (string, required): Schema version (e.g., "1.0")
4. **`required_fields`** (array, required): Array of required credential fields

### Optional Top-Level Properties

- **`optional_fields`** (array, optional): Array of optional credential fields
- **`documentation_url`** (string, optional): URL to additional documentation
- **`metadata`** (object, optional): Additional metadata

## Field Structure

Each field in `required_fields` or `optional_fields` must have:

### Required Field Properties

1. **`name`** (string, required): Field identifier (max 100 characters)
2. **`type`** (string, required): Field type (see valid types below)
3. **`description`** (string, required): Field description (max 500 characters)

### Optional Field Properties

- **`displayName`** (string, optional): Human-readable display name
- **`required`** (boolean, default: true): Whether the field is required
- **`sensitive`** (boolean, default: false): Whether the field contains sensitive data
- **`defaultValue`** (string, optional): Default value for the field
- **`pattern`** (string, optional): Regex pattern for validation
- **`minLength`** (integer, optional): Minimum length constraint
- **`maxLength`** (integer, optional): Maximum length constraint
- **`allowedValues`** (array, optional): List of allowed values
- **`validationMessage`** (string, optional): Custom validation message

## Valid Field Types

The following field types are supported:

### Basic Types
- `string`: General string value
- `number`: Numeric value
- `integer`: Integer value
- `boolean`: Boolean value

### Authentication-Specific Types
- `email`: Email address
- `url`: URL/URI
- `password`: Password field (automatically marked as sensitive)
- `jwt`: JSON Web Token
- `api_key`: API key
- `client_id`: OAuth client identifier
- `client_secret`: OAuth client secret
- `access_token`: OAuth access token
- `refresh_token`: OAuth refresh token
- `username`: Username field

### Security Types
- `certificate`: SSL/TLS certificate
- `private_key`: Private key
- `public_key`: Public key
- `base64`: Base64 encoded data
- `json`: JSON data

## Validation Rules

### Schema-Level Validation
- At least one required field must be defined
- All required fields must have valid names, types, and descriptions
- Field names cannot exceed 100 characters
- Field descriptions cannot exceed 500 characters
- Field types must be from the valid types list

### Field-Level Validation
- `minLength` cannot be greater than `maxLength`
- Length constraints cannot be negative
- `allowedValues` must be an array of strings
- `pattern` must be a valid regex string

## Examples

### API Key Authentication

```json
{
  "name": "API Key Authentication",
  "description": "API key based authentication for surcharge provider",
  "version": "1.0",
  "required_fields": [
    {
      "name": "api_key",
      "type": "api_key",
      "description": "API key for authentication",
      "displayName": "API Key",
      "required": true,
      "sensitive": true,
      "minLength": 1,
      "maxLength": 500
    },
    {
      "name": "api_key_header",
      "type": "string",
      "description": "HTTP header name for the API key",
      "displayName": "API Key Header",
      "required": true,
      "sensitive": false,
      "defaultValue": "X-API-Key",
      "minLength": 1,
      "maxLength": 100
    }
  ],
  "optional_fields": [
    {
      "name": "timeout",
      "type": "integer",
      "description": "Request timeout in seconds",
      "displayName": "Timeout",
      "required": false,
      "sensitive": false,
      "defaultValue": "30",
      "minLength": 1,
      "maxLength": 3
    }
  ]
}
```

### OAuth 2.0 Authentication

```json
{
  "name": "OAuth 2.0 Authentication",
  "description": "OAuth 2.0 client credentials flow",
  "version": "1.0",
  "required_fields": [
    {
      "name": "client_id",
      "type": "client_id",
      "description": "OAuth 2.0 client identifier",
      "displayName": "Client ID",
      "required": true,
      "sensitive": false,
      "minLength": 1,
      "maxLength": 255
    },
    {
      "name": "client_secret",
      "type": "client_secret",
      "description": "OAuth 2.0 client secret",
      "displayName": "Client Secret",
      "required": true,
      "sensitive": true,
      "minLength": 1,
      "maxLength": 255
    },
    {
      "name": "token_url",
      "type": "url",
      "description": "OAuth 2.0 token endpoint URL",
      "displayName": "Token URL",
      "required": true,
      "sensitive": false,
      "pattern": "^https?://.+"
    }
  ],
  "optional_fields": [
    {
      "name": "scope",
      "type": "string",
      "description": "OAuth 2.0 scope (optional)",
      "displayName": "Scope",
      "required": false,
      "sensitive": false,
      "maxLength": 500
    }
  ]
}
```

### JWT Authentication

```json
{
  "name": "JWT Authentication",
  "description": "JSON Web Token based authentication",
  "version": "1.0",
  "required_fields": [
    {
      "name": "jwt_token",
      "type": "jwt",
      "description": "JSON Web Token for authentication",
      "displayName": "JWT Token",
      "required": true,
      "sensitive": true,
      "minLength": 1,
      "maxLength": 2000
    }
  ],
  "optional_fields": [
    {
      "name": "token_type",
      "type": "string",
      "description": "Token type (e.g., Bearer)",
      "displayName": "Token Type",
      "required": false,
      "sensitive": false,
      "defaultValue": "Bearer",
      "allowedValues": ["Bearer", "JWT"]
    }
  ]
}
```

### Basic Authentication

```json
{
  "name": "Basic Authentication",
  "description": "Username and password authentication",
  "version": "1.0",
  "required_fields": [
    {
      "name": "username",
      "type": "string",
      "description": "Username for authentication",
      "displayName": "Username",
      "required": true,
      "sensitive": false,
      "minLength": 1,
      "maxLength": 100
    },
    {
      "name": "password",
      "type": "password",
      "description": "Password for authentication",
      "displayName": "Password",
      "required": true,
      "sensitive": true,
      "minLength": 1,
      "maxLength": 255
    }
  ]
}
```

## Error Handling

### Common Validation Errors

1. **Missing Required Properties**
   ```
   "Credentials schema must have a 'name' property"
   "Credentials schema must have a 'description' property"
   "Credentials schema must have a 'required_fields' property"
   ```

2. **Invalid Field Structure**
   ```
   "required_fields[0].name is required"
   "required_fields[0].type is required"
   "required_fields[0].description is required"
   ```

3. **Invalid Field Types**
   ```
   "required_fields[0].type 'invalid_type' is not a valid field type"
   ```

4. **Length Constraint Violations**
   ```
   "required_fields[0].name cannot exceed 100 characters"
   "required_fields[0].description cannot exceed 500 characters"
   ```

5. **Empty Required Fields**
   ```
   "At least one required field must be defined"
   ```

## Implementation Notes

### Service Layer Validation
- The `SurchargeProviderService.CreateAsync()` method validates the schema before creating providers
- The `SurchargeProviderService.UpdateAsync()` method validates the schema before updating providers
- Validation errors are returned as `InvalidOperationException` with detailed error messages

### Controller Layer Validation
- The `SurchargeProviderController` validates schemas in both create and update endpoints
- Validation errors are returned as HTTP 400 Bad Request with detailed error lists

### Request Model Validation
- The `SurchargeProviderRequest.ValidateCredentialsSchema()` method provides client-side validation
- Returns detailed error lists for debugging

## Security Considerations

1. **Sensitive Fields**: Fields marked as `sensitive: true` should be encrypted at rest
2. **Field Validation**: All fields are validated against their defined constraints
3. **Type Safety**: Field types ensure proper handling of different credential types
4. **Required Fields**: Prevents creation of providers with incomplete credential definitions

## Migration from Legacy Schemas

If you have existing providers with legacy credential schemas, you'll need to update them to include:

1. Top-level `name`, `description`, and `version` properties
2. Proper field structure with `name`, `type`, and `description`
3. Valid field types from the supported list

The system will reject any provider creation or updates that don't meet these requirements. 