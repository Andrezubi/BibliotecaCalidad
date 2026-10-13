# AGENTS.md — LibraryDB Backend

## Project Overview

This repository contains a virtual library management system built with C#, .NET, Entity Framework Core, MySQL, ASP.NET Core Razor Pages, and API controllers.

The architecture includes database access, repositories, services, DTOs, API endpoints, Razor Pages, authentication, and authorization.

Existing components have included `LibraryDbContext`, `BaseRepository<Book>`, `IBookRepository`, `BookService`, and `BookController`. Verify the current implementation before relying on these names.

## Source of Truth

* The current scaffolded entity models and `LibraryDbContext` are authoritative for the redesigned database schema.
* Inspect the actual code before making assumptions about entities, relationships, property names, or data types.
* Do not recreate the old schema from memory.
* Do not modify scaffolded models solely to make obsolete code compile.
* If the schema, database, and application code disagree, investigate and document the discrepancy.

## Architecture and Coding Rules

* Follow the existing project architecture unless a change is necessary and justified.
* Update all affected repositories, services, DTOs, mappings, validators, controllers, Razor Pages, and dependency injection registrations.
* Use asynchronous database operations consistently with the existing architecture.
* Keep database access and business logic in their appropriate layers.
* Preserve API contracts and business behavior unless the redesigned schema requires a change.
* Avoid unrelated refactoring, unnecessary package upgrades, and cosmetic changes.
* Remove obsolete code only after verifying that it has no required direct or indirect dependencies.

## Database Rules

* Use the existing MySQL and EF Core configuration unless the repository establishes a necessary change.
* Respect the actual scaffolded primary keys, foreign keys, composite keys, nullability, constraints, and navigation properties.
* Preserve audit-field and logical-deletion behavior according to the current schema and business rules.
* Do not invent missing entities, properties, or relationships.
* Do not drop, truncate, or recreate a database without explicit authorization.
* Do not delete migration history or generate conflicting migrations without first inspecting the existing migration strategy.
* Keep schema changes and database-destructive operations separate from ordinary application refactoring.

## Security

* Preserve JWT authentication and intended authorization behavior.
* Respect the intended permissions of administrators, librarians, and readers.
* Never expose password hashes or secrets in API responses or logs.
* Validate incoming identifiers, input data, and relationship assignments.
* Derive audit-user identity from the authenticated principal when required.
* Do not weaken validation or authorization to resolve implementation errors.

## Testing and Verification

* Inspect the available .NET SDK, solution structure, and package versions before choosing test tooling.
* Add automated tests for critical behavior when no suitable tests exist.
* Use isolated test infrastructure; do not run destructive tests against a developer or shared database.
* Build the solution and run applicable tests after meaningful changes.
* Report actual command results. Never claim that a build, test, migration, or endpoint verification succeeded unless it was executed.
* Inspect for obsolete entity references and inconsistent dependency injection registrations.

## Working Method

1. Inspect the repository before editing.
2. Create a plan based on the actual code and schema.
3. Implement changes in manageable stages.
4. Build and test affected functionality.
5. Update the project task checklist as work progresses.
6. Document unresolved ambiguities, assumptions, and manual steps.

Work autonomously on well-defined tasks. If a decision would risk data loss, weaken security, or require inventing an important business rule, stop that specific operation, investigate available evidence, and document the blocker.

## Completion Report

Summarize the files changed, important design decisions, obsolete code removed, build and test results, database steps, and remaining issues.
