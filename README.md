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

Early development. Setup instructions will be added as the code lands.

## License

[MIT](LICENSE)
