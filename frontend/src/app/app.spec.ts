import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { App } from './app';
import { appConfig } from './app.config';

describe('App authentication flow', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [...(appConfig.providers ?? []), provideHttpClientTesting()],
    }).compileComponents();
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('shows the login form with a masked password field', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('Welcome back');
    expect(root.querySelector('input#username')).not.toBeNull();
    expect(root.querySelector<HTMLInputElement>('input#password')?.type).toBe('password');
    expect(root.querySelector('button[type="submit"]')?.textContent).toContain('Sign in');
  });

  it('switches to registration and masks both password fields', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    findButton(root, 'Create an account').click();
    fixture.detectChanges();

    expect(root.textContent).toContain('Create your account');
    expect(root.querySelector<HTMLInputElement>('input#password')?.type).toBe('password');
    expect(root.querySelector<HTMLInputElement>('input#confirmPassword')?.type).toBe('password');
  });

  it('shows a mismatch message and prevents an invalid registration request', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    findButton(root, 'Create an account').click();
    fixture.detectChanges();
    setInput(root, 'username', 'narin');
    setInput(root, 'password', 'Correct Horse Battery Staple');
    const confirmation = setInput(root, 'confirmPassword', 'a different password');
    confirmation.dispatchEvent(new FocusEvent('blur'));
    fixture.detectChanges();

    expect(root.textContent).toContain('Passwords do not match.');
    expect((root.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);
    TestBed.inject(HttpTestingController).expectNone('/api/auth/register');
  });

  it('registers with the exact API contract, then returns to login with a success message', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    findButton(root, 'Create an account').click();
    fixture.detectChanges();
    setInput(root, 'username', 'Narin');
    setInput(root, 'password', 'Correct Horse Battery Staple');
    setInput(root, 'confirmPassword', 'Correct Horse Battery Staple');
    fixture.detectChanges();
    const submit = findButton(root, 'Create account', 'submit');
    expect(submit.disabled).toBe(false);
    submit.click();

    const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/register');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      username: 'Narin',
      password: 'Correct Horse Battery Staple',
      confirmPassword: 'Correct Horse Battery Staple',
    });
    request.flush({ id: 1, username: 'Narin' });
    fixture.detectChanges();

    expect(root.textContent).toContain('Welcome back');
    expect(root.textContent).toContain('Account created for Narin. Sign in to continue.');
    expect(root.querySelector<HTMLInputElement>('input#password')?.value).toBe('');
  });

  it('blocks duplicate registration attempts and displays the API conflict', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    findButton(root, 'Create an account').click();
    fixture.detectChanges();
    setInput(root, 'username', 'Narin');
    setInput(root, 'password', 'Correct Horse Battery Staple');
    setInput(root, 'confirmPassword', 'Correct Horse Battery Staple');
    fixture.detectChanges();
    const submit = findButton(root, 'Create account', 'submit');
    submit.click();

    const controller = TestBed.inject(HttpTestingController);
    const registration = controller.expectOne('/api/auth/register');
    fixture.detectChanges();
    expect(submit.disabled).toBe(true);
    submit.click();
    expect(controller.match('/api/auth/register')).toHaveLength(0);
    registration.flush({ title: 'Username already exists' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(root.textContent).toContain('That username is already in use. Choose another one.');
    expect(root.textContent).toContain('Create your account');
  });

  it('shows a generic login error and does not request the protected identity', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    setInput(root, 'username', 'narin');
    setInput(root, 'password', 'incorrect password');
    fixture.detectChanges();
    findButton(root, 'Sign in', 'submit').click();

    const controller = TestBed.inject(HttpTestingController);
    controller.expectOne('/api/auth/login').flush(
      { title: 'Invalid username or password' },
      { status: 401, statusText: 'Unauthorized' },
    );
    fixture.detectChanges();

    expect(root.textContent).toContain('Username or password is incorrect. Please try again.');
    expect(root.textContent).not.toContain('Welcome User :');
    controller.expectNone('/api/auth/me');
  });

  it('shows Welcome only after the protected API validates the access token', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    setInput(root, 'username', 'narin');
    setInput(root, 'password', 'Correct Horse Battery Staple');
    fixture.detectChanges();
    const submit = findButton(root, 'Sign in', 'submit');
    expect(submit.disabled).toBe(false);
    submit.click();

    const controller = TestBed.inject(HttpTestingController);
    const login = controller.expectOne('/api/auth/login');
    expect(login.request.body).toEqual({ username: 'narin', password: 'Correct Horse Battery Staple' });
    login.flush({ accessToken: 'signed-token', tokenType: 'Bearer', expiresAtUtc: '2026-10-20T12:15:00Z' });

    const me = controller.expectOne('/api/auth/me');
    expect(me.request.headers.get('Authorization')).toBe('Bearer signed-token');
    fixture.detectChanges();
    expect(root.textContent).not.toContain('Welcome User :');

    me.flush({ id: 14, username: 'narin' });
    fixture.detectChanges();

    expect(root.querySelector('#welcome-title')?.getAttribute('aria-label')).toBe('Welcome User : narin');
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('accessToken')).toBeNull();
  });

  it('clears a token and stays on login when protected validation fails', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    setInput(root, 'username', 'narin');
    setInput(root, 'password', 'Correct Horse Battery Staple');
    fixture.detectChanges();
    const submit = findButton(root, 'Sign in', 'submit');
    expect(submit.disabled).toBe(false);
    submit.click();
    const controller = TestBed.inject(HttpTestingController);
    controller.expectOne('/api/auth/login').flush({
      accessToken: 'invalid-token',
      tokenType: 'Bearer',
      expiresAtUtc: '2026-10-20T12:15:00Z',
    });
    controller.expectOne('/api/auth/me').flush({ status: 401 }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    expect(root.textContent).toContain('Welcome back');
    expect(root.textContent).not.toContain('Welcome User :');
    expect(root.textContent).toContain('Username or password is incorrect. Please try again.');
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('accessToken')).toBeNull();
  });
});

function setInput(root: HTMLElement, id: string, value: string): HTMLInputElement {
  const input = root.querySelector<HTMLInputElement>(`#${id}`);
  if (!input) {
    throw new Error(`Input #${id} was not rendered.`);
  }
  input.value = value;
  input.dispatchEvent(new Event('input', { bubbles: true }));
  return input;
}

function findButton(root: HTMLElement, label: string, type?: string): HTMLButtonElement {
  const button = Array.from(root.querySelectorAll<HTMLButtonElement>('button')).find(
    (candidate) => candidate.textContent?.includes(label) && (!type || candidate.type === type),
  );
  if (!button) {
    throw new Error(`Button with text "${label}" was not rendered.`);
  }
  return button;
}
