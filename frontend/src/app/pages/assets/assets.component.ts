import { Component, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { PlantContextService } from '../../core/services/plant-context.service';
import { Asset } from '../../models';

@Component({
  selector: 'app-assets',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './assets.component.html',
  styleUrl: './assets.component.scss'
})
export class AssetsComponent {
  assets = signal<Asset[]>([]);
  filter = signal('all');
  loading = signal(true);

  constructor(private api: ApiService, private plantContext: PlantContextService) {
    effect(() => {
      const siteId = this.plantContext.selectedSiteId();
      if (this.plantContext.accessibleSites().length === 0) return;
      this.load(siteId ?? undefined);
    });
  }

  load(siteId?: number): void {
    this.loading.set(true);
    this.api.getAssets(siteId).subscribe({
      next: data => { this.assets.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  filtered(): Asset[] {
    const f = this.filter();
    if (f === 'all') return this.assets();
    return this.assets().filter(a => a.type === f);
  }

  healthClass(score: number): string {
    if (score >= 80) return 'health-good';
    if (score >= 65) return 'health-warn';
    return 'health-bad';
  }

  statusClass(status: string): string {
    return 'status-' + status.toLowerCase();
  }
}
