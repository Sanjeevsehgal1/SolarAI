import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { UserProfile } from '../../models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  profile = signal<UserProfile | null>(null);

  constructor(public auth: AuthService, private api: ApiService) {}

  ngOnInit(): void {
    this.api.getProfile().subscribe(p => this.profile.set(p));
  }

  logout(): void {
    this.auth.logout();
  }
}
