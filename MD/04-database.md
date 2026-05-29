# Database Architektúra

## Áttekintés

A rendszer két különálló adatbázist használ, különböző felelősségi körrel:

| Adatbázis | Technológia | Felelősség |
|---|---|---|
| Event store | Supabase (PostgreSQL) | n8n által feldolgozott hírek tárolása |
| User store | SQLite | Felhasználók, szerepkörök, alert preferenciák |

> **Miért két adatbázis?**
> A Supabase az n8n workflow-ból közvetlenül elérhető, kezeli a nagy volumenű event adatot. Az SQLite a .NET WebAPI mellé kerül, user management és preferencia tárolásra — egyszerű, fájl alapú, nem igényel külön infrastruktúrát MVP-hez.

---

## 1. Supabase — Event Store

### Táblák

#### `events`

Az n8n által feldolgozott és Gemini Flash által strukturált híresemények.

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

| Mező | Típus | Leírás |
|---|---|---|
| `id` | UUID | Egyedi azonosító |
| `source_url` | TEXT | Az eredeti cikk URL-je |
| `source_feed` | TEXT | Melyik RSS feed-ből érkezett |
| `title` | TEXT | Gemini által kinyert cím |
| `summary` | TEXT | Gemini által generált összefoglaló (max 3 mondat) |
| `category` | TEXT | Fix kategória enum |
| `keywords` | TEXT[] | Gemini által kinyert kulcsszavak tömbje |
| `published_at` | TIMESTAMPTZ | Eredeti publikálás ideje az RSS-ben |
| `fetched_at` | TIMESTAMPTZ | Mikor dolgozta fel az n8n |
| `processed` | BOOLEAN | Ment-e már ki értesítés erre az eventre |
| `raw_content` | TEXT | Eredeti RSS tartalom (debug célra, opcionális) |

#### `processed_notifications`

Nyilvántartja, hogy melyik usernek melyik eventről ment már értesítés — duplikáció elkerülésére.

```sql
CREATE TABLE processed_notifications (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id   UUID NOT NULL REFERENCES events(id),
    user_id    INTEGER NOT NULL,
    channel    TEXT NOT NULL CHECK (channel IN ('email', 'slack')),
    sent_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

| Mező | Típus | Leírás |
|---|---|---|
| `event_id` | UUID | Melyik event |
| `user_id` | INTEGER | A .NET WebAPI SQLite user ID-ja |
| `channel` | TEXT | Melyik csatornán ment ki |
| `sent_at` | TIMESTAMPTZ | Mikor lett elküldve |

> **Megjegyzés:** A `user_id` itt a SQLite-ban lévő user integer ID-ja. Ez egy egyszerű referencia, nem foreign key constraint Supabase szintjén (két különböző adatbázis), az integritást az alkalmazás logika biztosítja.

### Indexek — Supabase

```sql
-- Leggyakoribb lekérdezés: feldolgozatlan eventek kategória szerint
CREATE INDEX idx_events_category ON events(category);
CREATE INDEX idx_events_processed ON events(processed);
CREATE INDEX idx_events_fetched_at ON events(fetched_at DESC);

-- Duplikáció ellenőrzés
CREATE UNIQUE INDEX idx_processed_notifications_unique
    ON processed_notifications(event_id, user_id, channel);

-- Source URL alapján duplikáció szűrés (ne dolgozzuk fel kétszer ugyanazt a cikket)
CREATE UNIQUE INDEX idx_events_source_url ON events(source_url);
```

### Adattisztítás stratégia

- Az `events` táblában az adatok **30 napig** maradnak meg, utána törölhetők
- A `processed_notifications` tábla az `events`-sel együtt tisztítható
- MVP-ben ez manuális vagy egy egyszerű n8n scheduled workflow, élesben Supabase Edge Function

---

## 2. SQLite — User Store

A .NET WebAPI mellett futó, fájl alapú adatbázis. Helye: `./data/userstore.db`

### Táblák

#### `users`

```sql
CREATE TABLE users (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    email         TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    full_name     TEXT NOT NULL,
    role          TEXT NOT NULL DEFAULT 'user'
                  CHECK (role IN ('admin', 'user')),
    is_active     INTEGER NOT NULL DEFAULT 1,
    created_at    TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at    TEXT NOT NULL DEFAULT (datetime('now'))
);
```

| Mező | Típus | Leírás |
|---|---|---|
| `id` | INTEGER | Auto increment PK — ez az ID amit Supabase-ben referálunk |
| `email` | TEXT | Egyedi, login azonosító |
| `password_hash` | TEXT | BCrypt hash |
| `full_name` | TEXT | Megjelenítési név |
| `role` | TEXT | `admin` vagy `user` |
| `is_active` | INTEGER | Soft delete (1 = aktív, 0 = inaktív) |

#### `alert_preferences`

Egy usernek több preference-e is lehet (pl. Breaking News + Market egyszerre).

```sql
CREATE TABLE alert_preferences (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    category   TEXT NOT NULL
               CHECK (category IN (
                   'breaking_news',
                   'market',
                   'natural_disaster',
                   'tech'
               )),
    keyword    TEXT,
    is_active  INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    UNIQUE(user_id, category, keyword)
);
```

| Mező | Típus | Leírás |
|---|---|---|
| `user_id` | INTEGER | FK a users táblára |
| `category` | TEXT | Fix kategória |
| `keyword` | TEXT | Opcionális kulcsszó szűrő (NULL = nincs szűrő) |
| `is_active` | INTEGER | Kikapcsolható anélkül hogy törölnénk |

#### `notification_channels`

Felhasználónként tárolja az értesítési csatorna adatait.

```sql
CREATE TABLE notification_channels (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id     INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    channel     TEXT NOT NULL CHECK (channel IN ('email', 'slack')),
    destination TEXT NOT NULL,
    is_active   INTEGER NOT NULL DEFAULT 1,
    created_at  TEXT NOT NULL DEFAULT (datetime('now')),
    UNIQUE(user_id, channel)
);
```

| Mező | Típus | Leírás |
|---|---|---|
| `user_id` | INTEGER | FK a users táblára |
| `channel` | TEXT | `email` vagy `slack` |
| `destination` | TEXT | Email cím VAGY Slack webhook URL |
| `is_active` | INTEGER | Kikapcsolható csatorna |

> **Miért nem egy mezőben az email és Slack?**
> Egy usernek lehet egyszerre email ÉS Slack csatornája. A `UNIQUE(user_id, channel)` constraint biztosítja, hogy típusonként csak egy destination legyen, de mindkét típust felveheti.

### Indexek — SQLite

```sql
CREATE INDEX idx_alert_preferences_user_id ON alert_preferences(user_id);
CREATE INDEX idx_alert_preferences_category ON alert_preferences(category);
CREATE INDEX idx_notification_channels_user_id ON notification_channels(user_id);
```

### Seed adat — Admin user

```sql
INSERT INTO users (email, password_hash, full_name, role)
VALUES (
    'admin@alertsystem.local',
    '$2a$12$...', -- BCrypt hash, indításkor generálva
    'System Admin',
    'admin'
);
```

---

## Adatfolyam összefoglalás

```
RSS feed
  ↓
n8n (Gemini Flash feldolgoz)
  ↓
Supabase: events tábla ← új sor kerül be
  ↓
n8n worker fut (ütemezett)
  ↓
SQLite-ból lekéri az aktív user preferenciákat (WebAPI endpoint-on keresztül)
  ↓
Összeveti: event.category == preference.category
           ÉS (preference.keyword IS NULL
               OR event.keywords CONTAINS preference.keyword
               OR event.summary CONTAINS preference.keyword)
  ↓
Ha match: notification_channels alapján küld Email / Slack értesítést
  ↓
Supabase: processed_notifications-be beírja (duplikáció védelem)
```

---

## Kategória értékek — közös enum

Mindkét adatbázisban és az alkalmazás kódjában ugyanezek a string értékek szerepelnek:

| Kód | Megjelenítés |
|---|---|
| `breaking_news` | Breaking News |
| `market` | Market |
| `natural_disaster` | Natural Disaster |
| `tech` | Tech |

---

## Migrációs stratégia (MVP után)

Ha a user szám nő és az SQLite szűk lesz:
- A `users`, `alert_preferences`, `notification_channels` táblák átmigrálhatók Supabase-be
- A WebAPI connection string és az EF Core provider cseréje elegendő
- A Supabase `processed_notifications` `user_id` mezője ekkor már valódi FK lehet
