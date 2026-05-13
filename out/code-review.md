# Code Review — 2026-05-13
HIGH: 2 | MEDIUM: 4 | LOW: 2

## HIGH
- **File**: `EmployeeService.cs:93`
  - **Issue**: SQL is built via string interpolation with user input, enabling SQL injection.
  - **Fix**: Use parameterized queries and bind `department` as a parameter.
- **File**: `EmployeeService.cs:79`
  - **Issue**: An empty catch block swallows all exceptions and hides runtime failures.
  - **Fix**: Catch specific exceptions and log or rethrow with context.

## MEDIUM
- **File**: `EmployeeService.cs:19`
  - **Issue**: `CreateEmployee` accepts unvalidated inputs, allowing invalid names, emails, and salary values.
  - **Fix**: Add input validation and reject null/empty/malformed or out-of-range values.
- **File**: `EmployeeService.cs:21`
  - **Issue**: Deeply nested if/else salary logic reduces readability and increases maintenance risk.
  - **Fix**: Refactor with guard clauses or extracted rule methods.
- **File**: `EmployeeService.cs:63`
  - **Issue**: Magic strings like `"ACTIVE"`, `"IT"`, and `"SALES"` are brittle and error-prone.
  - **Fix**: Replace string discriminators with enums or constants.
- **File**: `EmployeeService.cs:17`
  - **Issue**: Unused members (`_retryAttempts`, `LegacyBonusV1`) add dead code and noise.
  - **Fix**: Remove unused members or implement and test their intended behavior.

## LOW
- **File**: `Program.cs:7`
  - **Issue**: `Console.WriteLine` is used for logging instead of structured `ILogger<T>` logging.
  - **Fix**: Inject and use `ILogger<T>` for structured, configurable logs.
- **File**: `EmployeeService.cs:87`
  - **Issue**: `FindByName` materializes a list before filtering, causing unnecessary allocation.
  - **Fix**: Query directly with `_employees.FirstOrDefault(e => e.Name == name)`.

## Top 3 actions
1. Replace interpolated SQL with parameterized queries.
2. Remove empty catch blocks and implement explicit exception handling.
3. Add input validation to `CreateEmployee` and enforce domain constraints.
