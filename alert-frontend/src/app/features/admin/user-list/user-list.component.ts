import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UserService } from '../../../core/services/user.service';
import { User } from '../../../core/models/user.model';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-user-list',
  imports: [
    CommonModule,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatButtonToggleModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss'
})
export class UserListComponent implements OnInit {
  users: User[] = [];
  filteredUsers: User[] = [];
  displayedColumns = ['fullName', 'email', 'role', 'isActive', 'createdAt', 'actions'];
  isLoading = true;
  filter: 'all' | 'active' | 'inactive' = 'all';

  constructor(
    private userService: UserService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.userService.getAll().subscribe({
      next: (users) => {
        this.users = users;
        this.applyFilter();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.snackBar.open('Hiba a felhasználók betöltésekor', 'Bezár', { duration: 3000 });
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilter(): void {
    switch (this.filter) {
      case 'active':
        this.filteredUsers = this.users.filter(u => u.isActive);
        break;
      case 'inactive':
        this.filteredUsers = this.users.filter(u => !u.isActive);
        break;
      default:
        this.filteredUsers = [...this.users];
    }
  }

  onFilterChange(value: string): void {
    this.filter = value as 'all' | 'active' | 'inactive';
    this.applyFilter();
  }

  toggleUserStatus(user: User): void {
    const action = user.isActive ? 'deaktiválni' : 'aktiválni';
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Megerősítés',
        message: `Biztosan szeretné ${action} a következő felhasználót: ${user.fullName}?`,
        confirmText: user.isActive ? 'Deaktiválás' : 'Aktiválás'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        const obs = user.isActive
          ? this.userService.deactivate(user.id)
          : this.userService.activate(user.id);

        obs.subscribe({
          next: () => {
            this.snackBar.open(
              `Felhasználó sikeresen ${user.isActive ? 'deaktiválva' : 'aktiválva'}`,
              'Bezár',
              { duration: 3000 }
            );
            this.loadUsers();
          },
          error: () => {
            this.snackBar.open('Hiba történt', 'Bezár', { duration: 3000 });
          }
        });
      }
    });
  }
}
