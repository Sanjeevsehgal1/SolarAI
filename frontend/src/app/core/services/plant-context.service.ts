import { Injectable, signal, computed } from '@angular/core';
import { SiteSummary } from '../../models';

@Injectable({ providedIn: 'root' })
export class PlantContextService {
  private readonly storageKey = 'solarshield_plant';

  accessibleSites = signal<SiteSummary[]>([]);
  selectedSiteId = signal<number | null>(this.loadSelected());

  selectedSite = computed(() => {
    const id = this.selectedSiteId();
    return this.accessibleSites().find(s => s.id === id) ?? null;
  });

  setAccessibleSites(sites: SiteSummary[]): void {
    this.accessibleSites.set(sites);
    const current = this.selectedSiteId();
    if (!current || !sites.some(s => s.id === current)) {
      const first = sites[0]?.id ?? null;
      this.selectSite(first);
    }
  }

  selectSite(siteId: number | null): void {
    this.selectedSiteId.set(siteId);
    if (siteId) localStorage.setItem(this.storageKey, String(siteId));
    else localStorage.removeItem(this.storageKey);
  }

  private loadSelected(): number | null {
    const v = localStorage.getItem(this.storageKey);
    return v ? Number(v) : null;
  }
}
