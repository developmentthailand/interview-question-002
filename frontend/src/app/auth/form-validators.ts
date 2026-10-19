import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const usernameValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = String(control.value ?? '').trim();
  if (!value) {
    return { required: true };
  }

  if ([...value].length > 64) {
    return { usernameTooLong: true };
  }

  if (/\p{Cc}/u.test(value)) {
    return { usernameControlCharacter: true };
  }

  return null;
};

export const passwordLengthValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = String(control.value ?? '');
  const length = [...value].length;
  if (length < 15) {
    return { passwordTooShort: true };
  }

  if (length > 128) {
    return { passwordTooLong: true };
  }

  return null;
};

export const matchingPasswordsValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const confirmation = control.get('confirmPassword')?.value;
  return password === confirmation ? null : { passwordMismatch: true };
};
