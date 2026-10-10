# Tasks — LibraryDB Schema Refactor

## Objective

Refactor the entire backend to use the redesigned MySQL schema and the newly scaffolded Entity Framework Core entities and `LibraryDbContext`.

Replace obsolete model usage throughout the application while preserving supported business functionality. Implement appropriate validation for every applicable entity in `Backend/Models`, placing validation logic in the `Backend/Validations` folder and integrating it into the relevant application workflows.

Follow the repository instructions in `AGENTS.md`.

Use `DATABASE.md` as the reference for the intended database structure. Compare it with the scaffolded entities and `LibraryDbContext`; document any discrepancies and resolve them only when the intended behavior can be established from the repository or project requirements.

Do not perform destructive database operations without explicit authorization.


## Phase 1 — Inspect the Repository

* [ ] Identify the solution, projects, target frameworks, and .NET SDK.
* [ ] Locate the new scaffolded entity models and `LibraryDbContext`(Backend/Models).
* [ ] Inspect the current MySQL configuration and EF Core migrations.
* [ ] Locate old entities(Backend/Domain/Models),'LibraryDbContext'(Backend/Infraestructure/Persistence) and references to removed or renamed properties.
* [ ] Inspect repositories, services, DTOs, mappings, and validators.
* [ ] Inspect controllers, Razor Pages, and frontend integration.
* [ ] Inspect dependency injection, JWT authentication, and authorization.
* [ ] Establish the current build status.
* [ ] Document the affected components and initial blockers.
* [ ] Read `DATABASE.md` and compare its schema with the scaffolded models in `Backend/Models`.
* [ ] Inspect the existing validation infrastructure in `Backend/Validations`.
* [ ] Identify every entity requiring validation and determine how validation is currently implemented and invoked.
* [ ] Identify discrepancies between `DATABASE.md`, the scaffolded models, and `LibraryDbContext`.

**Acceptance:** 
- The intended schema, current validation architecture, and any discrepancies are documented before implementation begins.
-  The refactoring scope and current application structure are understood, and the new schema is identified.

## Phase 2 — Map the Old Model to the New Schema

* [ ] Identify renamed, removed, added, and structurally changed entities.
* [ ] Map old properties to their verified new equivalents.
* [ ] Verify primary keys, foreign keys, composite keys, and navigation properties.
* [ ] Identify removed tables and workflows that depended on them.
* [ ] Identify business rules that cannot be inferred from the code or schema.
* [ ] Record important mappings and unresolved questions.

**Acceptance:** The agent has a verified mapping plan based on the scaffolded schema, with ambiguous relationships explicitly documented.

## Phase 3 — Refactor Data Access

* [ ] Update repository interfaces and implementations.
* [ ] Update generic repository usage and entity types.
* [ ] Update LINQ queries, projections, filters, sorting, and pagination.
* [ ] Update navigation-property access and EF Core relationship handling.
* [ ] Update composite-key lookups and modifications.
* [ ] Update uniqueness checks and validation queries.
* [ ] Update audit-field and logical-deletion behavior.
* [ ] Update transactions for operations involving related entities.
* [ ] Remove obsolete repository code where safe.

**Acceptance:** Database access uses the new model consistently, and affected queries reflect the new relationships.

## Phase 4 — Refactor DTOs and Business Logic

* [ ] Update DTOs, mappings, and input validation.
* [ ] Update book-management workflows.
* [ ] Update author, category, and publisher relationships.
* [ ] Update user registration, authentication, and role handling.
* [ ] Update copy management and copy-status handling.
* [ ] Update loans, loan items, loan requests, and request items where implemented.
* [ ] Update fines and fine payments where implemented.
* [ ] Update notifications and other affected workflows.
* [ ] Preserve existing supported business rules.
* [ ] Remove obsolete fields and mappings.
* [ ] Review every entity in `Backend/Models` and determine its applicable validation requirements.
* [ ] Create or update the corresponding validation classes in `Backend/Validations`.
* [ ] Validate required fields, string lengths, numeric ranges, formats, and other constraints supported by `DATABASE.md` and the actual entity definitions.
* [ ] Implement business-rule validation where required, such as uniqueness and valid entity relationships, using the appropriate service or database-access layer.
* [ ] Integrate validation into the relevant create and update workflows.
* [ ] Ensure invalid input is rejected consistently by the applicable API endpoints and Razor Pages.
* [ ] Add meaningful validation messages and follow the existing error-response conventions.
* [ ] Avoid duplicating validation logic across layers when a shared or centralized implementation is appropriate.
* [ ] Ensure validation does not expose sensitive information or introduce unnecessary database queries.

**Acceptance:**

* Every applicable entity has its required validation implemented in the `Backend/Validations` folder.
* Validation rules are consistent with the intended database schema and established business rules.
* The relevant workflows enforce the validation rules.
* Invalid input is handled consistently without causing unhandled exceptions.
* Validation behavior is covered by automated tests.
* Supported service workflows use the new model and handle validation, missing records, and relationships correctly.

## Phase 5 — Refactor Controllers and Razor Pages

* [ ] Update controller dependencies and service calls.
* [ ] Update route parameters, identifiers, and request/response models.
* [ ] Update Razor PageModels and bound properties.
* [ ] Update forms, dropdowns, tables, and displayed relationships.
* [ ] Update create, edit, detail, and delete workflows.
* [ ] Preserve API behavior where compatible with the redesigned schema.
* [ ] Preserve intended authorization rules.
* [ ] Verify that no page or endpoint depends on obsolete properties.

**Acceptance:** All affected endpoints and pages are consistent with the new backend model.

## Phase 6 — Configuration and Database Strategy

* [ ] Review `Program.cs` and `LibraryDbContext` registration.
* [ ] Remove duplicate or conflicting provider configuration if present.
* [ ] Verify repository and service dependency injection registrations.
* [ ] Verify JWT and authorization configuration.
* [ ] Inspect migration history and determine the appropriate schema-update strategy.
* [ ] Document required database configuration or migration commands.
* [ ] Do not perform unapproved destructive database operations.

**Acceptance:** Application configuration is consistent, dependencies resolve, and database setup requirements are documented.

## Phase 7 — Automated Testing and Verification

* [ ] Create a suitable automated test project.
* [ ] Test critical repository and service behavior.
* [ ] Test entity relationships and composite-key behavior.
* [ ] Test logical deletion and audit-field handling.
* [ ] Test critical book-management workflows.
* [ ] Test user authentication and role-based access where practical.
* [ ] Test validation and error handling.
* [ ] Build the complete solution.
* [ ] Run all available tests.
* [ ] Fix errors introduced by the refactor.
* [ ] Document failures that cannot be resolved.
* [ ] Test validation rules for every entity that requires validation.
* [ ] Test missing required fields, invalid lengths, out-of-range values, and invalid formats where applicable.
* [ ] Test business-rule validation, including uniqueness and relationship checks where required.
* [ ] Verify that API endpoints and Razor Pages reject invalid input through their intended validation mechanisms.
* [ ] Verify that valid input continues to work.
* [ ] Verify that validation errors are returned or displayed using the application's established conventions.

**Acceptance:** 
- The solution builds successfully, critical tests pass, and unverified functionality is explicitly reported.
- Validation rules are tested, correctly integrated into the application, and do not reject valid data or allow invalid data through the workflows they protect.

## Phase 8 — Cleanup and Final Audit

* [ ] Search for remaining references to obsolete entities and properties.
* [ ] Remove unused entities, mappings, repositories, and configurations when safe.
* [ ] Check for broken references and unused imports.
* [ ] Verify dependency injection registrations.
* [ ] Review security and authorization behavior.
* [ ] Review the final schema-to-code consistency.
* [ ] Update this checklist to reflect completed work.
* [ ] Produce a final report.
* [ ] Verify that all applicable entities in `Backend/Models` have corresponding validation coverage in `Backend/Domain/Validators`.
* [ ] Verify that obsolete domain models in `Backend/Domain/Models` are no longer referenced by active workflows unless their continued use is intentional and documented.
* [ ] Verify that the scaffolded entities, `LibraryDbContext`, and `DATABASE.md` are consistent, or document unresolved discrepancies.


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
