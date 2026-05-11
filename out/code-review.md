# Code Review — 2026-05-11

HIGH: 2 | MEDIUM: 4 | LOW: 2

---

## HIGH

**File**: `EmployeeService.cs:93`
**Issue**: The `department` parameter is interpolated directly into a raw SQL string with no sanitisation, enabling SQL injection attacks.
**Fix**: Use a parameterised query (e.g., `SqlCommand` with `@department` parameter) instead of string interpolation.

---

**File**: `EmployeeService.cs:79`
**Issue**: The bare `catch (Exception) {}` in `CalculateBonus` discards all exceptions, making failures completely invisible.
**Fix**: Remove the try/catch entirely (the body cannot throw) or, if kept, at minimum log and rethrow the exception.

---

## MEDIUM

**File**: `EmployeeService.cs:21`
**Issue**: Literals such as `"IT"`, `"SALES"`, and `"ACTIVE"` are scattered across multiple methods with no compile-time safety or discoverability.
**Fix**: Introduce `DepartmentType` and `EmployeeStatus` enums and replace all string literals with enum values.

---

**File**: `EmployeeService.cs:21`
**Issue**: Four levels of nested conditionals in `CreateEmployee` make the salary-adjustment logic hard to read and test.
**Fix**: Flatten with early-return guard clauses or replace the ladder with a small private helper that maps salary ranges to multipliers.

---

**File**: `EmployeeService.cs:87`
**Issue**: `_employees.ToList()` allocates a full copy of the list before filtering, which is wasteful and unnecessary.
**Fix**: Call `_employees.FirstOrDefault(e => e.Name == name)` directly, removing the intermediate materialisation.

---

**File**: `EmployeeService.cs:54`
**Issue**: `Console.WriteLine` is used for operational output in `CreateEmployee` and `SearchByDepartment`, making log routing and level-control impossible.
**Fix**: Inject an `ILogger<EmployeeService>` and replace `Console.WriteLine` calls with `_logger.LogInformation(...)`.

---

## LOW

**File**: `EmployeeService.cs:98`, `EmployeeService.cs:17`
**Issue**: `LegacyBonusV1` is a private method never called anywhere, and `_retryAttempts` is a field declared but never used.
**Fix**: Delete both dead members to reduce noise and prevent future confusion.

---

**File**: `Program.cs:24`
**Issue**: `await Task.CompletedTask` is a no-op that adds async overhead with no benefit, likely left over from a template.
**Fix**: Remove the line; if no genuinely async work exists, change the signature back to `static void Main(string[] args)`.

---

## Top 3 actions

1. **Eliminate the SQL injection risk** in `SearchByDepartment` by switching to parameterised queries before any real database is wired up.
2. **Replace magic strings with enums** (`DepartmentType`, `EmployeeStatus`) to get compile-time safety across `Employee`, `EmployeeService`, and any future callers.
3. **Remove the empty catch block** in `CalculateBonus` and inject a proper `ILogger` in place of all `Console.WriteLine` calls so failures and events are observable in production.
