# MEMS Web API reference

Base URL when run through the AppHost: `https://localhost:7045`
Interactive documentation (OpenAPI + Swagger UI): `https://localhost:7045/swagger`, document at `/openapi/v1.json`.
Ready-made requests: `src/MEMS.Api/MEMS.Api.http`.

## Conventions

- **Tenants.** Tenant data lives under `/api/{tenant}/...`, where `{tenant}` is the municipality's URL
  slug (`burnaby`, `surrey`, `richmond`, ...). The slug is case-insensitive. An unknown slug returns 404.
- **Isolation.** Every query under `/api/{tenant}` is restricted to that tenant in the database layer.
  An id that belongs to another tenant returns **404** (`/api/burnaby/candidates/15`, where 15 is a
  Surrey candidate). A write to another tenant returns **403**.
- **Public reads.** Every `GET` is anonymous because the public site has no sign-in. An administrator's
  token adds no read access: `GET /api/surrey/candidates` with burnabyadmin's token returns Surrey's
  published candidates, the same response any visitor gets. Administrator accounts are not exposed by
  the API.
- **Authentication.** Writes need `Authorization: Bearer <accessToken>` from
  `POST /api/auth/login`. Tokens last 30 minutes.
- **Authorization.**
  - *Super Admin*: every endpoint.
  - *Municipality Administrator*: writes under their own `/api/{tenant}` only; may update their own
    municipality (not its slug); cannot create or delete municipalities.
- **Errors.** All errors are RFC 7807 `application/problem+json`, with a `traceId` that matches the
  trace in the Aspire dashboard. Exception details are never returned.
- **Privacy.** No endpoint accepts or returns voting-guide selections.

## Endpoints

| Method | Path | Auth | Success | Errors |
|---|---|---|---|---|
| POST | `/api/auth/login` | – | 200 `LoginResponse` | 400, 401, 429 |
| GET | `/api/municipalities` | – | 200 `MunicipalityDto[]` | – |
| GET | `/api/municipalities/{id}` | – | 200 `MunicipalityDto` | 404 |
| POST | `/api/municipalities` | Super Admin | 201 `MunicipalityDto` | 400, 401, 403, 409 |
| PUT | `/api/municipalities/{id}` | Any admin (own tenant) | 200 `MunicipalityDto` | 400, 401, 403, 404, 409 |
| DELETE | `/api/municipalities/{id}` | Super Admin | 204 | 401, 403, 404 |
| PUT | `/api/municipalities/{id}/logo` | Any admin (own tenant) | 200 `ImageUploadResult` | 400, 401, 404 |
| DELETE | `/api/municipalities/{id}/logo` | Any admin (own tenant) | 204 | 401, 404 |
| GET | `/api/{tenant}` | – | 200 `MunicipalityDto` | 404 |
| GET | `/api/{tenant}/positions` | – | 200 `PositionDto[]` | 404 |
| GET | `/api/{tenant}/positions/{id}` | – | 200 `PositionDto` | 404 |
| POST | `/api/{tenant}/positions` | Tenant admin | 201 `PositionDto` | 400, 401, 403, 404, 409 |
| PUT | `/api/{tenant}/positions/{id}` | Tenant admin | 200 `PositionDto` | 400, 401, 403, 404, 409 |
| DELETE | `/api/{tenant}/positions/{id}` | Tenant admin | 204 | 401, 403, 404 |
| GET | `/api/{tenant}/candidates?search=&positionId=&page=&pageSize=` | – | 200 `PagedResult<CandidateDto>` | 400, 404 |
| GET | `/api/{tenant}/candidates/{id}` | – | 200 `CandidateDto` (with endorsements) | 404 |
| POST | `/api/{tenant}/candidates` | Tenant admin | 201 `CandidateDto` | 400, 401, 403, 404 |
| PUT | `/api/{tenant}/candidates/{id}` | Tenant admin | 200 `CandidateDto` | 400, 401, 403, 404 |
| DELETE | `/api/{tenant}/candidates/{id}` | Tenant admin | 204 | 401, 403, 404 |
| PUT | `/api/{tenant}/candidates/{id}/photo` | Tenant admin | 200 `ImageUploadResult` | 400, 401, 403, 404 |
| DELETE | `/api/{tenant}/candidates/{id}/photo` | Tenant admin | 204 | 401, 403, 404 |
| GET | `/api/{tenant}/images/{guid}` | – | 200 image bytes | 404 |
| GET | `/health`, `/alive` | – | 200 (Development) | 503 |

*Tenant admin* = Super Admin, or the Municipality Administrator of `{tenant}`.

Image uploads are `multipart/form-data` with a single `file` field: JPG, PNG, GIF or WEBP, at most 5 MB.
The content is checked by file signature, not by name or browser content type.

## Request and response examples

All examples below are real responses from the running system.

### Sign in

```http
POST /api/auth/login
Content-Type: application/json

{ "userName": "burnabyadmin", "password": "P@$$w0rd" }
```

```json
200 OK
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9…",
  "expiresAtUtc": "2026-11-17T09:10:41Z",
  "userName": "burnabyadmin",
  "role": "MunicipalityAdmin",
  "tenantSlug": "burnaby"
}
```

A wrong password and an unknown username give the same answer:

```json
401 Unauthorized
{ "title": "Sign-in failed", "status": 401, "detail": "The username or password is incorrect.", "traceId": "00-…" }
```

### Search candidates in one tenant

```http
GET /api/burnaby/candidates?search=zhao&pageSize=1
```

```json
200 OK
{
  "items": [
    {
      "id": 1,
      "name": "Helen Zhao",
      "profile": "Community organizer prioritizing tenant protections and non-market housing around SkyTrain stations.",
      "photoImageId": null,
      "website": "https://helenzhao.example.com",
      "email": "helen.zhao@example.com",
      "socialX": "https://x.com/helenzhao",
      "dateCreated": "2026-11-17T03:33:11.8268479",
      "positionId": 1,
      "positionName": "Mayor",
      "seatsAvailable": 1,
      "municipalityId": 1,
      "municipalityName": "Burnaby",
      "municipalitySlug": "burnaby",
      "endorsements": []
    }
  ],
  "page": 1,
  "pageSize": 1,
  "totalCount": 1,
  "totalPages": 1
}
```

### Create a position

```http
POST /api/burnaby/positions
Authorization: Bearer <burnabyadmin token>
Content-Type: application/json

{ "name": "Park Board Commissioner", "seatsAvailable": 3 }
```

```json
201 Created
Location: https://localhost:7045/api/burnaby/positions/2004
{ "id": 2004, "name": "Park Board Commissioner", "seatsAvailable": 3, "municipalityId": 1, "municipalityName": "Burnaby", "candidateCount": 0 }
```

The municipality always comes from the URL. A request body cannot place data in another tenant.

### Create a municipality (tenant)

```http
POST /api/municipalities
Authorization: Bearer <super token>
Content-Type: application/json

{ "name": "Coquitlam", "slug": "coquitlam", "province": "British Columbia",
  "electionDate": "2026-10-17", "tagline": "Where trails meet the city.", "themeColor": "#9a3412" }
```

`201 Created` with the new `MunicipalityDto`. The public site is immediately available at `/coquitlam`.
No new application is needed.

## Error responses

**400: validation** (every invalid field is listed)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": [ "Candidate name must be between 2 and 100 characters." ],
    "Email": [ "Email must be a valid email address." ],
    "Website": [ "Website must be a valid http:// or https:// address." ]
  },
  "traceId": "00-841bf737b97fbeb0a2773419c71e828c-8de160f6029c1a1b-01"
}
```

**401: no or expired token**

```json
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2", "title": "Unauthorized", "status": 401, "traceId": "00-…" }
```

**403: another tenant** (`burnabyadmin` → `POST /api/surrey/positions`)

```json
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4", "title": "Forbidden", "status": 403, "traceId": "00-…" }
```

**404: unknown tenant or a record outside the tenant**

```json
{ "title": "Municipality not found", "status": 404, "detail": "There is no municipality at 'nowhere'.", "traceId": "00-…" }
{ "title": "Not found", "status": 404, "detail": "The candidate was not found.", "traceId": "00-…" }
```

**409: duplicate**

```json
{ "title": "Conflict", "status": 409, "errors": { "Slug": [ "This URL slug is already used by another municipality." ] } }
```

**429: too many sign-in attempts** (more than 10 per minute from one address)

**500: unexpected error** (details are only in the logs)

```json
{ "title": "Unexpected error", "status": 500,
  "detail": "Something went wrong on our side. Please try again; if it keeps happening, contact the administrator and quote the trace id.",
  "traceId": "00-…" }
```
