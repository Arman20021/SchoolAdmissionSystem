# Admission System --- Assumptions

This document lists the business and technical assumptions used by the
admission seat-hold system.

## 1. Student Identity

-   Each student is identified by a 17-digit Birth Registration ID
    (`BirthId` / `StudentId`).
-   The Birth ID is the unique identity key for admission rules.
-   The same Birth ID represents the same student across all classes and
    backend instances.
-   A student must provide a valid Birth ID before applying.
-   The backend must validate the Birth ID. Frontend validation alone is
    not trusted.
-   A client-supplied student ID header is an identifier, not proof of
    identity. Production use must verify the student's identity through
    a trusted process.

## 2. Class and Seat Capacity

-   Each admission class has a configured `TotalSeats` value.

-   `TotalSeats` must be greater than zero.

-   A seat can be in one of these relevant states:

    -   **Available:** not held and not confirmed.
    -   **Held:** temporarily reserved while the student completes the
        admission step.
    -   **Confirmed:** permanently allocated under the current business
        rules.

-   The number of occupied seats is the number of active holds plus
    confirmed seats.

-   The following invariant must always hold:

    ``` text
    HeldSeats + ConfirmedSeats <= TotalSeats
    ```

-   Available seats are calculated as:

    ``` text
    AvailableSeats = TotalSeats - HeldSeats - ConfirmedSeats
    ```

-   A class must not accept a new hold when no seats are available.

## 3. Creating a Hold

-   Applying for a class creates a temporary hold. It does not confirm
    admission.
-   A successful hold has a unique Hold ID.
-   A hold stores its creation time and expiry time.
-   The expiry time is calculated using the configured hold duration.
-   The default hold duration is 600 seconds (10 minutes).
-   The hold duration can be changed through `HOLD_DURATION_SECONDS`.
-   A held seat counts against the class capacity.
-   A student cannot create another active hold for a different class
    while an existing hold is active.
-   A student cannot create a duplicate active application for the same
    class.
-   If another request arrives while the student's hold is active, the
    backend returns a conflict response.
-   If the class has no available seats, the backend rejects the hold
    request.

## 4. Hold Expiration

-   A hold is temporary and expires at its stored expiry time.
-   An expired hold no longer reserves a seat.
-   When a hold expires, its seat becomes available again.
-   The student does not need to send an API request to release an
    expired hold.
-   A backend background service checks for expired holds.
-   The background check interval is configurable. The current Docker
    default is 1 second.
-   Expiration processing must be safe to run on more than one backend
    instance.
-   Seat availability must be calculated using the expiry time and
    persisted hold status consistently. It must not depend only on
    whether a browser page has been refreshed.
-   Small timing differences may occur between the exact expiry
    timestamp and the background worker's next check.

## 5. Confirming a Hold

-   A student can confirm an application only while its hold is still
    valid.
-   Confirmation changes the hold from `Held` to `Confirmed`.
-   A confirmed seat is no longer temporary.
-   A confirmed seat does not expire automatically.
-   A confirmed seat continues to count against `TotalSeats`.
-   Under the current business rule, a Birth ID that has confirmed one
    seat can never book or confirm another seat in any class.
-   This restriction applies across all classes, not just the class
    where the first seat was confirmed.
-   A later request using that Birth ID must be rejected with a message
    that the student has already confirmed a seat.
-   A confirmed application is treated as final. It is not released by
    the hold-expiration worker.
-   Any future cancellation or reversal of a confirmed admission would
    require a separate, explicitly authorized business workflow.

## 6. Cancellation

-   Cancelling an active hold releases its reserved seat.
-   A cancelled hold no longer counts as an occupied seat.
-   A cancelled or expired hold does not prevent the student from
    applying again, provided the student has no confirmed admission and
    no other active hold.
-   Hold cancellation and confirmation must not both succeed for the
    same hold.
-   The backend must verify that the hold belongs to the requesting
    student before allowing student-initiated cancellation or
    confirmation.

## 7. Concurrent Requests and Multiple Backend Instances

-   Multiple backend processes may run at the same time.

-   Each backend process may listen on a different port.

-   All backend processes use the same PostgreSQL database and the same
    database schema.

-   The database is the shared source of truth for classes, holds,
    confirmations, and seat counts.

-   Seat capacity is global. It is not calculated separately for each
    backend process.

-   Two or more students may submit booking requests at nearly the same
    time.

-   Concurrent requests must not cause more seats to be held or
    confirmed than the class capacity.

-   The backend must enforce seat allocation with database-safe
    transactions and appropriate locking or an equivalent atomic
    database operation.

-   Application-level checks alone are not sufficient to prevent race
    conditions.

-   Database constraints and unique indexes must enforce identity and
    active-application rules where applicable.

-   The same concurrency guarantees must apply when requests reach
    different backend processes.

-   The invariant must remain true after every successful transaction:

    ``` text
    HeldSeats + ConfirmedSeats <= TotalSeats
    ```

## 8. Shared Database and Data Consistency

-   All backend instances must use the same
    `DATABASE_CONNECTION_STRING`.
-   In Docker Compose, backend containers connect to PostgreSQL using
    the service hostname `postgres` and port `5432`.
-   Backend containers must not use `localhost` to reach the PostgreSQL
    container.
-   Database migrations must be applied before the API handles requests.
-   If multiple backend instances start together, migration execution
    must be coordinated or otherwise made safe.
-   PostgreSQL persistence is provided by the Docker named volume.
-   Restarting or recreating application containers must not erase
    database records.
-   Removing the database volume intentionally deletes the stored
    database data.

## 9. Time and Configuration

-   Hold timestamps are stored and compared in UTC.
-   The backend uses the server/database time consistently for expiry
    decisions.
-   `HOLD_DURATION_SECONDS` controls the hold duration. The default is
    `600`.
-   `HOLD_CHECK_INTERVAL_SECONDS` controls the background expiration
    check interval. The Docker default is `1`.
-   Configuration changes must be applied to every backend instance.
-   Changing the hold duration affects newly created holds. Existing
    holds retain their stored expiry timestamps unless a separate update
    process is implemented.

## 10. Frontend and Backend Responsibilities

-   The frontend displays class capacity and application status.
-   The backend is authoritative for seat availability and admission
    decisions.
-   The frontend must not decide that a seat is reserved or confirmed
    without a successful backend response.
-   The frontend may refresh class statistics periodically to show
    updated availability.
-   A browser refresh must not be required for the backend to expire a
    hold or release its seat.
-   The browser-facing API URL is configured through
    `VITE_API_BASE_URL`.
-   `VITE_API_BASE_URL` is embedded during the Vite build and is not a
    secret.
-   The backend CORS origin is configured through `FRONTEND_ORIGIN`.

## 11. Error and Conflict Handling

-   A class that does not exist cannot receive a hold.
-   A class with no available seats rejects new holds.
-   A student with an active hold for another class cannot apply for
    another class.
-   A student with an active hold for the same class cannot create a
    duplicate hold.
-   A student with a confirmed admission cannot book another seat with
    the same Birth ID.
-   A request that loses a concurrency race must receive a conflict
    response or another documented failure response.
-   Error messages should distinguish between:
    -   No seats available.
    -   An active hold already exists.
    -   A seat has already been confirmed for this Birth ID.
    -   The requested class or hold does not exist.
    -   The hold has expired or is no longer eligible for confirmation.

## 12. Operational Assumptions

-   Docker Compose starts PostgreSQL, the backend, and the frontend.
-   PostgreSQL is available only on the internal Docker network unless
    an explicit host port is added.
-   The frontend is available at `http://localhost:5173` by default.
-   The backend is available at `http://localhost:8080` by default.
-   Scalar API documentation is available at
    `http://localhost:8080/scalar` in the Docker environment.
-   The backend root URL redirects to Scalar in the Docker environment
    if that redirect is enabled in `Program.cs`.
-   The default Compose credentials are for local development only. They
    must be changed for shared or production environments.

## 13. Important Guarantees to Verify

The following are required system guarantees. They must be verified by
integration and concurrency tests; writing these assumptions in this
document does not prove that the current implementation already enforces
all of them.

-   Two backend processes using the same database cannot oversell a
    class.
-   Concurrent hold creation cannot make `HeldSeats + ConfirmedSeats`
    exceed `TotalSeats`.
-   A Birth ID with a confirmed admission is rejected across every class
    and every backend process.
-   A Birth ID can have no more than one active hold at a time across
    all classes.
-   Expired holds release capacity without a student API call.
-   Multiple expiration workers do not incorrectly release or modify the
    same hold.
-   Simultaneous confirmation, cancellation, and expiration operations
    produce one consistent final state.
-   Database constraints and transaction boundaries protect these rules
    even if requests arrive at exactly the same time.

------------------------------------------------------------------------

**Source of truth:** PostgreSQL and backend business rules are
authoritative. Frontend state, browser timers, and individual backend
process memory must never be treated as the final authority for seat
allocation.
