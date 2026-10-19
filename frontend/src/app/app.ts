import { Component, inject, signal } from '@angular/core';
import { AuthSessionService } from './services/auth-session.service';
import { LoginComponent } from './auth/login.component';
import { RegisterComponent } from './auth/register.component';
import { WelcomeComponent } from './auth/welcome.component';

type AuthScreen = 'login' | 'register' | 'welcome';

@Component({
  imports: [LoginComponent, RegisterComponent, WelcomeComponent],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  private readonly authSession = inject(AuthSessionService);
  protected readonly screen = signal<AuthScreen>('login');
  protected readonly registrationMessage = signal<string | null>(null);
  protected readonly currentUser = this.authSession.currentUser;

  protected showRegistration(): void {
    this.registrationMessage.set(null);
    this.screen.set('register');
  }

  protected showLogin(): void {
    this.screen.set('login');
  }

  protected onRegistered(username: string): void {
    this.registrationMessage.set(`Account created for ${username}. Sign in to continue.`);
    this.screen.set('login');
  }

  protected onAuthenticated(): void {
    this.registrationMessage.set(null);
    this.screen.set('welcome');
  }

  protected onSignOut(): void {
    this.authSession.signOut();
    this.screen.set('login');
  }
}
