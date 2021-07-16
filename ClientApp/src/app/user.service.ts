import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import {RegisterTools} from './register';
import {LoginTools} from './login';

import { Tools } from './tools';
declare let Swal: any;

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
    try {
      return JSON.parse(localStorage.getItem('user')) as {
        username: string;
        displayName: string;
      };
    } catch {
      return undefined;
    }
  }

  public get token(): string {
    return localStorage.getItem('token');
  }

  registerBegin = RegisterTools.registerBegin;
  registerEnd = RegisterTools.registerEnd;
  loginBegin = LoginTools.loginBegin;
  loginEnd = LoginTools.loginEnd;

  // Rejestracja użytkownika.
  public async register(
    username: string,
    displayName: string,
    password: string
  ) {
    let newCredential = await this.registerBegin(username, displayName, password);
    try {
      await this.registerEnd(newCredential);
    } catch (err) {
      Tools.showErrorAlert(err.message ? err.message : err);
    }
  }


  public async login(username: string, password: string) {
    let credential = await this.loginBegin(username, password);
    try {
      await this.loginEnd(credential);
    } catch (e) {
      Tools.showErrorAlert('Could not verify assertion', e);
    }
  }

}
