import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/services/auth.service';
import { PlantContextService } from '../core/services/plant-context.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.component.html',
  styleUrl: './layout.component.scss'
})
export class LayoutComponent {
  constructor(public auth: AuthService, public plantContext: PlantContextService) {}

  onPlantChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.plantContext.selectSite(value ? Number(value) : null);
  }
}
