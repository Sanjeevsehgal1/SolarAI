import { Component, OnInit, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { PlantContextService } from '../../core/services/plant-context.service';
import { CleaningSchedule, ZoneSoiling } from '../../models';

@Component({
  selector: 'app-cleaning',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cleaning.component.html',
  styleUrl: './cleaning.component.scss'
})
export class CleaningComponent implements OnInit {
  schedules = signal<CleaningSchedule[]>([]);
  soiling = signal<ZoneSoiling[]>([]);
  loading = signal(true);

  constructor(private api: ApiService, private plantContext: PlantContextService) {
    effect(() => {
      const siteId = this.plantContext.selectedSiteId();
      if (this.plantContext.accessibleSites().length === 0) return;
      this.loadData(siteId ?? undefined);
    });
  }

  ngOnInit(): void {}

  loadData(siteId?: number): void {
    if (!siteId) {
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    this.api.getCleaningSchedules(siteId).subscribe(s => this.schedules.set(s));
    this.api.getSoiling(siteId).subscribe({
      next: z => { this.soiling.set(z); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  soilingColor(index: number): string {
    if (index > 40) return '#e53935';
    if (index > 25) return '#fb8c00';
    return '#43a047';
  }

  priorityLabel(p: number): string {
    return p === 1 ? 'High' : p === 2 ? 'Medium' : 'Low';
  }
}
