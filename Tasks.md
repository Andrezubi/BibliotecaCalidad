# Task: Fix return logic, loan status automation and overdue fines

## Context
Read before starting:
- `FrontEnd/AGENTS.md` and `BackEnd/AGENTS.md` (conventions, architecture)
- `BackEnd/DATABASE.md` (schema reference, not executable)

Main files: `FrontEnd/Pages/Loans/Details.cshtml` and `.cshtml.cs`, `Loans/Index`,
`LoanService` in the frontend, and the loan/return logic in the backend.

## Schema facts (from DATABASE.md)
- `Loan`: `Status` ENUM(`Active`,`Completed`,`Overdue`,`Lost`), `DueDate` (DATETIME).
  The due date exists ONLY on Loan, not on LoanItem.
- `LoanItem`: composite PK `(LoanId, CopyId)`, `Status` ENUM(`Active`,`Returned`,
  `Overdue`,`Lost`), `ReturnedAt` (nullable DATETIME).
- `Copy.StatusId` references `CopyStatus` (1 = `Available`, 2 = `Loaned`).
  Use the existing enum/constants/lookup, not hard-coded ids or strings.
- `Fine`: `LoanId`, `CopyId`, `DailyRate` (default 10.00), `OverdueDays`, `Amount`,
  `Status` ENUM(`Pending`,`PartiallyPaid`,`Paid`,`Waived`).
  UNIQUE `(LoanId, CopyId)`: at most one fine per loan item.
  There is no BorrowerId on Fine; the borrower is reached through Loan.
- Tables have audit columns (`UpdatedAt`, `UserId`). Set them on every row you
  modify, following the existing pattern in the codebase.

## Definitions (source of truth)
- **Overdue (by calendar date):** an item is overdue when today's date is AFTER the
  date part of `Loan.DueDate` and the item is not `Returned`. The time of day is
  ignored. A loan due on the 12th is NOT overdue on the 12th; it is overdue from
  the 13th.
- **Loan is Completed** only when ALL of its items are `Returned`.
- **Loan is Overdue** when it is not Completed/Lost and the due date (date part) has passed.
- **Lost:** out of scope. The refresh must never modify a `Lost` loan or `Lost` item,
  and a loan that contains a `Lost` item must not be auto-completed.
- **Fine calculation:**
  - `DailyRate`: named constant/config, value 10.00 (same as the DB default).
  - `OverdueDays` = (date of return) - (date part of `DueDate`), in calendar days.
    If the result is 0 or less, there is no fine.
  - `Amount = OverdueDays * DailyRate`. No grace period and no cap.
  - Example: due on the 12th, returned on the 13th -> 1 day -> Bs 10.00.
    Returned on the 15th -> 3 days -> Bs 30.00. Returned on the 12th -> no fine.
  - Currency is bolivianos. Display amounts as `Bs 10.00` everywhere in the UI.
  - Use one clock consistently (UTC or local, whichever the project already uses
    for `DueDate`/`CreatedAt`).

## Current bugs
- Pressing "Devolver" on a single copy in the loan details page does NOT update that
  LoanItem's status.
- The same action changes the Loan status, which is wrong: the loan may only change
  when all its items are returned.
- Hint: `LoanItem` has a composite key. Check that the return flow identifies the
  item by BOTH `LoanId` and `CopyId`, and that the item (not the loan) is the entity
  being updated and saved.

---

## Task 1: Fix individual copy return (backend)
**Behavior:** returning one copy must, in a single transaction:
1. Set that LoanItem's `Status` to `Returned` and `ReturnedAt` to now.
2. Set the Copy's status back to `Available` (only if it is currently `Loaned`).
3. Re-evaluate the parent Loan (see Task 3): it becomes `Completed` only if every
   item is now `Returned`; otherwise its status is unchanged.
4. Reject returning an item that is already `Returned` (or `Lost`) with a clear error.

Find and fix the root cause of the LoanItem not updating.

**Acceptance criteria:**
- [ ] Returning 1 of 2 items: that item is `Returned`, the other is unchanged,
      the loan is NOT `Completed`.
- [ ] Returning the last remaining item: the loan becomes `Completed`.
- [ ] The returned copy is `Available` again.
- [ ] Returning an already returned item gives a clear error and changes nothing.
- [ ] `UpdatedAt`/`UserId` are set on the modified rows.

## Task 2: Return button and "Devuelto" badge (frontend)
**Note:** this may already be implemented. Verify first; change only what is missing.

**Behavior:** per loan item in the details page:
- Show the "Devolver" button only when the item is `Active` or `Overdue`.
- When the item is `Returned`, show a "Devuelto" badge with the `ReturnedAt` date
  and no button.
- Visibility is based on the LoanItem status, not the Loan status.

**Acceptance criteria:**
- [ ] Button appears for Active and Overdue items only.
- [ ] Returned items show badge + date and no button.
- [ ] Lost items show neither button nor "Devuelto" badge (just their status).

## Task 3: Automatic status refresh on page load
**Behavior:** every time the loans pages are loaded (`Loans/Index` and
`Loans/Details`), the backend refreshes statuses BEFORE returning data:
- Loans `Active` whose due date (date part) has passed -> `Overdue`; their `Active`
  items -> `Overdue`.
- Loans `Active` or `Overdue` whose items are all `Returned` -> `Completed`.

**Implementation notes:**
- One backend service method, called from the relevant endpoints, not from Razor pages.
- Set-based updates (single queries), not loading all loans into memory.
- Idempotent: running it twice in a row changes nothing the second time.
- Never touch `Completed` or `Lost` loans, or `Returned`/`Lost` items.
- This stays a refresh-on-load for now (no background job).

**Acceptance criteria:**
- [ ] A loan whose due date was yesterday or earlier shows `Overdue` (loan and its
      unreturned items) after opening the loans page.
- [ ] A loan due today is still `Active`.
- [ ] A loan with all items returned shows `Completed`.
- [ ] A second consecutive run changes no rows.

## Task 4: Fine popup when returning an overdue item
**Behavior:**
- Clicking "Devolver" on an `Overdue` item opens a modal with: book title, due date,
  days overdue, daily rate (`Bs 10.00`), total fine (`Bs X.00`), and
  "Confirmar devolución" / "Cancelar".
- Confirming returns the copy (Task 1) AND inserts one `Fine` row
  (`LoanId`, `CopyId`, `DailyRate`, `OverdueDays`, `Amount`, `Status = Pending`,
  `UserId` = acting librarian) in the same transaction.
- Items that are not overdue are returned directly, with no modal and no Fine.

**Implementation notes:**
- Fine amount is computed ONLY in the backend. Add an endpoint/method that returns a
  fine preview for an item; the frontend displays it and never sends the amount as input.
- The confirm step recalculates the fine on the server at that moment.
- Disable the confirm button after the first click.

**Acceptance criteria:**
- [ ] Due on the 12th, returned on the 13th: modal shows 1 day and Bs 10.00.
- [ ] Due on the 12th, returned on the 15th: modal shows 3 days and Bs 30.00.
- [ ] Cancelling changes nothing (item not returned, no Fine).
- [ ] Confirming sets the item `Returned` and creates exactly one Fine.
- [ ] A double submit cannot create two Fines (handle the unique `(LoanId, CopyId)`
      constraint gracefully, with a clear message rather than a 500).
- [ ] A return on or before the due date creates no Fine.

---

## Constraints
- Keep UI text in Spanish, consistent with existing messages.
- Follow existing patterns (services, DTOs, `IsInAnyRole` checks).
- Use existing enums/constants for statuses instead of string literals.
- Do not change the database schema; if you think it is needed, stop and ask.
- All rules enforced in the backend, not only in the UI.

## Out of scope
- Marking loans/items as `Lost`.
- Fine payment flow (`FinePayment`) and fine management pages.
- Renewals (not implemented yet) and loan creation (separate task).

## Before you start
Briefly list your plan, the root cause of the return bug, the exact enums/constants
you found, and any assumptions or ambiguities before writing code.

## Verification
- Backend builds and existing tests pass. Add tests for: partial return, final return,
  double return, idempotent status refresh, fine amount calculation (including the
  same-day return = no fine and the 12th -> 13th = Bs 10.00 cases), and
  duplicate-fine prevention.
- Summarize files changed and give manual test steps for each acceptance criterion.