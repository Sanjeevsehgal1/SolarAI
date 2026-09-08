import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { AdminUser, Organization, SiteSummary } from '../../models';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss'
})
export class AdminUsersComponent implements OnInit {
  users = signal<AdminUser[]>([]);
  orgs = signal<Organization[]>([]);
  plants = signal<SiteSummary[]>([]);
  loading = signal(true);
  showForm = signal(false);
  error = signal('');

  form = {
    fullName: '',
    email: '',
    phone: '',
    password: '',
    role: 'Technician',
    organizationId: null as number | null,
    siteIds: [] as number[]
  };

  roles = [
    'CompanyAdmin', 'CompanyTechnicalAdmin', 'PlantAdmin',
    'PlantTechnicalLead', 'Supervisor', 'Engineer', 'Technician', 'Viewer'
  ];

  constructor(private api: ApiService, public auth: AuthService) {}

  ngOnInit(): void {
    if (this.auth.isSuperAdmin()) {
      this.api.getOrganizations().subscribe(o => this.orgs.set(o));
    }
    this.api.getPlants().subscribe(p => this.plants.set(p));
    this.form.organizationId = this.auth.currentUser()?.organizationId ?? null;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAdminUsers().subscribe({
      next: u => { this.users.set(u); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleSite(siteId: number): void {
    const ids = this.form.siteIds;
    this.form.siteIds = ids.includes(siteId) ? ids.filter(id => id !== siteId) : [...ids, siteId];
  }

  submit(): void {
    this.error.set('');
    this.api.createUser(this.form).subscribe({
      next: () => {
        this.showForm.set(false);
        this.form = {
          fullName: '', email: '', phone: '', password: '',
          role: 'Technician', organizationId: this.auth.currentUser()?.organizationId ?? null,
          siteIds: []
        };
        this.load();
      },
      error: () => this.error.set('Could not create user. Email may already exist.')
    });
  }
}
