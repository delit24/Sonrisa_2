# Alert Rendszer — Projekt Összefoglaló

## Az eredeti brief

> "We want users to be able to set up alerts so they get notified when something important happens in the world — like breaking news, market movements, natural disasters, that kind of thing. Should work for both email and Slack. Make it flexible enough that we can add more channels later. We need an admin view too."

---

## Értelmezés és scope döntések

### Mi ez a rendszer?

Egy értesítési platform ahol:
- Felhasználók beállíthatják, milyen típusú világeseményekről kapjanak értesítést
- Az értesítés érkezhet emailben vagy Slack üzenetként
- Adminok kezelhetik a felhasználókat és látják a rendszer állapotát

### Mi az "important event"?

A brief nem határozza meg a forrást — mi RSS feed-eket használunk. Az n8n workflow Gemini Flash AI segítségével elemzi és kategorizálja a bejövő híreket, majd strukturált formában elmenti Supabase-be.

### Alert beállítás logika — Kombinált (Kategória + Kulcsszó)

A felhasználó két szinten szűrhet:

1. **Kategória (kötelező)** — legalább egyet ki kell választani:

| Kategória | Tartalom |
|---|---|
| Breaking News | Háború, terrortámadás, politikai válság |
| Market | Tőzsde, kripto, gazdasági mutatók |
| Natural Disaster | Földrengés, árvíz, hurrikán |
| Tech | AI, kibertámadás, nagy tech cégek |

2. **Kulcsszó (opcionális)** — egyszerű string match a feldolgozott hír szövegében. Példa: kategória = Natural Disaster, kulcsszó = "Hungary" → csak magyarországi természeti katasztrófákról kap értesítést.

### Értesítési csatornák (MVP scope)

| Csatorna | Megközelítés |
|---|---|
| Email | SMTP (Gmail app password vagy Mailtrap teszteléshez), n8n Email Send node |
| Slack | Fix workspace, bot token vagy incoming webhook, n8n Slack node |

> **Megjegyzés:** A bővíthetőség az n8n workflow szintjén valósul meg — új csatorna = új n8n node, nem kódmódosítás a backendben.

---

## Architektúra áttekintés

```
RSS feed(ek)
     ↓
n8n workflow
  ├── Gemini Flash: elemzés, kategorizálás, strukturálás
  ├── Supabase: strukturált events mentése
  ├── Supabase: user preferenciák olvasása
  └── Értesítés küldése: Email (SMTP) + Slack (webhook)
     ↓
Supabase
  └── events tábla (feldolgozott hírek)
     ↓
.NET WebAPI
  ├── User management (JWT auth, szerepkörök)
  ├── Alert preferenciák CRUD
  └── SQLite: user adatok, preferenciák
     ↓
Angular Frontend
  ├── Admin nézet: userek kezelése, rendszer állapot
  └── User nézet: saját alert beállítások
```

---

## Szerepkörök

### Admin
- Felhasználók listázása, szerkesztése, törlése
- Rendszer állapot megtekintése
- RSS források kezelése (opcionális, MVP után)

### Felhasználó (User)
- Saját alert preferenciák beállítása (kategória + opcionális kulcsszó)
- Értesítési csatorna megadása (email cím és/vagy Slack webhook URL)
- Saját beállítások szerkesztése

---

## Tech stack döntések

| Réteg | Technológia | Indok |
|---|---|---|
| Hír feldolgozás | n8n | Vizuális workflow, natív Slack/Email node, bővíthető |
| AI elemzés | Gemini Flash | Gyors, olcsó, jól strukturált JSON output |
| Event adatbázis | Supabase (PostgreSQL) | Managed, REST API-val megszólítható n8n-ből is |
| Backend | .NET WebAPI | User management, auth, business logika |
| User adatbázis | SQLite | Egyszerű, MVP-hez elegendő, könnyen migrálható |
| Frontend | Angular | Admin + user nézet, két szerepkör |

---

## Nyitott kérdések (MVP után döntendő)

- RSS forrás lista: hány feed, milyen domain-ek — ezt még meg kell határozni
- Már feldolgozott hírek tárolása: tároljuk-e a Supabase-ben hosszú távon vagy törlünk
- Supabase → WebAPI kommunikáció: n8n közvetlenül olvassa a user preferenciákat Supabase-ből, vagy WebAPI endpointon keresztül kérdezi le — MVP-ben Supabase direkt
- Slack: egy fix workspace bot token elegendő MVP-hez, OAuth flow MVP után
- SQLite → PostgreSQL migráció: ha a user szám nő, a WebAPI SQLite-ja migrálható

---

## Részletes dokumentumok

| Fájl | Tartalom |
|---|---|
| `01-n8n.md` | n8n workflow részletes terv, node-ok, Gemini prompt, Supabase séma |
| `02-backend.md` | .NET WebAPI endpointok, auth, SQLite séma, worker logika |
| `03-frontend.md` | Angular struktúra, komponensek, admin és user nézet |
| `04-database.md` | Supabase events tábla séma, SQLite user séma, kapcsolatok |
