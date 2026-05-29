# Sonrisa_2

Megkaptam a másik feladatot a Sonrisa-tól, mert első körben nem az én felvételi feladatomat kaptam meg, bár azt is meg tudtam csinálni. 

Ennél a feladatnál, is nagyon figyelmesen és aprólékosan kellett haladni , hogy egy értelmes működő modelt össze tudjak rakni.

Nyilván ez egy teszt rendszer, ígynme bajlódtam még azzal is hogy környezeti változókba rakom amit oda kell meg külön branch meg ilyenek, de természetesen ezek a dolgok egy éles projekten alap...

Az egész alkalmazás nálam összerekva fut, amit be is tudok mutatni, nyilván az n8n json ben lévő kulcsok  a felpusholt verzióban értelem szerűen nicsnenek benne.

I received the alternative assignment from Sonrisa because, in the first round, I didn't get my actual assessment task (although I was able to complete that one as well). For this task, I had to proceed very carefully and meticulously to build a meaningful and fully functional model. Obviously, since this is a test system, I didn't bother with setting up environment variables for everything or using separate branches, but naturally, these practices are absolute baseline requirements in a live production project.

The entire application is fully assembled and running on my machine, which I can also demonstrate. Naturally, the API keys and credentials in the n8n JSON file have been deliberately excluded from the pushed version.

Az infrastruktura és az app:

Áttekintés
Automatikus hírérzékelő és értesítési rendszer. A felhasználók beállítják milyen típusú hírekről akarnak értesítést kapni, a rendszer 15 percenként figyeli a híreket és automatikusan emailt vagy Slack üzenetet küld ha relevánsat talál.

A teljes folyamat
┌─────────────────────────────────────────────────────────────────┐
│                        FELHASZNÁLÓ                              │
│                                                                 │
│   Bejelentkezik az Angular frontendre                           │
│   Beállítja: kategória = Breaking News, kulcsszó = Iran         │
│   Megadja: email = sajat@gmail.com                              │
│                          │                                      │
│                          ▼                                      │
│              ┌─────────────────────┐                            │
│              │   .NET WebAPI       │                            │
│              │   SQLite adatbázis  │                            │
│              │   elmenti a beállít.│                            │
│              └─────────────────────┘                            │
└─────────────────────────────────────────────────────────────────┘

                    ⏰ 15 percenként automatikusan indul

┌─────────────────────────────────────────────────────────────────┐
│                      n8n WORKFLOW                               │
│                                                                 │
│  1. FEED BEOLVASÁS                                              │
│     📡 BBC RSS feed → 35+ cikk                                  │
│          │                                                      │
│          ▼                                                      │
│  2. LIMIT (csak 3 cikk teszteléshez)                            │
│          │                                                      │
│          ▼                                                      │
│  3. DUPLIKÁCIÓ ELLENŐRZÉS                                       │
│     ⚡ Supabase: szerepel már ez a cikk?                        │
│          │                                                      │
│     ┌────┴────┐                                                 │
│     │ ÚJ?    │ MÁR VOLT?                                        │
│     ▼        └──→ skip                                          │
│                                                                 │
│  4. AI ELEMZÉS (Groq / Llama 3.1)                               │
│     🤖 Kategorizálja: breaking_news / market /                  │
│        natural_disaster / tech                                  │
│        Kulcsszavakat kinyeri                                    │
│        3 mondatos összefoglalót ír                              │
│          │                                                      │
│          ▼                                                      │
│  5. RELEVANCIA SZŰRÉS                                           │
│     Vélemény / reklám cikkek kiszűrése                          │
│          │                                                      │
│          ▼                                                      │
│  6. SUPABASE MENTÉS                                             │
│     💾 events tábla: cikk + kategória + kulcsszavak             │
│          │                                                      │
│          ▼                                                      │
│  7. USER PREFERENCIÁK LEKÉRÉSE                                  │
│     🔗 GET /api/internal/user-preferences → .NET WebAPI         │
│     Visszakapja: ki, mire figyel, hova kap értesítést           │
│          │                                                      │
│          ▼                                                      │
│  8. EGYEZTETÉS (Match Events to Users)                          │
│     🔍 event.category == user.category?  ✓                      │
│         event.keywords tartalmazza a kulcsszót? ✓               │
│                                                                 │
│     Példa:                                                      │
│     Cikk: "Iran/US tárgyalások" → breaking_news, ["Iran","US"]  │
│     User: Breaking News + kulcsszó: Iran → ✅ MATCH!            │
│          │                                                      │
│     ┌────┴────────────┐                                         │
│     ▼                 ▼                                         │
│  📧 EMAIL          💬 SLACK                                     │
│  Gmail küld        Bot üzenetet küld                            │
│  az értesítést     a megadott channelbe                         │
│          │                 │                                    │
│          └────────┬────────┘                                    │
│                   ▼                                             │
│  9. DUPLIKÁCIÓ VÉDELEM                                          │
│     💾 processed_notifications tábla: ki kapott már értesítést  │
│     💾 events tábla: processed = TRUE → nem küld újra           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘

Komponensek összefoglalása
KomponensTechnológiaSzerepeFrontendAngularUser bejelentkezés, preferencia beállításBackend.NET WebAPI + SQLiteUser management, preferenciák tárolásaHír feldolgozón8n workflowRSS olvasás, AI elemzés, értesítés küldésAI motorGroq / Llama 3.1Kategorizálás, összefoglalás, kulcsszó kinyerésEvent adatbázisSupabase (PostgreSQL)Feldolgozott hírek tárolásaEmailGmail (OAuth2)Email értesítések küldéseChatSlack botSlack channel értesítések

Duplikáció védelem — kétszintű
1. szint — Feed szinten:
   Ugyanaz a cikk URL → nem dolgozza fel újra

2. szint — Értesítés szinten:
   processed_notifications tábla:
   ugyanaz az event + ugyanaz a user + ugyanaz a csatorna
   → csak egyszer megy értesítés

Adatstruktúra
Supabase events tábla:
┌──────────┬──────────────┬───────────────┬──────────────────┬───────────┐
│ id       │ title        │ category      │ keywords         │ processed │
├──────────┼──────────────┼───────────────┼──────────────────┼───────────┤
│ uuid-001 │ Iran/US deal │ breaking_news │ ["Iran","US"]    │ TRUE      │
│ uuid-002 │ Poison seller│ breaking_news │ ["suicides",.."] │ FALSE     │
└──────────┴──────────────┴───────────────┴──────────────────┴───────────┘

SQLite users tábla:
┌────┬──────────────────────┬───────────┐
│ id │ email                │ role      │
├────┼──────────────────────┼───────────┤
│ 1  │ admin@system.local   │ admin     │
│ 2  │ user@gmail.com       │ user      │
└────┴──────────────────────┴───────────┘

SQLite alert_preferences tábla:
┌────┬─────────┬───────────────┬─────────┐
│ id │ user_id │ category      │ keyword │
├────┼─────────┼───────────────┼─────────┤
│ 1  │ 2       │ breaking_news │ Iran    │
│ 2  │ 2       │ breaking_news │ US      │
└────┴─────────┴───────────────┴─────────┘