import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { PlatformStats } from '../../models';

@Component({
  selector: 'app-admin-platform',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-platform.component.html',
  styleUrl: './admin-platform.component.scss'
})
export class AdminPlatformComponent implements OnInit {
  stats = signal<PlatformStats | null>(null);
  loading = signal(true);

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getPlatformStats().subscribe({
      next: s => { this.stats.set(s); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}
