# Supabase Konfiguráció

## Áttekintés

A Supabase a feldolgozott híresemények tárolására szolgál. Kizárólag az n8n éri el, a .NET WebAPI nem kapcsolódik hozzá.

```
n8n (feed-processor)    → INSERT → Supabase events tábla
n8n (alert-dispatcher)  → SELECT → Supabase events tábla
n8n (alert-dispatcher)  → INSERT → Supabase processed_notifications tábla
```

---

## Projekt létrehozása

1. Supabase fiókba belépés: `https://supabase.com`
2. "New Project" létrehozása
3. Ajánlott beállítások:
   - **Name:** `alert-system`
   - **Region:** `West EU (Ireland)` — ha EU-s felhasználók, GDPR szempontból kedvező
   - **Database Password:** erős, generált jelszó — ezt elmenteni!

---

## API Key kezelés

A Supabase projektben két kulcs érhető el (`Settings → API`):

| Kulcs | Neve | Mire való |
|---|---|---|
| `anon` key | Publikus kulcs | Kliens oldali hozzáféréshez — **mi NEM használjuk** |
| `service_role` key | Privát kulcs | Szerver oldali teljes hozzáférés, megkerüli az RLS-t |

**Mi csak a `service_role` key-t használjuk**, kizárólag az n8n-ben, environment variable-ként tárolva:

```
SUPABASE_URL=https://xxxxxxxxxxx.supabase.co
SUPABASE_SERVICE_KEY=eyJ...
```

> **Fontos:** A `service_role` key soha ne kerüljön frontend kódba, publikus repóba, vagy logba. Csak az n8n szerver environment variable-jeiben él.

---

## Táblák létrehozása

A Supabase SQL Editorban futtatandó (`SQL Editor → New Query`):

### 1. events tábla

```sql
CREATE TABLE events (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_url    TEXT NOT NULL,
    source_feed   TEXT NOT NULL,
    title         TEXT NOT NULL,
    summary       TEXT NOT NULL,
    category      TEXT NOT NULL
                  CHECK (category IN (
                      'breaking_news',
                      'market',
                      'natural_disaster',
                      'tech'
                  )),
    keywords      TEXT[],
    published_at  TIMESTAMPTZ NOT NULL,
    fetched_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    processed     BOOLEAN NOT NULL DEFAULT false,
    raw_content   TEXT
);
```

### 2. processed_notifications tábla

```sql
CREATE TABLE processed_notifications (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id   UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    user_id    INTEGER NOT NULL,
    channel    TEXT NOT NULL CHECK (channel IN ('email', 'slack')),
    sent_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 3. Indexek

```sql
-- events tábla indexek
CREATE INDEX idx_events_category
    ON events(category);

CREATE INDEX idx_events_processed
    ON events(processed);

CREATE INDEX idx_events_fetched_at
    ON events(fetched_at DESC);

-- Duplikált cikk védelem
CREATE UNIQUE INDEX idx_events_source_url
    ON events(source_url);

-- Duplikált értesítés védelem
CREATE UNIQUE INDEX idx_processed_notifications_unique
    ON processed_notifications(event_id, user_id, channel);
```

---

## Row Level Security (RLS)

A Supabase alapból bekapcsolt RLS-sel véd minden táblát. Mivel mi `service_role` key-t használunk az n8n-ből, az RLS megkerülhető — de a táblán be kell kapcsolni, hogy ne legyen véletlenül nyilvánosan elérhető.

```sql
-- RLS bekapcsolása mindkét táblán
ALTER TABLE events ENABLE ROW LEVEL SECURITY;
ALTER TABLE processed_notifications ENABLE ROW LEVEL SECURITY;

-- Nem adunk meg semmilyen policy-t
-- service_role key megkerüli az RLS-t → az n8n teljes hozzáféréssel rendelkezik
-- anon key viszont semmit sem lát → a táblák publikusan nem érhetők el
```

> **Eredmény:** Az n8n service_role key-jel mindent tud csinálni. Bárki más (anon key, publikus hozzáférés) semmit sem lát. Ez a legbiztonságosabb MVP beállítás.

---

## n8n kapcsolat a Supabase-hez

Az n8n-ben kétféleképpen lehet csatlakozni Supabase-hez:

### A) Supabase beépített n8n node
- Egyszerűbb konfiguráció
- Csak alap CRUD műveleteket támogat
- MVP-hez elegendő

### B) HTTP Request node (Supabase REST API)
- Rugalmasabb, komplex lekérdezések
- Több konfiguráció szükséges

**MVP-ben az A) megközelítést használjuk.**

### Supabase credential beállítása n8n-ben

1. n8n-ben: `Credentials → New → Supabase`
2. Mezők:
   - **Host:** `https://xxxxxxxxxxx.supabase.co`
   - **Service Role Secret:** `eyJ...` (a service_role key)
3. Mentés után ez a credential használható minden Supabase node-ban

---

## Adatstruktúra — példa sorok

### events tábla

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "source_url": "https://www.bbc.com/news/world-12345678",
  "source_feed": "https://feeds.bbci.co.uk/news/rss.xml",
  "title": "Major earthquake strikes Turkey",
  "summary": "A 7.2 magnitude earthquake struck southern Turkey on Monday, causing widespread damage. Rescue teams have been deployed to affected areas. The death toll is expected to rise as search operations continue.",
  "category": "natural_disaster",
  "keywords": ["Turkey", "earthquake", "rescue", "magnitude", "damage"],
  "published_at": "2024-01-15T08:30:00Z",
  "fetched_at": "2024-01-15T08:45:00Z",
  "processed": true,
  "raw_content": "..."
}
```

### processed_notifications tábla

```json
{
  "id": "f1e2d3c4-b5a6-7890-abcd-ef1234567890",
  "event_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "user_id": 1,
  "channel": "email",
  "sent_at": "2024-01-15T08:50:00Z"
}
```

---

## Adattisztítás

MVP-ben manuális, élesben automatizálható.

### Manuális tisztítás (SQL Editor)

```sql
-- 30 napnál régebbi feldolgozott eventek törlése
DELETE FROM events
WHERE processed = true
  AND fetched_at < now() - INTERVAL '30 days';
```

### Automatikus tisztítás (opcionális, MVP után)

Egy n8n scheduled workflow hetente egyszer futtatja a fenti törlést, vagy Supabase Edge Function végzi.

---

## Ellenőrzési lista — Supabase beüzemelés

- [ ] Supabase projekt létrehozva
- [ ] `service_role` key biztonságosan elmentve
- [ ] `events` tábla létrehozva SQL Editorban
- [ ] `processed_notifications` tábla létrehozva
- [ ] Indexek létrehozva
- [ ] RLS bekapcsolva mindkét táblán
- [ ] n8n Supabase credential beállítva
- [ ] n8n environment variable-ök feltöltve (`SUPABASE_URL`, `SUPABASE_SERVICE_KEY`)
- [ ] Teszt INSERT futtatva SQL Editorból — sikeres
- [ ] n8n-ből teszt lekérdezés — sikeres
