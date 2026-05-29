import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { PreferenceService } from '../../../core/services/preference.service';
import { AlertPreference, Category, CATEGORY_LABELS } from '../../../core/models/preference.model';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-preferences',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatDialogModule
  ],
  templateUrl: './preferences.component.html',
  styleUrl: './preferences.component.scss'
})
export class PreferencesComponent implements OnInit {
  preferences: AlertPreference[] = [];
  addForm: FormGroup;
  isLoading = true;
  isAdding = false;
  duplicateError = '';

  categories: { value: Category; label: string }[] = [
    { value: 'breaking_news', label: 'Breaking News' },
    { value: 'market', label: 'Market' },
    { value: 'natural_disaster', label: 'Natural Disaster' },
    { value: 'tech', label: 'Tech' }
  ];

  categoryLabels = CATEGORY_LABELS;

  constructor(
    private fb: FormBuilder,
    private preferenceService: PreferenceService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {
    this.addForm = this.fb.group({
      category: ['', Validators.required],
      keyword: ['']
    });
  }

  ngOnInit(): void {
    this.loadPreferences();
  }

  loadPreferences(): void {
    this.isLoading = true;
    this.preferenceService.getAll().subscribe({
      next: (prefs) => {
        this.preferences = prefs;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.snackBar.open('Hiba a preferenciák betöltésekor', 'Bezár', { duration: 3000 });
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onAdd(): void {
    if (this.addForm.invalid) return;

    this.isAdding = true;
    this.duplicateError = '';

    const { category, keyword } = this.addForm.value;
    this.preferenceService.create({
      category,
      keyword: keyword?.trim() || undefined
    }).subscribe({
      next: () => {
        this.addForm.reset();
        this.loadPreferences();
        this.isAdding = false;
        this.snackBar.open('Preferencia hozzáadva', 'Bezár', { duration: 2000 });
      },
      error: (err) => {
        this.isAdding = false;
        if (err.status === 409) {
          this.duplicateError = 'Ez a kategória + kulcsszó kombináció már létezik';
        } else {
          this.snackBar.open('Hiba történt', 'Bezár', { duration: 3000 });
        }
      }
    });
  }

  onToggle(pref: AlertPreference): void {
    this.preferenceService.toggle(pref.id).subscribe({
      next: (updated) => {
        this.preferences = this.preferences.map(p => p.id === pref.id ? updated : p);
        this.cdr.detectChanges();
      },
      error: () => {
        this.snackBar.open('Hiba történt', 'Bezár', { duration: 3000 });
      }
    });
  }

  onDelete(pref: AlertPreference): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Preferencia törlése',
        message: `Biztosan törli a "${this.categoryLabels[pref.category]}" preferenciát${pref.keyword ? ` (${pref.keyword})` : ''}?`,
        confirmText: 'Törlés'
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.preferenceService.delete(pref.id).subscribe({
          next: () => {
            this.preferences = this.preferences.filter(p => p.id !== pref.id);
            this.snackBar.open('Preferencia törölve', 'Bezár', { duration: 2000 });
          },
          error: () => {
            this.snackBar.open('Hiba a törlés során', 'Bezár', { duration: 3000 });
          }
        });
      }
    });
  }
}
