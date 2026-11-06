# booking-calendars

A web app to share booking calendars.

Each user creates several calendars, each with its own public link. For each calendar, the user chooses the meeting lengths offered (15 minutes, 30 minutes or 1 hour), sets weekly hours, blocks periods when nobody can book, and sets a minimum gap before and after meetings. Anyone with the link sees the free time slots, books one and confirms it with a 6-digit code. A slot already taken on any of the owner's calendars is never offered.

## Who it is for

Freelancers and small teams who want to be booked simply, without back-and-forth emails.

## What it is not

- No sync with external calendars, no payment, no video meeting links.
- No email yet: people who book create a light account and get their code as an in-app notification.

## Stack

- Front end: Angular workspace (app + libraries)
- Back end: ASP.NET Core Web API (C#)
- Database: SQLite with EF Core

## Status

Milestone M0 (foundation). What works today:

- Create an account with your name, email, password and time zone, log in, log out, change your password and your time zone. Sessions expire after 60 minutes, and you are sent back to the login page.
- Create, edit and delete calendars with a title, a description and a location note. Each calendar gets a public link built from its title (`/book/{slug}`); you can change it, and links are unique. Copy each link from the calendar list.
- Choose the meeting lengths of each calendar (15 minutes, 30 minutes, 1 hour, one or more), with a buffer before and after each length.
- Set weekly hours per calendar: one or more time ranges per weekday, read in your time zone. Overlapping or empty ranges are refused.
- Pause a calendar so it takes no bookings, and resume it.
- Block whole days on one calendar or on all your calendars at once, with an optional label. Overlapping periods and periods that end before they start are refused.

The public booking page, free slots and bookings come with the next milestones (see [ROADMAP.md](ROADMAP.md)).

## Getting started

### Prerequisites

- [.NET SDK 10.0.403](https://dotnet.microsoft.com/download) or a later 10.0 feature band
- [Node.js 24](https://nodejs.org/) with npm 11

### Run the API

The access tokens are signed with a key that is never stored in the repository. The API refuses to start without it: set `Jwt__SigningKey` to a base64 key of at least 32 bytes in the terminal that runs the API.

Bash:

```bash
export Jwt__SigningKey="$(openssl rand -base64 32)"
cd src/backend
dotnet run --project src/BookingCalendars.Api --launch-profile http
```

PowerShell:

```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$env:Jwt__SigningKey = [Convert]::ToBase64String($bytes)
cd src/backend
dotnet run --project src/BookingCalendars.Api --launch-profile http
```

The API listens on http://localhost:5085. In Development it creates the SQLite database in `src/backend/src/BookingCalendars.Api/App_Data/`, applies the migrations, and adds the demo data below when the database has no users. A new key logs everybody out, so keep the same key between restarts if you want to stay logged in.

### Run the web app

```bash
cd src/frontend
npm ci
npm start
```

Open http://localhost:4200. Calls to `/api` are sent to the API on port 5085.

### Demo data

All demo accounts use the password `DemoPass123!`. They are fictional and only exist in Development.

| Name | Email | Role | Time zone | Calendars |
|------|-------|------|-----------|-----------|
| Mila Laurent | mila.laurent@example.com | Owner | Europe/Paris | Intro call (15 and 30 min), Coaching session (1 hour) |
| Owen Hart | owen.hart@example.com | Owner | America/New_York | Project review (30 min and 1 hour) |
| Ines Moreau | ines.moreau@example.com | Booker | Europe/Paris | — |
| Theo Brandt | theo.brandt@example.com | Booker | Europe/Berlin | — |

Each demo calendar has weekly hours and buffers. Mila also has two upcoming blocked periods, counted from the day the demo data is created: a training day on the coaching calendar in 9 days, and holidays on all her calendars from 21 to 25 days ahead. Bookers have no calendars yet; booking comes with milestone M1.

To start again from an empty demo, stop the API and delete `src/backend/src/BookingCalendars.Api/App_Data/`.

### Run the tests

Back end (with coverage, migrations and generated code excluded). The tests set their own signing key, so `Jwt__SigningKey` is not needed:

```bash
cd src/backend
dotnet test BookingCalendars.slnx --settings coverage.runsettings --collect "XPlat Code Coverage"
```

Front end (Vitest, the run fails below 100% coverage):

```bash
cd src/frontend
npm run test:ci
```

## Project layout

```
src/backend/    ASP.NET Core solution: Domain, Application.Contracts, Application, Infrastructure, Api, and their test projects
src/frontend/   Angular workspace: projects/apps/booking-calendars-app and the shared libraries in projects/libs/shared
```

## License

[MIT](LICENSE)
