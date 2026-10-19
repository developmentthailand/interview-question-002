import { Component, output, signal, input, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { CurrentUserResponse } from '../services/auth-api.service';
import { AuthSessionService } from '../services/auth-session.service';

@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-login',
  styleUrl: './login.component.scss',
  templateUrl: './login.component.html',
})
export class LoginComponent {
  readonly registrationMessage = input<string | null>(null);
  readonly registerRequested = output<void>();
  readonly authenticated = output<CurrentUserResponse>();

  private readonly formBuilder = inject(FormBuilder);
  private readonly session = inject(AuthSessionService);
  protected readonly pending = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly form = this.formBuilder.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  protected submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid || this.pending()) {
      this.form.markAllAsTouched();
      return;
    }

    this.pending.set(true);
    const { username, password } = this.form.getRawValue();
    this.session
      .signIn(username, password)
      .pipe(finalize(() => this.pending.set(false)))
      .subscribe({
        next: (user) => {
          this.form.reset();
          this.authenticated.emit(user);
        },
        error: () => this.errorMessage.set('Username or password is incorrect. Please try again.'),
      });
  }
}
