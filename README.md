# eRaven — Personnel Accounting (Blazor Server)

eRaven is a Blazor Server application (interactive mode) designed to help a unit keep **personnel records** in a simple and consistent way.

The system is intended for:
- **2 operators** (data entry / updates)
- **up to 5 users** in **view-only** mode

---

## Key concept: Person Card

A **Person Card** is the main record in the system.  
It represents a single person and stores both the current state (read model) and the history of changes (events).

### What a Person Card contains
Typical fields:
- Full name and identification data (e.g., Tax ID / RNOKPP)
- Rank
- Position
- Additional fields (e.g., BZVP, weapon, callsign — if used in your project)
- Lifecycle status (**Reserved** / **Enrolled**)
- Enrollment kind when the person is in the timesheet (**Unit / By List / By Order**)

---

## Lifecycle (Card status)

The project uses a simplified lifecycle:

- **Reserved** — the person is in reserve (not in the timesheet)
- **Enrolled** — the person is included in the timesheet

> “Exclusion” does not delete the card. It moves a person from **Enrolled** back to **Reserved** and stores exclusion metadata.

---

## Core functions (what operators can do)

### 1) Create a new card (Create Reserved)
Creates a new **Reserved** person card with basic information.

### 2) Enroll into the timesheet (Enroll)
Moves a person from **Reserved** to **Enrolled** and records:
- enrollment kind (how the person is listed)
- reference / reason (if used)
- enrollment date
- optional updates to rank/position

### 3) Exclude from the timesheet (Exclude)
Moves a person from **Enrolled** back to **Reserved** and records:
- effective date
- reason

### 4) View / browse the registry
The **Persons Registry** page shows cards in a paged table and supports:
- search (if enabled)
- filtering by lifecycle
- filtering by enrollment kind
- quick row actions (open card / enroll / exclude)

---

## Primary dashboard (current stage)

The current dashboard is the first (minimal) version and focuses on **timesheet counts**:

### Timesheet counters
It shows 4 numbers:
- **In timesheet (total)** — total number of cards with `Lifecycle = Enrolled`
- **In timesheet: Unit** — enrolled with `EnrollmentKind = Unit`
- **In timesheet: By list** — enrolled with `EnrollmentKind = AttachedByList`
- **In timesheet: By order** — enrolled with `EnrollmentKind = AttachedByOrder`

### Navigation from dashboard
Each counter is clickable and navigates to the **Persons Registry** page with filters applied via query string, e.g.:
- `/persons?lifecycle=Enrolled`
- `/persons?lifecycle=Enrolled&enrollmentKind=Unit`

---

## Architecture notes (short)

- The domain is built around the **Person aggregate** with its own event set.
- Read models are stored in the database and queried through handlers/repositories.
- The UI is Blazor Server components with tests (bUnit) and infrastructure tests using SQLite in-memory.

---

## Current scope (what is implemented now)

- Person card lifecycle (Reserved ↔ Enrolled)
- Registry page with paging and actions (create/enroll/exclude)
- Minimal dashboard with 4 timesheet counters + navigation to registry
- Automated tests for key pages and data access

---
