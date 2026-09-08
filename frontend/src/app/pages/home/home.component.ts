import { Component, OnInit, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { PlantContextService } from '../../core/services/plant-context.service';
import { CompanyDashboard, DashboardSummary, EnergyLoss, Notification, OperationsEfficiency } from '../../models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  summary = signal<DashboardSummary | null>(null);
  companyDashboard = signal<CompanyDashboard | null>(null);
  energyLoss = signal<EnergyLoss | null>(null);
  opsEfficiency = signal<OperationsEfficiency | null>(null);
  notifications = signal<Notification[]>([]);
  showNotifications = signal(false);
  loading = signal(true);

  constructor(
    public auth: AuthService,
    private api: ApiService,
    private plantContext: PlantContextService
  ) {
    effect(() => {
      const siteId = this.plantContext.selectedSiteId();
      if (this.plantContext.accessibleSites().length === 0) return;
      this.loadDashboard(siteId ?? undefined);
      this.loadEnergyLoss(siteId ?? undefined);
    });
  }

  ngOnInit(): void {
    this.api.getNotifications().subscribe(data => this.notifications.set(data));
    if (this.auth.canManagePlants() && !this.auth.isSuperAdmin()) {
      this.api.getCompanyDashboard().subscribe(d => this.companyDashboard.set(d));
      this.api.getOperationsEfficiency().subscribe(d => this.opsEfficiency.set(d));
    }
  }

  private loadDashboard(siteId?: number): void {
    this.loading.set(true);
    this.api.getDashboard(siteId).subscribe({
      next: data => { this.summary.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  private loadEnergyLoss(siteId?: number): void {
    this.api.getEnergyLoss(siteId).subscribe(data => this.energyLoss.set(data));
  }

  unreadCount(): number {
    return this.notifications().filter(n => !n.isRead).length;
  }

  toggleNotifications(): void {
    this.showNotifications.update(v => !v);
  }

  markRead(n: Notification): void {
    if (n.isRead) return;
    this.api.markNotificationRead(n.id).subscribe(() => {
      this.notifications.update(list => list.map(x => x.id === n.id ? { ...x, isRead: true } : x));
    });
  }
}
