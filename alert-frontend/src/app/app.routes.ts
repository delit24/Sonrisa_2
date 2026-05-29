import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

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
