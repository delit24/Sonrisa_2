# Frontend — Angular Terv

## Áttekintés

Egy Angular alkalmazás, két szerepkörrel, route guard alapú hozzáférés-védelemmel. Login után a szerepkör alapján automatikus redirect történik.

---

## Tech stack

| Összetevő | Technológia |
|---|---|
| Framework | Angular 17+ (standalone components) |
| UI komponensek | Angular Material |
| HTTP | Angular HttpClient |
| Auth state | BehaviorSubject (service szinten, nem NgRx) |
| Routing | Angular Router + Route Guards |
| Form kezelés | Reactive Forms |

> **Miért nem NgRx?** MVP-hez túlkomplikált. Egy AuthService-ben tárolt BehaviorSubject elegendő, kevesebb boilerplate, kevesebb hibalehetőség.

---

## Projekt struktúra

```
src/
├── app/
│   ├── core/
│   │   ├── services/
│   │   │   ├── auth.service.ts
│   │   │   ├── user.service.ts
│   │   │   ├── preference.service.ts
│   │   │   └── channel.service.ts
│   │   ├── guards/
│   │   │   ├── auth.guard.ts
│   │   │   └── admin.guard.ts
│   │   ├── interceptors/
│   │   │   └── auth.interceptor.ts
│   │   └── models/
│   │       ├── user.model.ts
│   │       ├── preference.model.ts
│   │       └── channel.model.ts
│   ├── features/
│   │   ├── auth/
│   │   │   └── login/
│   │   │       ├── login.component.ts
│   │   │       └── login.component.html
│   │   ├── admin/
│   │   │   ├── admin-layout/
│   │   │   │   ├── admin-layout.component.ts
│   │   │   │   └── admin-layout.component.html
│   │   │   ├── user-list/
│   │   │   │   ├── user-list.component.ts
│   │   │   │   └── user-list.component.html
│   │   │   └── user-form/
│   │   │       ├── user-form.component.ts
│   │   │       └── user-form.component.html
│   │   └── user/
│   │       ├── user-layout/
│   │       │   ├── user-layout.component.ts
│   │       │   └── user-layout.component.html
│   │       ├── dashboard/
│   │       │   ├── dashboard.component.ts
│   │       │   └── dashboard.component.html
│   │       ├── preferences/
│   │       │   ├── preferences.component.ts
│   │       │   └── preferences.component.html
│   │       └── channels/
│   │           ├── channels.component.ts
│   │           └── channels.component.html
│   ├── shared/
│   │   └── components/
│   │       └── confirm-dialog/
│   │           └── confirm-dialog.component.ts
│   ├── app.routes.ts
│   └── app.config.ts
└── environments/
    ├── environment.ts
    └── environment.prod.ts
```

---

## Routing

```typescript
// app.routes.ts
export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component')
        .then(m => m.LoginComponent)
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-layout/admin-layout.component')
        .then(m => m.AdminLayoutComponent),
    children: [
      { path: '', redirectTo: 'users', pathMatch: 'full' },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/admin/user-list/user-list.component')
            .then(m => m.UserListComponent)
      },
      {
        path: 'users/new',
        loadComponent: () =>
          import('./features/admin/user-form/user-form.component')
            .then(m => m.UserFormComponent)
      },
      {
        path: 'users/:id/edit',
        loadComponent: () =>
          import('./features/admin/user-form/user-form.component')
            .then(m => m.UserFormComponent)
      }
    ]
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/user/user-layout/user-layout.component')
        .then(m => m.UserLayoutComponent),
    children: [
      { path: '', redirectTo: 'alerts', pathMatch: 'full' },
      {
        path: 'alerts',
        loadComponent: () =>
          import('./features/user/preferences/preferences.component')
            .then(m => m.PreferencesComponent)
      },
      {
        path: 'channels',
        loadComponent: () =>
          import('./features/user/channels/channels.component')
            .then(m => m.ChannelsComponent)
      }
    ]
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];
```

---

## Route Guardok

### auth.guard.ts
```typescript
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoggedIn()) {
    return true;
  }

  router.navigate(['/login']);
  return false;
};
```

### admin.guard.ts
```typescript
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.currentUser()?.role === 'admin') {
    return true;
  }

  // User megpróbálja elérni az admin oldalt → visszairányítás
  router.navigate(['/dashboard']);
  return false;
};
```

---

## AuthService

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private currentUserSubject = new BehaviorSubject<User | null>(null);

  currentUser = this.currentUserSubject.asReadonly();

  constructor(private http: HttpClient, private router: Router) {
    // Oldal újratöltéskor token visszatöltése
    const token = localStorage.getItem(this.TOKEN_KEY);
    if (token) {
      const decoded = this.decodeToken(token);
      if (decoded && !this.isTokenExpired(decoded)) {
        this.currentUserSubject.next(decoded.user);
      } else {
        this.logout();
      }
    }
  }

  login(email: string, password: string): Observable<void> {
    return this.http.post<LoginResponse>('/api/auth/login', { email, password })
      .pipe(
        tap(response => {
          localStorage.setItem(this.TOKEN_KEY, response.token);
          this.currentUserSubject.next(response.user);
        }),
        map(response => {
          // Role alapú redirect
          if (response.user.role === 'admin') {
            this.router.navigate(['/admin']);
          } else {
            this.router.navigate(['/dashboard']);
          }
        })
      );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  isLoggedIn(): boolean {
    return this.currentUserSubject.value !== null;
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private decodeToken(token: string): any {
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch {
      return null;
    }
  }

  private isTokenExpired(decoded: any): boolean {
    return decoded.exp * 1000 < Date.now();
  }
}
```

---

## HTTP Interceptor

Minden kimenő kéréshez automatikusan hozzáadja a JWT tokent:

```typescript
// auth.interceptor.ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  if (token) {
    const authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
    return next(authReq);
  }

  return next(req);
};
```

---

## Képernyők és komponensek

### Login oldal (`/login`)

**Funkció:** Email + jelszó megadása, role alapú redirect

**Elemek:**
- Email input (required, email validáció)
- Jelszó input (required, min 6 karakter)
- Bejelentkezés gomb
- Hibaüzenet ha sikertelen a login

---

### Admin Layout (`/admin`)

**Funkció:** Admin navigáció wrapper

**Elemek:**
- Oldalsáv navigáció: Users
- Fejléc: alkalmazás neve, kijelentkezés gomb, bejelentkezett admin neve

---

### User List (`/admin/users`)

**Funkció:** Összes user listázása, kezelése

**Elemek:**
- Táblázat oszlopok: Név, Email, Szerepkör, Státusz (Aktív/Inaktív), Létrehozva, Műveletek
- Műveletek: Szerkesztés, Deaktiválás / Aktiválás
- "Új felhasználó" gomb → `/admin/users/new`
- Szűrő: aktív / inaktív / összes

**Deaktiválás folyamata:**
1. Admin rákattint a Deaktiválás gombra
2. Confirm dialog jelenik meg: "Biztosan deaktiválod ezt a felhasználót?"
3. Megerősítés után PATCH kérés → user `isActive = false`
4. Táblázat frissül

---

### User Form (`/admin/users/new` és `/admin/users/:id/edit`)

**Funkció:** Új user létrehozása, meglévő szerkesztése

**Elemek:**
- Teljes név (required)
- Email cím (required, email validáció, egyediség validáció 409 hiba esetén)
- Jelszó (required létrehozáskor, opcionális szerkesztéskor)
- Szerepkör választó: User / Admin
- Mentés gomb, Mégsem gomb

---

### User Layout (`/dashboard`)

**Funkció:** User navigáció wrapper

**Elemek:**
- Felső navigáció: Alert Beállítások, Értesítési Csatornák
- Fejléc: alkalmazás neve, kijelentkezés gomb, bejelentkezett user neve

---

### Preferences — Alert Beállítások (`/dashboard/alerts`)

**Funkció:** Alert preferenciák kezelése

**Elemek:**

**Meglévő preferenciák listája:**
- Kártyák vagy sorok: Kategória badge + kulcsszó (ha van) + Aktív/Inaktív jelző + Törlés gomb

**Új preferencia hozzáadása form:**
- Kategória választó (dropdown):
  - Breaking News
  - Market
  - Natural Disaster
  - Tech
- Kulcsszó input (opcionális, placeholder: "pl. Hungary, bitcoin...")
- Hozzáadás gomb

**Validáció:**
- Kategória kötelező
- Ha ugyanolyan kategória + kulcsszó kombináció már létezik → inline hibaüzenet (409 hiba kezelése)

**Slack channel figyelmeztetés:**
Ha a user Slack csatornát állít be, egy info banner jelenik meg:
> "⚠️ A megadott Slack channel-nek (#channel-name) tartalmaznia kell a rendszer botját. Ha nem kap értesítést, ellenőrizze, hogy a bot tagja-e a channelnek."

---

### Channels — Értesítési Csatornák (`/dashboard/channels`)

**Funkció:** Email és Slack értesítési csatornák beállítása

**Elemek:**

**Email kártya:**
- Jelenlegi email cím megjelenítése (ha be van állítva)
- Input: email cím
- Mentés gomb
- Aktív / Inaktív toggle

**Slack kártya:**
- Jelenlegi channel megjelenítése (ha be van állítva)
- Input: channel név (placeholder: `#alerts` vagy `@username`)
- Validáció: `#` vagy `@` karakterrel kell kezdődnie
- Mentés gomb
- Aktív / Inaktív toggle
- Info szöveg: "A botnak tagnak kell lennie a megadott channelben."

---

## Modellek

```typescript
// user.model.ts
export interface User {
  id: number;
  email: string;
  fullName: string;
  role: 'admin' | 'user';
  isActive: boolean;
  createdAt: string;
  channelCount?: number;
  preferenceCount?: number;
}

// preference.model.ts
export type Category =
  'breaking_news' | 'market' | 'natural_disaster' | 'tech';

export interface AlertPreference {
  id: number;
  category: Category;
  keyword: string | null;
  isActive: boolean;
  createdAt: string;
}

// channel.model.ts
export type ChannelType = 'email' | 'slack';

export interface NotificationChannel {
  id: number;
  channel: ChannelType;
  destination: string;
  isActive: boolean;
}
```

---

## Environment konfiguráció

```typescript
// environment.ts
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000'
};

// environment.prod.ts
export const environment = {
  production: true,
  apiBaseUrl: 'https://api.yourdomain.com'
};
```

---

## Hibakezelés — általános konvenciók

| HTTP státusz | Mit csinál az Angular |
|---|---|
| 401 | Automatikus kijelentkezés, redirect `/login`-ra |
| 403 | Snackbar: "Nincs jogosultsága ehhez a művelethez" |
| 409 | Inline hibaüzenet a formnál |
| 404 | Snackbar: "Az erőforrás nem található" |
| 500 | Snackbar: "Szerver hiba, próbálja újra később" |

A 401-es kezelés az interceptorban történik globálisan, a többi a service-ekben.
