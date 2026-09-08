import { Component, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { PlantContextService } from '../../core/services/plant-context.service';
import { ScadaSnapshot } from '../../models';

@Component({
  selector: 'app-scada',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './scada.component.html',
  styleUrl: './scada.component.scss'
})
export class ScadaComponent {
  snapshot = signal<ScadaSnapshot | null>(null);
  loading = signal(true);

  constructor(private api: ApiService, private plantContext: PlantContextService) {
    effect(() => {
      const siteId = this.plantContext.selectedSiteId();
      if (!siteId) return;
      this.load(siteId);
    });
  }

  load(siteId: number): void {
    this.loading.set(true);
    this.api.getScada(siteId).subscribe({
      next: data => { this.snapshot.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  commOk(status: string): boolean {
    return status === 'Connected';
  }
}
