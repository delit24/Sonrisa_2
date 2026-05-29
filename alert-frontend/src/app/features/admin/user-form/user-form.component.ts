import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService } from '../../../core/services/user.service';

@Component({
  selector: 'app-user-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.scss'
})
export class UserFormComponent implements OnInit {
  userForm!: FormGroup;
  isEditMode = false;
  userId: number | null = null;
  isLoading = false;
  isSaving = false;
  emailError = '';

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!idParam;
    this.userId = idParam ? parseInt(idParam) : null;

    this.userForm = this.fb.group({
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', this.isEditMode ? [] : [Validators.required, Validators.minLength(6)]],
      role: ['user', Validators.required]
    });

    if (this.isEditMode && this.userId) {
      this.loadUser();
    }
  }

  loadUser(): void {
    this.isLoading = true;
    this.userService.getById(this.userId!).subscribe({
      next: (user) => {
        this.userForm.patchValue({
          fullName: user.fullName,
          email: user.email,
          role: user.role
        });
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.snackBar.open('Felhasználó nem található', 'Bezár', { duration: 3000 });
        this.router.navigate(['/admin/users']);
      }
    });
  }

  onSubmit(): void {
    if (this.userForm.invalid) return;

    this.isSaving = true;
    this.emailError = '';

    const formValue = this.userForm.value;

    const obs = this.isEditMode
      ? this.userService.update(this.userId!, {
          email: formValue.email,
          fullName: formValue.fullName,
          role: formValue.role,
          ...(formValue.password ? { password: formValue.password } : {})
        })
      : this.userService.create(formValue);

    obs.subscribe({
      next: () => {
        this.snackBar.open(
          `Felhasználó sikeresen ${this.isEditMode ? 'módosítva' : 'létrehozva'}`,
          'Bezár',
          { duration: 3000 }
        );
        this.router.navigate(['/admin/users']);
      },
      error: (err) => {
        this.isSaving = false;
        if (err.status === 409) {
          this.emailError = 'Ez az email cím már foglalt';
        } else {
          this.snackBar.open('Hiba történt a mentés során', 'Bezár', { duration: 3000 });
        }
      }
    });
  }
}
