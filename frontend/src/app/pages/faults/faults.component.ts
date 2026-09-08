import { Component, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { PlantContextService } from '../../core/services/plant-context.service';
import { Fault } from '../../models';

@Component({
  selector: 'app-faults',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './faults.component.html',
  styleUrl: './faults.component.scss'
})
export class FaultsComponent {
  faults = signal<Fault[]>([]);
  filter = signal('all');
  selected = signal<Fault | null>(null);
  loading = signal(true);
  predicting = signal(false);
  actionMsg = signal('');

  constructor(
    private api: ApiService,
    public auth: AuthService,
    private plantContext: PlantContextService
  ) {
    effect(() => {
      const siteId = this.plantContext.selectedSiteId();
      if (this.plantContext.accessibleSites().length === 0) return;
      this.loadFaults(siteId ?? undefined);
    });
  }

  loadFaults(siteId?: number): void {
    this.loading.set(true);
    const status = this.filter() === 'all' ? undefined : this.filter();
    this.api.getFaults(siteId, status).subscribe({
      next: data => { this.faults.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  setFilter(f: string): void {
    this.filter.set(f);
    this.loadFaults(this.plantContext.selectedSiteId() ?? undefined);
  }

  runAiPrediction(): void {
    const siteId = this.plantContext.selectedSiteId();
    if (!siteId) return;
    this.predicting.set(true);
    this.actionMsg.set('');
    this.api.runPrediction(siteId).subscribe({
      next: created => {
        this.predicting.set(false);
        this.actionMsg.set(`AI scan complete — ${created.length} new prediction(s)`);
        this.loadFaults(siteId);
      },
      error: () => { this.predicting.set(false); this.actionMsg.set('AI prediction failed'); }
    });
  }

  openDetail(fault: Fault): void {
    this.selected.set(fault);
    this.actionMsg.set('');
  }

  closeDetail(): void {
    this.selected.set(null);
  }

  resolveFault(): void {
    const f = this.selected();
    if (!f) return;
    this.api.updateFault(f.id, { status: 'Resolved', fixNotes: 'Resolved via app' }).subscribe({
      next: updated => {
        this.faults.update(list => list.map(x => x.id === updated.id ? updated : x));
        this.selected.set(updated);
      }
    });
  }

  acknowledgeFault(): void {
    const f = this.selected();
    if (!f) return;
    this.api.acknowledgeFault(f.id).subscribe({
      next: updated => {
        this.faults.update(list => list.map(x => x.id === updated.id ? updated : x));
        this.selected.set(updated);
      }
    });
  }

  escalateFault(): void {
    const f = this.selected();
    if (!f) return;
    this.api.escalateFault(f.id).subscribe({
      next: updated => {
        this.faults.update(list => list.map(x => x.id === updated.id ? updated : x));
        this.selected.set(updated);
      }
    });
  }

  createMaintenance(): void {
    const f = this.selected();
    if (!f) return;
    this.api.createTaskFromFault(f.id, f.assignedToUserId).subscribe({
      next: () => {
        this.actionMsg.set('Work order created from alert');
        this.loadFaults(this.plantContext.selectedSiteId() ?? undefined);
      }
    });
  }

  severityClass(severity: string): string {
    return { Critical: 'sev-critical', Medium: 'sev-medium', Low: 'sev-low' }[severity] ?? '';
  }

  typeLabel(type: string): string {
    return {
      InverterError: 'INV', StringMismatch: 'STR', PanelDegradation: 'DEG',
      TemperatureAnomaly: 'TMP', SoilingLoss: 'SLG'
    }[type] ?? 'ALT';
  }

  typeClass(type: string): string {
    return {
      InverterError: 'type-inv', StringMismatch: 'type-str', PanelDegradation: 'type-deg',
      TemperatureAnomaly: 'type-tmp', SoilingLoss: 'type-slg'
    }[type] ?? 'type-alt';
  }
}
