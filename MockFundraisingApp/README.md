# MockFundraisingApp

A simple ASP.NET Core MVC application demonstrating authentication, authorization, and Firestore-backed data access for a mock fundraising platform.

---

## Prerequisites

- .NET 10 SDK
- Firebase project with Authentication enabled (Email/Password and Google)
- Firestore database enabled

---

## Configuration

Set the following values in `appsettings.json` :

```json
{
  "Firebase": {
    "ProjectId": "YOUR_PROJECT_ID",
    "AuthDomain": "YOUR_PROJECT_ID.firebaseapp.com",
    "WebApiKey": "YOUR_WEB_API_KEY",
    "ServiceAccountPath": "App_Data/firebase-service-account.json"
  }
}
```

Place the Firebase Admin service account JSON file at:

```
App_Data/firebase-service-account.json
```

---

## Run Locally

From the project root:

```bash
dotnet restore
dotnet run
```

Then open your browser to:

```
https://localhost:7067/
```

---

## Authentication

- Sign-in uses Firebase Authentication on the client (Email/Password and Google).
- After sign-in, the Firebase ID token is posted to `/Auth/Session`.
- The server verifies the token using Firebase Admin and creates an ASP.NET authentication cookie so `[Authorize]` works.
- Apple sign-in is present in the UI but requires hosting and domain configuration to fully validate in production.

---

## Firestore Data Model

- `requests/{requestId}` stores fundraising requests and derived aggregate fields used for sorting and display (totals, remaining amount, progress percentage).
- `requests/{requestId}/donations/{donationId}` stores individual donations (donor name, amount, timestamp).
- Donations update totals transactionally to keep aggregate fields consistent.

---

## Limitations / Notes

- No real payment processing (donations are simulated).
- Minimal authorization model (authenticated users can create requests).
- Validation and error handling are scoped to core flows for this assessment.
- Apple sign-in is deferred until the application is hosted.
