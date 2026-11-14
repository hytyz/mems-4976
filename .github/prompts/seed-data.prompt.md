# Seed data

Add realistic seed data for a British Columbia municipality to `src/MEMS.Data/Seeding/SeedData.cs`.

- Municipality: name, unique lowercase slug, tagline, description, theme colour (#rrggbb).
- Positions: Mayor (1 seat), Councillor (6–8 seats), School Trustee (2–7 seats).
- 6–10 candidates with short, neutral, fictional biographies; use `example.com` addresses only.
- Optionally one endorsement for a mayoral candidate.
- Optionally an administrator username (lowercase, `<slug>admin`).

Do not use real people's names together with real offices. Keep every string within the entity's
maximum length.
