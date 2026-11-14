# Entity Relationship Diagram (Assignment 2)

MEMS 2 database (SQL Server, EF Core Code First, migration `InitialCreate`).
Every tenant-owned table carries a `MunicipalityId` foreign key.

```mermaid
erDiagram
    Municipalities ||--o{ Positions : "has"
    Municipalities ||--o{ Candidates : "owns (tenant key)"
    Municipalities ||--o{ Images : "owns"
    Municipalities ||--o{ AspNetUsers : "administered by (Municipality Admin)"
    Municipalities |o--o| Images : "logo"
    Positions ||--o{ Candidates : "contested by (PositionId, MunicipalityId)"
    Candidates ||--o{ Endorsements : "receives"
    Candidates |o--o| Images : "photo"
    AspNetUsers ||--o{ AspNetUserRoles : "has"
    AspNetRoles ||--o{ AspNetUserRoles : "granted to"

    Municipalities {
        int MunicipalityId PK
        nvarchar100 Name UK
        nvarchar50 Slug UK "URL segment, CHECK lowercase/digits/hyphens"
        nvarchar50 Province
        date ElectionDate "CHECK 2000-2100"
        nvarchar2000 Description
        nvarchar150 Tagline
        nvarchar7 ThemeColor "CHECK #rrggbb"
        uniqueidentifier LogoImageId FK
    }

    Positions {
        int PositionId PK
        int MunicipalityId FK "tenant"
        nvarchar100 Name "UNIQUE per municipality"
        int SeatsAvailable "CHECK 1-100"
    }

    Candidates {
        int CandidateId PK
        int MunicipalityId FK "tenant"
        int PositionId FK "composite FK with MunicipalityId"
        nvarchar100 Name
        nvarchar2000 Profile
        nvarchar254 Email
        nvarchar300 Website
        nvarchar300 VideoUrl
        nvarchar300 SocialX
        nvarchar300 SocialFacebook
        nvarchar300 SocialInstagram
        nvarchar300 SocialLinkedIn
        uniqueidentifier PhotoImageId FK
        datetime2 DateCreated
    }

    Endorsements {
        int EndorsementId PK
        int CandidateId FK
        nvarchar100 EndorserName
        nvarchar150 Organization
        nvarchar1000 Text
    }

    Images {
        uniqueidentifier StoredImageId PK
        int MunicipalityId FK "tenant"
        nvarchar50 ContentType
        varbinary Data
        int Length "CHECK 1 byte - 5 MB"
        datetime2 UploadedUtc
    }

    AspNetUsers {
        nvarchar450 Id PK
        nvarchar256 UserName UK
        int MunicipalityId FK "NULL for Super Admin"
        nvarchar PasswordHash
        nvarchar SecurityStamp
    }

    AspNetRoles {
        nvarchar450 Id PK
        nvarchar256 Name UK "SuperAdmin or MunicipalityAdmin"
    }

    AspNetUserRoles {
        nvarchar450 UserId PK
        nvarchar450 RoleId PK
    }
```

## Notes

- **Tenant isolation in the database.** `Positions`, `Candidates`, `Images` and `AspNetUsers`
  reference `Municipalities`. `Candidates` uses a composite foreign key
  `(PositionId, MunicipalityId) → Positions(PositionId, MunicipalityId)`, so a candidate can never be
  attached to another municipality's position, even through a bug.
- **Row-level filtering in EF Core.** Global query filters on every tenant-owned entity restrict
  queries to the current tenant (`ITenantContext`). The filters fail closed.
- **Deletes.** Deleting a municipality removes its positions, candidates, endorsements, images and
  administrator accounts in one transaction. SQL Server allows one cascade path per table, so the
  second paths (`Candidates → Municipalities`, image references) are `NO ACTION` and handled by the
  service layer.
- **What is not stored.** There is no table for ballots, voting-guide selections or vote totals.
  The public site keeps selections in the browser's session storage only.
- **Changes from Assignment 1.** SQLite → SQL Server; one `MunicipalityAdmin` role plus
  `AspNetUsers.MunicipalityId` replaces one Identity role per municipality (`AdminRoleName`);
  new `Slug`, `Tagline`, `ThemeColor` columns; image files on disk → `Images` table; new
  `Candidates.MunicipalityId`; unique and check constraints added.
