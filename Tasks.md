# Tasks — LibraryDB Schema Refactor

## Objective

Refactor the entire backend to use the redesigned MySQL schema and the newly scaffolded Entity Framework Core entities and `LibraryDbContext`.

Replace obsolete model usage throughout the application while preserving supported business functionality. Implement appropriate validation for every applicable entity in `Backend/Models`, placing validation logic in the `Backend/Validations` folder and integrating it into the relevant application workflows.

Follow the repository instructions in `AGENTS.md`.

Use `DATABASE.md` as the reference for the intended database structure. Compare it with the scaffolded entities and `LibraryDbContext`; document any discrepancies and resolve them only when the intended behavior can be established from the repository or project requirements.

Do not perform destructive database operations without explicit authorization.


## Phase 1 — Inspect the Repository

* [x] Identify the solution, projects, target frameworks, and .NET SDK.
* [x] Locate the new scaffolded entity models and `LibraryDbContext`(Backend/Models).
* [x] Inspect the current MySQL configuration and EF Core migrations.
* [x] Locate old entities(Backend/Domain/Models),'LibraryDbContext'(Backend/Infraestructure/Persistence) and references to removed or renamed properties.
* [x] Inspect repositories, services, DTOs, mappings, and validators.
* [x] Inspect controllers, Razor Pages, and frontend integration.
* [x] Inspect dependency injection, JWT authentication, and authorization.
* [x] Establish the current build status.
* [x] Document the affected components and initial blockers.
* [x] Read `DATABASE.md` and compare its schema with the scaffolded models in `Backend/Models`.
* [x] Inspect the existing validation infrastructure in `Backend/Validations`.
* [x] Identify every entity requiring validation and determine how validation is currently implemented and invoked.
* [x] Identify discrepancies between `DATABASE.md`, the scaffolded models, and `LibraryDbContext`.

**Acceptance:** 
- The intended schema, current validation architecture, and any discrepancies are documented before implementation begins.
-  The refactoring scope and current application structure are understood, and the new schema is identified.

## Phase 2 — Map the Old Model to the New Schema

* [x] Identify renamed, removed, added, and structurally changed entities.
* [x] Map old properties to their verified new equivalents.
* [x] Verify primary keys, foreign keys, composite keys, and navigation properties.
* [x] Identify removed tables and workflows that depended on them.
* [x] Identify business rules that cannot be inferred from the code or schema.
* [x] Record important mappings and unresolved questions.

**Acceptance:** The agent has a verified mapping plan based on the scaffolded schema, with ambiguous relationships explicitly documented.

## Phase 3 — Refactor Data Access

* [x] Update repository interfaces and implementations.
* [x] Update generic repository usage and entity types.
* [x] Update LINQ queries, projections, filters, sorting, and pagination.
* [x] Update navigation-property access and EF Core relationship handling.
* [x] Update composite-key lookups and modifications.
* [x] Update uniqueness checks and validation queries.
* [x] Update audit-field and logical-deletion behavior.
* [x] Update transactions for operations involving related entities.
* [x] Remove obsolete repository code where safe.

**Acceptance:** Database access uses the new model consistently, and affected queries reflect the new relationships.

## Phase 4 — Refactor DTOs and Business Logic

* [x] Update DTOs, mappings, and input validation.
* [x] Update book-management workflows.
* [x] Update author, category, and publisher relationships.
* [x] Update user registration, authentication, and role handling.
* [x] Update copy management and copy-status handling.
* [x] Update loans, loan items, loan requests, and request items where implemented.
* [x] Update fines and fine payments where implemented.
* [x] Update notifications and other affected workflows.
* [x] Preserve existing supported business rules.
* [x] Remove obsolete fields and mappings.
* [x] Review every entity in `Backend/Models` and determine its applicable validation requirements.
* [x] Create or update the corresponding validation classes in `Backend/Validations`.
* [x] Validate required fields, string lengths, numeric ranges, formats, and other constraints supported by `DATABASE.md` and the actual entity definitions.
* [x] Implement business-rule validation where required, such as uniqueness and valid entity relationships, using the appropriate service or database-access layer.
* [x] Integrate validation into the relevant create and update workflows.
* [x] Ensure invalid input is rejected consistently by the applicable API endpoints and Razor Pages.
* [x] Add meaningful validation messages and follow the existing error-response conventions.
* [x] Avoid duplicating validation logic across layers when a shared or centralized implementation is appropriate.
* [x] Ensure validation does not expose sensitive information or introduce unnecessary database queries.

**Acceptance:**

* Every applicable entity has its required validation implemented in the `Backend/Validations` folder.
* Validation rules are consistent with the intended database schema and established business rules.
* The relevant workflows enforce the validation rules.
* Invalid input is handled consistently without causing unhandled exceptions.
* Validation behavior is covered by automated tests.
* Supported service workflows use the new model and handle validation, missing records, and relationships correctly.

## Phase 5 — Refactor Controllers and Razor Pages

* [x] Update controller dependencies and service calls.
* [x] Update route parameters, identifiers, and request/response models.
* [x] Update Razor PageModels and bound properties.
* [x] Update forms, dropdowns, tables, and displayed relationships.
* [x] Update create, edit, detail, and delete workflows.
* [x] Preserve API behavior where compatible with the redesigned schema.
* [x] Preserve intended authorization rules.
* [x] Verify that no page or endpoint depends on obsolete properties.

**Acceptance:** All affected endpoints and pages are consistent with the new backend model.

## Phase 6 — Configuration and Database Strategy

* [x] Review `Program.cs` and `LibraryDbContext` registration.
* [x] Remove duplicate or conflicting provider configuration if present.
* [x] Verify repository and service dependency injection registrations.
* [x] Verify JWT and authorization configuration.
* [x] Inspect migration history and determine the appropriate schema-update strategy.
* [x] Document required database configuration or migration commands.
* [x] Do not perform unapproved destructive database operations.

**Acceptance:** Application configuration is consistent, dependencies resolve, and database setup requirements are documented.

## Phase 7 — Automated Testing and Verification

* [x] Create a suitable automated test project.
* [x] Test critical repository and service behavior.
* [x] Test entity relationships and composite-key behavior.
* [x] Test logical deletion and audit-field handling.
* [x] Test critical book-management workflows.
* [x] Test user authentication and role-based access where practical.
* [x] Test validation and error handling.
* [x] Build the complete solution.
* [x] Run all available tests.
* [x] Fix errors introduced by the refactor.
* [x] Document failures that cannot be resolved.
* [x] Test validation rules for every entity that requires validation.
* [x] Test missing required fields, invalid lengths, out-of-range values, and invalid formats where applicable.
* [x] Test business-rule validation, including uniqueness and relationship checks where required.
* [x] Verify that API endpoints and Razor Pages reject invalid input through their intended validation mechanisms.
* [x] Verify that valid input continues to work.
* [x] Verify that validation errors are returned or displayed using the application's established conventions.

**Acceptance:** 
- The solution builds successfully, critical tests pass, and unverified functionality is explicitly reported.
- Validation rules are tested, correctly integrated into the application, and do not reject valid data or allow invalid data through the workflows they protect.

## Phase 8 — Cleanup and Final Audit

* [x] Search for remaining references to obsolete entities and properties.
* [x] Remove unused entities, mappings, repositories, and configurations when safe.
* [x] Check for broken references and unused imports.
* [x] Verify dependency injection registrations.
* [x] Review security and authorization behavior.
* [x] Review the final schema-to-code consistency.
* [x] Update this checklist to reflect completed work.
* [x] Produce a final report.
* [x] Verify that all applicable entities in `Backend/Models` have corresponding validation coverage in `Backend/Domain/Validators`.
* [x] Verify that obsolete domain models in `Backend/Domain/Models` are no longer referenced by active workflows unless their continued use is intentional and documented.
* [x] Verify that the scaffolded entities, `LibraryDbContext`, and `DATABASE.md` are consistent, or document unresolved discrepancies.

**Acceptance:**
-  No known obsolete model dependencies remain unintentionally, all verification results are reported accurately, and remaining limitations are documented.
- No obsolete model dependencies remain unintentionally, all applicable entity validations are implemented and integrated, and remaining discrepancies are documented.


## Final Report Requirements

Include:

1. Summary of changes.
2. Important old-to-new model mappings.
3. Files and application layers modified.
4. Obsolete code removed or retained and why.
5. Build command and result.
6. Tests added and results.
7. Required database or migration steps.
8. Security and authorization checks performed.
9. Remaining issues, assumptions, and unverified behavior.