# Backend — .NET WebAPI Terv

## Áttekintés

A .NET WebAPI felelősségi körei:
- JWT alapú autentikáció és autorizáció
- User management (admin és user szerepkör)
- Alert preferenciák CRUD
- Értesítési csatornák kezelése
- Belső endpoint az n8n számára (user preferenciák lekérése)
- SQLite adatbázis kezelése Entity Framework Core segítségével

---

## Tech stack

| Összetevő | Technológia |
|---|---|
| Framework | .NET 8 WebAPI |
| ORM | Entity Framework Core 8 |
| Adatbázis | SQLite |
| Auth | JWT Bearer token |
| Jelszó hash | BCrypt.Net |
| Dokumentáció | Swagger / OpenAPI |

---

## Projekt struktúra

```
AlertSystem.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── PreferencesController.cs
│   ├── ChannelsController.cs
│   └── InternalController.cs
├── Models/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── AlertPreference.cs
│   │   └── NotificationChannel.cs
│   ├── DTOs/
│   │   ├── Auth/
│   │   │   ├── LoginRequest.cs
│   │   │   └── LoginResponse.cs
│   │   ├── Users/
│   │   │   ├── UserDto.cs
│   │   │   ├── CreateUserRequest.cs
│   │   │   └── UpdateUserRequest.cs
│   │   ├── Preferences/
│   │   │   ├── PreferenceDto.cs
│   │   │   ├── CreatePreferenceRequest.cs
│   │   └── Channels/
│   │       ├── ChannelDto.cs
│   │       └── UpsertChannelRequest.cs
│   └── Internal/
│       └── UserPreferencesResponse.cs
├── Services/
│   ├── AuthService.cs
│   ├── UserService.cs
│   ├── PreferenceService.cs
│   └── ChannelService.cs
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── Middleware/
│   └── ApiKeyMiddleware.cs
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

---

## Entitások (Entity Framework)

### User.cs
```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "user"; // "admin" | "user"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AlertPreference> AlertPreferences { get; set; } = [];
    public ICollection<NotificationChannel> NotificationChannels { get; set; } = [];
}
```

### AlertPreference.cs
```csharp
public class AlertPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    // "breaking_news" | "market" | "natural_disaster" | "tech"
    public string? Keyword { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
```

### NotificationChannel.cs
```csharp
public class NotificationChannel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Channel { get; set; } = string.Empty;
    // "email" | "slack"
    public string Destination { get; set; } = string.Empty;
    // email cím VAGY Slack channel név (#alerts, @username)
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
```

---

## API Endpointok

### Auth

| Method | Endpoint | Auth | Leírás |
|---|---|---|---|
| POST | `/api/auth/login` | Nyilvános | Bejelentkezés, JWT token visszaadása |
| POST | `/api/auth/refresh` | Nyilvános | Token megújítás (opcionális MVP után) |

#### POST `/api/auth/login`
```json
// Request
{
  "email": "user@example.com",
  "password": "plaintext"
}

// Response 200
{
  "token": "eyJ...",
  "expiresAt": "2024-01-01T12:00:00Z",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "fullName": "Kovács János",
    "role": "user"
  }
}

// Response 401
{
  "error": "Invalid credentials"
}
```

---

### Users (Admin only)

| Method | Endpoint | Auth | Leírás |
|---|---|---|---|
| GET | `/api/users` | Admin | Összes user listázása |
| GET | `/api/users/{id}` | Admin | Egy user adatai |
| POST | `/api/users` | Admin | Új user létrehozása |
| PUT | `/api/users/{id}` | Admin | User adatainak módosítása |
| PATCH | `/api/users/{id}/deactivate` | Admin | User deaktiválása (soft delete) |
| PATCH | `/api/users/{id}/activate` | Admin | User aktiválása |

#### GET `/api/users`
```json
// Response 200
[
  {
    "id": 1,
    "email": "user@example.com",
    "fullName": "Kovács János",
    "role": "user",
    "isActive": true,
    "createdAt": "2024-01-01T10:00:00Z",
    "channelCount": 2,
    "preferenceCount": 3
  }
]
```

#### POST `/api/users`
```json
// Request
{
  "email": "newuser@example.com",
  "password": "SecurePass123!",
  "fullName": "Nagy Anna",
  "role": "user"
}

// Response 201
{
  "id": 2,
  "email": "newuser@example.com",
  "fullName": "Nagy Anna",
  "role": "user",
  "isActive": true,
  "createdAt": "2024-01-01T10:00:00Z"
}
```

---

### Preferences (User + Admin)

| Method | Endpoint | Auth | Leírás |
|---|---|---|---|
| GET | `/api/preferences` | User | Saját preferenciák listázása |
| POST | `/api/preferences` | User | Új preferencia hozzáadása |
| DELETE | `/api/preferences/{id}` | User | Preferencia törlése |
| PATCH | `/api/preferences/{id}/toggle` | User | Aktiválás / deaktiválás |

> **Admin jogosultság:** Admin lekérdezheti bármely user preferenciáit: `GET /api/users/{id}/preferences`

#### GET `/api/preferences`
```json
// Response 200
[
  {
    "id": 1,
    "category": "breaking_news",
    "keyword": null,
    "isActive": true,
    "createdAt": "2024-01-01T10:00:00Z"
  },
  {
    "id": 2,
    "category": "market",
    "keyword": "bitcoin",
    "isActive": true,
    "createdAt": "2024-01-01T10:00:00Z"
  }
]
```

#### POST `/api/preferences`
```json
// Request
{
  "category": "natural_disaster",
  "keyword": "Hungary"
}

// Response 201
{
  "id": 3,
  "category": "natural_disaster",
  "keyword": "Hungary",
  "isActive": true,
  "createdAt": "2024-01-01T10:00:00Z"
}

// Response 409 — duplikáció
{
  "error": "Preference with this category and keyword already exists"
}
```

---

### Channels (User + Admin)

| Method | Endpoint | Auth | Leírás |
|---|---|---|---|
| GET | `/api/channels` | User | Saját csatornák listázása |
| PUT | `/api/channels/email` | User | Email csatorna beállítása / frissítése |
| PUT | `/api/channels/slack` | User | Slack csatorna beállítása / frissítése |
| PATCH | `/api/channels/{id}/toggle` | User | Csatorna aktiválás / deaktiválás |

#### PUT `/api/channels/email`
```json
// Request
{
  "destination": "user@example.com"
}

// Response 200
{
  "id": 1,
  "channel": "email",
  "destination": "user@example.com",
  "isActive": true
}
```

#### PUT `/api/channels/slack`
```json
// Request
{
  "destination": "#alerts"
}

// Response 200
{
  "id": 2,
  "channel": "slack",
  "destination": "#alerts",
  "isActive": true
}
```

> **Fontos megjegyzés a Slack channel névről:** A `destination` értéknek `#channel-name` vagy `@username` formátumban kell lennie. A bot tokennek tagnak kell lennie a megadott channelben. Ezt a frontend validálja és jelzi a usernek.

---

### Internal (n8n számára)

| Method | Endpoint | Auth | Leírás |
|---|---|---|---|
| GET | `/api/internal/user-preferences` | API Key | Összes aktív user preferencia és csatorna |

> **Auth:** Nem JWT, hanem API key — `X-Internal-Api-Key` header. Az API key az `appsettings.json`-ban és environment variable-ben tárolva.

#### GET `/api/internal/user-preferences`
```json
// Response 200
[
  {
    "userId": 1,
    "preferences": [
      { "category": "breaking_news", "keyword": null },
      { "category": "market", "keyword": "bitcoin" }
    ],
    "channels": [
      { "type": "email", "destination": "user@example.com", "isActive": true },
      { "type": "slack", "destination": "#alerts", "isActive": true }
    ]
  },
  {
    "userId": 2,
    "preferences": [
      { "category": "natural_disaster", "keyword": "Hungary" }
    ],
    "channels": [
      { "type": "email", "destination": "anna@example.com", "isActive": true }
    ]
  }
]
```

---

## Autentikáció és autorizáció

### JWT konfiguráció

```json
// appsettings.json
{
  "Jwt": {
    "Secret": "env:JWT_SECRET",
    "Issuer": "AlertSystem",
    "Audience": "AlertSystemClient",
    "ExpiryMinutes": 480
  },
  "InternalApi": {
    "Key": "env:WEBAPI_INTERNAL_KEY"
  }
}
```

### Szerepkörök

| Endpoint csoport | User | Admin |
|---|---|---|
| Auth | ✓ | ✓ |
| Saját preferenciák | ✓ | ✓ |
| Saját csatornák | ✓ | ✓ |
| User management | ✗ | ✓ |
| Bármely user preferenciái | ✗ | ✓ |
| Internal API | ✗ | ✗ (csak API key) |

### ApiKeyMiddleware

Az `/api/internal/*` útvonalakra egy middleware ellenőrzi az `X-Internal-Api-Key` headert. Ha hiányzik vagy hibás → `401 Unauthorized`. Ez JWT-től teljesen független.

---

## Konfiguráció — environment variable-ök

| Változó | Leírás |
|---|---|
| `JWT_SECRET` | JWT aláírási kulcs (min. 32 karakter) |
| `WEBAPI_INTERNAL_KEY` | n8n → WebAPI belső API key |
| `SQLITE_PATH` | SQLite fájl elérési útja (default: `./data/userstore.db`) |
| `ALLOWED_ORIGINS` | CORS — Angular frontend URL-je |

---

## Hibakezelés — általános konvenciók

Minden hibaválasz egységes formátumú:

```json
{
  "error": "Human readable error message",
  "code": "ERROR_CODE",
  "details": {}
}
```

| HTTP státusz | Mikor |
|---|---|
| 200 | Sikeres lekérdezés |
| 201 | Sikeres létrehozás |
| 400 | Validációs hiba |
| 401 | Hiányzó vagy érvénytelen token / API key |
| 403 | Jogosultság hiánya |
| 404 | Erőforrás nem található |
| 409 | Duplikáció (pl. már létező email, preferencia) |
| 500 | Szerver oldali hiba |

---

## Seed adatok indításkor

Az alkalmazás első indításakor (ha a DB üres) automatikusan létrehozza az admin usert:

```csharp
// Program.cs — DbInitializer
if (!context.Users.Any())
{
    context.Users.Add(new User
    {
        Email = adminEmail,       // env: ADMIN_EMAIL
        PasswordHash = BCrypt.HashPassword(adminPassword), // env: ADMIN_PASSWORD
        FullName = "System Admin",
        Role = "admin",
        IsActive = true
    });
    await context.SaveChangesAsync();
}
```

> **Fontos:** Az `ADMIN_EMAIL` és `ADMIN_PASSWORD` environment variable-ökből jön, nem hardcode-olva. Első bejelentkezés után az admin megváltoztathatja a jelszavát.

---

## CORS konfiguráció

Az Angular frontend más porton fut fejlesztésben (`localhost:4200`), ezért CORS szükséges:

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins) // env: ALLOWED_ORIGINS
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```
