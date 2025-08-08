# FeeNominalService.Tests

This project contains unit tests for the FeeNominalService API.

## 📁 Project Structure

```
FeeNominalService.Tests/
├── Controllers/                    # Controller tests
│   └── SurchargeControllerTests.cs
├── Services/                       # Service layer tests
│   └── SurchargeTransactionServiceTests.cs
├── Repositories/                   # Repository tests (to be added)
├── Middleware/                     # Middleware tests (to be added)
├── Utils/                          # Utility tests (to be added)
├── TestHelpers/                    # Test data builders and helpers
│   └── TestDataBuilder.cs
└── README.md                       # This file
```

## 🧪 Testing Approach

### **Test Types**
- **Unit Tests**: Testing individual components in isolation
- **Integration Tests**: Testing component interactions
- **Controller Tests**: Testing API endpoints with mocked dependencies

### **Testing Framework**
- **xUnit**: Primary testing framework
- **Moq**: Mocking framework for dependencies
- **FluentAssertions**: Readable assertions
- **Microsoft.AspNetCore.Mvc.Testing**: Web application testing

### **Test Data Management**
- **TestDataBuilder**: Centralized test data creation
- **Mock Objects**: Dependency isolation
- **In-Memory Database**: Entity Framework testing

## 🚀 Getting Started

### **Running Tests**
```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~SurchargeControllerTests"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### **Test Naming Convention**
- **Format**: `{MethodName}_{Scenario}_{ExpectedResult}`
- **Example**: `ProcessRefund_WithValidRequest_ReturnsOkResult`

### **Test Structure (AAA Pattern)**
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange - Setup test data and mocks
    var request = TestDataBuilder.CreateValidRefundRequest();
    
    // Act - Execute the method under test
    var result = await service.ProcessRefundAsync(request, "test-actor");
    
    // Assert - Verify the results
    result.Should().NotBeNull();
    result.SurchargeTransactionId.Should().NotBeEmpty();
}
```

## 📋 Test Categories

### **Controller Tests**
- **HTTP Status Codes**: Verify correct status codes are returned
- **Response Models**: Ensure response models are correctly populated
- **Validation**: Test input validation and error handling
- **Authentication**: Test authentication and authorization

### **Service Tests**
- **Business Logic**: Test core business logic
- **Error Handling**: Test exception scenarios
- **Data Transformation**: Test data mapping and transformation
- **External Dependencies**: Test integration with external services

### **Repository Tests**
- **CRUD Operations**: Test database operations
- **Query Logic**: Test complex queries and filtering
- **Transaction Management**: Test transaction handling
- **Data Integrity**: Test data consistency

## 🔧 Test Configuration

### **Mock Setup**
```csharp
// Setup mock behavior
_mockService.Setup(x => x.Method(It.IsAny<Parameter>()))
    .ReturnsAsync(expectedResult);

// Verify mock interactions
_mockService.Verify(x => x.Method(It.IsAny<Parameter>()), Times.Once);
```

### **Test Data**
```csharp
// Use TestDataBuilder for consistent test data
var request = TestDataBuilder.CreateValidRefundRequest(
    surchargeTransactionId: Guid.NewGuid(),
    refundAmount: 100.00m
);
```

### **Assertions**
```csharp
// Use FluentAssertions for readable assertions
result.Should().NotBeNull();
result.SurchargeTransactionId.Should().NotBeEmpty();
result.RefundAmount.Should().Be(100.00m);
result.Error.Should().BeNull();
```

## 📊 Test Coverage

### **Current Coverage Areas**
- ✅ Surcharge Controller (Refund endpoint)
- ✅ Surcharge Transaction Service (Refund processing)
- 🔄 Repository Layer (To be implemented)
- 🔄 Middleware (To be implemented)
- 🔄 Utilities (To be implemented)

### **Coverage Goals**
- **Controllers**: 90%+ coverage
- **Services**: 85%+ coverage
- **Repositories**: 80%+ coverage
- **Overall**: 80%+ coverage

## 🐛 Debugging Tests

### **Running Tests in Debug Mode**
```bash
# Run tests in debug mode
dotnet test --configuration Debug

# Attach debugger to test process
dotnet test --logger "console;verbosity=detailed"
```

### **Test Output**
- **Console**: Use `Console.WriteLine()` for debugging
- **Logging**: Use `ILogger` for structured logging
- **Debugger**: Set breakpoints in test methods

## 📝 Best Practices

### **Test Isolation**
- Each test should be independent
- Use fresh mocks for each test
- Clean up test data after each test

### **Test Data**
- Use meaningful test data
- Avoid magic numbers and strings
- Use TestDataBuilder for consistency

### **Assertions**
- Test one thing per test method
- Use specific assertions
- Test both positive and negative scenarios

### **Naming**
- Use descriptive test names
- Follow the naming convention
- Make test purpose clear

## 🔄 Continuous Integration

### **CI/CD Pipeline**
- Tests run on every commit
- Coverage reports generated
- Test results published

### **Quality Gates**
- All tests must pass
- Minimum coverage threshold
- No test regressions

## 📚 Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/) 


#### Coerlet Code Coverage Comamnds: 
You're absolutely right! I apologize for the confusion. Here's the complete series of commands with the correct step 2:

## ** Code Coverage Commands**

### **1. Run Tests with Coverage Collection**
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

### **2. Generate HTML Coverage Report**
```powershell
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html
```

### **3. Open Coverage Report in Browser**
```powershell
Start-Process "./coverage-report/index.html"
```

### **4. Alternative: Run All Commands in Sequence**
```powershell
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html

# Open in browser
Start-Process "./coverage-report/index.html"
```

### **5. One-Liner Command (if you prefer)**
```powershell
dotnet test --collect:"XPlat Code Coverage" && reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html && Start-Process "./coverage-report/index.html"
```

## **📁 Expected Output Structure**

After running these commands, you'll have:
```
FeeNominalService.Tests/
├── TestResults/
│   └── [timestamp]/
│       └── coverage.cobertura.xml
└── coverage-report/
    ├── index.html          # Main coverage report
    ├── css/
    ├── js/
    └── [other report files]
```

## ** What You'll See in the Report**

The HTML coverage report will show:
- **Overall Coverage Percentage**
- **File-by-file breakdown**
- **Line-by-line coverage details**
- **Branch coverage** (if applicable)
- **Uncovered code highlighting**

## ** Coverage Goals**

With your **29 comprehensive tests**, you should see:
- **High coverage** on `SurchargeController` (likely 90%+)
- **Good coverage** on `SurchargeTransactionService` 
- **Areas for improvement** in other controllers/services

## **🔍 Quick Coverage Check**

If you just want to see the coverage percentage without the full report:
```powershell
dotnet test --collect:"XPlat Code Coverage" --logger "console;verbosity=minimal"
```

Run these commands and let me know what coverage percentage you achieve! This will help us plan the next steps for improving coverage across the entire codebase. 🚀