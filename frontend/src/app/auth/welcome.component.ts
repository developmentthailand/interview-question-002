import { Component, input, output } from '@angular/core';
import { CurrentUserResponse } from '../services/auth-api.service';

@Component({
  selector: 'app-welcome',
  styleUrl: './welcome.component.scss',
  templateUrl: './welcome.component.html',
})
export class WelcomeComponent {
  readonly user = input.required<CurrentUserResponse>();
  readonly signedOut = output<void>();
}
