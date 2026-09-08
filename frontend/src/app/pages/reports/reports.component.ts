import { Component, effect, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { PlantContextService } from '../../core/services/plant-context.service';
import { ReportSummary } from '../../models';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent {
  reports = signal<ReportSummary[]>([]);
  selected = signal<ReportSummary | null>(null);
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
    this.api.getReports(siteId).subscribe({
      next: data => { this.reports.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openReport(r: ReportSummary): void {
    this.selected.set(r);
  }

  closeReport(): void {
    this.selected.set(null);
  }
}
