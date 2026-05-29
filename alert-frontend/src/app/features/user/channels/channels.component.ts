import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ChannelService } from '../../../core/services/channel.service';
import { NotificationChannel } from '../../../core/models/channel.model';

@Component({
  selector: 'app-channels',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './channels.component.html',
  styleUrl: './channels.component.scss'
})
export class ChannelsComponent implements OnInit {
  channels: NotificationChannel[] = [];
  emailForm: FormGroup;
  slackForm: FormGroup;
  isLoading = true;
  isSavingEmail = false;
  isSavingSlack = false;

  constructor(
    private fb: FormBuilder,
    private channelService: ChannelService,
    private snackBar: MatSnackBar
  ) {
    this.emailForm = this.fb.group({
      destination: ['', [Validators.required, Validators.email]]
    });

    this.slackForm = this.fb.group({
      destination: ['', [Validators.required, Validators.pattern(/^[#@].+/)]]
    });
  }

  ngOnInit(): void {
    this.loadChannels();
  }

  get emailChannel(): NotificationChannel | undefined {
    return this.channels.find(c => c.channel === 'email');
  }

  get slackChannel(): NotificationChannel | undefined {
    return this.channels.find(c => c.channel === 'slack');
  }

  loadChannels(): void {
    this.isLoading = true;
    this.channelService.getAll().subscribe({
      next: (channels) => {
        this.channels = channels;
        const email = this.emailChannel;
        const slack = this.slackChannel;
        if (email) {
          this.emailForm.patchValue({ destination: email.destination });
        }
        if (slack) {
          this.slackForm.patchValue({ destination: slack.destination });
        }
        this.isLoading = false;
      },
      error: () => {
        this.snackBar.open('Hiba a csatornák betöltésekor', 'Bezár', { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  saveEmail(): void {
    if (this.emailForm.invalid) return;
    this.isSavingEmail = true;

    this.channelService.upsertEmail(this.emailForm.value.destination).subscribe({
      next: (channel) => {
        this.updateChannel(channel);
        this.isSavingEmail = false;
        this.snackBar.open('Email csatorna mentve', 'Bezár', { duration: 2000 });
      },
      error: () => {
        this.isSavingEmail = false;
        this.snackBar.open('Hiba a mentés során', 'Bezár', { duration: 3000 });
      }
    });
  }

  saveSlack(): void {
    if (this.slackForm.invalid) return;
    this.isSavingSlack = true;

    this.channelService.upsertSlack(this.slackForm.value.destination).subscribe({
      next: (channel) => {
        this.updateChannel(channel);
        this.isSavingSlack = false;
        this.snackBar.open('Slack csatorna mentve', 'Bezár', { duration: 2000 });
      },
      error: () => {
        this.isSavingSlack = false;
        this.snackBar.open('Hiba a mentés során', 'Bezár', { duration: 3000 });
      }
    });
  }

  toggleChannel(channel: NotificationChannel): void {
    this.channelService.toggle(channel.id).subscribe({
      next: (updated) => {
        this.updateChannel(updated);
      },
      error: () => {
        this.snackBar.open('Hiba történt', 'Bezár', { duration: 3000 });
      }
    });
  }

  private updateChannel(channel: NotificationChannel): void {
    const idx = this.channels.findIndex(c => c.id === channel.id);
    if (idx !== -1) {
      this.channels[idx] = channel;
    } else {
      this.channels.push(channel);
    }
  }
}
