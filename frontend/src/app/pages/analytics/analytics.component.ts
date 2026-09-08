import {
  Component, OnInit, OnDestroy, AfterViewInit,
  signal, ViewChild, ElementRef, effect, HostListener
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { ApiService } from '../../core/services/api.service';
import { PlantContextService } from '../../core/services/plant-context.service';
import { AnalyticsSummary } from '../../models';

Chart.register(...registerables);

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './analytics.component.html',
  styleUrl: './analytics.component.scss'
})
export class AnalyticsComponent implements OnInit, AfterViewInit, OnDestroy {
  data = signal<AnalyticsSummary | null>(null);
  loading = signal(true);
  periodDays = signal(30);

  @ViewChild('genChart') genChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('prChart') prChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('irrChart') irrChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('soilChart') soilChartRef!: ElementRef<HTMLCanvasElement>;

  private charts: Chart[] = [];
  private viewReady = false;

  constructor(private api: ApiService, private plantContext: PlantContextService) {
    effect(() => {
      const d = this.data();
      if (d && this.viewReady) {
        setTimeout(() => this.renderCharts(d), 0);
      }
    });
    effect(() => {
      const siteId = this.plantContext.selectedSiteId();
      if (this.plantContext.accessibleSites().length === 0) return;
      if (siteId) this.loadData(siteId);
    });
  }

  ngOnInit(): void {}

  ngAfterViewInit(): void {
    this.viewReady = true;
    const d = this.data();
    if (d) this.renderCharts(d);
  }

  ngOnDestroy(): void {
    this.destroyCharts();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.charts.forEach(c => c.resize());
  }

  setPeriod(days: number): void {
    if (this.periodDays() === days) return;
    this.periodDays.set(days);
    const siteId = this.plantContext.selectedSiteId();
    if (siteId) this.loadData(siteId);
  }

  soilingColor(index: number): string {
    if (index > 40) return 'var(--color-danger)';
    if (index > 25) return 'var(--color-warning)';
    return 'var(--color-success)';
  }

  severityClass(severity: string): string {
    return severity.toLowerCase();
  }

  private loadData(siteId: number): void {
    this.loading.set(true);
    this.api.getAnalytics(siteId, this.periodDays()).subscribe({
      next: a => { this.data.set(a); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  private destroyCharts(): void {
    this.charts.forEach(c => c.destroy());
    this.charts = [];
  }

  private renderCharts(d: AnalyticsSummary): void {
    this.destroyCharts();
    this.buildGenerationChart(d);
    this.buildPrChart(d);
    this.buildIrradianceChart(d);
    this.buildSoilingChart(d);
  }

  private chartDefaults() {
    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: { boxWidth: 10, font: { size: 11, family: 'Inter' }, color: '#64748b' }
        }
      },
      scales: {
        x: {
          ticks: { font: { size: 10 }, color: '#94a3b8', maxRotation: 0 },
          grid: { display: false }
        },
        y: {
          ticks: { font: { size: 10 }, color: '#94a3b8' },
          grid: { color: '#f1f5f9' }
        }
      }
    };
  }

  private labels(d: AnalyticsSummary): string[] {
    return d.dailyGeneration.map(g =>
      new Date(g.date).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' })
    );
  }

  private buildGenerationChart(d: AnalyticsSummary): void {
    const ctx = this.genChartRef?.nativeElement?.getContext('2d');
    if (!ctx) return;

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: this.labels(d),
        datasets: [
          {
            label: 'Actual (kWh)',
            data: d.dailyGeneration.map(g => g.actualKwh),
            borderColor: '#0284c7',
            backgroundColor: 'rgba(2, 132, 199, 0.08)',
            fill: true,
            tension: 0.35,
            pointRadius: 2,
            borderWidth: 2
          },
          {
            label: 'Target (kWh)',
            data: d.dailyGeneration.map(g => g.theoreticalKwh),
            borderColor: '#cbd5e1',
            borderDash: [4, 4],
            fill: false,
            tension: 0.35,
            pointRadius: 0,
            borderWidth: 1.5
          }
        ]
      },
      options: { ...this.chartDefaults() }
    };
    this.charts.push(new Chart(ctx, config));
  }

  private buildPrChart(d: AnalyticsSummary): void {
    const ctx = this.prChartRef?.nativeElement?.getContext('2d');
    if (!ctx) return;

    const config: ChartConfiguration = {
      type: 'bar',
      data: {
        labels: this.labels(d),
        datasets: [{
          label: 'PR (%)',
          data: d.dailyGeneration.map(g => g.performanceRatio),
          backgroundColor: d.dailyGeneration.map(g =>
            g.performanceRatio >= 82 ? 'rgba(5, 150, 105, 0.75)' :
            g.performanceRatio >= 75 ? 'rgba(2, 132, 199, 0.75)' :
            'rgba(217, 119, 6, 0.75)'
          ),
          borderRadius: 4,
          borderSkipped: false
        }]
      },
      options: {
        ...this.chartDefaults(),
        scales: {
          ...this.chartDefaults().scales,
          y: { ...this.chartDefaults().scales!.y, min: 60, max: 100 }
        }
      }
    };
    this.charts.push(new Chart(ctx, config));
  }

  private buildIrradianceChart(d: AnalyticsSummary): void {
    const ctx = this.irrChartRef?.nativeElement?.getContext('2d');
    if (!ctx) return;

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: this.labels(d),
        datasets: [
          {
            label: 'Irradiance (kWh/m²)',
            data: d.dailyGeneration.map(g => g.irradiance),
            borderColor: '#d97706',
            backgroundColor: 'rgba(217, 119, 6, 0.06)',
            fill: true,
            tension: 0.35,
            pointRadius: 2,
            borderWidth: 2,
            yAxisID: 'y'
          },
          {
            label: 'Temp (°C)',
            data: d.dailyGeneration.map(g => g.temperature),
            borderColor: '#dc2626',
            fill: false,
            tension: 0.35,
            pointRadius: 2,
            borderWidth: 1.5,
            yAxisID: 'y1'
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: { labels: { boxWidth: 10, font: { size: 11 }, color: '#64748b' } }
        },
        scales: {
          x: { ticks: { font: { size: 10 }, color: '#94a3b8', maxRotation: 0 }, grid: { display: false } },
          y: { position: 'left', ticks: { font: { size: 10 }, color: '#94a3b8' }, grid: { color: '#f1f5f9' } },
          y1: { position: 'right', ticks: { font: { size: 10 }, color: '#94a3b8' }, grid: { drawOnChartArea: false } }
        }
      }
    };
    this.charts.push(new Chart(ctx, config));
  }

  private buildSoilingChart(d: AnalyticsSummary): void {
    const ctx = this.soilChartRef?.nativeElement?.getContext('2d');
    if (!ctx || !d.zoneSoiling.length) return;

    const sorted = [...d.zoneSoiling].sort((a, b) => b.soilingIndex - a.soilingIndex);

    const config: ChartConfiguration = {
      type: 'bar',
      data: {
        labels: sorted.map(z => z.zoneName.replace('Zone ', '')),
        datasets: [{
          label: 'Soiling Index (%)',
          data: sorted.map(z => z.soilingIndex),
          backgroundColor: sorted.map(z =>
            z.soilingIndex > 40 ? 'rgba(220, 38, 38, 0.75)' :
            z.soilingIndex > 25 ? 'rgba(217, 119, 6, 0.75)' :
            'rgba(5, 150, 105, 0.75)'
          ),
          borderRadius: 4
        }]
      },
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          x: { ticks: { font: { size: 10 }, color: '#94a3b8' }, grid: { color: '#f1f5f9' }, max: 60 },
          y: { ticks: { font: { size: 10 }, color: '#64748b' }, grid: { display: false } }
        }
      }
    };
    this.charts.push(new Chart(ctx, config));
  }
}
