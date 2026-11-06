# Roadmap

This is the working plan for booking-calendars. Stories are grouped by epic and tagged with the milestone they belong to.

## Scope

In scope:
- Booking calendars with public links, meeting lengths of 15 minutes, 30 minutes or 1 hour, weekly hours, blocked periods and buffers.
- A public booking page with free slots, booking confirmed by a 6-digit code, and no conflicting bookings across the owner's calendars.
- Managing bookings: agenda, cancel and reschedule on both sides, notifications.

Out of scope:
- No sync with Google, Outlook or other external calendars in the first version.
- No payment and no video meeting links.
- No email in the first version. Notifications are in-app, behind an interface, so email delivery can be added later. People who book create a light account or log in to receive their code.
- No real user data. The demo runs on fictional accounts and bookings.

## Glossary

| Term | Meaning |
|------|---------|
| Owner | A user who creates booking calendars |
| Calendar | A bookable meeting type with a title, meeting lengths, a public link and availability rules |
| Meeting length | A duration offered by a calendar: 15 minutes, 30 minutes or 1 hour. A calendar offers one or more |
| Public link | The shareable address of a calendar: `/book/{slug}` |
| Weekly hours | The time ranges per weekday when the owner can be booked |
| Blocked period | One or more days when no booking is possible on a calendar, for example holidays |
| Date override | A change for one date: different hours |
| Buffer | Free time kept before and after a meeting, set per meeting length |
| Conflict | Two bookings, buffers included, that overlap for the same owner |
| Minimum notice | How early a slot must be booked, for example 4 hours before |
| Booking horizon | How far ahead a slot can be booked, for example 60 days |
| Slot | A start time offered on the public page, computed from the rules and existing bookings |
| Booker | A user who books a slot on a public page |
| Pending booking | A booking waiting for its confirmation code. It holds the slot for a few minutes |
| Confirmation code | A 6-digit code delivered in a notification to confirm a booking |

## Milestones

| Milestone | Goal |
|-----------|------|
| M0 Foundation | Runnable app: accounts, demo data, calendars with public links, meeting lengths, weekly hours, blocked periods and buffers |
| M1 Booking | Public page with free slots, booking with code confirmation, conflict handling |
| M2 Managing bookings | Owner's agenda, cancel and reschedule on both sides, notifications |
| M3 Advanced rules | Date overrides, notice and horizon, daily limit, questions on the booking form |
| M4 Quality | CI, tests, accessibility, docs |

---

## EPIC 1 — Project setup

| ID | Story | Milestone |
|----|-------|-----------|
| SETUP-1 | Repository with README, LICENSE (MIT), .gitignore, .editorconfig | M0 |
| SETUP-2 | .NET solution: Api, Domain, Application, Infrastructure and Tests projects | M0 |
| SETUP-3 | SQLite + EF Core, first migration, database created on startup in dev | M0 |
| SETUP-4 | Angular workspace with the main app and shared libraries | M0 |
| SETUP-5 | GitHub Actions: build and test both API and front end on each push | M4 |

## EPIC 2 — Accounts

| ID | Story | Milestone |
|----|-------|-----------|
| AUTH-1 | As a visitor, I create an account with name, email and password | M0 |
| AUTH-2 | As a user, I log in and get a token | M0 |
| AUTH-3 | As a user, I log out, and I am sent back to login when my session expires | M0 |
| AUTH-4 | As a user, I change my password with current password, new password and confirmation | M0 |
| AUTH-5 | Demo accounts (owners and bookers) are seeded with a documented demo password | M0 |
| AUTH-6 | As a user, I set my time zone in my profile | M0 |

Acceptance notes:
- Passwords are hashed. Demo emails use `@example.com`.

## EPIC 3 — Calendars

| ID | Story | Milestone |
|----|-------|-----------|
| CAL-1 | As an owner, I create a calendar with a title, a description and a location note | M0 |
| CAL-7 | As an owner, I choose the meeting lengths the calendar offers: 15 minutes, 30 minutes, 1 hour, one or more | M0 |
| CAL-8 | A calendar must offer at least one meeting length; removing a length keeps the existing bookings of that length | M0 |
| CAL-2 | Each calendar gets a public slug built from its title. I can change it; slugs are unique | M0 |
| CAL-3 | As an owner, I list my calendars and copy each public link | M0 |
| CAL-4 | As an owner, I pause a calendar. Its public page says it takes no bookings | M0 |
| CAL-5 | As an owner, I edit or delete a calendar. A calendar with upcoming bookings cannot be deleted | M0 |
| CAL-6 | As an owner, I duplicate a calendar with its rules | M3 |

## EPIC 4 — Availability

| ID | Story | Milestone |
|----|-------|-----------|
| AVAIL-1 | As an owner, I set weekly hours per calendar: one or more time ranges per weekday | M0 |
| AVAIL-2 | Overlapping or empty ranges are rejected with a clear message | M0 |
| AVAIL-3 | As an owner, I add date overrides with different hours for one date | M3 |
| AVAIL-4 | As an owner, I set a buffer before and a buffer after meetings, for each meeting length (for example 5 minutes around a 15-minute call, 15 minutes around a 1-hour meeting) | M0 |
| AVAIL-5 | As an owner, I set the minimum notice and the booking horizon | M3 |
| AVAIL-6 | As an owner, I set a maximum number of bookings per day | M3 |
| AVAIL-7 | A booking on one of my calendars blocks the same time, buffers included, on all my calendars | M1 |
| AVAIL-8 | As an owner, I block periods on a calendar, from one date to another, with an optional label. No slot is offered on those days | M0 |
| AVAIL-9 | As an owner, I block a period on all my calendars at once | M0 |
| AVAIL-10 | Blocked periods that overlap or end before they start are refused | M0 |
| AVAIL-11 | When I block a period or change my hours, existing bookings are kept and listed as conflicts so I can cancel or move them | M2 |

Acceptance notes:
- Slot computation is a pure function covered by unit tests: rules, meeting length, existing bookings, buffers, blocked periods, notice, horizon and time zone in, slots out.
- A slot is offered only if the meeting and its buffers fit inside the hours and do not overlap any confirmed or held booking of the owner, buffers included.
- Times are stored in UTC. Weekly hours are read in the owner's time zone, including daylight saving changes.

## EPIC 5 — Public booking page

| ID | Story | Milestone |
|----|-------|-----------|
| BOOK-1 | As a visitor, I open a public link and see the calendar title, owner name, meeting lengths and description | M1 |
| BOOK-2 | As a visitor, I pick a meeting length and see free slots by day, shown in my browser's time zone | M1 |
| BOOK-3 | As a visitor, I pick a slot. I log in or create a light account to continue | M1 |
| BOOK-4 | As a booker, I add an optional message and submit. The slot is held for 10 minutes | M1 |
| BOOK-5 | As a booker, I receive a 6-digit code in a notification and enter it to confirm | M1 |
| BOOK-6 | Wrong codes are limited, codes expire, and an expired hold frees the slot | M1 |
| BOOK-7 | Two people can never confirm overlapping slots: the second one gets a clear message and fresh slots | M1 |
| BOOK-10 | A slot taken or held by someone else, on any of the owner's calendars, disappears from every public page | M1 |
| BOOK-8 | Unknown, paused or deleted calendars show a clear page | M1 |
| BOOK-9 | As an owner, I add custom questions to the booking form (text, choice, required or not) | M3 |

Acceptance notes:
- Codes are random, stored hashed, and expire after 10 minutes.
- Conflicts are checked again in the database transaction that confirms a booking, not only in the UI, so two confirmations at the same time cannot both succeed.

## EPIC 6 — Managing bookings

| ID | Story | Milestone |
|----|-------|-----------|
| MANAGE-1 | As an owner, I see my upcoming and past bookings, filtered by calendar | M2 |
| MANAGE-2 | As an owner, I open a booking and see the booker, the message and the answers | M2 |
| MANAGE-3 | As an owner, I cancel a booking with a reason. The booker is notified | M2 |
| MANAGE-4 | As a booker, I see my bookings and cancel one up to the calendar's cancel limit | M2 |
| MANAGE-5 | As a booker, I reschedule to another free slot of the same calendar | M2 |
| MANAGE-6 | As an owner or booker, I download a booking as an .ics file | M2 |

## EPIC 7 — Notifications

| ID | Story | Milestone |
|----|-------|-----------|
| NOTIF-1 | Notifications go through a sender interface. The first implementation stores them in-app | M1 |
| NOTIF-2 | As a user, I see my notifications with an unread count and mark them as read | M1 |
| NOTIF-3 | The owner is notified of each new, cancelled or rescheduled booking | M2 |
| NOTIF-4 | The booker gets a reminder notification one day before | M2 |

Acceptance notes:
- A code sent through the same channel as the login confirms intent but adds no real security. The README says so. Email delivery is planned.

## EPIC 8 — Quality and docs

| ID | Story | Milestone |
|----|-------|-----------|
| QA-1 | API integration tests for the booking flow, including two concurrent confirmations | M4 |
| QA-2 | Unit tests for slot computation: meeting lengths, buffers, blocked periods, conflicts, time zones and daylight saving changes | M4 |
| QA-3 | End-to-end test: create a calendar, book from the public page, confirm with the code (Playwright) | M4 |
| QA-4 | Accessibility pass on the public booking page | M4 |
| QA-5 | Docs: architecture, slot rules, demo accounts | M4 |

---

## Later ideas

- Email delivery (SMTP) for codes, confirmations and reminders, so bookers do not need an account
- Two-way sync with external calendars (CalDAV, Google, Microsoft)
- Team calendars with round-robin assignment
- Embeddable booking widget for other websites
- Paid bookings
- French and English UI
