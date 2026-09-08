import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { Organization, SiteSummary } from '../../models';

@Component({
  selector: 'app-admin-plants',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-plants.component.html',
  styleUrl: './admin-plants.component.scss'
})
export class AdminPlantsComponent implements OnInit {
  plants = signal<SiteSummary[]>([]);
  orgs = signal<Organization[]>([]);
  loading = signal(true);
  showForm = signal(false);
  error = signal('');

  form = {
    organizationId: null as number | null,
    name: '',
    plantCode: '',
    location: '',
    capacityMw: 1,
    latitude: 26.2,
    longitude: 73.0,
    targetPr: 82
  };

  constructor(private api: ApiService, public auth: AuthService) {}

  ngOnInit(): void {
    if (this.auth.isSuperAdmin()) {
      this.api.getOrganizations().subscribe(o => this.orgs.set(o));
    }
    this.form.organizationId = this.auth.currentUser()?.organizationId ?? null;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getPlants().subscribe({
      next: p => { this.plants.set(p); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  submit(): void {
    this.error.set('');
    this.api.createPlant(this.form).subscribe({
      next: () => {
        this.showForm.set(false);
        this.form = {
          organizationId: this.auth.currentUser()?.organizationId ?? null,
          name: '', plantCode: '', location: '', capacityMw: 1,
          latitude: 26.2, longitude: 73.0, targetPr: 82
        };
        this.load();
      },
      error: () => this.error.set('Could not create plant. Check code and organization limits.')
    });
  }
}
