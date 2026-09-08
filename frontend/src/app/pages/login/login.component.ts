import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  email = 'admin@solarshield.ai';
  password = 'Admin@123';
  error = signal('');
  loading = signal(false);
  lang = signal('en');

  constructor(private auth: AuthService, private router: Router) {}

  submit(): void {
    this.loading.set(true);
    this.error.set('');
    this.auth.login(this.email, this.password).subscribe({
      next: () => this.router.navigate([this.auth.postLoginRoute()]),
      error: () => {
        this.error.set('Invalid email or password');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false)
    });
  }

  toggleLang(): void {
    this.lang.update(l => l === 'en' ? 'hi' : 'en');
  }
}
