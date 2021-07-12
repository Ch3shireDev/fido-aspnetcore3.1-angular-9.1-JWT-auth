import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { environment } from 'src/environments/environment';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  logout() {
    localStorage.removeItem('user');
    localStorage.removeItem('token');
  }
  api = environment.apiUrl;

  constructor(private httpClient: HttpClient) {}

  public get user(): { username: string; displayName: string } {
    return JSON.parse(localStorage.getItem('user')) as {
      username: string;
      displayName: string;
    };
  }

  public get token(): string {
    return localStorage.getItem('token');
  }

  public login(loginForm: FormGroup) {
    const formData = new FormData();
    formData.append('username', loginForm.get('username').value);
    formData.append('password', loginForm.get('password').value);
    return this.httpClient.post(`${this.api}/user/login-begin`, formData).pipe(
      map((success: { user: any }) => {
        console.log(success.user);
        localStorage.setItem('user', JSON.stringify(success.user));
        localStorage.setItem('token', success.user.token);
        return success;
      })
    );
  }

  public register(registerForm: FormGroup) {
    const formData = new FormData();
    formData.append('username', registerForm.get('username').value);
    formData.append('displayName', registerForm.get('displayName').value);
    formData.append('password1', registerForm.get('password1').value);
    formData.append('password2', registerForm.get('password2').value);

    return this.httpClient.post(`${this.api}/user/register-begin`, formData);
  }
}
