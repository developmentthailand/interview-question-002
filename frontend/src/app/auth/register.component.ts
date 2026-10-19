import { Component, inject, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthApiService } from '../services/auth-api.service';
import { matchingPasswordsValidator, passwordLengthValidator, usernameValidator } from './form-validators';

@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-register',
  styleUrl: './register.component.scss',
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  readonly cancelled = output<void>();
  readonly registered = output<string>();

  private readonly formBuilder = inject(FormBuilder);
  private readonly api = inject(AuthApiService);
  protected readonly pending = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly form = this.formBuilder.nonNullable.group(
    {
      username: ['', [usernameValidator]],
      password: ['', [Validators.required, passwordLengthValidator]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: matchingPasswordsValidator },
  );

  protected submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid || this.pending()) {
      this.form.markAllAsTouched();
      return;
    }

    this.pending.set(true);
    this.api
      .register(this.form.getRawValue())
      .pipe(finalize(() => this.pending.set(false)))
      .subscribe({
        next: ({ username }) => {
          this.form.reset();
          this.registered.emit(username);
        },
        error: (error: unknown) => {
          const status = (error as { status?: number } | null)?.status;
          this.errorMessage.set(
            status === 409
              ? 'That username is already in use. Choose another one.'
              : 'We could not create your account. Please check the details and try again.',
          );
        },
      });
  }
}
