import { environment } from './../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { UserService } from './user.service';

@Injectable({
  providedIn: 'root',
})
export class SecretService {
  apiUrl = environment.apiUrl;
  getSecret() {
    let token = this.userService.token;
    console.log(token);
    return this.httpClient.get(`http://localhost/api/secret`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  constructor(
    private httpClient: HttpClient,
    private userService: UserService
  ) {}
}
